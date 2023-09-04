using System;
using System.Collections.Generic;
using FishNet.FloatingOrigin.Types;
using UnityEngine;
using FishNet.Managing.Timing;
using UnityEngine.SceneManagement;
using FishNet.Transporting;

namespace FishNet.FloatingOrigin
{
    public partial class FOManager : MonoBehaviour
    {
        /// <summary>
        /// Set to whatever value in meters your game starts to noticeably lose precision at. ~8km is the default setting.
        /// </summary>
        public const int REBASE_CRITERIA = 8192;
        public const int HYSTERESIS = 0;
        public const int MERGE_CRITERIA = REBASE_CRITERIA / 2;
        public static FOManager instance;

        public PhysicsMode PhysicsMode => _physicsMode;

        internal FOClient local;

        [Tooltip("How to perform physics.")]
        [SerializeField]
        private PhysicsMode _physicsMode = PhysicsMode.Unity;

        private readonly Dictionary<Scene, OffsetGroup> offsetGroups = new Dictionary<Scene, OffsetGroup>();
        private readonly HashSet<FOClient> clients = new HashSet<FOClient>();
        private readonly HashGrid<FOObject> objects = new HashGrid<FOObject>(REBASE_CRITERIA);
        private readonly HashGrid<OffsetGroup> groups = new HashGrid<OffsetGroup>(REBASE_CRITERIA);
        private readonly Queue<OffsetGroup> queuedGroups = new Queue<OffsetGroup>();
        private readonly LoadSceneParameters parameters = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
        private readonly Scene invalidScene = new Scene();
        private IOffsetter ioffsetter;

        private bool subscribedToTick = false;
        private bool serverFullStart = false;
        private bool clientFullStart = false;

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
            InstanceFinder.ClientManager.UnregisterBroadcast<OffsetSyncBroadcast>(
                OnOffsetSyncBroadcast
            );
            InstanceFinder.TimeManager.OnPreTick -= PreTick;
        }

        private void ServerFullStart(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState != LocalConnectionState.Started)
                return;

            InstanceFinder.TimeManager.OnPreTick += PreTick;

