using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// The offset scene handles its own offset and responds to offset requests from the OffsetServer.
    /// </summary>
    [RequireComponent(typeof(Offsetter))]
    public class OffsetSceneHandler : OffsetBehaviour, IOffsetHandler<Scene>
    {
        [SerializeField]
        private Offsetter offsetter;
        private Vector3d current_offset;
        private Vector3d current_velocity;
        private readonly LoadSceneParameters parameters = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
        void Awake()
        {
            InitializeOffsetter();
            universe.server.RegisterOffsetHandler(this);
        }
        void OnDestroy()
        {
            universe.server.UnregisterOffsetHandler(this);
        }
#if UNITY_EDITOR
        protected override void Reset()
        {
            // Fires when the component is first added to a GameObject
            base.Reset();
            InitializeOffsetter();
        }

        protected override void OnValidate()
        {
            // Fires when the inspector updates. Acts as a safety net.
            base.OnValidate();
            InitializeOffsetter();
        }
#endif
        private void InitializeOffsetter()
        {
            // Only search for the asset if the field is currently empty
            if (offsetter == null)
            {
                offsetter = GetComponent<Offsetter>();
            }
        }

        public void Clone(Action<(Scene scene, float delta)> onSceneReady)
        {
            float start_time = Time.time;
            bool completed = false;
            // this is called twice if the editor is unfocused. seems to be a Unity bug.
            SceneManager.LoadSceneAsync(gameObject.scene.buildIndex, parameters).completed += (arg) => SetupScene(onSceneReady, start_time, ref completed);
        }

        private void SetupScene(Action<(Scene scene, float delta)> onSceneReady, float start_time, ref bool completed)
        {
            //fixes a bizarre Unity bug where the "completed" callback from LoadSceneAsync gets called twice under certain circumstances.
            // offsetGroups.ContainsKey(SceneManager.GetSceneAt(SceneManager.sceneCount - 1)) is causing scenes to NEVER be registered!
            if (completed)
            {
                Debug.LogWarning("Prevented double execution of completed callback by SceneManager LoadSceneAsync");
                return;
            }
            completed = true;
            Debug.Log($"setting up scene {SceneManager.GetSceneAt(SceneManager.sceneCount - 1).handle.ToHex()}");

            Scene scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);

            onSceneReady?.Invoke((scene, Time.time - start_time));

            CullFOObjects(scene);
        }

        private void CullFOObjects(Scene scene)
        {
            Debug.Log($"Culling objects from scene {scene.handle.ToHex()}");
            var objects = scene.GetRootGameObjects();

            foreach (GameObject g in objects)
            {
                OffsetTransform obj = g.GetComponent<OffsetTransform>();

                if (obj != null)
                {
                    obj.gameObject.SetActive(false);
                    Destroy(obj.gameObject);
                }
            }
        }

        public void UpdateOffset(OffsetScene<Scene> scene, float delta = 0)
        {
            Debug.Log($"Offset from {current_offset} to {scene.offset}");
            Vector3d old_offset = current_offset;
            current_offset = scene.offset;
            current_velocity = scene.velocity;
            offsetter.Offset(old_offset, current_offset);
        }
        public void MoveAllTo(OffsetScene<Scene> scene)
        {
            var objects = gameObject.scene.GetRootGameObjects();
            foreach (GameObject gob in objects)
            {
                var offset_transform = gob.GetComponent<OffsetTransform>();
                if (offset_transform != null)
                    MoveTo(offset_transform, scene);
            }
        }
        /// <summary>
        /// Transfer the given offsettable to the given offset scene. Removes it from the offset scene this was called on.
        /// </summary>
        /// <param name="offsettable"></param>
        /// <param name="scene"></param>
        public void MoveTo(IOffsetObject<Scene> offsettable, OffsetScene<Scene> scene)
        {
            offsettable.MoveTo(scene.key);
            Debug.Log($"Transferred {((MonoBehaviour)offsettable).name} from {gameObject.scene.handle.ToHex()} to {scene.key.handle.ToHex()}");
        }
        public void RegisterOffsettable(IOffsettable offsettable)
        {
            offsetter.RegisterOffsettable(offsettable);
        }

        public void UnregisterOffsettable(IOffsettable offsettable)
        {
            offsetter.UnregisterOffsettable(offsettable);
        }

        public Scene GetSceneKey()
        {
            return gameObject.scene;
        }
        /// <summary>
        /// Helper method to get the offest of an offset scene.
        /// </summary>
        /// <returns></returns>
        public Vector3d GetOffset()
        {
            return universe.server.GetSceneOffset(gameObject.scene);
        }
    }
}
