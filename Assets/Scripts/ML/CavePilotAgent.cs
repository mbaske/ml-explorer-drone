using Unity.MLAgents.Sensors;
using UnityEngine;

namespace DroneProject
{
    /// <summary>
    /// Agent that pilots the drone through a cave.
    /// </summary>
    public abstract class CavePilotAgent : PilotAgent
    {
        [SerializeField, Tooltip("Whether training is enabled (rewards and stats)")]
        protected bool m_Train;
        
        [SerializeField] 
        private Cave m_Cave;
        private CaveChunks m_Chunks;
        
        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            m_Cave.Initialize();
            m_Chunks = m_Cave.GetComponent<CaveChunks>();
            m_Chunks.Initialize();
        }

        /// <inheritdoc />
        protected override void ResetDrone()
        {
            base.ResetDrone();
            m_Chunks.ManagedReset();
            m_Drone.ResetTo(m_Cave.GetRandomSpawnPose());
        }
        
        /// <inheritdoc />
        public override void CollectObservations(VectorSensor sensor)
        {
            if (m_Drone.IsReady)
            {
                m_Chunks.ManagedUpdate(m_Drone.transform.localPosition); 
            }
            
            if (m_Train)
            {
                AddRewards(); 
            }
        }

        private void AddRewards()
        {
            // TBD coefficients.
            Ray path = m_Cave.GetRayAt(m_Drone.WorldPosition);
            float speed = Vector3.Dot(path.direction, m_Drone.WorldVelocity);
            float speedReward = MLUtil.Sigmoid(speed, 0.5f);
            float offset = path.Distance(m_Drone.WorldPosition);
            float pathReward = MLUtil.Reward(offset);

            AddReward(speedReward * pathReward);
            
            if (SendStats())
            {
                m_Stats.Add("Agent/Cave Path Offset", offset);
                m_Stats.Add("Agent/Cave Path Reward", pathReward);
                m_Stats.Add("Agent/Cave Speed", speed);
                m_Stats.Add("Agent/Cave Speed Reward", speedReward);
                m_Stats.Add("Agent/Cave Heading", 
                    Vector3.Dot(path.direction, m_Drone.WorldVelocity.normalized));
            }
        }
    }
}