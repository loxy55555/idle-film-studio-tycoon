#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>CATÁLOGO 5.0 — import integrated premium catalog into MovieConfig assets.</summary>
public static class MovieCatalogIntegrationTools
{
    public const string IntegratedCsvPath = "Assets/Data/Imports/MovieCatalog_Integrated_300.csv";
    public const string PremiumExportPath = "Assets/Data/Exports/PremiumCatalogFinal.csv";
    public const string CommonExportPath = "Assets/Data/Exports/CommonCatalogFinal.csv";
    public const string SummaryPath = "Assets/Data/Reports/Catalog5_0_IntegrationSummary.txt";

    [MenuItem("IdleFilm/Catalog Migration/Integrate Catalog 5.0 (Premium 130)")]
    public static void IntegrateCatalog50()
    {
        if (EditorApplication.isCompiling)
        {
            Debug.LogWarning("[MovieCatalog] Wait for compilation before integrating Catalog 5.0.");
            return;
        }

        string scriptPath = "Assets/Data/Imports/integrate_catalog_5_0.py";
        if (!File.Exists(scriptPath))
        {
            Debug.LogError($"[MovieCatalog] Missing integration script: {scriptPath}");
            return;
        }

        Debug.Log("[MovieCatalog] Run integrate_catalog_5_0.py externally, then import integrated CSV.");
        ImportIntegratedCsv();
    }

    [MenuItem("IdleFilm/Catalog Migration/Import Integrated Catalog CSV (5.0)")]
    public static void ImportIntegratedCsv()
    {
        if (!File.Exists(IntegratedCsvPath))
        {
            Debug.LogError($"[MovieCatalog] Missing integrated catalog: {IntegratedCsvPath}");
            return;
        }

        var result = MovieCatalogImporter.ImportFromCsv(IntegratedCsvPath, createMissing: false, updateExisting: true);
        LogImportResult(result);

        if (result.success)
        {
            var db = MovieCatalogDatabase.LoadOrCreateAsset();
            db.RebuildFromProject(runAudit: true);
            AssetDatabase.SaveAssets();
            Debug.Log("[MovieCatalog] Catalog 5.0 integration import complete.");
        }
    }

    [MenuItem("IdleFilm/Catalog Migration/Integrate Catalog 5.0 (Premium 130)", true)]
    [MenuItem("IdleFilm/Catalog Migration/Import Integrated Catalog CSV (5.0)", true)]
    static bool IntegrationMenuEnabled() => !EditorApplication.isCompiling;

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
