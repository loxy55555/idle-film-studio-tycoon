using UnityEngine;

/// <summary>City 1 — Garage Studio. Extends PlaceholderCityVisualTheme so it behaves
/// identically to Cities 2-8: sprite from CityBackgroundRegistry fills the stage,
/// no procedural props.</summary>
public sealed class City1GarageTheme : PlaceholderCityVisualTheme
{
    // Constants kept for GarageStudioVisualBuilder backward-compatibility.
    public const string ThemeIdValue    = "City1_Garage";
    public const string BackgroundMarker = "Garage_WallBack";

    public override CityTier Tier        => CityTier.City1;
    public override string   ThemeId     => ThemeIdValue;
    public override string   DisplayLabel => "GARAGE STUDIO";

    protected override string CityLabel  => "CIUDAD 1";

    // Fallback tints — only used if no sprite is assigned in CityBackgroundRegistry.
    protected override Color BackgroundTint => new Color(0.14f, 0.16f, 0.19f);
    protected override Color SetTint        => new Color(0.20f, 0.22f, 0.26f);
    protected override Color CharacterTint  => new Color(0.07f, 0.08f, 0.10f);
    protected override Color EquipmentTint  => new Color(0.22f, 0.23f, 0.26f);
}
