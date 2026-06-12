#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Automatic poster sprite assignment via catalogId + posterFile (Phase CATÁLOGO 3.1).</summary>
public static class MovieCatalogPosterAssigner
{
    public struct AssignedEntry
    {
        public string catalogId;
        public string posterFile;
        public string spritePath;
    }

    public struct MissingEntry
    {
        public string catalogId;
        public string posterFile;
        public string reason;
    }

    public struct DuplicateEntry
    {
        public string posterKey;
        public string[] assetPaths;
    }

    public struct PosterCatalogDuplicateEntry
    {
        public string posterFile;
        public string[] catalogIds;
    }

    public struct AssignReport
    {
        public int scannedMovies;
        public int withPosterFile;
        public AssignedEntry[] assigned;
        public MissingEntry[] missing;
        public DuplicateEntry[] duplicateSprites;
        public PosterCatalogDuplicateEntry[] duplicatePosterFileRefs;
    }

    public static AssignReport AssignAll(string postersFolder = null)
    {
        postersFolder ??= MovieCatalogDatabase.PostersFolder;
        Directory.CreateDirectory(postersFolder);

        var spriteIndex = BuildSpriteIndex(postersFolder, out var duplicateSprites);
        var assigned = new List<AssignedEntry>();
        var missing = new List<MissingEntry>();
        var posterFileRefs = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        int scanned = 0;
        int withPosterFile = 0;

        foreach (var guid in AssetDatabase.FindAssets("t:MovieConfig", new[] { MovieCatalogDatabase.MoviesFolder }))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var cfg = AssetDatabase.LoadAssetAtPath<MovieConfig>(assetPath);
            if (cfg == null) continue;

            scanned++;
            string catalogId = cfg.name;
            string posterFile = cfg.posterFile?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(posterFile))
                continue;

            withPosterFile++;
            TrackPosterFileRef(posterFileRefs, posterFile, catalogId);

            string key = NormalizePosterKey(posterFile);
            if (string.IsNullOrEmpty(key))
            {
                missing.Add(new MissingEntry
                {
                    catalogId = catalogId,
                    posterFile = posterFile,
                    reason = "Invalid posterFile value.",
                });
                continue;
            }

            if (duplicateSprites.ContainsKey(key))
            {
                missing.Add(new MissingEntry
                {
                    catalogId = catalogId,
                    posterFile = posterFile,
                    reason = "Ambiguous poster filename — duplicate sprite assets in folder.",
                });
                continue;
            }

            if (!spriteIndex.TryGetValue(key, out var spriteEntry))
            {
                missing.Add(new MissingEntry
                {
                    catalogId = catalogId,
                    posterFile = posterFile,
                    reason = "Sprite not found in posters folder.",
                });
                continue;
            }

            if (cfg.posterSprite != spriteEntry.sprite)
            {
                cfg.posterSprite = spriteEntry.sprite;
                EditorUtility.SetDirty(cfg);
            }

