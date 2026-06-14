using UnityEngine;
using UnityEngine.UI;

/// <summary>Applies button background sprites or fallback colors to UI Images.</summary>
[CreateAssetMenu(fileName = "UIButtonSkin", menuName = "IdleFilm/UI/Button Skin")]
public class UIButtonSkin : ScriptableObject
{
    [Header("Sprites (optional)")]
    public Sprite primarySprite;
    public Sprite secondarySprite;
    public Sprite successSprite;
    public Sprite dangerSprite;
    public Sprite ghostSprite;
    public Sprite disabledSprite;
    public Sprite lockedSprite;
    public Sprite readySprite;

    [Header("Fallback Colors")]
    public Color primaryColor    = CinematicTheme.ButtonSuccess;
    public Color secondaryColor  = CinematicTheme.CardBg2;
    public Color successColor    = CinematicTheme.ButtonSuccess;
    public Color dangerColor     = HudSkinDefaults.Hex("#C0392B");
    public Color ghostColor      = CinematicTheme.PanelBg;
    public Color disabledColor   = CinematicTheme.ButtonDisabled;
    public Color lockedColor     = CinematicTheme.TextDim;
    public Color readyColor      = CinematicTheme.ButtonReady;
    public Color cantAffordColor = CinematicTheme.ButtonDisabled;

    public void Apply(Image image, HudButtonVariant variant)
    {
        if (image == null) return;

        // Always source from CinematicTheme — stale GameHudSkins.asset serialization must not win.
        switch (variant)
        {
            case HudButtonVariant.Primary:
            case HudButtonVariant.Success:
                HudSkinUtil.ApplySpriteOrColor(image, successSprite ?? primarySprite, CinematicTheme.ButtonSuccess);
                break;
            case HudButtonVariant.Secondary:
                HudSkinUtil.ApplySpriteOrColor(image, secondarySprite, CinematicTheme.CardBg2);
                break;
            case HudButtonVariant.Danger:
                HudSkinUtil.ApplySpriteOrColor(image, dangerSprite, dangerColor);
                break;
            case HudButtonVariant.Ghost:
                HudSkinUtil.ApplySpriteOrColor(image, ghostSprite, CinematicTheme.PanelBg);
                break;
        }
    }

    public void ApplyState(Image image, HudButtonState state)
    {
        if (image == null) return;

        switch (state)
        {
            case HudButtonState.Normal:
                Apply(image, HudButtonVariant.Primary);
                break;
            case HudButtonState.Ready:
                HudSkinUtil.ApplySpriteOrColor(image, readySprite ?? successSprite ?? primarySprite, CinematicTheme.ButtonReady);
                break;
            case HudButtonState.Disabled:
                HudSkinUtil.ApplySpriteOrColor(image, disabledSprite, CinematicTheme.ButtonDisabled);
                break;
            case HudButtonState.Locked:
                HudSkinUtil.ApplySpriteOrColor(image, lockedSprite, lockedColor);
                break;
        }
    }

    public void ApplyPurchaseState(Image image, bool canBuy, bool locked)
    {
        if (image == null) return;

        if (canBuy)
            ApplyState(image, HudButtonState.Ready);
        else if (locked)
            ApplyState(image, HudButtonState.Locked);
        else
            HudSkinUtil.ApplySpriteOrColor(image, disabledSprite, CinematicTheme.ButtonDisabled);
    }

    public static void ApplyFallback(Image image, HudButtonVariant variant)
    {
        if (image == null) return;

        switch (variant)
        {
            case HudButtonVariant.Primary:   HudSkinUtil.ApplySpriteOrColor(image, null, CinematicTheme.ButtonSuccess); break;
            case HudButtonVariant.Secondary: HudSkinUtil.ApplySpriteOrColor(image, null, CinematicTheme.CardBg2); break;
            case HudButtonVariant.Success:   HudSkinUtil.ApplySpriteOrColor(image, null, CinematicTheme.ButtonSuccess); break;
            case HudButtonVariant.Danger:    HudSkinUtil.ApplySpriteOrColor(image, null, HudSkinDefaults.Hex("#C0392B")); break;
            case HudButtonVariant.Ghost:     HudSkinUtil.ApplySpriteOrColor(image, null, CinematicTheme.PanelBg); break;
        }
    }

    public static void ApplyStateFallback(Image image, HudButtonState state)
    {
        if (image == null) return;

        switch (state)
        {
            case HudButtonState.Normal:   ApplyFallback(image, HudButtonVariant.Primary); break;
            case HudButtonState.Ready:    HudSkinUtil.ApplySpriteOrColor(image, null, CinematicTheme.ButtonReady); break;
            case HudButtonState.Disabled: HudSkinUtil.ApplySpriteOrColor(image, null, CinematicTheme.ButtonDisabled); break;
            case HudButtonState.Locked:   HudSkinUtil.ApplySpriteOrColor(image, null, CinematicTheme.TextDim); break;
        }
    }
}
