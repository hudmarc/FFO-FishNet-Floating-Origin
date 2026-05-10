using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// The OffsetUniverse is used to share context between FloatingOffset objects.
    /// </summary>
    [CreateAssetMenu(fileName = "OffsetUniverse", menuName = "FloatingOffset/OffsetUniverse", order = 1)]
    public class OffsetUniverse : ScriptableObject
    {
        /// <summary>
        /// Reference to this game instance's OffsetServer
        /// </summary>
        public OffsetServer<Scene> server { internal set; get; }
        [field: SerializeField]
        public int RebaseCriteria { get; private set; } = 2048;

        [field: SerializeField]
        public float SpeedLimitMs { get; private set; } = 1000f;
    }
}
