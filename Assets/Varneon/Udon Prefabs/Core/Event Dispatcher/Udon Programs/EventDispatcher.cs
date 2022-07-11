using JetBrains.Annotations;
using UdonSharp;
using VRC.SDKBase;

namespace Varneon.UdonPrefabs.Core
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
        /// Handler behaviours for all of the events
        /// </summary>
        private UdonSharpBehaviour[]
            fixedUpdateHandlers = new UdonSharpBehaviour[0],
            updateHandlers = new UdonSharpBehaviour[0],
            lateUpdateHandlers = new UdonSharpBehaviour[0],
            postLateUpdateHandlers = new UdonSharpBehaviour[0];

        /// <summary>
        /// Quick state for checking if certain event has any handlers to bypass array lenght check
        /// </summary>
        private bool
            hasFixedUpdateHandlers,
            hasUpdateHandlers,
            hasLateUpdateHandlers,
            hasPostLateUpdateHandlers;

        /// <summary>
        /// Quick handler count for bypassing array length check
        /// </summary>
        private int
            fixedUpdateHandlerCount,
            updateHandlerCount,
            lateUpdateHandlerCount,
            postLateUpdateHandlerCount;
        #endregion

        #region Constants
        /// <summary>
        /// Handler method names for each event
        /// </summary>
        private const string
            FixedUpdateHandlerName = "_FixedUpdateHandler",
            UpdateHandlerName = "_UpdateHandler",
            LateUpdateHandlerName = "_LateUpdateHandler",
            PostLateUpdateHandlerName = "_PostLateUpdateHandler";
        #endregion

        #region Private Methods
        /// <summary>
        /// Add handler behaviour to an array
        /// </summary>
        /// <param name="handlers">Existing array of handler behaviours</param>
        /// <param name="newHandler">Handler behaviour to be added</param>
        /// <returns>The new handler behaviour array</returns>
        private UdonSharpBehaviour[] AddHandler(UdonSharpBehaviour[] handlers, UdonSharpBehaviour newHandler)
        {
            if (GetHandlerIndex(handlers, newHandler) >= 0) { return handlers; }

            int handlerCount = handlers.Length;

            UdonSharpBehaviour[] newHandlerList = new UdonSharpBehaviour[handlerCount + 1];

            handlers.CopyTo(newHandlerList, 0);

            newHandlerList[handlerCount] = newHandler;

            return newHandlerList;
        }

        /// <summary>
        /// Remove handler behaviour from an array
        /// </summary>
        /// <param name="handlers">Existing array of handler behaviours</param>
        /// <param name="handlerToRemove">Handler behaviour to be removed</param>
        /// <returns>The new handler behaviour array</returns>
        private UdonSharpBehaviour[] RemoveHandler(UdonSharpBehaviour[] handlers, UdonSharpBehaviour handlerToRemove)
        {
            if (GetHandlerIndex(handlers, handlerToRemove) < 0) { return handlers; }

            int handlerCount = handlers.Length;

            int offset = 0;

            UdonSharpBehaviour[] newHandlerList = new UdonSharpBehaviour[handlerCount - 1];

            for (int i = 0; i < handlerCount - 1; i++)
            {
                if (offset == 0 && handlers[i].Equals(handlerToRemove))
                {
                    offset = 1;
                }

                newHandlerList[i] = handlers[i + offset];
            }

            return newHandlerList;
        }

        /// <summary>
        /// Gets the index of the handler behaviour in the array
        /// </summary>
        /// <param name="handlers">Array of handler behaviours</param>
        /// <param name="handlerToQuery">Handler behaviour to find from the array</param>
        /// <returns>Index of the handler behaviour. Returns -1 if handler behaviour doesn't exist in the array</returns>
        private int GetHandlerIndex(UdonSharpBehaviour[] handlers, UdonSharpBehaviour handlerToQuery)
        {
            for (int i = 0; i < handlers.Length; i++)
            {
                if (handlers[i].Equals(handlerToQuery))
                {
                    return i;
                }
            }

            return -1;
        }
        #endregion

        #region Public API Methods
        /// <summary>
        /// Add handler for FixedUpdate()
        /// </summary>
        /// <param name="udonSharpBehaviour">Handler behaviour with _FixedUpdateHandler() which will be called</param>
        [PublicAPI]
        public void _AddFixedUpdateHandler(UdonSharpBehaviour udonSharpBehaviour)
        {
            hasFixedUpdateHandlers = (fixedUpdateHandlerCount = (fixedUpdateHandlers = AddHandler(fixedUpdateHandlers, udonSharpBehaviour)).Length) > 0;
        }

        /// <summary>
        /// Remove handler for FixedUpdate()
        /// </summary>
        /// <param name="udonSharpBehaviour">Handler behaviour</param>
        [PublicAPI]
        public void _RemoveFixedUpdateHandler(UdonSharpBehaviour udonSharpBehaviour)
        {
            if (fixedUpdateHandlerCount > 0) { hasFixedUpdateHandlers = (fixedUpdateHandlerCount = (fixedUpdateHandlers = RemoveHandler(fixedUpdateHandlers, udonSharpBehaviour)).Length) > 0; }
        }

        /// <summary>
        /// Add handler for Update()
        /// </summary>
        /// <param name="udonSharpBehaviour">Handler behaviour with _UpdateHandler() which will be called</param>
        [PublicAPI]
        public void _AddUpdateHandler(UdonSharpBehaviour udonSharpBehaviour)
        {
            hasUpdateHandlers = (updateHandlerCount = (updateHandlers = AddHandler(updateHandlers, udonSharpBehaviour)).Length) > 0;
        }

        /// <summary>
        /// Remove handler for Update()
        /// </summary>
        /// <param name="udonSharpBehaviour">Handler behaviour</param>
        [PublicAPI]
        public void _RemoveUpdateHandler(UdonSharpBehaviour udonSharpBehaviour)
        {
            if (updateHandlerCount > 0) { hasUpdateHandlers = (updateHandlerCount = (updateHandlers = RemoveHandler(updateHandlers, udonSharpBehaviour)).Length) > 0; }
        }

        /// <summary>
        /// Add handler for LateUpdate()
        /// </summary>
        /// <param name="udonSharpBehaviour">Handler behaviour with _LateUpdateHandler() which will be called</param>
        [PublicAPI]
        public void _AddLateUpdateHandler(UdonSharpBehaviour udonSharpBehaviour)
        {
            hasLateUpdateHandlers = (lateUpdateHandlerCount = (lateUpdateHandlers = AddHandler(lateUpdateHandlers, udonSharpBehaviour)).Length) > 0;
        }

        /// <summary>
        /// Remove handler for LateUpdate()
        /// </summary>
        /// <param name="udonSharpBehaviour">Handler behaviour</param>
        [PublicAPI]
        public void _RemoveLateUpdateHandler(UdonSharpBehaviour udonSharpBehaviour)
        {
            if (lateUpdateHandlerCount > 0) { hasLateUpdateHandlers = (lateUpdateHandlerCount = (lateUpdateHandlers = RemoveHandler(lateUpdateHandlers, udonSharpBehaviour)).Length) > 0; }
        }

        /// <summary>
        /// Add handler for PostLateUpdate()
        /// </summary>
        /// <param name="udonSharpBehaviour">Handler behaviour with _PostLateUpdateHandler() which will be called</param>
        [PublicAPI]
        public void _AddPostLateUpdateHandler(UdonSharpBehaviour udonSharpBehaviour)
        {
            hasPostLateUpdateHandlers = (postLateUpdateHandlerCount = (postLateUpdateHandlers = AddHandler(postLateUpdateHandlers, udonSharpBehaviour)).Length) > 0;
        }

        /// <summary>
        /// Remove handler for PostLateUpdate()
        /// </summary>
        /// <param name="udonSharpBehaviour">Handler behaviour</param>
        [PublicAPI]
        public void _RemovePostLateUpdateHandler(UdonSharpBehaviour udonSharpBehaviour)
        {
            if (postLateUpdateHandlerCount > 0) { hasPostLateUpdateHandlers = (postLateUpdateHandlerCount = (postLateUpdateHandlers = RemoveHandler(postLateUpdateHandlers, udonSharpBehaviour)).Length) > 0; }
        }
        #endregion

        #region Update Methods
        private void FixedUpdate()
        {
            if (hasFixedUpdateHandlers)
            {
                for (int i = 0; i < fixedUpdateHandlerCount; i++)
                {
                    fixedUpdateHandlers[i].SendCustomEvent(FixedUpdateHandlerName);
                }
            }
        }

        private void Update()
        {
            if (hasUpdateHandlers)
            {
                for (int i = 0; i < updateHandlerCount; i++)
                {
                    updateHandlers[i].SendCustomEvent(UpdateHandlerName);
                }
            }
        }

        private void LateUpdate()
        {
            if (hasLateUpdateHandlers)
            {
                for (int i = 0; i < lateUpdateHandlerCount; i++)
                {
                    lateUpdateHandlers[i].SendCustomEvent(LateUpdateHandlerName);
                }
            }
        }

        public override void PostLateUpdate()
        {
            if (hasPostLateUpdateHandlers)
            {
                for (int i = 0; i < postLateUpdateHandlerCount; i++)
                {
                    postLateUpdateHandlers[i].SendCustomEvent(PostLateUpdateHandlerName);
                }
            }
        }
        #endregion

        #region Player Events
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            // If the player leaving is the local player, disable all handlers to prevent errors
            if (!Utilities.IsValid(player))
            {
                hasFixedUpdateHandlers = false;
                hasUpdateHandlers = false;
                hasLateUpdateHandlers = false;
                hasPostLateUpdateHandlers = false;
            }
        }
        #endregion
    }
}
