using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// When leaving the rebase area the OS will be rebased to the centroid of all OffsetViews in the scene. <br/> Tracks the real position (relative to real zero) and the real velocity (in absolute space, relative to real zero velocity) of itself.
    /// </summary>
    public class OffsetView : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
        
        }

        public void SetRealPositionApproximate(Vector3d position)
        {
            OffsetScene scene = FOServiceLocator.registry.GetScened<OffsetScene>(gameObject.scene);
            transform.position = Mathd.RealToUnity(position, scene.GetOffset());
        }
    }
}
