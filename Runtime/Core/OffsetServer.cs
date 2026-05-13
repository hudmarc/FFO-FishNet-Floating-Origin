using System;
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
        private HashSet<IOffsetObject<TSceneKey>> views_to_remove = new HashSet<IOffsetObject<TSceneKey>>();

        /// <summary>
        /// Tracks all OffsetScenes in one fast array.
        /// </summary>
        private OffsetSceneCollection<TSceneKey> scenes = new OffsetSceneCollection<TSceneKey>();
        /// <summary>
        /// The sum of positions on the given scene, if applicable.
        /// </summary>
        private Dictionary<TSceneKey, (Vector3d position, double count)> positions_summed = new Dictionary<TSceneKey, (Vector3d position, double count)>();

        private readonly int MinimumJoinDistance = 5000;
        private readonly int MinimumJoinDistanceSquared = 5000 * 5000;

        private readonly int MinimumLeaveDistance = 6000;
        private readonly int MinimumLeaveDistanceSquared = 6000 * 6000;

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

        private HashGrid<int> view_grid;

        // Passing the base values as parameters with default values gives you the 
        // exact same out-of-the-box behavior, but allows for injection later if needed.
        public OffsetServer(IOffsetHandler<TSceneKey> handler, int RebaseCriteria = 2048, int MaxScenes = 200)
        {
            this.RebaseCriteria = RebaseCriteria;
            this.RebaseCriteriaSquared = RebaseCriteria * RebaseCriteria;

            this.MergeCriteria = RebaseCriteria * 2;
            this.MergeCriteriaSquared = MergeCriteria * MergeCriteria;

            this.TransferCriteria = MergeCriteria * 2;
            this.TransferCriteriaSquared = TransferCriteria * TransferCriteria;

            this.MaxScenes = MaxScenes;

            this.handler = handler;
            this.view_grid = new HashGrid<int>(RebaseCriteria);
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
        /// <param name="view"></param>
        public void RegisterView(IOffsetObject<TSceneKey> view)
        {
            if (scenes.Count < 1)
                scenes.Register(view.GetSceneKey());

            scenes.AddView(view.GetSceneKey());
            views.Add(view);
        }
        /// <summary>
        /// Downgrades the given view to a transform
        /// </summary>
        /// <param name="offsettable"></param>
        public void UnregisterView(IOffsetObject<TSceneKey> offsettable)
        {
            scenes.RemoveView(offsettable.GetSceneKey());
            views_to_remove.Add(offsettable);
        }

        private Vector3d[] view_positions = new Vector3d[8];
        private int[] parent = new int[8]; // For Union-Find
        private int[] root_counts = new int[8];
        private Vector3d[] root_sums = new Vector3d[8];

        private void EnsureCapacity(int count)
        {
            if (view_positions.Length < count)
            {
                int newSize = Mathf.NextPowerOfTwo(count);
                Array.Resize(ref view_positions, newSize);
                Array.Resize(ref parent, newSize);
                Array.Resize(ref root_counts, newSize);
                Array.Resize(ref root_sums, newSize);
            }
        }

        private int Find(int i)
        {
            int root = i;
            // Find the root
            while (root != parent[root])
                root = parent[root];

            // Path compression: make all nodes on the path point directly to root
            int curr = i;
            while (curr != root)
            {
                int nxt = parent[curr];
                parent[curr] = root;
                curr = nxt;
            }
            return root;
        }

        private void Union(int i, int j)
        {
            int rootI = Find(i);
            int rootJ = Find(j);
            if (rootI != rootJ)
            {
                parent[rootJ] = rootI; // Attach J's tree to I
            }
        }
        public void Process()
        {
            EnsureCapacity(views.Count);
            // prune views scheduled for removal
            for (int i = 0; i < views.Count; i++)
            {
                if (views_to_remove.Contains(views[i]))
                {
                    int lastIndex = views.Count - 1;
                    views[i] = views[lastIndex];
                    views.RemoveAt(lastIndex);
                    i--;
                    continue;
                }
            }

            int view_count = views.Count;

            // Cache for real view positions
            Vector3d[] view_positions = new Vector3d[view_count];

            view_grid.Clear();

            // Populate hashgrid
            for (int i = 0; i < view_count; i++)
            {
                IOffsetObject<TSceneKey> view = views[i];
                Vector3d position = view_positions[i] = GetSceneOffset(view.GetSceneKey()) + view.GetEnginePosition();

                view_grid.Add(position, i);
            }

            Member[] unions = new Member[view_count];

            // Initialize union-find
            for (int i = 0; i < view_count; i++)
            {
                unions[i] = new()
                {
                    representative = i,
                    summed_offsets = view_positions[i],
                    count = 1
                };
            }

            int union_count = view_count;

            // Populate union-find, compute offsets for unions
            for (int search_index = 0; search_index < view_count; search_index++)
            {
                if (unions[search_index].representative != search_index)
                    continue; //skip members that are not represented by themselves

                int[] found_result = view_grid.FindInBoundingBox(view_positions[search_index], MinimumJoinDistance);

                if (found_result.Length > 1)
                {
                    for (int found_index = 0; found_index < found_result.Length; found_index++)
                    {
                        if (found_result[found_index] != search_index)
                        {
                            //Mutate the parent of the union
                            unions[search_index] = new()
                            {
                                representative = search_index,
                                summed_offsets = unions[search_index].summed_offsets + unions[found_index].summed_offsets,
                                count = unions[search_index].count + unions[found_index].count
                            };

                            // subordinate found index to search index (apparently this overwrites some data?)
                            unions[found_result[found_index]] = new()
                            {
                                representative = search_index,
                                summed_offsets = Vector3d.zero,
                                count = 0
                            };

                            found_index--;
                            union_count--;
                            continue;
                        }
                    }
                }
            }


            // Initialize scene assignments
            // Key: (Union Representative, Scene), Value: Count
            var sceneCounts = new Dictionary<(int rep, TSceneKey scene), int>();

            // Key: Union Representative, Value: (Best Scene, Highest Count)
            var bestScenes = new Dictionary<int, (TSceneKey scene, int count)>();

            // assign scenes to unions
            for (int i = 0; i < view_count; i++)
            {
                TSceneKey scene = views[i].GetSceneKey();
                int rep = unions[i].representative;

                (int rep, TSceneKey scene) key = (rep, scene);

                // Increment the count for this specific scene within this specific union
                sceneCounts.TryGetValue(key, out int currentCount);
                currentCount++;
                sceneCounts[key] = currentCount;

                // Check if this newly incremented scene is now the winner for this union
                if (!bestScenes.TryGetValue(rep, out var currentBest) || currentCount > currentBest.count)
                {
                    // Update the winner for this representative
                    bestScenes[rep] = (scene, currentCount);
                }
            }

            TSceneKey source = scenes.GetSceneAt(0).key;

            bool[] valid_scene = new bool[view_count];

            // transfer all views that are not in the right scene && compute merges
            for (int i = 0; i < view_count; i++)
            {
                if (bestScenes.ContainsKey(unions[i].representative))
                {
                    if (views[i].GetSceneKey().Equals(bestScenes[unions[i].representative].scene))
                    {
                        // do nothing, this view is already in the correct scene
                        valid_scene[i] = true;
                        continue;
                    }
                    valid_scene[i] = true;
                    // transfer the view to the scene
                    TransferWithRepositioning(views[i], scenes.GetScene(views[i].GetSceneKey()), scenes.GetScene(bestScenes[unions[i].representative].scene));
                }
                else
                {
                    // request new scenes for stragglers who are not part of a union or unions without assigned scenes
                    // if we find an empty scene, great! we return it.
                    // if not, we do nothing because this view will be moved to the first available scene as soon as the scene loads.
                    // the flip-side: if you a player was just interacting with a bunch of other players and then they warp-speed out
                    // they might have a frame hitch as they warp out. keep this in mind as the gamedev, or use the Teleport(view,real_position);
                    // function on the OffsetManager.
                    if (RequestScene(source, unions[i].summed_offsets / (double)unions[i].count, out int found))
                    {
                        valid_scene[i] = true;
                        // transfer the view to the scene
                        TransferWithRepositioning(views[i], scenes.GetScene(views[i].GetSceneKey()), scenes.GetSceneAt(found));
                    }

                }
            }

            // set new scene offsets. if we are overwriting the value, no big deal.
            // all views where valid_scene[i]==true are in the correct scene already,
            // so views[i].GetSceneKey() will return the actual scenes the views are in.
            for (int i = 0; i < view_count; i++)
            {
                if (unions[i].representative == i && valid_scene[i])
                    scenes.Offset(views[i].GetSceneKey(), unions[i].summed_offsets / (double)unions[i].count);
            }

            // rebase all scenes whose actual offset does not match their expected offset
            for (int i = 0; i < scenes.Count; i++)
            {
                // updateOffset does nothing if the offset is already updated.
                handler.UpdateOffset(scenes.GetSceneAt(i));
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
            scenes.RemoveView(offsettable.GetSceneKey());
            scenes.AddView(to.key);

            Debug.Log($"TRANSFER: {offsettable.GetHashCode()} moved to {to.GetHashCode()} now has {to.view_count} views");
        }

        /// <summary>
        /// Fetch a scene from the empty scenes and set its position.
        /// </summary>
        /// <param name="offsettable"></param>
        /// <param name="offset"></param>
        private bool RequestScene(TSceneKey source, Vector3d offset, out int found_scene)
        {
            // 1. Check for empty scenes
            if (scenes.TryPopEmpty(out int empty_index))
            {
                Debug.LogWarning("Popped empty");

                OffsetScene<TSceneKey> empty_scene = scenes.OffsetAt(empty_index, offset);
                handler.UpdateOffset(empty_scene);
                found_scene = empty_index;
                return true;
            }
            else
            {
                found_scene = -1;
                // 2. Prevent infinite cloning
                if (scenes.Capacity < MaxScenes)
                {
                    handler.Clone(source, null);
                }
                else
                {
                    Debug.LogError("Exceeded maximum number of active Offset Scenes!");
                }

                return false;
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
    }
    internal struct Member
    {
        /// <summary>
        /// The index of the representative member of this union.
        /// </summary>
        public int representative;
        /// <summary>
        /// The sum of offsets of this union
        /// </summary>
        public Vector3d summed_offsets;
        /// <summary>
        /// The number of members in this member's union
        /// </summary>
        public int count;
    }
}