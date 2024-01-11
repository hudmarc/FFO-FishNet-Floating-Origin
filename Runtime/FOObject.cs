using FishNet.Object;
using UnityEngine;

namespace FishNet.FloatingOrigin
{
    public class FOObject : MonoBehaviour
    {

        internal NetworkObject _networking;
        public NetworkObject networking => _networking;
        private bool initialized = false;
        
        void Start()
        {
            TryGetComponent(out _networking);
            //if initialize failed, this object must not deinitialize, otherwise it will deinitalize the "real" object at the same position.
            Initialize();
            initialized = true;
        }
        void OnDestroy()
        {
            if (!initialized)
                return;
            //this will not run unless this object was correctly initialized
            Deinitialize();
        }
        public Vector3d realPosition => FOManager.instance.UnityToReal(transform.position, gameObject.scene);
        internal virtual void Initialize() { FOManager.instance?.RegisterObject(this); }
        internal virtual void Deinitialize()
        {
            FOManager.instance?.UnregisterObject(this);
        }
    }
}