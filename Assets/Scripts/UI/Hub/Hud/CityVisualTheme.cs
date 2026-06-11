using UnityEngine;

/// <summary>Visual theme contract — one implementation per city tier (Phase 7B.3).</summary>
public abstract class CityVisualTheme
{
    public abstract CityTier Tier { get; }
    public abstract string ThemeId { get; }
    public abstract string DisplayLabel { get; }

    public void Apply(StudioVisualStage stage)
    {
        if (stage == null) return;

        StudioVisualStageLayers.EnsureHierarchy(stage);
        var ctx = CityVisualThemeContext.Create(stage, this);

        BuildBackground(ctx);
        BuildSet(ctx);
        BuildCharacter(ctx);
        BuildEquipment(ctx);
        BuildEffects(ctx);
        BuildOverlay(ctx);
    }

    protected abstract void BuildBackground(CityVisualThemeContext ctx);
    protected abstract void BuildSet(CityVisualThemeContext ctx);
    protected abstract void BuildCharacter(CityVisualThemeContext ctx);
    protected abstract void BuildEquipment(CityVisualThemeContext ctx);
    protected virtual void BuildEffects(CityVisualThemeContext ctx) { }

    protected abstract void BuildOverlay(CityVisualThemeContext ctx);
}
