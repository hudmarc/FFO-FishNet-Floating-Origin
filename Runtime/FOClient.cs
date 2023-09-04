using FishNet.Object;
using UnityEngine;

namespace FishNet.FloatingOrigin
{
    [RequireComponent(typeof(NetworkObject))]
    public class FOClient : FOObject
    {
        [SerializeField] private bool complainIfMoved = false;
        Vector3 init;
        internal override void Initialize() => FOManager.instance.RegisterClient(this);
        internal override void Deinitialize() => FOManager.instance.UnregisterClient(this);
        void Start()
        {
            init = transform.position;
        }
        void Update()
        {
            if (complainIfMoved)
            {
                if (FOManager.instance.UnityToReal(transform.position, gameObject.scene) != FOManager.instance.UnityToReal(init, gameObject.scene))
                {
                    Debug.LogError("I was moved!");
                    Debug.Break();
                }
            }
        }
    }
}