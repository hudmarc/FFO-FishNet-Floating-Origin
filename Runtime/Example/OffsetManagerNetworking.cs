using FishNet.Managing;

namespace FloatingOffset.Runtime.Example
{
    public class OffsetManagerNetworking : OffsetManager
    {
        private NetworkManager networkManager;
        // Start is called before the first frame update
        new void Awake()
        {
            base.Awake();
            if (TryGetComponent(out networkManager))
            {

            }
            if (!useExternalUpdate)
                networkManager.TimeManager.OnPrePhysicsSimulation += PhysicsProcess;
        }

        void OnDisable()
        {
            if (networkManager == null)
                return;

            networkManager.TimeManager.OnPrePhysicsSimulation -= PhysicsProcess;
        }
    }
}
