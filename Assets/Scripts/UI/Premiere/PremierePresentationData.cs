using UnityEngine;

/// <summary>Premiere UI data — presentation only, no economy logic (Phase 8.5).</summary>
public readonly struct PremierePresentationData
{
    public readonly string movieName;
    public readonly MovieGenre genre;
    public readonly MovieRarity rarity;
    public readonly long moneyReward;
    public readonly float repGain;
    public readonly float xpGain;
    public readonly float varietyBonusPercent;
    public readonly bool isFirstDiscovery;
    public readonly Sprite posterSprite;
    public readonly string posterColorHex;

    public PremierePresentationData(
        string movieName,
        MovieGenre genre,
        MovieRarity rarity,
        long moneyReward,
        float repGain,
        float xpGain,
        float varietyBonusPercent,
        bool isFirstDiscovery,
        Sprite posterSprite,
        string posterColorHex)
    {
        this.movieName = movieName;
        this.genre = genre;
        this.rarity = rarity;
        this.moneyReward = moneyReward;
        this.repGain = repGain;
        this.xpGain = xpGain;
        this.varietyBonusPercent = varietyBonusPercent;
        this.isFirstDiscovery = isFirstDiscovery;
        this.posterSprite = posterSprite;
        this.posterColorHex = posterColorHex;
    }

    public static PremierePresentationData From(MovieCompletePayload payload)
    {
        var cfg = payload.config;
        return new PremierePresentationData(
            payload.movieName,
            cfg != null ? cfg.genre : default,
            cfg != null ? cfg.rarity : MovieRarity.Common,
            payload.moneyReward,
            payload.repGain,
            payload.xpGain,
            payload.varietyBonusPercent,
            payload.isFirstDiscovery,
            cfg != null ? cfg.posterSprite : null,
            cfg != null ? cfg.posterColorHex : null);
    }
}
