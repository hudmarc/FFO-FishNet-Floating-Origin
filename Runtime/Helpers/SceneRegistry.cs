using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// The SceneRegistry provides methods to assist with finding components in scenes.
    /// </summary>
    public class SceneRegistry
    {
        private readonly Dictionary<(Scene, Type), Component> scenedComponents = new();
        private readonly Dictionary<(Scene, Type), (int count, int index)> cache = new();
    
        /// <summary>
        /// Gets the given component from the ScenedComponents dictionary.
        /// </summary>
        /// <typeparam name="T">The type of component to search for.</typeparam>
        /// <param name="scene">The scene the component should be in.</param>
        /// <returns>The component, if found. Null otherwise.</returns>
        public T GetScened<T>(Scene scene) where T : Component
        {
            (Scene, Type) key = (scene, typeof(T));

            if (scenedComponents.ContainsKey(key))
                return (T)scenedComponents[key];

            return null;
        }
    
        /// <summary>
        /// Checks if 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scene"></param>
        /// <returns></returns>
        public bool HasScened<T>(Scene scene) where T : Component
        {
            return scenedComponents.ContainsKey((scene, typeof(T)));
        }
    
        /// <summary>
        /// Adds the given scened component to the SceneRegistry. If the component has already been added, nothing will happen.
        /// </summary>
        /// <param name="component">The component to add.</param>
        public void AddScened(Component component)
        {
            (Scene, Type) key = (component.gameObject.scene, component.GetType());

            if (scenedComponents.ContainsKey(key))
                return;

            scenedComponents.Add(key, component);
        }
    
        /// <summary>
        /// Returns the index of the first GameObject with the component of the given type in the given Scene. Returns -1 if a GameObject with the corresponding component could not be found.
        /// </summary>
        /// <typeparam name="T">The type of object to search for.</typeparam>
        /// <param name="scene">The scene to search for an object with the given component in.</param>
        /// <returns>The index of the first GameObject in the target scene with a component of the given type, -1 if nothing was found.</returns>
        public int FindFirstIndex<T>(GameObject[] gameObjects)
        {
            for (int i = 0; i < gameObjects.Length; i++)
            {
                if (gameObjects[i].GetComponent<T>() != null)
                {
                    return i;
                }
            }
            return -1;
        }
    
        /// <summary>
        /// Returns the first found component of the given type in the given scene.
        /// </summary>
        /// <typeparam name="T">The type to search for.</typeparam>
        /// <param name="scene">The scene to search for the component in.</param>
        /// <returns>The component that was found.</returns>
        public T FindFirst<T>(Scene scene)
        {
            GameObject[] gameObjects = scene.GetRootGameObjects();
            return gameObjects[FindFirstIndex<T>(gameObjects)].GetComponent<T>();
        }
    
        /// <summary>
        /// Returns the first found component of the given type in the given scene. Uses a cache of 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scene"></param>
        /// <returns></returns>
        public T FindFirstCached<T>(Scene scene)
        {
            (Scene, Type) key = (scene, typeof(T));
            //Check if cache exists
            if (!cache.ContainsKey(key))
            {
                GameObject[] gameObjects = scene.GetRootGameObjects();
                return gameObjects[Recache<T>(scene, gameObjects)].GetComponent<T>();
            }
            else
            {
                GameObject[] gameObjects = scene.GetRootGameObjects();
                if (cache[key].count != scene.rootCount)
                    return gameObjects[Recache<T>(scene, gameObjects)].GetComponent<T>();
                if (gameObjects[cache[key].index].TryGetComponent(out T component))
                    return component;
                else
                    return gameObjects[Recache<T>(scene, gameObjects)].GetComponent<T>();
            }
        }

        private int Recache<T>(Scene scene, GameObject[] gameObjects)
        {
            int found = FindFirstIndex<T>(gameObjects);
            cache[(scene, typeof(T))] = (scene.rootCount, found);
            return found;
        }
    }
}
