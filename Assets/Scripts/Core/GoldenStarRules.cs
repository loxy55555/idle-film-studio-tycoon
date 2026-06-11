using System;
using UnityEngine;

/// <summary>
/// Golden Star (Estrella de Oro) pipeline architecture.
/// Official flow: Movies → Reputation → Nominations → Present → Golden Star → City.
/// Nomination/presentation balance is intentionally not implemented yet.
/// </summary>
public enum GoldenStarPipelinePhase
{
    None,
    Nominated,
    Presented,
    StarAwarded,
}

[Serializable]
public struct GoldenStarMovieRecord
{
    public string movieKey;
    public GoldenStarPipelinePhase phase;
}

/// <summary>Future save payload — not wired to SaveSystem yet (TODO v4).</summary>
[Serializable]
public class GoldenStarProgressSaveData
{
    public GoldenStarMovieRecord[] pipelineRecords;
    public int totalGoldenStars;
}

public static class GoldenStarRules
{
    public static int GetTotalGoldenStars(GameHub hub) =>
        GetTotalGoldenStars(hub?.prestige);

    public static int GetTotalGoldenStars(PrestigeSystem prestige) =>
        prestige != null ? prestige.oscars : 0;

    /// <summary>TODO: Phase 7+ — reputation and quality gates for nomination.</summary>
    public static bool CanNominateMovie(GameHub hub, MovieConfig movie)
    {
        if (hub == null || movie == null || hub.studio == null) return false;
        if (!hub.studio.IsMovieCompleted(movie)) return false;
        return EvaluateNominationEligibility(hub, movie);
    }

    /// <summary>TODO: Phase 7+ — resolve config and nomination rules by key.</summary>
    public static bool CanNominateMovie(GameHub hub, string movieKey)
    {
        if (hub == null || string.IsNullOrEmpty(movieKey)) return false;
        // TODO: resolve MovieConfig from StudioManager catalog
        return false;
    }

    /// <summary>TODO: Phase 7+ — movie must be nominated and ceremony requirements met.</summary>
    public static bool CanPresentMovie(GameHub hub, MovieConfig movie)
    {
        if (hub == null || movie == null) return false;
        return EvaluatePresentationEligibility(hub, movie);
    }

    /// <summary>TODO: Phase 7+ — award star after successful presentation.</summary>
    public static bool AwardGoldenStar(GameHub hub, MovieConfig movie)
    {
        if (hub == null || movie == null || hub.prestige == null) return false;
        if (!CanPresentMovie(hub, movie)) return false;

        // TODO: Phase 7+ — persist pipeline phase, then grant star (hub.prestige.GainOscar()).
        return false;
    }

    public static bool IsEndgameReached(GameHub hub) =>
        EndgameProgressionRules.IsEndgame(hub);

    public static int GetRequiredStarsForCity(CityTier tier) =>
        CityProgressionRules.GetRequiredStars(tier);

    static bool EvaluateNominationEligibility(GameHub hub, MovieConfig movie)
    {
        _ = hub;
        _ = movie;
        // TODO: Phase 7+ — nomination requirements (reputation thresholds, quotas, etc.).
        return false;
    }

    static bool EvaluatePresentationEligibility(GameHub hub, MovieConfig movie)
    {
        _ = hub;
        _ = movie;
        // TODO: Phase 7+ — requires GoldenStarPipelinePhase.Nominated in persisted state.
        return false;
    }
}
