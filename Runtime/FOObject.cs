using FishNet.Object;
using UnityEngine;

namespace FishNet.FloatingOrigin
{
    public class FOObject : MonoBehaviour
    {

        internal NetworkObject _networking;
        public NetworkObject networking => _networking;
        private bool initialized = false;
        [Tooltip("The object will move precisely to the anchored position, if it is not Vector3d.zero")]
        public Vector3d anchoredPosition;
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
        public void MoveToAnchor()
        {
            if (anchoredPosition != Vector3d.zero)
            {
                transform.position = FOManager.instance.RealToUnity(anchoredPosition, gameObject.scene);
            }
        }
    }
}