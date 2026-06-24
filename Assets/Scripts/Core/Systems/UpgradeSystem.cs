using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all upgrades (equipment, personnel, installations, marketing).
/// Levels are stored in a dictionary keyed by UpgradeConfig.id.
/// Personnel upgrades are mirrored into DepartmentSystem for formula compatibility.
/// </summary>
public class UpgradeSystem : MonoBehaviour
{
    /// <summary>Temporary debug flag — set false when balancing is done.</summary>
    public const bool LOG_EFFECTS = false;

    [Header("Upgrade Database (assign all UpgradeConfig assets here)")]
    public UpgradeConfig[] allUpgrades;

    public event Action OnUpgradePurchased;

    private readonly Dictionary<string, int> _levels = new();

    // ─── Accessors ────────────────────────────────────────────────────────────

    public int GetLevel(string upgradeId) =>
        _levels.TryGetValue(upgradeId, out var v) ? v : 0;

    public int GetLevel(UpgradeConfig cfg) => GetLevel(cfg.id);

    public bool IsMaxLevel(UpgradeConfig cfg) => GetLevel(cfg) >= cfg.maxLevel;

    public long GetNextCost(UpgradeConfig cfg) =>
        cfg.CostAtLevel(GetLevel(cfg));

    // ─── Aggregate effect getters ───────────────────────────────────────────

    /// <summary>Sum of valuePerLevel × level for all upgrades matching the effect type.</summary>
    public float TotalEffect(UpgradeEffectType type) =>
        TotalEffect(type, includePersonnel: true);

    /// <summary>
    /// Personnel stats are synced into DepartmentSystem — exclude them when stacking
    /// on top of department formulas to avoid double-counting Quality/Speed/CostReduction.
    /// </summary>
    public float TotalEffectNonPersonnel(UpgradeEffectType type) =>
        TotalEffect(type, includePersonnel: false);

    public float TotalEffect(UpgradeEffectType type, bool includePersonnel)
    {
        float total = 0f;
        if (allUpgrades == null) return total;
        foreach (var cfg in allUpgrades)
        {
            if (cfg == null) continue;
            if (!includePersonnel && cfg.category == UpgradeCategory.Personnel) continue;
            int lvl = GetLevel(cfg);
            if (lvl <= 0) continue;
            total += cfg.GetTotalEffect(type, lvl);
        }
        return total;
    }

    public void LogEffects(string context)
    {
        if (!LOG_EFFECTS) return;
        Debug.Log($"[UpgradeFX:{context}] " +
                  $"Q+{TotalEffectNonPersonnel(UpgradeEffectType.Quality):0.###} " +
                  $"Vel+{TotalEffectNonPersonnel(UpgradeEffectType.Speed):0.###} " +
                  $"Cost-{TotalEffectNonPersonnel(UpgradeEffectType.CostReduction) * 100f:0.#}% " +
                  $"Rep×{1f + TotalEffect(UpgradeEffectType.ReputationBonus):0.###} " +
                  $"Inc+{TotalEffect(UpgradeEffectType.PassiveIncomeBonus):0}/s " +
                  $"XP×{1f + TotalEffect(UpgradeEffectType.XPBonus):0.###} " +
                  $"Slots+{Mathf.FloorToInt(TotalEffect(UpgradeEffectType.MaxMovieSlots))}");
    }

    /// <summary>Max production slots = 1 base + slots from installations.</summary>
    public int GetMaxMovieSlots() => 1 + Mathf.FloorToInt(TotalEffect(UpgradeEffectType.MaxMovieSlots));

    // ─── Purchase ─────────────────────────────────────────────────────────────

    public bool CanPurchase(UpgradeConfig cfg, long availableMoney, int currentStudioLevel)
    {
        if (cfg == null) return false;
        if (IsMaxLevel(cfg)) return false;
        if (currentStudioLevel < cfg.unlockStudioLevel) return false;
        if (GameHub.Instance?.city != null && !GameHub.Instance.city.IsUpgradeUnlocked(cfg)) return false;
        return availableMoney >= GetNextCost(cfg);
    }

    /// <summary>Phase 12.4H — first failing CanPurchase check (diagnostic).</summary>
    public string GetPurchaseBlockReason(UpgradeConfig cfg, long availableMoney, int currentStudioLevel)
    {
        if (cfg == null) return "cfg_null";
        if (IsMaxLevel(cfg)) return "max_level";
        if (currentStudioLevel < cfg.unlockStudioLevel)
            return $"studio_locked need={cfg.unlockStudioLevel} have={currentStudioLevel}";
        if (GameHub.Instance?.city != null && !GameHub.Instance.city.IsUpgradeUnlocked(cfg))
            return "city_locked";
        long cost = GetNextCost(cfg);
        if (availableMoney < cost)
            return $"insufficient_money money={availableMoney} cost={cost}";
        return string.Empty;
    }

