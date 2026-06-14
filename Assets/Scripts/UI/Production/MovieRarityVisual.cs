using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Rarity accent colors for production UI — Cinematic 2.0 (Phase 11).</summary>
public static class MovieRarityVisual
{
    // Accent colors used for badges and text
    static readonly Color CommonGrey     = new Color(0.55f, 0.58f, 0.62f);
    static readonly Color UncommonGreen  = new Color(0.28f, 0.78f, 0.42f);
    static readonly Color RareBlue       = new Color(0.35f, 0.55f, 0.97f);
    static readonly Color EpicPurple     = new Color(0.67f, 0.34f, 0.95f);
    static readonly Color LegendaryGold  = new Color(0.94f, 0.78f, 0.25f);

    // Subtle full-card background tints — Phase 13.4C
    // Alpha chosen to be clearly perceptible on the dark navy card base (~0.10,0.12,0.18)
    // while remaining low enough that white/light text stays fully legible.
    static readonly Color CommonBgTint    = new Color(0.40f, 0.48f, 0.60f, 0.22f); // soft blue-gray
    static readonly Color RareBgTint      = new Color(0.18f, 0.38f, 0.82f, 0.30f); // soft blue
    static readonly Color EpicBgTint      = new Color(0.48f, 0.18f, 0.74f, 0.30f); // soft violet
    static readonly Color LegendaryBgTint = new Color(0.74f, 0.60f, 0.08f, 0.32f); // soft gold

    public static string GetLocKey(MovieRarity rarity) => rarity switch
    {
        MovieRarity.Rare       => LocKeys.ProdRarityRare,
        MovieRarity.Epic       => LocKeys.ProdRarityEpic,
        MovieRarity.Legendary  => LocKeys.ProdRarityLegendary,
        _                      => LocKeys.ProdRarityCommon,
    };

    public static Color GetCardBgTint(MovieRarity rarity) => rarity switch
    {
        MovieRarity.Rare       => RareBgTint,
        MovieRarity.Epic       => EpicBgTint,
        MovieRarity.Legendary  => LegendaryBgTint,
        _                      => CommonBgTint,
    };

    public static Color GetAccentColor(MovieRarity rarity) => rarity switch
    {
        MovieRarity.Rare       => RareBlue,
        MovieRarity.Epic       => EpicPurple,
        MovieRarity.Legendary  => LegendaryGold,
        _                      => CommonGrey,
    };

    public static Color GetBadgeTextColor(MovieRarity rarity) => rarity switch
    {
        MovieRarity.Common    => new Color(0.70f, 0.82f, 0.96f),
        MovieRarity.Rare      => new Color(0.62f, 0.78f, 1.00f),
        MovieRarity.Epic      => new Color(0.86f, 0.62f, 1.00f),
        MovieRarity.Legendary => new Color(1.00f, 0.88f, 0.35f),
        _                     => Color.white,
    };

    /// <summary>Legacy frame hook for collection/premiere sub-frames — no full-card border.</summary>
    public static void ApplyFrame(Image frame, MovieRarity rarity)
    {
        if (frame == null) return;
        frame.color = Color.clear;
        RemoveLegacyStrip(frame.transform);
    }

    /// <summary>
    /// Phase 13.4C — full card background tint for production offer/slot cards.
    /// Replaces the previous 4-edge border: rarity is now communicated via a subtle
    /// semi-transparent color wash over the entire card background, maintaining legibility.
    /// </summary>
    public static void ApplyCardBorder(RectTransform cardRoot, MovieRarity rarity)
    {
        if (cardRoot == null) return;

        RemoveLegacyStrip(cardRoot);
        RemoveLegacyBorder(cardRoot);

        const string bgName = "__RarityBg";
        var bgT = cardRoot.Find(bgName);
        if (bgT == null)
        {
            var go = new GameObject(bgName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(cardRoot, false);
            bgT = go.transform;
            Stretch(bgT as RectTransform);
            var le = go.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
        }

        // Render behind all card content
        bgT.SetAsFirstSibling();
        var img = bgT.GetComponent<Image>();
        img.color = GetCardBgTint(rarity);
        img.raycastTarget = false;
        bgT.gameObject.SetActive(true);
    }

    static void RemoveLegacyStrip(Transform scope)
    {
        var strip = scope.Find("__RarityStrip");
        if (strip == null) return;
        if (Application.isPlaying) Object.Destroy(strip.gameObject);
        else Object.DestroyImmediate(strip.gameObject);
    }

    static void RemoveLegacyBorder(Transform scope)
    {
        var border = scope.Find("__RarityBorder");
        if (border == null) return;
        if (Application.isPlaying) Object.Destroy(border.gameObject);
        else Object.DestroyImmediate(border.gameObject);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    public static void ApplyBadge(TextMeshProUGUI badge, MovieRarity rarity)
    {
        if (badge == null) return;
        badge.text = ProductionLoc.GetRarityLabel(rarity);
        badge.color = GetBadgeTextColor(rarity);
    }
}
