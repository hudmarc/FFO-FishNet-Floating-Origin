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
        private readonly Dictionary<NetworkConnection, FOClient> networkedClients = new Dictionary<NetworkConnection, FOClient>();
        internal Scene mainScene;
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

            if (!mainScene.IsValid())
            {
                return;
            }
            ioffsetter.Offset(mainScene, (Vector3)difference);

            if (remainder != Vector3.zero)
            {
                ioffsetter.Offset(mainScene, (Vector3)remainder);
                Log("Remainder was not zero, offset with precise remainder. If this causes a bug, now you know what to debug.", "NETWORKING");
            }

        }

        [Server]
        internal void SyncOffset(FOClient client)
        {
            Log("Synchronizing Broadcast", "NETWORKING");
            if (!client.networking.IsOwner)
            {
                InstanceFinder.ServerManager.Broadcast(client.networking.Owner, new OffsetSyncBroadcast(offsetGroups[client.gameObject.scene].offset));
            }
        }

        internal Scene GetSceneForConnection(NetworkConnection connection)
        {
            if (!networkedClients.ContainsKey(connection))
            {
                return new Scene();
            }
            return networkedClients[connection].gameObject.scene;
        }

        void SyncGroup(OffsetGroup group)
        {
            Log($"Synchronizing group {Math.Abs(group.scene.handle):X}", "NETWORKING");

            foreach (FOClient client in group.clients)
            {
                if (!client.IsOwner)
                {
                    SyncOffset(client);
                }
            }
        }
    }
}
