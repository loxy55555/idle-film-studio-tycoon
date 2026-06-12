#if UNITY_EDITOR
using UnityEngine;

/// <summary>Structural defaults for scaffolded MovieConfig slots — not gameplay balance.</summary>
public static class MovieCatalogSlotDefaults
{
    public static void ApplyNew(MovieConfig cfg, MovieCatalogImportRow row)
    {
        if (cfg == null || row == null) return;

        cfg.movieName = row.displayName;
        cfg.genre = row.genre;
        cfg.rarity = row.rarity;
        cfg.sagaId = row.sagaId ?? string.Empty;
        cfg.sagaEntryIndex = row.sagaOrder;
        cfg.sagaDisplayName = string.Empty;
        cfg.tagline = string.IsNullOrWhiteSpace(row.tagline) ? "[CATALOG SLOT]" : row.tagline;
        cfg.posterColorHex = string.IsNullOrWhiteSpace(row.posterColorHex)
            ? MovieCatalogSchema.DefaultPosterHex(row.genre)
            : row.posterColorHex;
        cfg.genreIconKey = string.Empty;
        cfg.posterFile = row.posterFile ?? string.Empty;
        cfg.posterSprite = null;
        cfg.contentKind = string.IsNullOrEmpty(cfg.sagaId) ? MovieContentKind.Standard : MovieContentKind.Saga;
        cfg.catalogTier = MovieCatalogTier.Tier1;
        cfg.unlockStudioLevel = 1;
        cfg.unlockCityLevel = row.cityUnlock > 0 ? row.cityUnlock : 1;
        cfg.unlockReputation = 0f;

        // Structural placeholders only — balance assigned in a later phase.
        cfg.cost = 1;
        cfg.baseReward = 1;
        cfg.baseRep = 0.1f;
        cfg.duration = 6f;
        cfg.quality = 1f;
    }

    public static void ApplyMetadataUpdate(MovieConfig cfg, MovieCatalogImportRow row)
    {
        if (cfg == null || row == null) return;

        cfg.movieName = row.displayName;
        cfg.genre = row.genre;
        cfg.rarity = row.rarity;
        cfg.sagaId = row.sagaId ?? string.Empty;
        cfg.sagaEntryIndex = row.sagaOrder;
        if (!string.IsNullOrWhiteSpace(row.tagline))
            cfg.tagline = row.tagline;
        if (!string.IsNullOrWhiteSpace(row.posterColorHex))
            cfg.posterColorHex = row.posterColorHex;
        if (!string.IsNullOrWhiteSpace(row.posterFile))
            cfg.posterFile = row.posterFile;
        if (row.cityUnlock > 0)
            cfg.unlockCityLevel = row.cityUnlock;
        cfg.contentKind = string.IsNullOrEmpty(cfg.sagaId) ? MovieContentKind.Standard : MovieContentKind.Saga;
    }

    public static MovieCatalogImportRow BuildTemplateRow(MovieGenre genre, int slotIndex, bool existsInProject)
    {
        bool legendary = slotIndex == MovieCatalogSchema.MoviesPerGenre;
        return new MovieCatalogImportRow
        {
            catalogId = MovieCatalogSchema.SlotCatalogId(genre, slotIndex),
            displayName = MovieCatalogSchema.SlotDisplayName(genre, slotIndex, legendary),
            genre = genre,
            rarity = legendary ? MovieRarity.Legendary : MovieRarity.Common,
            sagaId = string.Empty,
            sagaOrder = 0,
            isLegendary = legendary,
            posterColorHex = MovieCatalogSchema.DefaultPosterHex(genre),
            posterFile = string.Empty,
            tagline = "[CATALOG SLOT]",
            slotReserved = !existsInProject,
        };
    }
}
#endif
