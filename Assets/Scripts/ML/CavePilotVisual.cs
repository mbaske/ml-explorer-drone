using Unity.MLAgents.Actuators;

namespace DroneProject
{
    /// <summary>
    /// Cave pilot agent that uses visual observations only.
    /// </summary>
    public class CavePilotVisual : CavePilotAgent
    {
        private StackedDepthSensorComponent m_Sensor;
        
        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            m_Sensor = GetComponentInChildren<StackedDepthSensorComponent>();
        }
        
        /// <inheritdoc />
        public override void OnEpisodeBegin()
        {
            base.OnEpisodeBegin();
            m_Sensor.ReleaseTexture();
        }

        /// <inheritdoc />
        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            base.OnActionReceived(actionBuffers);

            if (StepCount % m_DecisionInterval == 0)
            {
                // Last cycle step.
                // Must update render texture BEFORE next decision.
                m_Sensor.TakeSnapshot();
            }
        }
    }
}
