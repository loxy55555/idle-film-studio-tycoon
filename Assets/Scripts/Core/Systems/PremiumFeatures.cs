using UnityEngine;

/// <summary>
/// FASE 15.2A — BLOQUE H + I
/// Persistent premium entitlements:
///   H — NoAdsPurchased: skip ad display, keep all daily limits and balance restrictions.
///   I — OfflinePremiumUnlocked ("Productor Remoto"): doubles the offline income cap.
///
/// Both flags live in the JSON save file for security and consistency.
/// </summary>
public class PremiumFeatures : MonoBehaviour
{
    public static PremiumFeatures Instance { get; private set; }

    /// <summary>BLOQUE H — No Ads purchased. Ad display is skipped; rewards delivered immediately.</summary>
    public bool NoAdsPurchased { get; private set; }

    /// <summary>BLOQUE I — Offline max extended from BaseOfflineHours to PremiumOfflineHours.</summary>
    public bool OfflinePremiumUnlocked { get; private set; }

    /// <summary>Base offline cap in hours (same as StudioManager.MaxOfflineSeconds / 3600).</summary>
    public const float BaseOfflineHours = 1f;

    /// <summary>Premium offline cap in hours ("Productor Remoto").</summary>
    public const float PremiumOfflineHours = 2f;

    /// <summary>Returns the effective offline cap in seconds based on current entitlements.</summary>
    public float EffectiveOfflineSeconds =>
        (OfflinePremiumUnlocked ? PremiumOfflineHours : BaseOfflineHours) * 3600f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void OnEnable()
    {
        if (Instance == null) Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Public API ──────────────────────────────────────────────────────────────

    /// <summary>BLOQUE H — Idempotent NoAds grant (IAP, pack, or restore).</summary>
    public void GrantNoAds(bool persist = true)
    {
        if (NoAdsPurchased) return;
        NoAdsPurchased = true;
        if (persist)
            GameHub.Instance?.save?.Save("GrantNoAds");
        Debug.Log("[PremiumFeatures] NoAds granted.");
    }

    /// <summary>Legacy/simulated entry — offline premium store still uses direct grant.</summary>
    public void PurchaseNoAds() => GrantNoAds();

    /// <summary>BLOQUE I — Simulate or apply "Productor Remoto" offline upgrade.</summary>
    public void PurchaseOfflinePremium()
    {
        if (OfflinePremiumUnlocked) return;
        OfflinePremiumUnlocked = true;
        GameHub.Instance?.save?.Save("PurchaseOfflinePremium");
        Debug.Log($"[PremiumFeatures] Offline Premium unlocked — cap now {PremiumOfflineHours}h.");
    }

    // ── Save / Load ─────────────────────────────────────────────────────────────

    public PremiumFeaturesSaveData GetSaveData() => new PremiumFeaturesSaveData
    {
        noAdsPurchased       = NoAdsPurchased,
        offlinePremiumUnlocked = OfflinePremiumUnlocked,
    };

    public void LoadFromSave(PremiumFeaturesSaveData data)
    {
        if (data == null) return;
        NoAdsPurchased         = data.noAdsPurchased;
        OfflinePremiumUnlocked = data.offlinePremiumUnlocked;
    }

    /// <summary>
    /// BUG-03 — Called by SaveSystem after load to reconcile PlayerPrefs pack ownership
    /// with JSON-persisted NoAdsPurchased when they disagree. Does NOT trigger a re-save.
    /// </summary>
    public void ReconcileNoAds()
    {
        if (NoAdsPurchased) return;
        NoAdsPurchased = true;
        Debug.Log("[PremiumFeatures] NoAds reconciled from pack ownership (PlayerPrefs).");
    }

    public void Init()
    {
        NoAdsPurchased         = false;
        OfflinePremiumUnlocked = false;
    }
}

[System.Serializable]
public class PremiumFeaturesSaveData
{
    public bool noAdsPurchased;
    public bool offlinePremiumUnlocked;
}
