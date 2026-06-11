using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Central access point for HUD skin application (Phase 8.0).</summary>
public static class HudSkinProvider
{
    const string ResourcesPath = "GameHudSkins";

    static GameHudSkins _active;
    static GameHudSkins _runtimeDefaults;

    public static GameHudSkins Active
    {
        get
        {
            if (_active != null) return _active;
            EnsureLoaded();
            return _active;
        }
    }

    public static void SetActive(GameHudSkins skins) => _active = skins;

    public static void EnsureLoaded()
    {
        if (_active != null) return;
        _active = Resources.Load<GameHudSkins>(ResourcesPath);
        if (_active != null) return;

        _runtimeDefaults ??= GameHudSkins.CreateRuntimeDefaults();
        _active = _runtimeDefaults;
    }

    public static void ApplyPanel(Image image, HudPanelVariant variant)
    {
        if (image == null || variant == HudPanelVariant.Custom) return;

        var skin = Active?.panelSkin;
        if (skin != null) skin.Apply(image, variant);
        else UIPanelSkin.ApplyFallback(image, variant);
    }

    public static void ApplyPanelFromColor(Image image, Color legacyColor)
    {
        if (image == null) return;
        var variant = ResolvePanelVariant(legacyColor);
        if (variant == HudPanelVariant.Custom)
            image.color = legacyColor;
        else
            ApplyPanel(image, variant);
    }

    public static void ApplyCard(Image image, HudCardVariant variant)
    {
        if (image == null) return;

        var skin = Active?.cardSkin;
        if (skin != null) skin.Apply(image, variant);
        else UICardSkin.ApplyFallback(image, variant);
    }

    public static void ApplyCardFromColor(Image image, Color legacyColor)
    {
        if (image == null) return;
        var variant = ResolveCardVariant(legacyColor);
        if (variant == HudCardVariant.Primary && !IsKnownCardColor(legacyColor))
            image.color = legacyColor;
        else
            ApplyCard(image, variant);
    }

    public static void ApplyButton(Image image, HudButtonVariant variant)
    {
        if (image == null) return;

        var skin = Active?.buttonSkin;
        if (skin != null) skin.Apply(image, variant);
        else UIButtonSkin.ApplyFallback(image, variant);
    }

    public static void ApplyButtonFromColor(Image image, Color legacyColor)
    {
        if (image == null) return;
        ApplyButton(image, ResolveButtonVariant(legacyColor));
    }

    public static void ApplyButtonState(Image image, HudButtonState state)
    {
        if (image == null) return;

        var skin = Active?.buttonSkin;
        if (skin != null) skin.ApplyState(image, state);
        else UIButtonSkin.ApplyStateFallback(image, state);
    }

    public static void ApplyPurchaseButton(Image image, bool canBuy, bool locked)
    {
        if (image == null) return;

        var skin = Active?.buttonSkin;
        if (skin != null) skin.ApplyPurchaseState(image, canBuy, locked);
        else
        {
            if (canBuy) UIButtonSkin.ApplyStateFallback(image, HudButtonState.Ready);
            else if (locked) UIButtonSkin.ApplyStateFallback(image, HudButtonState.Locked);
            else image.color = HudSkinDefaults.BTN_CANT_AFFORD;
        }
    }

    public static void ApplyTab(Image background, TextMeshProUGUI label, bool active, HudTabVariant variant)
    {
        if (background == null) return;

        var skin = Active?.tabSkin;
        if (skin != null) skin.Apply(background, label, active, variant);
        else UITabSkin.ApplyFallback(background, label, active, variant);
    }

    public static HudPanelVariant ResolvePanelVariant(Color color)
    {
        if (ColorsClose(color, Color.clear)) return HudPanelVariant.Clear;
        if (ColorsClose(color, HudSkinDefaults.BG_DEEP)) return HudPanelVariant.Deep;
        if (ColorsClose(color, HudSkinDefaults.BG_SECTION)) return HudPanelVariant.Section;
        if (ColorsClose(color, HudSkinDefaults.BG_CARD)) return HudPanelVariant.Card;
        if (ColorsClose(color, HudSkinDefaults.BG_CARD2)) return HudPanelVariant.CardAlt;
        if (ColorsClose(color, HudSkinDefaults.TOPBAR_BG)) return HudPanelVariant.TopBar;
        if (ColorsClose(color, HudSkinDefaults.NAV_BG)) return HudPanelVariant.Nav;
        if (ColorsClose(color, HudSkinDefaults.STAGE_BG)) return HudPanelVariant.Stage;
        return HudPanelVariant.Custom;
    }

    public static HudCardVariant ResolveCardVariant(Color color)
    {
        if (ColorsClose(color, HudSkinDefaults.BG_CARD2)) return HudCardVariant.Secondary;
        if (ColorsClose(color, HudSkinDefaults.BG_SECTION)) return HudCardVariant.Hero;
        if (ColorsClose(color, HudSkinDefaults.BG_DEEP)) return HudCardVariant.Empty;
        return HudCardVariant.Primary;
    }

    public static HudButtonVariant ResolveButtonVariant(Color color)
    {
        if (ColorsClose(color, HudSkinDefaults.BTN_PRIMARY) ||
            ColorsClose(color, HudSkinDefaults.BTN_SUCCESS))
            return HudButtonVariant.Success;
        if (ColorsClose(color, HudSkinDefaults.BTN_DISABLED)) return HudButtonVariant.Ghost;
        return HudButtonVariant.Primary;
    }

    static bool IsKnownCardColor(Color color) =>
        ColorsClose(color, HudSkinDefaults.BG_CARD) ||
        ColorsClose(color, HudSkinDefaults.BG_CARD2) ||
        ColorsClose(color, HudSkinDefaults.BG_SECTION) ||
        ColorsClose(color, HudSkinDefaults.BG_DEEP);

    static bool ColorsClose(Color a, Color b) => HudSkinDefaults.ColorsClose(a, b);
}
