using UnityEngine;

/// <summary>Main progression tier — City 8 is the final content unlock band.</summary>
public enum CityTier
{
    City1 = 1,
    City2 = 2,
    City3 = 3,
    City4 = 4,
    City5 = 5,
    City6 = 6,
    City7 = 7,
    City8 = 8,
}

public static class CityTierExtensions
{
    public static int ToLevel(this CityTier tier) => (int)tier;

    public static CityTier FromLevel(int level) =>
        (CityTier)Mathf.Clamp(level, (int)CityTier.City1, (int)CityTier.City8);

    public static bool IsMax(this CityTier tier) => tier >= CityTier.City8;

    public static string GetDisplayName(this CityTier tier) =>
        CityLevelDatabase.GetLevel(tier.ToLevel()).displayName;
}
