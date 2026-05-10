using System;
using System.Collections.Generic;
using UnityEngine;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// The OffsetServer manages all offset calculations for the FloatingOffset package. This class should exist as a single instance.
    /// In a multiplayer environment, the OffsetServer will not be active on clients.
    /// </summary>
    public class OffsetServer<TVector, TSceneKey>
    {
        /// <summary>
        /// Quickly iterable list of views. An OffsetTransform is here <==> it is considered a view.
        /// </summary>
        private List<IOffsetObject<TVector, TSceneKey>> views = new List<IOffsetObject<TVector, TSceneKey>>();
        /// <summary>
        /// 1)
        /// If a offsettable is marked as 'PendingTransfer', that means it will be transferred asap to:<br/>
        /// a) The closest scene to its origin,<br/>
        /// b) If no scenes are close to its origin, an empty scene will be moved to it,<br/>
        /// c) If there are no empty scenes, a new scene will be queued and the offsettable will be moved there.
        /// 
        /// 2)
        /// If a view is marked as 'RemoveView', it will be removed from the view list and turned back into a plain offset offsettable (for example, when a player dies)
        /// </summary>
        private Dictionary<IOffsetObject<TVector, TSceneKey>, OffsetActions> pending_actions = new Dictionary<IOffsetObject<TVector, TSceneKey>, OffsetActions>();
        /// <summary>
        /// Fast lookup to find the OffsetScene related to a given scene key.
        /// </summary>

        /// <summary>
        /// Tracks all OffsetScenes
        /// </summary>
        private List<IOffsetScene<TVector, TSceneKey>> scenes = new List<IOffsetScene<TVector, TSceneKey>>();
        /// <summary>
        /// Empty scenes.
        /// </summary>
        private Queue<IOffsetScene<TVector, TSceneKey>> empty_scenes = new Queue<IOffsetScene<TVector, TSceneKey>>();

        /// <summary>
        /// This is the minimum distance a view must be from the origin before it triggers a rebase.
        /// </summary>
        public readonly int RebaseCriteria;
        public readonly int RebaseCriteriaSquared;

        /// <summary>
        /// This is the minimum distance between two scenes before they merge.
        /// </summary>
        public readonly int MergeCriteria;
        public readonly int MergeCriteriaSquared;

        /// <summary>
        /// This is the minimum distance a view must be from the scene it was in before it is moved to another scene.
        /// </summary>
        public readonly int TransferCriteria;
        public readonly int TransferCriteriaSquared;

        public readonly float SpeedLimitMs;
        public readonly float SpeedLimitMsSquared;

        private readonly IOffsetSceneRegistry<TVector, TSceneKey> registry;

        // Passing the base values as parameters with default values gives you the 
        // exact same out-of-the-box behavior, but allows for injection later if needed.
        public OffsetServer(IOffsetSceneRegistry<TVector, TSceneKey> registry, int baseRebaseCriteria = 2048, float speedLimitMs = 1000f)
        {
            RebaseCriteria = baseRebaseCriteria;
            RebaseCriteriaSquared = RebaseCriteria * RebaseCriteria;

            MergeCriteria = RebaseCriteria * 2;
            MergeCriteriaSquared = MergeCriteria * MergeCriteria;

            TransferCriteria = MergeCriteria * 2;
            TransferCriteriaSquared = TransferCriteria * TransferCriteria;

            SpeedLimitMs = speedLimitMs;
            SpeedLimitMsSquared = SpeedLimitMs * SpeedLimitMs;

            this.registry = registry;
        }

        /// <summary>
        /// Gets the offset for the given scene.
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public Vector3d GetOffset(TSceneKey scene)
        {
            return registry.GetScene(scene).GetOffset();
        }
        /// <summary>
        /// Gets the velocity for the given scene.
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public Vector3d GetVelocity(TSceneKey scene)
        {
            return registry.GetScene(scene).GetVelocity();
        }
        /// <summary>
        /// Registers the given offset offsettable as a view.
        /// </summary>
        /// <param name="offsettable"></param>
        internal void RegisterView(IOffsetObject<TVector, TSceneKey> offsettable)
        {
            views.Add(offsettable);
        }
        /// <summary>
        /// Downgrades the given view to a transform
        /// </summary>
        /// <param name="offsettable"></param>
        internal void UnregisterView(IOffsetObject<TVector, TSceneKey> offsettable)
        {
            pending_actions.Add(offsettable, OffsetActions.RemoveView);
        }
        internal void Process()
        {
            for (int i = 0; i < views.Count; i++)
            {
                IOffsetObject<TVector, TSceneKey> view = views[i];

                // Swap-and-pop
                if (view == null)
                {
                    int lastIndex = views.Count - 1;
                    views[i] = views[lastIndex];
                    views.RemoveAt(lastIndex);
                    i--;
                    continue;
                }

                // 1) What merges you into an offset scene?
                // If you are within render distance of another offset offsettable view in a different offset scene, 
                // OR marked as pending transfer from an offset scene other than the one you are currently in.

                // 1)a) Handles case where an offset scene is moved to you
                if (pending_actions.ContainsKey(view))
                {
                    switch (pending_actions[view])
                    {
                        case OffsetActions.PendingTransfer:
                            RequestScene(view, view.GetRealPosition(), view.GetRealVelocity());
                            pending_actions.Remove(view);
                            break;

                        case OffsetActions.RemoveView:
                            views[i] = null;
                            break;
                    }
                }

                // 2) What marks you as pending transfer?
                // If you are not in the bounds of the offset scene you are supposed to be in.
                // 2)a)
                float distance_squared = view.GetEnginePositionSquareMagnitude();
                IOffsetScene<TVector, TSceneKey> scene = registry.GetScene(view.GetSceneKey());

                if (scene.ViewCount() > 1 && distance_squared > TransferCriteriaSquared)
                {
                    Debug.Log($"Aded {view.GetHashCode()} to pending transfers");
                    pending_actions.Add(view, OffsetActions.PendingTransfer);
                }
                // Otherwise, if you are less than the transfer criteria away but more than the rebase criteria this triggers a rebase of your scene.
                else if (distance_squared > RebaseCriteriaSquared)
                {
                    Debug.Log($"Processing offset for view {view.GetHashCode()} in scene {scene.GetHashCode()}");
                    scene.ProcessOffsets();
                }
                // 2)b)
                if (view.EngineVelocitySquaredMagnitude() > SpeedLimitMsSquared)
                {
                    pending_actions.Add(view, OffsetActions.PendingTransfer);
                }
            }

            for (int i = 0; i < scenes.Count; i++)
            {
                IOffsetScene<TVector, TSceneKey> scene = scenes[i];

                // ignore/continue if scene is empty or otherwise not ready to process
                if (scene.IsEmpty())
                    continue;

                // 3) What causes offset scenes to merge?
                // If their origins are within bounds of one another, the scene with the most views absorbs the one with less views.
                // O(n^2) but the assumption is that there will not be more than 100 scenes active at once per runtime ()
                for (int j = i; j < scenes.Count; j++)
                {
                    IOffsetScene<TVector, TSceneKey> other = scenes[j];
                    if ((scene.GetOffset() - other.GetOffset()).sqrMagnitude < MergeCriteriaSquared)
                    {
                        if (scene.ViewCount() > other.ViewCount())
                        {
                            MergeBintoA(scene, other);
                        }
                        else
                        {
                            MergeBintoA(other, scene);
                        }
                    }
                }


            }
        }
        /// <summary>
        /// Transfers the given offsettable to the given scene.
        /// </summary>
        /// <param name="offsettable"></param>
        /// <param name="scene"></param>
        private void Transfer(IOffsetObject<TVector, TSceneKey> offsettable, IOffsetScene<TVector, TSceneKey> scene)
        {
            IOffsetScene<TVector, TSceneKey> source = registry.GetScene(offsettable.GetSceneKey());
            source.TransferTo(offsettable, registry.GetScene(scene.GetSceneKey()));
        }
        /// <summary>
        /// Moves contents of scene b into scene a.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        private void MergeBintoA(IOffsetScene<TVector, TSceneKey> a, IOffsetScene<TVector, TSceneKey> b)
        {
            Vector3d a_offset = a.GetOffset();
            Vector3d b_offset = b.GetOffset();
            Vector3d average_offset = (a_offset + b_offset) * 0.5;

            // Rebase scene B to the average of A and B
            b.SetOffset(average_offset);

            // Rebase scene A to the average of A and B
            a.SetOffset(average_offset);

            // Move the contents of scene B into scene A
            b.TransferAllTo(registry.GetScene(a.GetSceneKey()));

            // Add scene B to the empty scenes
            b.SetEmpty(true);
            empty_scenes.Enqueue(b);
        }

        /// <summary>
        /// Fetch a scene from the empty scenes and set its position and velocity, or create a scene from the given scene at position + (velocity * time delta)
        /// </summary>
        /// <param name="offsettable"></param>
        /// <param name="position"></param>
        /// <param name="velocity"></param>
        private void RequestScene(IOffsetObject<TVector, TSceneKey> offsettable, Vector3d position, Vector3d velocity)
        {
            if (empty_scenes.Count > 0)
            {
                // 4) What causes an empty offset scene to rebase?
                // If it is assigned to an OffsetView pending transfer that is not already bounded by an offset scene,
                // When assigned, it is removed from the empty scene stack.

                // 4) a) 
                IOffsetScene<TVector, TSceneKey> scene = empty_scenes.Dequeue();
                scene.SetOffset(position, velocity);
                Transfer(offsettable, scene);
            }
            else
            {
                // 5) What causes new offset scenes to spawn?
                // If there are still offset views in the pending queue
                // and no available empty scenes in the empty scene stack.
                IOffsetScene<TVector, TSceneKey> source = registry.GetScene(offsettable.GetSceneKey());
                source.Clone(result =>
                {
                    scenes.Add(result.scene);
                    result.scene.SetOffset(position, velocity, result.delta);
                    Transfer(offsettable, result.scene);
                });
            }
        }
    }
    public interface IOffsetObject<TVector, TScene>
    {
        public Vector3d GetRealPosition();
        public TVector GetEnginePosition();
        public Vector3d GetRealVelocity();
        public TVector GetEngineVelocity();
        public TScene GetSceneKey();
        public void MoveTo(TScene scene);
        public float EngineVelocitySquaredMagnitude();
        public float GetEnginePositionSquareMagnitude();
    }
    public interface IOffsettable
    {
        public void OnOffset(Vector3d old_offset, Vector3d new_offset);
    }
    public interface IOffsetScene<TVector, TSceneKey>
    {
        public TSceneKey GetSceneKey();
        public Vector3d GetOffset();
        public Vector3d GetVelocity();
        public int ViewCount();
        public bool IsEmpty();
        public void SetEmpty(bool empty);
        public void SetOffset(Vector3d offset, Vector3d velocity, float delta);
        public void SetOffset(Vector3d offset, Vector3d velocity);
        public void SetOffset(Vector3d offset);
        public void TransferTo(IOffsetObject<TVector, TSceneKey> offsettable, IOffsetScene<TVector, TSceneKey> scene);
        public void TransferAllTo(IOffsetScene<TVector, TSceneKey> scene);
        public void Clone(Action<(IOffsetScene<TVector, TSceneKey> scene, float delta)> onSceneReady);
        public void RegisterTransform(OffsetTransform offsetTransform);
        public void UnregisterTransform(OffsetTransform offsetTransform);
        public void RegisterOffsettable(IOffsettable offsettable);
        public void UnregisterOffsettable(IOffsettable offsettable);
        public void ProcessOffsets();
    }
    public interface IOffsetSceneRegistry<TVector, TSceneKey>
    {
        public void RegisterScene(IOffsetScene<TVector, TSceneKey> scene);
        public IOffsetScene<TVector, TSceneKey> GetScene(TSceneKey key);
    }
    public enum OffsetActions
    {
        RemoveView,
        PendingTransfer
    }
}