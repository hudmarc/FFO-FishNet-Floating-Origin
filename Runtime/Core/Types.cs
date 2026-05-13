using System;

namespace FloatingOffset.Runtime
{
    namespace Types
    {

        /// <summary>
        /// Interface used for communicating with an Offsetter. An Offsetter is responsible for correctly offsetting all scene objects when the origin is shifted. If you wish to write your own implementation, make sure it uses this interface.
        /// </summary>
        public interface IOffsetter<TSceneKey>
        {
            /// <summary>
            /// Offsets the given scene from the given old offset to the given new offset.
            /// </summary>
            /// <param name="old_offset"></param>
            /// <param name="new_offset"></param>
            /// <param name="scene"></param>
            /// <param name="offsettables"></param>
            void Offset(Vector3d old_offset, Vector3d new_offset, TSceneKey scene, IOffsettable<TSceneKey>[] offsettables = null);
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
            /// The only source of truth for the number of views in this offset scene. Assigned to in the view loop.
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
            /// <summary>
            /// If not self, the scene this scene will be merged to.
            /// </summary>
            internal int merge_target;
        }
        /// <summary>
        /// IOffsetObject is the generic form of the OffsetTransform used in the core.
        /// </summary>
        /// <typeparam name="TSceneKey"></typeparam>
        public interface IOffsetObject<TSceneKey>
        {
            /// <summary>
            /// The local scene position (converted from Vector3 into Vector3d) of this offset object.
            /// </summary>
            /// <returns></returns>
            Vector3d GetEnginePosition();
            /// <summary>
            /// Sets local scene position (converted from Vector3d into Vector3) of this offset object.
            /// </summary>
            /// <returns></returns>
            void SetEnginePosition(Vector3d position);
            /// <summary>
            /// The key of the scene this offset object resides in.
            /// </summary>
            /// <returns></returns>
            TSceneKey GetSceneKey();
            /// <summary>
            /// Sets this object's scene.
            /// </summary>
            /// <param name="key"></param>
            void SetSceneKey(TSceneKey key);
            /// <summary>
            /// Whether this OffsetTransform is a view
            /// </summary>
            /// <returns></returns>
            bool IsView();
            /// <summary>
            /// Whether this OffsetTransform is valid (i.e. not destroyed)
            /// </summary>
            /// <returns></returns>
            bool IsValid();
            void Destroy();
        }
        public interface IOffsettable<TSceneKey>
        {
            /// <summary>
            /// Called when this offsettable's scene is offset.
            /// </summary>
            /// <param name="old_offset"></param>
            /// <param name="new_offset"></param>
            void OnOffset(Vector3d old_offset, Vector3d new_offset);
            /// <summary>
            /// The key of the scene this offsettable object resides in.
            /// </summary>
            /// <returns></returns>
            TSceneKey GetSceneKey();
        }
        public interface IOffsetHandler<TSceneKey>
        {
            /// <summary>
            /// Applies the offset for the given scene.
            /// </summary>
            /// <param name="scene"></param>
            void UpdateOffset(OffsetScene<TSceneKey> scene);
            /// <summary>
            /// Transfer the given OffsetTransform from the 'from' scene to the 'to' scene. If reposition is true, the position of the transform will be changed too to match the target scene's offset.
            /// </summary>
            /// <param name="offsettable"></param>
            /// <param name="from"></param>
            /// <param name="to"></param>
            /// <param name="reposition"></param>
            void TransferTo(IOffsetObject<TSceneKey> offsettable, TSceneKey from, TSceneKey to, bool reposition = false);
            /// <summary>
            /// Clone the given scene. Calls the callback when done.
            /// </summary>
            /// <param name="scene"></param>
            /// <param name="onSceneReady"></param>
            void Clone(TSceneKey scene, Action<TSceneKey> onSceneReady);
            /// <summary>
            /// Register the given Offsettable with the offset handler.
            /// </summary>
            /// <param name="offsettable"></param>
            /// <param name="scene"></param>
            void RegisterOffsettable(IOffsettable<TSceneKey> offsettable, TSceneKey scene);
        }
    }
}
