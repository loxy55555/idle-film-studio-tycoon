using System;

/// <summary>FASE 15.9D — IAP ownership and fulfillment ledger (save v7).</summary>
[Serializable]
public class IapSaveData
{
    public bool noAdsOwned;
    public bool packSupporterOwned;
    public bool packProducerOwned;
    public bool packExecutiveOwned;

    public bool packSupporterRewardsDelivered;
    public bool packProducerRewardsDelivered;
    public bool packExecutiveRewardsDelivered;

    public IapTransactionEntry[] fulfilledTransactions;

    public long lastRestoreUnix;
    public int  restoreVersion;
}

[Serializable]
public class IapTransactionEntry
{
    public string transactionId;
    public string productId;
    public long   fulfilledUnix;
}
