using UnityEngine;

namespace DroneProject
{
    /// <summary>
    /// Takes care of enabling and disabling cave mesh chunks around the drone's current position.
    /// </summary>
    public class CaveChunks : MonoBehaviour
    {
        [SerializeField, Tooltip("Number of visible chunks in front of and behind current chunk")] 
        private int m_Padding = 2;
        private int m_Count;
        private int m_Angle;
        private GameObject[] m_Chunks;
        
        /// <summary>
        /// Initializes the chunk manager.
        /// </summary>
        public void Initialize()
        {
            Transform t = transform;
            m_Count = t.childCount;
            Debug.Assert(m_Count == 360);
            m_Chunks = new GameObject[m_Count];

            for (int i = 0; i < m_Count; i++)
            {
                m_Chunks[i] = t.GetChild(i).gameObject;
                m_Chunks[i].SetActive(false);
            }
        }

        /// <summary>
        /// Disables all chunks.
        /// </summary>
        public void ManagedReset()
        {
            for (int i = 0; i < m_Count; i++)
            {
                m_Chunks[i].SetActive(false);
            }

            m_Angle = -999;
        }

        /// <summary>
        /// Shows / hides chunks around drone's position.
        /// </summary>
        /// <param name="localDronePos">Drone's local position</param>
        public void ManagedUpdate(Vector3 localDronePos)
        {
            // Assuming overall cave center at local 0/0/0.
            Vector3 normal = Vector3.ProjectOnPlane(localDronePos, Vector3.up).normalized;
            // Assuming one chunk per angle 000 - 359.
            int angle = Mathf.Min(Mathf.FloorToInt(
                    Vector3.SignedAngle(Vector3.forward, normal, Vector3.up)) + 180,
                359);
            
            if (angle != m_Angle)
            {
                m_Angle = angle;
                int min = m_Angle - m_Padding;
                int max = m_Angle + m_Padding;
                
                for (int i = min; i <= max; i++)
                {
                    m_Chunks[WrapIndex(i)].SetActive(true);
                }
                m_Chunks[WrapIndex(min - 1)].SetActive(false);
                m_Chunks[WrapIndex(max + 1)].SetActive(false);
            }
        }

        private int WrapIndex(int index)
        {
            return (index + m_Count) % m_Count;
        }
    }
}