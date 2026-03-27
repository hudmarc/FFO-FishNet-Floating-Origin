using UnityEngine;

namespace FloatingOffset.Runtime
{
    public static class FOExtensions
    {
        public static Vector3d GetRealPosition(this OffsetTransform transform){
            OffsetScene scene = FOServiceLocator.registry.GetScened<OffsetScene>(transform.gameObject.scene);
            return Mathd.UnityToReal(transform.transform.position, scene.GetOffset());
        }
        public static Vector3d GetUnityPosition(this OffsetTransform transform){
            OffsetScene scene = FOServiceLocator.registry.GetScened<OffsetScene>(transform.gameObject.scene);
            return Mathd.UnityToReal(transform.transform.position, scene.GetOffset());
        }
        public static Vector3d GetRealPosition(this OffsetView view){
            OffsetScene scene = FOServiceLocator.registry.GetScened<OffsetScene>(view.gameObject.scene);
            return Mathd.UnityToReal(view.transform.position, scene.GetOffset());
        }
        public static Vector3d GetUnityPosition(this OffsetView view){
            OffsetScene scene = FOServiceLocator.registry.GetScened<OffsetScene>(view.gameObject.scene);
            return Mathd.UnityToReal(view.transform.position, scene.GetOffset());
        }
    }
}
