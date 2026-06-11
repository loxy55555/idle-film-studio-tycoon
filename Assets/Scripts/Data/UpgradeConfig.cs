using System;
using UnityEngine;

public enum UpgradeCategory
{
    Equipment = 0,
    Personnel = 1,
    Installation = 2,
    Marketing = 3,
    ContentFeature = 4,
}

public enum UpgradeEffectType
{
    Quality,            // adds to studio quality multiplier
    Speed,              // adds to production speed
    CostReduction,      // reduces movie production cost (max 50%)
    PassiveIncomeBonus, // flat $/s bonus
    ReputationBonus,    // multiplies reputation gained per movie
    MaxMovieSlots,      // +1 production slot per level
    UnlockMovieTier,    // unlocks next set of movies (value = studio level unlocked)
    XPBonus,            // bonus studio XP per movie completed
}

[Serializable]
public class UpgradeEffect
{
    public UpgradeEffectType type;
    public float valuePerLevel;
}

/// <summary>
/// Single ScriptableObject describing any upgrade (equipment, personnel, installation, marketing).
/// The UpgradeSystem tracks levels; this config defines costs and effects.
/// </summary>
[CreateAssetMenu(menuName = "IdleFilm/Upgrade")]
public class UpgradeConfig : ScriptableObject
{
    [Header("Identity")]
    public string id;               // unique key used in save data
    public string displayName;
    [TextArea(1, 3)]
    public string description;
    public UpgradeCategory category;

    [Header("Progression")]
    public int  maxLevel     = 25;
    public long baseCost     = 100;
    [Tooltip("Cost = baseCost × costGrowthRate ^ currentLevel")]
    public float costGrowthRate = 1.45f;

    [Header("Unlock")]
    [Tooltip("Studio level required before this upgrade appears.")]
    public int unlockStudioLevel = 0;

    [Tooltip("Minimum city level required. 1 = Garaje.")]
    public int unlockCityLevel = 1;

    [Header("Effects")]
    public UpgradeEffect[] effects;

    [Header("Visual (keys for future asset replacement)")]
    public string iconKey       = "";
    public string badgeColorHex = "#3498DB";

    // ─── Runtime helpers ─────────────────────────────────────────────────────
    public long CostAtLevel(int level) =>
        (long)(baseCost * Math.Pow(costGrowthRate, level));

    public float GetTotalEffect(UpgradeEffectType type, int level)
    {
        float total = 0f;
        foreach (var e in effects)
            if (e.type == type) total += e.valuePerLevel * level;
        return total;
    }
}
