using FishNet.Object;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FishNet.FloatingOrigin
{
    public class FOObject : NetworkBehaviour, IRealTransform
    {
        internal bool busy = false; //If true we are waiting for the scene we are going to be moved into to load so don't try rebuilding us
        internal Vector3Int gridPosition;
        internal int sceneHandle;
        [SerializeField] protected bool autoRegister = true;
        protected FOManager manager;
        public Vector3d overrideRealPosition = Vector3d.zero;
        void Awake()
        {
            manager = FOManager.instance;
        }
        void Start()
        {
            manager.RegisterFOObject(this);
            manager.RebasedScene += OnRebase;
            transform.position = manager.RealToUnity(Mathd.toVector3d(transform.position), gameObject.scene);
        }
        private void OnDestroy()
        {
            manager.RebasedScene -= OnRebase;
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
        public Vector3d groupOffset => FOManager.instance.GetOffset(gameObject.scene);
        void OnRebase(Scene scene)
        {
            // if(scene == gameObject.scene)
            //     transform.position = Vector3.zero;
        }
    }
}