#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// FASE 15.4A — Applies approved rarity-based durations to all MovieConfig assets.
///
/// Design spec (approved):
///   Common    →  60 s  (1 min)
///   Rare      → 300 s  (5 min)
///   Epic      → 2700 s (45 min)
///   Legendary → 21600 s (6 h)
///
/// City level does NOT affect duration. Only rarity matters.
/// All other economic values (cost, baseReward, baseRep, quality) remain unchanged.
///
/// Menu: IdleFilm / FASE 15.4A — Apply Rarity-Based Durations
/// </summary>
public static class MovieDurationApplicator
{
    static float DurationForRarity(MovieRarity rarity)
    {
        switch (rarity)
        {
            case MovieRarity.Common:    return 60f;
            case MovieRarity.Rare:      return 300f;
            case MovieRarity.Epic:      return 2700f;
            case MovieRarity.Legendary: return 21600f;
            default:                    return 60f;
        }
    }

    /// <summary>Programmatic entry point — no dialog.</summary>
    public static string ApplyDurationsSilent()
    {
        return RunApply();
    }

    [MenuItem("IdleFilm/FASE 15.4A \u2014 Apply Rarity-Based Durations")]
    public static void ApplyDurations()
    {
        if (!EditorUtility.DisplayDialog(
                "FASE 15.4A — Apply Rarity-Based Durations",
                "This will set duration on ALL MovieConfig assets based solely on rarity:\n" +
                "  Common    →  60 s\n" +
                "  Rare      → 300 s\n" +
                "  Epic      → 2700 s\n" +
                "  Legendary → 21600 s\n\n" +
                "All other economic values (cost, reward, rep, quality) are unchanged.\nContinue?",
                "Apply", "Cancel"))
            return;

        string result = RunApply();
        EditorUtility.DisplayDialog("FASE 15.4A Complete", result, "OK");
    }

    static string RunApply()
    {
        var guids = AssetDatabase.FindAssets("t:MovieConfig");
        int total = 0, errors = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var m    = AssetDatabase.LoadAssetAtPath<MovieConfig>(path);
            if (m == null) { errors++; continue; }

            m.duration = DurationForRarity(m.rarity);
            EditorUtility.SetDirty(m);
            total++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string msg = $"Rarity-based durations applied to {total} MovieConfig assets. Load errors: {errors}";
        Debug.Log($"[DurationApplicator] FASE 15.4A complete. {msg}");
        return msg;
    }
}
#endif
