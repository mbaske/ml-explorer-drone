using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace DroneProject
{
    /// <summary>
    /// Agent that pilots the drone by providing velocity vectors and look directions
    /// for the nested <see cref="RotorControlAgent"/>.
    /// </summary>
    public abstract class PilotAgent : Agent
    {
        [SerializeField, Tooltip("Penalty is applied at collision enter and stay")]
        private float m_CollisionPenalty = 0.1f;
        private int m_CollisionCount;
        
        [SerializeField, Tooltip("Log stats to TensorBoard, interval in decision steps")]
        protected int m_StatsInterval;
        protected StatsRecorder m_Stats;
        
        [Space, SerializeField, Tooltip("Scales normalized actions, equivalent to max speed")]
        private float m_VelocityScale = 5;
        
        [SerializeField, Tooltip("Scales normalized action for look angle change")]
        private float m_LookAngleScale = 1;

        private RotorControlAgent m_RotorCtrl;
        protected Drone m_Drone;
        
        protected int m_DecisionInterval;
        // Decision count is only used for sending stats.
        private int m_DecisionCount;
        
        
        /// <inheritdoc />
        public override void Initialize()
        {
            m_Stats = Academy.Instance.StatsRecorder;
            m_DecisionInterval = GetComponent<DecisionRequester>().DecisionPeriod;
            
            m_RotorCtrl = GetComponentInChildren<RotorControlAgent>();
            m_RotorCtrl.Initialize();
            m_RotorCtrl.DecisionInterval = m_DecisionInterval;
            
            m_Drone = m_RotorCtrl.Drone;
            m_Drone.CollisionEvent += OnCollision;
            m_Drone.CollisionTimeoutEvent += EndEpisode;
            m_Drone.TipOverEvent += EndEpisode;
        }
        
        /// <inheritdoc />
        public override void OnEpisodeBegin()
        {
            m_DecisionCount = 0;
            m_CollisionCount = 0;
            
            ResetDrone();
        }

        /// <summary>
        /// Resets the drone and rotor control agent.
        /// </summary>
        protected virtual void ResetDrone()
        {
            m_RotorCtrl.EndEpisode();
        }

        /// <summary>
        /// Observes the drone's basic physical properties (velocity, angular velocity and inclination).
        /// </summary>
        /// <param name="sensor">Vector sensor</param>
        /// <param name="velocityCoefficient">Velocity curve coefficient</param>
        protected void ObserveDronePhysics(VectorSensor sensor, float velocityCoefficient = 0.5f)
        {
            sensor.AddObservation(MLUtil.Sigmoid(m_Drone.LocalVelocity, velocityCoefficient));
            sensor.AddObservation(MLUtil.Sigmoid(m_Drone.LocalAngularVelocity));
            sensor.AddObservation(m_Drone.Inclination);
        }
        
        /// <inheritdoc />
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            // Null actions -> hover.
        }
        
        /// <inheritdoc />
        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            if (StepCount % m_DecisionInterval == 1)
            {
                // First cycle step.
                var a = actionBuffers.ContinuousActions;
                
                m_RotorCtrl.SetLocalTargets(
                    m_VelocityScale * new Vector3(a[0], a[1], a[2]),
                    m_LookAngleScale * a[3]);

                m_RotorCtrl.RequestDecision();
            }

            m_RotorCtrl.RequestAction();
        }

        /// <summary>
        /// Whether to send stats to TensorBoard.
        /// </summary>
        /// <returns>true is stats should be sent</returns>
        protected bool SendStats()
        {
            bool send = ++m_DecisionCount % m_StatsInterval == 0;

            if (send)
            {
                // Shared stats for all derived agents.
                m_Stats.Add("Agent/Pilot Velocity", m_Drone.WorldVelocity.magnitude);
                m_Stats.Add("Agent/Pilot Angular Velocity", m_Drone.WorldAngularVelocity.magnitude);
                m_Stats.Add("Agent/Pilot Collision Ratio", m_CollisionCount / (float) m_DecisionCount);
            }

            return send;
        }

        private void OnCollision(Collision collision)
        {
            AddReward(-m_CollisionPenalty);
            m_CollisionCount++;
        }
    }
}
