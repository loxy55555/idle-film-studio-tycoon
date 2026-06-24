using UnityEngine;

/// <summary>FTUE progression steps (Phase 8.4).</summary>
public enum FtueStep
{
    Welcome                = 0,
    ProductionGuide        = 1,
    StudioDuringProduction = 2,
    FirstMovieComplete     = 3,
    AwaitCompletion        = 4,
    Done                   = 5,
}

public static class FtueState
{
    const string DisabledPrefKey  = "idlefilm.ftue.disabled";
    const string CompletedPrefKey = "idlefilm.ftue.completed";

    public static bool Completed { get; set; }
    public static FtueStep Step { get; set; } = FtueStep.Welcome;

    static FtueState()
    {
        LoadFromPlayerPrefs();
    }

    /// <summary>FASE 19 — Pre-beta: skip FTUE popups; player enters gameplay directly.</summary>
    public const bool DisabledForPreBeta = true;

    /// <summary>Runtime kill-switch (debug / recovery). Not stored in game save.</summary>
    public static bool Disabled
    {
        get => PlayerPrefs.GetInt(DisabledPrefKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(DisabledPrefKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool IsPersistedCompleted =>
        PlayerPrefs.GetInt(CompletedPrefKey, 0) == 1;

    public static void LoadFromPlayerPrefs()
    {
        if (!IsPersistedCompleted) return;

        Completed = true;
        Step = FtueStep.Done;
    }

    public static void Reset()
    {
        if (DisabledForPreBeta || IsPersistedCompleted)
        {
            Completed = true;
            Step = FtueStep.Done;
            return;
        }

        Completed = false;
        Step = FtueStep.Welcome;
    }

    public static void ApplySave(bool completed, int step)
    {
        if (DisabledForPreBeta || IsPersistedCompleted || completed)
        {
            Completed = true;
            Step = FtueStep.Done;
            PersistCompleted();
            FtueLog.LoadedCompleted(true);
            FtueLog.LoadedStep((int)FtueStep.Done);
            FtueLog.State("ApplySave");
            return;
        }

        Completed = false;
        int clamped = Mathf.Clamp(step, (int)FtueStep.Welcome, (int)FtueStep.AwaitCompletion);
        Step = (FtueStep)clamped;

        FtueLog.LoadedCompleted(false);
        FtueLog.LoadedStep((int)Step);
        FtueLog.State("ApplySave");
    }

    public static (bool completed, int step) GetSaveData() => (Completed, (int)Step);

    public static void MarkCompleted()
    {
        Completed = true;
        Step = FtueStep.Done;
        PersistCompleted();
    }

    static void PersistCompleted()
    {
        PlayerPrefs.SetInt(CompletedPrefKey, 1);
        PlayerPrefs.Save();
    }

    /// <summary>Debug only — clears persisted completion and restarts the tutorial.</summary>
    public static void ResetTutorial()
    {
        Completed = false;
        Step = FtueStep.Welcome;
        PlayerPrefs.DeleteKey(CompletedPrefKey);
        PlayerPrefs.Save();
    }
}
