using System.Collections.Generic;
using UnityEngine;

namespace DroneProject
{
    /// <summary>
    /// Manages <see cref="SurfaceHighlight"/> objects for highlighting surface cubes.
    /// </summary>
    public class SurfaceHighlightManager : MonoBehaviour
    {
        [SerializeField] 
        private SurfaceHighlight m_Prefab;
        private Stack<SurfaceHighlight> m_Pool;
        private const int k_Mask = Layers.HighlightsMask;
        
        private void Awake()
        {
            m_Pool = new Stack<SurfaceHighlight>();
            FindObjectOfType<SurfaceReconstruction>().RaycastEvent += OnSurfaceRaycast;
        }

        private void OnSurfaceRaycast(SurfaceRaycastInfo raycastInfo, SurfaceCube cube)
        {
            if (!raycastInfo.HasValidHit) return;

            if (Physics.Raycast(raycastInfo.Origin, raycastInfo.Direction,
                    out RaycastHit hit, raycastInfo.Length, k_Mask))
            {
                hit.collider.GetComponent<SurfaceHighlight>().CancelAnimation();
            }
            
            SurfaceHighlight highlight = m_Pool.Count > 0
                ? m_Pool.Pop()
                : Instantiate(m_Prefab, cube.Bounds.min, Quaternion.identity, transform)
                    .GetComponent<SurfaceHighlight>();
            
            highlight.Initialize(raycastInfo, cube);
            highlight.AnimationDoneEvent += OnAnimationDone;
            highlight.gameObject.SetActive(true);
        }

        private void OnAnimationDone(SurfaceHighlight highlight)
        {
            highlight.AnimationDoneEvent -= OnAnimationDone;
            highlight.gameObject.SetActive(false);
            m_Pool.Push(highlight);
        }
    }
}