/// <summary>Premiere screen localization helpers (Phase 8.5).</summary>
public static class PremiereLoc
{
    public static string FormatMoneyReward(long amount) =>
        Loc.Format(LocKeys.PremiereMoneyReward, AnimatedMoneyText.FormatMoney(amount));

    public static string FormatRepReward(float rep) =>
        Loc.Format(LocKeys.PremiereRepReward, rep.ToString("0.0"));
}
