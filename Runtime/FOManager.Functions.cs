using System;
using FishNet.Managing.Timing;
using FishNet.Object;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FishNet.FloatingOrigin
{
    //Helper functions for the FOManager live here
    public partial class FOManager
    {
        private void RecomputeVisibleScenes()
        {
            if (local == null)
                return;

            Log("Recomputing scene visibility on server.", "SCENE MANAGEMENT");

            foreach (Scene scn in offsetGroups.Keys)
            {
                SetSceneVisibillity(scn, scn.handle == local.gameObject.scene.handle);
                if (scn.handle == local.gameObject.scene.handle)
                    SceneManager.SetActiveScene(scn);
            }

        }
        private void SetSceneVisibillity(Scene scene, bool visible)
        {
            Log($"Set scene {Math.Abs(scene.handle):X} {(visible ? "visible" : "not visible")}", "SCENE MANAGEMENT");
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
        private void SetPhysicsMode(PhysicsMode mode)
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

        public Vector3 RealToUnity(Vector3d realPosition, Scene scene) => Functions.RealToUnity(realPosition, serverFullStart ? offsetGroups[scene].offset : localOffset);

        public Vector3d UnityToReal(Vector3 unityPosition, Scene scene) => Functions.UnityToReal(unityPosition, serverFullStart ? offsetGroups[scene].offset : localOffset);
    }
    public static class FOManagerExtensions
    {
        public static Vector3d GetRealPosition(this Transform transform) => FOManager.instance.UnityToReal(transform.position, transform.gameObject.scene);
    }
}