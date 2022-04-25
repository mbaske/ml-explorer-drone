using System.Collections.Generic;
using System;
using UnityEngine;

namespace DroneProject
{
    /// <summary>
    /// Creates chunked meshes from raycast hits.
    /// </summary>
    public class SurfaceReconstruction : MonoBehaviour
    {
        public event Action<SurfaceRaycastInfo, SurfaceCube> RaycastEvent;
        
        /// <summary>
        /// Combined surface area of all chunks.
        /// </summary>
        private float m_TotalSurfaceArea;

        [SerializeField] 
        private MeshChunk m_ChunkPrefab;

        /// <summary>
        /// Chunks by node ID.
        /// </summary>
        private readonly Dictionary<ulong, MeshChunk> m_ChunkMap
            = new Dictionary<ulong, MeshChunk>();
        
        /// <summary>
        /// Inactive / unused chunks.
        /// </summary>
        private readonly Stack<MeshChunk> m_ChunkPool 
            = new Stack<MeshChunk>();
        
        /// <summary>
        /// Chunks that need to be re-meshed after raycast
        /// hit insertions.
        /// </summary>
        private readonly HashSet<OctreeNode> m_UpdatedChunkNodes 
            = new HashSet<OctreeNode>();

        private Octree m_Octree;
        private const int k_Mask = Layers.DetectableMask;
        
        /// <summary>
        /// Initializes surface reconstruction.
        /// </summary>
        public void Initialize()
        {
            m_Octree = new Octree(transform.position);
        }

        /// <summary>
        /// Resets the surface reconstruction, clears contents.
        /// </summary>
        public void ManagedReset()
        {
            foreach (MeshChunk chunk in m_ChunkMap.Values)
            {
                m_ChunkPool.Push(chunk);
                chunk.gameObject.SetActive(false);
                chunk.Clear();
            }

            m_Octree.Clear();
            m_ChunkMap.Clear();
            m_UpdatedChunkNodes.Clear();
            m_TotalSurfaceArea = 0;
        }

        /// <summary>
        /// Executes a raycast.
        /// </summary>
        /// <param name="raycastInfo">Surface raycast info</param>
        public void CastRay(SurfaceRaycastInfo raycastInfo)
        {
            SurfaceCube cube = null;
            raycastInfo.HasHit = Physics.Raycast(raycastInfo.Origin, raycastInfo.Direction, 
                out RaycastHit hit, raycastInfo.Length, k_Mask);
            
            if (raycastInfo.HasHit)
            {
                raycastInfo.Length = hit.distance;
                raycastInfo.HasValidHit = TryAddRaycastHit(hit, out OctreeNode node, out cube);
                
                if (raycastInfo.HasValidHit)
                {
                    raycastInfo.HitIsNew = cube.Hits.Count == 1;
                    raycastInfo.HitIsContinuous = node.IsContinuousSurface();
                }
            }
            
            RaycastEvent?.Invoke(raycastInfo, cube);
        }

        /// <summary>
        /// Tries to add a raycast hit point to the surface.
        /// </summary>
        /// <param name="hit">Raycast hit</param>
        /// <param name="node">Resulting node if hit was added</param>
        /// <param name="cube">Resulting cube if hit was added</param>
        /// <returns>Whether hit was added and the target node is continuous</returns>
        private bool TryAddRaycastHit(RaycastHit hit, out OctreeNode node, out SurfaceCube cube)
        {
            if (m_Octree.CanAddRaycastHit(hit, out node))
            {
                m_UpdatedChunkNodes.Add(node.GetAncestorAt(Octree.MeshChunkDepth));
                cube = node.GetSurfaceCube();
                cube.Hits.Add(hit);

                return true;
            }
            
            cube = null;
            return false;
        }

        /// <summary>
        /// Updates meshes of all chunks affected by raycast hits.
        /// </summary>
        /// <returns>Growth of total surface area</returns>
        public float UpdateMeshes()
        {
            if (m_UpdatedChunkNodes.Count == 0)
            {
                return 0;
            }

            float total = m_TotalSurfaceArea;
            foreach (OctreeNode node in m_UpdatedChunkNodes)
            {
                MeshChunk chunk = GetChunk(node);
                total -= chunk.Area;
                chunk.UpdateMesh();
                total += chunk.Area;
            }
            
            m_UpdatedChunkNodes.Clear();

            float growth = Mathf.Max(0, total - m_TotalSurfaceArea);
            m_TotalSurfaceArea = total;

            return growth;
        }

        /// <summary>
        /// Returns the chunk associated with the specified node.
        /// Instantiates a new chunk if required.
        /// </summary>
        /// <param name="node">Node</param>
        /// <returns>Chunk</returns>
        private MeshChunk GetChunk(OctreeNode node)
        {
            if (m_ChunkMap.TryGetValue(node.ID, out MeshChunk chunk)) return chunk;
            
            chunk = m_ChunkPool.Count > 0 
                ? m_ChunkPool.Pop() 
                : Instantiate(m_ChunkPrefab, transform).GetComponent<MeshChunk>();
            
            chunk.Initialize(node);
            chunk.gameObject.SetActive(true);
            m_ChunkMap.Add(node.ID, chunk);

            return chunk;
        }

        private void OnDrawGizmos()
        {
            m_Octree?.Draw();
        }
    }
}