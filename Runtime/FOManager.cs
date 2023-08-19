using System;
using System.Collections.Generic;
using FishNet.FloatingOrigin.Types;
using UnityEngine;
using FishNet.Managing.Timing;
using UnityEngine.SceneManagement;
using FishNet.Connection;
using System.Linq;
using FishNet.Observing;
using FishNet.Transporting;
using System.Data;

namespace FishNet.FloatingOrigin
{
    public partial class FOManager : MonoBehaviour
    {
        /// <summary>
        /// Set to whatever value in meters your game starts to noticeably lose precision at.
        /// </summary>
        public const int REBASE_CRITERIA = 512;
        public const int HYSTERESIS = 0;
        public const int MERGE_CRITERIA = (REBASE_CRITERIA / 2) + HYSTERESIS;
        public static FOManager instance;

        public PhysicsMode PhysicsMode => _physicsMode;

        public Action<OffsetGroup> GroupChanged { get; internal set; }

        internal FOObserver first;

        private readonly Dictionary<Scene, OffsetGroup> offsetGroups = new Dictionary<Scene, OffsetGroup>();
        private readonly HashSet<FOObserver> observers = new HashSet<FOObserver>();
        private readonly HashGrid<OffsetGroup> hashGrid = new HashGrid<OffsetGroup>(REBASE_CRITERIA / 2);

        private bool subscribedToTick = false;

        private void Awake()
        {
            if (instance != null)
            {
                gameObject.SetActive(false);
                return;
            }
            instance = this;
        }

        private void Start()
        {
            InstanceFinder.ServerManager.OnServerConnectionState += ServerFullStart;
            InstanceFinder.ClientManager.OnClientConnectionState += ClientFullStart;
            ioffsetter = GetComponent<IOffsetter>();

        }
        void OnDisable()
        {
            InstanceFinder.ServerManager.OnServerConnectionState -= ServerFullStart;
            InstanceFinder.ClientManager.UnregisterBroadcast<OffsetSyncBroadcast>(OnOffsetSyncBroadcast);
            InstanceFinder.TimeManager.OnTick -= OnTick;
        }

        private void ServerFullStart(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState != LocalConnectionState.Started)
            {
                return;
            }
            InstanceFinder.TimeManager.OnTick += OnTick;

            GetComponent<TimeManager>().SetPhysicsMode(PhysicsMode.Disabled);
            Physics.autoSimulation = false;
            SetPhysicsMode(PhysicsMode);
        }

        private void ClientFullStart(ClientConnectionStateArgs args)
        {
            Log(args.ConnectionState.ToString(), "NETWORKING");
            if (args.ConnectionState != LocalConnectionState.Started || InstanceFinder.IsServer)
            {
                return;
            }
            InstanceFinder.ClientManager.RegisterBroadcast<OffsetSyncBroadcast>(OnOffsetSyncBroadcast);
            Log("Listening for offset sync broadcasts...", "NETWORKING");
        }

        public bool HasScene(Scene scene)
        {
            return offsetGroups.ContainsKey(scene);
        }

