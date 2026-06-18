using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

/// <summary>
/// FASE 15.9D — Unity IAP 4.x bridge (Google Play). Initialize only after LoadGame.
/// </summary>
public class PurchaseManager : MonoBehaviour, IDetailedStoreListener
{
    public static PurchaseManager Instance { get; private set; }

    public event Action OnCatalogReady;
    public event Action<string> PurchaseFailed;
    public event Action OnEntitlementsChanged;

    public bool IsReady { get; private set; }
    public bool IsPurchaseInFlight { get; private set; }
    public bool IsRestoring { get; private set; }

    IStoreController _store;
    IExtensionProvider _extensions;
    bool _initializeRequested;
    bool _silentRestorePending = true;

    public static void EnsureOn(GameObject host)
    {
        if (Instance != null) return;
        var go = new GameObject("PurchaseManager");
        go.transform.SetParent(host.transform, false);
        go.AddComponent<PurchaseManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Call once after SaveSystem.LoadGame completes.</summary>
    public void Initialize()
    {
        if (_initializeRequested) return;
        _initializeRequested = true;

        if (IsReady) return;

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        foreach (string id in IapProductCatalog.AllProductIds)
            builder.AddProduct(id, IapProductCatalog.ToUnityProductType(id));

        Debug.Log($"[IAP] Initializing Unity Purchasing {IapProductCatalog.PackageVersion}...");
        UnityPurchasing.Initialize(this, builder);
    }

    public void Purchase(string productId)
    {
        if (string.IsNullOrEmpty(productId)) return;

        if (!IsReady || _store == null)
        {
            PurchaseFailed?.Invoke(productId);
            AdRewardUI.ShowMessage(Loc.Get(LocKeys.IapUnavailable));
            return;
        }

        if (IsPurchaseInFlight)
            return;

        if (IapProductCatalog.IsNonConsumable(productId) && IsOwned(productId))
        {
            AdRewardUI.ShowMessage(Loc.Get(LocKeys.StorePackAlreadyOwned));
            return;
        }

        var product = _store.products.WithID(productId);
        if (product == null || !product.availableToPurchase)
        {
            PurchaseFailed?.Invoke(productId);
            AdRewardUI.ShowMessage(Loc.Get(LocKeys.IapUnavailable));
            return;
        }

        IsPurchaseInFlight = true;
        _store.InitiatePurchase(product);
    }

    public void RestorePurchases()
    {
        if (!IsReady || _extensions == null)
        {
            AdRewardUI.ShowMessage(Loc.Get(LocKeys.IapRestoreFailed));
            return;
        }

        if (IsRestoring) return;
        IsRestoring = true;

#if UNITY_ANDROID
        var google = _extensions.GetExtension<IGooglePlayStoreExtensions>();
        if (google == null)
        {
            IsRestoring = false;
            AdRewardUI.ShowMessage(Loc.Get(LocKeys.IapRestoreFailed));
            return;
        }

        google.RestoreTransactions((success, error) =>
        {
            IsRestoring = false;
            if (success)
            {
                ApplyRestoreFromStore();
                EntitlementService.RecordRestore();
                GameHub.Instance?.save?.Save("IAP_Restore");
                AdRewardUI.ShowMessage(Loc.Get(LocKeys.IapRestoreSuccess));
            }
            else
            {
                Debug.LogWarning("[IAP] Restore failed: " + error);
                AdRewardUI.ShowMessage(Loc.Get(LocKeys.IapRestoreFailed));
            }
        });
#else
        ApplyRestoreFromStore();
        EntitlementService.RecordRestore();
        GameHub.Instance?.save?.Save("IAP_Restore");
        IsRestoring = false;
        AdRewardUI.ShowMessage(Loc.Get(LocKeys.IapRestoreSuccess));
#endif
    }

    public string GetLocalizedPrice(string productId)
    {
        if (IsReady && _store != null)
        {
            var product = _store.products.WithID(productId);
            if (product?.metadata != null && !string.IsNullOrEmpty(product.metadata.localizedPriceString))
                return product.metadata.localizedPriceString;
        }
        return IapProductCatalog.GetFallbackPrice(productId);
    }

    public bool IsOwned(string productId)
    {
        if (string.IsNullOrEmpty(productId) || IapProductCatalog.IsConsumable(productId))
            return false;

        if (EntitlementService.IsOwned(productId))
            return true;

        if (IsReady && _store != null)
        {
            var product = _store.products.WithID(productId);
            if (product != null && product.hasReceipt)
                return true;
        }

        return false;
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        _store = controller;
        _extensions = extensions;
        IsReady = true;
        Debug.Log("[IAP] Initialized successfully.");
        OnCatalogReady?.Invoke();

        if (_silentRestorePending)
        {
            _silentRestorePending = false;
            ApplyRestoreFromStore();
            GameHub.Instance?.save?.Save("IAP_SilentRestore");
        }
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogWarning("[IAP] Initialize failed: " + error);
        IsReady = false;
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogWarning("[IAP] Initialize failed: " + error + " — " + message);
        IsReady = false;
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        IsPurchaseInFlight = false;
        var product = args.purchasedProduct;
        string productId = product.definition.id;
        string txId = ResolveTransactionId(product);

        bool ok = IapFulfillmentService.TryFulfillNewPurchase(txId, productId);
        if (ok)
        {
            FirebaseManager.Instance?.LogIapPurchase(productId);
            OnEntitlementsChanged?.Invoke();
        }

        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        HandlePurchaseFailed(product, failureReason.ToString());
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        HandlePurchaseFailed(product, failureDescription.message);
    }

    void HandlePurchaseFailed(Product product, string reason)
    {
        IsPurchaseInFlight = false;
        string id = product?.definition?.id ?? "unknown";
        Debug.LogWarning($"[IAP] Purchase failed: {id} — {reason}");
        PurchaseFailed?.Invoke(id);
        AdRewardUI.ShowMessage(Loc.Get(LocKeys.IapPurchaseFailed));
    }

    void ApplyRestoreFromStore()
    {
        if (_store == null) return;

        foreach (string id in IapProductCatalog.AllProductIds)
        {
            if (!IapProductCatalog.IsNonConsumable(id)) continue;
            var product = _store.products.WithID(id);
            if (product == null || !product.hasReceipt) continue;
            IapFulfillmentService.ApplyRestoreOnly(id);
        }

        EntitlementService.SyncPremiumFeaturesFromIap();
        OnEntitlementsChanged?.Invoke();
    }

    static string ResolveTransactionId(Product product)
    {
        if (!string.IsNullOrEmpty(product.transactionID))
            return product.transactionID;
        if (!string.IsNullOrEmpty(product.receipt))
            return product.receipt.GetHashCode().ToString();
        return Guid.NewGuid().ToString("N");
    }
}
