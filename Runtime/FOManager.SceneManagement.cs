using System.Collections.Generic;
using FishNet.FloatingOrigin.Types;
using UnityEngine;
using UnityEngine.SceneManagement;
using FishNet.Managing.Timing;
using System.Collections;
using FishNet.Object;

namespace FishNet.FloatingOrigin
{
    public partial class FOManager
    {
        private Queue<OffsetGroup> queuedGroups = new Queue<OffsetGroup>();
        private readonly Scene invalidScene = new Scene();
        private IOffsetter ioffsetter;
        [Tooltip("How to perform physics.")]
        [SerializeField] private PhysicsMode _physicsMode = PhysicsMode.Unity;

        private readonly LoadSceneParameters parameters = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);

        /// <summary>
        /// Requests a new group. Returns first found existing group that is unused or newly created group.
        /// </summary>
        /// <param name="scene">
        /// Creates or dequeues new group.
        /// </param>
        private OffsetGroup RequestNewGroup(Scene scene)
        {
            while (queuedGroups.Count > 0 && queuedGroups.Peek().observers.Count > 0)
            {
                queuedGroups.Dequeue();
            }

            if (queuedGroups.Count > 0)
            {
                if (queuedGroups.Peek().scene.IsValid())
                    return queuedGroups.Dequeue();
                else
                    return null;
            }
            else
            {
                var offsetGroup = new OffsetGroup(invalidScene, Vector3d.zero);
                queuedGroups.Enqueue(offsetGroup);

                SceneManager.LoadSceneAsync(scene.buildIndex, parameters).completed += (arg) => SetupGroup(offsetGroup);
                return null;
            }

        }

        private void SetupGroup(OffsetGroup group)
        {
            group.scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);

            offsetGroups.Add(group.scene, group);

            CullNetworkObjects(group.scene);
        }
        /// <summary>
        /// Remove scene NOB's on newly created scenes.
        /// </summary>
        /// <param name="scene">
        /// The scene to cull NOB's from.
        /// </param>
        private void CullNetworkObjects(Scene scene)
        {
            var objects = scene.GetRootGameObjects();
            foreach (GameObject g in objects)
            {
                if (g.TryGetComponent<NetworkObject>(out NetworkObject obj))
                {
                    obj.gameObject.SetActive(false);
                    Destroy(obj.gameObject);
                }
            }
        }

        /// <summary>
        /// Unloads a given scene.
        /// </summary>
        /// <param name="scene">
        /// The scene you want to unload.
        /// </param>
        private void UnloadScene(Scene scene)
        {

        }

        private void SetGroupOffset(OffsetGroup group, Vector3d offset)
        {
            // hashGrid.Remove(group.offset);
            // hashGrid.Add(offset, group);
            //this is fine
            Vector3d difference = group.offset - offset;
            //this is allegedly fine
            Vector3 remainder = (Vector3)(difference - ((Vector3d)(Vector3)difference));

            // Log($"{group.scene.handle} old: {group.offset} new: {offset} diff: {difference} rem: {remainder}", "SCENE MANAGEMENT");

            //this might be called in the wrong spot!
            GroupChanged?.Invoke(group);

            ioffsetter.Offset(group.scene, (Vector3)difference);

            if (remainder != Vector3.zero)
            {
                ioffsetter.Offset(group.scene, (Vector3)remainder);
                Log("Remainder was not zero, offset with precise remainder. If this causes a bug, now you know what to debug.", "SCENE MANAGEMENT");
            }

            group.offset = offset;

            

        }

        private void RecomputeVisibleScenes()
        {
            if (first == null)
                return;

            Log($"Recomputing scene visibility. Local observer is {(first == null ? "null" : "something")}", "SCENE MANAGEMENT");

            foreach (Scene scn in offsetGroups.Keys)
            {
                SetSceneVisibillity(scn, scn.handle == first.gameObject.scene.handle);
                if (scn.handle == first.gameObject.scene.handle)
                    UnityEngine.SceneManagement.SceneManager.SetActiveScene(scn);

            }

        }
        private void SetSceneVisibillity(Scene scene, bool visible)
        {
            Log($"Set scene {scene.handle} {(visible ? "visible" : "not visible")}", "SCENE MANAGEMENT");
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