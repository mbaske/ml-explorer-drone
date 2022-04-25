using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace DroneProject
{
    /// <summary>
    /// Contains the mesh for rendering a single surface cube.
    /// Material color indicates raycast info results.
    /// </summary>
    public class SurfaceHighlight : MonoBehaviour
    {
        /// <summary>
        /// Invoked when fade out animation is done and the object cane be disabled.
        /// </summary>
        public event Action<SurfaceHighlight> AnimationDoneEvent;
        
        private const float k_VisibilityThresh = 0.01f;
        
        [SerializeField, Tooltip("Inverse fade")] 
        private float m_Persistence = 0.9f;
        [SerializeField] 
        private DemoColors m_DemoColors;
        private Color m_Color;
        private float m_Brightness;
        
        private readonly int m_EmissionID = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock m_Block;
        private MeshRenderer m_Renderer;
        private MeshCollider m_Collider;
        
        private Mesh m_Mesh;
        private MeshFilter m_MeshFilter;
        private Vector3[] m_TmpVertices;
        private List<Vector3> m_Vertices;
        private List<Vector3> m_Normals;
        private List<int> m_Triangles;

        /// <summary>
        /// Initializes the highlight.
        /// </summary>
        /// <param name="raycastInfo">Surface raycastInfo info</param>
        /// <param name="cube">Surface cube</param>
        public void Initialize(SurfaceRaycastInfo raycastInfo, SurfaceCube cube)
        {
            if (m_Mesh == null)
            {
                InitMesh();
                m_Block = new MaterialPropertyBlock();
                m_Renderer = GetComponent<MeshRenderer>();
                m_Collider = GetComponent<MeshCollider>();
            }
            else
            {
                m_Mesh.Clear();
                m_Vertices.Clear();
                m_Normals.Clear();
            }

            Matrix4x4 m = transform.worldToLocalMatrix;
            foreach (MeshFace face in cube.Faces)
            {
                face.GetLocalized(m, m_TmpVertices);
                m_Vertices.AddRange(m_TmpVertices);
                m_Normals.AddRange(face.Normals);
            }
            UpdateMesh();
            
            m_Brightness = 1;
            m_Color = GetColor(raycastInfo);
            UpdateColor();
        }

        private void InitMesh()
        {
            m_Mesh = new Mesh();
            m_Mesh.MarkDynamic();
            m_MeshFilter = GetComponent<MeshFilter>();
            m_TmpVertices = new Vector3[3];
            m_Vertices = new List<Vector3>();
            m_Normals = new List<Vector3>();
            m_Triangles = new List<int>();
        }

        private void UpdateMesh()
        {
            m_Mesh.SetVertices(m_Vertices);
            m_Mesh.SetNormals(m_Normals);
            m_Mesh.SetTriangles(UpdateTriangles(), 0);
            m_Mesh.RecalculateBounds();
            m_MeshFilter.sharedMesh = m_Mesh;
            m_Collider.sharedMesh = m_Mesh;
        }
        
        public void CancelAnimation()
        {
            m_Brightness = 0;
            UpdateColor();
            AnimationDoneEvent?.Invoke(this);
        }

        private void Update()
        {
            m_Brightness *= m_Persistence;
            
            if (m_Brightness > k_VisibilityThresh)
            {
                UpdateColor();
            }
            else
            {
                AnimationDoneEvent?.Invoke(this);
            }
        }
        
        private Color GetColor(SurfaceRaycastInfo raycastInfo)
        {
            if (!raycastInfo.HitIsNew) return m_DemoColors.Coplanar;
            
            return raycastInfo.HitIsContinuous 
                ? m_DemoColors.Continuous 
                : m_DemoColors.Isolated;
        }

        private void UpdateColor()
        { 
            Color col = Color.Lerp(Color.black, m_Color, m_Brightness);
            m_Renderer.GetPropertyBlock(m_Block);
            m_Block.SetColor(m_EmissionID, col);
            m_Renderer.SetPropertyBlock(m_Block);
        }

        /// <summary>
        /// Updates the triangles array. Each face has its own normal
        /// (no welding), so we can simply enumerate indices matching
        /// the vertex count.
        /// </summary>
        /// <returns></returns>
        private List<int> UpdateTriangles()
        {
            int n = m_Triangles.Count;
            int d = m_Vertices.Count - n;

            if (d > 0)
            {
                m_Triangles.AddRange(Enumerable.Range(n, d).ToArray());
            }
            else if (d < 0)
            {
                m_Triangles.RemoveRange(n + d, -d);
            }

            return m_Triangles;
        }
    }
}