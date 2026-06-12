/// <summary>Layout targets for the definitive HUD shell (Phase 7A.2 / 8.5C).</summary>
public static class HudLayoutConstants
{
    public const float SubTabBarHeight     = 56f;
    public const float SubTabFontSize      = 11f;
    public const float StudioHeaderHeight  = 64f;

    // Phase 8.6B: poster-peek — show only ~10% of the poster as a cinematic tease.
    // Dept cards are compact (192px) so we can afford the small stage while keeping
    // the bonus bar visible beneath it.
    public const float StudioVisualShare    = 0.12f;
    public const float StudioVisualMinHeight = 80f;
    public const float StudioVisualFixedHeight = 150f;

    public const float SectionHeaderHeight = 18f;

    // Phase 8.5C: production gets much more height — mobile-first tall cards
    public const float ProductionWidgetHeight = 160f;
    public const float ProductionSlotsHeight  = 400f;
    public const float ProductionActionHeight = 52f;
}
