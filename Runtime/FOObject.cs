using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FishNet.FloatingOrigin
{
    public class FOObject : NetworkBehaviour, IRealTransform
    {
        private bool busy = false; //If true we are waiting for the scene we are going to be moved into to load so don't try rebuilding us
        internal Vector3Int gridPosition;
        internal int sceneHandle;
        [SerializeField] protected bool autoRegister = true;
        protected FOManager manager;
        public Vector3d overrideRealPosition = Vector3d.zero;
        void Awake()
        {
            manager = FOManager.instance;
        }

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);
            if (InstanceFinder.NetworkManager != null)
                RequestSync(InstanceFinder.ClientManager.Connection);
        }
        void Start()
        {
            manager.RegisterFOObject(this);
            manager.SceneChanged += OnRebase;
            transform.position = manager.RealToUnity(Mathd.toVector3d(transform.position), gameObject.scene);
        }
        private void OnDestroy()
        {
            manager.SceneChanged -= OnRebase;
            manager.UnregisterFOObject(this);
        }
        public Vector3 unityPosition
        {
            get => transform.position;
            set => transform.position = value;
        }
        public Vector3d realPosition
        {
            get => manager.UnityToReal(unityPosition, IsServer ? groupOffset : manager.localObserver.groupOffset);
            set => transform.position = manager.RealToUnity(value, IsServer ? groupOffset : manager.localObserver.groupOffset);
        }
        public Vector3 groupOffset => FOManager.instance.GetOffset(gameObject.scene);
        public virtual void OnRebase(Scene scene)
        {
            // if (IsServer)
            // {
            //     SyncPosition(null, unityPosition);
            // }
        }
        [ServerRpc(RequireOwnership = false)]
        public void RequestSync(NetworkConnection connection)
        {
            SyncPosition(connection, unityPosition);
        }
        [Server]
        [TargetRpc]
        public void SyncPosition(NetworkConnection connection, Vector3 position)
        {
            if (!IsOwner)
                transform.position = position;
        }
        public virtual void OnMoveToNewScene(Scene scene)
        {
            // if(IsServer)
            // {
            //     SyncPosition(null,unityPosition);
            // }
        }
        public bool setBusy(bool busy) => this.busy = busy;
        public bool isBusy() => busy ? true : sceneHandle != gameObject.scene.handle;
    }
}