using System.Collections.Generic;

/// <summary>Maps CityTier → CityVisualTheme implementations.</summary>
public static class CityVisualThemeRegistry
{
    static readonly Dictionary<CityTier, CityVisualTheme> Themes = BuildThemes();

    public static IReadOnlyDictionary<CityTier, CityVisualTheme> All => Themes;

    public static CityVisualTheme Get(CityTier tier) =>
        Themes.TryGetValue(tier, out var theme) ? theme : Themes[CityTier.City1];

    public static CityVisualTheme Get(int cityLevel) =>
        Get(CityTierExtensions.FromLevel(cityLevel));

    static Dictionary<CityTier, CityVisualTheme> BuildThemes() =>
        new Dictionary<CityTier, CityVisualTheme>
        {
            { CityTier.City1, new City1GarageTheme() },
            { CityTier.City2, new City2IndieStudioTheme() },
            { CityTier.City3, new City3SmallStageTheme() },
            { CityTier.City4, new City4ProfessionalStudioTheme() },
            { CityTier.City5, new City5BacklotTheme() },
            { CityTier.City6, new City6FilmComplexTheme() },
            { CityTier.City7, new City7InternationalStudioTheme() },
            { CityTier.City8, new City8FilmEmpireTheme() },
        };
}
