using UnityEngine;

namespace DroneProject
{
    /// <summary>
    /// Utility class used with ML-Agents.
    /// </summary>
    public static class MLUtil
    {
        /// <summary>
        /// Sigmoid normalization for vector observations.
        /// Logistic function with output shifted by -0.5.
        /// See https://www.desmos.com/calculator/ - paste formula
        /// y\ =\ \frac{2}{1+\exp\left(-1\cdot x\cdot c\right)}-1
        /// Graph params: x = observable, y = normalized, c = coefficient (curvature).
        /// </summary>
        /// <param name="value">float value to normalize</param>
        /// <param name="coefficient">Coefficient applied to curve</param>
        /// <returns>Normalized float between -1 and +1</returns>
        public static float Sigmoid(float value, float coefficient = 1)
        {
            return 2 / (1 + Mathf.Exp(-value * coefficient)) - 1;
        }
        
        /// <summary>
        /// Inverse of Sigmoid(value, coef), shifted logit.
        /// See https://www.desmos.com/calculator/ - paste formula
        /// y=\frac{\ln\left(\frac{\left(x+1\right)}{1-x}\right)}{c}
        /// Graph params: x = normalized, y = result, c = coefficient (curvature).
        /// </summary>
        /// <param name="value">Normalized float value</param>
        /// <param name="coefficient">Coefficient applied to curve</param>
        /// <returns>Resulting float value</returns>
        public static float InvSigmoid(float value, float coefficient = 1)
        {
            return Mathf.Log((value + 1) / (1 - value)) / coefficient;
        }

        
        /// <summary>
        /// Sigmoid normalization for vector observations.
        /// </summary>
        /// <param name="vector">Vector3 to normalize</param>
        /// <param name="coefficient">Coefficient applied to curve</param>
        /// <returns>Normalized Vector3, components between -1 and +1</returns>
        public static Vector3 Sigmoid(Vector3 vector, float coefficient = 1)
        {
            vector.x = Sigmoid(vector.x, coefficient);
            vector.y = Sigmoid(vector.y, coefficient);
            vector.z = Sigmoid(vector.z, coefficient);
            return vector;
        }
        
        /// <summary>
        /// Inverse of Sigmoid(vector, coefficient).
        /// </summary>
        /// <param name="vector">Normalized Vector3</param>
        /// <param name="coefficient">Factor applied to curve</param>
        /// <returns>Resulting Vector3></returns>
        public static Vector3 InvSigmoid(Vector3 vector, float coefficient = 1)
        {
            vector.x = InvSigmoid(vector.x, coefficient);
            vector.y = InvSigmoid(vector.y, coefficient);
            vector.z = InvSigmoid(vector.z, coefficient);
            return vector;
        }


        /// <summary>
        /// Calculates a normalized reward falling off from 1 as error increases.
        /// See https://www.desmos.com/calculator/ - paste formula
        /// y\ =\ \min\left(1,\frac{-2}{1+\exp\left(-x\cdot c\right)}+2\right)
        /// Graph params: x = error >= 0, y = reward, c = coefficient > 0.
        /// </summary>
        /// <param name="error">Error</param>
        /// <param name="coefficient">Coefficient > 0</param>
        /// <returns>Positive reward</returns>
        public static float Reward(float error, float coefficient = 1)
        {
            return Mathf.Min(1, -2 / (1 + Mathf.Exp(-error * coefficient)) + 2);
        }
        
        /// <summary>
        /// Calculates a weighted reward falling off from 1 as error increases.
        /// Asymptotes towards 1 - weight value. Use as a factor when combining
        /// multiple reward components, where a component reward for error = 0 is 1,
        /// and for error = infinity is 1 - weight.
        /// See https://www.desmos.com/calculator/ - paste formula
        /// y\ =\ 1-w\ +w\cdot\min\left(1,\frac{-2}{1+\exp\left(-x\cdot c\right)}+2\right)
        /// Graph params: x = error >= 0, y = reward, w = weight 0/+1, c = coefficient > 0.
        /// </summary>
        /// <param name="error">Error</param>
        /// <param name="weight">Weight</param>
        /// <param name="coefficient">Coefficient > 0</param>
        /// <returns></returns>
        public static float WeightedReward(float error, float weight = 1, float coefficient = 1)
        {
            return 1 - weight + weight * Reward(error, coefficient);
        }
    }
}   