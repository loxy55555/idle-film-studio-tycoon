public static class EconomyFormula
{
    public static float GetQuality(DepartmentSystem d)
    {
        return d.CalculateQuality();
    }

    public static float GetSpeed(DepartmentSystem d)
    {
        return d.CalculateSpeed();
    }

    public static float GetCost(long cost, DepartmentSystem d)
    {
        return cost * (1f - d.CalculateCostReduction());
    }

    public static float GetIncome(long baseIncome, float quality, float reputation)
    {
        float repMultiplier = 1f + (reputation / 100f);
        return baseIncome * quality * repMultiplier;
    }
}