            GetComponent<TimeManager>().SetPhysicsMode(PhysicsMode.Disabled);
            Physics.autoSimulation = false;
            SetPhysicsMode(PhysicsMode);
            serverFullStart = true;
        }

        private void ClientFullStart(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState != LocalConnectionState.Started || InstanceFinder.IsServer)
                return;

            InstanceFinder.ClientManager.RegisterBroadcast<OffsetSyncBroadcast>(
                OnOffsetSyncBroadcast
            );
            Log("Listening for offset sync broadcasts...", "NETWORKING");
            clientFullStart = true;
        }

        public bool HasScene(Scene scene)
        {
            return offsetGroups.ContainsKey(scene);
        }

        internal void RegisterClient(FOClient client)
        {
            if (!serverFullStart && InstanceFinder.IsClientOnly)
                return;

            if (local == null)
            {
                local = client;
            }
            clients.Add(client);

            if (!networkedClients.ContainsKey(client.networking.Owner))
            {
                networkedClients.Add(client.networking.Owner, client);
            }

            OffsetGroup group;

            if (!offsetGroups.ContainsKey(client.gameObject.scene))
            {
                group = new OffsetGroup(client.gameObject.scene, Vector3d.zero);
                AddOffsetGroup(group);
            }
            else
            {
                group = offsetGroups[client.gameObject.scene];
            }

            group.clients.Add(client);
            Log($"Added FOClient to group {Math.Abs(group.scene.handle):X}", "HOUSEKEEPING");

            if (!client.networking.IsOwner)
            {
                SyncOffset(client);
            }
        }

        internal void UnregisterClient(FOClient client)
        {
            if (!serverFullStart && InstanceFinder.IsClientOnly)
                return;

            clients.Remove(client);
            offsetGroups[client.gameObject.scene].clients.Remove(client);

            if (networkedClients.ContainsKey(client.networking.Owner))
            {
                networkedClients.Remove(client.networking.Owner);
            }
        }

        internal void RegisterObject(FOObject foobject)
        {
            if (!serverFullStart && InstanceFinder.IsClientOnly)
                return;
            if (offsetGroups.ContainsKey(foobject.gameObject.scene))
                objects.Add(foobject.realPosition, foobject);
            else
                objects.Add((Vector3d)foobject.transform.position, foobject);
        }

        internal void UnregisterObject(FOObject foobject)
        {
            if (!serverFullStart && InstanceFinder.IsClientOnly)
                return;
            objects.Remove(foobject.realPosition);
        }

        private void SetGroupOffset(OffsetGroup group, Vector3d offset)
        {
            Log($"Set group offset {offset} for {Math.Abs(group.scene.handle):X}", "SCENE MANAGEMENT");
            groups.Remove(group.offset);
            groups.Add(offset, group);

            Vector3d difference = group.offset - offset;
            Vector3 remainder = (Vector3)(difference - ((Vector3d)(Vector3)difference));

            ioffsetter.Offset(group.scene, (Vector3)difference);

            if (remainder != Vector3.zero)
            {
                ioffsetter.Offset(group.scene, remainder);
                Log(
                    "Remainder was not zero, offset with precise remainder. If this causes a bug, now you know what to debug.",
                    "SCENE MANAGEMENT"
                );
            }

            group.offset = offset;

            CollectObjectsIntoGroup(group);
            SyncGroup(group);
        }

        private void PreTick()
        {
            // If an OffsetGroup has a pack of clients which are closely clumped this should keep them in the group while automatically kicking stragglers off to other groups.
            foreach (OffsetGroup group in offsetGroups.Values)
            {
                Vector3 centroid = group.GetClientCentroid();
                if (centroid.sqrMagnitude >= REBASE_CRITERIA * REBASE_CRITERIA)
                {
                    SetGroupOffset(group, group.offset + (Vector3d)centroid);
                }
            }
            foreach (FOClient client in clients)
            {
                // If this loop becomes a performance concern, can it be run less than every frame? For example only processing every nth client every frame?
                OffsetGroup found = groups.FindAnyInBoundingBox(client.realPosition, MERGE_CRITERIA, offsetGroups[client.gameObject.scene]);

                if (found != null && found.clients.Count > 0)
                {
                    Debug.Log($"Client in {Math.Abs(client.gameObject.scene.handle):X} found non-empty group {Math.Abs(found.scene.handle):X}");
                    MoveToGroup(client, found);
                    continue;
                }

                if (Functions.MaxLengthScalar(client.transform.position) < REBASE_CRITERIA)
                    continue;

                Debug.Log($"Rebasing {Math.Abs(client.gameObject.scene.handle):X}");

                OffsetGroup group = offsetGroups[client.gameObject.scene];

                if (group.clients.Count > 1)
                {
                    RequestMoveToNewGroup(client);
                    return;
                }
            }
        }

        /// <summary>
        /// Moves the given Foobject to the given group.
        /// If the given Foobject was an FOClient, re-registers the FOClient with the new group.
        /// </summary>
        /// <param name="foobject">
        /// The FOObject to move.
        /// </param>
        /// <param name="to">
        /// The scene to move the FOObject to.
        /// </param>
        private void MoveToGroup(FOObject foobject, OffsetGroup to)
        {
            if (foobject.GetType() == typeof(FOClient))
                UpdateClientGroup((FOClient)foobject, to);

            foobject.transform.position = RealToUnity(foobject.realPosition, to.scene);
            SceneManager.MoveGameObjectToScene(foobject.gameObject, to.scene);

            Log($"client {foobject.networking.OwnerId} group {Math.Abs(foobject.gameObject.scene.handle):X} moved to group {Math.Abs(to.scene.handle):X}");
            if (InstanceFinder.IsHost)
                RecomputeVisibleScenes();
        }

        // This updates the registration of the given FOClient. Should only be called internally by MoveToGroup.
        private void UpdateClientGroup(FOClient client, OffsetGroup to)
        {
            offsetGroups[client.gameObject.scene].clients.Remove(client);

            if (offsetGroups[client.gameObject.scene].clients.Count < 1)
            {
                if (queuedGroups.Count < 1 || queuedGroups.Peek() != offsetGroups[client.gameObject.scene])
                    queuedGroups.Enqueue(offsetGroups[client.gameObject.scene]);
            }

            offsetGroups[to.scene].clients.Add(client);
        }

        /// <summary>
        /// Tries to move this FOClient and any nearby FOObjects in range to a new group.
        /// </summary>
        /// <param name="observer"></param>
        private void RequestMoveToNewGroup(FOClient observer)
        {
            OffsetGroup group = RequestNewGroup(observer.gameObject.scene);

            if (group == null)
                return;

            SetGroupOffset(group, observer.realPosition);

            MoveToGroup(observer, group);

            CollectObjectsIntoGroup(group);

            Log($"Moved FOClient from client {observer.networking.OwnerId} to queued group {Math.Abs(group.scene.handle):X}", "GROUP MANAGEMENT");
        }

        private void CollectObjectsIntoGroup(OffsetGroup group)
        {
            // Stopwatch watch = new Stopwatch();
            // watch.Start();
            var found = objects.FindInBoundingBox(group.offset, 4);
            foreach (FOObject foobject in found)
            {
                if (foobject.gameObject.scene.handle == group.scene.handle)
                    MoveToGroup(foobject, group);
            }
            // watch.Stop();
        }

        private void FixedUpdate()
        {
            if (_physicsMode == PhysicsMode.Unity)
                Simulate();
        }

        internal void Simulate()
        {
            foreach (Scene scene in offsetGroups.Keys)
            {
                if (scene.IsValid())
                    scene.GetPhysicsScene().Simulate(Time.fixedDeltaTime);
            }
        }

        /// <summary>
        /// Requests a new group. Returns first found existing group that is unused or a newly created group.
        /// </summary>
        /// <param name="scene">
        /// Scene that should be instantiated.
        /// </param>
        private OffsetGroup RequestNewGroup(Scene scene)
        {
            while (queuedGroups.Count > 0 && queuedGroups.Peek().clients.Count > 0)
            {
                queuedGroups.Dequeue();
            }
            if (queuedGroups.Count > 0)
            {
                return queuedGroups.Dequeue();
            }
            else
            {
                var offsetGroup = new OffsetGroup(invalidScene, Vector3d.zero);
                SceneManager.LoadSceneAsync(scene.buildIndex, parameters).completed += (arg) =>
                    SetupGroup(offsetGroup);
                return null;
            }
        }
        private void SetupGroup(OffsetGroup group)
        {
            group.scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);

            AddOffsetGroup(group);

            CullNetworkObjects(group.scene);
        }
        /// <summary>
        /// Adds the given OffsetGroups to the tracked OffsetGroups and to the Groups hashgrid.
        /// </summary>
        /// <param name="group">
        /// The offset group being added.
        /// </param>
        private void AddOffsetGroup(OffsetGroup group)
        {
            //add to HashGrid as well!
            offsetGroups.Add(group.scene, group);
            //There should NEVER be two OffsetGroups with the same offset. By virtue of having the same offset this means the groups would be merged.
            groups.Add(group.offset, group);
            queuedGroups.Enqueue(group);
            Log(
                $"Added group {Math.Abs(group.scene.handle):X} on grid position {groups.Quantize(group.offset)}",
                "HOUSEKEEPING"
            );
        }
    }
}
