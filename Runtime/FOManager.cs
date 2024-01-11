using System;
using System.Collections.Generic;
using FishNet.FloatingOrigin.Types;
using UnityEngine;
using FishNet.Managing.Timing;
using UnityEngine.SceneManagement;
using FishNet.Transporting;
using System.Collections;
using FishNet.Managing;

namespace FishNet.FloatingOrigin
{
    public partial class FOManager : MonoBehaviour
    {
        /// <summary>
        /// Set to whatever value in meters your game starts to noticeably lose precision at. 8192m (~8km) is the default setting.
        /// </summary>
        public const int REBASE_CRITERIA = 8192;
        public const int HYSTERESIS = 0;
        public const int MERGE_CRITERIA = REBASE_CRITERIA / 2;
        public static FOManager instance;

        public PhysicsMode physicsMode => _physicsMode;
        /// <summary>
        /// The Local FOView is the FOView around which the world is rebased.
        /// </summary>
        internal FOView local;

        [Tooltip("How to perform physics.")]
        [SerializeField]
        private PhysicsMode _physicsMode = PhysicsMode.Unity;

        private readonly Dictionary<Scene, OffsetGroup> offsetGroups = new Dictionary<Scene, OffsetGroup>();
        private readonly HashSet<FOView> views = new HashSet<FOView>();
        private readonly HashGrid<FOObject> objects = new HashGrid<FOObject>(REBASE_CRITERIA);
        private readonly HashGrid<OffsetGroup> groups = new HashGrid<OffsetGroup>(REBASE_CRITERIA);
        private readonly Queue<OffsetGroup> queuedGroups = new Queue<OffsetGroup>();
        private readonly LoadSceneParameters parameters = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
        private readonly Scene invalidScene = new Scene();

        private IOffsetter ioffsetter;
        private NetworkManager networkManager;

        private bool subscribedToTick = false;
        private bool hostFullStart = false;
        private bool clientFullStart = false;

        private void Awake()
        {
            if (instance != null)
            {
                gameObject.SetActive(false);
                return;
            }
            Physics.autoSimulation = false;
            if (!TryGetComponent(out networkManager))
            {
                //Offline setup
                Log("Started FOManager in offline mode", "HOUSEKEEPING");
                StartCoroutine(OfflineUpdate());
            }
            instance = this;
        }

        private void Start()
        {
            Log("Starting FOManager", "HOUSEKEEPING");
            ioffsetter = GetComponent<IOffsetter>();
            if (networkManager == null)
            {
                hostFullStart = true; //host is considered the local game in this case
                return;
            }


            networkManager.ServerManager.OnServerConnectionState += ServerFullStart;
            networkManager.ClientManager.OnClientConnectionState += ClientFullStart;
        }

        void OnDisable()
        {
            if (networkManager == null)
                return;

            networkManager.ServerManager.OnServerConnectionState -= ServerFullStart;

            if (hostFullStart)
            {
                networkManager.ClientManager.UnregisterBroadcast<OffsetSyncBroadcast>(OnOffsetSyncBroadcast);
                networkManager.TimeManager.OnPreTick -= ProcessGroupsAndViews;
            }
        }

        private void ServerFullStart(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState != LocalConnectionState.Started)
                return;

            networkManager.TimeManager.OnPreTick += ProcessGroupsAndViews;
            GetComponent<TimeManager>().SetPhysicsMode(PhysicsMode.Disabled);
            SetPhysicsMode(physicsMode);
            hostFullStart = true;
        }

        private void ClientFullStart(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState != LocalConnectionState.Started || networkManager.IsServer)
                return;

