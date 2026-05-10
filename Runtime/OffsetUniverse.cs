using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// The OffsetUniverse is used to share context between FloatingOffset objects.
    /// </summary>
    [CreateAssetMenu(fileName = "OffsetUniverse", menuName = "FloatingOffset/OffsetUniverse", order = 1)]
    public class OffsetUniverse : ScriptableObject, IOffsetSceneRegistry<Scene>
    {
        /// <summary>
        /// Reference to this game instance's OffsetServer
        /// </summary>
        public OffsetServer<Scene> server { internal set; get; }
        private Dictionary<Scene, IOffsetScene<Scene>> scene_lookup = new Dictionary<Scene, IOffsetScene<Scene>>();
        [field: SerializeField]
        public int RebaseCriteria { get; private set; } = 2048;

        [field: SerializeField]
        public float SpeedLimitMs { get; private set; } = 1000f;

        public void RegisterScene(IOffsetScene<Scene> scene)
        {
            server.RegisterScene(scene);
            scene_lookup.Add(scene.GetSceneKey(), scene);
        }
        internal void Register(OffsetTransform transform)
        {
            Debug.Log($"Registered {transform}");
            if (transform.isView)
            {
                server.RegisterView(transform);
            }
        }
        internal void Unregister(OffsetTransform transform)
        {
            if (transform.isView)
            {
                server.UnregisterView(transform);
            }
        }
        public IOffsetScene<Scene> GetScene(Scene key)
        {
            return scene_lookup[key];
        }
    }
}
