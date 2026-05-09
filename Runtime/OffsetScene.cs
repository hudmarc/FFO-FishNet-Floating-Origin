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
    public class OffsetScene : MonoBehaviour, IOffsetScene<Vector3, Scene>
    {
        [SerializeField]
        private Offsetter offsetter;
        private Vector3d offset;
        private Vector3d velocity;
        private bool isEmpty;
        private HashSet<IOffsetObject<Vector3, Scene>> offsettables = new HashSet<IOffsetObject<Vector3, Scene>>();

        private readonly LoadSceneParameters parameters = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
        void Start()
        {
            if (offsetter == null)
                offsetter = GetComponent<Offsetter>();
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
                // The 'true' parameter ensures it also finds disabled GameObjects/Components
                offsetScene = root.GetComponentInChildren<OffsetScene>(true);

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

        public int CountViews()
        {
            return offsettables.Count;
        }

        public Vector3d GetOffset()
        {
            return offset;
        }

        public Scene GetSceneKey()
        {
            return gameObject.scene;
        }

        public Vector3d GetVelocity()
        {
            return velocity;
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
            Offset(offset, velocity, Time.deltaTime);
        }

        public void SetOffset(Vector3d offset, Vector3d velocity, float delta)
        {
            Offset(offset, velocity, delta);
        }
        private void Offset(Vector3d offset, Vector3d velocity, float delta)
        {
            Vector3d old_offset = this.offset;
            this.offset = offset + velocity * delta;
            this.velocity = velocity;
            offsetter.Offset(old_offset, this.offset);
        }
        public void TransferAllTo(IOffsetScene<Vector3, Scene> scene)
        {
            foreach (IOffsetObject<Vector3, Scene> offsettable in offsettables)
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
            offsettables.Remove(offsettable);
        }


        private (Vector3d position, Vector3d velocity) CalculateAverages()
        {
            Vector3d position_sum = Vector3d.zero;
            Vector3d velocity_sum = Vector3d.zero;

            foreach (IOffsetObject<Vector3, Scene> offsetable in offsettables)
            {
                position_sum += offsetable.GetRealPosition();
                velocity_sum += offsetable.GetRealVelocity();
            }

            double inverse = 1 / ((double)offsettables.Count);

            return (position_sum * inverse, velocity_sum * inverse);
        }
        public void ProcessOffsets()
        {
            var (position, velocity) = CalculateAverages();
            SetOffset(position, velocity);
        }

        public void RegisterTransform(OffsetTransform offsetTransform)
        {
            offsettables.Add(offsetTransform);
        }

        public void UnregisterTransform(OffsetTransform offsetTransform)
        {
            offsettables.Remove(offsetTransform);
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
