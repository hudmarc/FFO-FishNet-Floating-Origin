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
        [SerializeField]
        [Tooltip("Optional: If added, this will be used to calculate the velocity of the OffsetTransform.")]
        private Rigidbody referenceFrame;
        void Awake()
        {
            universe.Register(this);
        }
        void OnDestroy()
        {
            universe.Unregister(this);
        }
        public void SetRealPositionApproximate(Vector3d position)
        {
            transform.position = Mathd.RealToUnity(position, GetSceneOffset());
        }
        public Vector3d GetRealPosition()
        {
            return Mathd.UnityToReal(transform.position, GetSceneOffset());
        }
        private Vector3d GetSceneOffset()
        {
            return universe.server.GetSceneOffset(gameObject.scene);
        }

        public Vector3d GetRealVelocity()
        {
            if (referenceFrame == null)
                return universe.server.GetSceneVelocity(gameObject.scene);

            return Mathd.UnityToReal(referenceFrame.velocity, universe.server.GetSceneVelocity(gameObject.scene));
        }

        public Vector3d GetEnginePosition()
        {
            return Mathd.toVector3d(transform.position);
        }

        public Vector3d GetEngineVelocity()
        {
            return Mathd.toVector3d(referenceFrame == null ? Vector3.zero : referenceFrame.velocity);
        }

        public Scene GetSceneKey()
        {
            return gameObject.scene;
        }

        public float EngineVelocitySquaredMagnitude()
        {
            return referenceFrame == null ? 0 : referenceFrame.velocity.sqrMagnitude;
        }

        public GameObject GetObject()
        {
            return gameObject;
        }

        public void MoveTo(Scene scene)
        {
            SceneManager.MoveGameObjectToScene(gameObject, scene);
        }

        public float GetEnginePositionSquareMagnitude()
        {
            return transform.position.sqrMagnitude;
        }
    }
}
