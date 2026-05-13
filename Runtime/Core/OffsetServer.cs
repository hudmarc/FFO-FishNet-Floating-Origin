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

        private HashGrid view_grid;

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
            this.view_grid = new HashGrid(64, 64, RebaseCriteria);
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
        private int[] union_reps = new int[8]; // For Union-Find
        private int[] root_counts = new int[8];
        private Vector3d[] root_sums = new Vector3d[8];
        int[] neighborsBuffer = new int[8];

        // Key: (Union Representative, Scene), Value: Count
        private Dictionary<(int rep, TSceneKey scene), int> sceneCounts = new Dictionary<(int rep, TSceneKey scene), int>();

        // Key: Union Representative, Value: (Best Scene, Highest Count)
        private Dictionary<int, (TSceneKey scene, int count)> bestScenes = new Dictionary<int, (TSceneKey scene, int count)>();

        private void EnsureCapacity(int count)
        {
            if (view_positions.Length < count)
            {
                int newSize = Mathf.NextPowerOfTwo(count);
                Array.Resize(ref view_positions, newSize);
                Array.Resize(ref union_reps, newSize);
                Array.Resize(ref root_counts, newSize);
                Array.Resize(ref root_sums, newSize);
            }
        }

        private int Find(int i)
        {
            int root = i;
            // Find the root
            while (root != union_reps[root])
                root = union_reps[root];

            // Path compression: make all nodes on the path point directly to root
            int curr = i;
            while (curr != root)
            {
                int nxt = union_reps[curr];
                union_reps[curr] = root;
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
                union_reps[rootJ] = rootI; // Attach J's tree to I
            }
        }
        public void Process()
        {
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

            EnsureCapacity(views.Count);

            // Cache real view positions
            view_positions = new Vector3d[view_count];

            view_grid.Clear();

            for (int i = 0; i < view_count; i++)
            {
                // Get positions
                IOffsetObject<TSceneKey> view = views[i];
                Vector3d position = view_positions[i] = GetSceneOffset(view.GetSceneKey()) + view.GetEnginePosition();
                view_positions[i] = position;

                // Populate hashgrid
                view_grid.Add(position, i);

                // Initialize union-find
                union_reps[i] = i;

                // Reset caches
                root_counts[i] = 0;
                root_sums[i] = Vector3d.zero;
            }

            // Populate union-find, compute offsets for unions
            for (int i = 0; i < view_count; i++)
            {
                view_grid.FindNeighbors(view_positions[i], view_positions, ref neighborsBuffer, out int resultCount);

                for (int j = 0; j < resultCount; j++)
                {
                    int neighborIndex = neighborsBuffer[j];

                    if (neighborsBuffer[j] != i)
                    {
                        Union(i, neighborIndex);
                    }
                }
            }

            // Aggregate data for each union
            for (int i = 0; i < view_count; i++)
            {
                int root = Find(i);
                root_counts[root]++;
                root_sums[root] += view_positions[i];
            }

            sceneCounts.Clear();
            bestScenes.Clear();

            // Assign scenes to unions
            for (int i = 0; i < view_count; i++)
            {
                TSceneKey scene = views[i].GetSceneKey();
                int rep = Find(i);

                var key = (rep, scene);

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

            // Transfer all views that are not in the right scene && compute merges
            for (int i = 0; i < view_count; i++)
            {
                int rep = Find(i);
                TSceneKey current_scene = views[i].GetSceneKey();

                if (bestScenes.TryGetValue(rep, out var best))
                {
                    if (!current_scene.Equals(best.scene))
                    {
                        // transfer the view to the scene
                        TransferWithRepositioning(views[i], current_scene, best.scene);
                    }
                    if (rep == i)
                    {
                        scenes.Offset(views[i].GetSceneKey(), root_sums[i] / (double)root_counts[i]);
                    }

                }
                else
                {
                    // request new scenes for stragglers who are not part of a union or unions without assigned scenes
                    // if we find an empty scene, great! we return it.
                    // if not, we do nothing because this view will be moved to the first available scene as soon as the scene loads.
                    // the flip-side: if you a player was just interacting with a bunch of other players and then they warp-speed out
                    // they might have a frame hitch as they warp out. keep this in mind as the gamedev, or use the Teleport(view,real_position);
                    // function on the OffsetManager.
                    if (RequestScene(source, root_sums[rep] / (double)root_counts[i], out int found))
                    {
                        // transfer the view to the scene
                        TransferWithRepositioning(views[i], current_scene, scenes.GetSceneAt(found).key);
                    }

                }
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
        private void TransferWithRepositioning(IOffsetObject<TSceneKey> offsettable, TSceneKey from, TSceneKey to)
        {
            if (from.Equals(to))
                return;

            // Note the 'true'! This repositions the handler when it arrives at the target scene.
            handler.TransferTo(offsettable, from, to, true);

            // Update the logical scene registry after the transfer is complete.
            scenes.RemoveView(offsettable.GetSceneKey());
            scenes.AddView(to);

            Debug.Log($"TRANSFER: {offsettable.GetHashCode()} moved to {to.GetHashCode()} now has {scenes.GetViewCount(to)} views");
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
}