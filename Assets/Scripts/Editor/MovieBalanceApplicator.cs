#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// FASE 15.3B — Applies real economic balance values to all MovieConfig assets.
///
/// Design rationale:
///   • Primary balance axis  : unlockCityLevel (1–8), already correctly set.
///   • Secondary axis        : rarity band within each city (Common/Rare/Epic/Legendary).
///   • Tertiary axis         : per-genre cost/rep multiplier.
///   • Reward/cost ratio     : 1.80× for all genres (proven in MovieDatabaseBuilder).
///   • catalogTier           : derived from city level.
///
/// Menu: IdleFilm / FASE 15.3B — Apply Real Movie Balance
/// </summary>
public static class MovieBalanceApplicator
{
    // ── City economic ranges (Action genre baseline, full Common→Legendary spread) ──
    // Derived by fitting MovieDatabaseBuilder.cs data for all 6 designed genres.
    readonly struct CityRange
    {
        public readonly int   city;
        public readonly long  costMin,  costMax;
        public readonly float repMin,   repMax;
        public readonly float qualMin,  qualMax;
        public readonly float durMin,   durMax;

        public CityRange(int city,
            long cMin, long cMax,
            float rMin, float rMax,
            float qMin, float qMax,
            float dMin, float dMax)
        {
            this.city    = city;
            costMin = cMin; costMax = cMax;
            repMin  = rMin; repMax  = rMax;
            qualMin = qMin; qualMax = qMax;
            durMin  = dMin; durMax  = dMax;
        }
    }

    static readonly CityRange[] Ranges =
    {
        //            city   costMin  costMax  repMin  repMax  qualMin qualMax  durMin  durMax
        new CityRange( 1,     45,     200,     0.80f,  2.50f,  1.00f,  1.20f,   5f,    15f  ),
        new CityRange( 2,    150,     800,     2.20f,  9.50f,  1.20f,  1.60f,  13f,    40f  ),
        new CityRange( 3,    500,    2200,     5.50f, 23.00f,  1.60f,  2.20f,  30f,    72f  ),
        new CityRange( 4,   1800,    7200,    17.00f, 62.00f,  2.20f,  2.80f,  60f,   145f  ),
        new CityRange( 5,   5500,   21000,    48.00f,160.00f,  2.80f,  3.40f, 120f,   320f  ),
        new CityRange( 6,  17000,   72000,   130.00f,470.00f,  3.40f,  4.00f, 275f,   690f  ),
        new CityRange( 7,  50000,  165000,   360.00f,980.00f,  4.00f,  4.50f, 590f,  1150f  ),
        new CityRange( 8, 110000,  280000,   720.00f,1500.0f,  4.50f,  5.00f, 950f,  1800f  ),
    };

    // ── Genre cost/rep multipliers (Action = 1.0 reference) ──────────────────
    static readonly Dictionary<MovieGenre, float> GenreMult = new Dictionary<MovieGenre, float>
    {
        { MovieGenre.Action,       1.00f },
        { MovieGenre.Drama,        1.04f },
        { MovieGenre.Horror,       0.95f },
        { MovieGenre.Comedy,       0.94f },
        { MovieGenre.Romance,      1.00f },
        { MovieGenre.SciFi,        1.06f },
        { MovieGenre.Fantasy,      1.05f },
        { MovieGenre.Thriller,     0.97f },
        { MovieGenre.Animation,    0.90f },
        { MovieGenre.Documentary,  0.72f },
    };

    // ── Rarity interpolation band (t in [0,1] within city range) ─────────────
    static (float tMin, float tMax) RarityBand(MovieRarity r)
    {
        switch (r)
        {
            case MovieRarity.Common:    return (0.00f, 0.42f);
            case MovieRarity.Rare:      return (0.28f, 0.68f);
            case MovieRarity.Epic:      return (0.57f, 0.87f);
            case MovieRarity.Legendary: return (0.84f, 1.00f);
            default:                    return (0.00f, 0.50f);
        }
    }

