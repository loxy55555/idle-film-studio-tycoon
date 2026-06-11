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
        ProductionBudget.Cheap    => ProductionBudgetModifiers.Identity,
        ProductionBudget.Standard => ProductionBudgetModifiers.Identity,
        ProductionBudget.Premium  => ProductionBudgetModifiers.Identity,
        _                         => ProductionBudgetModifiers.Identity,
    };

    public static void Apply(ref float duration, ref long reward, ref float reputation, ProductionBudget budget)
    {
        var mod = GetModifiers(budget);
        duration   *= mod.durationMultiplier;
        reward      = (long)(reward * mod.moneyMultiplier);
        reputation *= mod.reputationMultiplier;
    }
}
