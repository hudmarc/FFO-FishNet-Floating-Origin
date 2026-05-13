using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    // Loosely based on the Unity Wiki FloatingOrigin script by Peter Stirling
    // URL: http://wiki.unity3d.com/index.php/Floating_Origin
    public class Offsetter : MonoBehaviour
    {
        internal void Offset(Vector3d old_offset, Vector3d new_offset, Scene scene, IOffsettable<Scene>[] offsettables = null)
        {
            OnOffset(old_offset, new_offset, scene, offsettables);
        }
        protected virtual void MoveRootTransforms(Vector3 offset, Scene scene)
        {
            // Debug.Log($"Moving by {offset}");
            var objects = scene.GetRootGameObjects();
            foreach (GameObject g in objects)
            {
                g.transform.position += offset;
            }
        }

        protected virtual void MoveOffsettables(IOffsettable<Scene>[] offsettables, Vector3d old_offset, Vector3d new_offset)
        {
            for (int i = 0; i < offsettables.Length; i++)
            {
                offsettables[i].OnOffset(old_offset, new_offset);
            }
        }
        protected virtual void OnOffset(Vector3d old_offset, Vector3d new_offset, Scene scene, IOffsettable<Scene>[] offsettables)
        {
            Vector3d real_difference = old_offset - new_offset;
            Vector3 difference = Mathd.toVector3(real_difference);

            MoveRootTransforms(difference, scene);

            Vector3 remainder = Mathd.toVector3(real_difference - Mathd.toVector3d(difference));

            if (remainder.sqrMagnitude > 0.0f)
                MoveRootTransforms(remainder, scene);

            if (offsettables != null)
                MoveOffsettables(offsettables, old_offset, new_offset);
        }
    }
}
