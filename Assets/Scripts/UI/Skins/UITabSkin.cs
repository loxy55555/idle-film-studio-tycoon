using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Applies tab background/label styling via sprites or fallback colors.</summary>
[CreateAssetMenu(fileName = "UITabSkin", menuName = "IdleFilm/UI/Tab Skin")]
public class UITabSkin : ScriptableObject
{
    [Header("Sprites (optional)")]
    public Sprite mainNavActiveSprite;
    public Sprite mainNavInactiveSprite;
    public Sprite subTabActiveSprite;
    public Sprite subTabInactiveSprite;
    public Sprite categoryActiveSprite;
    public Sprite categoryInactiveSprite;
    public Sprite activeIndicatorSprite;

    [Header("Fallback Colors — Main Nav")]
    public Color mainNavActiveBg = HudSkinDefaults.TAB_ACTIVE_BG;
    public Color mainNavInactiveBg = HudSkinDefaults.TAB_INACTIVE_BG;
    public Color mainNavActiveLabel = HudSkinDefaults.TAB_ACTIVE_LABEL;
    public Color mainNavInactiveLabel = HudSkinDefaults.TAB_INACTIVE_LABEL;

    [Header("Fallback Colors — Sub Tab")]
    public Color subTabActiveBg = new Color(0.14f, 0.14f, 0.28f);
    public Color subTabInactiveBg = new Color(0.09f, 0.09f, 0.18f);
    public Color subTabActiveLabel = Color.white;
    public Color subTabInactiveLabel = new Color(0.55f, 0.55f, 0.70f);

    [Header("Fallback Colors — Category Tab")]
    public Color categoryActiveBg = HudSkinDefaults.BG_CARD2;
    public Color categoryInactiveBg = HudSkinDefaults.BG_CARD;
    public Color categoryActiveLabel = Color.white;
    public Color categoryInactiveLabel = HudSkinDefaults.TAB_INACTIVE_LABEL;

    [Header("Active Indicator")]
    public Color activeIndicatorColor = HudSkinDefaults.BTN_SUCCESS;

    public void Apply(Image background, TextMeshProUGUI label, bool active, HudTabVariant variant)
    {
        if (background == null) return;

        switch (variant)
        {
            case HudTabVariant.MainNav:
                HudSkinUtil.ApplySpriteOrColor(background, active ? mainNavActiveSprite : mainNavInactiveSprite,
                    active ? mainNavActiveBg : mainNavInactiveBg);
                if (label != null)
                    label.color = active ? mainNavActiveLabel : mainNavInactiveLabel;
                break;
            case HudTabVariant.SubTab:
                HudSkinUtil.ApplySpriteOrColor(background, active ? subTabActiveSprite : subTabInactiveSprite,
                    active ? subTabActiveBg : subTabInactiveBg);
                if (label != null)
                    label.color = active ? subTabActiveLabel : subTabInactiveLabel;
                break;
            case HudTabVariant.CategoryTab:
                HudSkinUtil.ApplySpriteOrColor(background, active ? categoryActiveSprite : categoryInactiveSprite,
                    active ? categoryActiveBg : categoryInactiveBg);
                if (label != null)
                    label.color = active ? categoryActiveLabel : categoryInactiveLabel;
                break;
        }
    }

    public void ApplyIndicator(Image indicator, bool active)
    {
        if (indicator == null) return;

        indicator.gameObject.SetActive(active);
        if (!active) return;

        HudSkinUtil.ApplySpriteOrColor(indicator, activeIndicatorSprite, activeIndicatorColor);
    }

    public static void ApplyFallback(Image background, TextMeshProUGUI label, bool active, HudTabVariant variant)
    {
        if (background == null) return;

        switch (variant)
        {
            case HudTabVariant.MainNav:
                HudSkinUtil.ApplySpriteOrColor(background, null, active ? HudSkinDefaults.TAB_ACTIVE_BG : HudSkinDefaults.TAB_INACTIVE_BG);
                if (label != null) label.color = active ? HudSkinDefaults.TAB_ACTIVE_LABEL : HudSkinDefaults.TAB_INACTIVE_LABEL;
                break;
            case HudTabVariant.SubTab:
                HudSkinUtil.ApplySpriteOrColor(background, null, active ? new Color(0.14f, 0.14f, 0.28f) : new Color(0.09f, 0.09f, 0.18f));
                if (label != null) label.color = active ? Color.white : new Color(0.55f, 0.55f, 0.70f);
                break;
            case HudTabVariant.CategoryTab:
                HudSkinUtil.ApplySpriteOrColor(background, null, active ? HudSkinDefaults.BG_CARD2 : HudSkinDefaults.BG_CARD);
                if (label != null) label.color = active ? Color.white : HudSkinDefaults.TAB_INACTIVE_LABEL;
                break;
        }
    }
}
