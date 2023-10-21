using FishNet.Object;
using UnityEngine;

namespace FishNet.FloatingOrigin
{
    [RequireComponent(typeof(FOView))]
    public class FOObserverDebugger : NetworkBehaviour
    {
        private FOView observer;
        void OnEnable() => observer = GetComponent<FOView>();
#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!enabled)
                return;
            if (!Application.isPlaying || observer == null || !ClientManager.Started || (!IsServer && !IsOwner) || FOManager.instance == null)
                return;

            Debug.DrawLine(Vector3.zero, FOManager.instance.RealToUnity(Vector3d.zero, observer.gameObject.scene), Color.red);
            Debug.DrawLine(Vector3.zero, FOManager.instance.RealToUnity(observer.realPosition, observer.gameObject.scene), Color.blue);

            // if (Functions.RealToUnity(Vector3d.zero, observer.gameObject.scene) != FOManager.instance.transform.position)
            // {
            //     Debug.LogError($"DESYNCHRONIZED BY {Vector3.Distance(FOManager.instance.RealToUnity(Vector3d.zero, observer.gameObject.scene), FOManager.instance.transform.position)} UNITS");
            //     Debug.Break();
            // }
        }
#endif
    }
}