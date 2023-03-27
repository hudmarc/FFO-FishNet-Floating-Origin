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
        private Scene nullScene;
        private IOffsetter ioffsetter;
        private LoadSceneParameters parameters = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
        private bool subscribedToTick = false;
        public Vector3d GetOffset(Scene scene) => FOGroups[scene].offset;
        public int GetFOGroups(Scene scene) => FOGroups[scene].members;
        //this is being called incorrectly
        public void MoveToNullScene(FOObject foobject)
        {
            Vector3Int oldGridPosition = RealToGridPosition(foobject.realPosition);
            MoveToScene(foobject, nullScene, oldGridPosition);
        }
        public void MoveToOtherObserverSceneAndOffset(FOObject foobject, FOObserver target)
        {
            Vector3Int oldGridPosition = RealToGridPosition(foobject.realPosition);
            
            OffsetObject(foobject, foobject.groupOffset, target.groupOffset);
            MoveToScene(foobject, target.gameObject.scene, oldGridPosition);
        }
        // this should be called before anything else
        public void MoveToNewGroup(FOObject foobject, Vector3d newOffset)
        {
            Vector3d oldOffset = foobject.groupOffset;
            foobject.busy = true;
            SceneManager.LoadSceneAsync(foobject.gameObject.scene.buildIndex, parameters).completed += (AsyncOperation op) => OnMoveComplete(foobject, oldOffset, newOffset);
        }
        private void OnMoveComplete(FOObject foobject, Vector3d oldOffset, Vector3d newOffset)
        {
            Scene newScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            FOGroups.Add(newScene, new FOGroup());
            Vector3Int oldGridPosition = RealToGridPosition(foobject.realPosition);
            OffsetScene(newScene, Vector3d.zero, newOffset);
            OffsetObject(foobject, oldOffset, newOffset);
            MoveToScene(foobject, newScene, oldGridPosition);
        }
        private void OffsetScene(Scene scene, Vector3d oldOffset, Vector3d newOffset)
        {
            (Vector3 offset, Vector3 preciseOffset) difference = DifferenceBetween(newOffset, oldOffset);
            // Debug.Log($"Offsetting scene! {scene.handle} old: {oldOffset.ToString()} new: {newOffset.ToString()} offset: {difference.offset.ToString()} precise: {difference.preciseOffset.ToString()}");
            
            //this gets called first
            ioffsetter.Offset(scene, difference.offset);

            if (difference.preciseOffset != Vector3.zero)
                ioffsetter.Offset(scene, difference.preciseOffset);
            
            //here the scene cannot be found, why?
            FOGroups[scene] = FOGroups[scene].ChangedOffset(newOffset);
            
            RebasedScene?.Invoke(scene);
        }

        private void OffsetObject(FOObject foobject, Vector3d oldOffset, Vector3d newOffset)
        {
            (Vector3 offset, Vector3 preciseOffset) difference = DifferenceBetween(newOffset, oldOffset);
            foobject.unityPosition += difference.offset;

            if (difference.preciseOffset != Vector3.zero)
                foobject.unityPosition += difference.preciseOffset;
        }
        private void MoveToScene(FOObject foobject, Scene newScene, Vector3Int? oldGridPosition)
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
                    SceneManager.SetActiveScene(newScene);
            }
            foobject.busy = false;

            if (oldScene.handle == newScene.handle)
                return;

            FOGroups[newScene] = FOGroups[newScene].AddMember();

            if (FOGroups.ContainsKey(oldScene) && FOGroups[oldScene].members < 1 && oldScene.handle != nullScene.handle && oldGridPosition!=null)//make sure the oserver is in a known scene
            {
                //This seems to be called correctly initially, but then it is called incorrectly a second time for some reason?
                // GridCellEnabled(oldGridPosition.Value, false, oldScene);
                FOGroups.Remove(oldScene);
                //This will destroy every object in the scene! Make sure all FOObjecs are moved to the null scene before this happens!
                StartCoroutine(DelayedUnloadAsync(oldScene));
            }
            if (InstanceFinder.IsServer && localObserver != null)
                SetSingleVisibleScene(newScene, foobject == localObserver);

        }
        System.Collections.IEnumerator DelayedUnloadAsync(Scene scene)
        {
            yield return null;
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
        private void SetSingleVisibleScene(Scene scene, bool isLocalObserver)
        {
            if (isLocalObserver)
                foreach (Scene scn in FOGroups.Keys)
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
