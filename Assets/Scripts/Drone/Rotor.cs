using UnityEngine;

namespace DroneProject
{
    /// <summary>
    /// A single drone rotor.
    /// </summary>
    public class Rotor : MonoBehaviour
    {
        /// <summary>
        /// Rotor's world position at which thrust is applied.
        /// </summary>
        public Vector3 WorldPosition => transform.position;
        /// <summary>
        /// Rotor's world axis along which thrust is applied.
        /// </summary>
        public Vector3 WorldThrustAxis => transform.up;
        /// <summary>
        /// Rotor's local axis along which torque is applied.
        /// </summary>
        public Vector3 LocalTorqueAxis { get; private set; }
        /// <summary>
        /// Animated rotor blade.
        /// </summary>
        public Transform Blade { get; private set; }

        /// <summary>
        /// Initializes the rotor, invoked by <see cref="Drone"/>.
        /// </summary>
        public void Initialize()
        {
            LocalTorqueAxis = transform.parent.InverseTransformVector(-transform.up);
            Blade = transform.GetChild(0);
        }
    }
}