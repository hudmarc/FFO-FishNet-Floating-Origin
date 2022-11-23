using FishNet.Object;
using System.Threading.Tasks;
using FishNet.Connection;
using FishNet.FloatingOrigin.Types;
using UnityEngine;
// using FishNet.Component.Transforming;

namespace FishNet.FloatingOrigin
{

    public class FOObserver : NetworkBehaviour
    {

        public FOGroup group;
        public int groupHandle;//Used for adjacency checks
        internal bool busy = false;//If true we are waiting for the scene we are going to be moved into to load so don't try rebuilding us
        internal Vector3Int lastGrid;
        [SerializeField] private bool autoRegister = true;
        private FOManager manager;
        void Awake() => manager = FOManager.instance;

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);
            if (autoRegister)
                InitializeOnClient();
        }
        public override void OnOwnershipServer(NetworkConnection prevOwner)
        {
            base.OnOwnershipServer(prevOwner);
            if (autoRegister)
                InitializeOnServer();
        }
        private void InitializeOnClient()
        {
            if (IsOwner)
                manager.localObserver = this;
            if (!IsServer)
                manager.RegisterObserver(this);
        }
        [Server]
        private void InitializeOnServer() => manager.RegisterObserver(this);
        public Vector3 unityPosition
        {
            get => transform.position;
            set => transform.position = value;
        }
        public Vector3d realPosition
        {
            get => manager.UnityToReal(unityPosition, IsServer ? group.offset : manager.localObserver.group.offset);
            set => transform.position = manager.RealToUnity(value, IsServer ? group.offset : manager.localObserver.group.offset);
        }

    }
}