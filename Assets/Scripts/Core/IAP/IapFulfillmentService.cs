using UnityEngine;

/// <summary>
/// FASE 15.9D — Idempotent IAP reward delivery.
/// New purchases may deliver rewards; restore applies ownership only (15.9C-R).
/// </summary>
public static class IapFulfillmentService
{
    /// <summary>New purchase after Google Play confirmation. May deliver rewards.</summary>
    public static bool TryFulfillNewPurchase(string transactionId, string productId)
    {
        if (string.IsNullOrEmpty(productId))
            return false;

        if (!string.IsNullOrEmpty(transactionId) && EntitlementService.HasFulfilledTransaction(transactionId))
        {
            Debug.Log($"[IAP] Transaction already fulfilled: {transactionId}");
            ApplyOwnershipOnly(productId);
            return true;
        }

        // Consumables (diamonds) require a stable transaction ID so the fulfillment ledger can
        // de-duplicate if ProcessPurchase fires more than once for the same purchase.
        // Non-consumables are idempotent by nature (ownership flag), so we allow them through.
        if (string.IsNullOrEmpty(transactionId) && IapProductCatalog.IsConsumable(productId))
        {
            Debug.LogWarning($"[IAP] Blocking consumable {productId} — no stable transaction ID; rewards NOT delivered to prevent double-grant.");
            return false;
        }

        if (IapProductCatalog.IsConsumable(productId))
            return FulfillConsumable(transactionId, productId);

        if (productId == IapProductCatalog.NoAds)
            return FulfillNoAds(transactionId);

        var packId = IapProductCatalog.TryGetPackId(productId);
        if (packId.HasValue)
            return FulfillPackFirstPurchase(transactionId, packId.Value);

        Debug.LogWarning($"[IAP] Unknown product: {productId}");
        return false;
    }

    /// <summary>Restore path — ownership + NoAds only. Never re-delivers diamonds or pack rewards.</summary>
    public static void ApplyRestoreOnly(string productId)
    {
        if (string.IsNullOrEmpty(productId) || IapProductCatalog.IsConsumable(productId))
            return;

        ApplyOwnershipOnly(productId);
    }

    static void ApplyOwnershipOnly(string productId)
    {
        EntitlementService.SetProductOwned(productId, true);
    }

    static bool FulfillConsumable(string transactionId, string productId)
    {
        int amount = IapProductCatalog.GetDiamondAmount(productId);
        if (amount <= 0) return false;

        var wallet = GameHub.Instance?.diamonds;
        if (wallet == null) return false;

        wallet.Add(amount);
        EntitlementService.RecordFulfilledTransaction(transactionId, productId);
        GameHub.Instance?.save?.Save("IAP_" + productId);
        AdRewardUI.ShowMessage(string.Format(Loc.Get(LocKeys.FreeDiamondsAwarded), amount));
        Debug.Log($"[IAP] Consumable fulfilled: {productId} +{amount} diamonds.");
        return true;
    }

    static bool FulfillNoAds(string transactionId)
    {
        EntitlementService.SetProductOwned(IapProductCatalog.NoAds, true);
        PremiumFeatures.Instance?.GrantNoAds();
        EntitlementService.RecordFulfilledTransaction(transactionId, IapProductCatalog.NoAds);
        GameHub.Instance?.save?.Save("IAP_no_ads");
        AdRewardUI.ShowMessage(Loc.Get(LocKeys.StoreNoAds) + " ✓");
        Debug.Log("[IAP] no_ads fulfilled.");
        return true;
    }

    static bool FulfillPackFirstPurchase(string transactionId, PremiumPackCatalog.PackId packId)
    {
        string productId = IapProductCatalog.GetStoreProductId(packId);
        EntitlementService.SetProductOwned(productId, true);

        if (!EntitlementService.ArePackRewardsDelivered(packId))
        {
            var pack = PremiumPackCatalog.Get(packId);
            if (pack != null)
                PremiumPackCatalog.DeliverPackRewards(pack);
            EntitlementService.MarkPackRewardsDelivered(packId);
        }

        EntitlementService.RecordFulfilledTransaction(transactionId, productId);
        GameHub.Instance?.save?.Save("IAP_" + productId);
        AdRewardUI.ShowMessage(Loc.Format(LocKeys.IapPackActivatedFmt, packId));
        Debug.Log($"[IAP] Pack first purchase fulfilled: {packId}");
        return true;
    }
}
