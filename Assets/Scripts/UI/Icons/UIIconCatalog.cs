using UnityEngine;

/// <summary>Typed access to <see cref="UIIconRegistry"/> sprites.</summary>
public static class UIIconCatalog
{
    static UIIconRegistry _registry;

    static UIIconRegistry Registry
    {
        get
        {
            if (_registry == null)
                _registry = Resources.Load<UIIconRegistry>(UIIconRegistry.ResourceName);
            return _registry;
        }
    }

    public static Sprite GetDepartment(DepartmentType type)
    {
        var r = Registry;
        if (r == null) return null;
        var sprite = type switch
        {
            DepartmentType.Editor         => r.deptEditor,
            DepartmentType.Director       => r.deptDirector,
            DepartmentType.Actors         => r.deptActors,
            DepartmentType.Sound          => r.deptSound,
            DepartmentType.Cinematography => r.deptCinematography,
            DepartmentType.Makeup         => r.deptMakeup,
            DepartmentType.Costume        => r.deptCostume,
            DepartmentType.Art            => r.deptArt,
            DepartmentType.Lighting       => r.deptLighting,
            DepartmentType.Grip           => r.deptGrip,
            DepartmentType.Producer       => r.deptProducer,
            _                             => null,
        };
        // B2 diagnostic: log missing department icon on first encounter
        if (sprite == null)
            UnityEngine.Debug.LogWarning($"[UIIconCatalog] Department icon missing for: {type}");
        return sprite;
    }

    public static Sprite GetGenre(MovieGenre genre)
    {
        var r = Registry;
        if (r == null) return null;
        var sprite = genre switch
        {
            MovieGenre.Action      => r.genreAction,
            MovieGenre.Drama       => r.genreDrama,
            MovieGenre.Horror      => r.genreHorror,
            MovieGenre.Comedy      => r.genreComedy,
            MovieGenre.Romance     => r.genreRomance,
            MovieGenre.SciFi       => r.genreSciFi,
            MovieGenre.Fantasy     => r.genreFantasy,
            MovieGenre.Thriller    => r.genreThriller,
            MovieGenre.Animation   => r.genreAnimation,
            MovieGenre.Documentary => r.genreDocumentary,
            _                      => null,
        };
        // B1 diagnostic: log missing genre icon on first encounter
        if (sprite == null)
            UnityEngine.Debug.LogWarning($"[UIIconCatalog] Genre icon missing for: {genre}");
        return sprite;
    }

    public static Sprite GetProductionIcon(MovieRarity rarity, MovieGenre genre)
    {
        if (rarity == MovieRarity.Legendary)
        {
            var award = GetAwardStar();
            if (award != null) return award;
        }

        return GetGenre(genre);
    }

    public static Sprite GetNavigation(MainHudTab tab)
    {
        var r = Registry;
        if (r == null) return null;
        return tab switch
        {
            MainHudTab.Studio     => r.navStudio,
            MainHudTab.Production => r.navProduction,
            MainHudTab.Awards     => r.navAwards,
            MainHudTab.Collection => r.navCollection,
            MainHudTab.Shop       => r.navShop,
            _                     => null,
        };
    }

    public static Sprite GetResourceMoney()      => Registry?.resMoney;
    public static Sprite GetResourceReputation() => Registry?.resReputation;
    public static Sprite GetResourceGoldStar()   => Registry?.resGoldStar;
    public static Sprite GetResourceDiamonds()   => Registry?.resDiamonds;
    public static Sprite GetResourceExperience() => Registry?.resExperience;
    public static Sprite GetResourceTicket()     => Registry?.resTicket;
    public static Sprite GetResourceFilmReel()   => Registry?.resFilmReel;
    public static Sprite GetResourceDecoCamera() => Registry?.resDecoCamera;
    public static Sprite GetResourceSettings()   => Registry?.resSettings;

    public static Sprite GetAwardStar()       => Registry?.awardStar ?? Registry?.resGoldStar;
    public static Sprite GetAwardStarLocked()  => Registry?.awardStarLocked;

    public static Sprite GetUtilityBoost()            => Registry?.utilBoost;
    public static Sprite GetUtilitySpeedProduction()  => Registry?.utilSpeedProduction;
    public static Sprite GetMissions()                => Registry?.navMissions;
    public static Sprite GetMarketing()               => Registry?.deptMarketing;

    /// <summary>Maps Mejoras upgrade ids to catalog sprites (Phase 13.3C).</summary>
    public static Sprite GetUpgradeIcon(string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId)) return null;

        if (upgradeId.StartsWith("personal_"))
        {
            var dept = upgradeId switch
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
                "personal_composer"       => DepartmentType.Sound,
                "personal_publicist"      => DepartmentType.Producer,
                _                         => (DepartmentType)(-1),
            };
            if ((int)dept >= 0) return GetDepartment(dept);
        }

        if (upgradeId.StartsWith("mkt_")) return GetMarketing();
        if (upgradeId.Contains("camera")) return GetResourceDecoCamera();
        if (upgradeId.StartsWith("equip_")) return GetResourceFilmReel();
        if (upgradeId.StartsWith("install_")) return GetNavigation(MainHudTab.Studio);
        return null;
    }
}
