using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DroneProject
{
    /// <summary>
    /// Gameobject containing partial surface mesh.
    /// </summary>
    public class MeshChunk : MonoBehaviour
    {
        /// <summary>
        /// Combined surface area.
        /// </summary>
        public float Area { get; private set; }

        /// <summary>
        /// Associated node.
        /// </summary>
        private OctreeNode m_ChunkNode;

        /// <summary>
        /// Surface cubes in chunk.
        /// </summary>
        private List<SurfaceCube> m_Cubes;

        /// <summary>
        /// World-to-local matrix for localizing vertices.
        /// </summary>
        private Matrix4x4 m_Matrix;

        private Mesh m_Mesh;
        private MeshFilter m_MeshFilter;
        private List<Vector3> m_Vertices;
        private List<Vector3> m_Normals;
        private List<int> m_Triangles;

        // Demo.
        private bool m_IsDemo;
        private MeshFilter m_Hologram;


        /// <summary>
        /// Initializes the chunk.
        /// </summary>
        /// <param name="node">Associated node</param>
        public void Initialize(OctreeNode node)
        {
            m_ChunkNode = node;
            name = "Chunk#" + node.ID;
            
            Transform t = transform;
            t.position = node.Bounds.min;
            m_Matrix = t.worldToLocalMatrix;

            if (m_Mesh != null) return;
            
            m_Mesh = new Mesh();
            m_Mesh.MarkDynamic();
            m_MeshFilter = GetComponent<MeshFilter>();
            m_Cubes = new List<SurfaceCube>();
            m_Vertices = new List<Vector3>();
            m_Normals = new List<Vector3>();
            m_Triangles = new List<int>();

            // Demo chunk contains a nested surface mesh.
            m_IsDemo = t.childCount > 0;
            
            if (m_IsDemo)
            {
                m_Hologram = t.GetChild(0)
                    .GetComponent<MeshFilter>();
            }
        }

        /// <summary>
        /// Clears the chunk.
        /// </summary>
        public void Clear()
        {
            m_ChunkNode = null;
            m_Vertices.Clear();
            m_Normals.Clear();
            m_Cubes.Clear();
            m_Mesh.Clear();
            Area = 0;
        }
        
        /// <summary>
        /// Updates the mesh, calculates surface area.
        /// </summary>
        public void UpdateMesh()
        {
            m_Vertices.Clear();
            m_Normals.Clear();
            m_Cubes.Clear();
            
            m_ChunkNode.CollectSurfaceCubes(m_Cubes);

            Area = 0;
            foreach (SurfaceCube cube in m_Cubes)
            {
                foreach (MeshFace face in cube.Faces)
                {
                    Area += face.Area;
                    face.Localize(m_Matrix);
                    m_Vertices.AddRange(face.Vertices);
                    m_Normals.AddRange(face.Normals);
                }
            }

            m_Mesh.Clear();
            m_Mesh.SetVertices(m_Vertices);
            m_Mesh.SetNormals(m_Normals);
            m_Mesh.SetTriangles(UpdateTriangles(), 0);
            m_Mesh.RecalculateBounds();
            m_MeshFilter.sharedMesh = m_Mesh;

            if (m_IsDemo)
            {
                m_Hologram.sharedMesh = m_Mesh;
            }
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