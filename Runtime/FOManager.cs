using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.FloatingOrigin.Types;
using UnityEngine;
using FishNet.Managing.Timing;

namespace FishNet.FloatingOrigin
{
    public partial class FOManager : MonoBehaviour
    {
        public const double chunkSize = 8192;
        public const double inverseChunkSize = 1d / chunkSize;
        public static FOManager instance;
        public FOObserver localObserver;
        /// <summary>
        /// Called whenever this client has been rebased. Returns new offset.
        /// </summary>
        public Action<Vector3d> Rebased;
        internal List<FOObserver> observers = new List<FOObserver>();
        private bool initial = true;
        private Dictionary<Vector3Int, List<FOObserver>> observerGridPositions = new Dictionary<Vector3Int, List<FOObserver>>();
        private HashSet<FOObserver> tempMembers = new HashSet<FOObserver>();
        private Vector3Int gridPos;
        void Awake()
        {
            if (instance != null)
                throw new System.Exception("There is more than one FloatingOriginManager present! Please insure there is only ever one present per runtime!");
            instance = this;

            if (!(offsetter is IOffsetter))
                throw new System.Exception("Provided Offsetter does not implement interface IOffsetter!");
            ioffsetter = (IOffsetter)offsetter;
        }
        void Start()
        {
            InstanceFinder.ClientManager.RegisterBroadcast<OffsetSyncBroadcast>(OnOffsetSyncBroadcast);
            InstanceFinder.TimeManager.OnTick += OnTick;
            GetComponent<TimeManager>().SetPhysicsMode(PhysicsMode.Disabled);
            Physics.autoSimulation = false;
            SetPhysicsMode(PhysicsMode);
        }
        void OnDisable()
        {
            InstanceFinder.ClientManager.UnregisterBroadcast<OffsetSyncBroadcast>(OnOffsetSyncBroadcast);
            InstanceFinder.TimeManager.OnTick -= OnTick;
        }
        internal void RegisterObserver(FOObserver observer)
        {
            Debug.Log($"Registered Observer {observer.OwnerId}");
            observers.Add(observer);

            if (InstanceFinder.IsServer)
            {
                var offset = Mathd.toVector3d(observer.unityPosition);
                if (initial && InstanceFinder.IsServer)
                {
                    OffsetScene(observer.gameObject.scene, Vector3d.zero, offset);
                    MoveToScene(observer, new FOGroup(offset, 1), observer.gameObject.scene);
                    SetSceneObservers(observer.gameObject.scene, 1);
                    initial = false;
                }
                else
                {
                    if (InstanceFinder.IsServer)
                    {
                        scenes[observer.gameObject.scene]++;
                        MoveToNewGroup(observer, Vector3d.zero, new FOGroup(offset, 1));
                        SyncOffset(observer, offset);
                    }
                }
            }
            else
                MoveToScene(observer, new FOGroup(Vector3d.zero, 1), observer.gameObject.scene);
        }
        private bool hasRebuilt = false;
        private void OnTick()
        {
            if (!InstanceFinder.IsServer)
                return;

            if (observerGridPositions.Count != 0)
                observerGridPositions.Clear();

            hasRebuilt = false;

            foreach (var observer in observers)
            {
                if (observer == null || observer.group == null || hasRebuilt)
                    continue;

                gridPos = ObserverGridPosition(observer);

                if (observer.lastGrid != gridPos)
                {
                    if (observerGridPositions.Count == 0)

                        foreach (var ob in observers)
                            if (ob != null && !ob.busy)
                            {
                                var gp = ObserverGridPosition(ob);
                                if (observerGridPositions.ContainsKey(gp))
                                    observerGridPositions[gp].Add(ob);
                                else
                                    observerGridPositions.Add(gp, new List<FOObserver>() { ob });
                                ob.lastGrid = gp;
                            }

                    if (!observer.busy)
                    {
                        RebuildOffsetGroup(observer, gridPos, ref tempMembers, out FOObserver other);//runs synchronously
                        hasRebuilt = true;

                        if (tempMembers.Count > 1)
                            AssignGroup(other, tempMembers.ToArray());//first other observer's group (it will probably be bigger than yours)
                        else
                            AssignGroup(observer, tempMembers.ToArray());
                    }


                }
            }
        }

        private void RebuildOffsetGroup(FOObserver observer, Vector3Int gridPos, ref HashSet<FOObserver> tempMembers, out FOObserver other)
        {
            tempMembers.Clear();

            // Debug.Log($"Rebuilt Offset Group for {observer.OwnerId} at {gridPos.ToString()}");

            Vector3Int[] adjacencyGroup = AdjacentCellGroup(gridPos);
            int groupHandle = Time.time.GetHashCode();
            observer.groupHandle = groupHandle;
            tempMembers.Add(observer);
            other = null;
            foreach (Vector3Int cell in adjacencyGroup)
            {
                if (observerGridPositions.ContainsKey(cell))
                {
                    foreach (FOObserver ob in observerGridPositions[cell])
                    {
                        // Debug.Log($"{ob.OwnerId} group handle {groupHandle} observer handle {ob.groupHandle} is busy? {ob.busy}");
                        if (ob != observer && ob.groupHandle != groupHandle && !ob.busy)
                        {
                            // Debug.Log($"Found other: {ob.OwnerId}");
                            if (other == null)
                                other = ob;
                            FindAdjacent(ob, tempMembers, groupHandle, 0);//yum! recursion!
                        }
                    }
                }
            }
            // Debug.Log("---");
        }
        internal void FindAdjacent(FOObserver observer, HashSet<FOObserver> members, int groupHandle, int recursionDepth = 0)
        {
            if (recursionDepth > 4096)//If this is a problem, remove it or increase it. If your game ends up with 4096 players in a grid square you'll probably have other problems first though.
                throw new System.Exception("Max recursion depth exceeded!");

            members.Add(observer);
            observer.groupHandle = groupHandle;
            observer.busy = true;

            Vector3Int[] adjacencyGroup = AdjacentCellGroup(ObserverGridPosition(observer));

            foreach (Vector3Int cell in adjacencyGroup)
            {
                if (observerGridPositions.ContainsKey(cell))
                {
                    foreach (FOObserver ob in observerGridPositions[cell])
                    {
                        if (ob != observer && ob.group != observer.group && !ob.busy)
                        {
                            FindAdjacent(ob, members, groupHandle, ++recursionDepth);//yum! recursion!
                        }
                    }
                }
            }
        }
        private void AssignGroup(FOObserver head, FOObserver[] members, Vector3d newOffset = default)
        {
            if (members.Length == 0)
                members = new FOObserver[] { head };
            Vector3d oldOffset = head.group.offset;

            if (newOffset.Equals(Vector3d.zero))
            {
                newOffset = AverageOffset(members);
            }

            if (members.Length > 1)
            {
                OffsetScene(head.gameObject.scene, oldOffset, newOffset);
                head.group.offset = newOffset;
                foreach (FOObserver observer in members)
                {
                    if (observer != head)//Ignore the head since it was already offset by the global scene offset
                        MoveToAndOffset(observer, head);
                }
                head.busy = false;
            }
            else
            {
                if (head.group.members > 1)
                    MoveToNewGroup(head, oldOffset, new FOGroup(newOffset, members.Length));
                else //We were the only member in our old scene, we are the only member in our new scene.
                {
                    OffsetScene(head.gameObject.scene, oldOffset, newOffset);
                    head.group.offset = newOffset;
                    head.busy = false;
                }
            }

            head.group.members = members.Length;

            foreach (FOObserver observer in members)
                SyncOffset(observer, newOffset);
        }
    }
}
