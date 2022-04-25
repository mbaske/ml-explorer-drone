using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace DroneProject
{
    /// <summary>
    /// Maps the drone's environment using a single raycastInfo per time step.
    /// Generates move and look directions for nested <see cref="RotorControlAgent"/>,
    /// as well as angles for ray directions.
    /// </summary>
    public class MappingAgent : PilotAgent
    {
        [SerializeField, Tooltip("Whether training is enabled (rewards and stats)")]
        private bool m_Train;
        
        [SerializeField, Tooltip("Max ray angle up/down")]
        private float m_MaxRayAngle = 60;
        // [SerializeField, Tooltip("Max ray angles left/right, up/down")]
        // private Vector2 m_MaxRayAngles;

        [SerializeField, Tooltip("Max ray length")]
        private float m_RayLength = 5;

        [Space, SerializeField, Tooltip("Aligned with ray direction")]
        private Transform m_RayHelper;
        private DemoRay m_DemoRay;
        private bool m_ShowRay;

        [SerializeField] 
        private Transform m_SpawnPoints;

        [SerializeField] 
        private SurfaceReconstruction m_Surface;

        private const float k_EnergyPenaltyFactor = 0.01f;
        private const int k_Mask = Layers.DetectableMask;
        
        private static readonly Vector3[] s_Axes = new Vector3[5]
        {
            Vector3.left, Vector3.right, Vector3.down, Vector3.up, Vector3.back
        };

        // Index range of ray actions.
        private int m_MinRayAction;
        private int m_MaxRayAction;
        
        // Counter for continuous raycastInfo hit insertions.
        private int m_ContinuityCount;
        // Total surface area growth between decisions.
        private float m_SurfaceAreaGrowth;

        private DepthNormalsSensorComponent m_Sensor;
        

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            m_Sensor = GetComponentInChildren<DepthNormalsSensorComponent>();
            
            // Number of actions depends on decision interval, 
            // because we're casting one ray per time step.
            m_MinRayAction = 4; // after move vector (3) + look angle (1).
            m_MaxRayAction = m_MinRayAction + m_DecisionInterval;
            // m_MaxRayAction = m_MinRayAction + m_DecisionInterval * 2;

            var param = GetComponent<BehaviorParameters>().BrainParameters;
            var spec = param.ActionSpec;
            spec.NumContinuousActions = m_MaxRayAction;
            param.ActionSpec = spec;
            
            m_Surface.Initialize();
            m_Surface.RaycastEvent += OnSurfaceRaycast;
            
            if (!m_Train && m_RayHelper.childCount > 0)
            {
                m_DemoRay = m_RayHelper.GetComponentInChildren<DemoRay>();
                m_DemoRay.gameObject.SetActive(true);
                m_ShowRay = true;
            }
            
            Debug.LogWarningFormat("Launching in {0} mode", m_Train ? "training" : "demo");
        }

        /// <inheritdoc />
        public override void OnEpisodeBegin()
        {
            base.OnEpisodeBegin();
            
            m_ContinuityCount = 0;
            m_SurfaceAreaGrowth = 0;
            m_Surface.ManagedReset();
            m_Sensor.ReleaseTexture();
        }
        
        /// <inheritdoc />
        protected override void ResetDrone()
        {
            base.ResetDrone();
            
            int i = Random.Range(0, m_SpawnPoints.childCount);
            Vector3 pos = m_SpawnPoints.GetChild(i).position;
            m_Drone.ResetTo(pos);
        }

        /// <inheritdoc />
        public override void CollectObservations(VectorSensor sensor)
        {
            ObserveDronePhysics(sensor);
            ObserveProximity(sensor);

            if (m_Train)
            {
                AddRewards(); 
            }
        }

        /// <summary>
        /// Short range sensing of colliders along drone's cardinal axes.
        /// </summary>
        /// <param name="sensor">Vector sensor</param>
        /// <param name="range">Detection range (ray length)</param>
        /// <param name="radius">Spherecast radius</param>
        private void ObserveProximity(VectorSensor sensor, float range = 1, float radius = 0.25f)
        {
            Quaternion q = m_Drone.RotationY;
            Vector3 p = m_Drone.WorldPosition;

            for (int i = 0; i < 5; i++)
            {
                Vector3 dir = q * s_Axes[i];
                if (Physics.SphereCast(p - radius * dir, radius, dir, 
                        out RaycastHit hit, range, k_Mask))
                {
                    sensor.AddObservation(hit.distance / range * 2 - 1);
                    // Debug.DrawLine(p, hit.point, Color.red);
                }
                else
                {
                    sensor.AddObservation(1);
                    // Debug.DrawRay(p, dir * range, Color.green);
                }
            }
        }

        private void AddRewards()
        {
            float continuityRatio = m_ContinuityCount / (float) m_DecisionInterval;
            float surfaceAreaReward = MLUtil.Sigmoid(m_SurfaceAreaGrowth, 4); // TBD
            float energyPenalty = (m_Drone.WorldVelocity.magnitude + m_Drone.WorldAngularVelocity.magnitude) 
                                  * k_EnergyPenaltyFactor;
            
            AddReward(surfaceAreaReward * continuityRatio - energyPenalty);

            if (SendStats())
            {
                m_Stats.Add("Agent/Map Energy Penalty", energyPenalty);
                m_Stats.Add("Agent/Map Continuity Ratio", continuityRatio);
                m_Stats.Add("Agent/Map Surface Area Reward", surfaceAreaReward);
                m_Stats.Add("Agent/Map Surface Area Growth", m_SurfaceAreaGrowth);
            }
            
            m_ContinuityCount = 0;
        }
        
        /// <inheritdoc />
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            // Hover + random ray direction.
            var actions = actionsOut.ContinuousActions;
            for (int i = m_MinRayAction; i < m_MaxRayAction; i++)
            {
                actions[i] = Random.Range(-1f, 1f);
            }
        }
        
        /// <inheritdoc />
        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            base.OnActionReceived(actionBuffers);

            if (m_Drone.IsReady)
            {
                CastSurfaceRay(actionBuffers.ContinuousActions);
            }

            if (StepCount % m_DecisionInterval == 0)
            {
                // Last cycle step.
                // Need to update the meshes BEFORE next decision step,
                // so that the depth-normals sensor can see the changes.
                m_SurfaceAreaGrowth = m_Surface.UpdateMeshes();
            }
        }

        /// <summary>
        /// Casts a single ray with direction based on agent actions.
        /// Actions contain a batch of ray directions, but we only
        /// cast one ray per time step.
        /// </summary>
        /// <param name="actions">Agent actions</param>
        private void CastSurfaceRay(ActionSegment<float> actions)
        {
            // Ray index for current agent cycle step.
            int i = m_MinRayAction + (StepCount - 1) % m_DecisionInterval;
            // int i = m_MinRayAction + (StepCount - 1) % m_DecisionInterval * 2;

            m_RayHelper.localRotation = Quaternion.AngleAxis(actions[i] * m_MaxRayAngle, Vector3.right);
            // m_RayHelper.localRotation = Quaternion.Euler(
            //     m_MaxRayAngles.y * actions[i], m_MaxRayAngles.x * actions[i + 1], 0);
            
            // Will invoke OnSurfaceRaycast below.
            m_Surface.CastRay(new SurfaceRaycastInfo
            {
                Origin = m_RayHelper.position,
                Direction = m_RayHelper.forward,
                Length = m_RayLength
            });
        }
        
        private void OnSurfaceRaycast(SurfaceRaycastInfo raycastInfo, SurfaceCube cube)
        {
            if (raycastInfo.HasValidHit && raycastInfo.HitIsNew && raycastInfo.HitIsContinuous)
            {
                m_ContinuityCount++;
            }

            if (m_ShowRay)
            {
                m_DemoRay.ManagedUpdate(raycastInfo);
            } 
        }
    }
}