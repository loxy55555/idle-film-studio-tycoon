using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

/// <summary>
/// FASE 15.9D / 17B — Unity IAP 4.x bridge (Google Play). Initialize only after LoadGame.
/// </summary>
public class PurchaseManager : MonoBehaviour, IDetailedStoreListener
{
    public static PurchaseManager Instance { get; private set; }

    public event Action OnCatalogReady;
    public event Action<string, IapErrorKind> PurchaseFailed;
    public event Action<IapErrorKind> OnIapError;
    public event Action OnEntitlementsChanged;

    public bool IsReady { get; private set; }
    public bool IsPurchaseInFlight { get; private set; }
    public bool IsRestoring { get; private set; }
    public IapErrorKind LastErrorKind { get; private set; } = IapErrorKind.None;
    public string LastErrorDetail { get; private set; }

    IStoreController _store;
    IExtensionProvider _extensions;
    bool _initializeRequested;
    bool _initFailed;
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

        Debug.Log($"[IAP] Initializing Unity Purchasing {IapProductCatalog.PackageVersion} — {IapProductCatalog.AllProductIds.Length} SKUs.");
        UnityPurchasing.Initialize(this, builder);
    }

    public void Purchase(string productId)
    {
        if (string.IsNullOrEmpty(productId)) return;

        if (!IsReady || _store == null)
        {
            var kind = ResolveNotReadyErrorKind();
            NotifyError(kind, $"Purchase blocked — store not ready ({productId})");
            PurchaseFailed?.Invoke(productId, kind);
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
        if (product == null)
        {
            NotifyError(IapErrorKind.ProductNotFound, $"Product missing from catalog: {productId}");
            PurchaseFailed?.Invoke(productId, IapErrorKind.ProductNotFound);
            return;
        }

        if (!product.availableToPurchase)
        {
            NotifyError(IapErrorKind.NoProductsAvailable,
                $"Product not available in Play Console: {productId} (availableToPurchase=false)");
            PurchaseFailed?.Invoke(productId, IapErrorKind.NoProductsAvailable);
            return;
        }

        IsPurchaseInFlight = true;
        _store.InitiatePurchase(product);
    }

    public void RestorePurchases()
    {
        if (!IsReady || _extensions == null)
        {
            var kind = ResolveNotReadyErrorKind();
            NotifyError(kind, "Restore blocked — store not ready");
            return;
        }

        if (IsRestoring) return;
        IsRestoring = true;

#if UNITY_ANDROID
        var google = _extensions.GetExtension<IGooglePlayStoreExtensions>();
        if (google == null)
        {
            IsRestoring = false;
            NotifyError(IapErrorKind.InitializationFailure, "IGooglePlayStoreExtensions unavailable");
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

    public string GetStoreStatusLabel()
    {
        if (!IsReady)
            return Loc.Get(GetNotReadyLabelKey());

        if (LastErrorKind == IapErrorKind.NoProductsAvailable)
            return Loc.Get(LocKeys.IapNoProducts);

        return null;
    }

    public static string GetErrorMessage(IapErrorKind kind) => kind switch
    {
        IapErrorKind.InitializationFailure => Loc.Get(LocKeys.IapInitFailed),
        IapErrorKind.NoProductsAvailable   => Loc.Get(LocKeys.IapNoProducts),
        IapErrorKind.ProductNotFound       => Loc.Get(LocKeys.IapProductNotFound),
        IapErrorKind.PurchaseFailed        => Loc.Get(LocKeys.IapPurchaseFailed),
        _                                  => Loc.Get(LocKeys.IapUnavailable),
    };

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
        _initFailed = false;
        LastErrorKind = IapErrorKind.None;
        LastErrorDetail = null;

        LogCatalogDiagnostics();
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
        OnInitializeFailed(error, error.ToString());
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        IsReady = false;
        _initFailed = true;

        var kind = error == InitializationFailureReason.NoProductsAvailable
            ? IapErrorKind.NoProductsAvailable
            : IapErrorKind.InitializationFailure;

        NotifyError(kind, $"Initialize failed: {error} — {message}");
        OnCatalogReady?.Invoke();
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
        else
        {
            Debug.LogWarning($"[IAP] Fulfillment failed for {productId} (tx {txId})");
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
        NotifyError(IapErrorKind.PurchaseFailed, $"Purchase failed: {id} — {reason}");
        PurchaseFailed?.Invoke(id, IapErrorKind.PurchaseFailed);
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

    void LogCatalogDiagnostics()
    {
        if (_store == null) return;

        int available = 0;
        foreach (string id in IapProductCatalog.AllProductIds)
        {
            var product = _store.products.WithID(id);
            bool purchasable = product != null && product.availableToPurchase;
            if (purchasable) available++;

            string price = product?.metadata?.localizedPriceString ?? "(no price)";
            Debug.Log($"[IAP] SKU {id}: available={purchasable}, price={price}");
        }

        if (available == 0)
        {
            NotifyError(IapErrorKind.NoProductsAvailable,
                "Initialized but 0/8 products availableToPurchase — create SKUs in Play Console");
        }
    }

    IapErrorKind ResolveNotReadyErrorKind()
    {
        if (_initFailed)
            return LastErrorKind != IapErrorKind.None ? LastErrorKind : IapErrorKind.InitializationFailure;
        return IapErrorKind.InitializationFailure;
    }

    string GetNotReadyLabelKey()
    {
        if (_initFailed)
            return LastErrorKind switch
            {
                IapErrorKind.NoProductsAvailable => LocKeys.IapNoProducts,
                IapErrorKind.InitializationFailure => LocKeys.IapInitFailed,
                _ => LocKeys.IapInitFailed,
            };
        return LocKeys.IapConnecting;
    }

    void NotifyError(IapErrorKind kind, string detail)
    {
        LastErrorKind = kind;
        LastErrorDetail = detail;
        Debug.LogWarning($"[IAP] {kind}: {detail}");
        AdRewardUI.ShowMessage(GetErrorMessage(kind));
        OnIapError?.Invoke(kind);
    }

    static string ResolveTransactionId(Product product)
    {
        // Google Play always provides transactionID for confirmed purchases. Use it as the
        // primary idempotency key so the fulfillment ledger can reliably de-duplicate.
        if (!string.IsNullOrEmpty(product.transactionID))
            return product.transactionID;

        // Fallback: derive a stable key from the receipt payload (same purchase = same receipt).
        // GetHashCode on Mono/Unity is deterministic within an installation, sufficient here.
        if (!string.IsNullOrEmpty(product.receipt))
            return "receipt:" + product.receipt.GetHashCode().ToString();

        // No stable identifier available. Return empty rather than a random GUID, which would
        // bypass the idempotency check on every call. The caller guards against empty IDs for
        // consumables (IapFulfillmentService.TryFulfillNewPurchase).
        Debug.LogWarning("[IAP] ResolveTransactionId: no transactionID or receipt for product " + product.definition?.id);
        return string.Empty;
    }
}
