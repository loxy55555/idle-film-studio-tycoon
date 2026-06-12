using UnityEngine;

/// <summary>Approved definitive legendary catalog (Phase 9.0) — presentation + asset ids.</summary>
public static class CollectionLegendaryRegistry
{
    public struct LegendaryDef
    {
        public MovieGenre genre;
        public string catalogId;
        public string displayName;
        public string posterColorHex;
    }

    public static readonly LegendaryDef[] All =
    {
        new() { genre = MovieGenre.Action,      catalogId = "Action_Legendary",      displayName = "Iron Wolves",                 posterColorHex = "#D4AF37" },
        new() { genre = MovieGenre.SciFi,       catalogId = "SciFi_Legendary",       displayName = "Echoes of Titan",             posterColorHex = "#1ABC9C" },
        new() { genre = MovieGenre.Drama,       catalogId = "Drama_Legendary",       displayName = "When the River Sleeps",       posterColorHex = "#3498DB" },
        new() { genre = MovieGenre.Horror,      catalogId = "Horror_Legendary",      displayName = "The Hollow House",            posterColorHex = "#8E44AD" },
        new() { genre = MovieGenre.Comedy,      catalogId = "Comedy_Legendary",      displayName = "Detective Potato",            posterColorHex = "#F39C12" },
        new() { genre = MovieGenre.Fantasy,     catalogId = "Fantasy_Legendary",     displayName = "Song of the Forgotten Realm", posterColorHex = "#9B59B6" },
        new() { genre = MovieGenre.Romance,     catalogId = "Romance_Legendary",     displayName = "A Sky for Two",               posterColorHex = "#E91E63" },
        new() { genre = MovieGenre.Thriller,    catalogId = "Thriller_Legendary",    displayName = "Red File 27",                 posterColorHex = "#34495E" },
        new() { genre = MovieGenre.Animation,   catalogId = "Animation_Legendary",   displayName = "Little Giants",               posterColorHex = "#E67E22" },
        new() { genre = MovieGenre.Documentary, catalogId = "Documentary_Legendary", displayName = "Voices of the Deep",          posterColorHex = "#607D8B" },
    };

    public static LegendaryDef GetDef(MovieGenre genre)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].genre == genre)
                return All[i];
        }
        return default;
    }

    public static MovieConfig FindInCatalog(MovieConfig[] catalog, MovieGenre genre)
    {
        var def = GetDef(genre);
        if (string.IsNullOrEmpty(def.catalogId) || catalog == null) return null;

        for (int i = 0; i < catalog.Length; i++)
        {
            var cfg = catalog[i];
            if (cfg == null) continue;
            if (cfg.name == def.catalogId || (cfg.genre == genre && cfg.rarity == MovieRarity.Legendary))
                return cfg;
        }

        return null;
    }

    public static bool IsApprovedLegendary(MovieConfig cfg)
    {
        if (cfg == null || cfg.rarity != MovieRarity.Legendary) return false;
        var def = GetDef(cfg.genre);
        return !string.IsNullOrEmpty(def.catalogId) &&
               (cfg.name == def.catalogId || cfg.movieName == def.displayName);
    }
}
