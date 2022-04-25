using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

namespace DroneProject
{
    /// <summary>
    /// Surface cubes are owned by octree leaf <see cref="OctreeNode">nodes</see>.
    /// A cube is created where a raycast hit occurs. It stores the hit(s)
    /// and the corresponding tangent plane. Cubes are fed to the <see cref="MarchingCubes">
    /// marching cubes algorithm</see>, generating the associated mesh faces.
    /// </summary>
    public class SurfaceCube : IPoolable
    {
        /// <summary>
        /// Returns the octant index for the specified bounds and position.
        /// Bitwise order: z 4 / y 2 / x 1.
        /// </summary>
        /// <param name="bounds">Cube bounds</param>
        /// <param name="pos">Position</param>
        /// <returns>Octant index</returns>
        public static byte GetOctant(Bounds bounds, Vector3 pos)
        {
            Debug.Assert(bounds.Contains(pos), 
                $"Position {pos} out of bounds {bounds}");
            Vector3 d = pos - bounds.center;
            return (byte)((d.x < 0 ? 0 : 1) + (d.y < 0 ? 0 : 2) + (d.z < 0 ? 0 : 4));
        }
        
        /// <summary>
        /// Returns the octant bounds for the specified bounds and octant.
        /// </summary>
        /// <param name="bounds">Cube bounds</param>
        /// <param name="octant">Octant index</param>
        /// <returns></returns>
        public static Bounds GetOctantBounds(Bounds bounds, byte octant)
        {
            bounds.size *= 0.5f;
            bounds.center += Vector3.Scale(bounds.extents, s_Offsets[octant]);
            return bounds;
        }
        
        /// <summary>
        /// Offset multipliers for octants.
        /// Bitwise order: z 4 / y 2 / x 1.
        /// </summary>
        private static readonly Vector3Int[] s_Offsets = new Vector3Int[8]
        {
            new Vector3Int(-1, -1, -1), new Vector3Int(1, -1, -1),
            new Vector3Int(-1,  1, -1), new Vector3Int(1,  1, -1),
            new Vector3Int(-1, -1,  1), new Vector3Int(1, -1,  1),
            new Vector3Int(-1,  1,  1), new Vector3Int(1,  1,  1)
        };
        
        
        /// <summary>
        /// Pool, we reuse surface cubes.
        /// </summary>
        private static readonly Pool<SurfaceCube> s_Pool = Pool<SurfaceCube>.Instance;

        /// <summary>
        /// Factory.
        /// </summary>
        /// <param name="bounds">Cube world bounds</param>
        /// <param name="surface">Intersecting surface plane</param>
        /// <returns>New or pooled surface cube</returns>
        public static SurfaceCube Pooled(Bounds bounds, Plane surface)
        {
            return s_Pool.RetrieveItem().Initialize(bounds, surface);
        }
        
        /// <summary>
        /// Maximum allowed angle for testing whether raycast hit 
        /// normal is aligned with cube's intersecting surface normal.
        /// </summary>
        private const float k_CoplanarAngleTolerance = 3f;
        /// <summary>
        /// Maximum allowed distance for testing whether raycast hit 
        /// point is coplanar with cube's intersecting surface plane.
        /// </summary>
        private const float k_CoplanarDistanceTolerance = 0.1f;
        
        
        /// <summary>
        /// Cube vertex with normalized signed distance from surface plane.
        /// </summary>
        public struct Vertex
        {
            public Vector3 Position;
            public float SignedDistance;
        }
        /// <summary>
        /// Cube vertices, bitwise order: z 4 / y 2 / x 1.
        /// </summary>
        public Vertex[] Vertices { get; } = new Vertex[8];
        /// <summary>
        /// Mesh faces in cube, 5 max.
        /// </summary>
        public List<MeshFace> Faces { get; } = new List<MeshFace>(5);
        /// <summary>
        /// Raycast hits, can store multiple hits only if they are coplanar.
        /// </summary>
        public List<RaycastHit> Hits { get; } = new List<RaycastHit>(4);
        /// <summary>
        /// Intersecting surface plane.
        /// </summary>
        public Plane Surface { get; private set; }
        /// <summary>
        /// Cube world bounds.
        /// </summary>
        public Bounds Bounds { get; private set; }

        /// <summary>
        /// Initializes the surface cube.
        /// </summary>
        /// <param name="bounds">Cube world bounds</param>
        /// <param name="surface">Intersecting surface plane</param>
        /// <returns>The surface cube instance</returns>
        private SurfaceCube Initialize(Bounds bounds, Plane surface)
        {
            Bounds = bounds;
            Surface = surface;
            float length = Bounds.size.x;
            
            for (int i = 0; i < 8; i++)
            {
                Vector3 pos = Bounds.center + Vector3.Scale(
                    Bounds.extents, s_Offsets[i]);
                
                Vertices[i] = new Vertex
                {
                    Position = pos,
                    SignedDistance = surface.GetDistanceToPoint(pos) / length
                };
            }
            
            return this;
        }
     
        /// <inheritdoc/>
        public void Recycle()
        {
            foreach (MeshFace face in Faces)
            {
                face.Recycle();
            }
            Faces.Clear();
            Hits.Clear();
            Array.Clear(Vertices, 0, 8);
            
            s_Pool.ReturnItem(this);
        }
        
        /// <summary>
        /// Whether the specified raycast hit is coplanar with the cube's intersecting surface.
        /// </summary>
        /// <param name="hit"></param>
        /// <returns></returns>
        public bool IsCoplanar(RaycastHit hit)
        {
            return !(Vector3.Angle(Surface.normal, hit.normal) > k_CoplanarAngleTolerance 
                  || Mathf.Abs(Surface.GetDistanceToPoint(hit.point)) > k_CoplanarDistanceTolerance);
        }

        /// <summary>
        /// The surface area of this cube.
        /// </summary>
        /// <value>Area</value>
        public float Area
        {
            get { return Faces.Sum(face => face.Area); }
        }

        public override string ToString()
        {
            return $" - {Faces.Count} faces";
        }
    }
}