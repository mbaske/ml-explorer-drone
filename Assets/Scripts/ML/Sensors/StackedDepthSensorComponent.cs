using UnityEngine;
using Unity.MLAgents.Sensors;

namespace DroneProject
{
    /// <summary>
    /// Component that wraps a <see cref="RenderTextureSensor"/>,
    /// converting depth texture values to visual observations.
    /// Depth values are written to rgb channels consecutively,
    /// when <see cref="TakeSnapshot"/> is invoked. The differences
    /// between rgb channels encode agent velocity and acceleration.
    /// 
    /// TODO Unity might drop render frames at high training time scales,
    /// resulting in larger deltas between rgb channel contents.
    /// I recommend comparing the UI outputs at different time scales
    /// and perhaps training in realtime if they happen to differ. 
    /// </summary>
    public class StackedDepthSensorComponent : DepthSensorComponent
    {
        protected override string ShaderName => "Sensors/StackedDepth";
        
        private bool m_SnapshotFlag;
        
        private static readonly int s_RenderTexID = Shader.PropertyToID("_RenderTex");
        private static readonly int s_SnapshotID = Shader.PropertyToID("_Snapshot");

        /// <inheritdoc/>
        protected override void Initialize()
        {
            base.Initialize();
            
            m_Material.SetTexture(s_RenderTexID, m_Texture);
        }
        
        /// <summary>
        /// Take camera snapshot at next OnRenderImage.
        /// Channel stacking order:
        /// - Second last depth texture is copied from green to blue channel.
        /// - Previous depth texture is copied from red to green channel.
        /// - Current depth texture is stored in red channel.
        /// </summary>
        public void TakeSnapshot()
        {
            m_SnapshotFlag = true;
        }

        /// <summary>
        /// Renders the camera view to the render texture, using the specified material / shader.
        /// </summary>
        /// <param name="source">Source Render Texture</param>
        /// <param name="destination">Destination Render Texture</param>
        protected override void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (m_Material != null)
            {
                m_Material.SetFloat(s_SnapshotID, m_SnapshotFlag ? 1 : 0);
                Graphics.Blit(source, destination, m_Material, 0);
        
                m_SnapshotFlag = false;
            }
        }
    }
}
