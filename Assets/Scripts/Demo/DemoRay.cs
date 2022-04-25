using UnityEngine;

namespace DroneProject
{
    /// <summary>
    /// Color coded ray visualization for displaying raycast info results.
    /// </summary>
    public class DemoRay : MonoBehaviour
    {
        [SerializeField] 
        private DemoColors m_DemoColors;
        
        private readonly int m_ColorID = Shader.PropertyToID("_Color");
        private readonly int m_EmissionID = Shader.PropertyToID("_EmissionColor");
        private MeshRenderer m_Renderer;
        private Material m_Material;
        
        private void Awake()
        {
            m_Renderer = GetComponent<MeshRenderer>();
            m_Material = m_Renderer.sharedMaterial;
        }

        /// <summary>
        /// Updates the ray visualization.
        /// </summary>
        /// <param name="raycastInfo">Surface raycastInfo info</param>
        public void ManagedUpdate(SurfaceRaycastInfo raycastInfo)
        {
            Color color = GetColor(raycastInfo);
            m_Material.SetColor(m_ColorID, color);
            m_Material.SetColor(m_EmissionID, color);

            float length = raycastInfo.Length * 0.5f;
            Transform cylinder = transform;

            Vector3 tmp = cylinder.localScale;
            tmp.y = length;
            cylinder.localScale = tmp;

            tmp = cylinder.localPosition;
            tmp.z = length;
            cylinder.localPosition = tmp;
        }

        private Color GetColor(SurfaceRaycastInfo raycastInfo)
        {
            if (!raycastInfo.HasHit) return m_DemoColors.None;
            if (!raycastInfo.HasValidHit) return m_DemoColors.Invalid;
            if (!raycastInfo.HitIsNew) return m_DemoColors.Coplanar;
            return raycastInfo.HitIsContinuous 
                ? m_DemoColors.Continuous 
                : m_DemoColors.Isolated;
        }
    }
}