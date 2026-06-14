using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Scans Assets/Art/UI/Icons and builds Resources/UIIconRegistry.asset.</summary>
public static class UIIconRegistryBuilder
{
    const string RegistryPath = "Assets/Resources/UIIconRegistry.asset";

    static readonly string[] ScanRoots =
    {
        "Assets/Art/UI/Icons",
        "Assets/Art/UI/icons",
    };

    [MenuItem("Idle Film/UI/Rebuild Icon Registry")]
    public static void RebuildFromMenu()
    {
        Rebuild(log: true);
    }

    [InitializeOnLoadMethod]
    static void EnsureRegistryOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (AssetDatabase.LoadAssetAtPath<UIIconRegistry>(RegistryPath) != null) return;
            Rebuild(log: true);
        };
    }

    public static UIIconRegistry Rebuild(bool log = false)
    {
        var registry = AssetDatabase.LoadAssetAtPath<UIIconRegistry>(RegistryPath);
        if (registry == null)
        {
            registry = ScriptableObject.CreateInstance<UIIconRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryPath);
        }

        var best = new Dictionary<string, (Sprite sprite, int score)>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in ScanRoots)
        {
            if (!AssetDatabase.IsValidFolder(root)) continue;

            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) continue;

                var key = MapPathToKey(path);
                if (string.IsNullOrEmpty(key)) continue;

                int score = ScorePath(path);
                if (!best.TryGetValue(key, out var current) || score > current.score)
                    best[key] = (sprite, score);
            }
        }

        ApplyMapped(registry, best);

        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (log)
            Debug.Log($"[UIIconRegistry] Rebuilt with {best.Count} mapped sprites at {RegistryPath}");

        return registry;
    }

    static int ScorePath(string path)
    {
        int score = 0;
        if (path.Contains("/Icons/")) score += 2;
        if (!Path.GetFileName(path).StartsWith("icon_", StringComparison.OrdinalIgnoreCase)) score += 1;
        return score;
    }

    static string MapPathToKey(string assetPath)
    {
        var file = NormalizeToken(Path.GetFileNameWithoutExtension(assetPath));
        var folder = NormalizeToken(Path.GetFileName(Path.GetDirectoryName(assetPath)));

        if (folder == "departments")
        {
            return file switch
            {
                "edicion" => nameof(UIIconRegistry.deptEditor),
                "director" => nameof(UIIconRegistry.deptDirector),
                "actores" => nameof(UIIconRegistry.deptActors),
                "sonido" => nameof(UIIconRegistry.deptSound),
                "fotografia" => nameof(UIIconRegistry.deptCinematography),
                "maquillaje" => nameof(UIIconRegistry.deptMakeup),
                "vestuario" => nameof(UIIconRegistry.deptCostume),
                "arte" => nameof(UIIconRegistry.deptArt),
                "iluminacion" => nameof(UIIconRegistry.deptLighting),
                "grip" => nameof(UIIconRegistry.deptGrip),
                "productor" => nameof(UIIconRegistry.deptProducer),
                "marketing" => nameof(UIIconRegistry.deptMarketing),
                "guion" => nameof(UIIconRegistry.deptScript),
                _ => null,
            };
        }

        if (folder == "genres")
        {
            return file switch
            {
                "accion" => nameof(UIIconRegistry.genreAction),
                "drama" => nameof(UIIconRegistry.genreDrama),
                "terror" => nameof(UIIconRegistry.genreHorror),
                "comedia" => nameof(UIIconRegistry.genreComedy),
                "romance" => nameof(UIIconRegistry.genreRomance),
                "scifi" => nameof(UIIconRegistry.genreSciFi),
                "fantasia" => nameof(UIIconRegistry.genreFantasy),
                "thriller" => nameof(UIIconRegistry.genreThriller),
                "animacion" => nameof(UIIconRegistry.genreAnimation),
                "documental" => nameof(UIIconRegistry.genreDocumentary),
                _ => null,
            };
        }

        if (folder == "navigation")
        {
            return file switch
            {
                "estudio" => nameof(UIIconRegistry.navStudio),
                "produccion" => nameof(UIIconRegistry.navProduction),
                "premios" => nameof(UIIconRegistry.navAwards),
                "coleccion" => nameof(UIIconRegistry.navCollection),
                "tienda" => nameof(UIIconRegistry.navShop),
                "misiones" => nameof(UIIconRegistry.navMissions),
                _ => null,
            };
        }

        if (folder == "resources")
        {
            return file switch
            {
                "dinero" => nameof(UIIconRegistry.resMoney),
                "reputacion" => nameof(UIIconRegistry.resReputation),
                "estrelladeoro" => nameof(UIIconRegistry.resGoldStar),
                "ticket" => nameof(UIIconRegistry.resTicket),
                "experiencia" => nameof(UIIconRegistry.resExperience),
                "camaradeco" => nameof(UIIconRegistry.resDecoCamera),
                "carrete" => nameof(UIIconRegistry.resFilmReel),
                "diamantes" => nameof(UIIconRegistry.resDiamonds),
                _ => null,
            };
        }

        if (folder == "awards")
        {
            return file switch
            {
                "premios" or "premio" => nameof(UIIconRegistry.awardStar),
                "premiocerrado" => nameof(UIIconRegistry.awardStarLocked),
                _ => null,
            };
        }

        if (folder == "utility")
        {
            return file switch
            {
                "boost" => nameof(UIIconRegistry.utilBoost),
                "acelerarproduccion" => nameof(UIIconRegistry.utilSpeedProduction),
                _ => null,
            };
        }

        return null;
    }

    static void ApplyMapped(UIIconRegistry registry, Dictionary<string, (Sprite sprite, int score)> best)
    {
        var fields = typeof(UIIconRegistry).GetFields(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

        foreach (var field in fields)
        {
            if (field.FieldType != typeof(Sprite)) continue;
            field.SetValue(registry, best.TryGetValue(field.Name, out var entry) ? entry.sprite : null);
        }
    }

    static string NormalizeToken(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        var sb = new StringBuilder(value.Length);
        var formD = value.Normalize(NormalizationForm.FormD);
        foreach (var ch in formD)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
        }

        var normalized = sb.ToString();
        if (normalized.StartsWith("icon", StringComparison.Ordinal))
            normalized = normalized.Substring(4);
        return normalized;
    }
}
