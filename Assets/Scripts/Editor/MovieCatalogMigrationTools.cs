#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Migration scaffolding for the 300-movie definitive catalog (Phase CATÁLOGO 2).</summary>
public static class MovieCatalogMigrationTools
{
    const string TemplatePath = "Assets/Data/Exports/MovieCatalog_MigrationTemplate.csv";
    const string DefaultImportPath = "Assets/Data/Imports/MovieCatalog.csv";
    const string DefinitiveImportPath = "Assets/Data/Imports/MovieCatalog_Definitive_300.csv";

    [MenuItem("IdleFilm/Catalog Migration/Scaffold Missing Genre Slots (120)")]
    public static void ScaffoldExpansionGenres()
    {
        int created = 0;
        int skipped = 0;

        Directory.CreateDirectory(MovieCatalogDatabase.MoviesFolder);

        foreach (var genre in MovieCatalogSchema.ExpansionGenres)
        {
            for (int slot = 1; slot <= MovieCatalogSchema.MoviesPerGenre; slot++)
            {
                var row = MovieCatalogSlotDefaults.BuildTemplateRow(genre, slot, existsInProject: false);
                string assetPath = $"{MovieCatalogDatabase.MoviesFolder}/{row.catalogId}.asset";

                if (AssetDatabase.LoadAssetAtPath<MovieConfig>(assetPath) != null)
                {
                    skipped++;
                    continue;
                }

                var cfg = ScriptableObject.CreateInstance<MovieConfig>();
                MovieCatalogSlotDefaults.ApplyNew(cfg, row);
                AssetDatabase.CreateAsset(cfg, assetPath);
                created++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[MovieCatalog] Scaffold expansion genres — created {created}, skipped {skipped} (target +120 slots).");
    }

    [MenuItem("IdleFilm/Catalog Migration/Export Migration Template (300 slots)")]
    public static void ExportMigrationTemplate()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(TemplatePath));
        string csv = MovieCatalogImporter.BuildMergedMigrationTemplateCsv();
        File.WriteAllText(TemplatePath, csv, System.Text.Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log($"[MovieCatalog] Migration template exported to {TemplatePath}");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(TemplatePath));
    }

    [MenuItem("IdleFilm/Catalog Migration/Import Movie Catalog CSV...")]
    public static void ImportCsvDialog()
    {
        string path = EditorUtility.OpenFilePanel("Import Movie Catalog CSV", "Assets/Data/Imports", "csv");
        if (string.IsNullOrEmpty(path)) return;

        var result = MovieCatalogImporter.ImportFromCsv(path, createMissing: true, updateExisting: true);
        LogImportResult(result);

        if (result.success)
        {
            var db = MovieCatalogDatabase.LoadOrCreateAsset();
            db.RebuildFromProject(runAudit: true);
            AssetDatabase.SaveAssets();
        }
    }

    [MenuItem("IdleFilm/Catalog Migration/Import Definitive Catalog CSV (300)")]
    public static void ImportDefinitiveCsv()
    {
        if (!File.Exists(DefinitiveImportPath))
        {
            Debug.LogError($"[MovieCatalog] Definitive import file missing: {DefinitiveImportPath}");
            return;
        }

        SyncDefinitiveSagaAssets();
        PruneNonDefinitiveMovieAssets();

        var result = MovieCatalogImporter.ImportFromCsv(DefinitiveImportPath, createMissing: true, updateExisting: true);
        LogImportResult(result);

        if (result.success)
        {
            var db = MovieCatalogDatabase.LoadOrCreateAsset();
            db.RebuildFromProject(runAudit: true);
            AssetDatabase.SaveAssets();
            Debug.Log("[MovieCatalog] Definitive 300-movie catalog import complete.");
        }
    }

    [MenuItem("IdleFilm/Catalog Migration/Sync Definitive Saga Assets")]
    public static void SyncDefinitiveSagaAssets()
    {
        Directory.CreateDirectory("Assets/Resources/Sagas");

        var defs = new (string id, string name, SagaSizeClass size, int count, int firstCity)[]
        {
            ("project_avalanche", "Project Avalanche", SagaSizeClass.Large, 6, 2),
            ("dominion", "Dominion", SagaSizeClass.Large, 6, 3),
            ("atlas_signal", "Atlas Signal", SagaSizeClass.Medium, 4, 3),
            ("the_long_road", "The Long Road", SagaSizeClass.Medium, 4, 2),
            ("hollow_creek", "Hollow Creek", SagaSizeClass.Medium, 4, 3),
            ("moonkeeper", "Moonkeeper", SagaSizeClass.Medium, 4, 4),
            ("the_last_colony", "The Last Colony", SagaSizeClass.Large, 6, 5),
            ("winter_letters", "Winter Letters", SagaSizeClass.Mini, 2, 2),
            ("blackwater", "Blackwater", SagaSizeClass.Medium, 4, 4),
            ("uncle_gary", "Uncle Gary", SagaSizeClass.Mini, 2, 3),
            ("the_hidden_crown", "The Hidden Crown", SagaSizeClass.Medium, 4, 5),
            ("the_glass_forest", "The Glass Forest", SagaSizeClass.Mini, 2, 4),
            ("beneath_the_summer_sky", "Beneath the Summer Sky", SagaSizeClass.Mini, 2, 3),
            ("the_black_ledger", "The Black Ledger", SagaSizeClass.Medium, 4, 5),
            ("shadow_trigger", "Shadow Trigger", SagaSizeClass.Medium, 4, 2),
        };

        var keep = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var d in defs) keep.Add(d.id);

