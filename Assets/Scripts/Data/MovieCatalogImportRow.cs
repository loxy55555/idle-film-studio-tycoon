using System;
using System.Collections.Generic;

/// <summary>Single CSV row for bulk catalog import (Phase CATÁLOGO 2).</summary>
[Serializable]
public class MovieCatalogImportRow
{
    public int lineNumber;
    public string catalogId;
    public string displayName;
    public MovieGenre genre;
    public MovieRarity rarity;
    public string sagaId;
    public int sagaOrder;
    public bool isLegendary;
    public string posterColorHex;
    public string posterFile;
    public string tagline;
    public bool slotReserved;
    public int cityUnlock;
    public string genreRaw;
    public string rarityRaw;

    public bool HasSaga => !string.IsNullOrEmpty(sagaId);
}

/// <summary>CSV parse + row validation for catalog migration.</summary>
public static class MovieCatalogImportValidator
{
    public const string ImportHeader =
        "catalogId,displayName,genre,rarity,sagaId,sagaOrder,isLegendary,posterColorHex,posterFile,tagline,slotReserved";

    /// <summary>Phase CATÁLOGO 4.0 master CSV header.</summary>
    public const string DefinitiveImportHeader =
        "catalogId,movieName,genre,rarity,cityUnlock,sagaId,sagaOrder,isLegendary,posterFile,tagline";

    public struct RowIssue
    {
        public int lineNumber;
        public string catalogId;
        public string message;
        public bool isError;
    }

    public struct ValidationResult
    {
        public MovieCatalogImportRow[] rows;
        public RowIssue[] issues;
        public bool HasErrors;
    }

