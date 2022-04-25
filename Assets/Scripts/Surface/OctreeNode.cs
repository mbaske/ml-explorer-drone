using System.Collections.Generic;
using System.Text;
using System;
using System.Linq;
using UnityEngine;

namespace DroneProject
{
    /// <summary>
    /// Custom non-generic octree node implementation.
    /// Leaf nodes have <see cref="SurfaceCube">surface cubes</see>.
    /// </summary>
    public class OctreeNode : IPoolable
    {
        /// <summary>
        /// Pool, we reuse octree nodes.
        /// </summary>
        private static readonly Pool<OctreeNode> s_Pool = Pool<OctreeNode>.Instance;

        /// <summary>
        /// Factory.
        /// </summary>
        /// <param name="bounds">Node world bounds</param>
        /// <param name="parent">Parent node</param>
        /// <returns>New or pooled octree node</returns>
        public static OctreeNode Pooled(Bounds bounds, OctreeNode parent = null)
        {
            return s_Pool.RetrieveItem().Initialize(bounds, parent);
        }

        /// <summary>
        /// Node ID (morton code).
        /// </summary>
        public ulong ID { get; private set; } = 1;

        /// <summary>
        /// Node parent.
        /// </summary>
        public OctreeNode Parent { get; private set; }

        /// <summary>
        /// Node world bounds.
        /// </summary>
        public Bounds Bounds { get; private set; }

        /// <summary>
        /// Node depth, root is 0.
        /// </summary>
        public int Depth { get; private set; }

        /// <summary>
        /// Node octant in parent.
        /// </summary>
        public byte Octant { get; private set; }

        /// <summary>
        /// Child nodes, only used nodes are instantiated.
        /// </summary>
        private Dictionary<byte, OctreeNode> m_Children;

        /// <summary>
        /// The <see cref="SurfaceCube">surface cube</see>
        /// associated with this node, if it is a leaf.
        /// </summary>
        private SurfaceCube m_SurfaceCube;

        /// <summary>
        /// Initializes the node.
        /// </summary>
        /// <param name="bounds">Node world bounds</param>
        /// <param name="parent">Node parent</param>
        /// <returns>The octree node instance</returns>
        private OctreeNode Initialize(Bounds bounds, OctreeNode parent = null)
        {
            Bounds = bounds;
            Parent = parent;

            if (parent != null)
            {
                Octant = parent.GetOctant(bounds.center);
                Depth = parent.Depth + 1;
                ID = (parent.ID << 3) + Octant;
            }

            return this;
        }

        /// <inheritdoc/>
        public void Recycle()
        {
            if (HasChildren())
            {
                foreach (OctreeNode child in m_Children.Values)
                {
                    child.Recycle();
                }

                m_Children.Clear();
            }

            if (HasSurfaceCube())
            {
                RemoveSurfaceCube();
            }

            ID = 1;
            Depth = 0;
            Octant = 0;
            Parent = null;
            m_SurfaceCube = null;

            s_Pool.ReturnItem(this);
        }

        /// <summary>
        /// Returns this node's ancestor at the specified depth.
        /// </summary>
        /// <param name="depth">Depth</param>
        /// <returns>Ancestor</returns>
        public OctreeNode GetAncestorAt(int depth)
        {
            Debug.Assert(Depth >= depth, "Invalid depth " + depth);
            return Depth > depth ? Parent.GetAncestorAt(depth) : this;
        }

        /// <summary>
        /// Returns the deepest node at the specified position.
        /// This doesn't necessarily have to be a leaf node --
        /// A node with children can be returned if position is
        /// inside an octant for which no child node exists yet.
        /// </summary>
        /// <param name="pos">World position</param>
        /// <returns>Deepest node</returns>
        public OctreeNode GetDeepestNodeAt(Vector3 pos)
        {
            return TryGetChildAt(pos, out OctreeNode child) ? child.GetDeepestNodeAt(pos) : this;
        }

        /// <summary>
        /// Creates a branch at the specified position,
        /// down to the specified depth.
        /// </summary>
        /// <param name="pos">World position</param>
        /// <param name="depth">Branch depth</param>
        /// <returns>Leaf node at end of the new branch</returns>
        public OctreeNode CreateBranchTo(Vector3 pos, int depth)
        {
            Debug.Assert(depth >= Depth, "Invalid depth " + depth);
            return Depth == depth ? this : AddChildAt(pos).CreateBranchTo(pos, depth);
        }

        /// <summary>
        /// Removes the empty branch on top of this node.
        /// Used for undoing branch creation.
        /// </summary>
        /// <param name="octant">Auto-assigned</param>
        public void RemoveEmptyBranch(byte octant = 0)
        {
            Debug.Assert(!HasSurfaceCube(), "Has surface node " + this);

            if (HasChildren(2) || Depth == 0)
            {
                OctreeNode child = GetChild(octant);
                RemoveChild(child);
                child.Recycle();
            }
            else
            {
                Parent.RemoveEmptyBranch(Octant);
            }
        }

