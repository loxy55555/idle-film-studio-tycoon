using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Master catalog index for all MovieConfig assets (Phase CATÁLOGO 1).
/// Read-only at runtime — rebuild from editor via IdleFilm/Rebuild Movie Catalog Database.
/// </summary>
[CreateAssetMenu(menuName = "IdleFilm/Movie Catalog Database", fileName = "MovieCatalogDatabase")]
public class MovieCatalogDatabase : ScriptableObject
{
    public const string DefaultAssetPath = "Assets/Data/MovieCatalogDatabase.asset";
    public const string MoviesFolder = "Assets/Data/Movies";
    public const string PostersFolder = "Assets/Posters";

    public int targetMovieCount = ContentScaleDatabase.TargetMovieCount;
    public int schemaVersion = MovieCatalogSchema.SchemaVersion;
    public string builtAtUtc;
    public MovieCatalogEntry[] entries = Array.Empty<MovieCatalogEntry>();
    public MovieCatalogAuditReport lastAudit;

    static MovieCatalogDatabase _cached;

    public int Count => entries?.Length ?? 0;

    public static MovieCatalogDatabase Instance
    {
        get
        {
            if (_cached != null) return _cached;
            _cached = Resources.Load<MovieCatalogDatabase>("MovieCatalogDatabase");
            return _cached;
        }
    }

    public MovieCatalogEntry FindById(string catalogId)
    {
        if (string.IsNullOrEmpty(catalogId) || entries == null) return null;
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] != null && entries[i].catalogId == catalogId)
                return entries[i];
        }
        return null;
    }

    public IReadOnlyList<MovieCatalogEntry> GetEntriesSorted()
    {
        if (entries == null) return Array.Empty<MovieCatalogEntry>();
        return entries
            .Where(e => e != null)
            .OrderBy(e => Array.IndexOf(MovieCatalogSchema.DefinitiveGenreOrder, e.genre))
            .ThenBy(e => e.rarity)
            .ThenBy(e => e.displayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public MovieCatalogAuditReport RunAudit()
    {
        lastAudit = MovieCatalogAudit.Run(entries);
        return lastAudit;
    }

    /// <summary>CSV header for export / poster-generation pipelines.</summary>
    public static string CsvHeader =>
        "catalogId,displayName,genre,rarity,sagaId,sagaOrder,isLegendary,assetPath,assetGuid,posterColorHex,posterFile,tagline";

    public string ToCsv(bool includeHeader = true)
    {
        var sb = new StringBuilder();
        if (includeHeader) sb.AppendLine(CsvHeader);

        if (entries == null) return sb.ToString();

        foreach (var entry in GetEntriesSorted())
            sb.AppendLine(FormatCsvRow(entry));

        return sb.ToString();
    }

    static string FormatCsvRow(MovieCatalogEntry entry)
    {
        return string.Join(",",
            Csv(entry.catalogId),
            Csv(entry.displayName),
            Csv(entry.genre.ToString()),
            Csv(entry.rarity.ToString()),
            Csv(entry.sagaId),
            entry.sagaOrder.ToString(),
            entry.isLegendary ? "1" : "0",
            Csv(entry.assetPath),
            Csv(entry.assetGuid),
            Csv(entry.posterColorHex),
            Csv(entry.posterFile),
            Csv(entry.tagline));
    }

    static string Csv(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

#if UNITY_EDITOR
    public static MovieCatalogEntry[] BuildEntriesFromProject()
    {
        var guids = UnityEditor.AssetDatabase.FindAssets("t:MovieConfig", new[] { MoviesFolder });
        var list = new List<MovieCatalogEntry>(guids.Length);

        foreach (var guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<MovieConfig>(path);
            if (cfg == null) continue;
            list.Add(MovieCatalogEntry.FromMovieConfig(cfg, path, guid));
        }

        list.Sort((a, b) => string.Compare(a.catalogId, b.catalogId, StringComparison.Ordinal));
        return list.ToArray();
    }

    public static MovieCatalogDatabase LoadOrCreateAsset()
    {
        var db = UnityEditor.AssetDatabase.LoadAssetAtPath<MovieCatalogDatabase>(DefaultAssetPath);
        if (db != null) return db;

        db = CreateInstance<MovieCatalogDatabase>();
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(DefaultAssetPath));
        UnityEditor.AssetDatabase.CreateAsset(db, DefaultAssetPath);
        return db;
    }

    public void RebuildFromProject(bool runAudit = true)
    {
        entries = BuildEntriesFromProject();
        targetMovieCount = MovieCatalogSchema.TargetMovieCount;
        schemaVersion = MovieCatalogSchema.SchemaVersion;
        builtAtUtc = DateTime.UtcNow.ToString("o");

        if (runAudit)
            lastAudit = RunAudit();

        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
