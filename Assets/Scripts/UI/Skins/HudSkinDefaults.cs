using UnityEngine;

/// <summary>Default HUD chrome colors — used when no sprite is assigned on a skin asset.</summary>
public static class HudSkinDefaults
{
    public static readonly Color BG_DEEP       = Hex("#0E0E1E");
    public static readonly Color BG_SECTION    = Hex("#141428");
    public static readonly Color BG_CARD       = Hex("#1A1A30");
    public static readonly Color BG_CARD2      = Hex("#1F1F3A");
    public static readonly Color TOPBAR_BG     = Hex("#090915");
    public static readonly Color NAV_BG        = Hex("#08081A");
    public static readonly Color BTN_PRIMARY   = Hex("#27AE60");
    public static readonly Color BTN_SUCCESS   = Hex("#2ECC71");
    public static readonly Color BTN_DISABLED  = Hex("#1E1E34");
    public static readonly Color BTN_LOCKED    = new Color(0.50f, 0.50f, 0.50f);
    public static readonly Color BTN_CANT_AFFORD = new Color(0.30f, 0.30f, 0.35f);
    public static readonly Color TAB_ACTIVE_BG = new Color(0.14f, 0.14f, 0.28f);
    public static readonly Color TAB_INACTIVE_BG = new Color(0.09f, 0.09f, 0.18f);
    public static readonly Color TAB_ACTIVE_LABEL = new Color(0.18f, 0.80f, 0.44f);
    public static readonly Color TAB_INACTIVE_LABEL = new Color(0.55f, 0.55f, 0.70f);
    public static readonly Color STAGE_BG        = new Color(0.06f, 0.07f, 0.12f);

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }

    public static bool ColorsClose(Color a, Color b, float epsilon = 0.02f) =>
        Mathf.Abs(a.r - b.r) < epsilon &&
        Mathf.Abs(a.g - b.g) < epsilon &&
        Mathf.Abs(a.b - b.b) < epsilon;
}

public enum HudPanelVariant
{
    Deep,
    Section,
    Card,
    CardAlt,
    TopBar,
    Nav,
    Stage,
    Clear,
    Custom,
}

public enum HudCardVariant
{
    Primary,
    Secondary,
    Hero,
    Empty,
}

public enum HudButtonVariant
{
    Primary,
    Secondary,
    Success,
    Danger,
    Ghost,
}

public enum HudButtonState
{
    Normal,
    Disabled,
    Locked,
    Ready,
}

public enum HudTabVariant
{
    MainNav,
    SubTab,
    CategoryTab,
}
