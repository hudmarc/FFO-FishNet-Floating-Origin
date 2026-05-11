using System.Collections.Generic;
using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    // Loosely based on the Unity Wiki FloatingOrigin script by Peter Stirling
    // URL: http://wiki.unity3d.com/index.php/Floating_Origin
    public class Offsetter : MonoBehaviour, IOffsetter<Scene>
    {
        HashSet<IOffsettable> pending_removal = new HashSet<IOffsettable>();
        private void MoveRootTransforms(Vector3 offset, Scene scene)
        {
            // Debug.Log($"Moving by {offset}");
            var objects = scene.GetRootGameObjects();
            foreach (GameObject g in objects)
            {
                g.transform.position += offset;
            }
        }

        // private void MoveOffsettables(Vector3d old_offset, Vector3d new_offset)
        // {
        //     for (int i = 0; i < offsettables.Count; i++)
        //     {
        //         if (pending_removal.Count > 0 && pending_removal.Contains(offsettables[i]))
        //         {
        //             int lastIndex = offsettables.Count - 1;
        //             offsettables[i] = offsettables[lastIndex];
        //             offsettables.RemoveAt(lastIndex);
        //             i--;
        //             continue;
        //         }
        //         offsettables[i].OnOffset(old_offset, new_offset);
        //     }
        // }

        public void Offset(Vector3d old_offset, Vector3d new_offset, Scene scene)
        {

            Vector3d real_difference = old_offset - new_offset;
            Vector3 difference = Mathd.toVector3(real_difference);

            MoveRootTransforms(difference, scene);

            Vector3 remainder = Mathd.toVector3(real_difference - Mathd.toVector3d(difference));

            if (remainder.sqrMagnitude > 0.0f)
                MoveRootTransforms(remainder, scene);

            // MoveOffsettables(old_offset, new_offset);
        }
    }
}
