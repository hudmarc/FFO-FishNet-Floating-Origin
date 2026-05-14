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
        public OffsetServer<Scene> server { get; internal set; }
        [field: SerializeField]
        public int RebaseCriteria { get; private set; } = 2048;
        [field: SerializeField]
        public int MaxScenes { get; private set; } = 200;

        internal Queue<Scene> queued_scenes;

        // TODO: Add more user-facing functions here

        /// <summary>
        /// Teleport the given OffsetTransform view to the given position in space.
        /// </summary>
        /// <param name="offsetTransform">The offset transform to teleport.</param>
        /// <param name="position">The destination where this offset transform will be teleported.</param>
        public void TeleportTo(OffsetTransform offsetTransform, Vector3d position)
        {
            if (!offsetTransform.isView)
            {
                Debug.LogError("Cannot teleport transforms if they are not views. Set isView to 'true'.");
            }
            server.TeleportTo(offsetTransform, position);
            Debug.Log($"Teleported {offsetTransform.name} to {position}");
        }
    }
}