    // ── catalogTier mapping ───────────────────────────────────────────────────
    static MovieCatalogTier CatalogTierForCity(int city)
    {
        if (city <= 2) return MovieCatalogTier.Tier1;
        if (city <= 4) return MovieCatalogTier.Tier2;
        if (city <= 6) return MovieCatalogTier.Tier3;
        if (city == 7) return MovieCatalogTier.Tier4;
        return MovieCatalogTier.Epic;
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// Programmatic entry point (no dialog).
    public static string ApplyBalanceSilent()
    {
        return RunApply();
    }

    [MenuItem("IdleFilm/FASE 15.3B \u2014 Apply Real Movie Balance")]
    public static void ApplyBalance()
    {
        if (!EditorUtility.DisplayDialog(
                "FASE 15.3B — Apply Real Movie Balance",
                "This overwrites cost, baseReward, baseRep, duration, quality, and catalogTier " +
                "on ALL MovieConfig assets.\n\nunlockStudioLevel is left unchanged.\n\nContinue?",
                "Apply", "Cancel"))
            return;

        string result = RunApply();
        EditorUtility.DisplayDialog("FASE 15.3B Complete", result, "OK");
    }

    static string RunApply()
    {
        var guids = AssetDatabase.FindAssets("t:MovieConfig");

        var groups = new Dictionary<(int city, int genre, int rarity), List<MovieConfig>>();

        int missing = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var m    = AssetDatabase.LoadAssetAtPath<MovieConfig>(path);
            if (m == null) { missing++; continue; }

            var key = (Mathf.Clamp(m.unlockCityLevel, 1, 8), (int)m.genre, (int)m.rarity);
            if (!groups.ContainsKey(key))
                groups[key] = new List<MovieConfig>();
            groups[key].Add(m);
        }

        int total = 0, skipped = 0;
        foreach (var kvp in groups)
        {
            var (cityLvl, genreInt, rarityInt) = kvp.Key;
            var list   = kvp.Value.OrderBy(x => x.name).ToList();
            var range  = GetRange(cityLvl);
            if (range == null) { skipped += list.Count; continue; }

            var genre  = (MovieGenre) genreInt;
            var rarity = (MovieRarity)rarityInt;
            float gm   = GenreMult.TryGetValue(genre, out float v) ? v : 1.0f;
            var (tMin, tMax) = RarityBand(rarity);

            for (int i = 0; i < list.Count; i++)
            {
                float t = list.Count == 1
                    ? (tMin + tMax) * 0.5f
                    : Mathf.Lerp(tMin, tMax, (float)i / Mathf.Max(list.Count - 1, 1));

                ApplyValues(list[i], range.Value, t, gm, rarity, cityLvl);
                EditorUtility.SetDirty(list[i]);
                total++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string msg = $"Real economy values applied to {total} movie assets.\nSkipped: {skipped}  Load errors: {missing}";
        Debug.Log($"[MovieBalance] FASE 15.3B complete. {msg}");
        return msg;
    }

    // ── Core value applicator ─────────────────────────────────────────────────

    static void ApplyValues(MovieConfig m, CityRange r, float t, float gm, MovieRarity rarity, int city)
    {
        float rawCost = Mathf.Lerp(r.costMin, r.costMax, t) * gm;

        // Documentary has lower budget ceiling
        if (m.genre == MovieGenre.Documentary) rawCost *= 0.85f;

        m.cost       = (long)RoundSig(rawCost);
        m.baseReward = (long)RoundSig(rawCost * 1.80f);

        float rep = Mathf.Lerp(r.repMin, r.repMax, t) * gm;
        if (m.genre == MovieGenre.Documentary) rep *= 0.90f;
        // Round to 1 decimal for rep < 10, whole numbers above
        m.baseRep = rep < 10f
            ? Mathf.Round(rep * 10f) / 10f
            : Mathf.Round(rep);

        m.quality = Mathf.Round(Mathf.Lerp(r.qualMin, r.qualMax, t) * 100f) / 100f;

        float dur = Mathf.Lerp(r.durMin, r.durMax, t);
        if (m.genre == MovieGenre.Animation)   dur *= 0.90f;
        if (m.genre == MovieGenre.Documentary) dur *= 0.88f;
        m.duration = Mathf.Round(dur);

        m.catalogTier = CatalogTierForCity(city);

        // Legendary bonus on top of base values
        if (rarity == MovieRarity.Legendary)
        {
            m.cost       = (long)(m.cost       * 1.25f);
            m.baseReward = (long)(m.baseReward  * 1.35f);
            m.baseRep   *= 1.50f;
            m.baseRep    = m.baseRep < 10f
                ? Mathf.Round(m.baseRep * 10f) / 10f
                : Mathf.Round(m.baseRep);
            m.quality    = Mathf.Min(5.0f, m.quality + 0.40f);
            m.duration  *= 1.25f;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static CityRange? GetRange(int city)
    {
        foreach (var r in Ranges)
            if (r.city == city) return r;
        return null;
    }

    /// Rounds to 2-3 significant figures for cleaner in-game display.
    static float RoundSig(float value)
    {
        if (value <   100f) return Mathf.Round(value  /    5f) *    5f;
        if (value <  1000f) return Mathf.Round(value  /   10f) *   10f;
        if (value < 10000f) return Mathf.Round(value  /   50f) *   50f;
        if (value <100000f) return Mathf.Round(value  /  500f) *  500f;
        return                      Mathf.Round(value  / 5000f) * 5000f;
    }
}
#endif
