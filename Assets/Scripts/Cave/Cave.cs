using UnityEngine;

namespace DroneProject
{
    /// <summary>
    /// Provides cave path related info.
    /// The path is an array of consecutive positions, with a spacing of 
    /// 0.25 meters and roughly equidistant to the surrounding cave walls.
    /// </summary>
    public class Cave : MonoBehaviour
    {
        [SerializeField] 
        private Path m_Path;
        
        private Vector3 m_DefPos;
        private int m_Length;
        private int m_Index;
        
        /// <summary>
        /// Initializes the cave.
        /// </summary>
        public void Initialize()
        {
            m_DefPos = transform.position;
            m_Length = m_Path.Positions.Length;
        }

        /// <summary>
        /// Returns a random spawn position and rotation for the drone.
        /// </summary>
        /// <returns>Pose</returns>
        public Pose GetRandomSpawnPose()
        {
            m_Index = Random.Range(0, m_Length);
            return new Pose(GetPosition(m_Index), Quaternion.LookRotation(GetDirection(m_Index)));
        }

        /// <summary>
        /// Returns an interpolated ray at the drone's nearest path position.
        /// </summary>
        /// <param name="worldDronePos">Drone's world position</param>
        /// <returns></returns>
        public Ray GetRayAt(Vector3 worldDronePos)
        {
            UpdateIndex(worldDronePos);

            Vector3 p0 = GetPosition(m_Index);
            Vector3 p1 = GetPosition(m_Index + 1);
            Vector3 d0 = GetDirection(m_Index);
            Vector3 d1 = GetDirection(m_Index + 1);

            Ray ray = new Ray(p0, d0);
            float t = ray.Length(worldDronePos) / (p1 - p0).magnitude;
            ray.origin = Vector3.Lerp(p0, p1, t);
            ray.direction = Vector3.Lerp(d0, d1, t);
            
            return ray;
        }

        // We're assuming "forward" motion along the path (ascending indices)
        // and that the drone is never fast enough to skip an index.
        // TODO might want to do a binary search.
        private void UpdateIndex(Vector3 pos)
        {
            if ((pos - GetPosition(m_Index + 2)).sqrMagnitude < (pos - GetPosition(m_Index)).sqrMagnitude)
            {
                m_Index = WrapIndex(m_Index + 1);
            }
        }

        private Vector3 GetPosition(int i)
        {
            return m_DefPos + m_Path.Positions[WrapIndex(i)];
        }

        private Vector3 GetDirection(int i)
        {
            return (m_Path.Positions[WrapIndex(i + 1)] - m_Path.Positions[WrapIndex(i)]).normalized;
        }

        private int WrapIndex(int i)
        {
            return (i + m_Length) % m_Length;
        }

        private void OnDrawGizmos()
        {
            for (int i = 0, n = m_Path.Positions.Length - 1; i < n; i++)
            {
                Gizmos.DrawLine(m_Path.Positions[i], m_Path.Positions[i + 1]);
            }
        }
        
        private static Vector3 Interpolate(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            Vector3 a = 2 * p1;
            Vector3 b = p2 - p0;
            Vector3 c = 2 * p0 - 5 * p1 + 4 * p2 - p3;
            Vector3 d = -p0 + 3 * p1 - 3 * p2 + p3;
            return 0.5f * (a + b * t + c * t * t + d * t * t * t);
        }
    }

    public static class RayExtensions
    {
        /// <summary>
        /// Returns the point's shortest distance to the ray.
        /// </summary>
        /// <param name="ray">Ray</param>
        /// <param name="point">Point</param>
        /// <returns>Distance</returns>
        public static float Distance(this Ray ray, Vector3 point)
        {
            return Vector3.Cross(ray.direction, point - ray.origin).magnitude;
        }

        /// <summary>
        /// Returns the ray's length up to where the point is projected onto the ray.
        /// </summary>
        /// <param name="ray">Ray</param>
        /// <param name="point">Point</param>
        /// <returns>Length</returns>
        public static float Length(this Ray ray, Vector3 point)
        {
            return Vector3.Project(point - ray.origin, ray.direction).magnitude;
        }
    }
}