#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>FASE 16.5E — restore movieName for corrupt player-visible titles only.</summary>
public static class CorruptTitleRestoreTools
{
    const string ReportPath = "Assets/Data/Reports/FASE165E_TitleRestoreReport.txt";

    [MenuItem("IdleFilm/Catalog Migration/Restore Corrupt Player Titles (FASE 16.5E)")]
    public static void RestoreCorruptPlayerTitles()
    {
        if (EditorApplication.isCompiling)
        {
            Debug.LogWarning("[FASE165E] Wait for compilation before restoring titles.");
            return;
        }

        var okSnapshot = SnapshotOkTitles();
        var log = new StringBuilder();
        int updated = 0;
        int unchanged = 0;
        int missing = 0;

        log.AppendLine("FASE 16.5E — Restore Corrupt Player Titles");
        log.AppendLine($"Whitelist count: {CorruptPlayerTitleWhitelist.CatalogIds.Length}");
        log.AppendLine();

        foreach (string catalogId in CorruptPlayerTitleWhitelist.CatalogIds)
        {
            string assetPath = $"{MovieCatalogDatabase.MoviesFolder}/{catalogId}.asset";
            var cfg = AssetDatabase.LoadAssetAtPath<MovieConfig>(assetPath);
            if (cfg == null)
            {
                missing++;
                log.AppendLine($"MISSING {catalogId}");
                continue;
            }

            string posterFile = cfg.posterFile?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(posterFile))
            {
                log.AppendLine($"SKIP {catalogId} (no posterFile)");
                unchanged++;
                continue;
            }

            string stem = Path.GetFileNameWithoutExtension(posterFile);
            string newName = PremiumPosterTitleDecoder.ToMovieName(stem);
            if (string.IsNullOrWhiteSpace(newName))
            {
                log.AppendLine($"SKIP {catalogId} (decoder empty for {posterFile})");
                unchanged++;
                continue;
            }

            if (cfg.movieName == newName)
            {
                unchanged++;
                log.AppendLine($"UNCHANGED {catalogId}: {newName}");
                continue;
            }

            string oldName = cfg.movieName;
            cfg.movieName = newName;
            EditorUtility.SetDirty(cfg);
            updated++;
            log.AppendLine($"UPDATED {catalogId}: \"{oldName}\" -> \"{newName}\"");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var db = MovieCatalogDatabase.LoadOrCreateAsset();
        db.RebuildFromProject(runAudit: true);
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        int okDrift = VerifyOkTitlesIntact(okSnapshot, log);
        Directory.CreateDirectory("Assets/Data/Reports");
        log.AppendLine();
        log.AppendLine($"Updated: {updated}");
        log.AppendLine($"Unchanged: {unchanged}");
        log.AppendLine($"Missing: {missing}");
        log.AppendLine($"OK_NOMBRE_POSTER drift: {okDrift}");
        File.WriteAllText(ReportPath, log.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();

        if (okDrift > 0)
            Debug.LogError($"[FASE165E] OK title drift detected ({okDrift}). See {ReportPath}");
        else
            Debug.Log($"[FASE165E] Restored {updated} titles. OK snapshot intact. Report: {ReportPath}");
    }

    [MenuItem("IdleFilm/Catalog Migration/Restore Corrupt Player Titles (FASE 16.5E)", true)]
    static bool RestoreMenuEnabled() => !EditorApplication.isCompiling;

    static Dictionary<string, string> SnapshotOkTitles()
    {
        var corrupt = new HashSet<string>(CorruptPlayerTitleWhitelist.CatalogIds);
        var snapshot = new Dictionary<string, string>();
        foreach (var guid in AssetDatabase.FindAssets("t:MovieConfig", new[] { MovieCatalogDatabase.MoviesFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var cfg = AssetDatabase.LoadAssetAtPath<MovieConfig>(path);
            if (cfg == null) continue;
            if (corrupt.Contains(cfg.name)) continue;
            snapshot[cfg.name] = cfg.movieName ?? string.Empty;
        }
        return snapshot;
    }

    static int VerifyOkTitlesIntact(Dictionary<string, string> snapshot, StringBuilder log)
    {
        int drift = 0;
        foreach (var pair in snapshot)
        {
            string assetPath = $"{MovieCatalogDatabase.MoviesFolder}/{pair.Key}.asset";
            var cfg = AssetDatabase.LoadAssetAtPath<MovieConfig>(assetPath);
            if (cfg == null) continue;
            string current = cfg.movieName ?? string.Empty;
            if (current != pair.Value)
            {
                drift++;
                log.AppendLine($"DRIFT {pair.Key}: \"{pair.Value}\" -> \"{current}\"");
            }
        }
        return drift;
    }
}
#endif
