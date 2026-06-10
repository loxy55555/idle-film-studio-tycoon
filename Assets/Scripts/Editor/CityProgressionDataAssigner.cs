#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Assigns city progression metadata on existing MovieConfig and ContractConfig assets.
/// Menu: IdleFilm / Assign City Progression Data
/// </summary>
public static class CityProgressionDataAssigner
{
    const string MoviePath    = "Assets/Data/Movies";
    const string ContractPath = "Assets/Data/Contracts";

    [MenuItem("IdleFilm/Assign City Progression Data")]
    public static void AssignAll()
    {
        int movies = AssignMovies();
        int contracts = AssignContracts();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CityProgression] Assigned {movies} movies, {contracts} contracts.");
    }

    static int AssignMovies()
    {
        int count = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:MovieConfig", new[] { MoviePath }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var cfg  = AssetDatabase.LoadAssetAtPath<MovieConfig>(path);
            if (cfg == null) continue;

            cfg.catalogTier    = InferCatalogTier(cfg);
            cfg.contentKind    = InferContentKind(cfg);
            cfg.unlockCityLevel = CityProgressionRules.GetMovieRequiredCity(cfg);
            EditorUtility.SetDirty(cfg);
            count++;
        }
        return count;
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
        return count;
    }

    static MovieCatalogTier InferCatalogTier(MovieConfig cfg)
    {
        if (IsEpicMovie(cfg)) return MovieCatalogTier.Epic;
        if (cfg.unlockStudioLevel <= 3)  return MovieCatalogTier.Tier1;
        if (cfg.unlockStudioLevel <= 7)  return MovieCatalogTier.Tier2;
        if (cfg.unlockStudioLevel <= 14) return MovieCatalogTier.Tier3;
        return MovieCatalogTier.Tier4;
    }

    static MovieContentKind InferContentKind(MovieConfig cfg)
    {
        if (!string.IsNullOrEmpty(cfg.sagaId))
            return MovieContentKind.Saga;

        string name = cfg.movieName ?? "";
        if (name.Contains("Secuela") || name.Contains("Parte 2") || name.Contains(" II"))
            return MovieContentKind.Sequel;
        if (name.Contains("Remake"))
            return MovieContentKind.Remake;
        if (IsEpicMovie(cfg))
            return MovieContentKind.Epic;
        return MovieContentKind.Standard;
    }

    static bool IsEpicMovie(MovieConfig cfg) =>
        cfg.quality >= 3f && cfg.unlockStudioLevel >= 11;

    static ContractDifficultyBand InferContractBand(ContractConfig cfg)
    {
        if (!string.IsNullOrEmpty(cfg.id))
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

        if (cfg.unlockStudioLevel <= 3)  return ContractDifficultyBand.Simple;
        if (cfg.unlockStudioLevel <= 7)  return ContractDifficultyBand.Medium;
        if (cfg.unlockStudioLevel <= 10) return ContractDifficultyBand.Epic;
        if (cfg.unlockStudioLevel <= 14) return ContractDifficultyBand.Advanced;
        return ContractDifficultyBand.Elite;
    }
}
#endif
