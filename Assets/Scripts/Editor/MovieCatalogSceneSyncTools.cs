#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Resync scene movie refs + runtime registry from Assets/Data/Movies (Phase 6.1).</summary>
public static class MovieCatalogSceneSyncTools
{
    public const string MenuPath = "IdleFilm/Catalog/Rebuild Scene Movie References";

    [MenuItem(MenuPath)]
    public static void RebuildSceneMovieReferences()
    {
        if (EditorApplication.isCompiling)
        {
            Debug.LogWarning("[MovieCatalog] Wait for compilation before rebuilding scene movie references.");
            return;
        }

        var movies = LoadAllMovieConfigs();
        if (movies.Length == 0)
        {
            Debug.LogError($"[MovieCatalog] No MovieConfig assets found under {MovieCatalogDatabase.MoviesFolder}.");
            return;
        }

        int registryCount = RebuildRuntimeRegistry(movies);
        int sceneCount = AssignSceneReferences(movies);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[MovieCatalog] Rebuild Scene Movie References complete — " +
            $"movies={movies.Length}, registry={registryCount}, sceneComponents={sceneCount}. " +
            $"Expected {ContentScaleDatabase.TargetMovieCount}.");

        if (movies.Length != ContentScaleDatabase.TargetMovieCount)
        {
            Debug.LogWarning(
                $"[MovieCatalog] Catalog count mismatch: expected {ContentScaleDatabase.TargetMovieCount}, found {movies.Length}.");
        }
    }

    [MenuItem(MenuPath, true)]
    static bool RebuildSceneMovieReferencesEnabled() => !EditorApplication.isCompiling;

    public static MovieConfig[] LoadAllMovieConfigs()
    {
        var guids = AssetDatabase.FindAssets("t:MovieConfig", new[] { MovieCatalogDatabase.MoviesFolder });
        var movies = new MovieConfig[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            movies[i] = AssetDatabase.LoadAssetAtPath<MovieConfig>(
                AssetDatabase.GUIDToAssetPath(guids[i]));
        }

        System.Array.Sort(movies, (a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            return string.Compare(a.name, b.name, System.StringComparison.Ordinal);
        });

        return movies;
    }

    static int RebuildRuntimeRegistry(MovieConfig[] movies)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(MovieCatalogRuntimeRegistry.AssetPath));

        var registry = AssetDatabase.LoadAssetAtPath<MovieCatalogRuntimeRegistry>(MovieCatalogRuntimeRegistry.AssetPath);
        if (registry == null)
        {
            registry = ScriptableObject.CreateInstance<MovieCatalogRuntimeRegistry>();
            AssetDatabase.CreateAsset(registry, MovieCatalogRuntimeRegistry.AssetPath);
        }

        registry.movies = movies;
        EditorUtility.SetDirty(registry);
        return movies.Length;
    }

    static int AssignSceneReferences(MovieConfig[] movies)
    {
        int count = 0;
        count += AssignMovieArray<MovieTabUI>(movies, "allMovies");
        count += AssignMovieArray<MovieCollectionUI>(movies, "allMovies");
        count += AssignMovieArray<MovieGridUI>(movies, "allMovies");

        if (count > 0)
            EditorSceneManager.SaveOpenScenes();

        return count;
    }

    static int AssignMovieArray<T>(MovieConfig[] movies, string propertyName) where T : Component
    {
        int n = 0;
        foreach (var component in Object.FindObjectsByType<T>(FindObjectsInactive.Include))
        {
            if (component == null) continue;

            var so = new SerializedObject(component);
            var prop = so.FindProperty(propertyName);
            if (prop == null || !prop.isArray)
                continue;

            prop.arraySize = movies.Length;
            for (int i = 0; i < movies.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = movies[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(component);
            n++;
        }

        return n;
    }
}
#endif
