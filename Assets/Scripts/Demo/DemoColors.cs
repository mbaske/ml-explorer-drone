using UnityEngine;

namespace DroneProject
{
    /// <summary>
    /// Colors for displaying raycast info results.
    /// </summary>
    [CreateAssetMenu(fileName = "DemoColors", menuName = "ScriptableObjects/DemoColors", order = 1)]
    public class DemoColors : ScriptableObject
    {
        public Color None;
        public Color Invalid;
        public Color Coplanar;
        public Color Continuous;
        public Color Isolated;
    }
}