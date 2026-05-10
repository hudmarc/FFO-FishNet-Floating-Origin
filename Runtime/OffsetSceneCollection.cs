using System;
using System.Collections.Generic;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// State management for scenes. Wraps raw array modification calls.
    /// </summary>
    /// <typeparam name="TSceneKey"></typeparam>
    public class OffsetSceneCollection<TSceneKey>
    {
        /// <summary>
        /// Maps a scene key to its underlying index in the scenes array.
        /// </summary>
        private Dictionary<TSceneKey, int> scene_indexes = new Dictionary<TSceneKey, int>();

        /// <summary>
        /// Contiguous array of scene data for fast iteration.
        /// </summary>
        private OffsetScene<TSceneKey>[] scenes;

        public OffsetSceneCollection()
        {
            scenes = new OffsetScene<TSceneKey>[1];
        }

        private int activeCount = 0; // Tracks the boundary of populated scenes
        private int aliveCount = 0;  // Tracks the boundary of registered scenes

        private void Swap(int indexA, int indexB)
        {
            if (indexA == indexB) return;

            // Swap the structs in memory
            var temp = scenes[indexA];
            scenes[indexA] = scenes[indexB];
            scenes[indexB] = temp;

            // Sync the dictionary so lookups still point to the correct indices
            scene_indexes[scenes[indexA].key] = indexA;
            scene_indexes[scenes[indexB].key] = indexB;
        }

        /// <summary>
        /// Gets the current mathematical offset for the scene at the specified array index.
        /// </summary>
        /// <param name="scene_index">The array index of the scene.</param>
        /// <returns>The 64-bit vector representing the scene's floating origin offset.</returns>
        public Vector3d GetOffsetAt(int scene_index)
        {
            return scenes[scene_index].offset;
        }

        /// <summary>
        /// Gets the current mathematical offset for the specified scene key.
        /// </summary>
        /// <param name="scene">The unique key identifying the scene.</param>
        /// <returns>The 64-bit vector representing the scene's floating origin offset.</returns>
        public Vector3d GetOffset(TSceneKey scene)
        {
            return GetOffsetAt(scene_indexes[scene]);
        }

        /// <summary>
        /// Gets the current velocity of the scene at the specified array index.
        /// </summary>
        /// <param name="scene_index">The array index of the scene.</param>
        /// <returns>The 64-bit vector representing the scene's velocity.</returns>
        public Vector3d GetVelocityAt(int scene_index)
        {
            return scenes[scene_index].velocity;
        }

        /// <summary>
        /// Gets the current velocity of the specified scene key.
        /// </summary>
        /// <param name="scene">The unique key identifying the scene.</param>
        /// <returns>The 64-bit vector representing the scene's velocity.</returns>
        public Vector3d GetVelocity(TSceneKey scene)
        {
            return GetVelocityAt(scene_indexes[scene]);
        }

        /// <summary>
        /// Gets the total number of registered views (e.g., players, cameras) currently in the scene at the specified array index.
        /// </summary>
        /// <param name="scene_index">The array index of the scene.</param>
        /// <returns>The number of active views.</returns>
        public int GetViewCountAt(int scene_index)
        {
            if (scene_index >= scenes.Length)
            {
                return 0;
            }
            return scenes[scene_index].view_count;
        }

        /// <summary>
        /// Gets the total number of registered views (e.g., players, cameras) currently in the specified scene.
        /// </summary>
        /// <param name="scene">The unique key identifying the scene.</param>
        /// <returns>The number of active views.</returns>
        public int GetViewCount(TSceneKey scene)
        {
            return GetViewCountAt(scene_indexes[scene]);
        }

        /// <summary>
        /// Retrieves the component responsible for shifting the physics and root transforms of the scene at the specified array index.
        /// </summary>
        /// <param name="scene_index">The array index of the scene.</param>
        /// <returns>The scene's offset handler.</returns>
        public IOffsetHandler<TSceneKey> GetHandlerAt(int scene_index)
        {
            return scenes[scene_index].handler;
        }

        /// <summary>
        /// Retrieves the component responsible for shifting the physics and root transforms of the specified scene.
        /// </summary>
        /// <param name="scene">The unique key identifying the scene.</param>
        /// <returns>The scene's offset handler.</returns>
        public IOffsetHandler<TSceneKey> GetHandler(TSceneKey scene)
        {
            if (!scene_indexes.ContainsKey(scene))
                return null;
            return GetHandlerAt(scene_indexes[scene]);
        }

        /// <summary>
        /// Decrements the view count for the scene at the specified array index.
        /// </summary>
        /// <param name="scene_index">The array index of the scene.</param>
        public void RemoveViewAt(int scene_index)
        {
            scenes[scene_index].view_count--;
            
            if (scenes[scene_index].view_count < 1)
            {
                SetEmpty(scene_index);
            }
        }

        /// <summary>
        /// Removes a tracked view from its associated scene, decrementing that scene's view count.
        /// </summary>
        /// <param name="view">The offset object being removed.</param>
        public void RemoveView(IOffsetObject<TSceneKey> view)
        {
            if (scene_indexes.ContainsKey(view.GetSceneKey()))
            {
                RemoveViewAt(scene_indexes[view.GetSceneKey()]);
            }
        }

        /// <summary>
        /// Increments the view count for the scene at the specified array index.
        /// </summary>
        /// <param name="scene_index">The array index of the scene.</param>
        private void AddViewAt(int scene_index)
        {
            scenes[scene_index].view_count++;
        }

        /// <summary>
        /// Registers a tracked view to its associated scene, incrementing that scene's view count.
        /// </summary>
        /// <param name="view">The offset object being added.</param>
        public void AddView(IOffsetObject<TSceneKey> view)
        {
            AddViewAt(scene_indexes[view.GetSceneKey()]);
        }

        public void AddView(TSceneKey scene)
        {
            AddViewAt(scene_indexes[scene]);
        }

        public TSceneKey GetKeyAt(int scene_index)
        {
            return scenes[scene_index].key;
        }

        public void Register(TSceneKey sceneKey, IOffsetHandler<TSceneKey> handler)
        {
            // If our alive boundary hits the end of the array, we are out of space.
            if (aliveCount >= scenes.Length)
            {
                Array.Resize(ref scenes, scenes.Length * 2);
            }

            int newIndex = aliveCount;

            OffsetScene<TSceneKey> scene = new OffsetScene<TSceneKey>();
            scene.handler = handler;
            scene.key = sceneKey;
            scene.view_count = 0; // Starts in the 'Empty' zone

            scenes[newIndex] = scene;
            scene_indexes[sceneKey] = newIndex;

            aliveCount++;
        }
        public void Unregister(TSceneKey scene)
        {
            if (!scene_indexes.TryGetValue(scene, out int index)) return;

            // 1. If it is in the Active zone, downgrade it to the Empty zone
            if (index < activeCount)
            {
                activeCount--;
                Swap(index, activeCount);
                index = activeCount; // Update our local index reference
            }

            // 2. Now that it is in the Empty zone, downgrade it to the Dead zone
            aliveCount--;
            Swap(index, aliveCount);

            // 3. Clear the data and erase from dictionary
            scenes[aliveCount].handler = null;
            scene_indexes.Remove(scene);
        }
        public bool HasHandlerAt(int scene_index)
        {
            return scenes[scene_index].handler != null;
        }
        public bool HasScene(TSceneKey scene)
        {
            return scene_indexes.ContainsKey(scene);
        }
        public void SetOffsetAt(int index, Vector3d offset)
        {
            scenes[index].offset = offset;
        }
        public void SetOffset(TSceneKey key, Vector3d offset)
        {
            scenes[scene_indexes[key]].offset = offset;
        }

        public void SetVelocityAt(int index, Vector3d velocity)
        {
            scenes[index].velocity = velocity;
        }

        public OffsetScene<TSceneKey> GetSceneAt(int index)
        {
            return scenes[index];
        }
        public OffsetScene<TSceneKey> GetScene(TSceneKey key)
        {
            return scenes[scene_indexes[key]];
        }

        public void SetEmpty(int scene_index)
        {
            scenes[scene_index].view_count = 0;

            // If the scene is currently inside the Active zone, move it to the Empty zone
            if (scene_index < activeCount)
            {
                activeCount--;
                Swap(scene_index, activeCount);
            }
        }

        /// <summary>
        /// Attempts to recycle a scene from the Empty Zone. 
        /// Returns true and outputs the index if successful.
        /// </summary>
        public bool TryPopEmpty(out int recycledIndex)
        {
            // The Empty Zone exists strictly between the Active boundary and the Alive boundary.
            // If activeCount == aliveCount, the Empty Zone is completely empty.
            if (activeCount < aliveCount)
            {
                // 1. Grab the scene sitting exactly on the active boundary
                recycledIndex = activeCount;

                // 2. Expand the Active zone to officially pull this scene inside it
                activeCount++;
                return true;
            }

            // No empty scenes exist. The server must create a new one.
            recycledIndex = -1;
            return false;
        }

        internal void AddViewsAt(int scene_index, int count)
        {
            scenes[scene_index].view_count += count;
        }

        /// <summary>
        /// The current capacity of the underlying scenes array.
        /// </summary>
        public int Count { get => scenes.Length; }
        public int ActiveCount { get => activeCount; }
        public int EmptyCount { get => EmptyCount; }
    }

}
