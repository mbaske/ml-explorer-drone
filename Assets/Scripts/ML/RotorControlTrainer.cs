using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace DroneProject
{
    /// <summary>
    /// Trains <see cref="RotorControlAgent"/>.
    /// </summary>
    public class RotorControlTrainer : RotorControlAgent
    {
        [SerializeField, Tooltip("Log stats to TensorBoard, interval in decision steps")]
        private int m_StatsInterval;
        private StatsRecorder m_Stats;

        private TrainingValueGenerator m_Generator;
        private int m_DecisionCount;

        private Vector3 m_DefPos;
        
        // Allow some lag between expected and measured velocity.
        private Vector3[] m_VelocityBuffer;
        private int m_VelocityBufferIndex;
        private const int k_VelocityBufferSize = 10;
        private float m_VelocityError;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            m_DefPos = Drone.WorldPosition;
            Drone.TipOverEvent += EndEpisode;
            
            // Standalone agent has its own DecisionRequester.
            DecisionInterval = GetComponent<DecisionRequester>().DecisionPeriod;

            m_VelocityBuffer = new Vector3[k_VelocityBufferSize];
            m_Stats = Academy.Instance.StatsRecorder;
            
            m_Generator = GetComponent<TrainingValueGenerator>();
            m_Generator.Initialize();
        }

        /// <inheritdoc />
        public override void OnEpisodeBegin()
        {
            base.OnEpisodeBegin();

            m_VelocityBufferIndex = 0;
            Array.Clear(m_VelocityBuffer, 0, k_VelocityBufferSize);

            m_DecisionCount = 0;
            m_Generator.ManagedReset();
            
            Drone.ResetTo(m_DefPos);
        }
        
        /// <inheritdoc />
        public override void CollectObservations(VectorSensor sensor)
        {
            SetWorldTargets(m_Generator.WorldVelocity, m_Generator.WorldLookDirection);
            base.CollectObservations(sensor);

            AddRewards();
        }

        private void AddRewards()
        {
            m_VelocityBuffer[m_VelocityBufferIndex] = m_Generator.WorldVelocity;
            m_VelocityBufferIndex++;
            m_VelocityBufferIndex %= k_VelocityBufferSize;  

            float minSqrErr = Mathf.Infinity;
            Vector3 velocity = Drone.WorldVelocity;

            for (int i = 0; i < k_VelocityBufferSize; i++)
            {
                minSqrErr = Mathf.Min(minSqrErr, (m_VelocityBuffer[i] - velocity).sqrMagnitude);
            }

            // TBD coefficients.

            m_VelocityError = Mathf.Sqrt(minSqrErr); 
            float velocityCoefficient = m_Generator.IsMoving ? 1 : 10;
            float velocityReward = MLUtil.Reward(m_VelocityError, velocityCoefficient);

            float stabilityError = Drone.WorldAngularVelocity.magnitude;
            float stabilityReward = MLUtil.Reward(stabilityError, 2);

            float orientationError = Mathf.Abs(LocalLookAngle);
            float orientationReward = MLUtil.Reward(orientationError, 10);
            
            AddReward(velocityReward * stabilityReward * orientationReward);
            
            if (++m_DecisionCount % m_StatsInterval == 0)
            {
                m_Stats.Add("Agent/Ctrl Velocity Error", m_VelocityError);
                m_Stats.Add("Agent/Ctrl Velocity Reward", velocityReward);
                m_Stats.Add("Agent/Ctrl Stability Error", stabilityError);
                m_Stats.Add("Agent/Ctrl Stability Reward", stabilityReward);
                m_Stats.Add("Agent/Ctrl Orientation Error", orientationError);
                m_Stats.Add("Agent/Ctrl Orientation Reward", orientationReward);
            }
        }

        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            base.OnActionReceived(actionBuffers);
            m_Generator.ManagedUpdate(Time.fixedDeltaTime);
        }

        private void OnDrawGizmosSelected()
        {
            if (Drone != null)
            {
                Vector3 p = Drone.WorldPosition;
                Gizmos.color = Color.gray;
                Gizmos.DrawRay(p, Drone.WorldForwardXZ);
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(p, Drone.WorldVelocity);
                Gizmos.color = Color.green;
                Gizmos.DrawRay(p, m_Generator.WorldVelocity);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(p, m_VelocityError);
            }
        }
    }
}