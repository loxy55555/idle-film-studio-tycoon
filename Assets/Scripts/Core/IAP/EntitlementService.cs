using System;

/// <summary>FASE 15.9D — Runtime IAP ownership (mirrored in GameSaveData.iap v7).</summary>
public static class EntitlementService
{
    static IapSaveData _data;

    public static void LoadFromSave(IapSaveData data)
    {
        _data = data ?? new IapSaveData();
        if (_data.fulfilledTransactions == null)
            _data.fulfilledTransactions = Array.Empty<IapTransactionEntry>();
        SyncPremiumFeaturesFromIap();
    }

    public static IapSaveData GetSaveData()
    {
        if (_data == null)
            _data = new IapSaveData { fulfilledTransactions = Array.Empty<IapTransactionEntry>() };
        if (_data.fulfilledTransactions == null)
            _data.fulfilledTransactions = Array.Empty<IapTransactionEntry>();
        SyncPremiumFeaturesToIap();
        return _data;
    }

    public static void InitEmpty()
    {
        _data = new IapSaveData { fulfilledTransactions = Array.Empty<IapTransactionEntry>() };
    }

    public static bool IsOwned(string productId)
    {
        if (string.IsNullOrEmpty(productId)) return false;
        if (IapProductCatalog.IsConsumable(productId)) return false;

        if (productId == IapProductCatalog.NoAds)
            return _data?.noAdsOwned == true || PremiumFeatures.Instance?.NoAdsPurchased == true;

        var packId = IapProductCatalog.TryGetPackId(productId);
        if (packId.HasValue)
            return IsPackOwned(packId.Value);

        return false;
    }

    public static bool IsPackOwned(PremiumPackCatalog.PackId packId) => packId switch
    {
        PremiumPackCatalog.PackId.Supporter         => _data?.packSupporterOwned == true,
        PremiumPackCatalog.PackId.Producer          => _data?.packProducerOwned == true,
        PremiumPackCatalog.PackId.ExecutiveProducer => _data?.packExecutiveOwned == true,
        _ => false,
    };

    public static bool ArePackRewardsDelivered(PremiumPackCatalog.PackId packId) => packId switch
    {
        PremiumPackCatalog.PackId.Supporter         => _data?.packSupporterRewardsDelivered == true,
        PremiumPackCatalog.PackId.Producer          => _data?.packProducerRewardsDelivered == true,
        PremiumPackCatalog.PackId.ExecutiveProducer => _data?.packExecutiveRewardsDelivered == true,
        _ => false,
    };

    public static void SetProductOwned(string productId, bool owned)
    {
        EnsureData();
        switch (productId)
        {
            case IapProductCatalog.NoAds:
                _data.noAdsOwned = owned;
                if (owned) PremiumFeatures.Instance?.GrantNoAds(persist: false);
                break;
            case IapProductCatalog.PackSupporter:
                _data.packSupporterOwned = owned;
                if (owned) PremiumFeatures.Instance?.GrantNoAds(persist: false);
                break;
            case IapProductCatalog.PackProducer:
                _data.packProducerOwned = owned;
                if (owned) PremiumFeatures.Instance?.GrantNoAds(persist: false);
                break;
            case IapProductCatalog.PackExecutive:
                _data.packExecutiveOwned = owned;
                if (owned) PremiumFeatures.Instance?.GrantNoAds(persist: false);
                break;
        }
        SyncPremiumFeaturesToIap();
    }

    public static void MarkPackRewardsDelivered(PremiumPackCatalog.PackId packId)
    {
        EnsureData();
        switch (packId)
        {
            case PremiumPackCatalog.PackId.Supporter:
                _data.packSupporterRewardsDelivered = true;
                break;
            case PremiumPackCatalog.PackId.Producer:
                _data.packProducerRewardsDelivered = true;
                break;
            case PremiumPackCatalog.PackId.ExecutiveProducer:
                _data.packExecutiveRewardsDelivered = true;
                break;
        }
    }

    public static bool HasFulfilledTransaction(string transactionId)
    {
        if (string.IsNullOrEmpty(transactionId) || _data?.fulfilledTransactions == null)
            return false;
        foreach (var e in _data.fulfilledTransactions)
        {
            if (e != null && e.transactionId == transactionId)
                return true;
        }
        return false;
    }

    public static void RecordFulfilledTransaction(string transactionId, string productId)
    {
        if (string.IsNullOrEmpty(transactionId)) return;
        EnsureData();
        if (HasFulfilledTransaction(transactionId)) return;

        var list = new System.Collections.Generic.List<IapTransactionEntry>(_data.fulfilledTransactions ?? Array.Empty<IapTransactionEntry>());
        list.Add(new IapTransactionEntry
        {
            transactionId = transactionId,
            productId       = productId,
            fulfilledUnix   = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        });
        _data.fulfilledTransactions = list.ToArray();
    }

    public static void RecordRestore()
    {
        EnsureData();
        _data.lastRestoreUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _data.restoreVersion++;
    }

    public static void SyncPremiumFeaturesFromIap()
    {
        if (_data == null || PremiumFeatures.Instance == null) return;
        if (_data.noAdsOwned)
            PremiumFeatures.Instance.GrantNoAds(persist: false);
        else if (IsAnyNoAdsPackOwned())
            PremiumFeatures.Instance.GrantNoAds(persist: false);
    }

    public static void SyncPremiumFeaturesToIap()
    {
        if (_data == null) return;
        if (PremiumFeatures.Instance?.NoAdsPurchased == true)
            _data.noAdsOwned = true;
    }

    static bool IsAnyNoAdsPackOwned() =>
        _data.packSupporterOwned || _data.packProducerOwned || _data.packExecutiveOwned;

    static void EnsureData()
    {
        if (_data == null)
            _data = new IapSaveData { fulfilledTransactions = Array.Empty<IapTransactionEntry>() };
        if (_data.fulfilledTransactions == null)
            _data.fulfilledTransactions = Array.Empty<IapTransactionEntry>();
    }
}
