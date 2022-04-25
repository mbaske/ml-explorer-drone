using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DroneProject
{
    /// <summary>
    /// Raycast info.
    /// </summary>
    public struct SurfaceRaycastInfo
    {
        /// <summary>
        /// Ray origin.
        /// </summary>
        public Vector3 Origin;
        /// <summary>
        /// Ray direction.
        /// </summary>
        public Vector3 Direction;
        /// <summary>
        /// Ray length.
        /// </summary>
        public float Length;
        
        // RESULT
        
        /// <summary>
        /// Whether the ray hit a detectable surface.
        /// </summary>
        public bool HasHit;
        /// <summary>
        /// Whether the resulting point could be added to the octree.
        /// </summary>
        public bool HasValidHit;
        /// <summary>
        /// Whether the resulting point is NOT coplanar with existing ones.
        /// </summary>
        public bool HitIsNew; 
        /// <summary>
        /// Whether the resulting point is on a continuous surface.
        /// </summary>
        public bool HitIsContinuous;
    }
}