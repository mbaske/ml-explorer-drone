using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System;

namespace DroneProject
{
    /// <summary>
    /// Controls drone physics based on specified target values.
    /// </summary>
    public class RotorControlAgent : Agent
    {
        public Drone Drone { get; private set; }
        public int DecisionInterval { get; set; }

        // Buffer for action interpolation.
        private float[] m_PrevActions;
        private const int k_NumActions = 4;

        // Local target velocity (model was trained for -10/+10 range).
        private Vector3 m_LocalVelocity;
        // Normalized local target look angle on XZ-plane.
        protected float LocalLookAngle;

        /// <summary>
        /// Sets drone targets as world vectors.
        /// </summary>
        /// <param name="worldVelocity">World target velocity</param>
        /// <param name="worldLookDirection">World target look direction</param>
        public void SetWorldTargets(Vector3 worldVelocity, Vector3 worldLookDirection)
        {
            SetLocalTargets(Drone.LocalizeVector(worldVelocity), 
                Drone.LocalizeVector(worldLookDirection));
        }
        
        /// <summary>
        /// Sets drone targets as local vectors.
        /// </summary>
        /// <param name="localVelocity">Local target velocity</param>
        /// <param name="localLookDirection">Local target look direction</param>
        public void SetLocalTargets(Vector3 localVelocity, Vector3 localLookDirection)
        {
            localLookDirection = Vector3.ProjectOnPlane(localLookDirection, Vector3.up);  
            SetLocalTargets(localVelocity, Vector3.SignedAngle(
                Vector3.forward,  localLookDirection, Vector3.up) / 180f);
        }

        /// <summary>
        /// Sets drone targets as local vector and angle.
        /// </summary>
        /// <param name="localVelocity">Local target velocity</param>
        /// <param name="localLookAngle">Normalized target look angle</param>
        public void SetLocalTargets(Vector3 localVelocity, float localLookAngle)
        {
            m_LocalVelocity = localVelocity;
            LocalLookAngle = localLookAngle;
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            // TODO Code execution order unclear when
            // Initialize() is invoked via PilotAgent.
            if (Drone != null) return;

            m_PrevActions = new float[k_NumActions];
            Drone = GetComponentInChildren<Drone>();
            Drone.Initialize();
        }

        /// <inheritdoc />
        public override void OnEpisodeBegin()
        {
            Array.Clear(m_PrevActions, 0, k_NumActions);
        }

        /// <inheritdoc />
        public override void CollectObservations(VectorSensor sensor)
        {
            // TBD Sigmoid coefficients.
            sensor.AddObservation(LocalLookAngle);
            sensor.AddObservation(MLUtil.Sigmoid(m_LocalVelocity, 0.5f));
            sensor.AddObservation(MLUtil.Sigmoid(Drone.LocalVelocity, 0.5f));
            sensor.AddObservation(MLUtil.Sigmoid(Drone.LocalAngularVelocity));
            sensor.AddObservation(Drone.Inclination);
        }

        /// <inheritdoc />
        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            var actions = actionBuffers.ContinuousActions.Array;
            int step = StepCount % DecisionInterval;

            if (step == 0)
            {
                // Last cycle step: buffer and apply actions as is.
                Array.Copy(actions, m_PrevActions, k_NumActions);
            }
            else
            {
                // Interpolate: previous cycle's actions -> current actions.
                float t = step / (float) DecisionInterval;
                
                for (int i = 0; i < k_NumActions; i++)
                {
                    actions[i] = Mathf.Lerp(m_PrevActions[i], actions[i], t);
                }
            }

            Drone.ApplyActions(actions);
        }

        /// <inheritdoc />
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var actions = actionsOut.ContinuousActions;
            
            // Hover, TBD.
            float thrust = Drone.WorldVelocity.y * -0.25f;
            Vector3 incl = Drone.Inclination;
            float pitch = incl.z * -0.025f;
            float roll = incl.x * 0.025f;
            actions[0] = thrust + roll + pitch;
            actions[1] = thrust - roll - pitch;
            actions[2] = thrust - roll + pitch;
            actions[3] = thrust + roll - pitch;
        }
    }
}