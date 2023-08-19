using FishNet.FloatingOrigin.Types;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FishNet.FloatingOrigin
{
    public class FOAnchor : MonoBehaviour, IRealTransform
    {
        [SerializeField] private double x, y, z;
        public Vector3d realPosition
        {
            get => new Vector3d(x, y, z);
            set
            {
                x = value.x;
                y = value.y;
                z = value.z;
            }
        }
        private FOManager manager;
        private bool initialized = false;
        void Start()
        {
            Initialize();
        }
        private void Initialize()
        {
            manager = FOManager.instance;
            manager.GroupChanged += OnRebase;
        }
        private void OnDisable()
        {
            if (manager != null)
                manager.GroupChanged -= OnRebase;
        }
        void OnRebase(OffsetGroup group)
        {
            if (group.scene.handle == gameObject.scene.handle)
                transform.position = manager.RealToUnity(realPosition, gameObject.scene);
        }
    }
}