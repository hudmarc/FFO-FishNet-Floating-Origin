using System.Collections.Generic;
using UnityEngine;

namespace FloatingOffset.Runtime
{
    // Loosely based on the Unity Wiki FloatingOrigin script by Peter Stirling
    // URL: http://wiki.unity3d.com/index.php/Floating_Origin
    public class Offsetter : MonoBehaviour
    {
        HashSet<IOffsettable> pending_removal = new HashSet<IOffsettable>();
        List<IOffsettable> offsettables = new List<IOffsettable>();
        public void RegisterOffsettable(IOffsettable offsettable)
        {
            offsettables.Add(offsettable);
        }

        public void UnregisterOffsettable(IOffsettable offsettable)
        {
            pending_removal.Add(offsettable);
        }

        private void MoveRootTransforms(Vector3 offset)
        {
            Debug.Log($"Moving by {offset}");
            var objects = gameObject.scene.GetRootGameObjects();
            foreach (GameObject g in objects)
            {
                g.transform.position += offset;
            }
        }

        private void MoveOffsettables(Vector3d old_offset, Vector3d new_offset)
        {
            for (int i = 0; i < offsettables.Count; i++)
            {
                if (pending_removal.Count > 0 && pending_removal.Contains(offsettables[i]))
                {
                    int lastIndex = offsettables.Count - 1;
                    offsettables[i] = offsettables[lastIndex];
                    offsettables.RemoveAt(lastIndex);
                    i--;
                    continue;
                }
                offsettables[i].OnOffset(old_offset, new_offset);
            }
        }

        public void Offset(Vector3d old_offset, Vector3d new_offset)
        {

            Vector3d real_difference = old_offset - new_offset;
            Vector3 difference = Mathd.toVector3(real_difference);

            MoveRootTransforms(difference);

            Vector3 remainder = Mathd.toVector3(real_difference - Mathd.toVector3d(difference));

            if (remainder.sqrMagnitude > 0.0f)
                MoveRootTransforms(remainder);

            MoveOffsettables(old_offset, new_offset);
        }
    }
}
