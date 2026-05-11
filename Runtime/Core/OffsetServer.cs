using System.Collections.Generic;
using FloatingOffset.Runtime.Types;
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
        private Dictionary<TSceneKey, (Vector3d position, double count)> positions_summed = new Dictionary<TSceneKey, (Vector3d position, double count)>();

        /// <summary>
        /// This is the minimum distance a view must be from the origin before it triggers a rebase.
        /// </summary>
        private readonly int RebaseCriteria;
        private readonly int RebaseCriteriaSquared;

        /// <summary>
        /// This is the minimum distance between two scenes before they merge.
        /// </summary>
        private readonly int MergeCriteria;
        private readonly int MergeCriteriaSquared;

        /// <summary>
        /// This is the minimum distance a view must be from the scene it was in before it is moved to another scene.
        /// </summary>
        private readonly int TransferCriteria;
        private readonly int TransferCriteriaSquared;

        private readonly int MaxScenes;

        public IOffsetHandler<TSceneKey> handler { get; private set; }

        // Passing the base values as parameters with default values gives you the 
        // exact same out-of-the-box behavior, but allows for injection later if needed.
        public OffsetServer(IOffsetHandler<TSceneKey> handler, int baseRebaseCriteria = 2048, int maxScenes = 200)
        {
            RebaseCriteria = baseRebaseCriteria;
            RebaseCriteriaSquared = RebaseCriteria * RebaseCriteria;

            MergeCriteria = RebaseCriteria * 2;
            MergeCriteriaSquared = MergeCriteria * MergeCriteria;

            TransferCriteria = MergeCriteria * 2;
            TransferCriteriaSquared = TransferCriteria * TransferCriteria;

            MaxScenes = maxScenes;

            this.handler = handler;
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
        /// Gets the pending action state of the given offsettable.
        /// </summary>
        /// <param name="offsettable"></param>
        /// <returns></returns>
        public OffsetActions GetState(IOffsetObject<TSceneKey> offsettable)
        {
            if (pending_actions.ContainsKey(offsettable))
            {
                return pending_actions[offsettable];
            }
            return OffsetActions.None;
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
                            RequestScene(view, view.GetRealPosition()); //MissingReferenceException: The object of type 'OffsetTransform' has been destroyed but you are still trying to access it. Your script should either check if it is null or you should not destroy the object.
                            break;

                        case OffsetActions.RemoveView:
                            scenes.RemoveView(views[i]); //Cannot modify the return value of 'List<OffsetScene<TSceneKey>>.this[int]' because it is not a variable
                            views[i] = null;
                            break;

                        default:
                            break;
                    }
                    Debug.Log($"Processed action for {view.GetHashCode().ToHex()}");
                    continue;
                }

                // 2) What marks you as pending transfer?
                // If you are not in the bounds of the offset scene you are supposed to be in.
                // 2)a)
                float distance_squared = view.GetEnginePositionSquareMagnitude();

                var key = views[i].GetSceneKey();
                if (GetSceneViewCount(key) > 1 && distance_squared > TransferCriteriaSquared)
                {
                    Debug.Log($"Added {view.GetHashCode()} to pending transfers");
                    pending_actions.Add(view, OffsetActions.PendingTransfer);
                }
                // Otherwise, if you are less than the transfer criteria away but more than the rebase criteria this triggers a rebase of your scene.
                else if (distance_squared > RebaseCriteriaSquared)
                {
                    // Debug.Log($"Processing offset for view {view.GetHashCode()} in scene {key}");
                    if (positions_summed.ContainsKey(key))
                    {
                        positions_summed[key] = (positions_summed[key].position + view.GetEnginePosition(), positions_summed[key].count + 1);
                    }
                    else
                    {
                        positions_summed.Add(key, (view.GetEnginePosition(), 1));
                    }
                }
            }
            // Debug.Log($"Active scenes: {scenes.ActiveCount}");
            for (int i = 0; i < scenes.Count; i++)
            {
                // ignore/continue if scene is empty or otherwise not ready to process
                if (scenes.GetViewCountAt(i) < 1)
                {
                    // Debug.Log($"Skipping {i}");
                    continue;
                }

                var key = scenes.GetKeyAt(i);

                // Debug.Log($"Processing scene {key.GetHashCode().ToHex()}");

                // if sum and count includes the offset of the scene being processed...
                if (positions_summed.ContainsKey(key))
                {
                    Vector3d average = positions_summed[key].position / positions_summed[key].count;
                    // Debug.Log($"Offsetting {key} by {average}");
                    Vector3d offset = scenes.GetOffsetAt(i) + average;
                    handler.ApplyOffset(scenes.OffsetAt(i, offset));
                    positions_summed.Remove(key);
                }

                // 3) What causes offset scenes to merge?
                // If their origins are within bounds of one another, the scene with the most views absorbs the one with less views.
                // O(n^2) but the assumption is that there will not be more than 100 scenes active at once per runtime ()
                if (scenes.Capacity > 1 && i + 1 < scenes.Count && scenes.IsValid(i))
                {
                    Vector3d offset = scenes.GetOffsetAt(i);
                    for (int j = i + 1; j < scenes.Count; j++)
                    {
                        if ((offset - scenes.GetOffsetAt(j)).sqrMagnitude < MergeCriteriaSquared && scenes.IsValid(j) && scenes.SameLayer(i, j))
                        {
                            if (scenes.GetViewCountAt(i) > scenes.GetViewCountAt(j))
                            {
                                // i survives, j is destroyed and swapped
                                MergeBintoA(i, j);
                                j--;
                            }
                            else
                            {
                                // j survives, i is destroyed and swapped
                                MergeBintoA(j, i);

                                // Step back so the outer loop evaluates the new scene swapped into slot i
                                i--;
                                // Escape the inner loop since the scene we were testing (i) is gone
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Transfers the given offsettable to the given scene.
        /// </summary>
        /// <param name="offsettable"></param>
        /// <param name="handler"></param>
        private void TransferWithRepositioning(IOffsetObject<TSceneKey> offsettable, OffsetScene<TSceneKey> from, OffsetScene<TSceneKey> to)
        {
            if (from.Equals(to))
                return;

            // Note the 'true'! This repositions the handler when it arrives at the target scene.
            handler.TransferTo(offsettable, from, to, true);

            // Update the logical scene registry after the transfer is complete.
            scenes.RemoveView(offsettable);
            scenes.AddView(to.key);

            Debug.Log($"TRANSFER: {offsettable.GetHashCode()} moved to {to.GetHashCode()} now has {to.view_count} views");
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


            // Rebase scene B to match scene A
            var scene_b = scenes.OffsetAt(b, a_offset);
            handler.ApplyOffset(scene_b);

            // Move the contents of scene B into scene A. Need to call GetSceneAt again to get updated offset.
            handler.TransferAllTo(scene_b, scenes.GetSceneAt(a));
            scenes.AddViewsAt(a, scene_b.view_count);

            // Add scene B to the empty scenes
            scenes.SetEmpty(b);
        }

        /// <summary>
        /// Fetch a scene from the empty scenes and set its position.
        /// </summary>
        /// <param name="offsettable"></param>
        /// <param name="offset"></param>
        private void RequestScene(IOffsetObject<TSceneKey> offsettable, Vector3d offset)
        {
            // 1. Check for empty scenes
            if (scenes.TryPopEmpty(out int empty_index))
            {
                Debug.LogWarning("Popped empty");

                var empty_scene = scenes.OffsetAt(empty_index, offset);
                handler.ApplyOffset(empty_scene);

                //Handles adding the view to the destination scene
                TransferWithRepositioning(offsettable, scenes.GetScene(offsettable.GetSceneKey()), empty_scene);

                pending_actions.Remove(offsettable);
            }
            else
            {
                pending_actions.Remove(offsettable);
                // 2. Prevent infinite clone crashing
                if (scenes.Capacity >= MaxScenes)
                {
                    Debug.LogError("Exceeded maximum number of active Offset Scenes!");
                    return;
                }
                pending_actions.Add(offsettable, OffsetActions.AwaitingScene);

                handler.Clone(offsettable.GetSceneKey(), result =>
                {
                    pending_actions.Remove(offsettable);
                });
            }
        }
        /// <summary>
        /// Is the given scene tracked by this offset server?
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public bool HasScene(TSceneKey scene)
        {
            return scenes.HasScene(scene);
        }
        /// <summary>
        /// Register a scene with this offset server.
        /// </summary>
        /// <param name="scene"></param>
        internal void RegisterScene(TSceneKey scene)
        {
            scenes.Register(scene);
        }
    }
}