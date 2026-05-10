using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// The offset manager bootstraps the OffsetServer. Disable it on network clients.
    /// </summary>
    public class OffsetManager : OffsetBehaviour
    {
        void OnEnable()
        {
            universe.server = new OffsetServer<Vector3, Scene>(universe, universe.RebaseCriteria, universe.SpeedLimitMs);
        }
        void LateUpdate()
        {
            universe.server.Process();
        }
    }
}
