using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>Read-only catalog audit results (Phase CATÁLOGO 1).</summary>
[Serializable]
public class MovieCatalogAuditReport
{
    public int totalEntries;
    public int targetCount;
    public int slotsRemaining;
    public GenreCount[] genreCounts;
    public RarityCount[] rarityCounts;
    public DuplicateNameGroup[] exactDuplicateNames;
    public SimilarTitlePair[] similarTitles;
    public RepeatedWordEntry[] overusedWords;
    public SagaAuditEntry[] sagas;
    public LegendaryAuditEntry[] legendaries;
    public string[] warnings;
    public string[] errors;

    public bool HasErrors => errors != null && errors.Length > 0;
    public bool HasWarnings => warnings != null && warnings.Length > 0;

    [Serializable]
    public struct GenreCount
    {
        public MovieGenre genre;
        public int count;
        public int expectedPerGenre;
    }

    [Serializable]
    public struct RarityCount
    {
        public MovieRarity rarity;
        public int count;
    }

    [Serializable]
    public struct DuplicateNameGroup
    {
        public string displayName;
        public string[] catalogIds;
    }

    [Serializable]
    public struct SimilarTitlePair
    {
        public string catalogIdA;
        public string catalogIdB;
        public string titleA;
        public string titleB;
        public float similarity;
    }

    [Serializable]
    public struct RepeatedWordEntry
    {
        public string word;
        public int titleCount;
        public string[] sampleTitles;
    }

    [Serializable]
    public struct SagaAuditEntry
    {
        public string sagaId;
        public string displayName;
        public int expectedSize;
        public int assignedCount;
        public int[] missingIndices;
        public int[] duplicateIndices;
        public string[] orphanCatalogIds;
        public string[] memberCatalogIds;
    }

    [Serializable]
    public struct LegendaryAuditEntry
    {
        public MovieGenre genre;
        public int count;
        public string[] catalogIds;
    }
}

/// <summary>Validation rules for the master movie catalog.</summary>
public static class MovieCatalogAudit
{
    const float SimilarTitleThreshold = 0.82f;
    const int OverusedWordMinTitles = 5;

