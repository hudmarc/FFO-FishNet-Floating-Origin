using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        public Vector3d UnityToReal(Vector3 unityPosition) => UnityToReal(unityPosition, localObserver.groupOffset);
        /// <summary>
        /// Pass in a real coordinate, and return the Unity position relative to the local offset
        /// </summary>
        /// <param name="unityPosition"></param>
        /// <returns></returns>
        [Client]
        public Vector3 RealToUnity(Vector3d realPosition, Scene scene) => RealToUnity(realPosition, FOGroups[scene].offset);

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
        public Vector3d UnityToReal(Vector3 unityPosition, Vector3 offset) => Mathd.toVector3d(unityPosition) + Mathd.toVector3d(offset);
        public Vector3d UnityToReal(FOObserver observer) => UnityToReal(observer.unityPosition, observer.groupOffset);
        /// <summary>
        /// The Unity position of a Real position relative to a offset
        /// </summary>
        /// <param name="realPosition">Real position (relative to the Real origin)</param>
        /// <param name="offset">Which offset to use for the calculation</param>
        /// <returns></returns>
        public Vector3 RealToUnity(Vector3d realPosition, Vector3 offset) => Mathd.toVector3(realPosition - Mathd.toVector3d(offset));

        public Vector3 RemoteToLocal(Vector3 remoteUnityPosition, Vector3 remoteOffset, Vector3 localOffset) => RealToUnity(UnityToReal(remoteUnityPosition, remoteOffset), localOffset);

        /// <summary>
        /// High precision Vector3d square of distance between two observers
        /// </summary>
        /// <param name="observer1"></param>
        /// <param name="observer2"></param>
        /// <returns></returns>
        public double SqrDistanceHP(FOObserver observer1, FOObserver observer2) => Vector3d.SqrMagnitude(observer1.realPosition - observer2.realPosition);
        /// <summary>
        /// Low precision Vector3 square of distance between two observers
        /// </summary>
        /// <param name="observer1"></param>
        /// <param name="observer2"></param>
        /// <returns></returns>
        public float SqrDistanceLP(FOObserver observer1, FOObserver observer2) => Vector3.SqrMagnitude(observer1.unityPosition - (observer2.unityPosition + (observer2.groupOffset - observer2.groupOffset)));
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
        /// <param name="foobservers"></param>
        /// <returns></returns>
        public Vector3 AverageOffset(List<FOObserver> foobservers)
        {
            if (foobservers.Count == 1)
                return Mathd.toVector3(foobservers[0].realPosition);

            Vector3 offset = Vector3.zero;
            foreach (var observer in foobservers)
            {
                offset += Mathd.toVector3(observer.realPosition);
            }
            return offset / ((float)foobservers.Count);
        }
        /// <summary>
        /// Teleports an FOObserver to a real position
        /// </summary>
        /// <param name="realPosition"></param>
        public void TeleportTo(FOObserver observer, Vector3d realPosition)
        {
            Vector3Int gridPos = RealToGridPosition(realPosition);

            RebuildOffsetGroup(observer, gridPos, realPosition);//runs synchronously
            observer.unityPosition = RealToUnity(realPosition, observer.groupOffset);
        }
        /// <summary>
        /// Teleports an FOObserver to another FOObserver
        /// </summary>
        /// <param name="realPosition"></param>
        public void TeleportTo(FOObserver observer, FOObserver target)
        {
            TeleportTo(observer, target.realPosition);
        }
        /// <summary>
        /// EXPERIMENTAL!! Offsets an observer and its entire group, and an anchor simultaneously without actually moving anything in the Unity scene unless necessary.
        /// </summary>
        /// <param name="observer">
        /// An arbitrary observer in the group you want to offset
        /// </param>
        /// <param name="anchor">
        /// An anchor you want to use as a reference frame
        /// </param>
        /// <param name="offset">
        /// The amount you want to offset the entire group and its anchor
        /// </param>
        // public void OffsetObserverGroupAndAnchor(FOObserver observer, FOAnchor anchor, Vector3d offset)
        // {
        //     observer.groupOffset += offset;
        //     anchor.realPosition = (anchor.realPosition);
        //     RebuildOffsetGroup(observer);
        // }
        /// <summary>
        /// Rebuilds the Offset Group for an observer. This will affect other observers around the initial observer, since they may also be rebased. After this operation completes,
        /// all affected observers will have a new Unity position, the same Real position, and will have their new Offset Group assigned.
        /// </summary>
        /// <param name="observer"></param>
        // public void RebuildOffsetGroup(FOObserver observer)
        // {
        //     Vector3Int gridPos = ObserverGridPosition(observer);
        //     RebuildOffsetGroup(observer, gridPos, observer.realPosition);
        // }
        // public FOObserver[] ObserversInGrid()
        // {
        //     throw new System.NotImplementedException();
        // }
        // public FOAnchor[] AnchorsInGrid()
        // {
        //     throw new System.NotImplementedException();
        // }
    }
}
