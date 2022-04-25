using Unity.MLAgents;
using Unity.MLAgents.Actuators;

namespace DroneProject
{
    /// <summary>
    /// Agent for recording demonstrations to be used with imitation learning when
    /// training <see cref="CavePilotVisual"/>.
    /// Action heuristics are provided by trained <see cref="CavePilotVector"/> model.
    /// </summary>
    public class CavePilotVisualDemoRecorder : Agent
    {
        private CavePilotVector m_ActionProvider;
        private ActionSegment<float> m_ContinuousActions;
        private StackedDepthSensorComponent m_Sensor;
        
        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            
            m_Sensor = GetComponentInChildren<StackedDepthSensorComponent>();
            m_ActionProvider = GetComponentInParent<CavePilotVector>();
            m_ActionProvider.ActionStepEvent += OnActionStep;
        }
        
        private void OnActionStep(int cycleStep, ActionSegment<float> actions)
        {
            // Agent cycle steps:
            // - 1 decision
            // - 2
            // ...
            // - 0 pre-decision
            
            if (cycleStep == 1)
            {
                m_ContinuousActions = actions;
                m_Sensor.TakeSnapshot();
                RequestDecision();
            }
        }
        
        /// <inheritdoc />
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var actions = actionsOut.ContinuousActions;
            actions[0] = m_ContinuousActions[0];
            actions[1] = m_ContinuousActions[1];
            actions[2] = m_ContinuousActions[2];
            actions[3] = m_ContinuousActions[3];
        }
    }
}