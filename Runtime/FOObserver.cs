using FishNet.Object;
using FishNet.Connection;

namespace FishNet.FloatingOrigin
{
    public class FOObserver : FOObject, IRealTransform
    {
        [Server]
        public override void Initialize() => manager.RegisterObserver(this);

        public override void Deinitialize() => manager.UnregisterObserver(this);
    }
}