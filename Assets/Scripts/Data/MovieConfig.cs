using UnityEngine;

public enum MovieGenre { Action, Drama, Horror, Comedy, Romance, SciFi }

[CreateAssetMenu(menuName = "IdleFilm/Movie")]
public class MovieConfig : ScriptableObject
{
    [Header("Identity")]
    public string movieName;
    public MovieGenre genre;
    [TextArea(2, 4)]
    public string tagline;

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

    [Header("Visual (placeholder keys for future art)")]
    public string posterColorHex = "#3498DB";
    public string genreIconKey   = "";
    public Sprite posterSprite;

    [Header("City progression")]
    public MovieCatalogTier catalogTier = MovieCatalogTier.Tier1;
    public MovieContentKind contentKind = MovieContentKind.Standard;
    [Tooltip("Future saga / universe id (e.g. hero_universe_01).")]
    public string sagaId = "";
}
