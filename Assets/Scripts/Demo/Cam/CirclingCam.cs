using UnityEngine;

namespace DroneProject
{
    /// <summary>
    /// Circles the drone.
    /// </summary>
    public class CirclingCam : MonoBehaviour
    {
        [SerializeField, Tooltip("Cam look target")]
        protected Drone m_Drone;

        [SerializeField, Tooltip("x: circle radius, y: max offset along y-axis")]
        private Vector2 m_Offset;

        [SerializeField, Tooltip("x: circling speed, y: motion speed along y-axis")]
        private Vector2 m_Speed;

        [SerializeField, Tooltip("Smooth damp time")]
        protected float m_Damping = 0.1f;

        protected Vector3 m_LookPos;
        private Vector3 m_LookVelocity;

        private void FixedUpdate()
        {
            m_LookPos = Vector3.SmoothDamp(m_LookPos, m_Drone.WorldPosition,
                ref m_LookVelocity, m_Damping);
            
            transform.position = GetCamPos();
            transform.LookAt(m_LookPos);
        }

        protected virtual Vector3 GetCamPos()
        {
            float t = Time.time;
            return m_LookPos + new Vector3(
                m_Offset.x * Mathf.Cos(t * m_Speed.x),
                m_Offset.y * Mathf.Cos(t * m_Speed.y),
                m_Offset.x * Mathf.Sin(t * m_Speed.x));
        }
    }
}