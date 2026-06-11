using UnityEngine;
using UnityEngine.UI;

/// <summary>Applies panel background sprites or fallback colors to UI Images.</summary>
[CreateAssetMenu(fileName = "UIPanelSkin", menuName = "IdleFilm/UI/Panel Skin")]
public class UIPanelSkin : ScriptableObject
{
    [Header("Sprites (optional)")]
    public Sprite deepSprite;
    public Sprite sectionSprite;
    public Sprite cardSprite;
    public Sprite cardAltSprite;
    public Sprite topBarSprite;
    public Sprite navSprite;
    public Sprite stageSprite;

    [Header("Fallback Colors")]
    public Color deepColor = HudSkinDefaults.BG_DEEP;
    public Color sectionColor = HudSkinDefaults.BG_SECTION;
    public Color cardColor = HudSkinDefaults.BG_CARD;
    public Color cardAltColor = HudSkinDefaults.BG_CARD2;
    public Color topBarColor = HudSkinDefaults.TOPBAR_BG;
    public Color navColor = HudSkinDefaults.NAV_BG;
    public Color stageColor = HudSkinDefaults.STAGE_BG;
    public Color clearColor = Color.clear;

    public void Apply(Image image, HudPanelVariant variant)
    {
        if (image == null) return;

        switch (variant)
        {
            case HudPanelVariant.Deep:
                HudSkinUtil.ApplySpriteOrColor(image, deepSprite, deepColor);
                break;
            case HudPanelVariant.Section:
                HudSkinUtil.ApplySpriteOrColor(image, sectionSprite, sectionColor);
                break;
            case HudPanelVariant.Card:
                HudSkinUtil.ApplySpriteOrColor(image, cardSprite, cardColor);
                break;
            case HudPanelVariant.CardAlt:
                HudSkinUtil.ApplySpriteOrColor(image, cardAltSprite, cardAltColor);
                break;
            case HudPanelVariant.TopBar:
                HudSkinUtil.ApplySpriteOrColor(image, topBarSprite, topBarColor);
                break;
            case HudPanelVariant.Nav:
                HudSkinUtil.ApplySpriteOrColor(image, navSprite, navColor);
                break;
            case HudPanelVariant.Stage:
                HudSkinUtil.ApplySpriteOrColor(image, stageSprite, stageColor);
                break;
            case HudPanelVariant.Clear:
                HudSkinUtil.ApplySpriteOrColor(image, null, clearColor);
                break;
            case HudPanelVariant.Custom:
                break;
        }
    }

    public static void ApplyFallback(Image image, HudPanelVariant variant)
    {
        if (image == null || variant == HudPanelVariant.Custom) return;

        switch (variant)
        {
            case HudPanelVariant.Deep:     HudSkinUtil.ApplySpriteOrColor(image, null, HudSkinDefaults.BG_DEEP); break;
            case HudPanelVariant.Section:  HudSkinUtil.ApplySpriteOrColor(image, null, HudSkinDefaults.BG_SECTION); break;
            case HudPanelVariant.Card:     HudSkinUtil.ApplySpriteOrColor(image, null, HudSkinDefaults.BG_CARD); break;
            case HudPanelVariant.CardAlt:  HudSkinUtil.ApplySpriteOrColor(image, null, HudSkinDefaults.BG_CARD2); break;
            case HudPanelVariant.TopBar:   HudSkinUtil.ApplySpriteOrColor(image, null, HudSkinDefaults.TOPBAR_BG); break;
            case HudPanelVariant.Nav:      HudSkinUtil.ApplySpriteOrColor(image, null, HudSkinDefaults.NAV_BG); break;
            case HudPanelVariant.Stage:    HudSkinUtil.ApplySpriteOrColor(image, null, HudSkinDefaults.STAGE_BG); break;
            case HudPanelVariant.Clear:    HudSkinUtil.ApplySpriteOrColor(image, null, Color.clear); break;
        }
    }
}
