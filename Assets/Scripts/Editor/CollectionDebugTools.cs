#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Collection catalog visual review tools (Phase DEV TOOLS).</summary>
public static class CollectionDebugTools
{
    [MenuItem("IdleFilm/Collection Debug/Reveal Collection")]
    static void MenuRevealCollection()
    {
        if (!EnsurePlayMode()) return;
        RevealCollection();
    }

    [MenuItem("IdleFilm/Collection Debug/Hide Collection")]
    static void MenuHideCollection()
    {
        if (!EnsurePlayMode()) return;
        HideCollection();
    }

    [MenuItem("IdleFilm/Collection Debug/Unlock All Movies (DEV)")]
    static void MenuUnlockAllMovies()
    {
        if (!EnsurePlayMode()) return;
        UnlockAllMoviesDev();
    }

    [MenuItem("IdleFilm/Collection Debug/Reset Discovery Progress")]
    static void MenuResetDiscoveryProgress()
    {
        if (!EnsurePlayMode()) return;
        ResetDiscoveryProgress();
    }

    [MenuItem("IdleFilm/Collection Debug/Reveal Collection", true)]
    [MenuItem("IdleFilm/Collection Debug/Hide Collection", true)]
    [MenuItem("IdleFilm/Collection Debug/Unlock All Movies (DEV)", true)]
    [MenuItem("IdleFilm/Collection Debug/Reset Discovery Progress", true)]
    static bool MenuEnabled() => !EditorApplication.isCompiling;

    public static void RevealCollection()
    {
        CollectionDebugState.SetRevealAll(true);
        RefreshCollectionUi();
        Debug.Log("[CollectionDebug] Reveal Collection ON — visual preview only (save unchanged).");
    }

    public static void HideCollection()
    {
        CollectionDebugState.SetRevealAll(false);
        RefreshCollectionUi();
        Debug.Log("[CollectionDebug] Reveal Collection OFF — showing real discovery state.");
    }

    public static void UnlockAllMoviesDev()
    {
        var studio = GameHub.Instance?.studio;
        if (studio == null)
        {
            Debug.LogError("[CollectionDebug] StudioManager not available.");
            return;
        }

        var movies = ResolveAllMovies();
        if (movies == null || movies.Length == 0)
        {
            Debug.LogError("[CollectionDebug] No MovieConfig catalog found.");
            return;
        }

        var keys = new List<string>(movies.Length);
        foreach (var cfg in movies)
        {
            if (cfg == null || string.IsNullOrEmpty(cfg.name)) continue;
            keys.Add(cfg.name);
        }

        studio.LoadCompletedMovieKeys(keys.ToArray());
        CollectionDebugState.SetRevealAll(false);
        GameHub.Instance?.save?.Save("Debug");
        RefreshCollectionUi();

        Debug.Log($"[CollectionDebug] Unlock All Movies — {keys.Count} entries written to completedMovieKeys (saved).");
    }

    public static void ResetDiscoveryProgress()
    {
        var studio = GameHub.Instance?.studio;
        if (studio == null)
        {
            Debug.LogError("[CollectionDebug] StudioManager not available.");
            return;
        }

        studio.LoadCompletedMovieKeys(System.Array.Empty<string>());
        CollectionDebugState.SetRevealAll(false);
        GameHub.Instance?.save?.Save("Debug");
        RefreshCollectionUi();

        Debug.Log("[CollectionDebug] Discovery progress reset (completedMovieKeys cleared, saved).");
    }

    static void RefreshCollectionUi()
    {
        var ui = Object.FindAnyObjectByType<MovieCollectionUI>(FindObjectsInactive.Include);
        ui?.Refresh();
    }

    static MovieConfig[] ResolveAllMovies()
    {
        var collectionUi = Object.FindAnyObjectByType<MovieCollectionUI>(FindObjectsInactive.Include);
        var runtime = MovieCatalogRuntime.AllMovies;
        if (runtime != null && runtime.Length > 0)
            return runtime;

        if (collectionUi != null && collectionUi.allMovies != null && collectionUi.allMovies.Length > 0)
            return collectionUi.allMovies;

        var tab = Object.FindAnyObjectByType<MovieTabUI>(FindObjectsInactive.Include);
        if (tab != null && tab.allMovies != null && tab.allMovies.Length > 0)
            return tab.allMovies;

        var guids = AssetDatabase.FindAssets("t:MovieConfig", new[] { MovieCatalogDatabase.MoviesFolder });
        var list = new List<MovieConfig>(guids.Length);
        foreach (var guid in guids)
        {
            var cfg = AssetDatabase.LoadAssetAtPath<MovieConfig>(AssetDatabase.GUIDToAssetPath(guid));
            if (cfg != null) list.Add(cfg);
        }
        return list.ToArray();
    }

    static bool EnsurePlayMode()
    {
        if (Application.isPlaying) return true;
        Debug.LogWarning("[CollectionDebug] Enter Play Mode to use Collection Debug tools.");
        return false;
    }
}
#endif
