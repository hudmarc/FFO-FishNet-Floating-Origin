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
        private IOffsetter ioffsetter;
        protected Scene nullScene;
        private LoadSceneParameters parameters = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
        private bool subscribedToTick = false;
        public Vector3 GetOffset(Scene scene) => FOGroups[scene].offset;
        public void MoveToNullScene(FOObject foobject)
        {
            Log($"Moved {foobject.name} to Null scene");
            Vector3Int oldGridPosition = RealToGridPosition(foobject.realPosition);
            MoveFromSceneToScene(foobject, nullScene, oldGridPosition);
        }
        public void MoveToOtherObserverSceneAndOffset(FOObject foobject, FOObserver target)
        {
            Vector3Int oldGridPosition = RealToGridPosition(foobject.realPosition);

            OffsetObject(foobject, foobject.groupOffset, target.groupOffset);
            MoveFromSceneToScene(foobject, target.gameObject.scene, oldGridPosition);
        }
        // this should be called before anything else
        public void MoveToNewGroup(FOObject foobject, Vector3 newOffset)
        {
            Vector3 oldOffset = foobject.groupOffset;
            foobject.setBusy(true);
            //Bug here, says the scene is invalid??
            SceneManager.LoadSceneAsync(foobject.gameObject.scene.buildIndex, parameters).completed += (AsyncOperation op) => OnMoveComplete(foobject, oldOffset, newOffset);
        }
        private void OnMoveComplete(FOObject foobject, Vector3 oldOffset, Vector3 newOffset)
        {
            Scene newScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            FOGroups.Add(newScene, new FOGroup());
            Vector3Int oldGridPosition = RealToGridPosition(foobject.realPosition);
            OffsetScene(newScene, Vector3.zero, newOffset);
            OffsetObject(foobject, oldOffset, newOffset);
            MoveFromSceneToScene(foobject, newScene, oldGridPosition);
            if (foobject is FOObserver)
            {
                EnableAdjacent((FOObserver)foobject, foobject.gridPosition, newScene);
            }
        }
        private void OffsetScene(Scene scene, Vector3 oldOffset, Vector3 newOffset)
        {
            Vector3 difference = oldOffset - newOffset;
            Log($"Offsetting scene! {scene.handle} old: {oldOffset.ToString()} new: {newOffset.ToString()} diff: {difference.ToString()}");

            if (localObserver != null && scene.handle == localObserver.gameObject.scene.handle)
            {
                ioffsetter.Offset(gameObject.scene, difference);
            }
            //this gets called first
            ioffsetter.Offset(scene, difference);

            FOGroups[scene] = FOGroups[scene].ChangedOffset(newOffset);

            SceneChanged?.Invoke(scene);
        }

        private void OffsetObject(FOObject foobject, Vector3 oldOffset, Vector3 newOffset) => foobject.unityPosition += oldOffset - newOffset;
        private void MoveFromSceneToScene(FOObject foobject, Scene newScene, Vector3Int? oldGridPosition)
        {
            var oldScene = foobject.gameObject.scene;

            if (FOGroups.ContainsKey(oldScene) && oldScene != nullScene && oldScene != newScene)
                FOGroups[oldScene] = FOGroups[oldScene].RemoveMember();

            if (!FOGroups.ContainsKey(newScene))
                FOGroups.Add(newScene, new FOGroup());

            foobject.sceneHandle = newScene.handle;
            if (foobject.gameObject.scene != newScene)
            {
                SceneManager.MoveGameObjectToScene(foobject.gameObject, newScene);

                if (InstanceFinder.IsHost)
                    RecomputeVisibleScenes();
            }
            foobject.setBusy(false);
            foobject.OnMoveToNewScene(newScene);

            if (oldScene.handle == newScene.handle)
                return;

            FOGroups[newScene] = FOGroups[newScene].AddMember();

            if (FOGroups.ContainsKey(oldScene) && FOGroups[oldScene].members < 1 && oldScene.handle != nullScene.handle && oldGridPosition != null)//make sure the oserver is in a known scene
            {
                FOGroups.Remove(oldScene);
                //This will destroy every object in the scene! Make sure all FOObjecs are moved to the null scene before this happens!
                StartCoroutine(DelayedUnloadAsync(oldScene, oldGridPosition.Value, oldScene));
            }
        }
        private void MoveToSceneFromNull(FOObject foobject, Scene newScene)
        {
            if (!FOGroups.ContainsKey(newScene))
                FOGroups.Add(newScene, new FOGroup());

            foobject.sceneHandle = newScene.handle;

            if (foobject.gameObject.scene != newScene)
            {
                SceneManager.MoveGameObjectToScene(foobject.gameObject, newScene);

                if (InstanceFinder.IsHost)
                    RecomputeVisibleScenes();

            }
            foobject.setBusy(false);
            foobject.OnMoveToNewScene(newScene);
            FOGroups[newScene] = FOGroups[newScene].AddMember();

        }
        System.Collections.IEnumerator DelayedUnloadAsync(Scene scene, Vector3Int oldGridPosition, Scene oldScene)
        {

            yield return null;

            GridCellEnabled(oldGridPosition, false, scene);
            SceneManager.UnloadSceneAsync(scene);

        }
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
                subscribedToTick = false;
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
            foreach (Scene scene in FOGroups.Keys)
                if (scene.IsValid())
                    scene.GetPhysicsScene().Simulate(Time.fixedDeltaTime);
        }
        private void RecomputeVisibleScenes()
        {
            Log($"Recomputing scene visibility. Local observer is {(localObserver == null ? "null" : "something")}");
            if (localObserver == null)
                return;

            foreach (Scene scn in FOGroups.Keys)
            {
                SetSceneVisibillity(scn, scn.handle == localObserver.sceneHandle);
                if (scn.handle == localObserver.sceneHandle)
                    UnityEngine.SceneManagement.SceneManager.SetActiveScene(scn);

            }

        }
        private void SetSceneVisibillity(Scene scene, bool visible)
        {
            Log($"Set scene {scene.handle} {(visible ? "visible" : "not visible")}");
            var rootObjectsInScene = scene.GetRootGameObjects();
            for (int i = 0; i < rootObjectsInScene.Length; i++)
            {
                Renderer[] renderers = rootObjectsInScene[i].GetComponentsInChildren<Renderer>();
                for (int j = 0; j < renderers.Length; j++)
                {
                    renderers[j].enabled = visible;
                }

                Light[] lights = rootObjectsInScene[i].GetComponentsInChildren<Light>();
                for (int j = 0; j < lights.Length; j++)
                {
                    lights[j].enabled = visible;
                }

                Terrain[] terrains = rootObjectsInScene[i].GetComponentsInChildren<Terrain>();
                for (int j = 0; j < terrains.Length; j++)
                {
                    terrains[j].enabled = visible;
                }

                ParticleSystem[] ps = rootObjectsInScene[i].GetComponentsInChildren<ParticleSystem>();
                for (int j = 0; j < ps.Length; j++)
                {
                    if (visible)
                        ps[j].Play();
                    else
                        ps[j].Stop();
                }
            }
        }
    }
}
