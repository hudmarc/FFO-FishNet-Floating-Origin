using FishNet.Object;
using UnityEngine;

namespace FishNet.FloatingOrigin
{
    [RequireComponent(typeof(FOObserver))]
    public class FOObserverDebugger : NetworkBehaviour
    {
        private FOObserver observer;
        void OnEnable() => observer = GetComponent<FOObserver>();
#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!enabled)
                return;
            if (!Application.isPlaying || observer == null || !ClientManager.Started || (!IsServer && !IsOwner) || FOManager.instance == null || FOManager.instance.localObserver == null)
                return;

            Debug.DrawLine(FOManager.instance.RemoteToLocal(Vector3.zero, observer.groupOffset, FOManager.instance.localObserver.groupOffset), FOManager.instance.RealToUnity(Vector3d.zero, observer.groupOffset), Color.red);
            Debug.DrawLine(FOManager.instance.RemoteToLocal(Vector3.zero, observer.groupOffset, FOManager.instance.localObserver.groupOffset), FOManager.instance.RemoteToLocal(transform.position, observer.groupOffset, FOManager.instance.localObserver.groupOffset), Color.blue);

        }
        private void OnGUI()
        {
            if (!enabled)
                return;
            FOManager.instance.DrawDebug();
        }
#endif
    }
}