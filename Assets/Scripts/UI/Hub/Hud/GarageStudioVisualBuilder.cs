using UnityEngine;

/// <summary>Legacy entry point — delegates to City1GarageTheme via StudioVisualThemeController.</summary>
public static class GarageStudioVisualBuilder
{
    public const string BuiltMarkerName = City1GarageTheme.BackgroundMarker;

    public static bool IsBuilt(StudioVisualStage stage) =>
        StudioVisualThemeController.IsThemeBuilt(stage, City1GarageTheme.ThemeIdValue);

    public static void Ensure(StudioVisualStage stage) =>
        StudioVisualThemeController.ApplyTheme(stage, CityTier.City1);
}
