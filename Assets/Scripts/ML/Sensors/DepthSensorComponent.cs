using System;
using UnityEngine;
using Unity.MLAgents.Sensors;
using UnityEngine.UI;

namespace DroneProject
{
    /// <summary>
    /// Component that wraps a <see cref="RenderTextureSensor"/>,
    /// converting depth texture values to visual observations.
    /// Depth values are written to rgb channels uniformly.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class DepthSensorComponent : SensorComponent, IDisposable
    {
        /// <summary>
        /// Name of the generated <see cref="RenderTextureSensor"/>.
        /// Note that changing this at runtime does not affect how the Agent sorts the sensors.
        /// </summary>
        public string SensorName
        {
            get => m_SensorName;
            set => m_SensorName = value;
        }
        [SerializeField] 
        protected string m_SensorName = "DepthSensor";

        /// <summary>
        /// Compression type for the render texture observation.
        /// </summary>
        public SensorCompressionType CompressionType
        {
            get => m_Compression;
            set
            {
                m_Compression = value;
                UpdateSensor();
            }
        }
        [SerializeField] 
        protected SensorCompressionType m_Compression = SensorCompressionType.PNG;

        /// <summary>
        /// Filter Mode applied to the render texture.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        public FilterMode FilterMode
        {
            get => m_FilterMode;
            set => m_FilterMode = value;
        }
        [SerializeField] 
        protected FilterMode m_FilterMode = FilterMode.Point;
        
        /// <summary>
        /// Whether the RenderTexture observation should be converted to grayscale or not.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        // NOTE we need an rgb render texture even if a single color channel for depth
        // would suffice, because ObservationWriterExtension.WriteTexture averages the 
        // rgb channel values when reducing to grayscale.
        public bool Grayscale
        {
            get => m_Grayscale;
            set => m_Grayscale = value;
        }
        [SerializeField]
        protected bool m_Grayscale;
        
        /// <summary>
        /// Exponent applied when mapping depths to brightness.
        /// Higher exponents yield more contrast at small distances.
        /// Set to 1 for linear mapping.
        /// </summary>
        public int Exponent
        {
            get => m_Exponent;
            set
            {
                m_Exponent = value;
                UpdateMaterial();
            }
        }
        [SerializeField, Range(1, 16)] 
        protected int m_Exponent = 1;

        /// <summary>
        /// Whether to stack previous observations. Using 1 means no previous observations.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        public int ObservationStacks
        {
            get => m_ObservationStacks;
            set => m_ObservationStacks = value;
        }

        [SerializeField]
        [Range(1, 50)]
        [Tooltip("Number of frames that will be stacked before being fed to the neural network.")]
        protected int m_ObservationStacks = 1;

        /// <summary>
        /// Width of the generated observation.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        public int Width
        {
            get => m_Width;
            set => m_Width = value;
        }
        [SerializeField] 
        protected int m_Width = 128;

        /// <summary>
        /// Height of the generated observation.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        public int Height
        {
            get => m_Height;
            set => m_Height = value;
        }
        [SerializeField] 
        protected int m_Height = 128;
        
        [SerializeField, Tooltip("The corresponding material must be set in builds")] 
        protected Material m_Material;
        protected RenderTexture m_Texture;
        
        private RenderTextureSensor m_Sensor;
        
        protected virtual DepthTextureMode Mode => DepthTextureMode.Depth;
        protected virtual string ShaderName => "Sensors/Depth";
        
        private static readonly int s_ExponentID = Shader.PropertyToID("_Exponent");


        private void OnValidate()
        {
            UpdateSensor();
            UpdateMaterial();
        }

        /// <inheritdoc/>
        public override ISensor[] CreateSensors()
        {
#if (UNITY_EDITOR)
            if (Application.isPlaying)
            {
                EditorUtil.HideBehaviorParametersEditor();
            }
#endif
            Dispose();
            Initialize();
            
            m_Sensor = new RenderTextureSensor(
                m_Texture, m_Grayscale, SensorName, m_Compression);
            
            return ObservationStacks != 1
                ? new ISensor[] { new StackingSensor(m_Sensor, ObservationStacks) }
                : new ISensor[] { m_Sensor };
        }
        
        /// <summary>
        /// Initializes the render texture and material.
        /// </summary>
        protected virtual void Initialize()
        {
            m_Texture = new RenderTexture(Width, Height, 16, RenderTextureFormat.ARGB32)
            {
                filterMode = m_FilterMode,
                hideFlags = HideFlags.DontSave
            };

            Camera cam = GetComponent<Camera>();
            cam.depthTextureMode = Mode;
            cam.targetTexture = m_Texture;

            if (m_Material == null)
            {
                m_Material = new Material(Shader.Find(ShaderName));
                Debug.LogWarning("Creating material from shader " + ShaderName
                    + " - Set material in inspector for builds");
            }
            else
            {
                // Create unique instance copy.
                m_Material = new Material(m_Material);
            }
            m_Material.SetInt(s_ExponentID, m_Exponent);

            // Optional UI view.
            RawImage ui = FindObjectOfType<RawImage>();
            if (ui != null)
            {
                ui.texture = m_Texture;
            }
        }
        
        /// <summary>
        /// Releases the RenderTexture.
        /// </summary>
        public void ReleaseTexture()
        {
            if (m_Texture != null)
            {
                m_Texture.Release();
            }
        }

        /// <summary>
        /// Renders the camera view to the render texture, using the specified material / shader.
        /// </summary>
        /// <param name="source">Source Render Texture</param>
        /// <param name="destination">Destination Render Texture</param>
        protected virtual void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (m_Material != null)
            {
                Graphics.Blit(source, destination, m_Material, 0);
            }
        }

        /// <summary>
        /// Update fields that are safe to change on the Sensor at runtime.
        /// </summary>
        private void UpdateSensor()
        {
            if (m_Sensor != null)
            {
                m_Sensor.CompressionType = CompressionType;
            }
        }
        
        /// <summary>
        /// Updates the material / shader properties.
        /// </summary>
        private void UpdateMaterial()
        {
            if (m_Material != null)
            {
                m_Material.SetInt(s_ExponentID, m_Exponent);
            }
        }

        /// <summary>
        /// Clean up the sensor created by CreateSensors().
        /// </summary>
        public void Dispose()
        {
            ReleaseTexture();
            
            if (m_Sensor != null)
            {
                m_Sensor.Dispose();
                m_Sensor = null;
            }
        }
    }
}