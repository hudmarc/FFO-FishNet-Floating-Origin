using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// TODO: This should be capable of merging OffsetScenes that are near eachother, pooling OffsetScenes that are not in use and finding OffsetViews that need to be added to a new scene.
    /// </summary>
    public class OffsetManager : MonoBehaviour
    {
        public const int REBASE_CRITERIA = 8192;
        public const int HYSTERESIS = 0;
        public const int MERGE_CRITERIA = REBASE_CRITERIA / 2;

        /// <summary>
        /// Called when an update should occur. TODO: Potential memory leak spot. Check before release.
        /// </summary>
        public event Action OnUpdate;
        private HashGrid<OffsetScene> scenes = new HashGrid<OffsetScene>(REBASE_CRITERIA);
        private HashGrid<OffsetTransform> transformGrid = new HashGrid<OffsetTransform>(REBASE_CRITERIA);
        private HashSet<OffsetView> rebaseQueue = new HashSet<OffsetView>();
        private bool internalUpdate = true;

        void Start()
        {
            FOServiceLocator.manager = this;
            //TODO: This should throw an error if there is no IOffsetter attached.
            // Or should it just add the default?
            FOServiceLocator.offsetter = GetComponent<IOffsetter>();
        }

        void Update()
        {
            if (internalUpdate)
            {
                Process();
            }
        }

        public void Process()
        {
            OnUpdate?.Invoke();
            UpdateScenes();
        }

        private void UpdateScenes()
        {
            // Find all overlapping OffsetScenes. If there was overlap, both scenes should be offset to an equal offset (the median) and merged.
            HashSet<(OffsetScene,OffsetScene)> scenes = findOverlapping();
        }



        public bool IsQueued(OffsetView view)
        {
            return rebaseQueue.Contains(view);
        }

        public void AddToQueue(OffsetView view)
        {
            rebaseQueue.Add(view);
        }

        private HashSet<(OffsetScene,OffsetScene)> findOverlapping()
        {
            HashSet<(OffsetScene,OffsetScene)> found = new HashSet<(OffsetScene,OffsetScene)>();

            // basically, for each scene if the scene is not yet in found, check if a find any exclude overlaps finds anything

            // TODO implement

            return found;
        }

        internal void RegisterScene(OffsetScene offsetScene)
        {
            throw new NotImplementedException();
        }
    }
}
