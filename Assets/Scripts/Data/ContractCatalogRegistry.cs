using UnityEngine;

/// <summary>
/// Contract catalog scale targets (60–80 total). Runtime uses existing assets;
/// this registry documents distribution goals for future content authoring.
/// </summary>
public static class ContractCatalogRegistry
{
    public const int TargetMin = ContentScaleDatabase.TargetContractCountMin;
    public const int TargetMax = ContentScaleDatabase.TargetContractCountMax;

    public static int TargetRecommended => (TargetMin + TargetMax) / 2;

    public static int CumulativeTargetForCity(int cityLevel)
    {
        cityLevel = Mathf.Clamp(cityLevel, 1, ContentScaleDatabase.ContractCumulativeTarget.Length);
        return ContentScaleDatabase.ContractCumulativeTarget[cityLevel - 1];
    }

    public static int SlotsRemaining(int currentCount) =>
        Mathf.Max(0, TargetRecommended - currentCount);
}

/// <summary>Upgrade catalog scale targets (60–80 total).</summary>
public static class UpgradeCatalogRegistry
{
    public const int TargetMin = ContentScaleDatabase.TargetUpgradeCountMin;
    public const int TargetMax = ContentScaleDatabase.TargetUpgradeCountMax;

    public static int TargetRecommended => (TargetMin + TargetMax) / 2;

    public static int CumulativeTargetForCity(int cityLevel)
    {
        cityLevel = Mathf.Clamp(cityLevel, 1, ContentScaleDatabase.UpgradeCumulativeTarget.Length);
        return ContentScaleDatabase.UpgradeCumulativeTarget[cityLevel - 1];
    }

    public static int SlotsRemaining(int currentCount) =>
        Mathf.Max(0, TargetRecommended - currentCount);
}
