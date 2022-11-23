using FishNet.Broadcast;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FishNet.FloatingOrigin
{
    namespace Types
    {
        /// <summary>
        /// A Floating Origin Group keeps track of its offset and how many members it contains. When a group no longer contains members its scene is unloaded.
        /// </summary>
        public class FOGroup
        {
            public Vector3d offset;
            public int members;
            public FOGroup(Vector3d offset, int members)
            {
                this.offset = offset;
                this.members = members;
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
                return $"Offset: {offset.ToString()}";
            }
        }
    }
}
