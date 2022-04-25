using Unity.MLAgents.Sensors;
using System.Collections.Generic;
using UnityEngine;

namespace DroneProject
{
    /// <summary>
    /// Executes batches of radial raycasts from the transforms position and adds the
    /// normalized measured distances to a <see cref="VectorSensor"/> instance.
    /// Rays are constrained by
    /// - Maximum distance
    /// - Field of view angle
    /// - Cylinder surface around the transform's z-axis
    /// </summary>
    public class BatchedRayDetection : MonoBehaviour
    {
        [SerializeField, Range(1, 10), Tooltip("Number of raycast batches, should match decision interval")] 
        private int m_NumBatches = 3;
        [SerializeField, Range(1, 500), Tooltip("Total number of rays")] 
        private int m_NumRays = 150;
        [SerializeField, Range(1f, 20), Tooltip("Maximum ray length")] 
        private float m_MaxDistance = 10;
        [SerializeField, Range(10, 360), Tooltip("Field of view angle")] 
        private float m_FOV = 210;
        [SerializeField, Range(0.1f, 5), Tooltip("Radius around transform z-axis")] 
        private float m_Radius = 1.5f;
        [SerializeField, Range(0, 1), Tooltip("Shifts ray directions towards transform forward")] 
        private float m_Focus = 1;
       
        private static readonly float s_Phi = (Mathf.Sqrt(5) + 1) / 2;
        private const float k_TwoPI = Mathf.PI * 2f;

        // Ray directions and lengths.
        [SerializeField, HideInInspector] 
        private List<Vector4> m_Vectors = new List<Vector4>();
        // Normalized measured distances.
        private readonly List<float> m_Distances = new List<float>();
        private int m_BatchIndex;
        
        private const int k_Mask = Layers.DetectableMask;
        

        private void OnValidate()
        {
            CalcSerializedVectors();
        }
        
        /// <summary>
        /// Clears measured distances, resets batch index.
        /// </summary>
        public void Clear()
        {
            m_Distances.Clear();
            m_BatchIndex = 0;
        }

        /// <summary>
        /// Adds measured distances to the vector sensor.
        /// </summary>
        /// <param name="sensor">Vector sensor</param>
        public void AddDistances(VectorSensor sensor)
        {
            for (int i = m_Distances.Count; i < m_NumRays; i++)
            {
                m_Distances.Add(1); // pad
            }
            sensor.AddObservation(m_Distances);
            Clear();
        }

        /// <summary>
        /// Executes a raycast batch.
        /// </summary>
        public void BatchRaycast()
        {
            Transform t = transform;
            Vector3 pos = t.position;
            Quaternion rot = t.rotation;

            int batchSize = m_NumRays / m_NumBatches;
            int n0 = m_BatchIndex * batchSize;
            int n1 = m_BatchIndex == m_NumBatches - 1 ? m_NumRays : n0 + batchSize;
            m_BatchIndex++;
            
            for (int i = n0; i < n1; i++)
            {
                Vector4 v = m_Vectors[i];
                
                if (Physics.Raycast(pos, rot * v,
                        out RaycastHit hit, v.w, k_Mask))
                {
                    m_Distances.Add(hit.distance / v.w * 2 - 1);
                }
                else
                {
                    m_Distances.Add(1);
                }
            }
        }

        private void CalcSerializedVectors()
        {
            // Ray directions are a subset of all fibonacci sphere points:
            // num rays / num points total = FOV area / sphere area.
            float halfFOV = m_FOV * 0.5f;
            float halfSphereArea = k_TwoPI * m_MaxDistance * m_MaxDistance;
            float fovArea = halfSphereArea * (1 - Mathf.Cos(halfFOV * Mathf.Deg2Rad));
            int numPointsTotal = Mathf.RoundToInt(m_NumRays * (2 * halfSphereArea / fovArea));

            m_Vectors.Clear();
        
            for (int i = 0; i < numPointsTotal; i++)
            {
                Vector3 v3 = GetFibonacciSpherePoint(i, numPointsTotal);
                float angle = Vector3.Angle(Vector3.forward, v3);
                
                if (angle > halfFOV)
                {
                    // Ignore point.
                    continue;
                }

                if (m_Focus > 0)
                {
                    float t = (1 - angle / halfFOV) * m_Focus;
                    v3 = Vector3.Slerp(v3, Vector3.forward, t);
                    // Must re-calc angle.
                    angle = Vector3.Angle(Vector3.forward, v3);
                }
                
                Vector4 v4 = v3;
                v4.w = m_MaxDistance;
                // Constrain to radius, cylinder around z-axis.
                float sin = Mathf.Sin(angle * Mathf.Deg2Rad) * m_MaxDistance;
                if (sin > m_Radius)
                {
                    v4.w *= m_Radius / sin;
                }
                m_Vectors.Add(v4);
            }
            
            Debug.Assert(m_NumBatches < m_NumRays, "Less batches than rays");
            Debug.Assert(m_Vectors.Count == m_NumRays, "Vector count mismatch " + m_Vectors.Count);
        }

        private static Vector3 GetFibonacciSpherePoint(int i, int n)
        {
            float d = i / s_Phi;
            float z = 1 - (2 * i + 1f) / n;
            float phi = k_TwoPI * (d - Mathf.Floor(d));
            float sinTheta = Mathf.Sin(Mathf.Acos(z));
        
            return new Vector3(Mathf.Cos(phi) * sinTheta, Mathf.Sin(phi) * sinTheta, z);
        }

        private void OnDrawGizmosSelected()
        {
            Transform t = transform;
            Vector3 pos = t.position;
            Quaternion rot = t.rotation;
 
            foreach (Vector4 v in m_Vectors)
            {
                if (Physics.Raycast(pos, rot * v,
                        out RaycastHit hit, v.w, k_Mask))
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(pos, hit.point);
                }
                else
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawRay(pos, rot * v * v.w);
                }
            }
        }
    }
}