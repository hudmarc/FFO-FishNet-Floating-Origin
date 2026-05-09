using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// The OffsetUniverse is used to share context between FloatingOffset objects.
    /// </summary>
    public class OffsetUniverse : ScriptableObject, IOffsetSceneRegistry<Vector3, Scene>
    {
        /// <summary>
        /// Reference to this game instance's OffsetServer
        /// </summary>
        public OffsetServer<Vector3, Scene> server { internal set; get; }
        private Dictionary<Scene, IOffsetScene<Vector3, Scene>> scene_lookup = new Dictionary<Scene, IOffsetScene<Vector3, Scene>>();
        [field: SerializeField]
        public int RebaseCriteria { get; private set; } = 2048;

        [field: SerializeField]
        public float SpeedLimitMs { get; private set; } = 1000f;

        public void RegisterScene(IOffsetScene<Vector3, Scene> scene)
        {
            scene_lookup.Add(scene.GetSceneKey(), scene);
        }

        public IOffsetScene<Vector3, Scene> GetScene(Scene key)
        {
            return scene_lookup[key];
        }
    }
}
