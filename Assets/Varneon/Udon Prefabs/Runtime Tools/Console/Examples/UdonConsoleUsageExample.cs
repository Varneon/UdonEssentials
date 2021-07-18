#if !UDONSHARP_COMPILER && UNITY_EDITOR

using UdonSharp;
using UnityEngine;

public class UdonConsoleUsageExample : UdonSharpBehaviour
{
    #region VARIABLES
    [SerializeField]
    private Varneon.UdonPrefabs.RuntimeTools.UdonConsole Console;

    //Setting up a custom prefix for the UdonSharp class makes it easy to see from which UdonBehaviour the log entries are coming from
    private const string LOG_PREFIX = "[<color=Cyan>YOURCLASSNAMEHERE</color>]: ";
    #endregion

    #region LOGGING EXAMPLE
    private void Start()
    {
        //Instead of Debug.Log or other variants you can now use the proxies below
        Log("This is how you can log normal messages");
        LogWarning("You can use LogWarning for logging warnings");
        LogError("If something goes wrong, you can also use LogError");
    }
    #endregion

    #region PROXY METHODS
    private void Log(string message)
    {
        Debug.Log(message = $"{LOG_PREFIX} {message}");
        if (Console) { Console._Log(message); }
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning(message = $"{LOG_PREFIX} {message}");
        if (Console) { Console._LogWarning(message); }
    }

    private void LogError(string message)
    {
        Debug.LogError(message = $"{LOG_PREFIX} {message}");
        if (Console) { Console._LogError(message); }
    }
    #endregion
}

#endif
