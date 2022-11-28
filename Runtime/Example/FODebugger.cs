using FishNet.Object;
using UnityEngine;

namespace FishNet.FloatingOrigin.Debugging
{
    public class FODebugger : NetworkBehaviour
    {
        private FOObserver observer;
        void Awake() => observer = GetComponent<FOObserver>();
        void OnDrawGizmos()
        {
            if (!Application.isPlaying || !ClientManager.Started || (!IsServer && !IsOwner))
                return;

            Debug.DrawLine(FOManager.instance.RemoteToLocal(Vector3.zero, observer.group.offset, FOManager.instance.localObserver.group.offset), FOManager.instance.RealToUnity(Vector3d.zero, observer.group.offset), Color.red);
            Debug.DrawLine(FOManager.instance.RemoteToLocal(Vector3.zero, observer.group.offset, FOManager.instance.localObserver.group.offset), FOManager.instance.RemoteToLocal(transform.position, observer.group.offset, FOManager.instance.localObserver.group.offset), Color.blue);
        }
    }
}