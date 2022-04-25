using System.Collections;
using UnityEngine;

namespace DroneProject
{
    /// <summary>
    /// Alternates between circling and following.
    /// </summary>
    public class FollowCam : CirclingCam
    {
        [SerializeField, Tooltip("Smooth damp time (follow)")] 
        private float m_DampingFollow;
        private float m_CurrentDamping;
        [SerializeField, Tooltip("Duration in seconds")] 
        private float m_FollowDuration = 5;
        [SerializeField, Tooltip("Duration in seconds")] 
        private float m_CircleDuration = 5;

        private Vector3 m_CamPos;
        private Vector3 m_CamVelocity;
        private bool m_Circle;
        private Coroutine m_Coroutine;

        private void Awake()
        {
            m_CurrentDamping = m_DampingFollow;
            m_Coroutine = StartCoroutine(SwitchToCircle());
        }
        
        protected override Vector3 GetCamPos()
        {
            m_CurrentDamping = Mathf.Lerp(m_CurrentDamping, 
                m_Circle ? m_Damping : m_DampingFollow, Time.fixedDeltaTime);
            
            m_CamPos = Vector3.SmoothDamp(m_CamPos, 
                m_Circle ? base.GetCamPos() : m_LookPos, 
                ref m_CamVelocity, m_CurrentDamping);

            // Switch to circling if drone is stuck.
            if (!m_Circle && (m_CamPos - m_LookPos).sqrMagnitude < 0.04f)
            {
                StopCoroutine(m_Coroutine);
                m_Circle = true;
                m_Coroutine = StartCoroutine(SwitchToFollow());
            }

            return m_CamPos;
        }
        
        private IEnumerator SwitchToCircle()
        {
            yield return new WaitForSeconds(m_FollowDuration);
            m_Circle = true;
            m_Coroutine = StartCoroutine(SwitchToFollow());
        }
        
        private IEnumerator SwitchToFollow()
        {
            yield return new WaitForSeconds(m_CircleDuration);
            m_Circle = false;
            m_Coroutine = StartCoroutine(SwitchToCircle());
        }
    }
}