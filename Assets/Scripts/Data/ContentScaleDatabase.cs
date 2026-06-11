using UnityEngine;

/// <summary>
/// Target scale for finite catalog content (movies, contracts, upgrades).
/// Used by distribution tools and progression UI — not hard balance numbers.
/// </summary>
public static class ContentScaleDatabase
{
    public const int TargetMovieCount = 300;

    public const int TargetContractCountMin = 60;
    public const int TargetContractCountMax = 80;

    public const int TargetUpgradeCountMin = 60;
    public const int TargetUpgradeCountMax = 80;

    /// <summary>Cumulative movie count unlocked at each city (1–8).</summary>
    public static readonly int[] MovieCumulativeByCity = { 50, 95, 140, 180, 215, 245, 275, 300 };

    /// <summary>Target cumulative contracts per city band (for 70-contract plan).</summary>
    public static readonly int[] ContractCumulativeTarget = { 8, 16, 26, 38, 50, 60, 70, 70 };

    /// <summary>Target cumulative upgrades per city band (for 70-upgrade plan).</summary>
    public static readonly int[] UpgradeCumulativeTarget = { 7, 14, 22, 30, 40, 52, 62, 70 };

    public static int MaxCityLevel => MovieCumulativeByCity.Length;

    public static int GetMovieTargetForCity(int cityLevel)
    {
        cityLevel = Mathf.Clamp(cityLevel, 1, MaxCityLevel);
        return MovieCumulativeByCity[cityLevel - 1];
    }

    public static float CollectionPercentAtCity(int cityLevel, int currentCatalogSize)
    {
        int target = GetMovieTargetForCity(cityLevel);
        int cap = Mathf.Min(target, currentCatalogSize > 0 ? currentCatalogSize : TargetMovieCount);
        return cap / (float)TargetMovieCount * 100f;
    }
}
