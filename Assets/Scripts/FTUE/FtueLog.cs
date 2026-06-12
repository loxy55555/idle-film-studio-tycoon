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

    public static void CurrentStep(FtueStep step) =>
        Info($"CurrentStep={step}");

    public static void Transition(FtueStep from, FtueStep to, string reason = null)
    {
        string msg = $"Transition={from}->{to}";
        if (!string.IsNullOrEmpty(reason))
            msg += $" reason={reason}";
        Info(msg);
    }

    public static void Complete(string reason) =>
        Info($"Complete={reason}");

    public static void LoadedCompleted(bool completed) =>
        Info($"LoadedCompleted={completed}");

    public static void LoadedStep(int step) =>
        Info($"LoadedStep={step}");

    public static void TutorialStarted() =>
        Info("TutorialStarted=");

    public static void TutorialSkipped() =>
        Info("TutorialSkipped=");

    public static void TutorialCompleted() =>
        Info("TutorialCompleted=");
}
