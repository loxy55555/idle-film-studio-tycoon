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
    const string DisabledPrefKey = "idlefilm.ftue.disabled";

    public static bool Completed { get; set; }
    public static FtueStep Step { get; set; } = FtueStep.Welcome;

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

    public static void Reset()
    {
        Completed = false;
        Step = FtueStep.Welcome;
    }

    public static void ApplySave(bool completed, int step)
    {
        Completed = completed;

        if (completed)
        {
            Step = FtueStep.Done;
            FtueLog.LoadedCompleted(true);
            FtueLog.LoadedStep((int)FtueStep.Done);
            FtueLog.State("ApplySave");
            return;
        }

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
    }
}
