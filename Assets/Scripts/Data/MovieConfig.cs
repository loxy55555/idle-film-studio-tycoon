using UnityEngine;

public enum MovieGenre
{
    Action, Drama, Horror, Comedy, Romance, SciFi,
    Fantasy, Thriller, Animation, Documentary,
}

public enum MovieRarity { Common, Rare, Epic, Legendary }

[CreateAssetMenu(menuName = "IdleFilm/Movie")]
public class MovieConfig : ScriptableObject
{
    [Header("Identity")]
    public string movieName;
    public MovieGenre genre;
    [TextArea(2, 4)]
    public string tagline;
    [TextArea(3, 6)]
    public string synopsis;

    [Header("Localized Synopsis (leave blank to use default synopsis)")]
    [TextArea(3, 6)]
    public string synopsisEn;
    [TextArea(3, 6)]
    public string synopsisFr;
    [TextArea(3, 6)]
    public string synopsisDe;
    [TextArea(3, 6)]
    public string synopsisJa;

    [Header("Economy")]
    public long cost;
    public long baseReward;
    public float baseRep;

    [Header("Production")]
    public float duration;

    [Tooltip("Tier quality multiplier (1.0 basic → 5.0 blockbuster)")]
    public float quality;

    [Header("Unlock")]
    [Tooltip("Minimum studio level required. 0 = always available.")]
    public int unlockStudioLevel;

    [Tooltip("Minimum city level required. 1 = Garaje.")]
    public int unlockCityLevel = 1;

    [Tooltip("Minimum reputation required. 0 = no reputation gate.")]
    public float unlockReputation;

    [Header("Rarity (future display)")]
    public MovieRarity rarity = MovieRarity.Common;

    [Header("Visual (placeholder keys for future art)")]
    public string posterColorHex = "#3498DB";
    public string genreIconKey   = "";
    [Tooltip("Poster filename in Assets/Data/Posters (e.g. IronWolves.png). Linked by catalogId, not display title.")]
    public string posterFile     = "";
    public Sprite posterSprite;

    [Header("City progression")]
    public MovieCatalogTier catalogTier = MovieCatalogTier.Tier1;
    public MovieContentKind contentKind = MovieContentKind.Standard;
    [Tooltip("Saga id (e.g. shadow_trigger). Empty = standalone.")]
    public string sagaId = "";
    [Tooltip("Entry order inside saga (1-based).")]
    public int sagaEntryIndex = 0;
    [Tooltip("Optional saga display name override.")]
    public string sagaDisplayName = "";
}
