using System.Collections.Generic;
using FishNet.Broadcast;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FishNet.FloatingOrigin
{
    namespace Types
    {
        public class OffsetGroup
        {
            public Scene scene;
            public Vector3d offset;
            public HashSet<FOView> views = new HashSet<FOView>();
            private FOObject[] cached_objects;
            private bool dirty = true;

            public FOObject[] GetFOObjectsCached()
            {
                if (dirty || cached_objects == null)
                {
                    List<FOObject> foobjects = new List<FOObject>();

                    var objects = scene.GetRootGameObjects();
                    foreach (GameObject g in objects)
                    {
                        if (g.TryGetComponent(out FOObject obj))
                        {
                            foobjects.Add(obj);
                        }
                    }

                    cached_objects = foobjects.ToArray();
                    dirty = false;
                }
                return cached_objects;
            }
            public void MakeDirty()
            {
                dirty = true;
            }

            public OffsetGroup(Scene scene, Vector3d offset)
            {
                this.scene = scene;
                this.offset = offset;
            }
            public Vector3 GetClientCentroid()
            {
                Vector3 position = Vector3.zero;
                foreach (var client in views)
                {
                    position += client.transform.position;
                }
                return position / views.Count;
            }
        }
        /// <summary>
        /// Interface used for communicating with an Offsetter. An Offsetter is responsible for correctly offsetting all scene objects when the origin is shifted. If you wish to write your own implementation, make sure it uses this interface.
        /// </summary>
        public interface IOffsetter
        {
            void Offset(Scene scene, Vector3 offset);
        }
        public struct OffsetSyncBroadcast : IBroadcast
        {
            public double offsetX, offsetY, offsetZ;
            public Vector3d offset => new Vector3d(offsetX, offsetY, offsetZ);

            public OffsetSyncBroadcast(Vector3d offset)
            {
                this.offsetX = offset.x;
                this.offsetY = offset.y;
                this.offsetZ = offset.z;
            }
            public override string ToString()
            {
                return $"Offset: {offset}";
            }
        }
    }
}
