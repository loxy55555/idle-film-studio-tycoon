using System;
using UnityEngine;

/// <summary>
/// FASE 15.2A / 15.9D — Premium pack definitions and reward delivery.
/// Ownership lives in IapSaveData (save v7); purchases go through PurchaseManager.
/// </summary>
public static class PremiumPackCatalog
{
    public enum PackId
    {
        StarterPack     = 0,
        Supporter       = 1,
        Producer        = 2,
        ExecutiveProducer = 3,
    }

    public static readonly PremiumPackDef[] AllPacks = new[]
    {
        new PremiumPackDef
        {
            id            = PackId.Supporter,
            storeProductId = IapProductCatalog.PackSupporter,
            locNameKey    = "pack.supporter.name",
            locDescKey    = "pack.supporter.desc",
            priceDisplay  = "4,99 €",
            diamonds      = 500,
            noAdsIncluded = true,
        },
        new PremiumPackDef
        {
            id            = PackId.Producer,
            storeProductId = IapProductCatalog.PackProducer,
            locNameKey    = "pack.producer.name",
            locDescKey    = "pack.producer.desc",
            priceDisplay  = "9,99 €",
            diamonds      = 2000,
            noAdsIncluded = true,
        },
        new PremiumPackDef
        {
            id            = PackId.ExecutiveProducer,
            storeProductId = IapProductCatalog.PackExecutive,
            locNameKey    = "pack.executive.name",
            locDescKey    = "pack.executive.desc",
            priceDisplay  = "19,99 €",
            diamonds      = 5000,
            noAdsIncluded = true,
        },
    };

    public static PremiumPackDef Get(PackId id)
    {
        foreach (var p in AllPacks)
            if (p.id == id) return p;
        return null;
    }

    /// <summary>Deliver pack rewards only — called by IapFulfillmentService on first purchase.</summary>
    public static void DeliverPackRewards(PremiumPackDef pack)
    {
        var hub = GameHub.Instance;
        if (hub == null || pack == null) return;

        if (pack.diamonds > 0)
            hub.diamonds?.Add(pack.diamonds);

        if (pack.moneyBonus > 0)
            hub.studio?.AddMoney(pack.moneyBonus);

        if (pack.extraXP > 0)
            hub.studioLevel?.AddXP(pack.extraXP);

        if (pack.boostIncome)  BoostSystem.Instance?.TryActivate(BoostSystem.BoostType.Income);
        if (pack.boostXP)      BoostSystem.Instance?.TryActivate(BoostSystem.BoostType.XP);
        if (pack.boostRep)     BoostSystem.Instance?.TryActivate(BoostSystem.BoostType.Rep);

        if (pack.noAdsIncluded)
            PremiumFeatures.Instance?.GrantNoAds(persist: false);
    }

    public static bool PackAlreadyPurchased(PackId id) =>
        EntitlementService.IsPackOwned(id);

#if UNITY_EDITOR
    /// <summary>Editor/debug only — bypasses store.</summary>
    public static void DebugSimulatePurchase(PackId id)
    {
        if (PackAlreadyPurchased(id)) return;
        var pack = Get(id);
        if (pack == null) return;
        string productId = IapProductCatalog.GetStoreProductId(id);
        EntitlementService.SetProductOwned(productId, true);
        if (!EntitlementService.ArePackRewardsDelivered(id))
        {
            DeliverPackRewards(pack);
            EntitlementService.MarkPackRewardsDelivered(id);
        }
        EntitlementService.RecordFulfilledTransaction("debug:" + id, productId);
        GameHub.Instance?.save?.Save("DebugPack_" + id);
    }

    public static void DebugResetAllPurchases()
    {
        EntitlementService.InitEmpty();
        Debug.Log("[PremiumPack] DEBUG: IAP ownership reset in memory — reload save to restore.");
    }
#endif
}

[Serializable]
public class PremiumPackDef
{
    public PremiumPackCatalog.PackId id;
    public string storeProductId;
    public string locNameKey;
    public string locDescKey;
    public string priceDisplay;
    public int    diamonds;
    public long   moneyBonus;
    public float  extraXP;
    public bool   boostIncome;
    public bool   boostXP;
    public bool   boostRep;
    public bool   noAdsIncluded;
}
