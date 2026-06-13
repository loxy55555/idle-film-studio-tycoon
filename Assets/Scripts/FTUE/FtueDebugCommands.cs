#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>Runtime/editor debug commands for FTUE recovery and QA.</summary>
public static class FtueDebugCommands
{
    public static void CompleteFtue()
    {
        EnsureController()?.DebugCompleteFtue();
    }

    public static void ResetFtue()
    {
        EnsureController()?.DebugResetFtue();
    }

    /// <summary>Debug only — clears PlayerPrefs tutorial completion.</summary>
    public static void ResetTutorial()
    {
        FtueState.ResetTutorial();
        EnsureController()?.DebugResetFtue();
    }

    public static void DisableFtue()
    {
        EnsureController()?.DebugDisableFtue();
    }

    public static void EnableFtue()
    {
        EnsureController()?.DebugEnableFtue();
    }

    static FtueController EnsureController()
    {
        if (FtueController.Instance != null)
            return FtueController.Instance;

        var hub = GameHub.Instance ?? Object.FindAnyObjectByType<GameHub>();
        if (hub == null)
        {
            FtueLog.Error("GameHub not found — cannot run FTUE debug command.");
            return null;
        }

        var controller = hub.GetComponent<FtueController>();
        if (controller == null)
            controller = hub.gameObject.AddComponent<FtueController>();

        return controller;
    }

#if UNITY_EDITOR
    [MenuItem("IdleFilm/FTUE Debug/Complete FTUE")]
    static void MenuCompleteFtue()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[FTUE] Enter Play Mode to run FTUE debug commands.");
            return;
        }
        CompleteFtue();
    }

    [MenuItem("IdleFilm/FTUE Debug/Reset FTUE")]
    static void MenuResetFtue()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[FTUE] Enter Play Mode to run FTUE debug commands.");
            return;
        }
        ResetFtue();
    }

    [MenuItem("IdleFilm/FTUE Debug/Disable FTUE")]
    static void MenuDisableFtue()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[FTUE] Enter Play Mode to run FTUE debug commands.");
            return;
        }
        DisableFtue();
    }

    [MenuItem("IdleFilm/FTUE Debug/Enable FTUE")]
    static void MenuEnableFtue()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[FTUE] Enter Play Mode to run FTUE debug commands.");
            return;
        }
        EnableFtue();
    }
#endif
}
