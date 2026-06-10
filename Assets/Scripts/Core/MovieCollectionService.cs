using System.Collections.Generic;
using UnityEngine;

/// <summary>Collection stats and saga progress from completedMovieKeys.</summary>
public static class MovieCollectionService
{
    public struct GenreProgress
    {
        public MovieGenre genre;
        public int discovered;
        public int total;
    }

    public struct SagaProgress
    {
        public string sagaId;
        public string displayName;
        public int discovered;
        public int total;
    }

    public struct Snapshot
    {
        public int discoveredTotal;
        public int catalogTotal;
        public float completionPercent;
        public GenreProgress[] genres;
        public SagaProgress[] sagas;
        public int sagasCompleted;
        public int sagasTracked;
        public MovieGenre topGenre;
        public int topGenreDiscovered;
        public int topGenreTotal;
        public string topGenreLabel;
        public string lastDiscoveredTitle;
    }

    static readonly MovieGenre[] AllGenres =
    {
        MovieGenre.Action, MovieGenre.Drama, MovieGenre.Horror,
        MovieGenre.Comedy, MovieGenre.Romance, MovieGenre.SciFi,
    };

    public static bool IsDiscovered(MovieConfig cfg, IReadOnlyCollection<string> completedKeys)
    {
        if (cfg == null || completedKeys == null) return false;
        foreach (var key in completedKeys)
        {
            if (key == cfg.name) return true;
        }
        return false;
    }

    public static Snapshot BuildSnapshot(
        MovieConfig[] allMovies,
        IReadOnlyCollection<string> completedKeys,
        IReadOnlyList<MovieHistoryEntry> history = null)
    {
        var completed = completedKeys != null ? new HashSet<string>(completedKeys) : new HashSet<string>();
        var snapshot = new Snapshot
        {
            genres = new GenreProgress[AllGenres.Length],
            lastDiscoveredTitle = FindLastDiscoveredTitle(allMovies, completed, history),
        };

        var genreCounts = new Dictionary<MovieGenre, (int discovered, int total)>();
        foreach (var g in AllGenres)
            genreCounts[g] = (0, 0);

        var sagaMap = new Dictionary<string, List<MovieConfig>>();

        if (allMovies != null)
        {
            foreach (var m in allMovies)
            {
                if (m == null) continue;
                snapshot.catalogTotal++;

                if (genreCounts.ContainsKey(m.genre))
                {
                    var pair = genreCounts[m.genre];
                    pair.total++;
                    if (completed.Contains(m.name)) pair.discovered++;
                    genreCounts[m.genre] = pair;
                }

                if (completed.Contains(m.name))
                    snapshot.discoveredTotal++;

                if (!string.IsNullOrEmpty(m.sagaId))
                {
                    if (!sagaMap.TryGetValue(m.sagaId, out var list))
                    {
                        list = new List<MovieConfig>();
                        sagaMap[m.sagaId] = list;
                    }
                    list.Add(m);
                }
            }
        }

        snapshot.completionPercent = snapshot.catalogTotal > 0
            ? snapshot.discoveredTotal * 100f / snapshot.catalogTotal
            : 0f;

        for (int i = 0; i < AllGenres.Length; i++)
        {
            var g = AllGenres[i];
            genreCounts.TryGetValue(g, out var pair);
            snapshot.genres[i] = new GenreProgress
            {
                genre = g,
                discovered = pair.discovered,
                total = pair.total,
            };
        }

        int bestDisc = -1;
        foreach (var g in AllGenres)
        {
            genreCounts.TryGetValue(g, out var pair);
            if (pair.discovered <= bestDisc) continue;
            bestDisc = pair.discovered;
            snapshot.topGenre = g;
            snapshot.topGenreDiscovered = pair.discovered;
            snapshot.topGenreTotal = pair.total;
            snapshot.topGenreLabel = GenreLabel(g);
        }

        var sagaList = new List<SagaProgress>();
        foreach (var pair in sagaMap)
        {
            int done = 0;
            string display = pair.Key;
            foreach (var m in pair.Value)
            {
                if (completed.Contains(m.name)) done++;
                if (!string.IsNullOrEmpty(m.movieName) && display == pair.Key)
                    display = FormatSagaName(pair.Key, m);
            }

            sagaList.Add(new SagaProgress
            {
                sagaId = pair.Key,
                displayName = display,
                discovered = done,
                total = pair.Value.Count,
            });

            if (done >= pair.Value.Count && pair.Value.Count > 0)
                snapshot.sagasCompleted++;
        }

        sagaList.Sort((a, b) => string.Compare(a.displayName, b.displayName, System.StringComparison.Ordinal));
        snapshot.sagas = sagaList.ToArray();
        snapshot.sagasTracked = sagaList.Count;

        return snapshot;
    }

    public static List<MovieConfig> SortForDisplay(
        MovieConfig[] allMovies,
        IReadOnlyCollection<string> completedKeys,
        StudioManager studio,
        StudioLevelSystem level,
        CitySystem city)
    {
        var list = new List<MovieConfig>();
        if (allMovies == null) return list;

        foreach (var m in allMovies)
        {
            if (m != null) list.Add(m);
        }

        list.Sort((a, b) =>
        {
            bool discA = IsDiscovered(a, completedKeys);
            bool discB = IsDiscovered(b, completedKeys);
            if (discA != discB) return discA ? -1 : 1;
            return ContentSortOrder.CompareMovies(a, b, studio, level, city);
        });

        return list;
    }

    static string FindLastDiscoveredTitle(
        MovieConfig[] allMovies,
        HashSet<string> completed,
        IReadOnlyList<MovieHistoryEntry> history)
    {
        if (history != null && allMovies != null)
        {
            var byKey = new Dictionary<string, MovieConfig>();
            foreach (var m in allMovies)
            {
                if (m != null && !byKey.ContainsKey(m.name))
                    byKey[m.name] = m;
            }

            foreach (var entry in history)
            {
                if (string.IsNullOrEmpty(entry.movieName)) continue;
                foreach (var pair in byKey)
                {
                    if (pair.Value.movieName == entry.movieName && completed.Contains(pair.Key))
                        return pair.Value.movieName;
                }
                if (completed.Contains(entry.movieName))
                    return entry.movieName;
            }
        }

        if (allMovies != null)
        {
            foreach (var m in allMovies)
            {
                if (m != null && completed.Contains(m.name))
                    return m.movieName;
            }
        }

        return "—";
    }

    static string FormatSagaName(string sagaId, MovieConfig sample)
    {
        if (!string.IsNullOrEmpty(sagaId))
        {
            string cleaned = sagaId.Replace('_', ' ').Replace('-', ' ');
            if (cleaned.Length > 0)
                return char.ToUpper(cleaned[0]) + cleaned.Substring(1);
        }
        return sample?.movieName ?? sagaId;
    }

    public static string GenreLabel(MovieGenre g) => g switch
    {
        MovieGenre.Action  => "Acción",
        MovieGenre.Drama   => "Drama",
        MovieGenre.Horror  => "Terror",
        MovieGenre.Comedy  => "Comedia",
        MovieGenre.Romance => "Romance",
        MovieGenre.SciFi   => "SciFi",
        _                  => g.ToString(),
    };

    public static string TierLabel(MovieCatalogTier tier) => tier switch
    {
        MovieCatalogTier.Tier1 => "Tier 1",
        MovieCatalogTier.Tier2 => "Tier 2",
        MovieCatalogTier.Tier3 => "Tier 3",
        MovieCatalogTier.Tier4 => "Tier 4",
        MovieCatalogTier.Epic  => "Épica",
        _                      => tier.ToString(),
    };
}
