#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Editor tools to rebuild, validate, and export the master movie catalog (Phase CATÁLOGO 1).</summary>
public static class MovieCatalogTools
{
    const string ReportPath = "Assets/Data/Reports/MovieCatalogAudit.txt";
    const string CsvExportPath = "Assets/Data/Exports/MovieCatalog.csv";

    [MenuItem("IdleFilm/Rebuild Movie Catalog Database")]
    public static void RebuildDatabase()
    {
        var db = MovieCatalogDatabase.LoadOrCreateAsset();
        db.RebuildFromProject(runAudit: true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[MovieCatalog] Rebuilt database with {db.Count} entries at {MovieCatalogDatabase.DefaultAssetPath}");
        LogAuditSummary(db.lastAudit);
    }

    [MenuItem("IdleFilm/Validate Movie Catalog")]
    public static void ValidateCatalog()
    {
        var db = MovieCatalogDatabase.LoadOrCreateAsset();
        if (db.entries == null || db.entries.Length == 0)
            db.RebuildFromProject(runAudit: false);

        var report = db.RunAudit();
        db.lastAudit = report;
        EditorUtility.SetDirty(db);

        string text = MovieCatalogAudit.FormatReport(report);
        WriteReportFile(text);

        if (report.HasErrors)
            Debug.LogError($"[MovieCatalog] Validation failed — {report.errors.Length} error(s).\n{text}");
        else if (report.HasWarnings)
            Debug.LogWarning($"[MovieCatalog] Validation passed with {report.warnings.Length} warning(s).\n{text}");
        else
            Debug.Log($"[MovieCatalog] Validation passed.\n{text}");

        AssetDatabase.SaveAssets();
    }

    [MenuItem("IdleFilm/Export Movie Catalog CSV")]
    public static void ExportCsv()
    {
        var db = MovieCatalogDatabase.LoadOrCreateAsset();
        if (db.entries == null || db.entries.Length == 0)
            db.RebuildFromProject(runAudit: false);

        Directory.CreateDirectory(Path.GetDirectoryName(CsvExportPath));
        File.WriteAllText(CsvExportPath, db.ToCsv(), System.Text.Encoding.UTF8);
        AssetDatabase.Refresh();

        Debug.Log($"[MovieCatalog] Exported {db.Count} rows to {CsvExportPath}");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(CsvExportPath));
    }

    [MenuItem("IdleFilm/Validate Movie Catalog", true)]
    [MenuItem("IdleFilm/Export Movie Catalog CSV", true)]
    static bool ValidateMenuEnabled() => !EditorApplication.isCompiling;

    static void LogAuditSummary(MovieCatalogAuditReport report)
    {
        if (report == null) return;
        Debug.Log($"[MovieCatalog] Audit: {report.totalEntries}/{report.targetCount} entries, " +
                  $"{report.errors?.Length ?? 0} errors, {report.warnings?.Length ?? 0} warnings.");
    }

    static void WriteReportFile(string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, text, System.Text.Encoding.UTF8);
        AssetDatabase.Refresh();
    }
}
#endif
