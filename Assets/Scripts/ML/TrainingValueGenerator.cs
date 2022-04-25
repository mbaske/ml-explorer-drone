using UnityEngine;

namespace DroneProject
{
    /// <summary>
    /// Random value generator for <see cref="RotorControlTrainer"/>.
    /// </summary>
    public class TrainingValueGenerator : MonoBehaviour
    {
        /// <summary>
        /// Whether the drone should move.
        /// </summary>
        public bool IsMoving => !m_Stop;
        /// <summary>
        /// Target world velocity.
        /// </summary>
        public Vector3 WorldVelocity => m_Point.Velocity;
        /// <summary>
        /// Target world look direction towards center.
        /// </summary>
        public Vector3 WorldLookDirection => Vector3.ProjectOnPlane(
            -m_Point.Position, Vector3.up).normalized;

        private struct Point
        {
            public Vector3 Position;
            public Vector3 Velocity;
        }
        private Point m_Point;

        private struct Attractor
        {
            public Vector3 Position;
            public float Strength;
        }
        private Attractor[] m_Attractors;

        [SerializeField, Min(1), Tooltip("Number of attractors")] 
        private int m_NumAttractors;
        [SerializeField, Min(10), Tooltip("Attractor spacing radius")] 
        private float m_Radius;
        [SerializeField, Min(100), Tooltip("Attractor randomization interval")] 
        private int m_RndInterval;
        [SerializeField, Min(0.01f), Tooltip("Attractor min strength")] 
        private float m_MinStrength;
        [SerializeField, Min(0.01f), Tooltip("Attractor max strength")] 
        private float m_MaxStrength;

        [Space, SerializeField, Min(1), Tooltip("Max drone velocity")] 
        private float m_MaxVelocity;
        private float m_MaxVelocitySqr;
        [SerializeField, Range(0f, 0.1f), Tooltip("Friction strength")] 
        private float m_Friction;
        private float m_InvFriction;
        [SerializeField, Range(0f, 1f), Tooltip("Drone stop probability")] 
        private float m_StopProbability;

        private bool m_Stop;
        private int m_StepCount;

        /// <summary>
        /// Initializes the generator.
        /// </summary>
        public void Initialize()
        {
            m_Attractors = new Attractor[m_NumAttractors];
            m_MaxVelocitySqr = m_MaxVelocity * m_MaxVelocity;
            m_InvFriction = 1 - m_Friction;
        }

        private void OnValidate()
        {
            m_MaxVelocitySqr = m_MaxVelocity * m_MaxVelocity;
            m_InvFriction = 1 - m_Friction;
        }

        /// <summary>
        /// Resets the generator.
        /// </summary>
        public void ManagedReset()
        {
            m_StepCount = 0;
            m_Point.Position = Vector3.zero;
            m_Point.Velocity = Vector3.zero;
        }

        /// <summary>
        /// Updates the generator by one time step.
        /// </summary>
        /// <param name="deltaTime">Step delta time</param>
        public void ManagedUpdate(float deltaTime)
        {
            if (m_StepCount++ % m_RndInterval == 0)
            {
                Randomize();
            }

            if (m_Stop)
            {
                m_Point.Velocity *= 0.75f; // TBD
            }
            else
            {
                for (int i = 0; i < m_NumAttractors; i++)
                {
                    Vector3 delta = m_Attractors[i].Position - m_Point.Position;
                    float sqrMag = delta.sqrMagnitude;
                    m_Point.Velocity += m_Attractors[i].Strength / sqrMag * delta;

                    if (sqrMag < 1)
                    {
                        // Avoid spiraling into attractor.
                        Randomize();
                        break;
                    }
                }

                if (m_Point.Velocity.sqrMagnitude > m_MaxVelocitySqr)
                {
                    m_Point.Velocity = m_Point.Velocity.normalized * m_MaxVelocity;
                }
                else
                {
                    m_Point.Velocity *= m_InvFriction;
                }
            }

            m_Point.Position += m_Point.Velocity * deltaTime;
        }

        private void Randomize()
        {
            for (int i = 0; i < m_NumAttractors; i++)
            {
                m_Attractors[i].Position = Random.insideUnitSphere * m_Radius;
                m_Attractors[i].Strength = Random.Range(m_MinStrength, m_MaxStrength);
            }

            m_Stop = Random.value < m_StopProbability;
        }

        private void OnDrawGizmosSelected()
        {
            if (m_Attractors != null)
            {
                Vector3 t = transform.position;
                Vector3 p = t + m_Point.Position;

                // Gizmos.color = Color.gray;
                // Gizmos.DrawWireSphere(t, m_Radius);

                Gizmos.color = Color.blue;
                for (int i = 0; i < m_NumAttractors; i++)
                {
                    Gizmos.DrawSphere(t + m_Attractors[i].Position, 0.1f);
                }

                Gizmos.color = Color.green;
                Gizmos.DrawRay(p, WorldVelocity);
                Gizmos.color = Color.gray;
                Gizmos.DrawRay(p, WorldLookDirection);
                Gizmos.DrawSphere(p, 0.1f);
            }
        }
    }
}