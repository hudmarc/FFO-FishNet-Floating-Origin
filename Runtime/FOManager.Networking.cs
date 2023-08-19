// using FishNet.Component.Transforming;
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
        private readonly Dictionary<NetworkConnection, FOObserver> networkedObservers = new Dictionary<NetworkConnection, FOObserver>();
        internal Scene mainScene;
        [Client]
        void OnOffsetSyncBroadcast(OffsetSyncBroadcast broadcast)
        {
            Log($"Received Offset Sync Broadcast {broadcast.offset}", "NETWORKING");

            if (InstanceFinder.IsServer)
                return;


            Vector3 remainder = (Vector3)(broadcast.offset - ((Vector3d)(Vector3)broadcast.offset));

            //yup this is kind of hacky
            if(!mainScene.IsValid())
            {
                return;
            }
            ioffsetter.Offset(mainScene, (Vector3)broadcast.offset);

            if (remainder != Vector3.zero)
            {
                ioffsetter.Offset(mainScene, (Vector3)remainder);
                Log("Remainder was not zero, offset with precise remainder. If this causes a bug, now you know what to debug.", "SCENE MANAGEMENT");
            }

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
        internal Scene GetSceneForConnection(NetworkConnection connection)
        {
            if (!networkedObservers.ContainsKey(connection))
            {
                return new Scene();
            }
            return networkedObservers[connection].gameObject.scene;
        }
    }
}
