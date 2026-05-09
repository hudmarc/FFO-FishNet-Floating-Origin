using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// Will always be within the merge area of the nearest scene. If it is not in the merge area of any scene it will be moved to the null-scene where it will be disabled until needed.<br/><br/>
    /// Has a real position. When an OffsetTransform is moved to the null scene, its real position will be cached. If the OffsetTransform's real position is in range of any merge area, it will be taken from the nullscene and moved to the local position relative to its real position.<br/><br/>
    /// If an OffsetTransform is set to act as a view, it will additionally be continuosly updated so that it never goes to the null scene.
    /// </summary>
    public class OffsetTransform : MonoBehaviour, IOffsetObject<Vector3, Scene>
    {
        [SerializeField]
        private OffsetUniverse universe;
        [SerializeField]
        [Tooltip("If checked, this transform can trigger rebases and will always be kept in a scene. If not, this object will be moved to the null scene if in an empty scene.")]
        private bool isView;
        [SerializeField]
        private Rigidbody reference_frame;
        void Awake()
        {
            if (isView)
            {
                universe.GetScene(gameObject.scene).RegisterTransform(this);
            }
            else
            {
                universe.server.RegisterView(this);
            }
        }
        void OnDestroy()
        {
            if (isView)
            {
                universe.GetScene(gameObject.scene).UnregisterTransform(this);
            }
            else
            {
                universe.server.UnregisterView(this);
            }
        }
        public void SetRealPositionApproximate(Vector3d position)
        {
            transform.position = Mathd.RealToUnity(position, GetSceneOffset());
        }
        public Vector3d GetRealPosition()
        {
            return Mathd.UnityToReal(transform.position, GetSceneOffset());
        }
        public Vector3d GetUnityPosition()
        {
            return Mathd.UnityToReal(transform.position, GetSceneOffset());
        }

        public int getID()
        {
            throw new System.NotImplementedException();
        }

        public Vector3d GetSceneOffset()
        {
            return universe.server.GetOffset(gameObject.scene);
        }
        public float squaredUnityVelocityMagnitude()
        {
            return reference_frame.velocity.sqrMagnitude;
        }

        public Vector3d GetRealVelocity()
        {
            return Mathd.UnityToReal(reference_frame.velocity, universe.server.GetVelocity(gameObject.scene));
        }

        public Vector3 GetEnginePosition()
        {
            return transform.position;
        }

        public Vector3 GetEngineVelocity()
        {
            return reference_frame.velocity;
        }

        public Scene GetSceneKey()
        {
            return gameObject.scene;
        }

        public float SquaredEngineVelocityMagnitude()
        {
            throw new System.NotImplementedException();
        }

        public GameObject GetObject()
        {
            return gameObject;
        }

        public void MoveTo(Scene scene)
        {
            SceneManager.MoveGameObjectToScene(gameObject, scene);
        }
    }
}
