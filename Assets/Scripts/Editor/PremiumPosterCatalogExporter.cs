#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// TEMP — Export premium poster folders to review CSV (Phase CATÁLOGO PREMIUM FINAL).
/// Does not modify MovieCatalog, rarities, or city unlocks.
/// </summary>
public static class PremiumPosterCatalogExporter
{
    public const string PremiumPostersRoot = "Assets/Posters";
    public const string ExportPath = "Assets/Data/Exports/PremiumPosterCatalog.csv";

    static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".webp" };

    static readonly Dictionary<string, MovieGenre> FolderToGenre = new(System.StringComparer.OrdinalIgnoreCase)
    {
        ["accion"] = MovieGenre.Action,
        ["action"] = MovieGenre.Action,
        ["scifi"] = MovieGenre.SciFi,
        ["cienciaficcion"] = MovieGenre.SciFi,
        ["drama"] = MovieGenre.Drama,
        ["horror"] = MovieGenre.Horror,
        ["terror"] = MovieGenre.Horror,
        ["comedy"] = MovieGenre.Comedy,
        ["comedia"] = MovieGenre.Comedy,
        ["fantasy"] = MovieGenre.Fantasy,
        ["fantasia"] = MovieGenre.Fantasy,
        ["romance"] = MovieGenre.Romance,
        ["thriller"] = MovieGenre.Thriller,
        ["animation"] = MovieGenre.Animation,
        ["animacion"] = MovieGenre.Animation,
        ["documentary"] = MovieGenre.Documentary,
        ["documental"] = MovieGenre.Documentary,
    };

    public struct PosterEntry
    {
        public MovieGenre genre;
        public string movieName;
        public string posterFile;
        public string folderName;
    }

    [MenuItem("IdleFilm/Catalog/Export Premium Poster Catalog (TEMP)")]
    public static void ExportFromMenu()
    {
        if (EditorApplication.isCompiling)
        {
            Debug.LogWarning("[PremiumPosterCatalog] Wait for compilation before exporting.");
            return;
        }

        var result = Export();
        AssetDatabase.Refresh();
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(ExportPath));
        Debug.Log(result.summary);
    }

    [MenuItem("IdleFilm/Catalog/Export Premium Poster Catalog (TEMP)", true)]
    static bool ExportFromMenuEnabled() => !EditorApplication.isCompiling;

    public struct ExportResult
    {
        public PosterEntry[] entries;
        public string summary;
        public int total;
    }

    public static ExportResult Export(string postersRoot = null, string exportPath = null)
    {
        postersRoot ??= PremiumPostersRoot;
        exportPath ??= ExportPath;

        if (!AssetDatabase.IsValidFolder(postersRoot))
        {
            string msg = $"[PremiumPosterCatalog] Posters root not found: {postersRoot}";
            Debug.LogError(msg);
            return new ExportResult { summary = msg };
        }

        var entries = ScanPosters(postersRoot);
        WriteCsv(exportPath, entries);
        string summary = BuildSummary(entries);
        return new ExportResult
        {
            entries = entries,
            summary = summary,
            total = entries.Length,
        };
    }

    public static PosterEntry[] ScanPosters(string postersRoot)
    {
        var list = new List<PosterEntry>();
        string absoluteRoot = Path.GetFullPath(postersRoot);

        foreach (string dir in Directory.GetDirectories(absoluteRoot, "*", SearchOption.AllDirectories))
        {
            string folderName = Path.GetFileName(dir);
            if (!FolderToGenre.TryGetValue(folderName, out MovieGenre genre))
                continue;

            foreach (string file in Directory.GetFiles(dir))
            {
                string ext = Path.GetExtension(file);
                if (System.Array.IndexOf(ImageExtensions, ext.ToLowerInvariant()) < 0)
                    continue;

                string stem = Path.GetFileNameWithoutExtension(file);
                list.Add(new PosterEntry
                {
                    genre = genre,
                    movieName = PremiumPosterTitleDecoder.ToMovieName(stem),
                    posterFile = Path.GetFileName(file),
                    folderName = folderName,
                });
            }
        }

        return list
            .OrderBy(e => GenreSortIndex(e.genre))
            .ThenBy(e => e.movieName, System.StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    static int GenreSortIndex(MovieGenre genre)
    {
        int idx = System.Array.IndexOf(MovieCatalogSchema.DefinitiveGenreOrder, genre);
        return idx >= 0 ? idx : int.MaxValue;
    }

    static void WriteCsv(string exportPath, PosterEntry[] entries)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(exportPath));
        var sb = new StringBuilder();
        sb.AppendLine("genre,movieName");

        foreach (var entry in entries)
            sb.AppendLine($"{Csv(MovieCatalogSchema.GenreAssetPrefix(entry.genre))},{Csv(entry.movieName)}");

        File.WriteAllText(exportPath, sb.ToString(), Encoding.UTF8);
    }

    static string BuildSummary(PosterEntry[] entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[PremiumPosterCatalog] Export complete");
        sb.AppendLine($"  Output: {ExportPath}");
        sb.AppendLine($"  Posters root: {PremiumPostersRoot}");
        sb.AppendLine($"  Total: {entries.Length}");
        sb.AppendLine("  By genre:");

        foreach (var genre in MovieCatalogSchema.DefinitiveGenreOrder)
        {
            int count = entries.Count(e => e.genre == genre);
            if (count > 0)
                sb.AppendLine($"    {MovieCatalogSchema.GenreAssetPrefix(genre)}: {count}");
        }

        var unknownFolders = entries
            .GroupBy(e => e.folderName, System.StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Key)
            .Where(f => !FolderToGenre.ContainsKey(f))
            .ToArray();

        if (unknownFolders.Length > 0)
            sb.AppendLine($"  Unknown folders skipped: {string.Join(", ", unknownFolders)}");

        return sb.ToString();
    }

    static string Csv(string value)
    {
        if (value == null) return string.Empty;
        if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
            return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
#endif
