using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using UnityEngine;

namespace FishNet.FloatingOrigin
{
    public partial class FOManager
    {
        /// <summary>
        /// Pass in a unity coordinate (for example, from transform.position) relative to the local offset, and return the real position
        /// </summary>
        /// <param name="unityPosition"></param>
        /// <returns></returns>
        [Client]
        public Vector3d UnityToReal(Vector3 unityPosition) => UnityToReal(unityPosition, localObserver.group.offset);
        /// <summary>
        /// Pass in a real coordinate, and return the Unity position relative to the local offset
        /// </summary>
        /// <param name="unityPosition"></param>
        /// <returns></returns>
        [Client]
        public Vector3 RealToUnity(Vector3d realPosition) => RealToUnity(realPosition, localObserver.group.offset);

        /// <summary>
        /// The grid position of an observer
        /// </summary>
        public Vector3Int ObserverGridPosition(FOObserver observer) => RealToGridPosition(observer.realPosition);
        /// <summary>
        /// The grid position of a real position
        /// </summary>
        public Vector3Int RealToGridPosition(Vector3d realPosition) => new Vector3Int((int)(realPosition.x * inverseChunkSize), (int)(realPosition.y * inverseChunkSize), (int)(realPosition.z * inverseChunkSize));

        /// <summary>
        /// Group of cells adjacent to passed cell. Includes cell passed.
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public Vector3Int[] AdjacentCellGroup(Vector3Int cell)
        {
            Vector3Int[] cells = new Vector3Int[27];

            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                    for (int z = 0; z < 3; z++)
                        cells[z + y * 3 + x * 9] = cell + new Vector3Int(x - 1, y - 1, z - 1);

            return cells;
        }

        /// <summary>
        /// The Real position of a Unity position (which is relative to a particular offset)
        /// </summary>
        /// <param name="unityPosition"> Unity position (relative to 0,0,0 in the Scene)</param>
        /// <param name="offset"> Which offset to use for the calculation</param>
        /// <returns></returns>
        public Vector3d UnityToReal(Vector3 unityPosition, Vector3d offset) => Mathd.toVector3d(unityPosition) + offset;
        public Vector3d UnityToReal(FOObserver observer) => UnityToReal(observer.unityPosition, observer.group.offset);
        /// <summary>
        /// The Unity position of a Real position relative to a offset
        /// </summary>
        /// <param name="realPosition">Real position (relative to the Real origin)</param>
        /// <param name="offset">Which offset to use for the calculation</param>
        /// <returns></returns>
        public Vector3 RealToUnity(Vector3d realPosition, Vector3d offset) => Mathd.toVector3(realPosition - offset);

        public Vector3 RemoteToLocal(Vector3 remoteUnityPosition, Vector3d remoteOffset, Vector3d localOffset) => RealToUnity(UnityToReal(remoteUnityPosition, remoteOffset), localOffset);

        /// <summary>
        /// High precision Vector3d square of distance between two observers
        /// </summary>
        /// <param name="observer1"></param>
        /// <param name="observer2"></param>
        /// <returns></returns>
        public double SqrDistanceHP(FOObserver observer1, FOObserver observer2) => Vector3d.SqrMagnitude(UnityToReal(observer1) - UnityToReal(observer2));
        /// <summary>
        /// Low precision Vector3 square of distance between two observers
        /// </summary>
        /// <param name="observer1"></param>
        /// <param name="observer2"></param>
        /// <returns></returns>
        public float SqrDistanceLP(FOObserver observer1, FOObserver observer2) => Vector3.SqrMagnitude(observer1.unityPosition - (observer2.unityPosition + (Mathd.toVector3(observer2.group.offset) - Mathd.toVector3(observer2.group.offset))));
        /// <summary>
        /// Subtract vector1 from vector2
        /// </summary>
        /// <param name="vector1"></param>
        /// <param name="vector2"></param>
        /// <returns></returns>
        public (Vector3 offset, Vector3 preciseOffset) DifferenceBetween(Vector3d vector1, Vector3d vector2)
        {
            Vector3 offset = Mathd.toVector3(vector2 - vector1);
            return (offset, Mathd.toVector3(vector2 - vector1 - Mathd.toVector3d(offset)));
        }
        /// <summary>
        /// Find average offset of a group of Floating Origin Observers
        /// </summary>
        /// <param name="groupedObservers"></param>
        /// <returns></returns>
        Vector3d AverageOffset(FOObserver[] groupedObservers)
        {
            if (groupedObservers.Length == 1)
                return groupedObservers[0].realPosition;

            Vector3d offset = Vector3d.zero;
            foreach (var observer in groupedObservers)
            {
                offset += observer.realPosition;
            }
            return offset / ((double)groupedObservers.Length);
        }
        /// <summary>
        /// Teleports an FOObserver to a real position
        /// </summary>
        /// <param name="realPosition"></param>
        public void TeleportTo(FOObserver observer, Vector3d realPosition)
        {
            Vector3Int gridPos = RealToGridPosition(realPosition);

            HashSet<FOObserver> temp = new HashSet<FOObserver>();

            RebuildOffsetGroup(observer, gridPos, temp);//runs synchronously
            hasRebuilt = true;
            observer.unityPosition = RealToUnity(realPosition, observer.group.offset);
        }
        /// <summary>
        /// Teleports an FOObserver to another FOObserver
        /// </summary>
        /// <param name="realPosition"></param>
        public void TeleportTo(FOObserver observer, FOObserver target)
        {
            TeleportTo(observer, target.realPosition);
        }
    }
}
