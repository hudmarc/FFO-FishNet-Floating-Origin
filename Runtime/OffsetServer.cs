using System;
using System.Collections.Generic;
using UnityEngine;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// The OffsetServer manages all offset calculations for the FloatingOffset package. This class should exist as a single instance.
    /// In a multiplayer environment, the OffsetServer will not be active on clients.
    /// </summary>
    public class OffsetServer<TSceneKey>
    {
        /// <summary>
        /// Quickly iterable list of views. An OffsetTransform is here <==> it is considered a view.
        /// </summary>
        private List<IOffsetObject<TSceneKey>> views = new List<IOffsetObject<TSceneKey>>();
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
        private Dictionary<IOffsetObject<TSceneKey>, OffsetActions> pending_actions = new Dictionary<IOffsetObject<TSceneKey>, OffsetActions>();
        /// <summary>
        /// Fast lookup to find the OffsetScene related to a given scene key.
        /// </summary>

        /// <summary>
        /// Tracks all OffsetScenes in one fast array.
        /// </summary>
        private OffsetSceneCollection<TSceneKey> scenes = new OffsetSceneCollection<TSceneKey>();
        /// <summary>
        /// The sum of positions on the given scene, if applicable.
        /// </summary>
        private Dictionary<TSceneKey, Vector3d> positions_summed = new Dictionary<TSceneKey, Vector3d>();

        /// <summary>
        /// Empty scenes.
        /// </summary>
        private Queue<int> empty_scenes = new Queue<int>();

        // private Queue<OffsetScene<TSceneKey>> pending_scenes = 


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

        // Passing the base values as parameters with default values gives you the 
        // exact same out-of-the-box behavior, but allows for injection later if needed.
        public OffsetServer(int baseRebaseCriteria = 2048, float speedLimitMs = 1000f)
        {
            RebaseCriteria = baseRebaseCriteria;
            RebaseCriteriaSquared = RebaseCriteria * RebaseCriteria;

            MergeCriteria = RebaseCriteria * 2;
            MergeCriteriaSquared = MergeCriteria * MergeCriteria;

            TransferCriteria = MergeCriteria * 2;
            TransferCriteriaSquared = TransferCriteria * TransferCriteria;

            SpeedLimitMs = speedLimitMs;
            SpeedLimitMsSquared = SpeedLimitMs * SpeedLimitMs;
        }
        /// <summary>
        /// Gets the offset for the given scene.
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public Vector3d GetSceneOffset(TSceneKey scene)
        {
            return scenes.GetOffset(scene);
        }
        /// <summary>
        /// Gets the velocity for the given scene.
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public Vector3d GetSceneVelocity(TSceneKey scene)
        {
            return scenes.GetVelocity(scene);
        }
        /// <summary>
        /// Gets the number of views in the given scene.
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public int GetSceneViewCount(TSceneKey scene)
        {
            return scenes.GetViewCount(scene);
        }
        public IOffsetHandler<TSceneKey> GetHandler(TSceneKey scene)
        {
            return scenes.GetHandler(scene);
        }
        /// <summary>
        /// Registers the given offset offsettable as a view.
        /// </summary>
        /// <param name="offsettable"></param>
        internal void RegisterView(IOffsetObject<TSceneKey> offsettable)
        {
            scenes.AddView(offsettable);
            views.Add(offsettable);
        }
        /// <summary>
        /// Downgrades the given view to a transform
        /// </summary>
        /// <param name="offsettable"></param>
        internal void UnregisterView(IOffsetObject<TSceneKey> offsettable)
        {
            scenes.RemoveView(offsettable);
            pending_actions.TryAdd(offsettable, OffsetActions.RemoveView);
        }
        internal void Process()
        {
            for (int i = 0; i < views.Count; i++)
            {
                IOffsetObject<TSceneKey> view = views[i];

                // Swap-and-pop
                if (view.Equals(null))
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
                            RequestScene(view, view.GetRealPosition(), view.GetRealVelocity()); //MissingReferenceException: The object of type 'OffsetTransform' has been destroyed but you are still trying to access it. Your script should either check if it is null or you should not destroy the object.
                            break;

                        case OffsetActions.RemoveView:
                            scenes.RemoveView(views[i]); //Cannot modify the return value of 'List<OffsetScene<TSceneKey>>.this[int]' because it is not a variable
                            views[i] = null;
                            break;
                    }
                    continue;
                }

                // 2) What marks you as pending transfer?
                // If you are not in the bounds of the offset scene you are supposed to be in.
                // 2)a)
                float distance_squared = view.GetEnginePositionSquareMagnitude();

                var key = views[i].GetSceneKey();
                if (GetSceneViewCount(key) > 1 && distance_squared > TransferCriteriaSquared)
                {
                    Debug.Log($"Aded {view.GetHashCode()} to pending transfers");
                    pending_actions.Add(view, OffsetActions.PendingTransfer);
                }
                // Otherwise, if you are less than the transfer criteria away but more than the rebase criteria this triggers a rebase of your scene.
                else if (distance_squared > RebaseCriteriaSquared)
                {
                    Debug.Log($"Processing offset for view {view.GetHashCode()} in scene {key}");
                    if (positions_summed.ContainsKey(key))
                    {
                        positions_summed[key] += view.GetEnginePosition();
                    }
                    else
                    {
                        positions_summed.Add(key, view.GetEnginePosition());
                    }
                }
                // 2)b)
                if (view.EngineVelocitySquaredMagnitude() > SpeedLimitMsSquared)
                {
                    pending_actions.Add(view, OffsetActions.PendingTransfer);
                }
            }



            for (int i = 0; i < scenes.Count; i++)
            {
                // ignore/continue if scene is empty or otherwise not ready to process
                if (scenes.GetViewCountAt(i) < 1)
                {
                    // Debug.Log($"Skipping scene {i.ToHex()}");
                    continue;
                }

                var key = scenes.GetKeyAt(i);

                // if sum and count includes the offset of the scene being processed...
                if (positions_summed.ContainsKey(key))
                {
                    Vector3d average = positions_summed[key] / ((double)scenes.GetViewCount(key));
                    Debug.Log($"Offsetting {key} by {average}");
                    scenes.SetOffsetAt(i, scenes.GetOffsetAt(i) + average);
                    // scenes.SetVelocityAt(i, scenes.GetVelocityAt(i) + average);

                    scenes.GetHandlerAt(i).UpdateOffset(scenes.GetSceneAt(i));
                    positions_summed.Remove(key);
                }

                // 3) What causes offset scenes to merge?
                // If their origins are within bounds of one another, the scene with the most views absorbs the one with less views.
                // O(n^2) but the assumption is that there will not be more than 100 scenes active at once per runtime ()
                if (scenes.Count > 1 && i + 1 < scenes.Count)
                    for (int j = i + 1; j < scenes.Count; j++)
                    {
                        // OffsetScene<TSceneKey> other = scenes[j];

                        if ((scenes.GetOffsetAt(i) - scenes.GetOffsetAt(j)).sqrMagnitude < MergeCriteriaSquared && scenes.GetHandlerAt(i) != null && scenes.GetHandlerAt(j) != null)
                        {
                            if (scenes.GetViewCountAt(i) > scenes.GetViewCountAt(j))
                            {
                                MergeBintoA(j, i);
                            }
                            else
                            {
                                MergeBintoA(i, j);
                            }
                        }
                    }
            }
        }

        internal void RegisterOffsetHandler(IOffsetHandler<TSceneKey> handler)
        {
            scenes.Register(handler.GetSceneKey(), handler);
        }

        /// <summary>
        /// Transfers the given offsettable to the given scene.
        /// </summary>
        /// <param name="offsettable"></param>
        /// <param name="handler"></param>
        private void Transfer(IOffsetObject<TSceneKey> offsettable, IOffsetHandler<TSceneKey> handler)
        {

            bool same_scene = offsettable.GetSceneKey().Equals(handler.GetSceneKey());
            TSceneKey from = offsettable.GetSceneKey();
            TSceneKey to = handler.GetSceneKey();
            if (!same_scene)
                scenes.RemoveView(offsettable);

            scenes.AddView(to);


            Debug.Log($"TRANSFER: {offsettable.GetHashCode()} moved to {to.GetHashCode()} now has {scenes.GetViewCount(to)} views");

            if (!same_scene)
                scenes.GetHandler(from).MoveTo(offsettable, scenes.GetScene(to));

        }
        /// <summary>
        /// Moves contents of scene b into scene a.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        private void MergeBintoA(int a, int b)
        {
            Debug.Log($"Merging {b.ToHex()} into {a.ToHex()}");

            Vector3d a_offset = scenes.GetOffsetAt(a);
            Vector3d b_offset = scenes.GetOffsetAt(b);

            Vector3d average_offset = (a_offset + b_offset) * 0.5;

            // Rebase scene B to the average of A and B
            scenes.SetOffsetAt(b, average_offset);

            // Rebase scene A to the average of A and B
            scenes.SetOffsetAt(a, average_offset);

            // Move the contents of scene B into scene A
            scenes.GetHandlerAt(b).MoveAllTo(scenes.GetSceneAt(a));


            scenes.AddViewsAt(a, scenes.GetViewCountAt(b));

            // Add scene B to the empty scenes
            scenes.SetEmpty(b);

            empty_scenes.Enqueue(b);
        }

        /// <summary>
        /// Fetch a scene from the empty scenes and set its position and velocity, or create a scene from the given scene at position + (velocity * time delta)
        /// </summary>
        /// <param name="offsettable"></param>
        /// <param name="offset"></param>
        /// <param name="velocity"></param>
        private void RequestScene(IOffsetObject<TSceneKey> offsettable, Vector3d offset, Vector3d velocity)
        {
            if (empty_scenes.Count > 0)
            {
                // 4) What causes an empty offset scene to rebase?
                // If it is assigned to an OffsetView pending transfer that is not already bounded by an offset scene,
                // When assigned, it is removed from the empty scene stack.

                // 4) a) 
                if (!scenes.HasHandlerAt(empty_scenes.Peek()))
                {
                    // wait until scene has an assigned handler
                    return;
                }
                int index = empty_scenes.Dequeue();
                IOffsetHandler<TSceneKey> handler = scenes.GetHandlerAt(index);
                scenes.SetOffsetAt(index, offset);
                scenes.SetVelocityAt(index, velocity);
                handler.UpdateOffset(scenes.GetSceneAt(index));
                Transfer(offsettable, handler);
                pending_actions.Remove(offsettable);
            }
            else
            {
                // 5) What causes new offset scenes to spawn?
                // If there are still offset views in the pending queue
                // and no available empty scenes in the empty scene stack.
                IOffsetHandler<TSceneKey> source = GetHandler(offsettable.GetSceneKey());
                source.Clone(result =>
                {
                    pending_actions.Remove(offsettable);
                });
            }
        }

        internal void UnregisterOffsetHandler(IOffsetHandler<TSceneKey> handler)
        {
            scenes.Unregister(handler.GetSceneKey());
        }
    }
    /// <summary>
    /// Handles the scene and offset.
    /// </summary>
    /// <typeparam name="TSceneKey"></typeparam>
    public struct OffsetScene<TSceneKey>
    {
        public Vector3d offset;
        public Vector3d velocity;
        public int view_count;
        public TSceneKey key;
        public IOffsetHandler<TSceneKey> handler;
    }
    public interface IOffsetObject<TScene>
    {
        public Vector3d GetRealPosition();
        public Vector3d GetEnginePosition();
        public Vector3d GetRealVelocity();
        public Vector3d GetEngineVelocity();
        public TScene GetSceneKey();
        public void MoveTo(TScene scene);
        public float EngineVelocitySquaredMagnitude();
        public float GetEnginePositionSquareMagnitude();
    }
    public interface IOffsettable
    {
        public void OnOffset(Vector3d old_offset, Vector3d new_offset);
    }
    public interface IOffsetHandler<TSceneKey>
    {
        public void UpdateOffset(OffsetScene<TSceneKey> scene, float delta = 0);
        public void MoveTo(IOffsetObject<TSceneKey> offsettable, OffsetScene<TSceneKey> scene);
        public void MoveAllTo(OffsetScene<TSceneKey> scene);
        public void Clone(Action<(TSceneKey scene, float delta)> onSceneReady);
        public void RegisterOffsettable(IOffsettable offsettable);
        public void UnregisterOffsettable(IOffsettable offsettable);
        public TSceneKey GetSceneKey();
        public Vector3d GetOffset();
    }
    public enum OffsetActions
    {
        RemoveView,
        PendingTransfer
    }
}