using FishNet.Object;
using UnityEngine;

namespace FishNet.FloatingOrigin
{
    public class FOView : FOObject
    {
        internal override void Initialize() => FOManager.instance.RegisterView(this);
        internal override void Deinitialize() => FOManager.instance.UnregisterView(this);
        public void SetRealPositionApproximate(Vector3d position)
        {
            transform.position = FOManager.instance.RealToUnity(position, gameObject.scene);
        }
    }
}