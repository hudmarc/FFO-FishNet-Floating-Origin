using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// TODO: This should be capable of doing floating origin on a specific scene, and marking Views that have gone out of range and cannot be reconciled.
    /// In Singleplayer mode, this should throw warnings for FOViews that are out of range if such a thing occurs.
    /// This should also be capable of sending OffsetTransforms and OffsetAnchors that are out of range to sleep and re-adding them when they come back in range.
    /// 
    /// OffsetTransforms that go out of range should also be added to an untracked dictionary on the OffsetManager (if present) but this should NOT throw an error or warning in
    /// singleplayer mode, since this is intended behaviour. I think each OffsetScene should track "sleeping" OffsetTransforms individually, and if any are found then the OffsetManager
    /// can move the sleeping OffsetTransforms to the appropriate target scenes where they will be more in-range.
    /// </summary>
    public class OffsetScene : MonoBehaviour
    {
        private HashSet<OffsetView> views = new HashSet<OffsetView>();
        private OffsetManager manager;
        private IOffsetter offsetter;
        private Vector3d offset;
        void Start()
        {
            // Register this OffsetScene with the Service Locator
            FOServiceLocator.registry.AddScened(this);
            manager = FOServiceLocator.manager;
            offsetter = FOServiceLocator.offsetter;
            manager.RegisterScene(this);
            manager.OnUpdate += UpdateScene;
        }

        private void UpdateScene()
        {
            bool rebased = false;
            foreach (OffsetView view in views)
            {
                break;
                // If the view is not in the Manager's rebase set...
                // If the view is out of bounds, exit this loop early and
                // recalculate this scene's centroid and offset the scene.
            }

            if (rebased)
            {
                foreach (OffsetView view in views)
                {
                    // If a view is still out of bounds, add it to the Manager's set of views to rebase.
                }
            }
        }

        public void Offset()
        {
            Vector3 average = Vector3.zero;
            float count = 0;

            foreach (OffsetView view in views)
            {
                // If the view is not in the Manager's rebase set...
                if (!manager.IsQueued(view))
                {
                    average += view.transform.position;
                    count++;
                }
            }

            average = -average * (1 / count);

            // Double check the sign here.
            offsetter.Offset(gameObject.scene, average);

            offset += (Vector3d)average;

            // This needs to find any OT's in range of the OffsetScene and move them into the OffsetScene. (OV's already are moved into appropriate scenes automatically)

            // Somewhere on the OM, this needs to request whether it overlaps any other OffsetScenes. If there was overlap, both scenes should be offset to an equal offset (the median) and merged.

            // If there was an overlap, we should suspend the entire UpdateScene process until after the merge is complete.
        }

        public Vector3d GetOffset() => offset;

        public void Register(OffsetView view)
        {
            views.Add(view);
        }

        public void Unregister(OffsetView view)
        {
            views.Remove(view);
        }
    }
}
