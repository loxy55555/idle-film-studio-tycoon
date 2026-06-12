#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Bulk CSV import for MovieConfig + catalog metadata (Phase CATÁLOGO 2).</summary>
public static class MovieCatalogImporter
{
    public struct ImportResult
    {
        public int created;
        public int updated;
        public int skipped;
        public MovieCatalogImportValidator.RowIssue[] issues;
        public bool success;
    }

    public static ImportResult ImportFromCsv(string csvPath, bool createMissing = true, bool updateExisting = true)
    {
        var result = new ImportResult();
        if (!File.Exists(csvPath))
        {
            result.issues = new[]
            {
                new MovieCatalogImportValidator.RowIssue
                {
                    lineNumber = 0,
                    message = $"File not found: {csvPath}",
                    isError = true,
                },
            };
            return result;
        }

        var rows = MovieCatalogImportValidator.ParseCsv(File.ReadAllText(csvPath, Encoding.UTF8));
        var existingIds = LoadExistingCatalogIds();
        var validation = MovieCatalogImportValidator.ValidateRows(rows, existingIds);
        result.issues = validation.issues;

        if (validation.HasErrors)
        {
            result.success = false;
            return result;
        }

        Directory.CreateDirectory(MovieCatalogDatabase.MoviesFolder);

        foreach (var row in rows)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.catalogId)) continue;

            string assetPath = $"{MovieCatalogDatabase.MoviesFolder}/{row.catalogId}.asset";
            var cfg = AssetDatabase.LoadAssetAtPath<MovieConfig>(assetPath);

            if (cfg == null)
            {
                if (!createMissing)
                {
                    result.skipped++;
                    continue;
                }

                cfg = ScriptableObject.CreateInstance<MovieConfig>();
                MovieCatalogSlotDefaults.ApplyNew(cfg, row);
                AssetDatabase.CreateAsset(cfg, assetPath);
                result.created++;
            }
            else
            {
                if (!updateExisting)
                {
                    result.skipped++;
                    continue;
                }

                MovieCatalogSlotDefaults.ApplyMetadataUpdate(cfg, row);
                EditorUtility.SetDirty(cfg);
                result.updated++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        result.success = true;
        return result;
    }

    public static string BuildMigrationTemplateCsv(IReadOnlyDictionary<string, MovieCatalogEntry> existingById)
    {
        var sb = new StringBuilder();
        sb.AppendLine(MovieCatalogImportValidator.ImportHeader);

        foreach (var genre in MovieCatalogSchema.DefinitiveGenreOrder)
        {
            for (int slot = 1; slot <= MovieCatalogSchema.MoviesPerGenre; slot++)
            {
                string catalogId = MovieCatalogSchema.SlotCatalogId(genre, slot);
                bool exists = existingById != null && existingById.ContainsKey(catalogId);

                MovieCatalogEntry existing = null;
                existingById?.TryGetValue(catalogId, out existing);

                if (existing != null)
                {
                    sb.AppendLine(FormatTemplateRow(existing, slotReserved: false));
                    continue;
                }

                var template = MovieCatalogSlotDefaults.BuildTemplateRow(genre, slot, existsInProject: false);
                sb.AppendLine(FormatTemplateRow(template));
            }
        }

        return sb.ToString();
    }

    public static Dictionary<string, MovieCatalogEntry> BuildExistingMap()
    {
        var map = new Dictionary<string, MovieCatalogEntry>(System.StringComparer.Ordinal);
        foreach (var entry in MovieCatalogDatabase.BuildEntriesFromProject())
        {
            if (entry != null && !string.IsNullOrEmpty(entry.catalogId))
                map[entry.catalogId] = entry;
        }
        return map;
    }

    /// <summary>Map legacy assets (non-slot ids) by genre for template merge.</summary>
    public static Dictionary<MovieGenre, List<MovieCatalogEntry>> GroupLegacyEntriesByGenre()
    {
        var groups = new Dictionary<MovieGenre, List<MovieCatalogEntry>>();
        foreach (var genre in MovieCatalogSchema.DefinitiveGenreOrder)
            groups[genre] = new List<MovieCatalogEntry>();

        foreach (var entry in MovieCatalogDatabase.BuildEntriesFromProject())
        {
            if (entry == null) continue;
            if (entry.catalogId.Contains("_Slot_") || entry.catalogId.EndsWith("_Legendary"))
                continue;
            if (groups.TryGetValue(entry.genre, out var list))
                list.Add(entry);
        }

        foreach (var pair in groups)
            pair.Value.Sort((a, b) => string.Compare(a.catalogId, b.catalogId, System.StringComparison.Ordinal));

        return groups;
    }

    public static string BuildMergedMigrationTemplateCsv()
    {
        var existingById = BuildExistingMap();
        var legacyByGenre = GroupLegacyEntriesByGenre();
        var sb = new StringBuilder();
        sb.AppendLine(MovieCatalogImportValidator.ImportHeader);

        foreach (var genre in MovieCatalogSchema.DefinitiveGenreOrder)
        {
            var legacy = legacyByGenre.TryGetValue(genre, out var list) ? list : new List<MovieCatalogEntry>();
            int legacyIndex = 0;

            for (int slot = 1; slot <= MovieCatalogSchema.MoviesPerGenre; slot++)
            {
                string slotId = MovieCatalogSchema.SlotCatalogId(genre, slot);
                bool legendarySlot = slot == MovieCatalogSchema.MoviesPerGenre;

                if (existingById.TryGetValue(slotId, out var slotted))
                {
                    sb.AppendLine(FormatTemplateRow(slotted, slotReserved: false));
                    continue;
                }

                if (!legendarySlot && legacyIndex < legacy.Count)
                {
                    var legacyEntry = legacy[legacyIndex++];
                    sb.AppendLine(FormatTemplateRow(legacyEntry, slotReserved: false, mappedSlotId: slotId));
                    continue;
                }

                if (legendarySlot)
                {
                    var template = MovieCatalogSlotDefaults.BuildTemplateRow(genre, slot, existsInProject: false);
                    if (!MovieCatalogSchema.IsExpansionGenre(genre))
                    {
                        template.displayName = $"[ASSIGN LEGENDARY {MovieCatalogSchema.GenreAssetPrefix(genre)}]";
                        template.tagline = "[ASSIGN EXISTING MOVIE VIA CSV IMPORT]";
                    }
                    sb.AppendLine(FormatTemplateRow(template));
                    continue;
                }

                var slotTemplate = MovieCatalogSlotDefaults.BuildTemplateRow(genre, slot, existsInProject: false);
                sb.AppendLine(FormatTemplateRow(slotTemplate));
            }
        }

        return sb.ToString();
    }

    static string FormatTemplateRow(
        MovieCatalogEntry entry,
        bool slotReserved = true,
        string mappedSlotId = null)
    {
        return string.Join(",",
            Csv(mappedSlotId ?? entry.catalogId),
            Csv(entry.displayName),
            Csv(entry.genre.ToString()),
            Csv(entry.rarity.ToString()),
            Csv(entry.sagaId),
            entry.sagaOrder.ToString(),
            entry.isLegendary || entry.rarity == MovieRarity.Legendary ? "1" : "0",
            Csv(entry.posterColorHex),
            Csv(entry.posterFile),
            Csv(entry.tagline),
            slotReserved ? "1" : "0");
    }

    static string FormatTemplateRow(MovieCatalogImportRow row) =>
        string.Join(",",
            Csv(row.catalogId),
            Csv(row.displayName),
            Csv(row.genre.ToString()),
            Csv(row.rarity.ToString()),
            Csv(row.sagaId),
            row.sagaOrder.ToString(),
            row.isLegendary ? "1" : "0",
            Csv(row.posterColorHex),
            Csv(row.posterFile),
            Csv(row.tagline),
            row.slotReserved ? "1" : "0");

    static HashSet<string> LoadExistingCatalogIds()
    {
        var ids = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var guid in AssetDatabase.FindAssets("t:MovieConfig", new[] { MovieCatalogDatabase.MoviesFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var cfg = AssetDatabase.LoadAssetAtPath<MovieConfig>(path);
            if (cfg != null) ids.Add(cfg.name);
        }
        return ids;
    }

    static string Csv(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
#endif