        /// <summary>
        /// Whether this node has (a minimum number of) child nodes.
        /// </summary>
        /// <param name="min">Minimum number of children, defaults to 1</param>
        /// <returns>true if number of children is greater or equal min</returns>
        public bool HasChildren(int min = 1)
        {
            return m_Children != null && m_Children.Count >= min;
        }

        /// <summary>
        /// Adds a child node at the specified octant.
        /// </summary>
        /// <param name="octant">Octant index</param>
        /// <returns>Child node</returns>
        public OctreeNode AddChild(byte octant)
        {
            OctreeNode child = Pooled(SurfaceCube.GetOctantBounds(Bounds, octant), this);
            m_Children ??= new Dictionary<byte, OctreeNode>();
            m_Children.Add(octant, child);
            return child;
        }

        /// <summary>
        /// Returns the child node at the specified octant.
        /// </summary>
        /// <param name="octant">Octant index</param>
        /// <returns>Child node</returns>
        /// <exception cref="ArgumentOutOfRangeException">No child at octant</exception>
        private OctreeNode GetChild(byte octant)
        {
            if (TryGetChild(octant, out OctreeNode child))
            {
                return child;
            }

            throw new ArgumentOutOfRangeException("No child at " + octant);
        }

        /// <summary>
        /// Adds a child node at the specified position.
        /// </summary>
        /// <param name="pos">World position</param>
        /// <returns>Child node</returns>
        public OctreeNode AddChildAt(Vector3 pos)
        {
            return AddChild(GetOctant(pos));
        }

        /// <summary>
        /// Returns the child node at the specified position.
        /// </summary>
        /// <param name="pos">World position</param>
        /// <returns>Child node</returns>
        public OctreeNode GetChildAt(Vector3 pos)
        {
            return GetChild(GetOctant(pos));
        }

        /// <summary>
        /// Tries to get the child node at the specified octant.
        /// </summary>
        /// <param name="octant">Octant index</param>
        /// <param name="child">Child node</param>
        /// <returns>true if child node exists</returns>
        public bool TryGetChild(byte octant, out OctreeNode child)
        {
            child = null;
            return HasChildren() && m_Children.TryGetValue(octant, out child);
        }

        /// <summary>
        /// Tries to get the child node at the specified position.
        /// </summary>
        /// <param name="pos">World position</param>
        /// <param name="child">Child node</param>
        /// <returns>true if child node exists</returns>
        public bool TryGetChildAt(Vector3 pos, out OctreeNode child)
        {
            return TryGetChild(GetOctant(pos), out child);
        }

        /// <summary>
        /// Removes the specified child node.
        /// </summary>
        /// <param name="child">Child to remove</param>
        private void RemoveChild(OctreeNode child)
        {
            m_Children.Remove(child.Octant);
        }

        /// <summary>
        /// Enumerates the child nodes.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<OctreeNode> Children()
        {
            return HasChildren() ? m_Children.Values : Enumerable.Empty<OctreeNode>();
        }

        /// <summary>
        /// Whether this node has an associated <see cref="SurfaceCube">surface cube</see>.
        /// If it does, the node has to be a leaf.
        /// </summary>
        /// <returns>true if surface cube is defined</returns>
        private bool HasSurfaceCube()
        {
            return m_SurfaceCube != null;
        }

        /// <summary>
        /// Creates a surface cube for this node.
        /// </summary>
        /// <param name="surface">Intersecting plane</param>
        /// <returns>New surface cube</returns>
        public SurfaceCube CreateSurfaceCube(Plane surface)
        {
            Debug.Assert(!HasSurfaceCube() && !HasChildren(),
                $"HasSurfaceCube {HasSurfaceCube()} HasChildren {HasChildren()}");
            m_SurfaceCube = SurfaceCube.Pooled(Bounds, surface);
            return m_SurfaceCube;
        }

        /// <summary>
        /// Removes the surface cube.
        /// </summary>
        public void RemoveSurfaceCube()
        {
            Debug.Assert(HasSurfaceCube(), "No surface cube");
            m_SurfaceCube.Recycle();
            m_SurfaceCube = null;
        }

        /// <summary>
        /// Returns the surface cube.
        /// </summary>
        /// <returns></returns>
        public SurfaceCube GetSurfaceCube()
        {
            Debug.Assert(HasSurfaceCube(), "No surface cube");
            return m_SurfaceCube;
        }

        /// <summary>
        /// Tries to get the surface cube.
        /// </summary>
        /// <param name="surfaceCube">Surface cube</param>
        /// <returns>true if surface cube exists</returns>
        public bool TryGetSurfaceCube(out SurfaceCube surfaceCube)
        {
            surfaceCube = m_SurfaceCube;
            return HasSurfaceCube();
        }