        internal void RegisterObserver(FOObserver observer)
        {
            if (first == null)
            {
                first = observer;
            }
            observers.Add(observer);

            if (!networkedObservers.ContainsKey(observer.Owner))
            {
                networkedObservers.Add(observer.Owner, observer);
            }
            OffsetGroup group = null;
            if (!offsetGroups.ContainsKey(observer.gameObject.scene))
            {
                group = new OffsetGroup(observer.gameObject.scene, Vector3d.zero);

                offsetGroups.Add(observer.gameObject.scene, group);
                Log($"Added group {group.scene.handle}", "HOUSEKEEPING");
            }
            else
            {
                group = offsetGroups[observer.gameObject.scene];
            }

            group.observers.Add(observer);
            Log($"Added observer to group {group.scene.handle}", "HOUSEKEEPING");

            if (!observer.IsOwner)
            {
                //this is good
                SyncOffset(observer, observer.realPosition - group.offset);
                // observer.SyncPosition(observer.Owner, observer.unityPosition);
            }

        }
        internal void UnregisterObserver(FOObserver observer)
        {
            if (observer.GetType() == typeof(FOObserver))
            {
                observers.Remove((FOObserver)observer);
                offsetGroups[observer.gameObject.scene].observers.Remove((FOObserver)observer);

                if (networkedObservers.ContainsKey(observer.Owner))
                {
                    networkedObservers.Remove(observer.Owner);
                }
            }
        }
        void OnTick()
        {
            foreach (FOObserver observer in observers)
            {
                if (Functions.MaxLengthScalar(observer.unityPosition) < REBASE_CRITERIA)
                    continue;

                OffsetGroup group = offsetGroups[observer.gameObject.scene];
                Vector3d difference = group.offset - observer.realPosition;
                if (group.observers.Count > 1)
                {
                    RequestMoveToNewGroup(observer);
                    SyncObservers(group, difference);
                    return; //we could also queue the remove operation in MoveFromGroupToGroup
                }
                else
                {
                    SetGroupOffset(group, observer.realPosition);
                    SyncObservers(group, difference);
                }

            }
        }
        // This does not seem to work very well at the moment.
        void SyncObservers(OffsetGroup group, Vector3d difference)
        {
            foreach (FOObserver ob in group.observers)
            {
                if (!ob.IsOwner)
                {
                    SyncOffset(ob, difference);
                }
            }
        }

        internal void MoveFromGroupToGroup(FOObject foobject, OffsetGroup to)
        {
            if (foobject.GetType() == typeof(FOObserver))
            {
                offsetGroups[foobject.gameObject.scene].observers.Remove((FOObserver)foobject);

                if (offsetGroups[foobject.gameObject.scene].observers.Count < 1)
                {
                    queuedGroups.Enqueue(offsetGroups[foobject.gameObject.scene]);
                }

                offsetGroups[to.scene].observers.Add((FOObserver)foobject);
            }
            SceneManager.MoveGameObjectToScene(foobject.gameObject, to.scene);

            Log($"client {foobject.OwnerId} group {foobject.gameObject.scene.handle} moved to group {to.scene.handle}");
            if (InstanceFinder.IsHost)
                RecomputeVisibleScenes();
        }



        /// <summary>
        /// Tries to move this observer and any nearby FOObjects in range to a new group.
        /// </summary>
        /// <param name="observer"></param>
        /// <returns></returns>
        private void RequestMoveToNewGroup(FOObserver observer)
        {
            //This is super zorked!

            OffsetGroup group = RequestNewGroup(observer.gameObject.scene);

            if (group == null)
                return;

            // Vector3d difference = group.offset - observer.realPosition;
            // foreach (FOObserver ob in group.observers)
            // {
            //     if (!ob.IsOwner)
            //     {
            //         SyncOffset(ob, difference);
            //     }

            // }

            SetGroupOffset(group, observer.realPosition);


            // MoveFromGroupToGroup(observer, group);






            // observer.unityPosition -= difference;

            Log($"Tried to move observer from client {observer.OwnerId} to {group.scene.handle}", "GROUP MANAGEMENT");
        }


        private void SetPhysicsMode(PhysicsMode mode)
        {
            if (mode == PhysicsMode.TimeManager)
            {
                if (!subscribedToTick)
                {
                    InstanceFinder.TimeManager.OnTick += Simulate;
                    subscribedToTick = true;
                }
            }
            else
            {
                InstanceFinder.TimeManager.OnTick -= Simulate;
                subscribedToTick = false;
            }
            _physicsMode = mode;
        }
        void FixedUpdate()
        {
            if (_physicsMode == PhysicsMode.Unity)
                Simulate();
        }
        internal void Simulate()
        {
            foreach (Scene scene in offsetGroups.Keys)
                if (scene.IsValid())
                    scene.GetPhysicsScene().Simulate(Time.fixedDeltaTime);
        }

        internal Vector3d GetOffset(Scene scene) => offsetGroups[scene].offset;
    }
}