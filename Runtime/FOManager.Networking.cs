// using FishNet.Component.Transforming;
using FishNet.FloatingOrigin.Types;
using FishNet.Object;
using UnityEngine;

namespace FishNet.FloatingOrigin
{
    public partial class FOManager
    {
        [Client]
        void OnOffsetSyncBroadcast(OffsetSyncBroadcast broadcast)
        {
            if (localObserver == null || InstanceFinder.IsServer)
                return;

            // Debug.Log($"Received offset sync broadcast {broadcast.ToString()}");
            // NetworkTransform.ForceResyncAll();

            OffsetScene(localObserver.gameObject.scene, localObserver.group.offset, broadcast.offset);
            localObserver.group.offset = broadcast.offset;
            
            Rebased?.Invoke(broadcast.offset);
        }
        [Server]
        internal void SyncOffset(FOObserver observer, Vector3d offset)
        {
            if (!observer.IsOwner)
            {
                // Debug.Log($"Sending offset sync broadcast {new OffsetSyncBroadcast(offset).ToString()}");
                // NetworkTransform.ForceResyncAll();
                InstanceFinder.ServerManager.Broadcast(observer.Owner, new OffsetSyncBroadcast(offset));
            }
        }
    }
}
