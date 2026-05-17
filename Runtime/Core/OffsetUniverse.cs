using System;
using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// The OffsetUniverse is used to interact with the OffsetServer in Unity.
    /// </summary>
    [CreateAssetMenu(fileName = "OffsetUniverse", menuName = "FloatingOffset/OffsetUniverse", order = 1)]
    public class OffsetUniverse : ScriptableObject
    {
        /// <summary>
        /// Reference to this game instance's OffsetServer
        /// </summary>
        private OffsetServer<Scene> server;

        [field: SerializeField]
        public int MinimumJoinDistance { get; private set; } = 1000;
        [field: SerializeField]
        public int Hysteresis { get; private set; } = 1000;
        [field: SerializeField]
        public int MaxScenes { get; private set; } = 200;

        public bool Active => server != null;
        [NonSerialized]
        //main view on the server
        internal IOffsetObject<Scene> mainView = null;
        [NonSerialized]

        public Action<OffsetTransform> onTransformRegistered = null;

        public void InitializeServer(IOffsetHandler<Scene> handler)
        {
            server = new OffsetServer<Scene>(handler, MinimumJoinDistance, MaxScenes, Hysteresis);
        }

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

        internal void Process()
        {
            server.Process();
        }

        internal void RegisterView(OffsetTransform offsetTransform)
        {
            if (Active)
            {
                server.RegisterView(offsetTransform);
                if (mainView == null)
                    mainView = offsetTransform;
            }
            onTransformRegistered?.Invoke(offsetTransform);
        }

        internal void UnregisterView(OffsetTransform offsetTransform)
        {
            if (Active)
                server.UnregisterView(offsetTransform);
        }

        public Vector3d GetSceneOffset(Scene scene)
        {
            if (Active)
                return server.GetSceneOffset(scene);
            return Vector3d.zero;
        }

        internal void RegisterOffsettable(OffsetAnchor offsetAnchor, Scene scene)
        {
            server.handler.RegisterOffsettable(offsetAnchor, scene);
        }

        public bool HasScene(Scene scene)
        {
            return server.HasScene(scene);
        }
    }
}
