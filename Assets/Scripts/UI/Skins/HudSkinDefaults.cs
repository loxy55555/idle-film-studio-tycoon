using UnityEngine;



/// <summary>Default HUD chrome colors — Cinematic 2.0 theme (Phase 11.7).</summary>

public static class HudSkinDefaults

{

    // ── Backgrounds (darkened ~12% — deep petroleum / cinematic black) ────────

    public static readonly Color BG_DEEP       = Hex("#050C16");

    public static readonly Color BG_SECTION    = Hex("#081220");

    public static readonly Color BG_CARD       = Hex("#0D1A2C");

    public static readonly Color BG_CARD2      = Hex("#101F33");

    public static readonly Color BG_POPUP      = Hex("#121F34");

    public static readonly Color TOPBAR_BG     = Hex("#040A12");

    public static readonly Color NAV_BG        = Hex("#040911");

    public static readonly Color STAGE_BG      = Hex("#050C16");



    // ── Borders ───────────────────────────────────────────────────────────────

    public static readonly Color BORDER_SUBTLE  = Hex("#163048");

    public static readonly Color BORDER_ACCENT  = Hex("#1F3D5C");



    // ── Cinematic Gold ────────────────────────────────────────────────────────

    public static readonly Color GOLD_DIM       = Hex("#B8832A");

    public static readonly Color GOLD_BASE      = Hex("#D9A441");

    public static readonly Color GOLD_BRIGHT    = Hex("#F0C66A");



    // ── Buttons ───────────────────────────────────────────────────────────────

    public static readonly Color BTN_PRIMARY    = CinematicTheme.BronzeBase;

    public static readonly Color BTN_SUCCESS    = CinematicTheme.BronzeBase;

    public static readonly Color BTN_DISABLED   = CinematicTheme.ButtonDisabled;

    public static readonly Color BTN_LOCKED     = new Color(0.22f, 0.28f, 0.36f);

    public static readonly Color BTN_CANT_AFFORD= Hex("#1A2420");



    // ── Tabs ──────────────────────────────────────────────────────────────────

    public static readonly Color TAB_ACTIVE_BG      = Hex("#0C1A2C");

    public static readonly Color TAB_INACTIVE_BG    = Hex("#060D16");

    public static readonly Color TAB_ACTIVE_LABEL   = Hex("#F0C66A");

    public static readonly Color TAB_INACTIVE_LABEL = Hex("#6B7E94");



    // ── Text ──────────────────────────────────────────────────────────────────

    public static readonly Color TEXT_PRIMARY   = Hex("#F2F2F2");

    public static readonly Color TEXT_SECONDARY = Hex("#B9C2CF");

    public static readonly Color TEXT_DIM       = Hex("#6B7E94");



    public static Color Hex(string hex)

    {

        ColorUtility.TryParseHtmlString(hex, out var c);

        return c;

    }



    public static bool ColorsClose(Color a, Color b, float epsilon = 0.03f) =>

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

