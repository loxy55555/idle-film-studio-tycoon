using UnityEngine;
using UnityEngine.UI;

/// <summary>Applies card background sprites or fallback colors to UI Images.</summary>
[CreateAssetMenu(fileName = "UICardSkin", menuName = "IdleFilm/UI/Card Skin")]
public class UICardSkin : ScriptableObject
{
    [Header("Sprites (optional)")]
    public Sprite primarySprite;
    public Sprite secondarySprite;
    public Sprite heroSprite;
    public Sprite emptySprite;

    [Header("Fallback Colors")]
    public Color primaryColor = HudSkinDefaults.BG_CARD;
    public Color secondaryColor = HudSkinDefaults.BG_CARD2;
    public Color heroColor = HudSkinDefaults.BG_SECTION;
    public Color emptyColor = HudSkinDefaults.BG_DEEP;

    public void Apply(Image image, HudCardVariant variant)
    {
        if (image == null) return;

        switch (variant)
        {
            case HudCardVariant.Primary:
                HudSkinUtil.ApplySpriteOrColor(image, primarySprite, primaryColor);
                break;
            case HudCardVariant.Secondary:
                HudSkinUtil.ApplySpriteOrColor(image, secondarySprite, secondaryColor);
                break;
            case HudCardVariant.Hero:
                HudSkinUtil.ApplySpriteOrColor(image, heroSprite, heroColor);
                break;
            case HudCardVariant.Empty:
                HudSkinUtil.ApplySpriteOrColor(image, emptySprite, emptyColor);
                break;
        }
    }

    public static void ApplyFallback(Image image, HudCardVariant variant)
    {
        if (image == null) return;

        switch (variant)
        {
            case HudCardVariant.Primary:   HudSkinUtil.ApplySpriteOrColor(image, null, HudSkinDefaults.BG_CARD); break;
            case HudCardVariant.Secondary: HudSkinUtil.ApplySpriteOrColor(image, null, HudSkinDefaults.BG_CARD2); break;
            case HudCardVariant.Hero:      HudSkinUtil.ApplySpriteOrColor(image, null, HudSkinDefaults.BG_SECTION); break;
            case HudCardVariant.Empty:     HudSkinUtil.ApplySpriteOrColor(image, null, HudSkinDefaults.BG_DEEP); break;
        }
    }
}
