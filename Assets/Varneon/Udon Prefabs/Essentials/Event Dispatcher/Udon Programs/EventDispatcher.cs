using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Varneon.UdonPrefabs.Essentials
{
    /// <summary>
    /// Dispatcher for following update events: Fixed Update, Update, LateUpdate, PostLateUpdate
    /// </summary>
    /// <remarks>
    /// Default execution order of the dispatcher is 0
    /// </remarks>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class EventDispatcher : UdonSharpBehaviour
    {
        #region Private Variables
        /// <summary>
        /// Delegate behaviours for all of the events
        /// </summary>
        private Component[]
            fixedUpdateDelegates = new Component[0],
            updateDelegates = new Component[0],
            lateUpdateDelegates = new Component[0],
            postLateUpdateDelegates = new Component[0];

        /// <summary>
        /// Quick state for checking if certain event has any delegates to bypass array lenght check
        /// </summary>
        private bool
            hasFixedUpdateDelegates,
            hasUpdateDelegates,
            hasLateUpdateDelegates,
            hasPostLateUpdateDelegates;

        /// <summary>
        /// Quick delegate count for bypassing array length check
        /// </summary>
        private int
            fixedUpdateDelegateCount,
            updateDelegateCount,
            lateUpdateDelegateCount,
            postLateUpdateDelegateCount;
        #endregion

        #region Constants
        /// <summary>
        /// Delegate method names for each event
        /// </summary>
        private const string
            FixedUpdateEvent = "_FixedUpdateDelegate",
            UpdateEvent = "_UpdateDelegate",
            LateUpdateEvent = "_LateUpdateDelegate",
            PostLateUpdateEvent = "_PostLateUpdateDelegate";
        #endregion

        #region Private Methods
        /// <summary>
        /// Add delegate behaviour to an array
        /// </summary>
        /// <param name="delegates">Existing array of delegate behaviours</param>
        /// <param name="newDelegate">Delegate behaviour to be added</param>
        /// <returns>The new delegate behaviour array</returns>
        private Component[] AddDelegate(Component[] delegates, UdonSharpBehaviour newDelegate)
        {
            if (GetDelegateIndex(delegates, newDelegate) >= 0) { return delegates; }

            int delegateCount = delegates.Length;

            Component[] newDelegateList = new UdonSharpBehaviour[delegateCount + 1];

            delegates.CopyTo(newDelegateList, 0);

            newDelegateList[delegateCount] = newDelegate;

            return newDelegateList;
        }

        /// <summary>
        /// Remove delegate behaviour from an array
        /// </summary>
        /// <param name="delegates">Existing array of delegate behaviours</param>
        /// <param name="delegateToRemove">Delegate behaviour to be removed</param>
        /// <returns>The new delegate behaviour array</returns>
        private Component[] RemoveDelegate(Component[] delegates, UdonSharpBehaviour delegateToRemove)
        {
            if(GetDelegateIndex(delegates, delegateToRemove) < 0) { return delegates; }

            int delegateCount = delegates.Length;

            int offset = 0;

            Component[] newDelegateList = new UdonSharpBehaviour[delegateCount - 1];

            for(int i = 0; i < delegateCount - 1; i++)
            {
                if (offset == 0 && delegates[i].Equals(delegateToRemove))
                {
                    offset = 1;
                }

                newDelegateList[i] = delegates[i + offset];
            }

            return newDelegateList;
        }

        /// <summary>
        /// Gets the index of the delegate behaviour in the array
        /// </summary>
        /// <param name="delegates">Array of delegate behaviours</param>
        /// <param name="delegateToQuery">Delegate behaviour to find from the array</param>
        /// <returns>Index of the delegate behaviour. Returns -1 if delegate behaviour doesn't exist in the array</returns>
        private int GetDelegateIndex(Component[] delegates, UdonSharpBehaviour delegateToQuery)
        {
            for(int i = 0; i < delegates.Length; i++)
            {
                if (delegates[i].Equals(delegateToQuery))
                {
                    return i;
                }
            }

            return -1;
        }
        #endregion

        #region Public API Methods
        /// <summary>
        /// Add delegate for FixedUpdate()
        /// </summary>
        /// <param name="udonSharpBehaviour">Delegate behaviour with _FixedUpdateDelegate() which will be called</param>
        [PublicAPI]
        public void _AddFixedUpdateDelegate(UdonSharpBehaviour udonSharpBehaviour)
        {
            hasFixedUpdateDelegates = (fixedUpdateDelegateCount = (fixedUpdateDelegates = AddDelegate(fixedUpdateDelegates, udonSharpBehaviour)).Length) > 0;
        }

        /// <summary>
        /// Remove delegate for FixedUpdate()
        /// </summary>
        /// <param name="udonSharpBehaviour">Delegate behaviour</param>
        [PublicAPI]
        public void _RemoveFixedUpdateDelegate(UdonSharpBehaviour udonSharpBehaviour)
        {
            if (fixedUpdateDelegateCount > 0) { hasFixedUpdateDelegates = (fixedUpdateDelegateCount = (fixedUpdateDelegates = RemoveDelegate(fixedUpdateDelegates, udonSharpBehaviour)).Length) > 0; }
        }

        /// <summary>
        /// Add delegate for Update()
        /// </summary>
        /// <param name="udonSharpBehaviour">Delegate behaviour with _UpdateDelegate() which will be called</param>
        [PublicAPI]
        public void _AddUpdateDelegate(UdonSharpBehaviour udonSharpBehaviour)
        {
            hasUpdateDelegates = (updateDelegateCount = (updateDelegates = AddDelegate(updateDelegates, udonSharpBehaviour)).Length) > 0;
        }

        /// <summary>
        /// Remove delegate for Update()
        /// </summary>
        /// <param name="udonSharpBehaviour">Delegate behaviour</param>
        [PublicAPI]
        public void _RemoveUpdateDelegate(UdonSharpBehaviour udonSharpBehaviour)
        {
            if (updateDelegateCount > 0) { hasUpdateDelegates = (updateDelegateCount = (updateDelegates = RemoveDelegate(updateDelegates, udonSharpBehaviour)).Length) > 0; }
        }

        /// <summary>
        /// Add delegate for LateUpdate()
        /// </summary>
        /// <param name="udonSharpBehaviour">Delegate behaviour with _LateUpdateDelegate() which will be called</param>
        [PublicAPI]
        public void _AddLateUpdateDelegate(UdonSharpBehaviour udonSharpBehaviour)
        {
            hasLateUpdateDelegates = (lateUpdateDelegateCount = (lateUpdateDelegates = AddDelegate(lateUpdateDelegates, udonSharpBehaviour)).Length) > 0;
        }

        /// <summary>
        /// Remove delegate for LateUpdate()
        /// </summary>
        /// <param name="udonSharpBehaviour">Delegate behaviour</param>
        [PublicAPI]
        public void _RemoveLateUpdateDelegate(UdonSharpBehaviour udonSharpBehaviour)
        {
            if (lateUpdateDelegateCount > 0) { hasLateUpdateDelegates = (lateUpdateDelegateCount = (lateUpdateDelegates = RemoveDelegate(lateUpdateDelegates, udonSharpBehaviour)).Length) > 0; }
        }

        /// <summary>
        /// Add delegate for PostLateUpdate()
        /// </summary>
        /// <param name="udonSharpBehaviour">Delegate behaviour with _PostLateUpdateDelegate() which will be called</param>
        [PublicAPI]
        public void _AddPostLateUpdateDelegate(UdonSharpBehaviour udonSharpBehaviour)
        {
            hasPostLateUpdateDelegates = (postLateUpdateDelegateCount = (postLateUpdateDelegates = AddDelegate(postLateUpdateDelegates, udonSharpBehaviour)).Length) > 0;
        }

        /// <summary>
        /// Remove delegate for PostLateUpdate()
        /// </summary>
        /// <param name="udonSharpBehaviour">Delegate behaviour</param>
        [PublicAPI]
        public void _RemovePostLateUpdateDelegate(UdonSharpBehaviour udonSharpBehaviour)
        {
            if (postLateUpdateDelegateCount > 0) { hasPostLateUpdateDelegates = (postLateUpdateDelegateCount = (postLateUpdateDelegates = RemoveDelegate(postLateUpdateDelegates, udonSharpBehaviour)).Length) > 0; }
        }
        #endregion

        #region Update Methods
        private void FixedUpdate()
        {
            if (hasFixedUpdateDelegates)
            {
                for(int i = 0; i < fixedUpdateDelegateCount; i++)
                {
                    ((UdonSharpBehaviour)fixedUpdateDelegates[i]).SendCustomEvent(FixedUpdateEvent);
                }
            }
        }

        private void Update()
        {
            if (hasUpdateDelegates)
            {
                for (int i = 0; i < updateDelegateCount; i++)
                {
                    ((UdonSharpBehaviour)updateDelegates[i]).SendCustomEvent(UpdateEvent);
                }
            }
        }

        private void LateUpdate()
        {
            if (hasLateUpdateDelegates)
            {
                for (int i = 0; i < lateUpdateDelegateCount; i++)
                {
                    ((UdonSharpBehaviour)lateUpdateDelegates[i]).SendCustomEvent(LateUpdateEvent);
                }
            }
        }

        public override void PostLateUpdate()
        {
            if (hasPostLateUpdateDelegates)
            {
                for (int i = 0; i < postLateUpdateDelegateCount; i++)
                {
                    ((UdonSharpBehaviour)postLateUpdateDelegates[i]).SendCustomEvent(PostLateUpdateEvent);
                }
            }
        }
        #endregion

        #region Player Events
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            // If the player leaving is the local player, disable all delegates to prevent errors
            if (!Utilities.IsValid(player))
            {
                hasFixedUpdateDelegates = false;
                hasUpdateDelegates = false;
                hasLateUpdateDelegates = false;
                hasPostLateUpdateDelegates = false;
            }
        }
        #endregion
    }
}
