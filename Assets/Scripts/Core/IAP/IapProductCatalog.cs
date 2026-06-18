using System.Collections.Generic;
using UnityEngine.Purchasing;

/// <summary>
/// FASE 15.9D — Central IAP SKU catalog.
/// Package: com.unity.purchasing 4.12.2 (Unity IAP 4.x, Google Play, Unity 6000.4).
/// </summary>
public static class IapProductCatalog
{
    public const string PackageVersion = "4.11.0";

    public const string Diamonds100   = "diamonds_100";
    public const string Diamonds550   = "diamonds_550";
    public const string Diamonds1500  = "diamonds_1500";
    public const string Diamonds5000  = "diamonds_5000";
    public const string NoAds         = "no_ads";
    public const string PackSupporter = "pack_supporter";
    public const string PackProducer  = "pack_producer";
    public const string PackExecutive = "pack_executive";

    public enum ProductKind { Consumable, NonConsumable }

    static readonly Dictionary<string, ProductKind> Kinds = new()
    {
        { Diamonds100,   ProductKind.Consumable },
        { Diamonds550,   ProductKind.Consumable },
        { Diamonds1500,  ProductKind.Consumable },
        { Diamonds5000,  ProductKind.Consumable },
        { NoAds,         ProductKind.NonConsumable },
        { PackSupporter, ProductKind.NonConsumable },
        { PackProducer,  ProductKind.NonConsumable },
        { PackExecutive, ProductKind.NonConsumable },
    };

    static readonly Dictionary<string, int> DiamondAmounts = new()
    {
        { Diamonds100,   100 },
        { Diamonds550,   550 },
        { Diamonds1500,  1500 },
        { Diamonds5000,  5000 },
    };

    static readonly Dictionary<string, string> FallbackPrices = new()
    {
        { Diamonds100,   "0,99 €" },
        { Diamonds550,   "3,99 €" },
        { Diamonds1500,  "7,99 €" },
        { Diamonds5000,  "19,99 €" },
        { NoAds,         "3,99 €" },
        { PackSupporter, "4,99 €" },
        { PackProducer,  "9,99 €" },
        { PackExecutive, "19,99 €" },
    };

    public static readonly string[] AllProductIds =
    {
        Diamonds100, Diamonds550, Diamonds1500, Diamonds5000,
        NoAds, PackSupporter, PackProducer, PackExecutive,
    };

    public static ProductKind GetKind(string productId) =>
        Kinds.TryGetValue(productId, out var k) ? k : ProductKind.Consumable;

    public static bool IsConsumable(string productId) =>
        GetKind(productId) == ProductKind.Consumable;

    public static bool IsNonConsumable(string productId) =>
        GetKind(productId) == ProductKind.NonConsumable;

    public static ProductType ToUnityProductType(string productId) =>
        IsConsumable(productId) ? ProductType.Consumable : ProductType.NonConsumable;

    public static int GetDiamondAmount(string productId) =>
        DiamondAmounts.TryGetValue(productId, out int n) ? n : 0;

    public static string GetFallbackPrice(string productId) =>
        FallbackPrices.TryGetValue(productId, out var p) ? p : "—";

    public static PremiumPackCatalog.PackId? TryGetPackId(string productId) => productId switch
    {
        PackSupporter => PremiumPackCatalog.PackId.Supporter,
        PackProducer  => PremiumPackCatalog.PackId.Producer,
        PackExecutive => PremiumPackCatalog.PackId.ExecutiveProducer,
        _ => null,
    };

    public static string GetStoreProductId(PremiumPackCatalog.PackId packId) => packId switch
    {
        PremiumPackCatalog.PackId.Supporter         => PackSupporter,
        PremiumPackCatalog.PackId.Producer          => PackProducer,
        PremiumPackCatalog.PackId.ExecutiveProducer => PackExecutive,
        _ => null,
    };
}
