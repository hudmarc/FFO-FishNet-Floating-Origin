using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// Will always be within the merge area of the nearest scene. If it is not in the merge area of any scene it will be moved to the null-scene where it will be disabled until needed.<br/><br/>
    /// Has a real position. When an OffsetTransform is moved to the null scene, its real position will be cached. If the OffsetTransform's real position is in range of any merge area, it will be taken from the nullscene and moved to the local position relative to its real position.<br/><br/>
    /// If an OffsetTransform is set to act as a view, it will additionally be continuosly updated so that it never goes to the null scene.
    /// </summary>
    public class OffsetTransform : OffsetBehaviour, IOffsetObject<Scene>
    {
        [field: SerializeField]
        [Tooltip("If checked, this transform can trigger rebases and will always be kept in a scene. If not, this object will be moved to the null scene if in an empty scene.")]
        public bool isView { get; private set; }
        private bool registered = false;
        void Start()
        {
            if (enabled && isView)
            {
                universe.server.RegisterView(this);
                registered = true;
            }

        }
        void OnDestroy()
        {
            if (isView && registered)
                universe.server.UnregisterView(this);
        }
        public void SetRealPositionApproximate(Vector3d position) => transform.position = Mathd.RealToUnity(position, GetSceneOffset());

        public Vector3d GetRealPosition() => Mathd.UnityToReal(transform.position, GetSceneOffset());
        private Vector3d GetSceneOffset() => universe.server.GetSceneOffset(gameObject.scene);
        public Vector3d GetEnginePosition() => Mathd.toVector3d(transform.position);
        public Scene GetSceneKey() => gameObject.scene;
        public float GetEnginePositionSquareMagnitude() => transform.position.sqrMagnitude;
        public void SetEnginePosition(Vector3d vector3d) => transform.position = Mathd.toVector3(vector3d);
        public bool IsView() => isView;
    }
}
