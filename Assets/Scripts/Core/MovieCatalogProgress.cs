using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks how many movies remain eligible for offers (future catalog expansion hook).
/// </summary>
public static class MovieCatalogProgress
{
    public const int LowCatalogThreshold = 6;

    public struct Snapshot
    {
        public int eligibleTotal;
        public int eligibleRemaining;
        public int completedCount;
        public bool NeedsCatalogExpansion => eligibleRemaining <= LowCatalogThreshold;
    }

    public static Snapshot Evaluate(
        MovieConfig[] allMovies,
        int studioLevel,
        float reputation,
        IReadOnlyCollection<string> completedKeys)
    {
        var completed = completedKeys != null ? new HashSet<string>(completedKeys) : new HashSet<string>();
        int total = 0;
        int remaining = 0;

        if (allMovies != null)
        {
            foreach (var m in allMovies)
            {
                if (m == null) continue;
                if (m.unlockStudioLevel > studioLevel) continue;
                if (GameHub.Instance?.city != null && !GameHub.Instance.city.IsMovieUnlocked(m)) continue;
                if (m.unlockReputation > 0 && reputation < m.unlockReputation) continue;
                total++;
                if (!completed.Contains(m.name))
                    remaining++;
            }
        }

        return new Snapshot
        {
            eligibleTotal = total,
            eligibleRemaining = remaining,
            completedCount = completed.Count,
        };
    }
}
