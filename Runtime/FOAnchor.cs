using UnityEngine;

namespace FishNet.FloatingOrigin
{
    public class FOAnchor : MonoBehaviour
    {
        [Tooltip("The object will move precisely to the anchored position, if it is not Vector3d.zero")]
        public Vector3d anchoredPosition;
        public void MoveToAnchor()
        {
            transform.position = FOManager.instance.RealToUnity(anchoredPosition, gameObject.scene);
        }
    }
}
