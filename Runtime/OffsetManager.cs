using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine;
using System;
using FloatingOffset.Runtime.Types;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// The offset manager bootstraps the OffsetServer. Disable it on network clients.
    /// </summary>
    [RequireComponent(typeof(Offsetter))]
    public class OffsetManager : OffsetBehaviour, IOffsetHandler<Scene>
    {
        private readonly LoadSceneParameters parameters = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
        [SerializeField]
        private Offsetter offsetter;
        private Dictionary<Scene, Vector3d> offsets = new Dictionary<Scene, Vector3d>();
        private Dictionary<Scene, Vector3d> velocities = new Dictionary<Scene, Vector3d>();
        private Dictionary<Scene, List<IOffsettable<Scene>>> offsettables = new Dictionary<Scene, List<IOffsettable<Scene>>>();

        private void Awake()
        {
            if (enabled)
                universe.server = new OffsetServer<Scene>(this, universe.RebaseCriteria, universe.MaxScenes);
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
        void LateUpdate()
        {
            universe.server.Process();
        }
        /// <summary>
        /// Clone the given scene. Calls the callback when done.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="onSceneReady"></param>
        public void Clone(Scene scene, Action<Scene> onSceneReady)
        {
            float start_time = Time.time;
            bool completed = false;
            // this is called twice if the editor is unfocused. seems to be a Unity bug.
            SceneManager.LoadSceneAsync(scene.buildIndex, parameters).completed += (arg) => SetupScene(onSceneReady, start_time, ref completed);
        }
        // Runs some setup code on the scene and calls the callback.
        private void SetupScene(Action<Scene> onSceneReady, float start_time, ref bool completed)
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

            onSceneReady?.Invoke(scene);

            CullFOObjects(scene);
        }
        // culls scened FOObjects from any scenes that are duplicates of an existing scene.
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
        /// <summary>
        /// Transfer the given offsettable to the given offset scene. Removes it from the offset scene this was called on.<br>
        /// Offsets the transform so that it matches the offset of the target scene.
        /// </summary>
        /// <param name="offsetTransform"></param>
        /// <param name="scene"></param>
        public void TransferTo(IOffsetObject<Scene> offsetTransform, OffsetScene<Scene> from, OffsetScene<Scene> to, bool reposition = false)
        {
            if (!offsetTransform.IsView() && (to.offset - (from.offset + offsetTransform.GetEnginePosition())).sqrMagnitude > universe.RebaseCriteria * universe.RebaseCriteria)
            {
                MonoBehaviour off = (MonoBehaviour)offsetTransform;
                Debug.Log($"Destroyed out of range Offset Transform {off.name}");
                Destroy(off.gameObject);
                return;
            }

            Transform trf = ((OffsetBehaviour)offsetTransform).transform;

            Vector3d absoluteRealPos = from.offset + offsetTransform.GetEnginePosition();

            SceneManager.MoveGameObjectToScene(trf.gameObject, to.key);

            // Calculate the exact local Unity position required for the new scene
            // Because Real = Unity + Offset, therefore Unity = Real - Offset
            if (reposition)
            {
                Vector3d newUnityPos = absoluteRealPos - to.offset;

                trf.position = Mathd.toVector3(newUnityPos);
            }


            Debug.Log($"Transferred {((MonoBehaviour)offsetTransform).name} from {from.key.handle.ToHex()} to {to.key.handle.ToHex()}");
        }
        /// <summary>
        /// Updates the offset for the given scene.
        /// </summary>
        /// <param name="scene"></param>
        public void UpdateOffset(OffsetScene<Scene> scene)
        {
            if (scene.offset == offsets[scene.key])
                return;

            var key = scene.key;
            if (!offsets.ContainsKey(key))
            {
                offsets.Add(key, Vector3d.zero);
            }
            Debug.Log($"Offset from {offsets[key]} to {scene.offset}");
            Vector3d old_offset = offsets[key];
            offsets[key] = scene.offset;
            if (offsettables.TryGetValue(scene.key, out List<IOffsettable<Scene>> list))
            {
                offsetter.Offset(old_offset, offsets[key], scene.key, list.ToArray());
            }
            else
            {
                offsetter.Offset(old_offset, offsets[key], scene.key);
            }
        }

        public void RegisterOffsettable(IOffsettable<Scene> offsettable, Scene scene)
        {
            if (!offsettables.ContainsKey(scene))
                offsettables.Add(scene, new List<IOffsettable<Scene>> { offsettable });
            else
                offsettables[scene].Add(offsettable);
        }
    }
}