            networkManager.ClientManager.RegisterBroadcast<OffsetSyncBroadcast>(
                OnOffsetSyncBroadcast
            );
            Log("Listening for offset sync broadcasts...", "NETWORKING");
            clientFullStart = true;
        }

        public bool HasScene(Scene scene)
        {
            return offsetGroups.ContainsKey(scene);
        }

        internal void RegisterView(FOView view)
        {
            if (networkManager?.IsClientOnly == true)
                return;

            if (local?._networking.IsOwner != true)
            {
                local = view;
            }
            if (views.Contains(view))
            {
                Debug.LogWarning($"Tried to register FOView on {view.gameObject.name} that was already registered!");
                return;
            }
            views.Add(view);

            OffsetGroup group;

            if (!offsetGroups.ContainsKey(view.gameObject.scene))
            {
                group = new OffsetGroup(view.gameObject.scene, Vector3d.zero);
                AddOffsetGroup(group);
                queuedGroups.Enqueue(group);
            }
            else
            {
                group = offsetGroups[view.gameObject.scene];
            }

            group.views.Add(view);
            Log($"Added View to group {group.scene.ToHex()}", "HOUSEKEEPING");

            if (view._networking != null && !view._networking.IsOwner)
            {
                SyncOffset(view);
            }
        }

        internal void UnregisterView(FOView view)
        {
            if (!views.Contains(view))
                return;
            // Debug.LogWarning($"Tried to unregister FOView on {view.gameObject.name} that is not registered!");



            views.Remove(view);
            offsetGroups[view.gameObject.scene].views.Remove(view);
        }

        internal void RegisterObject(FOObject foobject)
        {
            Log($"Registered object {foobject.name} from scene {foobject.gameObject.scene.ToHex()}", "HOUSEKEEPING");
            if (offsetGroups.ContainsKey(foobject.gameObject.scene))
                objects.Add(foobject.realPosition, foobject);
            else
                objects.Add((Vector3d)foobject.transform.position, foobject);
        }

        internal void UnregisterObject(FOObject foobject)
        {
            Log($"Unregistered object {foobject.name} from scene {foobject.gameObject.scene.ToHex()}", "HOUSEKEEPING");

            objects.Remove(foobject.realPosition);
        }

        private void SetGroupOffset(OffsetGroup group, Vector3d offset)
        {
            Log($"Set group offset {offset} for {group.scene.ToHex()}", "SCENE MANAGEMENT");
            groups.Remove(group.offset);
            groups.Add(offset, group);

            OffsetScene(group.scene, group.offset, offset);

            group.offset = offset;

            CollectObjectsIntoGroup(group);
            SyncGroup(group);
            DoOffsetCallbackOnObjects(group);
        }
        private void OffsetScene(Scene scene, Vector3d previous, Vector3d offset)
        {
            Vector3d difference = previous - offset;

            Vector3 remainder = (Vector3)(difference - ((Vector3d)((Vector3)difference)));

            ioffsetter.Offset(scene, (Vector3)difference);
            if (InstanceFinder.IsServer)
                Log($"Offset {scene.ToHex()} by {(Vector3)difference}");

            if (remainder != Vector3.zero)
            {
                Debug.Log($"Remainder was {remainder}");
                ioffsetter.Offset(scene, remainder);
                if (InstanceFinder.IsServer)
                {
                    Log($"Offset {scene.ToHex()} by {remainder}");
                    Log("Offset with precise remainder. If this causes a bug, now you know what to debug.", "SCENE MANAGEMENT");
                }
            }
        }
        private void DoOffsetCallbackOnObjects(OffsetGroup group)
        {
            var objects = group.GetFOObjectsCached();
            foreach (FOObject foo in objects)
            {
                foo.MoveToAnchor();
            }
        }
        internal void ProcessGroupsAndViews()
        {
            foreach (FOView view in views)
            {
                // If this loop becomes a performance concern, can it be run less than every frame? For example only processing every nth view every frame?
                OffsetGroup found = groups.FindAnyInBoundingBox(view.realPosition, MERGE_CRITERIA, offsetGroups[view.gameObject.scene]);

                if (found != null)
                {
                    if (found.views.Count > 0)
                    {
                        Log($"View in {view.gameObject.scene.ToHex()} found non-empty group {found.scene.ToHex()}");
                        MoveToGroup(view, found);
                        continue;
                    }
                    if (found.scene == invalidScene)
                    {
                        continue;
                    }
                }

                if (Functions.MaxLengthScalar(view.transform.position) < REBASE_CRITERIA)
                    continue;

                Log($"View {view.networking?.ObjectId} in group {view.gameObject.scene.ToHex()} is out of bounds ({view.realPosition}), attempting to move to new group.");

                OffsetGroup group = offsetGroups[view.gameObject.scene];

                if (group.views.Count > 1)
                {
                    RequestMoveToNewGroup(view);
                }
            }

            // If an OffsetGroup has a pack of Views which are closely clumped this should keep them in the group while automatically kicking stragglers off to other groups.
            // If this for loop is runs before the loop that iterates over Views it freaks out in a multiplayer environment and will sometimes cause incorrect offset.
            // When the loop runs here, after Views, it seems to work fine.
            foreach (OffsetGroup group in offsetGroups.Values)
            {
                Vector3 centroid = group.GetClientCentroid();
                if (centroid.sqrMagnitude >= REBASE_CRITERIA * REBASE_CRITERIA)
                {
                    Log($"Rebasing {group.scene.ToHex()}");
                    //This seems to cause problems when in multiplayer
                    SetGroupOffset(group, group.offset + (Vector3d)centroid);
                }
            }
        }

        /// <summary>
        /// Moves the given FOObject to the given group.
        /// If the given FOObject was an FOView, re-registers the FOView with the new group.
        /// </summary>
        /// <param name="foobject">
        /// The FOObject to move.
        /// </param>
        /// <param name="to">
        /// The scene to move the FOObject to.
        /// </param>
        private void MoveToGroup(FOObject foobject, OffsetGroup to)
        {
            bool is_view = foobject.GetType() == typeof(FOView);
            if (is_view)
                UpdateViewGroup((FOView)foobject, to);

            foobject.transform.position = RealToUnity(foobject.realPosition, to.scene);

            Log($"View {foobject._networking?.ObjectId} {foobject.gameObject.scene.ToHex()} will move to {to.scene.ToHex()}");

            if (!is_view)
                offsetGroups[foobject.gameObject.scene].MakeDirty();

            SceneManager.MoveGameObjectToScene(foobject.gameObject, to.scene);

            if (!is_view)
                to.MakeDirty();

            if (InstanceFinder.IsHost)
                RecomputeVisibleScenes();

            if (is_view)
            {
                var view = (FOView)foobject;
                if (view._networking != null && !view._networking.IsOwner)
                {
                    SyncOffset(view);
                }
            }

        }

        /// <summary>
        /// This updates the registration of the given FOView. Should only be called internally by MoveToGroup.
        /// </summary>
        /// <param name="view">The View who's registration will be updated.</param>
        /// <param name="to">The OffsetGroup this View is being moved to.</param>
        private void UpdateViewGroup(FOView view, OffsetGroup to)
        {
            offsetGroups[view.gameObject.scene].views.Remove(view);

            if (offsetGroups[view.gameObject.scene].views.Count < 1)
            {
                if (queuedGroups.Count < 1 || queuedGroups.Peek() != offsetGroups[view.gameObject.scene])
                    queuedGroups.Enqueue(offsetGroups[view.gameObject.scene]);
            }

            offsetGroups[to.scene].views.Add(view);
        }

        /// <summary>
        /// Tries to move this FOView and any nearby FOObjects in range to a new group.
        /// </summary>
        /// <param name="view">The View to move.</param>
        private void RequestMoveToNewGroup(FOView view)
        {
            OffsetGroup group = RequestNewGroup(view.gameObject.scene);

            if (group == null)
            {
                Log("Started loading new group.", "GROUP MANAGEMENT");
                return;
            }
            String old = view.gameObject.scene.ToHex();
            if (view.realPosition == Vector3d.one * REBASE_CRITERIA)
            {
                Debug.LogWarning("POTENTIAL ERROR CONDITION!");
            }

            SetGroupOffset(group, view.realPosition);

            MoveToGroup(view, group);

            // CollectObjectsIntoGroup(group);

            Log($"View {view.networking?.ObjectId} {old} -> {group.scene.ToHex()}", "GROUP MANAGEMENT");
        }
        /// <summary>
        /// Collects FOObjects in range of group into this OffsetGroup.
        /// </summary>
        /// <param name="group">The group to collect objects into.</param>
        private void CollectObjectsIntoGroup(OffsetGroup group)
        {
            var found = objects.FindInBoundingBox(group.offset, 4);
            foreach (FOObject foobject in found)
            {
                if (foobject.gameObject.scene.handle != group.scene.handle)
                    MoveToGroup(foobject, group);
            }
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
            while (queuedGroups.Count > 0 && queuedGroups.Peek().views.Count > 0)
            {
                queuedGroups.Dequeue();
            }
            if (queuedGroups.Count > 0)
            {
                if (queuedGroups.Peek().scene != invalidScene)
                {
                    //doesn't seem to be an issue
                    // if (queuedGroups.Peek().offset != Vector3d.zero)
                    // {
                    //     Debug.LogWarning("DEQUEUED SCENE WITH NONZERO OFFSET");
                    // }
                    return queuedGroups.Dequeue();
                }
                else
                {
                    return null;
                }
            }
            else
            {
                var group = new OffsetGroup(invalidScene, Vector3d.zero);
                // this is called twice if the editor is unfocused. seems to be a Unity bug.
                SceneManager.LoadSceneAsync(scene.buildIndex, parameters).completed += (arg) =>
                    SetupGroup(group);
                queuedGroups.Enqueue(group);
                //was originally called after CullFOObjects in SetupGroup
                return null;
            }
        }

        private void SetupGroup(OffsetGroup group)
        {
            //fixes a bizarre Unity bug where the "completed" callback from LoadSceneAsync gets called twice under certain circumstances.
            // offsetGroups.ContainsKey(SceneManager.GetSceneAt(SceneManager.sceneCount - 1)) is causing scenes to NEVER be registered!
            if (group.scene != invalidScene)
            {
                Debug.LogWarning("Prevented double execution of completed callback by SceneManager LoadSceneAsync");
                return;
            }
            Debug.Log($"setting up group {SceneManager.GetSceneAt(SceneManager.sceneCount - 1).ToHex()}");

            group.scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            CullFOObjects(group.scene);
            AddOffsetGroup(group);

        }
        /// <summary>
        /// Adds the given OffsetGroups to the tracked OffsetGroups and to the Groups hashgrid.
        /// </summary>
        /// <param name="group">
        /// The offset group being added.
        /// </param>
        private void AddOffsetGroup(OffsetGroup group)
        {
            if (offsetGroups.ContainsKey(group.scene))
            {
                Debug.LogWarning("Prevented double execution of completed callback by SceneManager LoadSceneAsync (second checkpoint)");
                return;
            }

            //EDIT: it is a bug with Unity. it happens when the Unity editor is unfocused before the game starts running
            // and before compilation is finished. the "completed" callback seems to fire twice.
            // 
            // try
            // {
            offsetGroups.Add(group.scene, group);
            // }
            // catch (Exception e)
            // {
            //     Debug.LogError($"Failed to add offset group with scene {group.scene.ToHex()} [invalid scene is {invalidScene.ToHex()}]");
            //     Debug.LogException(e);
            //     if (group.scene != invalidScene)
            //         SceneManager.UnloadSceneAsync(group.scene);
            // }

            //There should NEVER be two OffsetGroups with the same offset. By virtue of having the same offset this means the groups would be merged.
            groups.Add(group.offset, group);

            Log(
                $"Added group {group.scene.ToHex()} on grid position {groups.Quantize(group.offset)}",
                "HOUSEKEEPING"
            );
        }
        private IEnumerator OfflineUpdate()
        {
            while (!hostFullStart)
            {
                yield return new WaitForFixedUpdate();
            }
            while (Application.isPlaying)
            {
                ProcessGroupsAndViews();
                yield return new WaitForFixedUpdate();
            }
        }
        /// <summary>
        /// FOObjects shouldn't move! if you have to move an FOObject any time after spawning it, manually unregister it then
        /// register it again in the new spot!
        /// </summary>
        /// <param name="foo"></param>
        /// <param name="position"></param>
        // internal void SetPosition(FOObject foo, Vector3 position)
        // {
        //     objects.Remove(foo.realPosition);
        //     foo.transform.position = position;
        //     objects.Add(UnityToReal(position, foo.gameObject.scene), foo);
        // }
    }
}
