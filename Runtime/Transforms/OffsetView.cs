using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FloatingOffset.Runtime
{
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
