using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Eligible offer pool rules. Completed movies are removed permanently.
/// Movies shown in an offer but not chosen return to the pool — no rejected state is stored.
/// Random draws use uniform selection from this pool only (no pity, weighting, or genre bias).
/// </summary>
public static class MovieOfferPoolRules
{
    public static List<MovieConfig> BuildEligiblePool(
        MovieConfig[] allMovies,
        int studioLevel,
        float reputation,
        IReadOnlyCollection<string> completedMovieKeys)
    {
        var pool = new List<MovieConfig>();
        if (allMovies == null || allMovies.Length == 0) return pool;

        int cityLevel = GameHub.Instance?.city?.Level ?? 1;
        foreach (var movie in allMovies)
        {
            if (movie == null) continue;
            if (!SagaProgressionRules.IsOfferEligible(
                    movie, studioLevel, reputation, cityLevel, completedMovieKeys, allMovies))
                continue;
            pool.Add(movie);
        }

        return pool;
    }

    public static bool IsPermanentlyRemovedFromPool(
        MovieConfig cfg,
        IReadOnlyCollection<string> completedKeys) =>
        cfg != null && completedKeys != null && completedKeys.Contains(cfg.name);
}
