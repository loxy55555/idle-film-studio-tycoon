#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Bakes approved legendary metadata into MovieConfig assets (Phase 9.0).</summary>
public static class CollectionLegendaryDataTools
{
    const string MoviesFolder = "Assets/Data/Movies";

    [MenuItem("IdleFilm/Integrate Collection Legendaries")]
    public static void IntegrateLegendaries()
    {
        Directory.CreateDirectory(MoviesFolder);
        int created = 0;
        int updated = 0;

        foreach (var def in CollectionLegendaryRegistry.All)
        {
            string path = $"{MoviesFolder}/{def.catalogId}.asset";
            var cfg = AssetDatabase.LoadAssetAtPath<MovieConfig>(path);

            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<MovieConfig>();
                cfg.cost = 1;
                cfg.baseReward = 1;
                cfg.baseRep = 0.1f;
                cfg.duration = 6f;
                cfg.quality = 1f;
                cfg.unlockStudioLevel = 1;
                cfg.unlockCityLevel = 1;
                cfg.unlockReputation = 0f;
                cfg.catalogTier = MovieCatalogTier.Tier1;
                cfg.contentKind = MovieContentKind.Standard;
                cfg.tagline = "[LEGENDARY]";
                AssetDatabase.CreateAsset(cfg, path);
                created++;
            }
            else
            {
                updated++;
            }

            cfg.movieName = def.displayName;
            cfg.genre = def.genre;
            cfg.rarity = MovieRarity.Legendary;
            cfg.posterColorHex = def.posterColorHex;
            if (string.IsNullOrWhiteSpace(cfg.tagline) || cfg.tagline == "[CATALOG SLOT]")
                cfg.tagline = "[LEGENDARY]";
            EditorUtility.SetDirty(cfg);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Collection] Legendaries integrated — created {created}, updated {updated}. Rebuild Studio UI to wire new assets.");
    }
}
#endif