    public static ValidationResult ValidateRows(
        IReadOnlyList<MovieCatalogImportRow> rows,
        IReadOnlyCollection<string> existingCatalogIds = null)
    {
        var issues = new List<RowIssue>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var existing = existingCatalogIds != null
            ? new HashSet<string>(existingCatalogIds, StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);

        if (rows == null || rows.Count == 0)
        {
            issues.Add(Error(0, null, "Import file has no data rows."));
            return new ValidationResult { rows = Array.Empty<MovieCatalogImportRow>(), issues = issues.ToArray(), HasErrors = true };
        }

        foreach (var row in rows)
        {
            if (row == null)
            {
                issues.Add(Error(0, null, "Null row encountered."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.catalogId))
                issues.Add(Error(row.lineNumber, row.catalogId, "Missing catalogId."));
            else if (!seenIds.Add(row.catalogId))
                issues.Add(Error(row.lineNumber, row.catalogId, "Duplicate catalogId in import file."));

            if (string.IsNullOrWhiteSpace(row.genreRaw) || !MovieCatalogSchema.TryParseGenre(row.genreRaw, out _))
                issues.Add(Error(row.lineNumber, row.catalogId, $"Invalid genre '{row.genreRaw}'."));
            else if (!MovieCatalogSchema.IsApprovedGenre(row.genre))
                issues.Add(Error(row.lineNumber, row.catalogId, $"Genre '{row.genre}' is not in the definitive catalog."));

            if (string.IsNullOrWhiteSpace(row.rarityRaw) || !MovieCatalogSchema.TryParseRarity(row.rarityRaw, out _))
                issues.Add(Error(row.lineNumber, row.catalogId, $"Invalid rarity '{row.rarityRaw}'."));
            if (string.IsNullOrWhiteSpace(row.displayName))
                issues.Add(Warn(row.lineNumber, row.catalogId, "Missing displayName."));

            bool legendaryByRarity = row.rarity == MovieRarity.Legendary;
            if (row.isLegendary != legendaryByRarity)
                issues.Add(Error(row.lineNumber, row.catalogId,
                    "isLegendary flag must match rarity (Legendary => 1, otherwise 0)."));

            if (row.HasSaga)
            {
                if (row.sagaOrder <= 0)
                    issues.Add(Error(row.lineNumber, row.catalogId, "sagaOrder must be >= 1 when sagaId is set."));
                if (SagaDatabase.Get(row.sagaId) == null)
                    issues.Add(Warn(row.lineNumber, row.catalogId, $"Unknown sagaId '{row.sagaId}'."));
            }
            else if (row.sagaOrder > 0)
            {
                issues.Add(Warn(row.lineNumber, row.catalogId, "sagaOrder set without sagaId."));
            }
        }

        AuditLegendaryDistribution(rows, issues);

        return new ValidationResult
        {
            rows = rows as MovieCatalogImportRow[] ?? CopyRows(rows),
            issues = issues.ToArray(),
            HasErrors = issues.Exists(i => i.isError),
        };
    }

    static void AuditLegendaryDistribution(IReadOnlyList<MovieCatalogImportRow> rows, List<RowIssue> issues)
    {
        var byGenre = new Dictionary<MovieGenre, List<string>>();
        foreach (var genre in MovieCatalogSchema.DefinitiveGenreOrder)
            byGenre[genre] = new List<string>();

        foreach (var row in rows)
        {
            if (row == null || row.rarity != MovieRarity.Legendary) continue;
            if (byGenre.TryGetValue(row.genre, out var list))
                list.Add(row.catalogId);
        }

        int legendaryTotal = 0;
        foreach (var genre in MovieCatalogSchema.DefinitiveGenreOrder)
        {
            var ids = byGenre[genre];
            legendaryTotal += ids.Count;
            if (ids.Count > MovieCatalogSchema.LegendariesPerGenre)
            {
                issues.Add(Error(0, string.Join(",", ids),
                    $"Genre {genre} has {ids.Count} legendaries (expected {MovieCatalogSchema.LegendariesPerGenre})."));
            }
        }

        if (rows.Count >= MovieCatalogSchema.TargetMovieCount && legendaryTotal != MovieCatalogSchema.LegendaryCount)
        {
            issues.Add(Warn(0, null,
                $"Import has {legendaryTotal} legendaries (target {MovieCatalogSchema.LegendaryCount} — one per genre)."));
        }
    }

    public static MovieCatalogImportRow[] ParseCsv(string csvText)
    {
        if (string.IsNullOrWhiteSpace(csvText)) return Array.Empty<MovieCatalogImportRow>();

        var lines = SplitCsvLines(csvText);
        if (lines.Count == 0) return Array.Empty<MovieCatalogImportRow>();

        int start = 0;
        Dictionary<string, int> columnMap = null;
        if (lines[0].StartsWith("catalogId", StringComparison.OrdinalIgnoreCase))
        {
            columnMap = BuildColumnMap(ParseCsvLine(lines[0]));
            start = 1;
        }

        var rows = new List<MovieCatalogImportRow>();
        for (int i = start; i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            if (lines[i].TrimStart().StartsWith("#")) continue;
            var cols = ParseCsvLine(lines[i]);
            if (cols.Count == 0) continue;

            var row = new MovieCatalogImportRow { lineNumber = i + 1 };
            row.catalogId = GetColumn(cols, columnMap, "catalogId", 0);
            row.displayName = GetColumn(cols, columnMap, "displayName", 1);
            if (string.IsNullOrWhiteSpace(row.displayName))
                row.displayName = GetColumn(cols, columnMap, "movieName", -1);
            row.genreRaw = GetColumn(cols, columnMap, "genre", 2);
            row.rarityRaw = GetColumn(cols, columnMap, "rarity", 3);

            if (!MovieCatalogSchema.TryParseGenre(row.genreRaw, out row.genre))
                Enum.TryParse(row.genreRaw, true, out row.genre);

            if (!MovieCatalogSchema.TryParseRarity(row.rarityRaw, out row.rarity))
                Enum.TryParse(row.rarityRaw, true, out row.rarity);

            string cityRaw = GetColumn(cols, columnMap, "cityUnlock", -1);
            if (!string.IsNullOrWhiteSpace(cityRaw))
                int.TryParse(cityRaw, out row.cityUnlock);

            row.sagaId = GetColumn(cols, columnMap, "sagaId", 4);
            int.TryParse(GetColumn(cols, columnMap, "sagaOrder", 5), out row.sagaOrder);

            string legendaryRaw = GetColumn(cols, columnMap, "isLegendary", 6);
            row.isLegendary = legendaryRaw == "1" ||
                              string.Equals(legendaryRaw, "true", StringComparison.OrdinalIgnoreCase);

            row.posterColorHex = GetColumn(cols, columnMap, "posterColorHex", 7);

            if (columnMap != null)
            {
                row.posterFile = columnMap.ContainsKey("posterFile")
                    ? GetColumn(cols, columnMap, "posterFile", -1)
                    : string.Empty;
                row.tagline = columnMap.ContainsKey("tagline")
                    ? GetColumn(cols, columnMap, "tagline", -1)
                    : GetColumn(cols, columnMap, "tagline", 8);
                row.slotReserved = columnMap.ContainsKey("slotReserved")
                    && ParseBool(GetColumn(cols, columnMap, "slotReserved", 9));
            }
            else if (cols.Count >= 11)
            {
                row.posterFile = Col(cols, 8);
                row.tagline = Col(cols, 9);
                row.slotReserved = ParseBool(Col(cols, 10));
            }
            else
            {
                row.posterFile = string.Empty;
                row.tagline = Col(cols, 8);
                row.slotReserved = ParseBool(Col(cols, 9));
            }

            rows.Add(row);
        }

        return rows.ToArray();
    }

    static Dictionary<string, int> BuildColumnMap(List<string> headerCols)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headerCols.Count; i++)
        {
            string key = headerCols[i]?.Trim();
            if (string.IsNullOrEmpty(key)) continue;
            if (!map.ContainsKey(key))
                map[key] = i;
        }
        return map.Count > 0 ? map : null;
    }

    static string GetColumn(List<string> cols, Dictionary<string, int> map, string name, int fallbackIndex)
    {
        if (map != null && map.TryGetValue(name, out int idx))
            return idx >= 0 && idx < cols.Count ? cols[idx]?.Trim() ?? string.Empty : string.Empty;
        if (fallbackIndex >= 0 && fallbackIndex < cols.Count)
            return cols[fallbackIndex]?.Trim() ?? string.Empty;
        return string.Empty;
    }

    static bool ParseBool(string value) =>
        value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    static MovieCatalogImportRow[] CopyRows(IReadOnlyList<MovieCatalogImportRow> rows)
    {
        var copy = new MovieCatalogImportRow[rows.Count];
        for (int i = 0; i < rows.Count; i++) copy[i] = rows[i];
        return copy;
    }

    static RowIssue Error(int line, string id, string msg) =>
        new() { lineNumber = line, catalogId = id, message = msg, isError = true };

    static RowIssue Warn(int line, string id, string msg) =>
        new() { lineNumber = line, catalogId = id, message = msg, isError = false };

    static string Col(List<string> cols, int index) =>
        index < cols.Count ? cols[index]?.Trim() ?? string.Empty : string.Empty;

    static List<string> SplitCsvLines(string text)
    {
        var lines = new List<string>();
        var sb = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else inQuotes = !inQuotes;
                continue;
            }

            if ((c == '\n' || c == '\r') && !inQuotes)
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                if (sb.Length > 0) lines.Add(sb.ToString());
                sb.Clear();
                continue;
            }

            sb.Append(c);
        }

        if (sb.Length > 0) lines.Add(sb.ToString());
        return lines;
    }

    static List<string> ParseCsvLine(string line)
    {
        var cols = new List<string>();
        var sb = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else inQuotes = !inQuotes;
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                cols.Add(sb.ToString());
                sb.Clear();
                continue;
            }

            sb.Append(c);
        }

        cols.Add(sb.ToString());
        return cols;
    }
}
