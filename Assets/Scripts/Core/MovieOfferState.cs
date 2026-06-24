using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Persistent production offer slots — survives tab switches (Phase 7.3).</summary>
public static class MovieOfferState
{
    public const int SlotCount = MovieOfferPicker.OfferSlotCount;

    static readonly string[] _keys = new string[SlotCount];

    /// <summary>Fired when offer slot data changes (consume, refill, reroll, load).</summary>
    public static event Action OnOffersChanged;

    public static bool HasActiveOffers
    {
        get
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (!string.IsNullOrEmpty(_keys[i]))
                    return true;
            }
            return false;
        }
    }

    public static void Clear()
    {
        for (int i = 0; i < SlotCount; i++)
            _keys[i] = null;
    }

    public static void ApplySave(string[] saved)
    {
        Clear();
        if (saved == null) return;

        for (int i = 0; i < SlotCount && i < saved.Length; i++)
        {
            string key = saved[i];
            _keys[i] = string.IsNullOrEmpty(key) ? null : key;
        }
    }

    public static string[] GetSaveData()
    {
        var copy = new string[SlotCount];
        for (int i = 0; i < SlotCount; i++)
            copy[i] = _keys[i] ?? string.Empty;
        return copy;
    }

    public static void SyncOffers(
        MovieConfig[] catalog,
        int studioLevel,
        float reputation,
        IReadOnlyCollection<string> completedKeys,
        IEnumerable<MovieConfig> activeProductions = null)
    {
        if (HasActiveOffers)
        {
            PruneAndRefill(catalog, studioLevel, reputation, completedKeys, activeProductions);
            return;
        }

        RegenerateAll(catalog, studioLevel, reputation, completedKeys, activeProductions);
    }

    /// <summary>Legacy alias — syncs offer state without firing UI events.</summary>
    public static void EnsureOffers(
        MovieConfig[] catalog,
        int studioLevel,
        float reputation,
        IReadOnlyCollection<string> completedKeys,
        IEnumerable<MovieConfig> activeProductions = null) =>
        SyncOffers(catalog, studioLevel, reputation, completedKeys, activeProductions);

    public static void ForceRegenerateAll(
        MovieConfig[] catalog,
        int studioLevel,
        float reputation,
        IReadOnlyCollection<string> completedKeys,
        IEnumerable<MovieConfig> activeProductions = null)
    {
        RegenerateAll(catalog, studioLevel, reputation, completedKeys, activeProductions);
        Debug.Log("[Offers] RefreshGenerated");
        RaiseOffersChanged();
    }

    /// <summary>
    /// Production confirmed — all current offers are discarded and 3 new ones are rolled (Phase 8.3).
    /// </summary>
    public static void NotifyProductionStarted(
        MovieConfig config,
        MovieConfig[] catalog,
        int studioLevel,
        float reputation,
        IReadOnlyCollection<string> completedKeys,
        IEnumerable<MovieConfig> activeProductions)
    {
        if (config == null) return;

        RegenerateAll(catalog, studioLevel, reputation, completedKeys, activeProductions);
        Debug.Log($"[Production] OffersConsumedAll Selected={config.movieName} OffersRebuilt={CountFilled()}");
        RaiseOffersChanged();
    }

    public static void OnMovieCompleted(
        MovieConfig[] catalog,
        int studioLevel,
        float reputation,
        IReadOnlyCollection<string> completedKeys,
        IEnumerable<MovieConfig> activeProductions)
    {
        PruneAndRefill(catalog, studioLevel, reputation, completedKeys, activeProductions);
        RaiseOffersChanged();
    }

    static void PruneAndRefill(
        MovieConfig[] catalog,
        int studioLevel,
        float reputation,
        IReadOnlyCollection<string> completedKeys,
        IEnumerable<MovieConfig> activeProductions = null)
    {
        PruneInvalid(catalog, completedKeys, activeProductions);
        RefillToFull(catalog, studioLevel, reputation, completedKeys, activeProductions);
    }

    static void RegenerateAll(
        MovieConfig[] catalog,
        int studioLevel,
        float reputation,
        IReadOnlyCollection<string> completedKeys,
        IEnumerable<MovieConfig> activeProductions = null)
    {
        Clear();
        var exclude = BuildExcludeSet(activeProductions);
        var picks = MovieOfferPicker.PickMany(
            catalog, studioLevel, reputation, completedKeys, SlotCount, exclude);
        for (int i = 0; i < picks.Count; i++)
            _keys[i] = picks[i].name;
    }

    static void PruneInvalid(
        MovieConfig[] catalog,
        IReadOnlyCollection<string> completedKeys,
        IEnumerable<MovieConfig> activeProductions = null)
    {
        var activeKeys = new HashSet<string>();
        if (activeProductions != null)
        {
            foreach (var cfg in activeProductions)
            {
                if (cfg != null && !string.IsNullOrEmpty(cfg.name))
                    activeKeys.Add(cfg.name);
            }
        }

        for (int i = 0; i < SlotCount; i++)
        {
            if (string.IsNullOrEmpty(_keys[i])) continue;

            if (activeKeys.Contains(_keys[i]))
            {
                _keys[i] = null;
                continue;
            }

            var cfg = FindConfig(catalog, _keys[i]);
            if (cfg == null ||
                MovieOfferPoolRules.IsPermanentlyRemovedFromPool(cfg, completedKeys))
            {
                _keys[i] = null;
            }
        }
    }

    static void RefillToFull(
        MovieConfig[] catalog,
        int studioLevel,
        float reputation,
        IReadOnlyCollection<string> completedKeys,
        IEnumerable<MovieConfig> activeProductions)
    {
        var exclude = BuildExcludeSet(activeProductions);

        while (CountFilled() < SlotCount)
        {
            var pick = MovieOfferPicker.PickOne(
                catalog, studioLevel, reputation, completedKeys, exclude);
            if (pick == null) break;

            PutInFirstEmpty(pick.name);
            exclude.Add(pick.name);
        }
    }

    static HashSet<string> BuildExcludeSet(IEnumerable<MovieConfig> activeProductions)
    {
        var exclude = new HashSet<string>();
        for (int i = 0; i < SlotCount; i++)
        {
            if (!string.IsNullOrEmpty(_keys[i]))
                exclude.Add(_keys[i]);
        }

        if (activeProductions != null)
        {
            foreach (var cfg in activeProductions)
            {
                if (cfg != null && !string.IsNullOrEmpty(cfg.name))
                    exclude.Add(cfg.name);
            }
        }

        return exclude;
    }

    static void PutInFirstEmpty(string key)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (string.IsNullOrEmpty(_keys[i]))
            {
                _keys[i] = key;
                return;
            }
        }
    }

    static int CountFilled()
    {
        int n = 0;
        for (int i = 0; i < SlotCount; i++)
        {
            if (!string.IsNullOrEmpty(_keys[i]))
                n++;
        }
        return n;
    }

    public static List<MovieConfig> ResolveOffers(MovieConfig[] catalog)
    {
        var list = new List<MovieConfig>(SlotCount);
        for (int i = 0; i < SlotCount; i++)
        {
            if (string.IsNullOrEmpty(_keys[i]))
            {
                list.Add(null);
                continue;
            }
            list.Add(FindConfig(catalog, _keys[i]));
        }
        return list;
    }

    static MovieConfig FindConfig(MovieConfig[] catalog, string key)
    {
        if (catalog == null || string.IsNullOrEmpty(key)) return null;
        foreach (var cfg in catalog)
        {
            if (cfg != null && cfg.name == key)
                return cfg;
        }
        return null;
    }

    static void RaiseOffersChanged() => OnOffersChanged?.Invoke();
}
