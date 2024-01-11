using System;
using FishNet.FloatingOrigin.Types;
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
            {
                Log("No local observer found.", "SCENE MANAGEMENT");
                return;
            }

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
            Log($"Set scene {scene.ToHex()} {(visible ? "visible" : "not visible")}", "SCENE MANAGEMENT");
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
        /// Remove scene FOObjects and FOViews on newly created scenes.
        /// </summary>
        /// <param name="scene">
        /// The scene to cull FOObjects and FOViews from.
        /// </param>
        private void CullFOObjects(Scene scene)
        {
            Debug.Log($"Culling objects from scene {scene.ToHex()}");
            var objects = scene.GetRootGameObjects();
            foreach (GameObject g in objects)
            {

                FOObject obj = g.GetComponent<FOObject>();

                if (obj != null)
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

        public Vector3 RealToUnity(Vector3d realPosition, Scene scene) => Functions.RealToUnity(realPosition, hostFullStart ? offsetGroups[scene].offset : localOffset);

        public Vector3d UnityToReal(Vector3 unityPosition, Scene scene) => Functions.UnityToReal(unityPosition, hostFullStart ? offsetGroups[scene].offset : localOffset);
        public void RecomputeObjectGridPositions()
        {
            var foobjects = GameObject.FindObjectsOfType<FOObject>();

            objects.Clear();

            foreach (var foo in foobjects)
            {
                objects.Add(foo.realPosition, foo);
            }
           
        }
        public OffsetGroup GetGroup(Scene scene)
        {
            return offsetGroups[scene];
        }
        public OffsetGroup GetLocalGroup()
        {
            return offsetGroups[local.gameObject.scene];
        }
        public bool IsGroup(Scene scene)
        {
            return offsetGroups.ContainsKey(scene);
        }
    }
    public static class FOManagerExtensions
    {
        const string HEX = "X";
        public static Vector3d GetRealPosition(this Transform transform) => FOManager.instance.UnityToReal(transform.position, transform.gameObject.scene);
        public static string ToHex(this Scene scene) => Math.Abs(scene.handle).ToString(HEX);
        public static PhysicsScene Physics(this GameObject gameObject) => gameObject.scene.GetPhysicsScene();
        // public static void SetPosition(this FOObject foo, Vector3 position)
        // {
        //     FOManager.instance.SetPosition(foo, position);
        // }
    }
}