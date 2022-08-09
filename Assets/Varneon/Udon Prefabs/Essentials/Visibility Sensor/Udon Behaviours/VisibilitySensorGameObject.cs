using UnityEngine;
using Varneon.UdonPrefabs.Abstract;

namespace Varneon.UdonPrefabs.Essentials
{
    /// <summary>
    /// Visibility Sensor for activating and deactivating GameObjects
    /// </summary>
    public class VisibilitySensorGameObject : VisibilitySensor
    {
        /// <summary>
        /// GameObject to activate when visible
        /// </summary>
        [SerializeField]
        private GameObject activateWhenVisible;

        /// <summary>
        /// GameObject to activate when invisible
        /// </summary>
        [SerializeField]
        private GameObject activateWhenInvisible;

        /// <summary>
        /// Does the visibility sensor have a GameObject to activate when visible
        /// </summary>
        private bool hasVisibilityGameObject;

        /// <summary>
        /// Does the visibility sensor have a GameObject to activate when invisible
        /// </summary>
        private bool hasInvisibilityGameObject;

        private void Start()
        {
            hasVisibilityGameObject = activateWhenVisible;

            hasInvisibilityGameObject = activateWhenInvisible;
        }

        public override void _onBecameVisible()
        {
            if (hasVisibilityGameObject) { activateWhenVisible.SetActive(true); }
            if (hasInvisibilityGameObject) { activateWhenInvisible.SetActive(false); }
        }

        public override void _onBecameInvisible()
        {
            if (hasInvisibilityGameObject) { activateWhenInvisible.SetActive(true); }
            if (hasVisibilityGameObject) { activateWhenVisible.SetActive(false); }
        }
    }
}

