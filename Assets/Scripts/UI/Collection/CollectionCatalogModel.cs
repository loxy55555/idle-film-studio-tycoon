using System.Collections.Generic;

/// <summary>Read-only collection grouping — Genre → Rarity → Movies (Phase 8.6).</summary>
public static class CollectionCatalogModel
{
    public struct MovieEntry
    {
        public MovieConfig config;
        public bool discovered;
        public bool legendaryLocked;
    }

    public struct RarityGroup
    {
        public MovieRarity rarity;
        public MovieEntry[] movies;
    }

    public struct GenreGroup
    {
        public MovieGenre genre;
        public int discovered;
        public int total;
        public RarityGroup[] rarities;
    }

    public struct CatalogView
    {
        public int discoveredTotal;
        public int targetTotal;
        public float targetPercent;
        public GenreGroup[] genres;
    }

    public static CatalogView Build(MovieConfig[] allMovies, IReadOnlyCollection<string> completedKeys)
    {
        var completed = completedKeys != null ? new HashSet<string>(completedKeys) : new HashSet<string>();
        bool revealAll = CollectionDebugState.RevealAll;
        var byGenre = new Dictionary<MovieGenre, List<MovieConfig>>();

        if (allMovies != null)
        {
            foreach (var m in allMovies)
            {
                if (m == null) continue;
                if (!byGenre.TryGetValue(m.genre, out var list))
                {
                    list = new List<MovieConfig>();
                    byGenre[m.genre] = list;
                }
                list.Add(m);
            }
        }

        var genreGroups = new List<GenreGroup>();

        foreach (var genre in CollectionGenreOrder.DisplayOrder)
        {
            if (!byGenre.TryGetValue(genre, out var movies))
            {
                movies = new List<MovieConfig>();
                byGenre[genre] = movies;
            }

            if (movies.Count == 0)
            {
                var legendaryOnly = CollectionLegendaryRegistry.FindInCatalog(allMovies, genre);
                if (legendaryOnly != null)
                    movies.Add(legendaryOnly);
            }

            if (movies.Count == 0)
                continue;

            movies.Sort((a, b) => ContentSortOrder.CompareMovies(a, b, null, null, null));

            int genreDisc = 0;
            var rarityMap = new Dictionary<MovieRarity, List<MovieEntry>>();

            foreach (var rarity in CollectionGenreOrder.RarityOrder)
                rarityMap[rarity] = new List<MovieEntry>();

            foreach (var cfg in movies)
            {
                bool disc = revealAll || MovieCollectionService.IsDiscovered(cfg, completedKeys);
                if (disc) genreDisc++;

                bool legLock = revealAll
                    ? false
                    : IsLegendaryPresentationLocked(cfg, movies, completed);

                var entry = new MovieEntry
                {
                    config = cfg,
                    discovered = disc,
                    legendaryLocked = legLock,
                };

                var r = cfg.rarity;
                if (!rarityMap.ContainsKey(r))
                    rarityMap[r] = new List<MovieEntry>();
                rarityMap[r].Add(entry);
            }

            EnsureLegendaryEntry(genre, movies, rarityMap, completedKeys, revealAll, ref genreDisc);

            var rarityGroups = new List<RarityGroup>();
            foreach (var rarity in CollectionGenreOrder.RarityOrder)
            {
                if (!rarityMap.TryGetValue(rarity, out var entries) || entries.Count == 0)
                    continue;

                rarityGroups.Add(new RarityGroup
                {
                    rarity = rarity,
                    movies = entries.ToArray(),
                });
            }

            genreGroups.Add(new GenreGroup
            {
                genre = genre,
                discovered = genreDisc,
                total = movies.Count,
                rarities = rarityGroups.ToArray(),
            });
        }

        int discoveredTotal = CountDiscovered(allMovies, completed);

        return new CatalogView
        {
            discoveredTotal = discoveredTotal,
            targetTotal = ContentScaleDatabase.TargetMovieCount,
            targetPercent = discoveredTotal * 100f / ContentScaleDatabase.TargetMovieCount,
            genres = genreGroups.ToArray(),
        };
    }

    static int CountDiscovered(MovieConfig[] allMovies, HashSet<string> completed)
    {
        int n = 0;
        foreach (var m in allMovies)
        {
            if (m != null && completed.Contains(m.name)) n++;
        }
        return n;
    }

    /// <summary>Presentation-only lock for legendary cards — does not affect gameplay.</summary>
    public static bool IsLegendaryPresentationLocked(
        MovieConfig cfg,
        IReadOnlyList<MovieConfig> genreMovies,
        IReadOnlyCollection<string> completedKeys)
    {
        if (cfg == null || cfg.rarity != MovieRarity.Legendary)
            return false;

        foreach (var m in genreMovies)
        {
            if (m == null || m.genre != cfg.genre) continue;
            if (m.rarity == MovieRarity.Legendary) continue;
            if (!MovieCollectionService.IsDiscovered(m, completedKeys))
                return true;
        }
        return false;
    }

    static void EnsureLegendaryEntry(
        MovieGenre genre,
        List<MovieConfig> genreMovies,
        Dictionary<MovieRarity, List<MovieEntry>> rarityMap,
        IReadOnlyCollection<string> completedKeys,
        bool revealAll,
        ref int genreDiscovered)
    {
        if (!rarityMap.TryGetValue(MovieRarity.Legendary, out var legendaryList))
        {
            legendaryList = new List<MovieEntry>();
            rarityMap[MovieRarity.Legendary] = legendaryList;
        }

        for (int i = 0; i < legendaryList.Count; i++)
        {
            if (legendaryList[i].config != null && legendaryList[i].config.rarity == MovieRarity.Legendary)
                return;
        }

        var cfg = FindLegendaryConfig(genre, genreMovies);
        if (cfg == null) return;

        if (!genreMovies.Contains(cfg))
            genreMovies.Add(cfg);

        bool disc = revealAll || MovieCollectionService.IsDiscovered(cfg, completedKeys);
        if (disc) genreDiscovered++;

        legendaryList.Add(new MovieEntry
        {
            config = cfg,
            discovered = disc,
            legendaryLocked = revealAll
                ? false
                : IsLegendaryPresentationLocked(cfg, genreMovies, completedKeys),
        });
    }

    static MovieConfig FindLegendaryConfig(MovieGenre genre, List<MovieConfig> genreMovies)
    {
        for (int i = 0; i < genreMovies.Count; i++)
        {
            var m = genreMovies[i];
            if (m != null && m.rarity == MovieRarity.Legendary)
                return m;
        }

        var def = CollectionLegendaryRegistry.GetDef(genre);
        if (string.IsNullOrEmpty(def.catalogId)) return null;

        for (int i = 0; i < genreMovies.Count; i++)
        {
            var m = genreMovies[i];
            if (m != null && m.name == def.catalogId)
                return m;
        }

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<MovieConfig>(
            $"{MovieCatalogDatabase.MoviesFolder}/{def.catalogId}.asset");
#else
        return null;
#endif
    }

}
