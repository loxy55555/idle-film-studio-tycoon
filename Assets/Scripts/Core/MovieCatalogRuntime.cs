using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dynamic movie catalog loaded at startup — no dependency on scene-serialized allMovies (Phase 6.1).
/// </summary>
public static class MovieCatalogRuntime
{
    static MovieConfig[] _movies = Array.Empty<MovieConfig>();
    static bool _loaded;

    public static int LoadedCount
    {
        get
        {
            EnsureLoaded();
            return _movies.Length;
        }
    }

    public static MovieConfig[] AllMovies
    {
        get
        {
            EnsureLoaded();
            return _movies;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap() => EnsureLoaded();

    public static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

#if UNITY_EDITOR
        _movies = LoadFromProjectFolder();
        if (_movies.Length == 0)
            _movies = LoadFromResourcesRegistry();
#else
        _movies = LoadFromResourcesRegistry();
#endif

        if (_movies == null)
            _movies = Array.Empty<MovieConfig>();

        Debug.Log($"Movies Loaded: {_movies.Length}");

        if (_movies.Length != ContentScaleDatabase.TargetMovieCount)
        {
            Debug.LogWarning(
                $"[MovieCatalogRuntime] Expected {ContentScaleDatabase.TargetMovieCount} movies, got {_movies.Length}. " +
                "Run IdleFilm → Catalog → Rebuild Scene Movie References.");
        }
    }

    /// <summary>Prefer runtime catalog; optional serialized array is legacy fallback only.</summary>
    public static MovieConfig[] Resolve(MovieConfig[] serializedFallback)
    {
        EnsureLoaded();
        if (_movies.Length > 0) return _movies;
        return FilterValid(serializedFallback);
    }

    public static MovieConfig FindByCatalogId(string catalogId)
    {
        if (string.IsNullOrEmpty(catalogId)) return null;
        EnsureLoaded();
        for (int i = 0; i < _movies.Length; i++)
        {
            var cfg = _movies[i];
            if (cfg != null && cfg.name == catalogId)
                return cfg;
        }
        return null;
    }

    static MovieConfig[] LoadFromResourcesRegistry()
    {
        var registry = Resources.Load<MovieCatalogRuntimeRegistry>(MovieCatalogRuntimeRegistry.ResourceName);
        if (registry == null || registry.movies == null || registry.movies.Length == 0)
            return Array.Empty<MovieConfig>();

        return SortAndFilter(registry.movies);
    }

#if UNITY_EDITOR
    static MovieConfig[] LoadFromProjectFolder()
    {
        var guids = UnityEditor.AssetDatabase.FindAssets("t:MovieConfig", new[] { MovieCatalogDatabase.MoviesFolder });
        if (guids == null || guids.Length == 0)
            return Array.Empty<MovieConfig>();

        var list = new List<MovieConfig>(guids.Length);
        foreach (var guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<MovieConfig>(path);
            if (cfg != null)
                list.Add(cfg);
        }

        return SortAndFilter(list.ToArray());
    }
#endif

    static MovieConfig[] SortAndFilter(MovieConfig[] source)
    {
        if (source == null || source.Length == 0)
            return Array.Empty<MovieConfig>();

        var list = new List<MovieConfig>(source.Length);
        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] != null)
                list.Add(source[i]);
        }

        list.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
        return list.ToArray();
    }

    static MovieConfig[] FilterValid(MovieConfig[] source)
    {
        if (source == null || source.Length == 0)
            return Array.Empty<MovieConfig>();

        var list = new List<MovieConfig>(source.Length);
        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] != null)
                list.Add(source[i]);
        }

        return list.Count > 0 ? list.ToArray() : Array.Empty<MovieConfig>();
    }

#if UNITY_EDITOR
    /// <summary>Editor/tests — force reload on next access.</summary>
    public static void ResetForTests()
    {
        _loaded = false;
        _movies = Array.Empty<MovieConfig>();
    }
#endif
}
