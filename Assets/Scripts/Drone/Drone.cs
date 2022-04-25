using UnityEngine;
using System;

namespace DroneProject
{
    /// <summary>
    /// Basic drone physics, controllable via agent actions.
    /// </summary>
    public class Drone : MonoBehaviour
    {
        /// <summary>
        /// Invoked when tipping over.
        /// </summary>
        public event Action TipOverEvent;
        
        /// <summary>
        /// Invoked when colliding (enter and stay).
        /// </summary>
        public event Action<Collision> CollisionEvent;
        
        /// <summary>
        /// Invoked after timeout WHILE colliding.
        /// </summary>
        public event Action CollisionTimeoutEvent;

        /// <summary>
        /// Whether the drone is currently colliding.
        /// </summary>
        public bool IsColliding { get; private set; }
        
        /// <summary>
        /// Whether the drone is ready for action.
        /// </summary>
        public bool IsReady => !m_ResetFlag;

        /// <summary>
        /// Drone's world position.
        /// </summary>
        public Vector3 WorldPosition => transform.TransformPoint(m_CenterOfMass);

        /// <summary>
        /// Drone's world velocity.
        /// </summary>
        public Vector3 WorldVelocity => m_Rigidbody.velocity;

        /// <summary>
        /// Drone's local velocity.
        /// </summary>
        public Vector3 LocalVelocity => LocalizeVector(m_Rigidbody.velocity);

        /// <summary>
        /// Drone's world angular velocity.
        /// </summary>
        public Vector3 WorldAngularVelocity => m_Rigidbody.angularVelocity;

        /// <summary>
        /// Drone's local angular velocity.
        /// </summary>
        public Vector3 LocalAngularVelocity => LocalizeVector(m_Rigidbody.angularVelocity);

        /// <summary>
        /// Drone's inclination, measured in axes y-component values.
        /// </summary>
        public Vector3 Inclination => new Vector3(transform.right.y, transform.up.y, transform.forward.y);

        /// <summary>
        /// Drone's world forward axis on XZ-plane.
        /// </summary>
        public Vector3 WorldForwardXZ => Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        /// <summary>
        /// Drone's world rotation around Y-axis.
        /// </summary>
        public Quaternion RotationY => Quaternion.Euler(0, transform.eulerAngles.y, 0);
        
        // Timeout in secs for continuous collision.
        private const float k_Timeout = 2;
        // Up-axis y.
        private const float k_TipOverThresh = -0.5f;

        [Space]
        // Multipliers.
        [SerializeField, Tooltip("Action multiplier")]
        private float m_ThrustFactor = 25;

        [SerializeField, Tooltip("Action multiplier")]
        private float m_TorqueFactor = 5;

        [SerializeField, Tooltip("Action multiplier")]
        private float m_AnimSpeedFactor = 4000;

        [SerializeField, Tooltip("Whether to animate the rotors")]
        private bool m_Animate = true;
        
        [SerializeField] 
        private Rotor[] m_Rotors;

        private readonly float[] m_RotorTurnDirections = {1, 1, -1, -1};
        private readonly float[] m_AnimationSpeeds = new float[4];
        
        private float m_DefTilt;
        private Vector3 m_CenterOfMass;
        private Rigidbody m_Rigidbody;
        private int m_CollisionEnterCount;
        private bool m_ResetFlag;
        
        
        /// <summary>
        /// Initializes the drone.
        /// </summary>
        public void Initialize()
        {
            m_DefTilt = transform.localEulerAngles.x;
            
            for (int i = 0; i < 4; i++)
            {
                m_Rotors[i].Initialize();
                m_CenterOfMass += transform.InverseTransformPoint(m_Rotors[i].WorldPosition);
            }

            m_CenterOfMass *= 0.25f;
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Rigidbody.centerOfMass = m_CenterOfMass;
        }

        /// <summary>
        /// Resets rigidbody, places drone at specified position.
        /// </summary>
        /// <param name="position">Drone position</param>
        /// <param name="randomRotation">Whether to apply random rotation
        /// around world y-axis</param>
        public void ResetTo(Vector3 position, bool randomRotation = false)
        {
            ManagedReset();
            
            m_Rigidbody.position = position;
            m_Rigidbody.rotation = Quaternion.Euler(m_DefTilt, 
                randomRotation ? UnityEngine.Random.value * 360 : 0, 0);
        }
        