        foreach (var guid in AssetDatabase.FindAssets("t:SagaDefinition", new[] { "Assets/Resources/Sagas" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var existing = AssetDatabase.LoadAssetAtPath<SagaDefinition>(path);
            if (existing != null && !keep.Contains(existing.sagaId))
                AssetDatabase.DeleteAsset(path);
        }

        int created = 0;
        int updated = 0;
        foreach (var d in defs)
        {
            string path = $"Assets/Resources/Sagas/saga_{d.id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<SagaDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<SagaDefinition>();
                AssetDatabase.CreateAsset(asset, path);
                created++;
            }
            else updated++;

            asset.sagaId = d.id;
            asset.displayName = d.name;
            asset.sizeClass = d.size;
            asset.expectedEntryCount = d.count;
            asset.firstEntryCity = d.firstCity;
            EditorUtility.SetDirty(asset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[MovieCatalog] Synced definitive saga assets — created {created}, updated {updated}.");
    }

    [MenuItem("IdleFilm/Catalog Migration/Prune Legacy Movie Assets")]
    public static void PruneNonDefinitiveMovieAssets()
    {
        if (!File.Exists(DefinitiveImportPath))
        {
            Debug.LogError($"[MovieCatalog] Cannot prune — missing {DefinitiveImportPath}");
            return;
        }

        var rows = MovieCatalogImportValidator.ParseCsv(File.ReadAllText(DefinitiveImportPath, System.Text.Encoding.UTF8));
        var keep = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var row in rows)
        {
            if (row != null && !string.IsNullOrWhiteSpace(row.catalogId))
                keep.Add(row.catalogId);
        }

        int removed = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:MovieConfig", new[] { MovieCatalogDatabase.MoviesFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string id = System.IO.Path.GetFileNameWithoutExtension(path);
            if (keep.Contains(id)) continue;
            if (AssetDatabase.DeleteAsset(path))
                removed++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[MovieCatalog] Pruned {removed} legacy movie assets (keeping {keep.Count} definitive entries).");
    }

    [MenuItem("IdleFilm/Catalog Migration/Import Default CSV")]
    public static void ImportDefaultCsv()
    {
        if (!File.Exists(DefaultImportPath))
        {
            Debug.LogWarning($"[MovieCatalog] Default import file missing: {DefaultImportPath}. Export migration template first.");
            return;
        }

        var result = MovieCatalogImporter.ImportFromCsv(DefaultImportPath);
        LogImportResult(result);

        if (result.success)
        {
            var db = MovieCatalogDatabase.LoadOrCreateAsset();
            db.RebuildFromProject(runAudit: true);
            AssetDatabase.SaveAssets();
        }
    }

    [MenuItem("IdleFilm/Catalog Migration/Run Definitive Catalog Migration")]
    public static void RunFullMigration()
    {
        ScaffoldExpansionGenres();
        ExportMigrationTemplate();

        var db = MovieCatalogDatabase.LoadOrCreateAsset();
        db.RebuildFromProject(runAudit: true);
        AssetDatabase.SaveAssets();

        string report = MovieCatalogAudit.FormatReport(db.lastAudit);
        Directory.CreateDirectory("Assets/Data/Reports");
        File.WriteAllText("Assets/Data/Reports/MovieCatalogMigration.txt", report, System.Text.Encoding.UTF8);
        AssetDatabase.Refresh();

        Debug.Log($"[MovieCatalog] Definitive migration pass complete.\n{report}");
    }

    [MenuItem("IdleFilm/Catalog Migration/Prune Legacy Movie Assets", true)]
    [MenuItem("IdleFilm/Catalog Migration/Import Definitive Catalog CSV (300)", true)]
    [MenuItem("IdleFilm/Catalog Migration/Sync Definitive Saga Assets", true)]
    [MenuItem("IdleFilm/Catalog Migration/Scaffold Missing Genre Slots (120)", true)]
    [MenuItem("IdleFilm/Catalog Migration/Export Migration Template (300 slots)", true)]
    [MenuItem("IdleFilm/Catalog Migration/Import Movie Catalog CSV...", true)]
    [MenuItem("IdleFilm/Catalog Migration/Import Default CSV", true)]
    [MenuItem("IdleFilm/Catalog Migration/Run Definitive Catalog Migration", true)]
    static bool MigrationMenuEnabled() => !EditorApplication.isCompiling;

    static void LogImportResult(MovieCatalogImporter.ImportResult result)
    {
        if (result.issues != null)
        {
            foreach (var issue in result.issues)
            {
                string prefix = issue.isError ? "ERROR" : "WARN";
                string msg = $"[{prefix}] line {issue.lineNumber} {issue.catalogId}: {issue.message}";
                if (issue.isError) Debug.LogError(msg);
                else Debug.LogWarning(msg);
            }
        }

        if (result.success)
            Debug.Log($"[MovieCatalog] Import complete — created {result.created}, updated {result.updated}, skipped {result.skipped}.");
        else
            Debug.LogError("[MovieCatalog] Import aborted due to validation errors.");
    }
}
#endif
