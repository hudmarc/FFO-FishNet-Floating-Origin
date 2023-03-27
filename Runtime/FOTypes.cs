using FishNet.Broadcast;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FishNet.FloatingOrigin
{
    namespace Types
    {
        public struct FOGroup
        {
            public Vector3d offset;
            public int members;
            public FOGroup ChangedOffset(Vector3d newOffset)
            {
                this.offset = newOffset;
                return this;
            }
            public FOGroup ChangedMembers(int members)
            {
                this.members = members;
                return this;
            }
            public FOGroup RemoveMember()
            {
                members = members-1;
                return this;
            }
            public FOGroup AddMember()
            {
                members = members+1;
                return this;
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
