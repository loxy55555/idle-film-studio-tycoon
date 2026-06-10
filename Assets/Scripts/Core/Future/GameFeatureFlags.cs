using UnityEngine;

/// <summary>Feature flags for post-Alpha systems (City, Shop, etc.).</summary>
public static class GameFeatureFlags
{
    public static bool CityEnabled             { get; set; } = true;
    public static bool ShopEnabled             { get; set; }
    public static bool AdvancedDiamondsEnabled { get; set; }

    public static bool IsEnabled(GameFeature feature) => feature switch
    {
        GameFeature.City             => CityEnabled,
        GameFeature.Shop             => ShopEnabled,
        GameFeature.AdvancedDiamonds => AdvancedDiamondsEnabled,
        _                            => false,
    };
}

public enum GameFeature
{
    City,
    Shop,
    AdvancedDiamonds,
}
