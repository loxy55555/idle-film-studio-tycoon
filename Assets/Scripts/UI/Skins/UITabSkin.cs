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
    public Color mainNavActiveBg      = HudSkinDefaults.TAB_ACTIVE_BG;
    public Color mainNavInactiveBg    = HudSkinDefaults.TAB_INACTIVE_BG;
    public Color mainNavActiveLabel   = HudSkinDefaults.TAB_ACTIVE_LABEL;   // gold
    public Color mainNavInactiveLabel = HudSkinDefaults.TAB_INACTIVE_LABEL;

    [Header("Fallback Colors — Sub Tab")]
    public Color subTabActiveBg      = CinematicTheme.CardBg2;
    public Color subTabInactiveBg    = CinematicTheme.PanelBg;
    public Color subTabActiveLabel   = CinematicTheme.GoldBright;
    public Color subTabInactiveLabel = CinematicTheme.TextDim;

    [Header("Fallback Colors — Category Tab")]
    public Color categoryActiveBg    = CinematicTheme.CardBg2;
    public Color categoryInactiveBg  = CinematicTheme.CardBg;
    public Color categoryActiveLabel = CinematicTheme.TextPrimary;
    public Color categoryInactiveLabel = CinematicTheme.TextDim;

    [Header("Active Indicator")]
    public Color activeIndicatorColor = CinematicTheme.GoldBase;

    public void Apply(Image background, TextMeshProUGUI label, bool active, HudTabVariant variant)
    {
        // Delegate to static fallbacks — always CinematicTheme / HudSkinDefaults, never stale asset colors.
        ApplyFallback(background, label, active, variant);
    }

    public void ApplyIndicator(Image indicator, bool active)
    {
        ApplyIndicatorFallback(indicator, active);
    }

    public static void ApplyFallback(Image background, TextMeshProUGUI label, bool active, HudTabVariant variant)
    {
        if (background == null) return;

        switch (variant)
        {
            case HudTabVariant.MainNav:
                HudSkinUtil.ApplySpriteOrColor(background, null, active ? HudSkinDefaults.TAB_ACTIVE_BG : HudSkinDefaults.TAB_INACTIVE_BG);
                if (label != null)
                {
                    label.color = active ? HudSkinDefaults.TAB_ACTIVE_LABEL : HudSkinDefaults.TAB_INACTIVE_LABEL;
                    label.fontStyle = active ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
                }
                break;
            case HudTabVariant.SubTab:
                HudSkinUtil.ApplySpriteOrColor(background, null, active ? CinematicTheme.CardBg2 : CinematicTheme.PanelBg);
                if (label != null)
                {
                    label.color = active ? CinematicTheme.GoldBright : CinematicTheme.TextDim;
                    label.fontStyle = active ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
                }
                break;
            case HudTabVariant.CategoryTab:
                HudSkinUtil.ApplySpriteOrColor(background, null, active ? CinematicTheme.CardBg2 : CinematicTheme.CardBg);
                if (label != null)
                    label.color = active ? CinematicTheme.TextPrimary : CinematicTheme.TextDim;
                break;
        }
    }

    public static void ApplyIndicatorFallback(Image indicator, bool active)
    {
        if (indicator == null) return;

        indicator.gameObject.SetActive(active);
        if (!active) return;

        HudSkinUtil.ApplySpriteOrColor(indicator, null, CinematicTheme.GoldBase);
    }
}
