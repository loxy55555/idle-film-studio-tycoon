using System;
using UnityEngine;

/// <summary>Stable master-catalog row for a single movie (Phase CATÁLOGO 1).</summary>
[Serializable]
public class MovieCatalogEntry
{
    [Tooltip("Stable unique id — matches MovieConfig asset name / save discovery key.")]
    public string catalogId;

    public string displayName;
    public MovieGenre genre;
    public MovieRarity rarity;
    public string sagaId;
    public int sagaOrder;
    public bool isLegendary;

    [Tooltip("Project-relative asset path (editor/export).")]
    public string assetPath;

    [Tooltip("Unity asset GUID for stable references across renames.")]
    public string assetGuid;

    [Header("Export hooks (Phase CATÁLOGO 1)")]
    public string posterColorHex;
    [Tooltip("Poster filename in Assets/Data/Posters — stable art reference (Phase CATÁLOGO 3.1).")]
    public string posterFile;
    public string tagline;

    public static MovieCatalogEntry FromMovieConfig(MovieConfig cfg, string assetPath = null, string assetGuid = null)
    {
        if (cfg == null) return null;

        return new MovieCatalogEntry
        {
            catalogId   = cfg.name,
            displayName = cfg.movieName,
            genre       = cfg.genre,
            rarity      = cfg.rarity,
            sagaId      = cfg.sagaId ?? string.Empty,
            sagaOrder   = cfg.sagaEntryIndex,
            isLegendary = cfg.rarity == MovieRarity.Legendary,
            assetPath   = assetPath ?? string.Empty,
            assetGuid   = assetGuid ?? string.Empty,
            posterColorHex = cfg.posterColorHex ?? string.Empty,
            posterFile     = cfg.posterFile ?? string.Empty,
            tagline     = cfg.tagline ?? string.Empty,
        };
    }

    public bool HasSaga => !string.IsNullOrEmpty(sagaId);
}
