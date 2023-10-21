using FishNet.Connection;
using FishNet.FloatingOrigin.Types;
using FishNet.Object;
using FishNet.Observing;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FishNet.FloatingOrigin.Observing
{
    /// <summary>
    /// When this observer condition is placed on an object, a View must be within the same floating origin group to see the object.
    /// </summary>
    [CreateAssetMenu(menuName = "FishNet/Observers/Floating Origin Condition", fileName = "New Floating Origin Condition")]
    public class FOCondition : ObserverCondition
    {
        private bool registered = false;
        // bool processed = false;
        /// <summary>
        /// Initializes this script for use.
        /// </summary>
        /// <param name="networkObject"></param>
        public override void Initialize(NetworkObject networkObject)
        {
            base.Initialize(networkObject);
            if (registered)
                return;
            // FOManager.instance.GroupChanged += CheckConditionOnRebase;
            registered = true;
        }

        // private void CheckConditionOnRebase(OffsetGroup group)
        // {
        //     processed = false;
        // }

        /// <summary>
        /// Returns if the object which this condition resides should be visible to connection.
        /// </summary>
        /// <param name="connection">Connection which the condition is being checked for.</param>
        /// <param name="currentlyAdded">True if the connection currently has visibility of this object.</param>
        /// <param name="notProcessed">True if the condition was not processed. This can be used to skip processing for performance. While output as true this condition result assumes the previous ConditionMet value.</param>
        public override bool ConditionMet(NetworkConnection connection, bool currentlyAdded, out bool notProcessed)
        {
            notProcessed = false;
            if (connection == InstanceFinder.NetworkManager.ClientManager.Connection)
                return true;

            return FOManager.instance.GetSceneForConnection(connection).handle == NetworkObject.gameObject.scene.handle;
        }

        /// <summary>
        /// Clones referenced ObserverCondition. This must be populated with your conditions settings.
        /// </summary>
        /// <returns></returns>
        public override ObserverCondition Clone()
        {
            return ScriptableObject.CreateInstance<FOCondition>();
        }

        /// <summary>
        /// How a condition is handled.
        /// </summary>
        /// <returns></returns>
        public override ObserverConditionType GetConditionType() => ObserverConditionType.Timed;
    }
}
