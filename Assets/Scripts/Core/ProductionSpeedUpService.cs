/// <summary>FASE 17C — Fixed diamond costs for production time reduction.</summary>
public static class ProductionSpeedUpService
{
    public const float Reduce5MinutesSeconds  = 5f * 60f;
    public const float Reduce15MinutesSeconds = 15f * 60f;
    public const int Reduce5DiamondCost  = 10;
    public const int Reduce15DiamondCost = 25;

    public static bool IsOptionValid(float timeLeftSeconds, float reductionSeconds) =>
        timeLeftSeconds > 0f && timeLeftSeconds >= reductionSeconds;

    public static bool HasAnyOption(float timeLeftSeconds) =>
        IsOptionValid(timeLeftSeconds, Reduce5MinutesSeconds) ||
        IsOptionValid(timeLeftSeconds, Reduce15MinutesSeconds);

    public static bool TryApply(string movieKey, float reductionSeconds, int diamondCost)
    {
        var studio = GameHub.Instance?.studio;
        return studio != null && studio.TrySpeedUpProduction(movieKey, reductionSeconds, diamondCost);
    }
}
