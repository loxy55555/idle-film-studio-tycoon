using System;
using UnityEngine;

/// <summary>
/// Studio level = psychological progression anchor.
/// Level advances as the player earns XP (from movies + contracts).
/// Each level unlocks new movies, upgrades, and contracts.
/// </summary>
public class StudioLevelSystem : MonoBehaviour
{
    public int   Level    { get; private set; } = 1;
    public float XP       { get; private set; } = 0f;
    public float XPToNext => GetXPRequired(Level);

    public event Action<int> OnLevelUp;

    private const float BASE_XP  = 120f;
    private const float EXPONENT = 1.6f;

    public static float GetXPRequired(int level) =>
        Mathf.Round(BASE_XP * Mathf.Pow(level, EXPONENT));

    public int AddXP(float amount)
    {
        XP += amount;
        int firstNewLevel = Level + 1;
        int levelUps = 0;

        // DATA PHASE — increment level and update contract tracking (no UI events yet).
        // If this loop were mixed with OnLevelUp invocations and a subscriber threw,
        // the remaining level-ups and contract notifications would be silently skipped.
        while (XP >= XPToNext)
        {
            XP -= XPToNext;
            Level++;
            levelUps++;
            Debug.Log($"[StudioLevel] Level up → {Level}");
            GameHub.Instance?.contracts?.OnStudioLevelUp(Level);
        }

        // UI EVENT PHASE — fire OnLevelUp for each new level, isolated from the data loop.
        for (int lvl = firstNewLevel; lvl <= Level; lvl++)
        {
            try { OnLevelUp?.Invoke(lvl); }
            catch (System.Exception ex)
            {
                Debug.LogError($"[StudioLevel] OnLevelUp UI error for level {lvl}: {ex}");
            }
        }

        return levelUps;
    }

    /// <summary>Base XP from movie config (without upgrade bonus).</summary>
    public static float BaseMovieXP(MovieConfig config) =>
        30f + config.quality * 20f;

    /// <summary>
    /// Final XP = base × (1 + XPBonus from UpgradeConfig).
    /// XPBonus valuePerLevel is a fractional multiplier (e.g. 0.10 = +10%).
    /// </summary>
    public static float CalculateMovieXP(MovieConfig config, UpgradeSystem upgrades)
    {
        float baseXp = BaseMovieXP(config);
        float mult   = 1f + (upgrades?.TotalEffect(UpgradeEffectType.XPBonus) ?? 0f);
        float final  = baseXp * mult;
        if (UpgradeSystem.LOG_EFFECTS && mult > 1f)
            Debug.Log($"[UpgradeFX:XP] base={baseXp:0.#} × {mult:0.###} → {final:0.#}");
        return final;
    }

    public bool IsMovieUnlocked(MovieConfig config) =>
        Level >= config.unlockStudioLevel;

    public bool IsUpgradeUnlocked(UpgradeConfig config) =>
        Level >= config.unlockStudioLevel;

    public void LoadFromSave(int savedLevel, float savedXP)
    {
        Level = Mathf.Max(1, savedLevel);
        XP    = Mathf.Max(0f, savedXP);
    }

    public void Init()
    {
        Level = 1;
        XP    = 0f;
    }
}
