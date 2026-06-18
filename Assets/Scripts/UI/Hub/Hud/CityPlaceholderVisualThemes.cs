using TMPro;
using UnityEngine;

/// <summary>Minimal per-city placeholder theme — separate structure, ready for future art swap.</summary>
public abstract class PlaceholderCityVisualTheme : CityVisualTheme
{
    protected abstract Color BackgroundTint { get; }
    protected abstract Color SetTint { get; }
    protected abstract Color CharacterTint { get; }
    protected abstract Color EquipmentTint { get; }
    protected abstract string CityLabel { get; }

    // Cached within a single Apply() cycle (BuildBackground is always called first).
    Sprite _cachedSprite;
    bool   _spriteCacheValid;

    Sprite GetRegistrySprite()
    {
        if (!_spriteCacheValid)
        {
            _cachedSprite     = CityBackgroundRegistry.Load()?.GetBackground(Tier);
            _spriteCacheValid = true;
        }
        return _cachedSprite;
    }

    protected override void BuildBackground(CityVisualThemeContext ctx)
    {
        // BuildBackground is always the first Build* call inside Apply(), so reset the cache here.
        _spriteCacheValid = false;

        var root   = ctx.CreateLayerRoot(StudioVisualLayerKind.Background);
        var sprite = GetRegistrySprite();

        if (sprite != null)
        {
            StudioVisualShapeUtil.CreateSpriteBlock(root, $"{ThemeId}_Background", sprite);
            return;
        }

        // Procedural fallback
        StudioVisualShapeUtil.CreateBlock(root, $"{ThemeId}_Background", BackgroundTint,
            new Vector2(0f, 0.28f), new Vector2(1f, 1f));
        StudioVisualShapeUtil.CreateBlock(root, $"{ThemeId}_Floor", SetTint,
            new Vector2(0f, 0f), new Vector2(1f, 0.30f));
    }

    protected override void BuildSet(CityVisualThemeContext ctx)
    {
        // Skip colored prop blocks when a real background sprite covers the scene.
        if (GetRegistrySprite() != null) return;

        var root = ctx.CreateLayerRoot(StudioVisualLayerKind.Set);
        StudioVisualShapeUtil.CreateBlock(root, $"{ThemeId}_SetProp", SetTint,
            new Vector2(0.30f, 0.12f), new Vector2(0.70f, 0.28f));
    }

    protected override void BuildCharacter(CityVisualThemeContext ctx)
    {
        if (GetRegistrySprite() != null) return;

        var root = ctx.CreateLayerRoot(StudioVisualLayerKind.Character);
        StudioVisualShapeUtil.CreateBlock(root, $"{ThemeId}_Character", CharacterTint,
            new Vector2(0.44f, 0.12f), new Vector2(0.56f, 0.42f));
    }

    protected override void BuildEquipment(CityVisualThemeContext ctx)
    {
        if (GetRegistrySprite() != null) return;

        var root = ctx.CreateLayerRoot(StudioVisualLayerKind.Equipment);
        StudioVisualShapeUtil.CreateBlock(root, $"{ThemeId}_Equipment", EquipmentTint,
            new Vector2(0.10f, 0.18f), new Vector2(0.22f, 0.34f));
    }

    protected override void BuildOverlay(CityVisualThemeContext ctx)
    {
        // Overlay text (studio name + city number) removed in FASE 14.0A.
        // The artistic background images replace the need for these debug labels.
    }

    static void StretchAnchored(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (rt == null) return;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }
}

public sealed class City2IndieStudioTheme : PlaceholderCityVisualTheme
{
    public override CityTier Tier => CityTier.City2;
    public override string ThemeId => "City2_IndieStudio";
    public override string DisplayLabel => "INDIE STUDIO";
    protected override string CityLabel => "CIUDAD 2";
    protected override Color BackgroundTint => new Color(0.15f, 0.17f, 0.22f);
    protected override Color SetTint => new Color(0.28f, 0.30f, 0.36f);
    protected override Color CharacterTint => new Color(0.08f, 0.09f, 0.12f);
    protected override Color EquipmentTint => new Color(0.20f, 0.22f, 0.28f);
}

