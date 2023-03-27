using FishNet.Object;
using FishNet.Connection;

namespace FishNet.FloatingOrigin
{
    public class FOObserver : FOObject, IRealTransform
    {
        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);
            if (autoRegister)
                InitializeOnClient();
        }
        public override void OnOwnershipServer(NetworkConnection prevOwner)
        {
            base.OnOwnershipServer(prevOwner);
            if (autoRegister)
                InitializeOnServer();
        }
        private void InitializeOnClient()
        {
            if (!IsServer)
                manager.RegisterFOObserver(this);
        }
        [Server]
        private void InitializeOnServer() => manager.RegisterFOObserver(this);
    }
}