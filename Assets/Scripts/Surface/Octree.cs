using UnityEngine;

namespace DroneProject
{
    /// <summary>
    /// Manages raycast hit insertion for the octree nodes.
    /// </summary>
    public class Octree
    {
        /// <summary>
        /// Root node length in meters.
        /// </summary>
        public const int RootNodeLength = 128;
        /// <summary>
        /// Maximum recursion depth / minimum surface cube size.
        /// </summary>
        public const int MaxDepth = 15;
        /// <summary>
        /// Mesh chunk depth / size, 8m for depth 4 with 128m root.
        /// </summary>
        public const int MeshChunkDepth = 4;
        /// <summary>
        /// Minimum depth / maximum size for surface cubes,
        /// 7 => 1m with root = 128m.
        /// </summary>
        public const int MinSurfaceDepth = 7;
        
        /// <summary>
        /// Root node world bounds.
        /// </summary>
        private readonly Bounds m_Bounds;
        /// <summary>
        /// Octree root node.
        /// </summary>
        private OctreeNode m_RootNode;
        
        /// <summary>
        /// Creates a new octree with the specified bounds.
        /// </summary>
        /// <param name="center">Root node center</param>
        public Octree(Vector3 center)
        {
            m_Bounds = new Bounds(center, RootNodeLength * Vector3.one);
        }

        /// <summary>
        /// Clears all octree contents.
        /// </summary>
        public void Clear()
        {
            m_RootNode?.Recycle();
            m_RootNode = OctreeNode.Pooled(m_Bounds);
        }

        /// <summary>
        /// Whether a raycast hit can be added to the tree.
        /// </summary>
        /// <param name="hit">Raycast hit</param>
        /// <param name="node">Node the hit can be added to</param>
        /// <returns>true if hit can be added</returns>
        public bool CanAddRaycastHit(RaycastHit hit, out OctreeNode node)
        {
            Plane surface = new Plane(hit.normal, hit.point); // tangent plane
            node = GetLeafNodeAt(hit.point);
            
            while (node.Depth <= MaxDepth)
            {
                bool isValid;
                bool hasSurface = node.TryGetSurfaceCube(out SurfaceCube surfaceCube);
                
                if (hasSurface)
                {
                    // Is the new hit coplanar?
                    isValid = surfaceCube.IsCoplanar(hit);
                }
                else
                {
                    // No previous hits: copy raycast hit surface to new 
                    // cube and check whether it can contain valid faces.
                    surfaceCube = node.CreateSurfaceCube(surface);
                    isValid = MarchingCubes.HasFaces(surfaceCube);
                }
               
                if (isValid)
                {
                    return true;
                }

                // Can't add point at the current depth...
                
                if (hasSurface)
                {
                    // Reassign current node contents to new children.
                    SplitNode(node, surfaceCube);
                }
                // Surface cube was either split or has just been
                // created above. Either way, it cannot contain 
                // raycast hits (any longer) and must be removed.
                node.RemoveSurfaceCube();
                
                if (!node.TryGetChildAt(hit.point, out OctreeNode child))
                {
                    // Add the required child in case
                    // it wasn't created during split.
                    child = node.AddChildAt(hit.point);
                }
                
                // Repeat for child...
                node = child;
            }
            
            // Max depth exceeded, undo changes.
            node.RemoveEmptyBranch();

            return false;
        }

        /// <summary>
        /// Returns the leaf node at the specified position.
        /// </summary>
        /// <param name="pos">World position</param>
        /// <returns>Leaf node</returns>
        private OctreeNode GetLeafNodeAt(Vector3 pos)
        {
            // Node may or may not have a surface.
            OctreeNode node = m_RootNode.GetDeepestNodeAt(pos);

            if (node.Depth < MinSurfaceDepth)
            {
                // Create new branch.
                // Leaf node is empty (no surface).
                node = node.CreateBranchTo(pos, MinSurfaceDepth);
            }
            else if (node.HasChildren())
            {
                // Has children, but not at pos, add extra child.
                // Leaf node is empty (no surface).
                node = node.AddChildAt(pos);
            }
            // else: deepest is already a leaf.

            return node;
        }

        /// <summary>
        /// Splits the node contents into octants. Child nodes
        /// are only created for octants containing raycast hits.
        /// </summary>
        /// <param name="node">The node to split</param>
        /// <param name="surfaceCube">The node's surface cube</param>
        private static void SplitNode(OctreeNode node, SurfaceCube surfaceCube)
        {
            foreach (RaycastHit hit in surfaceCube.Hits)
            {
                byte octant = node.GetOctant(hit.point);
                if (!node.TryGetChild(octant, out OctreeNode child))
                {
                    // Need to add new child at hit pos.
                    child = node.AddChild(octant);
                    // Copy parent plane to new child surface.
                    // If there are multiple hits stored in the
                    // parent, we can assume they are coplanar.
                    child.CreateSurfaceCube(surfaceCube.Surface);
                }
                // Copy raycast hit to child surface.
                child.GetSurfaceCube().Hits.Add(hit);
            }
        }

        /// <summary>
        /// Gizmo-draws the tree.
        /// </summary>
        public void Draw()
        {
            m_RootNode.Draw();
        }
    }
}