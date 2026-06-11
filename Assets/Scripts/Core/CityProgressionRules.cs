using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central city-gated progression rules (departments, movies, upgrades, contracts).
/// </summary>
public static class CityProgressionRules
{
    static readonly Dictionary<DepartmentType, int> DepartmentCity = new()
    {
        { DepartmentType.Director,       1 },
        { DepartmentType.Actors,         1 },
        { DepartmentType.Editor,         1 },
        { DepartmentType.Sound,          2 },
        { DepartmentType.Lighting,       2 },
        { DepartmentType.Cinematography, 3 },
        { DepartmentType.Grip,           3 },
        { DepartmentType.Makeup,         4 },
        { DepartmentType.Costume,        4 },
        { DepartmentType.Art,            5 },
        { DepartmentType.Producer,       6 },
    };

    static readonly Dictionary<string, int> ContractCityById = new()
    {
        { "c_first_movie",        1 },
        { "c_three_movies",       1 },
        { "c_first_upgrade",      1 },
        { "c_rep_50",             1 },
        { "c_first_reputation",   1 },
        { "c_drama_fan",          1 },
        { "c_action_fan",         1 },
        { "c_rep_200",            2 },
        { "c_studio_level5",      2 },
        { "c_speed_run",          2 },
        { "c_horror_night",       2 },
        { "c_blockbuster",        3 },
        { "c_studio_level10",     3 },
        { "c_ten_movies",         3 },
        { "c_rep_1000",           3 },
        { "c_studio_level15",     4 },
        { "c_big_spender",        4 },
        { "c_comedy_king",        4 },
        { "c_scifi_pioneer",      4 },
        { "c_studio_level20",     6 },
        { "c_rep_5000",           6 },
        { "c_all_genres",         6 },
    };

    public static int GetDepartmentRequiredCity(DepartmentType type) =>
        DepartmentCity.TryGetValue(type, out int city) ? city : 1;

    public static bool IsDepartmentUnlocked(DepartmentType type, int cityLevel) =>
        cityLevel >= GetDepartmentRequiredCity(type);

    public static string GetDepartmentLockLabel(DepartmentType type) =>
        $"Disponible en Ciudad {GetDepartmentRequiredCity(type)}";

    public static int GetMovieRequiredCity(MovieConfig cfg)
    {
        if (cfg == null) return 99;

        if (cfg.unlockCityLevel >= 1 && cfg.unlockCityLevel <= ContentScaleDatabase.MaxCityLevel)
            return cfg.unlockCityLevel;

        return GetMovieRequiredCityFromRules(cfg);
    }

    static int GetMovieRequiredCityFromRules(MovieConfig cfg)
    {
        if (cfg.contentKind == MovieContentKind.Sequel || cfg.contentKind == MovieContentKind.Remake)
            return 2;
        if (cfg.contentKind == MovieContentKind.Epic)
            return 3;
        if (cfg.contentKind == MovieContentKind.Saga)
            return 4;

        switch (cfg.catalogTier)
        {
            case MovieCatalogTier.Tier1:
            case MovieCatalogTier.Tier2:
                return 1;
            case MovieCatalogTier.Epic:
                return 3;
            case MovieCatalogTier.Tier3:
                return 4;
            case MovieCatalogTier.Tier4:
                return 5;
        }

        if (IsEpicHeuristic(cfg)) return 3;
        if (cfg.unlockStudioLevel <= 3) return 1;
        if (cfg.unlockStudioLevel <= 7) return 2;
        if (cfg.unlockStudioLevel <= 10) return 4;
        if (cfg.unlockStudioLevel <= 14) return 5;
        if (cfg.unlockStudioLevel <= 18) return 6;
        if (cfg.unlockStudioLevel <= 22) return 7;
        return 8;
    }

    public static bool IsMovieUnlocked(MovieConfig cfg, int cityLevel) =>
        cfg != null && cityLevel >= GetMovieRequiredCity(cfg);

    public static string GetMovieLockLabel(MovieConfig cfg) =>
        $"Requiere Ciudad {GetMovieRequiredCity(cfg)}";

    public static int GetUpgradeRequiredCity(UpgradeConfig cfg)
    {
        if (cfg == null) return 99;

        if (cfg.category == UpgradeCategory.Personnel)
        {
            if (TryPersonnelDepartment(cfg.id, out var dept))
                return GetDepartmentRequiredCity(dept);
        }

        return cfg.id switch
        {
            "equip_camera_basic" or "equip_mic_pro" => 1,
            "personal_editor" or "personal_director" or "personal_actors" => 1,
            "install_plato_small" or "mkt_social_media" => 1,

            "equip_lighting_kit" or "equip_sound_surround" or "personal_sound" => 2,

            "equip_lighting_led" or "equip_steadicam" or "equip_drone"
                or "install_plato_medium" or "install_editing_room" or "personal_lighting"
                or "personal_grip" or "equip_editing_soft" => 3,

            "personal_makeup" or "personal_costume" or "equip_vfx_basic" => 4,

            "personal_art" or "personal_cinematography" or "equip_camera_pro"
                or "install_plato_large" or "install_offices" => 5,

            "equip_vfx_advanced" or "equip_camera_digital" or "install_vfx_dept"
                or "install_exterior" or "mkt_press_kit" or "mkt_trailer" => 6,

            "equip_render_farm" or "equip_coloring" or "equip_dolby"
                or "install_screening" or "install_studio_lot" or "mkt_global"
                or "personal_composer" or "personal_publicist" or "personal_producer" => 7,

            _ => FallbackUpgradeCity(cfg),
        };
    }

