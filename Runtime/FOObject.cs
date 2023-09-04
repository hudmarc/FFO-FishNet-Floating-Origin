using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

namespace FishNet.FloatingOrigin
{
    [RequireComponent(typeof(NetworkObject))]
    public class FOObject : NetworkBehaviour
    {
        internal NetworkObject networking;
        private void Awake()
        {
            networking = GetComponent<NetworkObject>();
        }
        public Vector3d realPosition => FOManager.instance.UnityToReal(transform.position, gameObject.scene);

        internal virtual void Initialize() => FOManager.instance?.RegisterObject(this);
        internal virtual void Deinitialize() => FOManager.instance?.UnregisterObject(this);
        public override void OnOwnershipServer(NetworkConnection prevOwner)
        {
            base.OnOwnershipServer(prevOwner);
            Initialize();
        }
        private void OnDestroy() => Deinitialize();
    }
}