        /// <summary>
        /// Collects all surface cubes inside the node bounds
        /// and adds them to the provided list.
        /// </summary>
        /// <param name="list">Found surface cubes</param>
        public void CollectSurfaceCubes(List<SurfaceCube> list)
        {
            foreach (OctreeNode child in Children())
            {
                child.CollectSurfaceCubes(list);
            }

            if (HasSurfaceCube())
            {
                list.Add(m_SurfaceCube);
            }
        }

        /// <summary>
        /// Whether the surface is continuous at this node,
        /// meaning at least one neighboring node has a surface
        /// cube as well.
        /// 
        /// NOTE: we're not checking if neighboring surface planes
        /// actually connect, so theoretically, opposite sides
        /// of a wall would be considered a continuous surface
        /// if they happen to be in neighboring nodes.
        /// </summary>
        /// <param name="pad">Padding for neighbor node inclusion</param>
        /// <returns>true if continuous</returns>
        public bool IsContinuousSurface(float pad = 0.01f)
        {
            Bounds padded = Bounds;
            padded.Expand(pad);

            int count = 0;
            // Enclosing node must be grandparent or above.
            OctreeNode enclosing = Parent.Parent.GetEnclosingNode(padded);
            enclosing.CountSurfaceCubes(padded, ref count);
            // Count includes this node.
            return count > 1;
        }
        
        /// <summary>
        /// Counts the surface cubes inside the specified bounds.
        /// </summary>
        /// <param name="bounds">World bounds</param>
        /// <param name="count">Cube count</param>
        private void CountSurfaceCubes(Bounds bounds, ref int count)
        {
            if (HasSurfaceCube())
            {
                if (Bounds.Intersects(bounds))
                {
                    count++;
                }
            }
            else
            {
                foreach (OctreeNode child in Children())
                {
                    child.CountSurfaceCubes(bounds, ref count);
                }
            }
        }
        
        /// <summary>
        /// Returns the deepest node that encloses the specified bounds.
        /// </summary>
        /// <param name="bounds">World bounds</param>
        /// <returns>Enclosing nodes</returns>
        private OctreeNode GetEnclosingNode(Bounds bounds)
        {
            return Contains(bounds) ? this : Parent.GetEnclosingNode(bounds);
        }

        /// <summary>
        /// Whether this node contains the specified bounds.
        /// </summary>
        /// <param name="bounds">World bounds</param>
        /// <returns>true if bounds are contained</returns>
        private bool Contains(Bounds bounds)
        {
            return bounds.min.GreaterOrEqual(Bounds.min) && 
                   bounds.max.SmallerOrEqual(Bounds.max);
        }
        
        /// <summary>
        /// Returns the octant index at the specified position.
        /// </summary>
        /// <param name="pos">World position</param>
        /// <returns>Octant index</returns>
        public byte GetOctant(Vector3 pos)
        {
            return SurfaceCube.GetOctant(Bounds, pos);
        }

        /// <summary>
        /// Returns a string representation
        /// of this node and its children.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            BuildTreeString(sb);
            return sb.ToString();
        }

        private void BuildTreeString(StringBuilder sb)
        {
            sb.AppendLine();
            sb.Insert(sb.Length, "-   ", Depth);
            sb.Append($"{Depth}/{Octant} {Bounds}");
            
            if (HasSurfaceCube())
            {
                sb.Append(m_SurfaceCube);
            }
            else
            {
                foreach (OctreeNode child in Children())
                {
                    child.BuildTreeString(sb);
                }
            }
        }
        
        /// <summary>
        /// Gizmo-draws this node and its child nodes.
        /// </summary>
        /// <param name="offset">Offset</param>
        public void Draw(float offset = -0.001f)
        {
            Bounds b = Bounds;
            b.Expand(offset);
            Gizmos.color = s_Palette[Depth];
            Gizmos.DrawWireCube(b.center, b.size);

            foreach (OctreeNode child in Children())
            {
                child.Draw();
            }
        }

        /// <summary>
        /// Color palette for Gizmo draw.
        /// </summary>
        private static readonly Color[] s_Palette = CreatePalette();
        
        /// <summary>
        /// Creates a color palette based on node depths.
        /// </summary>
        /// <param name="depths"></param>
        /// <returns></returns>
        private static Color[] CreatePalette(int depths = 20)
        {
            var palette = new Color[depths];
            for (int i = 0; i < depths; i++)
            {
                float t = i / (depths - 1f);
                palette[i] = Color.Lerp(Color.blue, Color.red, t);
                palette[i].a = Mathf.Clamp01(0.8f - Mathf.Sqrt(t)); // TBD
            }
            return palette;
        }
    }

    public static class VectorExtensions
    {
        public static bool SmallerOrEqual (this Vector3 a, Vector3 b)
        {
            return a.x <= b.x && a.y <= b.y && a.z <= b.z;
        }
        
        public static bool GreaterOrEqual (this Vector3 a, Vector3 b)
        {
            return a.x >= b.x && a.y >= b.y && a.z >= b.z;
        }
    }
}