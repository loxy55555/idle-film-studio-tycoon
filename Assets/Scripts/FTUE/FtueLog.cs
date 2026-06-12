using UnityEngine;

/// <summary>Diagnostic logging for FTUE (filter console with [FTUE]).</summary>
public static class FtueLog
{
    const string Tag = "[FTUE]";

    public static void Info(string message) => Debug.Log($"{Tag} {message}");

    public static void Warn(string message) => Debug.LogWarning($"{Tag} {message}");

    public static void Error(string message) => Debug.LogError($"{Tag} {message}");

    public static void State(string context)
    {
        Info($"{context} | completed={FtueState.Completed} step={FtueState.Step} disabled={FtueState.Disabled}");
    }
}
