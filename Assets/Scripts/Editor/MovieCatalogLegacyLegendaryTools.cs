#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>CSV helper to assign legendary rarity on existing legacy-genre movies (Phase CATÁLOGO 2).</summary>
public static class MovieCatalogLegacyLegendaryTools
{
    const string LegacyLegendaryCsvPath = "Assets/Data/Imports/LegacyLegendaryAssignments.csv";

    [MenuItem("IdleFilm/Catalog Migration/Export Legacy Legendary Assignment Template")]
    public static void ExportLegacyLegendaryTemplate()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LegacyLegendaryCsvPath));
        var sb = new StringBuilder();
        sb.AppendLine(MovieCatalogImportValidator.ImportHeader);
        sb.AppendLine("# Assign legendary rarity to ONE existing movie per legacy genre.");
        sb.AppendLine("# Update catalogId to an existing MovieConfig asset name (do not create a 31st movie).");

        foreach (var genre in MovieCatalogSchema.LegacyGenres)
        {
            sb.AppendLine(string.Join(",",
                "REPLACE_WITH_EXISTING_ID",
                $"[ASSIGN LEGENDARY {MovieCatalogSchema.GenreAssetPrefix(genre)}]",
                genre.ToString(),
                MovieRarity.Legendary.ToString(),
                string.Empty,
                "0",
                "1",
                MovieCatalogSchema.DefaultPosterHex(genre),
                string.Empty,
                "[LEGENDARY SLOT]",
                "0"));
        }

        File.WriteAllText(LegacyLegendaryCsvPath, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log($"[MovieCatalog] Legacy legendary assignment template exported to {LegacyLegendaryCsvPath}");
    }

    [MenuItem("IdleFilm/Catalog Migration/Import Legacy Legendary Assignments")]
    public static void ImportLegacyLegendaryAssignments()
    {
        if (!File.Exists(LegacyLegendaryCsvPath))
        {
            Debug.LogWarning($"[MovieCatalog] Missing {LegacyLegendaryCsvPath}. Export template first.");
            return;
        }

        var result = MovieCatalogImporter.ImportFromCsv(LegacyLegendaryCsvPath, createMissing: false, updateExisting: true);
        if (result.success)
        {
            var db = MovieCatalogDatabase.LoadOrCreateAsset();
            db.RebuildFromProject(runAudit: true);
            AssetDatabase.SaveAssets();
        }

        Debug.Log(result.success
            ? $"[MovieCatalog] Updated {result.updated} legacy legendary assignments."
            : "[MovieCatalog] Legacy legendary import failed validation.");
    }
}
#endif
