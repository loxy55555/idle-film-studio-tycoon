#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Assigns city progression metadata, catalog distribution, and sample sagas.
/// Menu: IdleFilm / Assign City Progression Data
/// Menu: IdleFilm / Create Saga Database Assets
/// </summary>
public static class CityProgressionDataAssigner
{
    const string MoviePath    = "Assets/Data/Movies";
    const string ContractPath = "Assets/Data/Contracts";
    const string SagaPath     = "Assets/Data/Sagas";
    const string SagaResourcePath = "Assets/Resources/Sagas";

    [MenuItem("IdleFilm/Assign City Progression Data")]
    public static void AssignAll()
    {
        EnsureSagaAssets();
        int movies = AssignMovies();
        int contracts = AssignContracts();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CityProgression] Assigned {movies} movies, {contracts} contracts.");
    }

    [MenuItem("IdleFilm/Create Saga Database Assets")]
    public static void EnsureSagaAssets()
    {
        Directory.CreateDirectory(SagaResourcePath);

        CreateSagaAsset("shadow_trigger", "Shadow Trigger", SagaSizeClass.Medium, 4, 1,
            "Saga de acción original del estudio. Entregas escalonadas por Ciudad.");
        CreateSagaAsset("neon_drift", "Neon Drift", SagaSizeClass.Mini, 2, 2,
            "Mini saga neo-noir. Dos entregas.");
        CreateSagaAsset("last_signal", "La Última Señal", SagaSizeClass.Single, 1, 3,
            "Película única dentro del universo sci-fi del estudio.");
        CreateSagaAsset("iron_circuit", "Circuito de Hierro", SagaSizeClass.Large, 6, 4,
            "Saga grande de ciencia ficción. Seis entregas planificadas.");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void CreateSagaAsset(string id, string name, SagaSizeClass size, int count, int firstCity, string desc)
    {
        string path = $"{SagaResourcePath}/saga_{id}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<SagaDefinition>(path);
        if (existing != null)
        {
            existing.sagaId = id;
            existing.displayName = name;
            existing.sizeClass = size;
            existing.expectedEntryCount = count;
            existing.firstEntryCity = firstCity;
            existing.description = desc;
            EditorUtility.SetDirty(existing);
            return;
        }

        var def = ScriptableObject.CreateInstance<SagaDefinition>();
        def.sagaId = id;
        def.displayName = name;
        def.sizeClass = size;
        def.expectedEntryCount = count;
        def.firstEntryCity = firstCity;
        def.description = desc;
        AssetDatabase.CreateAsset(def, path);
    }

    static int AssignMovies()
    {
        var list = new List<MovieConfig>();
        foreach (var guid in AssetDatabase.FindAssets("t:MovieConfig", new[] { MoviePath }))
        {
            var cfg = AssetDatabase.LoadAssetAtPath<MovieConfig>(AssetDatabase.GUIDToAssetPath(guid));
            if (cfg != null) list.Add(cfg);
        }

        CityCatalogDistribution.ApplyDistribution(list);
        AssignSampleSagas(list);

        foreach (var cfg in list)
        {
            cfg.catalogTier = InferCatalogTier(cfg);
            if (string.IsNullOrEmpty(cfg.sagaId))
                cfg.contentKind = InferContentKind(cfg);
            else
                cfg.contentKind = MovieContentKind.Saga;

            EditorUtility.SetDirty(cfg);
        }

        LogMovieDistribution(list);
        return list.Count;
    }

    static void AssignSampleSagas(List<MovieConfig> movies)
    {
        if (movies == null || movies.Count == 0) return;

        var sorted = new List<MovieConfig>(movies);
        sorted.Sort((a, b) => CityCatalogDistribution.ComplexityScore(a)
            .CompareTo(CityCatalogDistribution.ComplexityScore(b)));

        ClearSagaFields(movies);

        AssignSagaEntries(sorted, "shadow_trigger", "Shadow Trigger",
            new[] { 8, 55, 110, 160 }, new[] { 1, 2, 4, 7 });

        AssignSagaEntries(sorted, "neon_drift", "Neon Drift",
            new[] { 20, 75 }, new[] { 2, 4 });

        AssignSagaEntries(sorted, "last_signal", "La Última Señal",
            new[] { 100 }, new[] { 3 });

        int[] ironIndices = { 130, 140, 150, 160, 165, 170 };
        int[] ironCities  = { 4, 5, 5, 6, 7, 8 };
        AssignSagaEntries(sorted, "iron_circuit", "Circuito de Hierro", ironIndices, ironCities);
    }

    static void ClearSagaFields(List<MovieConfig> movies)
    {
        foreach (var m in movies)
        {
            m.sagaId = "";
            m.sagaEntryIndex = 0;
            m.sagaDisplayName = "";
        }
    }

    static void AssignSagaEntries(
        List<MovieConfig> sorted,
        string sagaId,
        string displayName,
        int[] sortedIndices,
        int[] cityLevels)
    {
        for (int i = 0; i < sortedIndices.Length && i < cityLevels.Length; i++)
        {
            int idx = Mathf.Clamp(sortedIndices[i], 0, sorted.Count - 1);
            var cfg = sorted[idx];
            cfg.sagaId = sagaId;
            cfg.sagaDisplayName = displayName;
            cfg.sagaEntryIndex = i + 1;
            cfg.unlockCityLevel = cityLevels[i];
            cfg.contentKind = MovieContentKind.Saga;
        }
    }

    static void LogMovieDistribution(List<MovieConfig> movies)
    {
        var counts = CityCatalogDistribution.CountByCity(movies);
        var sb = new System.Text.StringBuilder("[CityProgression] Movie distribution:");
        for (int c = 1; c <= ContentScaleDatabase.MaxCityLevel; c++)
        {
            counts.TryGetValue(c, out int n);
            int target = ContentScaleDatabase.GetMovieTargetForCity(c);
            sb.Append($"\n  Ciudad {c}: {n} new (target cumulative {target})");
        }
        Debug.Log(sb.ToString());
    }

    static int AssignContracts()
    {
        int count = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:ContractConfig", new[] { ContractPath }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var cfg  = AssetDatabase.LoadAssetAtPath<ContractConfig>(path);
            if (cfg == null) continue;

            cfg.difficultyBand = InferContractBand(cfg);
            cfg.unlockCityLevel = CityProgressionRules.GetContractRequiredCity(cfg);
            EditorUtility.SetDirty(cfg);
            count++;
        }
        Debug.Log($"[CityProgression] Contracts assigned: {count}/{ContractCatalogRegistry.TargetRecommended} target.");
        return count;
    }

    static MovieCatalogTier InferCatalogTier(MovieConfig cfg)
    {
        return cfg.unlockCityLevel switch
        {
            <= 1 => MovieCatalogTier.Tier1,
            2    => MovieCatalogTier.Tier2,
            <= 4 => MovieCatalogTier.Tier3,
            5    => MovieCatalogTier.Tier4,
            _    => MovieCatalogTier.Epic,
        };
    }

    static MovieContentKind InferContentKind(MovieConfig cfg)
    {
        string name = cfg.movieName ?? "";
        if (name.Contains("Secuela") || name.Contains("Parte 2") || name.Contains(" II"))
            return MovieContentKind.Sequel;
        if (name.Contains("Remake"))
            return MovieContentKind.Remake;
        if (cfg.catalogTier == MovieCatalogTier.Epic)
            return MovieContentKind.Epic;
        return MovieContentKind.Standard;
    }

    static ContractDifficultyBand InferContractBand(ContractConfig cfg)
    {
        int city = CityProgressionRules.GetContractRequiredCity(cfg);
        return city switch
        {
            1 => ContractDifficultyBand.Simple,
            2 => ContractDifficultyBand.Medium,
            3 => ContractDifficultyBand.Epic,
            4 => ContractDifficultyBand.Advanced,
            _ => ContractDifficultyBand.Elite,
        };
    }
}
#endif
