using UdonSharp;
using UnityEngine;
using Varneon.UdonPrefabs.Common.PlayerTrackerEnums;

namespace Varneon.UdonPrefabs.Abstract
{
    /// <summary>
    /// Abstract receiver for physical interactions
    /// </summary>
    public abstract class TouchReceiver : UdonSharpBehaviour
    {
        public virtual void _OnInteractTrackerEntered(TrackerType type, Transform tracker) { }

        public virtual void _OnInteractTrackerExited(TrackerType type) { }
    }
}
