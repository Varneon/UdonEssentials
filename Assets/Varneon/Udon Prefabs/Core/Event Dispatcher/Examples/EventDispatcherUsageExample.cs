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
        // Call the "_Add<Event>Delegate(this)" on the dispatcher to add the delegate on this behaviour
        eventDispatcher._AddFixedUpdateDelegate(this);
        eventDispatcher._AddUpdateDelegate(this);
        eventDispatcher._AddLateUpdateDelegate(this);
        eventDispatcher._AddPostLateUpdateDelegate(this);
    }

    public void _FixedUpdateDelegate()
    {
        Debug.Log("_FixedUpdateDelegate()");

        // Call the "_Remove<Event>Delegate(this)" on the dispatcher to remove the delegate on this behaviour
        eventDispatcher._RemoveFixedUpdateDelegate(this);
    }

    public void _UpdateDelegate()
    {
        Debug.Log("_UpdateDelegate()");

        eventDispatcher._RemoveUpdateDelegate(this);
    }

    public void _LateUpdateDelegate()
    {
        Debug.Log("_LateUpdateDelegate()");

        eventDispatcher._RemoveLateUpdateDelegate(this);
    }

    public void _PostLateUpdateDelegate()
    {
        Debug.Log("_PostLateUpdateDelegate()");

        eventDispatcher._RemovePostLateUpdateDelegate(this);
    }
}

#endif