    public bool Purchase(UpgradeConfig cfg, StudioManager studio, DepartmentSystem departments, int studioLevel)
    {
        if (cfg == null)
        {
            Debug.Log("[Upgrade] Purchase()=false motivo=cfg_null");
            return false;
        }

        long cost = GetNextCost(cfg);
        long money = studio != null ? studio.Money : 0;
        double moneyExact = studio != null ? studio.MoneyExact : 0;
        bool canPurchase = CanPurchase(cfg, money, studioLevel);
        string blockReason = GetPurchaseBlockReason(cfg, money, studioLevel);

        Debug.Log($"[Upgrade] Purchase() id={cfg.id} money={money} moneyExact={moneyExact:F2} cost={cost} CanPurchase={canPurchase} blockReason={blockReason}");

        if (!canPurchase)
        {
            Debug.Log($"[Upgrade] Purchase()=false motivo=CanPurchase ({blockReason})");
            return false;
        }

        if (!studio.TrySpendMoney(cost))
        {
            Debug.Log($"[Upgrade] Purchase()=false motivo=TrySpendMoney moneyExact={moneyExact:F2} cost={cost}");
            return false;
        }

        _levels[cfg.id] = GetLevel(cfg) + 1;

        if (cfg.category == UpgradeCategory.Personnel)
            SyncPersonnel(cfg, departments);

        studio.RecalculateIncome();
        studio.SyncMaxMovieSlotsFromUpgrades();
        LogEffects($"Purchase:{cfg.id}");

        GameHub.Instance?.contracts?.OnMoneySpentOnUpgrade(cost);

        // ── SAVE before UI event ────────────────────────────────────────────────
        GameHub.Instance?.save?.Save("PurchaseUpgrade");

        try { OnUpgradePurchased?.Invoke(); }
        catch (System.Exception ex) { Debug.LogError($"[Upgrade] UI event error post-purchase (save ya guardado): {ex}"); }

        Debug.Log($"[Upgrade] Purchased UpgradeId={cfg.id} Level={GetLevel(cfg)}");
        return true;
    }

    // ─── Personnel sync ───────────────────────────────────────────────────────

    private void SyncPersonnel(UpgradeConfig cfg, DepartmentSystem d)
    {
        int lvl = GetLevel(cfg);
        switch (cfg.id)
        {
            case "personal_editor":         d.editor         = lvl; break;
            case "personal_director":       d.director       = lvl; break;
            case "personal_actors":         d.actors         = lvl; break;
            case "personal_sound":          d.sound          = lvl; break;
            case "personal_cinematography": d.cinematography = lvl; break;
            case "personal_makeup":         d.makeup         = lvl; break;
            case "personal_costume":        d.costume        = lvl; break;
            case "personal_art":            d.art            = lvl; break;
            case "personal_lighting":       d.lighting       = lvl; break;
            case "personal_grip":           d.grip           = lvl; break;
            case "personal_producer":       d.producer       = lvl; break;
        }
    }

    public void ResyncAllPersonnel(DepartmentSystem d)
    {
        if (allUpgrades == null || d == null) return;
        foreach (var cfg in allUpgrades)
            if (cfg != null && cfg.category == UpgradeCategory.Personnel && GetLevel(cfg) > 0)
                SyncPersonnel(cfg, d);
    }

    // ─── Save/Load support ────────────────────────────────────────────────────

    public UpgradeSaveEntry[] GetSaveData()
    {
        var list = new List<UpgradeSaveEntry>();
        foreach (var kv in _levels)
            if (kv.Value > 0)
                list.Add(new UpgradeSaveEntry { id = kv.Key, level = kv.Value });
        return list.ToArray();
    }

    public void LoadFromSave(UpgradeSaveEntry[] entries)
    {
        _levels.Clear();
        if (entries == null) return;
        foreach (var e in entries)
            if (!string.IsNullOrEmpty(e.id) && e.level > 0)
                _levels[e.id] = e.level;
    }

    public void Init()
    {
        _levels.Clear();
    }
}

[Serializable]
public class UpgradeSaveEntry
{
    public string id;
    public int    level;
}
