using System;
using UnityEngine;

namespace FloatingOffset.Runtime
{
    namespace Types
    {

        /// <summary>
        /// Interface used for communicating with an Offsetter. An Offsetter is responsible for correctly offsetting all scene objects when the origin is shifted. If you wish to write your own implementation, make sure it uses this interface.
        /// </summary>
        /// 
        public interface IOffsetter<TSceneKey>
        {
            public void Offset(Vector3d old_offset, Vector3d new_offset,TSceneKey scene);
        }
        /// <summary>
        /// Handles the scene and offset.
        /// </summary>
        /// <typeparam name="TSceneKey"></typeparam>
        public struct OffsetScene<TSceneKey>
        {
            /// <summary>
            /// The real offset of this offset scene.
            /// </summary>
            public Vector3d offset;
            /// <summary>
            /// The only source of truth for the number of views in this offset scene.
            /// </summary>
            public int view_count;
            /// <summary>
            /// The Scene associated with this offset scene.
            /// </summary>
            public TSceneKey key;
            /// <summary>
            /// Whether this offset scene is valid (ready to use) or not (i.e. still loading or broken)
            /// </summary>
            internal bool valid;
            /// <summary>
            /// If two scenes have different layers then they will not merge
            /// </summary>
            public int layer;
        }
        /// <summary>
        /// IOffsetObject is the generic form of the OffsetTransform used in the core.
        /// </summary>
        /// <typeparam name="TScene"></typeparam>
        public interface IOffsetObject<TScene>
        {
            /// <summary>
            /// The real position of this offset object.
            /// </summary>
            /// <returns></returns>
            public Vector3d GetRealPosition();
            /// <summary>
            /// The local scene position (converted from Vector3 into Vector3d) of this offset object.
            /// </summary>
            /// <returns></returns>
            public Vector3d GetEnginePosition();
            /// <summary>
            /// The key of the scene this offset object resides in.
            /// </summary>
            /// <returns></returns>
            public TScene GetSceneKey();
            /// <summary>
            /// The position of 
            /// </summary>
            /// <returns></returns>
            public float GetEnginePositionSquareMagnitude();
            public void SetEnginePosition(Vector3d vector3d);
            public bool IsView();
        }
        public interface IOffsettable
        {
            public void OnOffset(Vector3d old_offset, Vector3d new_offset);
        }
        public interface IOffsetHandler<TSceneKey>
        {
            public void UpdateOffset(OffsetScene<TSceneKey> scene);
            public void TransferTo(IOffsetObject<TSceneKey> offsettable, OffsetScene<TSceneKey> from, OffsetScene<TSceneKey> to, bool reposition = false);
            public void TransferAllTo(OffsetScene<TSceneKey> from, OffsetScene<TSceneKey> to);
            public void Clone(TSceneKey scene, Action<(TSceneKey scene, float delta)> onSceneReady);
            public void RegisterOffsettable(IOffsettable offsettable, TSceneKey scene);
        }
        public enum OffsetActions
        {
            RemoveView,
            PendingTransfer,
            AwaitingScene,
            None
        }
    }
}
