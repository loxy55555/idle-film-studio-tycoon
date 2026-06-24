using System.Collections.Generic;

/// <summary>
/// Legendary productions occupy two slot units simultaneously until they finish.
/// </summary>
public static class LegendaryProductionRules
{
    public const int LegendarySlotCost = 2;
    public const int NormalSlotCost    = 1;

    public static bool IsLegendary(MovieConfig cfg) =>
        cfg != null && cfg.rarity == MovieRarity.Legendary;

    public static int GetSlotCost(MovieConfig cfg) =>
        IsLegendary(cfg) ? LegendarySlotCost : NormalSlotCost;

    public static int GetUsedSlotUnits(IEnumerable<MovieConfig> activeConfigs)
    {
        if (activeConfigs == null) return 0;

        int used = 0;
        foreach (var cfg in activeConfigs)
        {
            if (cfg == null) continue;
            used += GetSlotCost(cfg);
        }

        return used;
    }

    public static bool CanStart(MovieConfig cfg, int maxSlots, int usedSlotUnits, out string blockReason)
    {
        blockReason = null;
        if (cfg == null)
        {
            blockReason = Loc.Get(LocKeys.ProdMovieInvalid);
            return false;
        }

        int cost = GetSlotCost(cfg);
        if (usedSlotUnits + cost <= maxSlots) return true;

        blockReason = IsLegendary(cfg)
            ? Loc.Get(LocKeys.ProdLegendaryNeedsSlots)
            : Loc.Get(LocKeys.UxNoSlotsAvailable);
        return false;
    }
}