    static int FallbackUpgradeCity(UpgradeConfig cfg)
    {
        int studio = cfg.unlockStudioLevel;
        if (studio <= 1) return 1;
        if (studio <= 4) return 2;
        if (studio <= 7) return 3;
        if (studio <= 9) return 4;
        if (studio <= 12) return 5;
        if (studio <= 15) return 6;
        if (studio <= 20) return 7;
        return 8;
    }

    public static bool IsUpgradeUnlocked(UpgradeConfig cfg, int cityLevel) =>
        cfg != null && cityLevel >= GetUpgradeRequiredCity(cfg);

    public static string GetUpgradeLockLabel(UpgradeConfig cfg) =>
        $"Requiere Ciudad {GetUpgradeRequiredCity(cfg)}";

    public static int GetContractRequiredCity(ContractConfig cfg)
    {
        if (cfg == null) return 99;

        if (cfg.difficultyBand != ContractDifficultyBand.Simple && (int)cfg.difficultyBand > 0)
            return (int)cfg.difficultyBand;

        if (!string.IsNullOrEmpty(cfg.id) && ContractCityById.TryGetValue(cfg.id, out int city))
            return city;

        if (cfg.unlockStudioLevel <= 3) return 1;
        if (cfg.unlockStudioLevel <= 7) return 2;
        if (cfg.unlockStudioLevel <= 10) return 3;
        if (cfg.unlockStudioLevel <= 14) return 4;
        if (cfg.unlockStudioLevel <= 18) return 5;
        return 6;
    }

    public static bool IsContractUnlocked(ContractConfig cfg, int cityLevel) =>
        cfg != null && cityLevel >= GetContractRequiredCity(cfg);

    public static string GetCityDisplayName(int cityLevel) =>
        CityLevelDatabase.GetLevel(cityLevel).displayName;

    public static bool IsEpicHeuristic(MovieConfig cfg) =>
        cfg.quality >= 3f || cfg.unlockStudioLevel >= 11;

    static bool TryPersonnelDepartment(string upgradeId, out DepartmentType dept)
    {
        dept = upgradeId switch
        {
            "personal_editor"         => DepartmentType.Editor,
            "personal_director"       => DepartmentType.Director,
            "personal_actors"         => DepartmentType.Actors,
            "personal_sound"          => DepartmentType.Sound,
            "personal_cinematography" => DepartmentType.Cinematography,
            "personal_makeup"         => DepartmentType.Makeup,
            "personal_costume"        => DepartmentType.Costume,
            "personal_art"            => DepartmentType.Art,
            "personal_lighting"       => DepartmentType.Lighting,
            "personal_grip"           => DepartmentType.Grip,
            "personal_producer"       => DepartmentType.Producer,
            _                         => DepartmentType.Editor,
        };
        return upgradeId != null && upgradeId.StartsWith("personal_");
    }

    public static List<DepartmentType> GetDepartmentsUnlockedAtCity(int cityLevel)
    {
        var list = new List<DepartmentType>();
        foreach (var pair in DepartmentCity)
        {
            if (pair.Value == cityLevel)
                list.Add(pair.Key);
        }
        return list;
    }

    public static int CountMoviesUnlockedAtCity(MovieConfig[] movies, int cityLevel)
    {
        if (movies == null) return 0;
        int count = 0;
        foreach (var m in movies)
        {
            if (m == null) continue;
            if (GetMovieRequiredCity(m) == cityLevel) count++;
        }
        return count;
    }

    public static int CountUpgradesUnlockedAtCity(UpgradeConfig[] upgrades, int cityLevel)
    {
        if (upgrades == null) return 0;
        int count = 0;
        foreach (var u in upgrades)
        {
            if (u == null) continue;
            if (GetUpgradeRequiredCity(u) == cityLevel) count++;
        }
        return count;
    }

    public static int CountContractsUnlockedAtCity(ContractConfig[] contracts, int cityLevel)
    {
        if (contracts == null) return 0;
        int count = 0;
        foreach (var c in contracts)
        {
            if (c == null) continue;
            if (GetContractRequiredCity(c) == cityLevel) count++;
        }
        return count;
    }

    // ── Phase 6 — city tier state facade (delegates to CitySystem; no new balance) ──

    public static CityTier GetCurrentCity(GameHub hub) =>
        GetCurrentCity(hub?.city);

    public static CityTier GetCurrentCity(CitySystem city) =>
        CityTierExtensions.FromLevel(city?.Level ?? 1);

    public static int GetRequiredStars(CityTier tier) =>
        CityLevelDatabase.GetLevel(tier.ToLevel()).requiredOscars;

    public static int GetRequiredStarsForNextCity(GameHub hub)
    {
        var city = hub?.city;
        if (city == null) return GetRequiredStars(CityTier.City1);
        if (city.IsMaxLevel) return GetRequiredStars(CityTier.City8);

        var next = city.GetNextDefinition();
        return next != null ? next.requiredOscars : GetRequiredStars(CityTier.City8);
    }

    public static bool CanAdvanceCity(GameHub hub) =>
        hub?.city != null && hub.city.CanUpgrade();

    public static bool AdvanceCity(GameHub hub) =>
        hub?.city != null && hub.city.TryUpgrade();
}
