/// <summary>Real accumulated studio bonuses for the summary strip (Phase 10.2).</summary>
public static class StudioBonusSummary
{
    public static string FormatAccumulatedLine()
    {
        var hub = GameHub.Instance;
        if (hub?.departments == null) return string.Empty;

        var D = hub.departments;
        var U = hub.upgrades;

        float incomePct = (D.CalculateQuality() - 1f) * 100f;
        float speedPct  = (D.CalculateSpeed() - 1f) * 100f;
        float repPct    = U != null ? U.TotalEffect(UpgradeEffectType.ReputationBonus) * 100f : 0f;

        return string.Format(Loc.Get(LocKeys.StudioBonusSummaryFmt), incomePct, repPct, speedPct);
    }
}
