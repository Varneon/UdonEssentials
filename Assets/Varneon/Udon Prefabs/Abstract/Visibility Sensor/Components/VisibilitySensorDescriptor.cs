using UnityEngine;

namespace Varneon.UdonPrefabs.Abstract
{
    /// <summary>
    /// Helper component that overrides the LODGroup, MeshFilter and MeshRenderer, which are required for VisibilitySensor to work
    /// </summary>
    [RequireComponent(typeof(LODGroup))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class VisibilitySensorDescriptor : MonoBehaviour
    {
        /// <summary>
        /// Relative scale of the object in screenspace vertical axis at which the object will become visible/invisible
        /// </summary>
        [Range(0f, 1f)]
        public float TransitionHeight = 0.4f;

        /// <summary>
        /// Target object to encapsulate with the visibility sensor
        /// </summary>
        public GameObject EncapsulationTarget;

        /// <summary>
        /// Has the components overriden by this component been intialized
        /// </summary>
        public bool Initialized;
    }
}
