using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System;

namespace DroneProject
{
    /// <summary>
    /// Cave pilot agent that uses vector observations.
    /// </summary>
    public class CavePilotVector : CavePilotAgent
    {
        /// <summary>
        /// Invoked at each agent step, used by <see cref="CavePilotVisualDemoRecorder"/>.
        /// </summary>
        public event Action<int, ActionSegment<float>> ActionStepEvent;

        private BatchedRayDetection m_RayDetection;
        
        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            m_RayDetection = GetComponentInChildren<BatchedRayDetection>();
        }
        
        /// <inheritdoc />
        public override void OnEpisodeBegin()
        {
            base.OnEpisodeBegin();
            m_RayDetection.Clear();
        }
        
        /// <inheritdoc />
        public override void CollectObservations(VectorSensor sensor)
        {
            base.CollectObservations(sensor);
            
            ObserveDronePhysics(sensor);
            m_RayDetection.AddDistances(sensor);
        }
        
        /// <inheritdoc />
        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            if (!m_Train)
            {
                // Provides actions for CavePilotVisualDemoRecorder.
                ActionStepEvent?.Invoke(StepCount % m_DecisionInterval, 
                    actionBuffers.ContinuousActions);
            }
            
            m_RayDetection.BatchRaycast();
            base.OnActionReceived(actionBuffers);
        }
    }
}