using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Rarity accent colors for production UI — display only (Phase 8.2).</summary>
public static class MovieRarityVisual
{
    static readonly Color CommonBlue   = new Color(0.12f, 0.22f, 0.42f);
    static readonly Color RarePurple   = new Color(0.45f, 0.22f, 0.72f);
    static readonly Color EpicOrange    = new Color(0.88f, 0.42f, 0.10f);
    static readonly Color LegendaryGold = new Color(0.92f, 0.76f, 0.18f);

    public static string GetLocKey(MovieRarity rarity) => rarity switch
    {
        MovieRarity.Rare       => LocKeys.ProdRarityRare,
        MovieRarity.Epic       => LocKeys.ProdRarityEpic,
        MovieRarity.Legendary  => LocKeys.ProdRarityLegendary,
        _                      => LocKeys.ProdRarityCommon,
    };

    public static Color GetAccentColor(MovieRarity rarity) => rarity switch
    {
        MovieRarity.Rare       => RarePurple,
        MovieRarity.Epic       => EpicOrange,
        MovieRarity.Legendary  => LegendaryGold,
        _                      => CommonBlue,
    };

    public static Color GetBadgeTextColor(MovieRarity rarity) => rarity switch
    {
        MovieRarity.Common     => new Color(0.65f, 0.75f, 0.92f),
        MovieRarity.Rare       => new Color(0.82f, 0.68f, 1f),
        MovieRarity.Epic       => new Color(1f, 0.62f, 0.22f),
        MovieRarity.Legendary  => new Color(1f, 0.88f, 0.35f),
        _                      => Color.white,
    };

    public static void ApplyFrame(Image frame, MovieRarity rarity)
    {
        if (frame == null) return;
        var accent = GetAccentColor(rarity);
        frame.color = new Color(accent.r, accent.g, accent.b, 0.55f);
    }

    public static void ApplyBadge(TextMeshProUGUI badge, MovieRarity rarity)
    {
        if (badge == null) return;
        badge.text = ProductionLoc.GetRarityLabel(rarity);
        badge.color = GetBadgeTextColor(rarity);
    }
}
