using UdonSharp;
using UnityEngine;
using Varneon.UdonPrefabs.Common.PlayerTrackerEnums;
using VRC.SDKBase;

namespace Varneon.UdonPrefabs.Abstract
{
    /// <summary>
    /// Abstract tracker for physical interactions
    /// </summary>
    [DefaultExecutionOrder(-2146483648)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class InteractTracker : UdonSharpBehaviour
    {
        /// <summary>
        /// Type of this tracker
        /// </summary>
        private TrackerType trackerType;

        public void OnTriggerEnter(Collider other)
        {
            if (!Utilities.IsValid(other) || other == null) { return; }

            TouchReceiver receiver = other.GetComponent<TouchReceiver>();

            if(receiver == null) { return; }

            OnInteractTrackerEntered(receiver);
        }

        public void OnTriggerExit(Collider other)
        {
            if (!Utilities.IsValid(other) || other == null) { return; }

            TouchReceiver receiver = other.GetComponent<TouchReceiver>();

            if (receiver == null) { return; }

            OnInteractTrackerExited(receiver);
        }

        private protected virtual void OnInteractTrackerEntered(TouchReceiver receiver)
        {
            receiver._OnInteractTrackerEntered(trackerType, transform);
        }

        private protected virtual void OnInteractTrackerExited(TouchReceiver receiver)
        {
            receiver._OnInteractTrackerExited(trackerType);
        }

        public void SetTrackerType(TrackerType type)
        {
            trackerType = type;
        }
    }
}