        /// <summary>
        /// Resets rigidbody, places drone at specified position and rotation.
        /// </summary>
        public void ResetTo(Pose pose)
        {
            ManagedReset();
            
            m_Rigidbody.position = pose.position;
            m_Rigidbody.rotation = pose.rotation;
        }

        private void ManagedReset()
        {
            CancelInvoke();
            
            m_ResetFlag = true;
            IsColliding = false;
            m_CollisionEnterCount = 0;
    
            m_Rigidbody.velocity = Vector3.zero;
            m_Rigidbody.angularVelocity = Vector3.zero;
        }

        /// <summary>
        /// Applies agent actions as forces to drone's rigidbody.
        /// </summary>
        /// <param name="actions">Normalized action values [4]</param>
        public void ApplyActions(float[] actions)
        {
            // TODO Rigidbody issue. Need to skip one frame after reset.
            if (m_ResetFlag)
            {
                m_ResetFlag = false;
                return;
            }

            // TODO Train with individual rotor axes, see WorldThrustAxis and LocalTorqueAxis.
            // For now, we'll use a simplified setup, all rotors are aligned with drone's y-axis.
            Vector3 thrustAxis = transform.up; // world
            Vector3 torqueAxis = Vector3.down; // local
            
            for (int i = 0; i < 4; i++)
            {
                // -1/+1 => 0/+1
                actions[i] = (actions[i] + 1) * 0.5f; 

                // Thrust per rotor.
                
                // thrustAxis = m_Rotors[i].WorldThrustAxis;
                m_Rigidbody.AddForceAtPosition(
                    actions[i] * m_ThrustFactor * thrustAxis, 
                    m_Rotors[i].WorldPosition);

                
                // Counter torque per rotor.
                
                // TODO We apply torques at the drone's center of mass, which is
                // not accurate. They should be applied at each rotor's position.
                
                // Flip direction for 2 of 4 rotors, torques need to cancel each other out.
                actions[i] *= m_RotorTurnDirections[i];
                
                // torqueAxis = m_Rotors[i].LocalTorqueAxis;
                m_Rigidbody.AddRelativeTorque(actions[i] * m_TorqueFactor * torqueAxis);
                
                // Buffer value for animation.
                m_AnimationSpeeds[i] = actions[i];
            }
            
            if (transform.up.y < k_TipOverThresh)
            {
                TipOverEvent?.Invoke();
            }
        }

        /// <summary>
        /// Transforms world vector to drone's local frame.
        /// </summary>
        /// <param name="vector">World vector</param>
        /// <returns>Local vector</returns>
        public Vector3 LocalizeVector(Vector3 vector)
        {
            return transform.InverseTransformVector(vector);
        }
        
        private void Update()
        {
            if (m_Animate)
            {
                float maxSpeed = Time.deltaTime * m_AnimSpeedFactor;
                for (int i = 0; i < 4; i++)
                {
                    m_Rotors[i].Blade.Rotate(0, m_AnimationSpeeds[i] * maxSpeed, 0);
                }
            }
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            m_CollisionEnterCount++;
            UpdateCollisionState();
            CollisionEvent?.Invoke(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            CollisionEvent?.Invoke(collision);
        }

        private void OnCollisionExit(Collision collision)
        {
            m_CollisionEnterCount--;
            UpdateCollisionState();
        }

        private void UpdateCollisionState()
        {
            if (m_CollisionEnterCount < 0)
            {
                Debug.LogWarning("Collision count < 0");
                m_CollisionEnterCount = 0;
            }
            
            bool tmp = IsColliding;
            IsColliding = m_CollisionEnterCount > 0;

            if (!IsColliding)
            {
                CancelInvoke();
            }
            else if (!tmp)
            {
                Invoke(nameof(NotifyTimeout), k_Timeout);
            }
        }

        private void NotifyTimeout()
        {
            CollisionTimeoutEvent?.Invoke();
        }
    }
}