public sealed class City3SmallStageTheme : PlaceholderCityVisualTheme
{
    public override CityTier Tier => CityTier.City3;
    public override string ThemeId => "City3_SmallStage";
    public override string DisplayLabel => "SMALL STAGE";
    protected override string CityLabel => "CIUDAD 3";
    protected override Color BackgroundTint => new Color(0.14f, 0.18f, 0.20f);
    protected override Color SetTint => new Color(0.26f, 0.32f, 0.34f);
    protected override Color CharacterTint => new Color(0.08f, 0.10f, 0.11f);
    protected override Color EquipmentTint => new Color(0.18f, 0.24f, 0.26f);
}

public sealed class City4ProfessionalStudioTheme : PlaceholderCityVisualTheme
{
    public override CityTier Tier => CityTier.City4;
    public override string ThemeId => "City4_ProfessionalStudio";
    public override string DisplayLabel => "PRO STUDIO";
    protected override string CityLabel => "CIUDAD 4";
    protected override Color BackgroundTint => new Color(0.12f, 0.16f, 0.22f);
    protected override Color SetTint => new Color(0.24f, 0.28f, 0.36f);
    protected override Color CharacterTint => new Color(0.07f, 0.09f, 0.13f);
    protected override Color EquipmentTint => new Color(0.16f, 0.22f, 0.30f);
}

public sealed class City5BacklotTheme : PlaceholderCityVisualTheme
{
    public override CityTier Tier => CityTier.City5;
    public override string ThemeId => "City5_Backlot";
    public override string DisplayLabel => "BACKLOT";
    protected override string CityLabel => "CIUDAD 5";
    protected override Color BackgroundTint => new Color(0.18f, 0.16f, 0.14f);
    protected override Color SetTint => new Color(0.32f, 0.28f, 0.24f);
    protected override Color CharacterTint => new Color(0.10f, 0.09f, 0.08f);
    protected override Color EquipmentTint => new Color(0.24f, 0.22f, 0.18f);
}

public sealed class City6FilmComplexTheme : PlaceholderCityVisualTheme
{
    public override CityTier Tier => CityTier.City6;
    public override string ThemeId => "City6_FilmComplex";
    public override string DisplayLabel => "FILM COMPLEX";
    protected override string CityLabel => "CIUDAD 6";
    protected override Color BackgroundTint => new Color(0.13f, 0.15f, 0.20f);
    protected override Color SetTint => new Color(0.26f, 0.28f, 0.34f);
    protected override Color CharacterTint => new Color(0.08f, 0.09f, 0.12f);
    protected override Color EquipmentTint => new Color(0.18f, 0.20f, 0.26f);
}

public sealed class City7InternationalStudioTheme : PlaceholderCityVisualTheme
{
    public override CityTier Tier => CityTier.City7;
    public override string ThemeId => "City7_InternationalStudio";
    public override string DisplayLabel => "INTL STUDIO";
    protected override string CityLabel => "CIUDAD 7";
    protected override Color BackgroundTint => new Color(0.11f, 0.14f, 0.21f);
    protected override Color SetTint => new Color(0.22f, 0.26f, 0.36f);
    protected override Color CharacterTint => new Color(0.07f, 0.08f, 0.12f);
    protected override Color EquipmentTint => new Color(0.15f, 0.20f, 0.30f);
}

public sealed class City8FilmEmpireTheme : PlaceholderCityVisualTheme
{
    public override CityTier Tier => CityTier.City8;
    public override string ThemeId => "City8_FilmEmpire";
    public override string DisplayLabel => "FILM EMPIRE";
    protected override string CityLabel => "CIUDAD 8";
    protected override Color BackgroundTint => new Color(0.16f, 0.12f, 0.20f);
    protected override Color SetTint => new Color(0.30f, 0.24f, 0.34f);
    protected override Color CharacterTint => new Color(0.09f, 0.07f, 0.11f);
    protected override Color EquipmentTint => new Color(0.22f, 0.18f, 0.28f);
}
