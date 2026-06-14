using UnityEngine;
using UnityEngine.UI;

/// <summary>Layout targets for the definitive HUD shell (Phase 7A.2 / 9.2).</summary>
public static class HudLayoutConstants
{
    // Phase 9.2: Studio main tabs (Departamentos / Mejoras / Contratos)
    public const float StudioMainTabHeight = 72f;
    public const float StudioMainTabFontSize = 15f;

    public const float SubTabBarHeight     = 60f;
    public const float SubTabFontSize      = 14f;
    public const float MejorasSubTabBarHeight = 68f;
    public const float MejorasSubTabFontSize  = 15f;
    public const float StudioHeaderHeight  = 64f;

    public const float StudioVisualShare    = 0.38f;
    public const float StudioVisualMinHeight = 120f;
    public const float StudioVisualFixedHeight = 570f;

    public const float StudioSummaryHeight = 100f;
    public const float StudioBonificationsHeight = 100f;
    public const float StudioDeptCardHeight = 148f; // Phase 12.3: larger for mobile readability
    public const float StudioDeptVisibleRows     = 2.0f;

    public const float SectionHeaderHeight = 18f;

    public const float ProductionWidgetHeight = 260f;
    public const float ProductionSlotsHeight  = 400f;
    public const float ProductionActionHeight = 56f;
    public const float ProductionHistoryHeight = 96f;

    public const float OfferCardBaseHeight    = 300f;
    public const float OfferCardMaxExpanded   = 520f;

    // Visual grid (Phase 9.1)
    public const float ScreenPaddingH  = 14f;
    public const float ScreenPaddingV  = 12f;
    public const float SectionSpacing  = 10f;
    public const float AccentLineHeight = 2f;

    public static RectOffset SectionPadding => new RectOffset(14, 14, 12, 16);
    public static RectOffset TightPadding   => new RectOffset(14, 14, 10, 14);

    public static void ApplySectionPadding(VerticalLayoutGroup vlg)
    {
        if (vlg == null) return;
        vlg.padding = SectionPadding;
        vlg.spacing = SectionSpacing;
    }

    public static void ApplySectionPadding(HorizontalLayoutGroup hlg)
    {
        if (hlg == null) return;
        hlg.padding = new RectOffset((int)ScreenPaddingH, (int)ScreenPaddingH, (int)ScreenPaddingV, (int)ScreenPaddingV);
        hlg.spacing = SectionSpacing;
    }
}
