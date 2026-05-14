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
        private int[] view_scene_indexes = new int[8];
        private int[] union_reps = new int[8]; // For Union-Find
        private int[] union_counts = new int[8];
        private Vector3d[] union_sums = new Vector3d[8];
        int[] neighborsBuffer = new int[8];
        // Tracks the count of views for a specific (root, scene) combination
        Dictionary<(int root, TSceneKey scene), int> members = new Dictionary<(int, TSceneKey), int>();

        // Tracks the current winning scene for a given root
        // Key: root | Value: (Winning Scene, Max Count, Representative View Index)
        Dictionary<int, (int scene_index, int count, int winner_index)> winners = new Dictionary<int, (int, int, int)>();
        private void EnsureCapacity(int count)
        {
            if (view_positions.Length < count)
            {
                int newSize = Mathf.NextPowerOfTwo(count);
                Array.Resize(ref view_positions, newSize);
                Array.Resize(ref view_scene_indexes, newSize);
                Array.Resize(ref union_reps, newSize);
                Array.Resize(ref union_counts, newSize);
                Array.Resize(ref union_sums, newSize);
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
                view_scene_indexes[i] = scenes.IndexOf(view.GetSceneKey());
                Vector3d position = view_positions[i] = GetSceneOffset(scenes.GetKeyAt(view_scene_indexes[i])) + view.GetEnginePosition();
                view_positions[i] = position;

                // Populate hashgrid
                view_grid.Add(position, i);

                // Initialize union-find
                union_reps[i] = i;

                // Reset caches
                union_counts[i] = 0;
                union_sums[i] = Vector3d.zero;
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
                int rep = Find(i);
                union_counts[rep]++;
                union_sums[rep] += view_positions[i];
            }


            winners.Clear();
            if (view_count == 0) return;

            // 1. Gather and Sort
            (int scene_index, int union_rep)[] sorted = new (int scene_index, int union_rep)[view_count];
            for (int i = 0; i < view_count; i++)
            {
                sorted[i] = (view_scene_indexes[i], Find(i));
            }
            Array.Sort(sorted); // Sorts by Scene, then by Union

            // 2. Initialize State Tracking OUTSIDE the loop
            int current_scene = sorted[0].scene_index;
            int current_union = sorted[0].union_rep;
            int current_run_count = 1;

            int scene_champion_union = current_union;
            int scene_champion_count = 1;

            // 3. Scan and Reduce
            for (int i = 1; i < sorted.Length; i++)
            {
                var item = sorted[i];

                if (item.scene_index == current_scene && item.union_rep == current_union)
                {
                    // Still looking at the same union in the same scene
                    current_run_count++;
                }
                else
                {
                    // The union changed OR the scene changed. 
                    // First, see if the run that just finished beats the current scene champion.
                    if (current_run_count > scene_champion_count)
                    {
                        scene_champion_union = current_union;
                        scene_champion_count = current_run_count;
                    }

                    // If the SCENE changed, the battle for the previous scene is officially over.
                    if (item.scene_index != current_scene)
                    {
                        // Lock in the winner for the old scene
                        winners[scene_champion_union] = (current_scene, scene_champion_count, scene_champion_union);

                        // Reset champion tracking for the brand new scene
                        current_scene = item.scene_index;
                        scene_champion_union = item.union_rep;
                        scene_champion_count = 0;
                    }

                    // Reset the run tracking for the new union
                    current_union = item.union_rep;
                    current_run_count = 1;
                }
            }

            // 4. Resolve the Tail
            // Evaluate the final run that was active when the loop ended
            if (current_run_count > scene_champion_count)
            {
                scene_champion_union = current_union;
                scene_champion_count = current_run_count;
            }
            // Lock in the final scene
            winners[scene_champion_union] = (current_scene, scene_champion_count, scene_champion_union);




            TSceneKey source = scenes.GetSceneAt(0).key;

            // Transfer all views that are not in the right scene && compute merges
            for (int i = 0; i < view_count; i++)
            {
                Debug.Log($"--- View {i} in scene {views[i].GetSceneKey()} @ pos {view_positions[i]}---");
                int rep = Find(i);

                if (winners.TryGetValue(rep, out var winner))
                {
                    var scene = scenes.GetKeyAt(winner.scene_index);
                    Debug.Log($"found scene @ {scene}");
                    if (!view_scene_indexes[i].Equals(winner.scene_index))
                    {
                        Debug.Log($"going to transfer from {views[i].GetSceneKey()} to {winner.scene_index}");

                        // transfer the view to the scene
                        TransferWithRepositioning(views[i], views[i].GetSceneKey(), scene);
                    }
                    if (winner.winner_index == i)
                    {
                        Debug.Log($"leader {winner.winner_index}");
                        scenes.Offset(scene, union_sums[rep] / (double)union_counts[rep]);
                    }
                }
                else
                {
                    Debug.Log("tried to request a scene");
                    // request new scenes for stragglers who are not part of a union or unions without assigned scenes
                    // if we find an empty scene, great! we return it.
                    // if not, we do nothing because this view will be moved to the first available scene as soon as the scene loads.
                    // the flip-side: if you a player was just interacting with a bunch of other players and then they warp-speed out
                    // they might have a frame hitch as they warp out. keep this in mind as the gamedev, or use the Teleport(view,real_position);
                    // function on the OffsetManager.
                    if (RequestScene(source, union_sums[rep] / (double)union_counts[rep], out int found))
                    {
                        // transfer the view to the scene
                        TransferWithRepositioning(views[i], views[i].GetSceneKey(), scenes.GetSceneAt(found).key);
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
            Debug.Log("Requested scene");
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
                    handler.Clone(source, scene =>
                    {
                        Debug.Log("Loaded another scene");
                        scenes.Register(scene);
                    });
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