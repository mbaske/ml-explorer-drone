using UnityEngine;
using Unity.MLAgents.Sensors;

namespace DroneProject
{
    /// <summary>
    /// Component that wraps a <see cref="RenderTextureSensor"/>,
    /// converting depth and normal texture values to visual observations.
    /// Normals are in agent view space, encoded in two color channels.
    /// </summary>
    public class DepthNormalsSensorComponent : DepthSensorComponent
    {
        protected override DepthTextureMode Mode => DepthTextureMode.DepthNormals;
        protected override string ShaderName => "Sensors/DepthNormals";
    }
}