using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// Offset Anchors ensure that the object they are attached to is always at the exact position specified in the OffsetAnchor's target position.<br/>
    /// This also means that they may exist in more than one scene at a time on the server.
    /// </summary>
    public class OffsetAnchor : OffsetBehaviour, IOffsettable<Scene>
    {
        [SerializeField]
        private Vector3d realPosition;
        private Scene scene = default;
        void Start()
        {
            universe.server.handler.RegisterOffsettable(this, gameObject.scene);
            scene = gameObject.scene;
        }
        public void OnOffset(Vector3d old_offset, Vector3d new_offset)
        {
            transform.position = Mathd.RealToUnity(realPosition, new_offset);
        }

        public Scene GetSceneKey()
        {
            return scene;
        }
    }
}