    static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "de", "la", "el", "en", "los", "las", "un", "una", "del", "al", "y", "o", "con", "por", "para", "a", "su", "sus", "que", "se", "es", "son",
    };

    public static MovieCatalogAuditReport Run(IReadOnlyList<MovieCatalogEntry> entries)
    {
        var list = entries?.Where(e => e != null).ToList() ?? new List<MovieCatalogEntry>();
        var warnings = new List<string>();
        var errors = new List<string>();

        int target = MovieCatalogSchema.TargetMovieCount;
        if (list.Count != target && list.Count < target)
            warnings.Add($"Catalog has {list.Count} entries; target is {target} ({target - list.Count} slots remaining).");

        var genreCounts = BuildGenreCounts(list, warnings);
        var rarityCounts = BuildRarityCounts(list, warnings);
        var exactDups = FindExactDuplicateNames(list, errors);
        var similar = list.Any(IsStructuralSlotEntry)
            ? SkippedSimilarTitles(warnings)
            : FindSimilarTitles(list, warnings);
        var overused = list.Any(IsStructuralSlotEntry)
            ? SkippedOverusedWords(warnings)
            : FindOverusedWords(list, warnings);
        var sagas = AuditSagas(list, warnings, errors);
        var legendaries = AuditLegendaries(list, warnings, errors);

        AuditApprovedGenres(list, errors);
        AuditCatalogIds(list, errors);

        return new MovieCatalogAuditReport
        {
            totalEntries = list.Count,
            targetCount = target,
            slotsRemaining = Mathf.Max(0, target - list.Count),
            genreCounts = genreCounts,
            rarityCounts = rarityCounts,
            exactDuplicateNames = exactDups,
            similarTitles = similar,
            overusedWords = overused,
            sagas = sagas,
            legendaries = legendaries,
            warnings = warnings.ToArray(),
            errors = errors.ToArray(),
        };
    }

    public static string FormatReport(MovieCatalogAuditReport report)
    {
        if (report == null) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("=== MOVIE CATALOG AUDIT ===");
        sb.AppendLine($"Schema v{MovieCatalogSchema.SchemaVersion} — {MovieCatalogSchema.GenreCount} genres, {MovieCatalogSchema.LegendaryCount} legendaries");
        sb.AppendLine($"Entries: {report.totalEntries} / {report.targetCount} (remaining {report.slotsRemaining})");
        sb.AppendLine();

        sb.AppendLine("-- Genre distribution --");
        if (report.genreCounts != null)
        {
            foreach (var g in report.genreCounts)
                sb.AppendLine($"  {g.genre}: {g.count} (expected {g.expectedPerGenre})");
        }

        sb.AppendLine();
        sb.AppendLine("-- Rarity distribution --");
        if (report.rarityCounts != null)
        {
            foreach (var r in report.rarityCounts)
                sb.AppendLine($"  {r.rarity}: {r.count}");
        }

        sb.AppendLine();
        sb.AppendLine($"-- Exact duplicate titles ({report.exactDuplicateNames?.Length ?? 0}) --");
        if (report.exactDuplicateNames != null)
        {
            foreach (var d in report.exactDuplicateNames)
                sb.AppendLine($"  \"{d.displayName}\" -> {string.Join(", ", d.catalogIds)}");
        }

        sb.AppendLine();
        sb.AppendLine($"-- Similar titles ({report.similarTitles?.Length ?? 0}, threshold {SimilarTitleThreshold:P0}) --");
        if (report.similarTitles != null)
        {
            foreach (var p in report.similarTitles.Take(40))
                sb.AppendLine($"  {p.similarity:P0}  \"{p.titleA}\"  ~  \"{p.titleB}\"  [{p.catalogIdA} / {p.catalogIdB}]");
            if (report.similarTitles.Length > 40)
                sb.AppendLine($"  ... and {report.similarTitles.Length - 40} more");
        }

        sb.AppendLine();
        sb.AppendLine($"-- Overused words (>={OverusedWordMinTitles} titles) --");
        if (report.overusedWords != null)
        {
            foreach (var w in report.overusedWords.Take(25))
                sb.AppendLine($"  {w.word}: {w.titleCount} titles (e.g. {string.Join("; ", w.sampleTitles.Take(2))})");
        }

        sb.AppendLine();
        sb.AppendLine("-- Sagas --");
        if (report.sagas != null)
        {
            foreach (var s in report.sagas)
            {
                sb.AppendLine($"  {s.sagaId} ({s.displayName}): {s.assignedCount}/{s.expectedSize}");
                if (s.missingIndices != null && s.missingIndices.Length > 0)
                    sb.AppendLine($"    missing indices: {string.Join(", ", s.missingIndices)}");
                if (s.duplicateIndices != null && s.duplicateIndices.Length > 0)
                    sb.AppendLine($"    duplicate indices: {string.Join(", ", s.duplicateIndices)}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("-- Legendaries (1 per genre) --");
        if (report.legendaries != null)
        {
            foreach (var leg in report.legendaries)
                sb.AppendLine($"  {leg.genre}: {leg.count} [{string.Join(", ", leg.catalogIds ?? System.Array.Empty<string>())}]");
        }

        AppendMessages(sb, "WARNINGS", report.warnings);
        AppendMessages(sb, "ERRORS", report.errors);
        return sb.ToString();
    }

    static void AppendMessages(StringBuilder sb, string label, string[] messages)
    {
        sb.AppendLine();
        sb.AppendLine($"-- {label} ({messages?.Length ?? 0}) --");
        if (messages == null || messages.Length == 0)
        {
            sb.AppendLine("  (none)");
            return;
        }

        foreach (var m in messages)
            sb.AppendLine($"  - {m}");
    }

    static MovieCatalogAuditReport.GenreCount[] BuildGenreCounts(
        List<MovieCatalogEntry> list, List<string> warnings)
    {
        var counts = new Dictionary<MovieGenre, int>();
        foreach (var genre in Enum.GetValues(typeof(MovieGenre)))
            counts[(MovieGenre)genre] = 0;

        foreach (var e in list)
            counts[e.genre]++;

        var result = new List<MovieCatalogAuditReport.GenreCount>();
        foreach (var genre in MovieCatalogSchema.DefinitiveGenreOrder)
        {
            int count = counts.TryGetValue(genre, out var n) ? n : 0;
            int expected = MovieCatalogSchema.TargetCountForGenre(genre);
            if (count != expected)
                warnings.Add($"Genre {genre}: {count} entries (expected {expected}).");
            result.Add(new MovieCatalogAuditReport.GenreCount
            {
                genre = genre,
                count = count,
                expectedPerGenre = expected,
            });
        }

        return result.ToArray();
    }

    static MovieCatalogAuditReport.RarityCount[] BuildRarityCounts(
        List<MovieCatalogEntry> list, List<string> warnings)
    {
        var counts = new Dictionary<MovieRarity, int>();
        foreach (MovieRarity r in Enum.GetValues(typeof(MovieRarity)))
            counts[r] = 0;

        foreach (var e in list)
            counts[e.rarity]++;

        if (counts[MovieRarity.Common] == list.Count && list.Count > 0)
            warnings.Add("All entries use rarity Common — rarity distribution not assigned yet.");

        var result = new List<MovieCatalogAuditReport.RarityCount>();
        foreach (var rarity in MovieCatalogSchema.RarityOrder)
        {
            result.Add(new MovieCatalogAuditReport.RarityCount
            {
                rarity = rarity,
                count = counts.TryGetValue(rarity, out var n) ? n : 0,
            });
        }

        return result.ToArray();
    }

    static MovieCatalogAuditReport.DuplicateNameGroup[] FindExactDuplicateNames(
        List<MovieCatalogEntry> list, List<string> errors)
    {
        var groups = list
            .Where(e => !string.IsNullOrWhiteSpace(e.displayName))
            .GroupBy(e => NormalizeTitle(e.displayName), StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => new MovieCatalogAuditReport.DuplicateNameGroup
            {
                displayName = g.First().displayName,
                catalogIds = g.Select(x => x.catalogId).ToArray(),
            })
            .ToArray();

        foreach (var g in groups)
            errors.Add($"Exact duplicate title \"{g.displayName}\" on {g.catalogIds.Length} entries.");

        return groups;
    }

    static MovieCatalogAuditReport.SimilarTitlePair[] SkippedSimilarTitles(List<string> warnings)
    {
        warnings.Add("Similar title scan skipped while structural catalog slots ([TBD] / _Slot_) are present.");
        return Array.Empty<MovieCatalogAuditReport.SimilarTitlePair>();
    }

    static bool IsStructuralSlotEntry(MovieCatalogEntry entry)
    {
        if (entry == null) return false;
        if (!string.IsNullOrEmpty(entry.catalogId) &&
            (entry.catalogId.Contains("_Slot_") || entry.catalogId.EndsWith("_Legendary")))
            return true;
        return IsPlaceholderTitle(entry.displayName);
    }

    static MovieCatalogAuditReport.SimilarTitlePair[] FindSimilarTitles(
        List<MovieCatalogEntry> list, List<string> warnings)
    {
        var pairs = new List<MovieCatalogAuditReport.SimilarTitlePair>();

        for (int i = 0; i < list.Count; i++)
        {
            for (int j = i + 1; j < list.Count; j++)
            {
                var a = list[i];
                var b = list[j];
                if (ShouldSkipSimilarityCompare(a, b))
                    continue;

                float sim = TitleSimilarity(a.displayName, b.displayName);
                if (sim < SimilarTitleThreshold) continue;

                pairs.Add(new MovieCatalogAuditReport.SimilarTitlePair
                {
                    catalogIdA = a.catalogId,
                    catalogIdB = b.catalogId,
                    titleA = a.displayName,
                    titleB = b.displayName,
                    similarity = sim,
                });
            }
        }

        pairs.Sort((x, y) => y.similarity.CompareTo(x.similarity));
        if (pairs.Count > 0)
            warnings.Add($"Found {pairs.Count} highly similar title pairs (>= {SimilarTitleThreshold:P0}).");

        return pairs.ToArray();
    }

    static MovieCatalogAuditReport.RepeatedWordEntry[] SkippedOverusedWords(List<string> warnings)
    {
        warnings.Add("Overused-word scan skipped while structural catalog slots are present.");
        return Array.Empty<MovieCatalogAuditReport.RepeatedWordEntry>();
    }

    static MovieCatalogAuditReport.RepeatedWordEntry[] FindOverusedWords(
        List<MovieCatalogEntry> list, List<string> warnings)
    {
        var wordToTitles = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in list)
        {
            if (string.IsNullOrWhiteSpace(entry.displayName)) continue;
            if (IsPlaceholderTitle(entry)) continue;
            foreach (var word in Tokenize(entry.displayName))
            {
                if (!wordToTitles.TryGetValue(word, out var titles))
                {
                    titles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    wordToTitles[word] = titles;
                }
                titles.Add(entry.displayName);
            }
        }

        var overused = wordToTitles
            .Where(p => p.Value.Count >= OverusedWordMinTitles)
            .OrderByDescending(p => p.Value.Count)
            .Select(p => new MovieCatalogAuditReport.RepeatedWordEntry
            {
                word = p.Key,
                titleCount = p.Value.Count,
                sampleTitles = p.Value.Take(4).ToArray(),
            })
            .ToArray();

        if (overused.Length > 0)
            warnings.Add($"Found {overused.Length} overused words appearing in >={OverusedWordMinTitles} titles.");

        return overused;
    }

    static MovieCatalogAuditReport.SagaAuditEntry[] AuditSagas(
        List<MovieCatalogEntry> list, List<string> warnings, List<string> errors)
    {
        var sagaMembers = list.Where(e => e.HasSaga).GroupBy(e => e.sagaId).ToList();
        var knownIds = new HashSet<string>();
        foreach (var def in SagaDatabase.All)
        {
            if (def != null && !string.IsNullOrEmpty(def.sagaId))
                knownIds.Add(def.sagaId);
        }

        var assignedIds = new HashSet<string>(sagaMembers.Select(g => g.Key));
        var result = new List<MovieCatalogAuditReport.SagaAuditEntry>();

        foreach (var def in SagaDatabase.All)
        {
            if (def == null || string.IsNullOrEmpty(def.sagaId)) continue;

            var members = list.Where(e => e.sagaId == def.sagaId).OrderBy(e => e.sagaOrder).ToList();
            var indices = members.Select(m => m.sagaOrder).ToList();
            var missing = new List<int>();
            var duplicates = indices.GroupBy(i => i).Where(g => g.Key > 0 && g.Count() > 1).Select(g => g.Key).ToList();

            for (int i = 1; i <= def.expectedEntryCount; i++)
            {
                if (!indices.Contains(i))
                    missing.Add(i);
            }

            if (members.Count != def.expectedEntryCount)
                warnings.Add($"Saga {def.sagaId}: {members.Count} assigned, expected {def.expectedEntryCount}.");

            if (missing.Count > 0)
                warnings.Add($"Saga {def.sagaId}: missing entry indices {string.Join(", ", missing)}.");

            if (duplicates.Count > 0)
                errors.Add($"Saga {def.sagaId}: duplicate sagaOrder values {string.Join(", ", duplicates)}.");

            result.Add(new MovieCatalogAuditReport.SagaAuditEntry
            {
                sagaId = def.sagaId,
                displayName = def.displayName,
                expectedSize = def.expectedEntryCount,
                assignedCount = members.Count,
                missingIndices = missing.ToArray(),
                duplicateIndices = duplicates.ToArray(),
                orphanCatalogIds = Array.Empty<string>(),
                memberCatalogIds = members.Select(m => m.catalogId).ToArray(),
            });
        }

        foreach (var group in sagaMembers)
        {
            if (knownIds.Contains(group.Key)) continue;
            var ids = group.Select(g => g.catalogId).ToArray();
            errors.Add($"Unknown sagaId \"{group.Key}\" on {ids.Length} entries.");
            result.Add(new MovieCatalogAuditReport.SagaAuditEntry
            {
                sagaId = group.Key,
                displayName = group.Key,
                expectedSize = 0,
                assignedCount = group.Count(),
                orphanCatalogIds = ids,
                memberCatalogIds = ids,
            });
        }

        var unassignedKnown = knownIds.Where(id => !assignedIds.Contains(id)).ToList();
        foreach (var id in unassignedKnown)
            warnings.Add($"Saga \"{id}\" has no movies assigned in catalog.");

        return result.ToArray();
    }

    static void AuditApprovedGenres(List<MovieCatalogEntry> list, List<string> errors)
    {
        foreach (var e in list)
        {
            if (!MovieCatalogSchema.IsApprovedGenre(e.genre))
                errors.Add($"Entry '{e.catalogId}' uses non-approved genre {e.genre}.");
        }
    }

    static MovieCatalogAuditReport.LegendaryAuditEntry[] AuditLegendaries(
        List<MovieCatalogEntry> list, List<string> warnings, List<string> errors)
    {
        var result = new List<MovieCatalogAuditReport.LegendaryAuditEntry>();
        int totalLegendary = 0;

        foreach (var genre in MovieCatalogSchema.DefinitiveGenreOrder)
        {
            var ids = list
                .Where(e => e.genre == genre && (e.rarity == MovieRarity.Legendary || e.isLegendary))
                .Select(e => e.catalogId)
                .ToArray();

            totalLegendary += ids.Length;

            if (ids.Length != MovieCatalogSchema.LegendariesPerGenre)
            {
                if (list.Count >= MovieCatalogSchema.TargetMovieCount)
                    warnings.Add($"Genre {genre}: {ids.Length} legendary (expected {MovieCatalogSchema.LegendariesPerGenre}).");
                else if (ids.Length == 0)
                    warnings.Add($"Genre {genre}: no legendary assigned yet.");
                else
                    warnings.Add($"Genre {genre}: {ids.Length} legendary entries.");
            }

            foreach (var entry in list.Where(e => e.genre == genre))
            {
                if (entry.isLegendary != (entry.rarity == MovieRarity.Legendary))
                    errors.Add($"Entry '{entry.catalogId}' legendary flag does not match rarity.");
            }

            result.Add(new MovieCatalogAuditReport.LegendaryAuditEntry
            {
                genre = genre,
                count = ids.Length,
                catalogIds = ids,
            });
        }

        if (list.Count >= MovieCatalogSchema.TargetMovieCount && totalLegendary != MovieCatalogSchema.LegendaryCount)
            warnings.Add($"Catalog has {totalLegendary} legendaries (expected {MovieCatalogSchema.LegendaryCount}).");
        else if (totalLegendary == 0 && list.Count > 0)
            warnings.Add("No legendary entries assigned yet.");

        return result.ToArray();
    }

    static void AuditCatalogIds(List<MovieCatalogEntry> list, List<string> errors)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var e in list)
        {
            if (string.IsNullOrEmpty(e.catalogId))
            {
                errors.Add("Entry with empty catalogId.");
                continue;
            }
            if (!ids.Add(e.catalogId))
                errors.Add($"Duplicate catalogId \"{e.catalogId}\".");
        }
    }

    static bool ShouldSkipSimilarityCompare(MovieCatalogEntry a, MovieCatalogEntry b)
    {
        if (a == null || b == null) return true;
        if (string.IsNullOrWhiteSpace(a.displayName) || string.IsNullOrWhiteSpace(b.displayName))
            return true;
        return IsPlaceholderTitle(a) || IsPlaceholderTitle(b);
    }

    static bool IsPlaceholderTitle(MovieCatalogEntry entry) =>
        IsPlaceholderTitle(entry.displayName) ||
        (!string.IsNullOrEmpty(entry.catalogId) &&
         (entry.catalogId.Contains("_Slot_") || entry.catalogId.EndsWith("_Legendary")));

    static bool IsPlaceholderTitle(string title) =>
        !string.IsNullOrWhiteSpace(title) &&
        (title.StartsWith("[TBD", StringComparison.OrdinalIgnoreCase) ||
         title.StartsWith("[ASSIGN", StringComparison.OrdinalIgnoreCase));

    public static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return string.Empty;
        var sb = new StringBuilder(title.Length);
        foreach (char c in title.Normalize(NormalizationForm.FormD))
        {
            if (char.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(c))
                sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    static IEnumerable<string> Tokenize(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) yield break;

        foreach (Match match in WordRegex.Matches(title))
        {
            string word = NormalizeTitle(match.Value);
            if (word.Length > 2 && !StopWords.Contains(word))
                yield return word;
        }
    }

    static readonly Regex WordRegex = new(@"[\p{L}\p{N}]+", RegexOptions.Compiled);

    static float TitleSimilarity(string a, string b)
    {
        string na = NormalizeTitle(a);
        string nb = NormalizeTitle(b);
        if (na.Length == 0 || nb.Length == 0) return 0f;
        if (na == nb) return 1f;
        if (na.Contains(nb) || nb.Contains(na))
            return Math.Max(0.85f, (float)Math.Min(na.Length, nb.Length) / Math.Max(na.Length, nb.Length));

        int dist = LevenshteinDistance(na, nb);
        int maxLen = Math.Max(na.Length, nb.Length);
        return 1f - dist / (float)maxLen;
    }

    static int LevenshteinDistance(string a, string b)
    {
        int n = a.Length, m = b.Length;
        var d = new int[n + 1, m + 1];
        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }
}
