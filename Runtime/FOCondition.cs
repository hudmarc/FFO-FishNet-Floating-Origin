using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Observing;
using FishNet.Utility.Extension;
using UnityEngine;

namespace FishNet.FloatingOrigin.Observing
{
    /// <summary>
    /// When this observer condition is placed on an object, a client must be within the same floating origin group to view the object.
    /// </summary>
    [CreateAssetMenu(menuName = "FishNet/Observers/Floating Origin Condition", fileName = "New Floating Origin Condition")]
    public class FOCondition : ObserverCondition
    {
        #region Serialized.
        [Tooltip("How often this condition may change for a connection. This prevents objects from appearing and disappearing rapidly. A value of 0f will cause the object the update quickly as possible while any other value will be used as a delay.")]
        [Range(0f, 60f)]
        [SerializeField]
        private float _updateFrequency;
        /// <summary>
        /// How often this condition may change for a connection. This prevents objects from appearing and disappearing rapidly. A value of 0f will cause the object the update quickly as possible while any other value will be used as a delay.
        /// </summary>
        public float UpdateFrequency { get => _updateFrequency; set => _updateFrequency = value; }
        #endregion

        #region Private.
        /// <summary>
        /// Tracks when connections may be updated for this object.
        /// </summary>
        private Dictionary<NetworkConnection, float> _timedUpdates = new Dictionary<NetworkConnection, float>();
        #endregion

        public void ConditionConstructor(float updateFrequency) => _updateFrequency = updateFrequency;

        /// <summary>
        /// Returns if the object which this condition resides should be visible to connection.
        /// </summary>
        /// <param name="connection">Connection which the condition is being checked for.</param>
        /// <param name="currentlyAdded">True if the connection currently has visibility of this object.</param>
        /// <param name="notProcessed">True if the condition was not processed. This can be used to skip processing for performance. While output as true this condition result assumes the previous ConditionMet value.</param>
        public override bool ConditionMet(NetworkConnection connection, bool currentlyAdded, out bool notProcessed)
        {
            if (_updateFrequency > 0f)
            {
                float nextAllowedUpdate;
                float currentTime = Time.time;
                if (!_timedUpdates.TryGetValueIL2CPP(connection, out nextAllowedUpdate))
                {
                    _timedUpdates[connection] = (currentTime + _updateFrequency);
                }
                else
                {
                    //Not enough time to process again.
                    if (currentTime < nextAllowedUpdate)
                    {
                        notProcessed = true;
                        //The return does not really matter since notProcessed is returned.
                        return false;
                    }
                    //Can process again.
                    else
                    {
                        _timedUpdates[connection] = (currentTime + _updateFrequency);
                    }
                }
            }
            notProcessed = false;
            return FOManager.instance.localObserver.gameObject.scene == base.NetworkObject.gameObject.scene;
        }

        /// <summary>
        /// True if the condition requires regular updates.
        /// </summary>
        /// <returns></returns>
        public override bool Timed() => true;

        /// <summary>
        /// Clones referenced ObserverCondition. This must be populated with your conditions settings.
        /// </summary>
        /// <returns></returns>
        public override ObserverCondition Clone()
        {
            FOCondition copy = ScriptableObject.CreateInstance<FOCondition>();
            //copy.ConditionConstructor(_synchronizeScene);
            copy.ConditionConstructor(_updateFrequency);
            return copy;
        }

    }
}
