using FishNet.Object;
using UnityEngine;

namespace FishNet.FloatingOrigin
{
    [RequireComponent(typeof(NetworkObject))]
    public class FOClient : FOObject
    {
        Vector3 init;
        internal override void Initialize() => FOManager.instance.RegisterClient(this);
        internal override void Deinitialize() => FOManager.instance.UnregisterClient(this);
        void Start()
        {
            init = transform.position;
        }
    }
}