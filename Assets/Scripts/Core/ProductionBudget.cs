using UnityEngine;

/// <summary>Future production budget tier — modifiers are placeholders until balance phase.</summary>
public enum ProductionBudget
{
    Cheap,
    Standard,
    Premium,
}

[System.Serializable]
public struct ProductionBudgetModifiers
{
    public float durationMultiplier;
    public float moneyMultiplier;
    public float reputationMultiplier;

    public static ProductionBudgetModifiers Identity => new()
    {
        durationMultiplier     = 1f,
        moneyMultiplier        = 1f,
        reputationMultiplier   = 1f,
    };
}

/// <summary>Central hook for budget-based production modifiers (no UI yet).</summary>
public static class ProductionBudgetRules
{
    public static ProductionBudgetModifiers GetModifiers(ProductionBudget budget) => budget switch
    {
        ProductionBudget.Cheap    => new ProductionBudgetModifiers
        {
            durationMultiplier   = 0.8f,
            moneyMultiplier      = 0.8f,
            reputationMultiplier = 0.8f,
        },
        ProductionBudget.Standard => ProductionBudgetModifiers.Identity,
        ProductionBudget.Premium  => new ProductionBudgetModifiers
        {
            durationMultiplier   = 1.5f,
            moneyMultiplier      = 1.5f,
            reputationMultiplier = 1.5f,
        },
        _                         => ProductionBudgetModifiers.Identity,
    };

    public static string FormatPercentDelta(float multiplier)
    {
        float pct = (multiplier - 1f) * 100f;
        if (Mathf.Approximately(pct, 0f)) return "0%";
        return (pct > 0f ? "+" : string.Empty) + pct.ToString("0") + "%";
    }

    public static string FormatStatBlock(ProductionBudget budget)
    {
        var mod = GetModifiers(budget);
        return Loc.Format(
            LocKeys.ProdBudgetStatBlock,
            FormatPercentDelta(mod.durationMultiplier),
            FormatPercentDelta(mod.moneyMultiplier),
            FormatPercentDelta(mod.reputationMultiplier));
    }

    public static void Apply(ref float duration, ref long reward, ref float reputation, ProductionBudget budget)
    {
        var mod = GetModifiers(budget);
        duration   *= mod.durationMultiplier;
        reward      = (long)(reward * mod.moneyMultiplier);
        reputation *= mod.reputationMultiplier;
    }
}
