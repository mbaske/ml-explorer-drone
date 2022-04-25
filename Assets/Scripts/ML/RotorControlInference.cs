using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;
using Unity.MLAgents;
using UnityEngine;

namespace DroneProject
{
    /// <summary>
    /// Used for testing trained rotor control model.
    /// </summary>
    public class RotorControlInference: RotorControlAgent
    {
        [SerializeField, Range(-5f, 5f)] private float m_Roll;
        [SerializeField, Range(-5f, 5f)] private float m_Climb;
        [SerializeField, Range(-5f, 5f)] private float m_Pitch;
        [SerializeField, Range(-1f, 1f)] private float m_Look;
        [SerializeField] private bool m_Hover;
        
        private BehaviorParameters m_Params;
        
        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            Drone.TipOverEvent += EndEpisode;
            
            // Standalone agent has its own DecisionRequester.
            DecisionInterval = GetComponent<DecisionRequester>().DecisionPeriod;
            m_Params = GetComponent<BehaviorParameters>();
        }
        
        /// <inheritdoc />
        public override void CollectObservations(VectorSensor sensor)
        {
            SetLocalTargets(new Vector3(m_Roll, m_Climb, m_Pitch), m_Look);

            m_Params.BehaviorType = m_Hover ? BehaviorType.HeuristicOnly : BehaviorType.InferenceOnly;

            base.CollectObservations(sensor);
        }
    }
}