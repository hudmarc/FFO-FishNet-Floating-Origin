#if FISHNET
using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.FloatingOrigin.Types;
using FishNet.Object;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FishNet.FloatingOrigin
{
    public partial class FOManager
    {
        private Vector3d localOffset = Vector3d.zero;

        [Client]
        void OnOffsetSyncBroadcast(OffsetSyncBroadcast broadcast)
        {
            Vector3d difference = localOffset - broadcast.offset;

            Log($"Received Offset Sync Broadcast {broadcast.offset} offsetting {difference} from {localOffset}", "NETWORKING");

            localOffset = broadcast.offset;

            if (InstanceFinder.IsServer)
                return;

            Vector3 remainder = (Vector3)(difference - ((Vector3d)(Vector3)difference));

            if (!GetLocalFirstCached().gameObject.scene.IsValid())
            {
                return;
            }
            ioffsetter.Offset(GetLocalFirstCached().gameObject.scene, (Vector3)difference);

            if (remainder != Vector3.zero)
            {
                ioffsetter.Offset(GetLocalFirstCached().gameObject.scene, (Vector3)remainder);
                Log("Remainder was not zero, offset with precise remainder. If this causes a bug, now you know what to debug.", "NETWORKING");
            }

        }

        [Server]
        internal void SyncOffset(FOView view)
        {
            if (view._networking != null && view._networking.Owner?.IsValid == true && !view._networking.IsOwner)
            {
                Log("Synchronizing Broadcast", "NETWORKING");
                InstanceFinder.ServerManager.Broadcast(view._networking.Owner, new OffsetSyncBroadcast(offsetGroups[view.gameObject.scene].offset));
            }
        }
        [Client]
        private NetworkObject GetLocalFirstCached()
        {
            if (cachedFirst == null && InstanceFinder.ClientManager.Connection != null)
            {
                cachedFirst = GetLocalFirst();
            }
            return cachedFirst;
        }
        [Client]
        private NetworkObject GetLocalFirst() => InstanceFinder.ClientManager.Connection.FirstObject;

        internal Scene GetSceneForConnection(NetworkConnection connection) => connection.FirstObject.gameObject.scene;
        private NetworkObject cachedFirst = null;
        void SyncGroup(OffsetGroup group)
        {
            Log($"Synchronizing group {group.scene.ToHex()}", "NETWORKING");

            foreach (FOView view in group.views)
            {
                if (view._networking?.IsOwner == false)
                {
                    SyncOffset(view);
                }
            }
        }
    }
}
#endif