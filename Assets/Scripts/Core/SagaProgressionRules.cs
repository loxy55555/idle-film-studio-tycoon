using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Saga ordering and offer eligibility — sequels never appear before prior entries.
/// </summary>
public static class SagaProgressionRules
{
    public static bool HasSaga(MovieConfig cfg) =>
        cfg != null && !string.IsNullOrEmpty(cfg.sagaId);

    public static int GetEffectiveEntryIndex(MovieConfig cfg, MovieConfig[] allMovies = null)
    {
        if (!HasSaga(cfg)) return 0;
        if (cfg.sagaEntryIndex > 0) return cfg.sagaEntryIndex;

        if (allMovies == null) return 1;

        var sagaMovies = GetSagaMovies(cfg.sagaId, allMovies);
        sagaMovies.Sort(CompareSagaOrder);
        for (int i = 0; i < sagaMovies.Count; i++)
        {
            if (sagaMovies[i] == cfg)
                return i + 1;
        }

        return 1;
    }

    public static int GetSagaTotal(MovieConfig cfg, MovieConfig[] allMovies = null)
    {
        if (!HasSaga(cfg)) return 0;
        int catalogCount = allMovies != null ? GetSagaMovies(cfg.sagaId, allMovies).Count : 0;
        return SagaDatabase.GetExpectedSize(cfg.sagaId, catalogCount);
    }

    public static bool ArePreviousSagaEntriesDiscovered(
        MovieConfig cfg,
        IReadOnlyCollection<string> completedKeys,
        MovieConfig[] allMovies)
    {
        if (!HasSaga(cfg)) return true;

        int entryIndex = GetEffectiveEntryIndex(cfg, allMovies);
        if (entryIndex <= 1) return true;

        var completed = ToSet(completedKeys);
        for (int required = 1; required < entryIndex; required++)
        {
            if (!IsSagaEntryDiscovered(cfg.sagaId, required, completed, allMovies))
                return false;
        }

        return true;
    }

    public static bool IsOfferEligible(
        MovieConfig cfg,
        int studioLevel,
        float reputation,
        int cityLevel,
        IReadOnlyCollection<string> completedKeys,
        MovieConfig[] allMovies)
    {
        if (cfg == null) return false;

        var completed = ToSet(completedKeys);
        if (completed.Contains(cfg.name)) return false;
        if (cfg.unlockStudioLevel > studioLevel) return false;
        if (!CityProgressionRules.IsMovieUnlocked(cfg, cityLevel)) return false;
        if (cfg.unlockReputation > 0 && reputation < cfg.unlockReputation) return false;
        if (!ArePreviousSagaEntriesDiscovered(cfg, completedKeys, allMovies)) return false;

        return true;
    }

    public static int CountDiscoveredInSaga(
        string sagaId,
        IReadOnlyCollection<string> completedKeys,
        MovieConfig[] allMovies)
    {
        if (string.IsNullOrEmpty(sagaId) || allMovies == null) return 0;

        var completed = ToSet(completedKeys);
        int count = 0;
        foreach (var m in allMovies)
        {
            if (m == null || m.sagaId != sagaId) continue;
            if (completed.Contains(m.name)) count++;
        }

        return count;
    }

    /// <summary>Card label for discovered saga entries: "2/4" or "4/4 COMPLETADA".</summary>
    public static string GetCardSagaProgressLabel(
        MovieConfig cfg,
        IReadOnlyCollection<string> completedKeys,
        MovieConfig[] allMovies)
    {
        if (!HasSaga(cfg)) return "";

        int total = GetSagaTotal(cfg, allMovies);
        if (total <= 0) total = 1;

        int entry = GetEffectiveEntryIndex(cfg, allMovies);
        int discovered = CountDiscoveredInSaga(cfg.sagaId, completedKeys, allMovies);

        if (discovered >= total)
            return $"{total}/{total} COMPLETADA";

        return $"{entry}/{total}";
    }

    public static string GetSagaBlockReason(MovieConfig cfg, MovieConfig[] allMovies)
    {
        if (!HasSaga(cfg)) return null;

        int entryIndex = GetEffectiveEntryIndex(cfg, allMovies);
        if (entryIndex <= 1) return null;

        return $"Completa la entrada {entryIndex - 1} de la saga primero";
    }

    static bool IsSagaEntryDiscovered(
        string sagaId,
        int entryIndex,
        HashSet<string> completed,
        MovieConfig[] allMovies)
    {
        if (allMovies == null) return false;

        bool foundEntry = false;
        foreach (var m in allMovies)
        {
            if (m == null || m.sagaId != sagaId) continue;
            if (GetEffectiveEntryIndex(m, allMovies) != entryIndex) continue;
            foundEntry = true;
            if (completed.Contains(m.name)) return true;
            return false;
        }

        return !foundEntry;
    }

    static List<MovieConfig> GetSagaMovies(string sagaId, MovieConfig[] allMovies)
    {
        var list = new List<MovieConfig>();
        if (allMovies == null || string.IsNullOrEmpty(sagaId)) return list;

        foreach (var m in allMovies)
        {
            if (m != null && m.sagaId == sagaId)
                list.Add(m);
        }

        return list;
    }

    static int CompareSagaOrder(MovieConfig a, MovieConfig b)
    {
        int indexCmp = a.sagaEntryIndex.CompareTo(b.sagaEntryIndex);
        if (indexCmp != 0) return indexCmp;

        int cityCmp = CityProgressionRules.GetMovieRequiredCity(a)
            .CompareTo(CityProgressionRules.GetMovieRequiredCity(b));
        if (cityCmp != 0) return cityCmp;

        return string.Compare(a.name, b.name, System.StringComparison.Ordinal);
    }

    static HashSet<string> ToSet(IReadOnlyCollection<string> keys) =>
        keys != null ? new HashSet<string>(keys) : new HashSet<string>();
}
