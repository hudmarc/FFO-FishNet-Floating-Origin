using FishNet.FloatingOrigin.Types;
using UnityEngine;

namespace FishNet.FloatingOrigin
{
    [RequireComponent(typeof(IOffsetter))]
    public class FOSubscene : MonoBehaviour
    {
        /// <summary>
        /// The dimensions, in real space, of the check sphere. This can be useful for rough checks
        /// (i.e. is the player's spaceship near enough to a planet so as to burn up in the atmosphere?)
        /// For more precise readings, you're on your own.
        /// </summary>
        public float collisionCheckRadius = 1000;
        [SerializeField] private Vector3d offset = Vector3d.zero;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private double scale = 1;
        private IOffsetter ioffsetter;
        private void Start()
        {
            ioffsetter = GetComponent<IOffsetter>();
        }

        void FixedUdate()
        {
            if (offset != (FOManager.instance.GetLocalGroup().offset / scale))
            {
                Vector3d difference = offset - (FOManager.instance.GetLocalGroup().offset / scale);

                ioffsetter.Offset(gameObject.scene, (Vector3)difference);

                offset = (FOManager.instance.GetLocalGroup().offset / scale);
            }
        }
        /// <summary>
        /// This is simply a rough check as to whether or not the local FOView is localViewRadius away from any colliders in the subscene (scaled)
        /// </summary>
        /// <returns>
        /// Are we possibly colliding?
        /// </returns>
        public bool IsColliderInRadius()
        {
            return Physics.CheckSphere(FOManager.instance.local.transform.position, collisionCheckRadius / (float)scale);
        }

        public Vector3d toRealSpace(Vector3 position)
        {
            return ((Vector3d)position) * scale;
        }
    }
}
