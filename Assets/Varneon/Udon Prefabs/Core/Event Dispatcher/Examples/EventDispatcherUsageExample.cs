#if !UDONSHARP_COMPILER && UNITY_EDITOR

using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class EventDispatcherUsageExample : UdonSharpBehaviour
{
    // Add a reference to the EventDispatcher in the scene
    [SerializeField]
    private Varneon.UdonPrefabs.Core.EventDispatcher eventDispatcher;

    private void Start()
    {
        // Call the "_Add<Event>Handler(this)" on the dispatcher to add the handler on this behaviour
        eventDispatcher._AddFixedUpdateHandler(this);
        eventDispatcher._AddUpdateHandler(this);
        eventDispatcher._AddLateUpdateHandler(this);
        eventDispatcher._AddPostLateUpdateHandler(this);
    }

    public void _FixedUpdateHandler()
    {
        Debug.Log("_FixedUpdateHandler()");

        // Call the "_Remove<Event>Handler(this)" on the dispatcher to remove the handler on this behaviour
        eventDispatcher._RemoveFixedUpdateHandler(this);
    }

    public void _UpdateHandler()
    {
        Debug.Log("_UpdateHandler()");

        eventDispatcher._RemoveUpdateHandler(this);
    }

    public void _LateUpdateHandler()
    {
        Debug.Log("_LateUpdateHandler()");

        eventDispatcher._RemoveLateUpdateHandler(this);
    }

    public void _PostLateUpdateHandler()
    {
        Debug.Log("_PostLateUpdateHandler()");

        eventDispatcher._RemovePostLateUpdateHandler(this);
    }
}

#endif