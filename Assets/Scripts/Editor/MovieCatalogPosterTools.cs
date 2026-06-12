#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Editor menu — bulk poster assignment (Phase CATÁLOGO 3.1).</summary>
public static class MovieCatalogPosterTools
{
    const string ReportPath = "Assets/Data/Reports/MovieCatalogPosterAssign.txt";

    [MenuItem("IdleFilm/Catalog/Assign Posters")]
    public static void AssignPosters()
    {
        if (EditorApplication.isCompiling)
        {
            Debug.LogWarning("[MovieCatalog] Wait for compilation before assigning posters.");
            return;
        }

        string postersFolder = MovieCatalogDatabase.PostersFolder;
        var report = MovieCatalogPosterAssigner.AssignAll(postersFolder);
        string text = MovieCatalogPosterAssigner.FormatReport(report, postersFolder);

        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, text, System.Text.Encoding.UTF8);
        AssetDatabase.Refresh();

        if (report.missing.Length > 0 || report.duplicateSprites.Length > 0)
        {
            Debug.LogWarning(
                $"[MovieCatalog] Poster assignment finished — assigned={report.assigned.Length}, " +
                $"missing={report.missing.Length}, duplicateSprites={report.duplicateSprites.Length}.\n{text}");
        }
        else
        {
            Debug.Log(
                $"[MovieCatalog] Poster assignment finished — assigned={report.assigned.Length}.\n{text}");
        }

        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(ReportPath));
    }

    [MenuItem("IdleFilm/Catalog/Assign Posters", true)]
    static bool AssignPostersEnabled() => !EditorApplication.isCompiling;
}
#endif
