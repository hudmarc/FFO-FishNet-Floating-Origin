using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// The offset manager bootstraps the OffsetServer. Disable it on network clients.
    /// </summary>
    public class OffsetManager : OffsetBehaviour
    {
        void OnEnable()
        {
            universe.server = new OffsetServer<Scene>(universe.RebaseCriteria, universe.SpeedLimitMs,universe.MaxScenes);
        }
        void LateUpdate()
        {
            // Debug.Log($"Frame: {Time.frameCount}");
            universe.server.Process();
        }
    }

    public struct UnitySceneComparer : IEqualityComparer<Scene>
    {
        // Evaluates the native == operator without boxing the struct
        public bool Equals(Scene x, Scene y) => x == y;

        public int GetHashCode(Scene obj) => obj.GetHashCode();
    }
}
