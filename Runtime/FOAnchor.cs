using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FishNet.FloatingOrigin
{
    public class FOAnchor : NetworkBehaviour, IRealTransform
    {
        [SyncVar]
        [SerializeField] private double x, y, z;
        public Vector3d realPosition
        {
            get => new Vector3d(x, y, z);
            set
            {
                x = value.x;
                y = value.y;
                z = value.z;
            }
        }
        private FOManager manager;
        private bool initialized = false;
        public override void OnStartClient()
        {
            base.OnStartClient();
            if (initialized)
                return;
            initialized = true;
            Initialize();
        }
        public override void OnStartServer()
        {
            base.OnStartServer();
            if (initialized)
                return;
            initialized = true;
            Initialize();
        }
        private void Initialize()
        {
            manager = FOManager.instance;
            manager.SceneChanged += OnRebase;
        }
        private void OnDisable()
        {
            if (manager != null)
                manager.SceneChanged -= OnRebase;
        }
        void OnRebase(Scene scene)
        {
            if (scene == gameObject.scene)
                transform.position = manager.RealToUnity(realPosition, gameObject.scene);
        }
    }
}