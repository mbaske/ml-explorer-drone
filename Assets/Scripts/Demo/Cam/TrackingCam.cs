using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace DroneProject
{
    /// <summary>
    /// Cam is placed at random positions near target
    /// and smoothly follows its movement.
    /// </summary>
    public class TrackingCam : MonoBehaviour
    {
        private DepthOfField m_DepthOfField;
        private Camera m_Cam;

        [SerializeField, Tooltip("Cam look target")]
        private Transform m_Target;

        [SerializeField, Tooltip("Set Profile for updating depth of field")]
        private PostProcessProfile m_Profile;

        [SerializeField, Tooltip("Max. distance to target")]
        private float m_MaxDistance = 5;
        private float m_MedDistance;
        private float m_MinDistance;
        
        [SerializeField, Tooltip("Place cam at a distance to objects")]
        private float m_PlacemenOffset = 0.1f;

        [SerializeField, Tooltip("Follow target damping")]
        private float m_Damping = 1;
        
        private Vector3 m_LookPos;
        private Vector3 m_LookPosVlc;
        
        private const int k_Mask = Layers.DetectableMask;

        private void Awake()
        {
            // TBD placement range.
            m_MedDistance = m_MaxDistance * 0.75f;
            m_MinDistance = 0.25f;

            m_Profile.TryGetSettings(out m_DepthOfField);
            m_Cam = GetComponent<Camera>();
        }

        private void Update()
        {
            Transform t = transform;
            Vector3 camPos =  t.position;
            Vector3 targetPos = m_Target.position;
            
            Vector3 delta = targetPos - camPos;
            float distance = delta.magnitude;
            
            m_Cam.fieldOfView = 75 - distance * 3; // TBD
            m_DepthOfField.focusDistance.value = distance;
            
            // Need new cam pos when target pos is out of range or occluded.
            if (distance > m_MaxDistance ||
                Physics.Raycast(camPos, delta, distance, k_Mask))
            {
                camPos = GetNewCamPos(targetPos);
                t.position = camPos;
                m_LookPos = targetPos;
            }
            else
            {
                m_LookPos = Vector3.SmoothDamp(m_LookPos, targetPos,
                    ref m_LookPosVlc, m_Damping);
            }


            t.LookAt(m_LookPos);
        }

        private Vector3 GetNewCamPos(Vector3 pos)
        {
            int count = 0;
            const int maxRetries = 64;
            Vector3 back = -m_Target.forward;
            RaycastHit hit;

            while (!Physics.Raycast(pos, RandomRotation() * back, 
                       out hit, m_MedDistance, k_Mask) || hit.distance < m_MinDistance)
            {
                if (++count == maxRetries)
                {
                    m_MaxDistance++;
                    m_MedDistance = m_MaxDistance * 0.75f;
                    Debug.LogWarning("Increased max cam distance to " + m_MaxDistance);
                    return pos;
                }
            }

            Vector3 offset = (pos - hit.point).normalized * m_PlacemenOffset;
            return hit.point + offset;
        }

        private static Quaternion RandomRotation()
        {
            return Quaternion.Euler(RandomAngle(), RandomAngle(), 0);
        }

        private static float RandomAngle(float min = 15, float max = 120)
        {
            return Random.Range(min, max) * Mathf.Sign(Random.value - 0.5f);
        }
    }
}