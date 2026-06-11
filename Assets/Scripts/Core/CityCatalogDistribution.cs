using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Assigns city unlock tiers across the finite movie catalog (300 target, scales to current count).
/// </summary>
public static class CityCatalogDistribution
{
    public static int ComplexityScore(MovieConfig cfg)
    {
        if (cfg == null) return 0;
        return cfg.unlockStudioLevel * 1000
             + (int)Mathf.Min(cfg.cost / 100, 999)
             + (int)(cfg.quality * 10);
    }

    /// <summary>
    /// Returns city level (1–8) for a movie at the given sorted index [0..total-1].
    /// </summary>
    public static int CityForSortedIndex(int sortedIndex, int totalCount)
    {
        if (totalCount <= 0) return 1;

        int[] targets = ContentScaleDatabase.MovieCumulativeByCity;

        // Current catalog (≤180): use exact early-city targets up to 180.
        if (totalCount <= targets[3])
        {
            for (int city = 1; city <= 4; city++)
            {
                int cap = Mathf.Min(targets[city - 1], totalCount);
                if (sortedIndex < cap)
                    return city;
            }
            return 4;
        }

        // Growing catalog (181–300): scale all eight city bands.
        for (int city = 1; city <= targets.Length; city++)
        {
            int scaledTarget = ScaleTarget(targets[city - 1], totalCount);
            if (sortedIndex < scaledTarget)
                return city;
        }

        return targets.Length;
    }

    static int ScaleTarget(int fullTarget, int currentCatalogSize)
    {
        int fullMax = ContentScaleDatabase.MovieCumulativeByCity[ContentScaleDatabase.MovieCumulativeByCity.Length - 1];
        if (currentCatalogSize >= fullMax)
            return fullTarget;

        float ratio = currentCatalogSize / (float)fullMax;
        return Mathf.Max(1, Mathf.RoundToInt(fullTarget * ratio));
    }

    /// <summary>
    /// Sort movies by complexity and assign unlockCityLevel to match distribution targets.
    /// </summary>
    public static void ApplyDistribution(IList<MovieConfig> movies)
    {
        if (movies == null || movies.Count == 0) return;

        var sorted = new List<MovieConfig>(movies);
        sorted.Sort((a, b) => ComplexityScore(a).CompareTo(ComplexityScore(b)));

        for (int i = 0; i < sorted.Count; i++)
        {
            var cfg = sorted[i];
            cfg.unlockCityLevel = CityForSortedIndex(i, sorted.Count);
            SyncCatalogMetaFromCity(cfg);
        }
    }

    static void SyncCatalogMetaFromCity(MovieConfig cfg)
    {
        int city = cfg.unlockCityLevel;
        if (city <= 1)
            cfg.catalogTier = MovieCatalogTier.Tier1;
        else if (city == 2)
            cfg.catalogTier = MovieCatalogTier.Tier2;
        else if (city <= 4)
            cfg.catalogTier = MovieCatalogTier.Tier3;
        else if (city == 5)
            cfg.catalogTier = MovieCatalogTier.Tier4;
        else
            cfg.catalogTier = MovieCatalogTier.Epic;

        if (string.IsNullOrEmpty(cfg.sagaId))
        {
            if (cfg.contentKind == MovieContentKind.Saga)
                cfg.contentKind = MovieContentKind.Standard;
        }
        else
        {
            cfg.contentKind = MovieContentKind.Saga;
        }
    }

    public static Dictionary<int, int> CountByCity(IEnumerable<MovieConfig> movies)
    {
        var counts = new Dictionary<int, int>();
        if (movies == null) return counts;
        foreach (var m in movies)
        {
            if (m == null) continue;
            int city = Mathf.Clamp(m.unlockCityLevel, 1, ContentScaleDatabase.MaxCityLevel);
            counts.TryGetValue(city, out int n);
            counts[city] = n + 1;
        }
        return counts;
    }
}
