using System;
using System.Collections.Generic;
using FishNet.FloatingOrigin.Types;
using UnityEngine;
using FishNet.Managing.Timing;
using UnityEngine.SceneManagement;
using FishNet.Connection;
namespace FishNet.FloatingOrigin
{
    // TODO cleanup comments, and also register observer is broken!
    public partial class FOManager : MonoBehaviour
    {
        public const double chunkSize = 1024;
        public const double inverseChunkSize = 1d / chunkSize;
        public static FOManager instance;
        public FOObserver localObserver;
        public event Action<Scene> SceneChanged;
        private HashSet<FOObserver> observers = new HashSet<FOObserver>();
        internal Dictionary<NetworkConnection, FOObserver> connectionObservers = new Dictionary<NetworkConnection, FOObserver>();

        #region hash grids
        protected Dictionary<Scene, FOGroup> FOGroups = new Dictionary<Scene, FOGroup>();
        /// <summary>
        /// The current implementation will probably fill this up with garbage after a while, especially if many foobjects are destroyed.
        /// </summary>
        protected Dictionary<Vector3Int, HashSet<FOObject>> FOHashGrid = new Dictionary<Vector3Int, HashSet<FOObject>>();
        #endregion
        void Start()
        {
            if (instance!=null){return;} //this should prevent the FOManager from attempting to initialize itself if it already exists
            InstanceFinder.ClientManager.RegisterBroadcast<OffsetSyncBroadcast>(OnOffsetSyncBroadcast);
            InstanceFinder.TimeManager.OnTick += OnTick;
            GetComponent<TimeManager>().SetPhysicsMode(PhysicsMode.Disabled);
            Physics.autoSimulation = false;
            SetPhysicsMode(PhysicsMode);
            ioffsetter = GetComponent<IOffsetter>();
            nullScene = SceneManager.CreateScene("Null Scene");
        }
        public Scene GetNullScene() => nullScene;
        void OnDisable()
        {
            InstanceFinder.ClientManager.UnregisterBroadcast<OffsetSyncBroadcast>(OnOffsetSyncBroadcast);
            InstanceFinder.TimeManager.OnTick -= OnTick;
        }
        public void RegisterFOObject(FOObject foobject)
        {
            var gridPos = RealToGridPosition(Mathd.toVector3d(foobject.unityPosition));
            if (!FOHashGrid.ContainsKey(gridPos))
                FOHashGrid.Add(gridPos, new HashSet<FOObject>());
            FOHashGrid[gridPos].Add(foobject);
            foobject.gridPosition = gridPos;

            if (foobject is FOObserver && localObserver != null)
            {
                connectionObservers.Add(foobject.Owner, (FOObserver)foobject);
            }
        }
        public void UnregisterFOObject(FOObject foobject)
        {
            if (FOHashGrid.ContainsKey(foobject.gridPosition))
                FOHashGrid[foobject.gridPosition].Remove(foobject);
            if (foobject is FOObserver)
                observers.Remove((FOObserver)foobject);
            if (foobject is FOObserver && localObserver != null)
            {
                connectionObservers.Remove(foobject.Owner);
            }
        }
        public void RegisterFOObserver(FOObserver foobserver)
        {
            Log($"Registered FOObserver for client {foobserver.OwnerId} is owner {foobserver.IsOwner} "); //why is IsOwner wrong on the host?
            if (InstanceFinder.NetworkManager == null)
                throw new System.Exception("Instance Finder is not yet initialized! Do not register FOObjects before the InstanceFinder is fully initialized!");

            if (InstanceFinder.IsClientOnly && !foobserver.IsOwner)
                return;

            if (localObserver == null)
                localObserver = foobserver;
            observers.Add(foobserver);

            Vector3d initialPosition = Mathd.toVector3d(foobserver.unityPosition);
            if (InstanceFinder.IsServer)
            {
                MoveToSceneFromNull(foobserver, SceneManager.GetSceneAt(1));
                Log(initialPosition.ToString());
                Log(foobserver.unityPosition.ToString());
            }
            else
            {
                if (!FOGroups.ContainsKey(foobserver.gameObject.scene))
                    FOGroups.Add(foobserver.gameObject.scene, new FOGroup());
            }

        }
        internal void RebuildOffsetGroup(FOObserver first, Vector3Int gridPos, Vector3d newPosition)
        {
            //Remove from old hash grid cell
            if (FOHashGrid.ContainsKey(first.gridPosition))
            {
                FOHashGrid[first.gridPosition].Remove(first);
                //This means there are no FOObservers or FOObjects in this cell
                if (FOHashGrid[first.gridPosition].Count < 1)
                {
                    FOHashGrid.Remove(first.gridPosition);
                }
            }
            //Add to new hash grid cell
            if (!FOHashGrid.ContainsKey(gridPos))
                FOHashGrid.Add(gridPos, new HashSet<FOObject>());

            FOHashGrid[gridPos].Add(first);
            GridCellEnabled(gridPos, true, first.gameObject.scene);
            List<FOObserver> observers = new List<FOObserver>();
            observers.Add(first);
            // create list of adjacent grid cells
            // is anyone in the adjacent grid cells who isn't in my group?
            // if yes, rebuild offset group for them and move to appropriate scene
            FindAdjacentAndGroup(first, gridPos, true, observers);

            //average out the offset
            Vector3 averageOffset = AverageOffset(observers);
            Log(averageOffset.ToString());

            //if none are found, we are the only person in the new scene so create a new scene, unless we were the only ones in our previous scene, so we just keep it.
            if (observers.Count <= 1 && FOGroups[first.gameObject.scene].members > 1)
            {
                Log($"Moving player {first.OwnerId} to new group ");
                MoveToNewGroup(first, averageOffset);
            }
            else
            {
                Log($"Offsetting scene {first.gameObject.scene.handle} from offset {first.groupOffset} to offset {averageOffset} ");
                OffsetScene(first.gameObject.scene, first.groupOffset, averageOffset);
                foreach (var observer in observers)
                {
                    EnableAdjacent(observer, observer.gridPosition, first.gameObject.scene);
                }
            }

            foreach (FOObserver observer in observers)
                SyncOffset(observer, averageOffset);
        }
        /// <summary>
        /// Automatic culling of FOObjects in unrendered scenes, also ensures no FOObjects will be destroyed when a scene is automatically unloaded.
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="enable"></param>
        /// <param name="scene"></param>
        protected void GridCellEnabled(Vector3Int cell, bool enable, Scene scene)
        {
            Log($"Grid cell {cell} enabled {enable} for scene {scene.handle} ");
            if (!FOHashGrid.ContainsKey(cell))
                return;

            if (enable)
                foreach (var foobject in FOHashGrid[cell])
                {
                    if (foobject.enabled == false && foobject.sceneHandle == scene.handle)
                    {
                        MoveFromSceneToScene(foobject, scene, null);
                        foobject.realPosition = foobject.overrideRealPosition;
                        foobject.overrideRealPosition = Vector3d.zero;
                        foobject.gameObject.SetActive(true);
                    }

                }
            else
            {
                foreach (var foobject in FOHashGrid[cell])
                {
                    if (foobject.enabled == true && foobject.sceneHandle == scene.handle)
                    {
                        MoveToNullScene(foobject);
                        foobject.overrideRealPosition = foobject.realPosition;
                        foobject.unityPosition = Vector3.zero;
                        foobject.gameObject.SetActive(false);
                    }
                }
            }


        }
        private void FindAdjacentAndGroup(FOObserver first, Vector3Int gridPos, bool initial, List<FOObserver> observers)
        {
            Vector3Int[] searchGrid = AdjacentCellGroup(gridPos);

            foreach (Vector3Int gridPosition in searchGrid)
            {
                if (FOHashGrid.ContainsKey(gridPosition))
                {
                    foreach (var foobject in FOHashGrid[gridPos])
                    {
                        if (foobject.sceneHandle != first.sceneHandle && !foobject.isBusy())
                        {
                            if (!initial)
                                MoveToOtherObserverSceneAndOffset(foobject, first);

                            if (foobject is FOObserver)
                            {
                                //if we found another object, move the first to this other object's scene, since it is likely it contains more members than ours
                                if (initial)
                                    MoveToOtherObserverSceneAndOffset(first, (FOObserver)foobject);
                                observers.Add((FOObserver)foobject);
                                FindAdjacentAndGroup((FOObserver)foobject, gridPos, false, observers);
                            }
                        }
                    }

                }
            }
        }
        private void EnableAdjacent(FOObserver observer, Vector3Int gridPos, Scene scene)
        {
            Vector3Int[] searchGrid = AdjacentCellGroup(gridPos);
            foreach (var cell in searchGrid)
            {
                GridCellEnabled(cell, true, scene);
            }

        }
        private Vector3Int _grid_position;
        public void OnTick()
        {
            if (!InstanceFinder.IsServer)
                return;

            foreach (var observer in observers)
            {
                if (observer.gameObject == null)
                    continue;

                _grid_position = ObserverGridPosition(observer);

                if (_grid_position != observer.gridPosition && !observer.isBusy())
                {
                    Log("Can Rebuild");
                    RebuildOffsetGroup(observer, _grid_position, observer.realPosition);
                    observer.gridPosition = _grid_position;

                    return; //Only update one changed observer per tick!
                }
            }

        }
        public Scene GetSceneForConnection(NetworkConnection connection)
        {
            if(!connectionObservers.ContainsKey(connection))
                return localObserver.gameObject.scene;
                
            return connectionObservers[connection].gameObject.scene;

        }
        public void Awake()
        {
            if (instance!=null){return;} //this should prevent the FOManager from attempting to initialize itself if it already exists
            instance = this;
        }
    }
}
