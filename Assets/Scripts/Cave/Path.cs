using UnityEngine;

namespace DroneProject
{
    /// <summary>
    /// Cave path is an array of consecutive positions, with a spacing of 
    /// 0.25 meters and roughly equidistant to the surrounding cave walls.
    /// </summary>
    [CreateAssetMenu(fileName = "Path", menuName = "ScriptableObjects/Path", order = 1)]
    public class Path : ScriptableObject
    {
        public Vector3[] Positions;
    }
}