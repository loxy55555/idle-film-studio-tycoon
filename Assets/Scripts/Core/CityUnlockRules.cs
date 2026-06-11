using UnityEngine;

/// <summary>
/// City-gated content unlock queries — separate facade for future tier tables.
/// Department/upgrade/movie/contract delegate to existing rules (no behavior change).
/// Genre/rarity tables are placeholders until a future balance phase.
/// </summary>
public static class CityUnlockRules
{
    public static int GetCurrentCityLevel(GameHub hub) =>
        hub?.city?.Level ?? 1;

    public static CityTier GetCurrentCityTier(GameHub hub) =>
        CityTierExtensions.FromLevel(GetCurrentCityLevel(hub));

    public static bool IsDepartmentUnlocked(DepartmentType type, int cityLevel) =>
        CityProgressionRules.IsDepartmentUnlocked(type, cityLevel);

    public static bool IsDepartmentUnlocked(GameHub hub, DepartmentType type) =>
        IsDepartmentUnlocked(type, GetCurrentCityLevel(hub));

    public static bool IsUpgradeUnlocked(UpgradeConfig cfg, int cityLevel) =>
        CityProgressionRules.IsUpgradeUnlocked(cfg, cityLevel);

    public static bool IsUpgradeUnlocked(GameHub hub, UpgradeConfig cfg) =>
        IsUpgradeUnlocked(cfg, GetCurrentCityLevel(hub));

    public static bool IsMovieUnlocked(MovieConfig cfg, int cityLevel) =>
        CityProgressionRules.IsMovieUnlocked(cfg, cityLevel);

    public static bool IsMovieUnlocked(GameHub hub, MovieConfig cfg) =>
        IsMovieUnlocked(cfg, GetCurrentCityLevel(hub));

    public static bool IsContractUnlocked(ContractConfig cfg, int cityLevel) =>
        CityProgressionRules.IsContractUnlocked(cfg, cityLevel);

    public static bool IsContractUnlocked(GameHub hub, ContractConfig cfg) =>
        IsContractUnlocked(cfg, GetCurrentCityLevel(hub));

    /// <summary>TODO: Phase 7+ — assign genre unlock city per genre.</summary>
    public static int GetGenreRequiredCity(MovieGenre genre)
    {
        _ = genre;
        return 1;
    }

    public static bool IsGenreUnlocked(MovieGenre genre, int cityLevel) =>
        cityLevel >= GetGenreRequiredCity(genre);

    public static bool IsGenreUnlocked(GameHub hub, MovieGenre genre) =>
        IsGenreUnlocked(genre, GetCurrentCityLevel(hub));

    /// <summary>TODO: Phase 7+ — assign rarity unlock city per rarity tier.</summary>
    public static int GetRarityRequiredCity(MovieRarity rarity)
    {
        _ = rarity;
        return 1;
    }

    public static bool IsRarityUnlocked(MovieRarity rarity, int cityLevel) =>
        cityLevel >= GetRarityRequiredCity(rarity);

    public static bool IsRarityUnlocked(GameHub hub, MovieRarity rarity) =>
        IsRarityUnlocked(rarity, GetCurrentCityLevel(hub));
}
