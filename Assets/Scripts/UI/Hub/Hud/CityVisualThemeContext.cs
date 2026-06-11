using UnityEngine;

/// <summary>Shared layer targets when building a city visual theme into StudioVisualStage.</summary>
public sealed class CityVisualThemeContext
{
    public StudioVisualStage Stage { get; private set; }
    public CityVisualTheme Theme { get; private set; }

    public RectTransform BackgroundLayer => Stage?.BackgroundLayer;
    public RectTransform SetLayer        => Stage?.SetLayer;
    public RectTransform CharacterLayer  => Stage?.CharacterLayer;
    public RectTransform EquipmentLayer  => Stage?.EquipmentLayer;
    public RectTransform EffectsLayer    => Stage?.EffectsLayer;
    public RectTransform OverlayLayer    => Stage?.OverlayLayer;

    public static CityVisualThemeContext Create(StudioVisualStage stage, CityVisualTheme theme) =>
        new CityVisualThemeContext { Stage = stage, Theme = theme };

    /// <summary>Creates (or reuses) a full-bleed root for this theme on the given layer.</summary>
    public RectTransform CreateLayerRoot(StudioVisualLayerKind layerKind)
    {
        var layer = Stage.GetLayer(layerKind);
        if (layer == null || Theme == null) return null;

        var rootName = ThemeRootName;
        var existing = layer.Find(rootName) as RectTransform;
        if (existing != null) return existing;

        var go = new GameObject(rootName, typeof(RectTransform));
        go.transform.SetParent(layer, false);
        StudioVisualStageLayers.ApplyStretch(go.GetComponent<RectTransform>());
        return go.GetComponent<RectTransform>();
    }

    public string ThemeRootName => $"ThemeRoot_{Theme.ThemeId}";
}
