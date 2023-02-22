using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace FishNet.FloatingOrigin
{
    public class FOAnchor : NetworkBehaviour
    {
        [SyncVar]
        [SerializeField] private double x, y, z;
        private Vector3d position => new Vector3d(x, y, z);
        private FOManager manager;
        private bool initialized = false;
        public override void OnStartClient()
        {
            base.OnStartClient();
            if (initialized)
                return;
            initialized = true;
            initialize();
        }
        public override void OnStartServer()
        {
            base.OnStartServer();
            if (initialized)
                return;
            initialized = true;
            initialize();
        }
        private void initialize()
        {
            manager = FOManager.instance;
            manager.Rebased += OnRebase;
        }
        private void OnDisable()
        {
            if (manager != null)
                manager.Rebased -= OnRebase;
        }
        [ServerRpc]
        public void SetPosition(Vector3d newPosition)
        {
            x = newPosition.x;
            y = newPosition.y;
            z = newPosition.z;
        }
        void OnRebase(Vector3d newOffset)
        {
            transform.position = manager.RealToUnity(position);
        }
    }
}