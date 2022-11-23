using System.Collections.Generic;
using FishNet.FloatingOrigin.Types;
using UnityEngine;
using UnityEngine.SceneManagement;
using FishNet.Managing.Timing;

namespace FishNet.FloatingOrigin
{
    public partial class FOManager
    {
        [Tooltip("How to perform physics.")]
        [SerializeField] private PhysicsMode _physicsMode = PhysicsMode.Unity;
        /// <summary>
        /// How to perform physics.
        /// </summary>
        public PhysicsMode PhysicsMode => _physicsMode;
        [SerializeField] private UnityEngine.Component offsetter;
        private Dictionary<Scene, int> scenes = new Dictionary<Scene, int>();
        private IOffsetter ioffsetter;
        private LoadSceneParameters parameters = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
        private bool subscribedToTick = false;

        private void OffsetScene(Scene scene, Vector3d oldOffset, Vector3d newOffset)
        {
            (Vector3 offset, Vector3 preciseOffset) difference = DifferenceBetween(newOffset, oldOffset);

            // Debug.Log($"Offsetting scene! {scene.handle} old: {oldOffset.ToString()} new: {newOffset.ToString()} offset: {difference.offset.ToString()} precise: {difference.preciseOffset.ToString()}");

            ioffsetter.Offset(scene, difference.offset);

            if (difference.preciseOffset != Vector3.zero)
                ioffsetter.Offset(scene, difference.preciseOffset);
        }

        private void OffsetObserver(FOObserver observer, Vector3d oldOffset, Vector3d newOffset)
        {
            (Vector3 offset, Vector3 preciseOffset) difference = DifferenceBetween(newOffset, oldOffset);
            observer.unityPosition += difference.offset;

            if (difference.preciseOffset != Vector3.zero)
                observer.unityPosition += difference.preciseOffset;
        }

        private void MoveToAndOffset(FOObserver observer, FOObserver head)
        {
            OffsetObserver(observer, observer.group.offset, head.group.offset);
            MoveToScene(observer, head.group, head.gameObject.scene);
            if (InstanceFinder.IsServer && localObserver != null)
                SetSingleVisibleScene(head.gameObject.scene, head == localObserver || observer == localObserver);
        }
        private void MoveToNewGroup(FOObserver head, Vector3d oldOffset, FOGroup group)
        {
            // Debug.Log("About to load scene");
            SceneManager.LoadSceneAsync(head.gameObject.scene.buildIndex, parameters).completed += (AsyncOperation op) => OnAsyncComplete(head, oldOffset, group);
        }
        private void OnAsyncComplete(FOObserver head, Vector3d oldOffset, FOGroup group)
        {
            Scene newScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            // Debug.Log($"Head:{head.OwnerId}");
            OffsetScene(newScene, Vector3d.zero, group.offset);
            OffsetObserver(head, oldOffset, group.offset);
            MoveToScene(head, group, newScene);
        }
        private void SetSceneObservers(Scene scene, int num)
        {
            scenes[scene] = num;
        }
        private void MoveToScene(FOObserver observer, FOGroup group, Scene newScene)
        {
            var oldScene = observer.gameObject.scene;

            if (observer.group != null && oldScene.handle == newScene.handle)
                observer.group.members--;

            observer.group = group;

            // Debug.Log($"Moving from {oldScene.handle} to {newScene.handle}");

            if (!scenes.ContainsKey(newScene))
                scenes.Add(newScene, 0);

            if (observer.gameObject.scene != newScene)
            {
                SceneManager.MoveGameObjectToScene(observer.gameObject, newScene);
                if (InstanceFinder.IsHost)
                    SceneManager.SetActiveScene(newScene);
            }
            observer.busy = false;


            if (oldScene.handle == newScene.handle)
                return;

            scenes[newScene]++;

            if (--scenes[oldScene] < 1)//make sure the oserver's group isn't null. if it is they don't yet have an old scene.
            {
                scenes.Remove(oldScene);
                StartCoroutine(DelayedUnloadAsync(oldScene));
            }
            if (InstanceFinder.IsServer && localObserver != null)
                SetSingleVisibleScene(newScene, observer == localObserver);

        }
        System.Collections.IEnumerator DelayedUnloadAsync(Scene scene)
        {
            yield return null;
            SceneManager.UnloadSceneAsync(scene);
        }
#if UNITY_EDITOR
        void OnGUI()
        {
            foreach (var val in scenes)
            {
                GUILayout.Button($" Scene {val.Key.handle.ToString()}: {val.Value}");
            }
            foreach (var ob in observers)
            {
                if (ob != null)
                    GUILayout.Button($"Owner: {ob.OwnerId} Unity Position: {(int)ob.unityPosition.x} {(int)ob.unityPosition.y} {(int)ob.unityPosition.z} Real Position: {(int)ob.realPosition.x} {(int)ob.realPosition.y} {(int)ob.realPosition.z} Group Offset: {(int)ob.group.offset.x} {(int)ob.group.offset.y} {(int)ob.group.offset.z} Group Members: {ob.group.members}");
            }
        }
#endif
        public void SetPhysicsMode(PhysicsMode mode)
        {
            if (mode == PhysicsMode.TimeManager)
            {
                if (!subscribedToTick)
                {
                    InstanceFinder.TimeManager.OnTick += Simulate;
                    subscribedToTick = true;
                }
            }
            else
            {
                InstanceFinder.TimeManager.OnTick -= Simulate;
            }
            _physicsMode = mode;
        }
        void FixedUpdate()
        {
            if (_physicsMode == PhysicsMode.Unity)
                Simulate();
        }
        internal void Simulate()
        {
            foreach (Scene scene in scenes.Keys)
                if (scene.IsValid())
                    scene.GetPhysicsScene().Simulate(Time.fixedDeltaTime);
        }
        private void SetSingleVisibleScene(Scene scene, bool isLocalObserver)
        {
            if (isLocalObserver)
                foreach (Scene scn in scenes.Keys)
                    SetSceneVisibillity(scn, scn == scene);
            else
                SetSceneVisibillity(scene, false);
        }
        private void SetSceneVisibillity(Scene scene, bool visible)
        {
            var rootObjectsInScene = scene.GetRootGameObjects();
            for (int i = 0; i < rootObjectsInScene.Length; i++)
            {
                Renderer[] renderers = rootObjectsInScene[i].GetComponentsInChildren<Renderer>(false);
                for (int j = 0; j < renderers.Length; j++)
                {
                    renderers[j].enabled = visible;
                }
            }
        }
    }
}
