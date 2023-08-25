using FishNet.Object;
using UnityEngine;

namespace FishNet.FloatingOrigin
{
    [RequireComponent(typeof(NetworkObject))]
    public class FOClient : FOObject
    {
        internal override void Initialize() => FOManager.instance.RegisterClient(this);
        internal override void Deinitialize() => FOManager.instance.UnregisterClient(this);
    }
}