            assigned.Add(new AssignedEntry
            {
                catalogId = catalogId,
                posterFile = posterFile,
                spritePath = spriteEntry.path,
            });
        }

        if (assigned.Count > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        return new AssignReport
        {
            scannedMovies = scanned,
            withPosterFile = withPosterFile,
            assigned = assigned.ToArray(),
            missing = missing.ToArray(),
            duplicateSprites = duplicateSprites.Values.ToArray(),
            duplicatePosterFileRefs = BuildPosterFileDuplicateRefs(posterFileRefs),
        };
    }

    public static string FormatReport(AssignReport report, string postersFolder)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Movie Catalog — Poster Assignment Report (Phase CATÁLOGO 3.1)");
        sb.AppendLine($"Posters folder: {postersFolder}");
        sb.AppendLine($"Scanned MovieConfig assets: {report.scannedMovies}");
        sb.AppendLine($"Entries with posterFile: {report.withPosterFile}");
        sb.AppendLine($"Assigned: {report.assigned?.Length ?? 0}");
        sb.AppendLine($"Missing: {report.missing?.Length ?? 0}");
        sb.AppendLine($"Duplicate sprite filenames: {report.duplicateSprites?.Length ?? 0}");
        sb.AppendLine($"Duplicate posterFile refs (same file, multiple catalogIds): {report.duplicatePosterFileRefs?.Length ?? 0}");
        sb.AppendLine();

        AppendSection(sb, "ASSIGNED", report.assigned, e => $"{e.catalogId}  posterFile={e.posterFile}  ->  {e.spritePath}");
        AppendSection(sb, "MISSING", report.missing, e => $"{e.catalogId}  posterFile={e.posterFile}  ({e.reason})");

        if (report.duplicateSprites != null && report.duplicateSprites.Length > 0)
        {
            sb.AppendLine("DUPLICATE SPRITE FILENAMES");
            foreach (var dup in report.duplicateSprites)
            {
                sb.AppendLine($"  {dup.posterKey}");
                foreach (var path in dup.assetPaths)
                    sb.AppendLine($"    - {path}");
            }
            sb.AppendLine();
        }

        if (report.duplicatePosterFileRefs != null && report.duplicatePosterFileRefs.Length > 0)
        {
            sb.AppendLine("DUPLICATE posterFile REFERENCES (informational — shared art allowed)");
            foreach (var dup in report.duplicatePosterFileRefs)
                sb.AppendLine($"  {dup.posterFile} -> {string.Join(", ", dup.catalogIds)}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    static void AppendSection<T>(StringBuilder sb, string title, T[] items, Func<T, string> line)
    {
        sb.AppendLine(title);
        if (items == null || items.Length == 0)
        {
            sb.AppendLine("  (none)");
            sb.AppendLine();
            return;
        }

        foreach (var item in items)
            sb.AppendLine("  " + line(item));
        sb.AppendLine();
    }

    struct SpriteEntry
    {
        public Sprite sprite;
        public string path;
    }

    static Dictionary<string, SpriteEntry> BuildSpriteIndex(
        string postersFolder,
        out Dictionary<string, DuplicateEntry> duplicateSprites)
    {
        var index = new Dictionary<string, SpriteEntry>(StringComparer.OrdinalIgnoreCase);
        var pathsByKey = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        duplicateSprites = new Dictionary<string, DuplicateEntry>(StringComparer.OrdinalIgnoreCase);

        if (!AssetDatabase.IsValidFolder(postersFolder))
            return index;

        foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { postersFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) continue;

            string key = NormalizePosterKey(Path.GetFileName(path));
            if (string.IsNullOrEmpty(key)) continue;

            if (!pathsByKey.TryGetValue(key, out var paths))
            {
                paths = new List<string>();
                pathsByKey[key] = paths;
            }
            paths.Add(path);
        }

        foreach (var pair in pathsByKey)
        {
            if (pair.Value.Count > 1)
            {
                duplicateSprites[pair.Key] = new DuplicateEntry
                {
                    posterKey = pair.Key,
                    assetPaths = pair.Value.ToArray(),
                };
                continue;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pair.Value[0]);
            if (sprite == null) continue;
            index[pair.Key] = new SpriteEntry { sprite = sprite, path = pair.Value[0] };
        }

        return index;
    }

    static void TrackPosterFileRef(Dictionary<string, List<string>> map, string posterFile, string catalogId)
    {
        if (!map.TryGetValue(posterFile, out var ids))
        {
            ids = new List<string>();
            map[posterFile] = ids;
        }
        ids.Add(catalogId);
    }

    static PosterCatalogDuplicateEntry[] BuildPosterFileDuplicateRefs(Dictionary<string, List<string>> map)
    {
        return map
            .Where(p => p.Value.Count > 1)
            .Select(p => new PosterCatalogDuplicateEntry
            {
                posterFile = p.Key,
                catalogIds = p.Value.ToArray(),
            })
            .OrderBy(p => p.posterFile, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string NormalizePosterKey(string posterFile)
    {
        if (string.IsNullOrWhiteSpace(posterFile)) return string.Empty;
        string fileName = Path.GetFileName(posterFile.Trim());
        if (string.IsNullOrEmpty(fileName)) return string.Empty;
        string stem = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
        var sb = new StringBuilder(stem.Length);
        foreach (char c in stem)
        {
            if (c >= 'a' && c <= 'z' || c >= '0' && c <= '9')
                sb.Append(c);
        }
        return sb.ToString();
    }
}
#endif
