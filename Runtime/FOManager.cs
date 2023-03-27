using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.FloatingOrigin.Types;
using UnityEngine;
using FishNet.Managing.Timing;
using UnityEngine.SceneManagement;

namespace FishNet.FloatingOrigin
{
    public partial class FOManager : MonoBehaviour
    {
        public const double chunkSize = 1024;
        public const double inverseChunkSize = 1d / chunkSize;
        public static FOManager instance;
        public FOObserver localObserver;
        public event Action<Scene> RebasedScene;
        private HashSet<FOObserver> observers = new HashSet<FOObserver>();

        #region hash grids
        protected Dictionary<Scene, FOGroup> FOGroups = new Dictionary<Scene, FOGroup>();
        protected Dictionary<Vector3Int, HashSet<FOObject>> FOHashGrid = new Dictionary<Vector3Int, HashSet<FOObject>>();

        #endregion
        void Start()
        {
            InstanceFinder.ClientManager.RegisterBroadcast<OffsetSyncBroadcast>(OnOffsetSyncBroadcast);
            InstanceFinder.TimeManager.OnTick += OnTick;
            GetComponent<TimeManager>().SetPhysicsMode(PhysicsMode.Disabled);
            Physics.autoSimulation = false;
            SetPhysicsMode(PhysicsMode);
            nullScene = SceneManager.CreateScene("Floating Origin Null Scene");
            ioffsetter = GetComponent<IOffsetter>();
        }

        public Scene GetNullScene() => nullScene;
        void OnDisable()
        {
            InstanceFinder.ClientManager.UnregisterBroadcast<OffsetSyncBroadcast>(OnOffsetSyncBroadcast);
            InstanceFinder.TimeManager.OnTick -= OnTick;
        }
        public void RegisterFOObject(FOObject foobject)
        {
            if (!FOHashGrid.ContainsKey(foobject.gridPosition))
                FOHashGrid.Add(foobject.gridPosition, new HashSet<FOObject>());
            FOHashGrid[foobject.gridPosition].Add(foobject);
            // if (!offsets.ContainsKey(foobject.gameObject.scene))
            //     offsets.Add(foobject.gameObject.scene, Vector3d.zero);
        }
        public void UnregisterFOObject(FOObject foobject)
        {
            if (FOHashGrid.ContainsKey(foobject.gridPosition))
                FOHashGrid[foobject.gridPosition].Add(foobject);
            if(foobject is FOObserver)
                observers.Remove((FOObserver)foobject);
        }
        public void RegisterFOObserver(FOObserver foobserver)
        {
            if (InstanceFinder.NetworkManager == null)
                throw new System.Exception("Instance Finder is not yet initialized! Do not register FOObjects before the InstanceFinder is fully initialized!");

            if (foobserver.IsOwner)
            {
                localObserver = foobserver;
                observers.Add(foobserver);
            }
            //this seems kinda hacky ngl
            if (!FOGroups.ContainsKey(foobserver.gameObject.scene))
                FOGroups.Add(foobserver.gameObject.scene, new FOGroup());

            if (InstanceFinder.IsClientOnly && !foobserver.IsOwner)
                return;

            observers.Add(foobserver);

            if (InstanceFinder.IsServer)
            {
                RebuildOffsetGroup(foobserver, RealToGridPosition(Mathd.toVector3d(foobserver.unityPosition)), Mathd.toVector3d(foobserver.unityPosition));
            }
            if (FOGroups.ContainsKey(foobserver.gameObject.scene))
                FOGroups[foobserver.gameObject.scene] = FOGroups[foobserver.gameObject.scene].AddMember();
        }
        internal void RebuildOffsetGroup(FOObserver first, Vector3Int gridPos, Vector3d newPosition)
        {
            //Remove from old hash grid cell
            if (FOHashGrid.ContainsKey(first.gridPosition))
            {
                FOHashGrid[first.gridPosition].Remove(first);
                //this means there are NO FOObservers or FOObjects in this cell. This cell is pretty damn empty.
                if (FOHashGrid[first.gridPosition].Count < 1)
                {
                    FOHashGrid.Remove(first.gridPosition);
                }
            }
            //Add to new hash grid cell
            if (!FOHashGrid.ContainsKey(gridPos))
                FOHashGrid.Add(gridPos, new HashSet<FOObject>());

            FOHashGrid[gridPos].Add(first);
            // GridCellEnabled(gridPos, true, first.gameObject.scene);
            List<FOObserver> observers = new List<FOObserver>();
            observers.Add(first);
            // create list of adjacent grid cells
            // is anyone in the adjacent grid cells who isn't in my group?
            // if yes, rebuild offset group for them and move to appropriate scene
            FindAdjacentAndGroup(first, gridPos, true, observers);

            //average out the offset
            Vector3d averageOffset = AverageOffset(observers);
            // Debug.Log(averageOffset.ToString());

            //if none are found, we are the only person in the new scene so create a new scene, unless we were the only ones in our previous scene, so we just keep it.
            if (observers.Count <= 1 && FOGroups[first.gameObject.scene].members > 1)
            {
                MoveToNewGroup(first, averageOffset);
            }
            else
            {
                //difference between first.groupOffset and averageOffset is zero?? why??
                OffsetScene(first.gameObject.scene, first.groupOffset, averageOffset);
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
            Debug.Log($"Grid cell {cell} enabled {enable} for scene {scene.handle} ");
            if (enable)
                foreach (var foobject in FOHashGrid[cell])
                {
                    if (foobject.enabled == false)
                    {
                        MoveToScene(foobject, scene, null);
                        foobject.realPosition = foobject.overrideRealPosition;
                        foobject.overrideRealPosition = Vector3d.zero;
                        foobject.gameObject.SetActive(true);
                    }

                }
            else
            {
                //this is called on the incorrect hash cell it seems
                foreach (var foobject in FOHashGrid[cell])
                {
                    if (foobject.enabled == true)
                    {
                        MoveToNullScene(foobject);
                        foobject.overrideRealPosition = foobject.realPosition;
                        foobject.unityPosition = Vector3.zero;
                        foobject.gameObject.SetActive(false);
                    }
                }
                Debug.Break();
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
                        if (foobject.sceneHandle != first.sceneHandle && !foobject.busy)
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
        private Vector3Int _grid_position;
        private void OnTick()
        {
            if (!InstanceFinder.IsServer)
                return;

            foreach (var observer in observers)
            {
                if (observer.gameObject == null)
                    continue;

                _grid_position = ObserverGridPosition(observer);

                if (_grid_position != observer.gridPosition && !observer.busy)
                {
                    // Debug.Log("Can Rebuild");
                    RebuildOffsetGroup(observer, _grid_position, observer.realPosition);
                    observer.gridPosition = _grid_position;
                    return; //Only update one changed observer per tick!
                }
            }

        }
        public void Awake()
        {
            instance = this;
        }
    }
}
