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
    public Color primaryColor = HudSkinDefaults.BTN_PRIMARY;
    public Color secondaryColor = HudSkinDefaults.BG_CARD2;
    public Color successColor = HudSkinDefaults.BTN_SUCCESS;
    public Color dangerColor = HudSkinDefaults.Hex("#E74C3C");
    public Color ghostColor = HudSkinDefaults.BG_SECTION;
    public Color disabledColor = HudSkinDefaults.BTN_DISABLED;
    public Color lockedColor = HudSkinDefaults.BTN_LOCKED;
    public Color readyColor = HudSkinDefaults.BTN_SUCCESS;
    public Color cantAffordColor = HudSkinDefaults.BTN_CANT_AFFORD;

    public void Apply(Image image, HudButtonVariant variant)
    {
        if (image == null) return;

        switch (variant)
        {
            case HudButtonVariant.Primary:
                HudSkinUtil.ApplySpriteOrColor(image, primarySprite, primaryColor);
                break;
            case HudButtonVariant.Secondary:
                HudSkinUtil.ApplySpriteOrColor(image, secondarySprite, secondaryColor);
                break;
            case HudButtonVariant.Success:
                HudSkinUtil.ApplySpriteOrColor(image, successSprite, successColor);
                break;
            case HudButtonVariant.Danger:
                HudSkinUtil.ApplySpriteOrColor(image, dangerSprite, dangerColor);
                break;
            case HudButtonVariant.Ghost:
                HudSkinUtil.ApplySpriteOrColor(image, ghostSprite, ghostColor);
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
                HudSkinUtil.ApplySpriteOrColor(image, readySprite ?? successSprite ?? primarySprite, readyColor);
                break;
            case HudButtonState.Disabled:
                HudSkinUtil.ApplySpriteOrColor(image, disabledSprite, disabledColor);
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
            HudSkinUtil.ApplySpriteOrColor(image, disabledSprite, cantAffordColor);
    }

    public static void ApplyFallback(Image image, HudButtonVariant variant)
    {
        if (image == null) return;

        switch (variant)
        {
            case HudButtonVariant.Primary:   HudSkinUtil.ApplySpriteOrColor(image, null, HudSkinDefaults.BTN_PRIMARY); break;
            case HudButtonVariant.Secondary: HudSkinUtil.ApplySpriteOrColor(image, null, HudSkinDefaults.BG_CARD2); break;
            case HudButtonVariant.Success:   HudSkinUtil.ApplySpriteOrColor(image, null, HudSkinDefaults.BTN_SUCCESS); break;
            case HudButtonVariant.Danger:    HudSkinUtil.ApplySpriteOrColor(image, null, HudSkinDefaults.Hex("#E74C3C")); break;
            case HudButtonVariant.Ghost:     HudSkinUtil.ApplySpriteOrColor(image, null, HudSkinDefaults.BG_SECTION); break;
        }
    }

    public static void ApplyStateFallback(Image image, HudButtonState state)
    {
        if (image == null) return;

        switch (state)
        {
            case HudButtonState.Normal:   ApplyFallback(image, HudButtonVariant.Primary); break;
            case HudButtonState.Ready:    HudSkinUtil.ApplySpriteOrColor(image, null, HudSkinDefaults.BTN_SUCCESS); break;
            case HudButtonState.Disabled: HudSkinUtil.ApplySpriteOrColor(image, null, HudSkinDefaults.BTN_DISABLED); break;
            case HudButtonState.Locked:   HudSkinUtil.ApplySpriteOrColor(image, null, HudSkinDefaults.BTN_LOCKED); break;
        }
    }
}
