using FishNet.Connection;
using FishNet.FloatingOrigin.Types;
using FishNet.Object;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FishNet.FloatingOrigin
{
    public class FOObject : NetworkBehaviour, IRealTransform
    {
        // private bool busy = false; //If true we are waiting for the scene we are going to be moved into to load so don't try rebuilding us
        internal Vector3Int gridPosition;
        [SerializeField] protected bool autoRegister = true;
        protected FOManager manager;
        public Vector3d overrideRealPosition = Vector3d.zero;
        void Awake()
        {
            manager = FOManager.instance;
        }
        public override void OnOwnershipServer(NetworkConnection prevOwner)
        {
            base.OnOwnershipServer(prevOwner);
            if (autoRegister)
                Initialize();
        }
        Vector3 lastPosition;

        private void Update()
        {
            if (name != "FOObject A")
            {
                return;
            }
            if (lastPosition != transform.position)
            {
                Debug.Log($"Position change {InstanceFinder.TimeManager.Tick}");
                lastPosition = transform.position;
            }
        }


        void OnGroupChanged(OffsetGroup group)
        {

            Debug.Log($"Group change! Offset {manager.GetOffset(gameObject.scene)} id: {ObjectId} name: {name} position: {realPosition}");
            //IFF we are in range of the new offset group:



            if (Functions.MaxLengthScalar(realPosition - group.offset) < FOManager.REBASE_CRITERIA || Functions.MaxLengthScalar(manager.GetOffset(gameObject.scene) - group.offset) < FOManager.MERGE_CRITERIA)  //
            {
                // SyncPosition(Owner, unityPosition);
                if (group.scene.handle != gameObject.scene.handle)
                {
                    Debug.Log($"{group.scene.handle} Met merge criteria with {gameObject.scene.handle}");
                    transform.position = Functions.RealToUnity(realPosition, group.offset); //manager.GetOffset(gameObject.scene) -
                    manager.MoveFromGroupToGroup(this, group);

                }

            }

        }
        [Server]
        public virtual void Initialize() { }
        public virtual void Deinitialize() { }
        void Start()
        {
            if (manager.HasScene(gameObject.scene))
                transform.position = manager.RealToUnity(Mathd.toVector3d(transform.position), gameObject.scene);

            manager.GroupChanged += OnGroupChanged;
            manager.mainScene = gameObject.scene;
        }
        private void OnDestroy()
        {
            if (!IsServer)
            {
                return;
            }
            manager.GroupChanged -= OnGroupChanged;
            Deinitialize();
        }

        public Vector3 unityPosition
        {
            get => transform.position;
            set => transform.position = value;
        }

        #region position sync

        // public override void OnOwnershipClient(NetworkConnection prevOwner)
        // {
        //     base.OnOwnershipClient(prevOwner);
        //     manager.mainScene = gameObject.scene;
        //     if (InstanceFinder.NetworkManager != null)
        //         RequestSync(InstanceFinder.ClientManager.Connection);
        // }

        /// <summary>
        /// Gets the RealPosition of this FOObject. How will this work on clients?
        /// </summary>
        public Vector3d realPosition
        {
            get => manager.UnityToReal(unityPosition, gameObject.scene);
            set => transform.position = manager.RealToUnity(value, gameObject.scene);
        }
        // public Vector3d groupOffset => FOManager.instance.GetOffset(gameObject.scene);
        // [ServerRpc(RequireOwnership = false)]
        // public void RequestSync(NetworkConnection connection)
        // {
        //     SyncPosition(connection, unityPosition);
        // }
        // [Server]
        // [TargetRpc]
        // public void SyncPosition(NetworkConnection connection, Vector3 position)
        // {
        //     Debug.Log(position);

        //     transform.position = position;
        // }
        #endregion
    }
}