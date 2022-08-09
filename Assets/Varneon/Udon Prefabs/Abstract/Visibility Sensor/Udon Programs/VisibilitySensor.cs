using UdonSharp;
using UnityEngine;

namespace Varneon.UdonPrefabs.Abstract
{
    /// <summary>
    /// Abstract visibility sensor for detecting if the object has become visible or invisible
    /// </summary>
    /// <remarks>
    /// Highly performant solution for culling canvases, setting a bool on an UdonBehaviour to minimize operations, etc.
    /// </remarks>
    [RequireComponent(typeof(VisibilitySensorDescriptor))] // VisibilitySensorDescriptor overrides the LODGroup, MeshFilter and MeshRenderer components, which are required for VisibilitySensor to work
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class VisibilitySensor : UdonSharpBehaviour
    {
#pragma warning disable IDE1006
        /// <summary>
        /// Method which gets called when the object becomes visible
        /// </summary>
        public abstract void _onBecameVisible();

        /// <summary>
        /// Method which gets called when the objects becomes invisible
        /// </summary>
        public abstract void _onBecameInvisible();
#pragma warning restore IDE1006
    }
}
