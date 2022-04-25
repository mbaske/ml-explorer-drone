using UnityEngine;
using System;

namespace DroneProject
{
    /// <summary>
    /// A single surface triangle.
    /// </summary>
    public class MeshFace : IPoolable
    {
        /// <summary>
        /// Pool, we reuse mesh faces.
        /// </summary>
        private static readonly Pool<MeshFace> s_Pool = Pool<MeshFace>.Instance;

        /// <summary>
        /// Factory.
        /// </summary>
        /// <param name="a">World vertex</param>
        /// <param name="b">World vertex</param>
        /// <param name="c">World vertex</param>
        /// <returns>New or pooled face</returns>
        public static MeshFace Pooled(Vector3 a, Vector3 b, Vector3 c)
        {
            return s_Pool.RetrieveItem().Initialize(a, b, c);
        }

        /// <summary>
        /// The maximum allowed error when checking for validity.
        /// Error is sum(unsigned distances) from the vertices to the
        /// underlying collider surface, measured along the face's normal.
        /// </summary>
        private const float k_MaxError = 0.2f;

        /// <summary>
        /// Face offset from underlying collider / surface point.
        /// Used to prevent plane fighting in demo mode.
        /// The drone's sensor only sees the surface layer anyway.
        /// </summary>
        private const float k_RenderOffset = 0.05f;


        /// <summary>
        /// Whether the face is valid, meaning its vertices are
        /// close enough to the underlying collider (see max error).
        /// 
        /// NOTE: checking for validity is actually cheating a bit, 
        /// because we're casting additional rays at the vertex positions, 
        /// rather than exclusively relying on already detected points.
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        /// Face area.
        /// </summary>
        public float Area { get; private set; }

        /// <summary>
        /// Face normal array. The three normals identical, no welding.
        /// </summary>
        public Vector3[] Normals { get; private set; } = new Vector3[3];
        
        /// <summary>
        /// Face local vertex array.
        /// </summary>
        public Vector3[] Vertices { get; private set; } = new Vector3[3];

        /// <summary>
        /// Face world vertex array.
        /// </summary>
        private readonly Vector3[] m_WorldVertices = new Vector3[3];

        /// <summary>
        /// Flag for preventing repeated vertex localization.
        /// </summary>
        private bool m_IsLocalized;

        private const int k_Mask = Layers.DetectableMask;
        
        
        /// <summary>
        /// Initializes the face.
        /// </summary>
        /// <param name="a">World vertex</param>
        /// <param name="b">World vertex</param>
        /// <param name="c">World vertex</param>
        private MeshFace Initialize(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 d = b - a;
            Vector3 e = c - a;
            Vector3 n = Vector3.Cross(d, e).normalized;

            IsValid = GetDistance(a, n) 
                + GetDistance(b, n) 
                + GetDistance(c, n) <= k_MaxError;

            if (IsValid)
            {
                Vector3 f = c - b;
                float dm = d.magnitude;
                float em = e.magnitude;
                float fm = f.magnitude;
                float s = (dm + em + fm) * 0.5f;
                Area = Mathf.Sqrt(s * (s - dm) * (s - em) * (s - fm));

                Vector3 offset = n * k_RenderOffset;
                a += offset;
                b += offset;
                c += offset;

                Normals[0] = n;
                Normals[1] = n;
                Normals[2] = n;

                m_WorldVertices[0] = a;
                m_WorldVertices[1] = b;
                m_WorldVertices[2] = c; 
            }
            
            return this;
        }

        /// <inheritdoc/>
        public void Recycle()
        {
            if (IsValid)
            {
                Array.Clear(Vertices, 0, 3);
                Array.Clear(Normals, 0, 3);
                Array.Clear(m_WorldVertices, 0, 3);

                m_IsLocalized = false;
                IsValid = false;
                Area = 0;
            }

            s_Pool.ReturnItem(this);
        }

        /// <summary>
        /// Localizes the vertices for chunk meshes.
        /// </summary>
        /// <param name="matrix">Chunk's world-to-local matrix</param>
        public void Localize(Matrix4x4 matrix)
        {
            if (m_IsLocalized) return;

            for (int i = 0; i < 3; i++)
            {
                Vertices[i] = matrix.MultiplyPoint3x4(m_WorldVertices[i]);
            }
            m_IsLocalized = true;
        }

        /// <summary>
        /// Returns localized vertices, used for demo (highlights).
        /// </summary>
        /// <param name="matrix">World-to-local matrix</param>
        /// <param name="vertices">Result vertex array</param>
        /// <param name="offset">Additional placement offset</param>
        public void GetLocalized(Matrix4x4 matrix, Vector3[] vertices, float offset = 0.01f)
        {
            Vector3 o = offset * Normals[0];
            for (int i = 0; i < 3; i++)
            {
                vertices[i] = matrix.MultiplyPoint3x4(m_WorldVertices[i] + o);
            }
        }

        /// <summary>
        /// Returns the unsigned distance between face and underlying
        /// collider at the specified vertex, measured along the face's
        /// normal.
        /// </summary>
        /// <param name="vertex">Vertex</param>
        /// <param name="normal">Normal</param>
        /// <returns>Unsigned distance between 0 and 1</returns>
        private static float GetDistance(Vector3 vertex, Vector3 normal)
        {
            return Physics.Raycast(vertex + normal, -normal, out RaycastHit hit,
                2, k_Mask)
                ? Mathf.Abs(1 - hit.distance)
                : 1;
        }

        /// <summary>
        /// Gizmo-draws the face.
        /// </summary>
        public void Draw()
        {
            Gizmos.color = Color.yellow * 0.75f;
            Gizmos.DrawLine(m_WorldVertices[0], m_WorldVertices[1]);
            Gizmos.DrawLine(m_WorldVertices[1], m_WorldVertices[2]);
            Gizmos.DrawLine(m_WorldVertices[2], m_WorldVertices[0]);
        }
    }
}