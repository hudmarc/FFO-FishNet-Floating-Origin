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
    public class OffsetScene : OffsetBehaviour, IOffsetScene<Vector3, Scene>
    {
        [SerializeField]
        private Offsetter offsetter;
        private Vector3d universe_offset;
        private Vector3d universe_velocity;
        private bool isEmpty;
        private HashSet<IOffsetObject<Vector3, Scene>> offset_transforms = new HashSet<IOffsetObject<Vector3, Scene>>();

        private readonly LoadSceneParameters parameters = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
        void Awake()
        {
            if (offsetter == null)
                offsetter = GetComponent<Offsetter>();
            universe.RegisterScene(this);
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

        public void Clone(Action<(IOffsetScene<Vector3, Scene> scene, float delta)> onSceneReady)
        {
            float start_time = Time.time;
            bool completed = false;
            // this is called twice if the editor is unfocused. seems to be a Unity bug.
            SceneManager.LoadSceneAsync(gameObject.scene.buildIndex, parameters).completed += (arg) => SetupScene(onSceneReady, start_time, ref completed);
        }

        private void SetupScene(Action<(IOffsetScene<Vector3, Scene> scene, float delta)> onSceneReady, float start_time, ref bool completed)
        {
            //fixes a bizarre Unity bug where the "completed" callback from LoadSceneAsync gets called twice under certain circumstances.
            // offsetGroups.ContainsKey(SceneManager.GetSceneAt(SceneManager.sceneCount - 1)) is causing scenes to NEVER be registered!
            if (completed)
            {
                Debug.LogWarning("Prevented double execution of completed callback by SceneManager LoadSceneAsync");
                return;
            }
            completed = true;
            Debug.Log($"setting up group {SceneManager.GetSceneAt(SceneManager.sceneCount - 1).ToHex()}");

            Scene scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);

            OffsetScene offsetScene = null;

            // 1. Get all top-level GameObjects in this specific scene
            GameObject[] rootObjects = scene.GetRootGameObjects();

            // 2. Iterate through the roots and search their children
            foreach (GameObject root in rootObjects)
            {
                offsetScene = root.GetComponent<OffsetScene>();

                if (offsetScene != null)
                {
                    break; // Found it, stop searching
                }
            }

            if (offsetScene != null)
            {
                // targetComponent is ready to use
                onSceneReady?.Invoke((offsetScene, Time.time - start_time));
            }
            else
            {
                Debug.LogError($"Could not find an OffsetScene component in scene {scene.name}");
            }

            CullFOObjects(scene);
        }

        private void CullFOObjects(Scene scene)
        {
            Debug.Log($"Culling objects from scene {scene.ToHex()}");
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

        public int ViewCount()
        {
            return offset_transforms.Count;
        }

        public Vector3d GetOffset()
        {
            return universe_offset;
        }

        public Scene GetSceneKey()
        {
            return gameObject.scene;
        }

        public Vector3d GetVelocity()
        {
            return universe_velocity;
        }

        public bool IsEmpty()
        {
            return isEmpty;
        }

        public void SetEmpty(bool empty)
        {
            isEmpty = empty;
        }

        public void SetOffset(Vector3d offset, Vector3d velocity)
        {
            Offset(offset, Vector3d.zero, 0);
        }

        public void SetOffset(Vector3d offset)
        {
            Offset(offset, universe_velocity, Time.deltaTime);
        }

        public void SetOffset(Vector3d offset, Vector3d velocity, float delta)
        {
            Offset(offset, velocity, delta);
        }
        private void Offset(Vector3d offset, Vector3d velocity, float delta)
        {
            Debug.Log($"Offset from {this.universe_offset} to {offset}");
            Vector3d old_offset = this.universe_offset;
            this.universe_offset = offset + velocity * delta;
            this.universe_velocity = velocity;
            offsetter.Offset(old_offset, this.universe_offset);
        }
        public void TransferAllTo(IOffsetScene<Vector3, Scene> scene)
        {
            foreach (IOffsetObject<Vector3, Scene> offsettable in offset_transforms)
            {
                TransferTo(offsettable, scene);
            }
        }
        /// <summary>
        /// Transfer the given offsettable to the given offset scene. Removes it from the offset scene this was called on.
        /// </summary>
        /// <param name="offsettable"></param>
        /// <param name="scene"></param>
        public void TransferTo(IOffsetObject<Vector3, Scene> offsettable, IOffsetScene<Vector3, Scene> scene)
        {
            if (offsettable.GetSceneKey() != gameObject.scene)
                throw new Exception($"Offsettable not found on scene {gameObject.scene.ToHex()}");
            offsettable.MoveTo(scene.GetSceneKey());
            Debug.Log("Transferred and unregistered transform");
            offset_transforms.Remove(offsettable);
        }


        private (Vector3d position, Vector3d velocity) CalculateAverageLocalOffset()
        {
            if (offset_transforms.Count == 0)
            {
                return (Vector3d.zero, Vector3d.zero);
            }
            Vector3d position_sum = Vector3d.zero;
            Vector3d velocity_sum = Vector3d.zero;

            foreach (IOffsetObject<Vector3, Scene> offsetable in offset_transforms)
            {
                position_sum += Mathd.toVector3d(offsetable.GetEnginePosition());
                Debug.Log($"id {((MonoBehaviour)offsetable).name} pos {offsetable.GetEnginePosition()} sum {position_sum}");
                velocity_sum += Mathd.toVector3d(offsetable.GetEngineVelocity());
            }
            double inverse = 1 / ((double)offset_transforms.Count);

            return (position_sum * inverse, velocity_sum * inverse);
        }
        public void ProcessOffsets()
        {
            var (position_offset, velocity_offset) = CalculateAverageLocalOffset();
            Debug.Log($"Calculated average position offset:{position_offset}");
            SetOffset(universe_offset + position_offset, universe_velocity + velocity_offset);
        }

        public void RegisterTransform(OffsetTransform offsetTransform)
        {
            Debug.Log("Added transform");
            offset_transforms.Add(offsetTransform);
        }

        public void UnregisterTransform(OffsetTransform offsetTransform)
        {
            Debug.Log("Unregistered transform");
            offset_transforms.Remove(offsetTransform);
        }
        public void RegisterOffsettable(IOffsettable offsettable)
        {
            offsetter.RegisterOffsettable(offsettable);
        }

        public void UnregisterOffsettable(IOffsettable offsettable)
        {
            offsetter.UnregisterOffsettable(offsettable);
        }
    }
}
