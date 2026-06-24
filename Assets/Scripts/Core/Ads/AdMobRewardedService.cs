using System;
using System.Collections;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

/// <summary>
/// FASE 18 / 18.6 — AdMob Rewarded Ads: init, preload, retry, show-on-ready queue, user feedback.
/// Implements IAdsRewardProvider and registers with GameplayRefreshService.
/// </summary>
public class AdMobRewardedService : MonoBehaviour, IAdsRewardProvider
{
    public static AdMobRewardedService Instance { get; private set; }

    const float InitialRetrySeconds = 5f;
    const float MaxRetrySeconds = 60f;
    const float PendingShowTimeoutSeconds = 30f;

    const string ReasonNotReady = "not_ready";
    const string ReasonNotInitialized = "not_initialized";
    const string ReasonLoadFailed = "load_failed";
    const string ReasonAlreadyShowing = "already_showing";

    struct PendingShowRequest
    {
        public string Placement;
        public Action<bool> Callback;
        public float RequestedAt;
        public bool WaitingForInit;
    }

    RewardedAd _rewardedAd;
    bool _initialized;
    bool _initializing;
    bool _loading;
    bool _showInFlight;
    bool _rewardEarnedThisShow;
    float _nextRetryDelay = InitialRetrySeconds;
    float _retryAt = -1f;

    string _currentPlacement;
    Action<bool> _pendingCallback;
    PendingShowRequest? _queuedShow;

    public static void EnsureOn(GameObject host)
    {
        if (Instance != null) return;
        var go = new GameObject("AdMobRewardedService");
        go.transform.SetParent(host.transform, false);
        go.AddComponent<AdMobRewardedService>();
    }

    public void BeginAfterGameHub()
    {
        if (_initialized || _initializing) return;
        StartCoroutine(InitializeRoutine());
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        DestroyLoadedAd();
    }

    void Update()
    {
        if (_retryAt > 0f && Time.unscaledTime >= _retryAt && !_loading && !_showInFlight)
        {
            _retryAt = -1f;
            RequestLoad("retry_timer");
        }

        if (_queuedShow.HasValue)
        {
            var q = _queuedShow.Value;
            if (Time.unscaledTime - q.RequestedAt >= PendingShowTimeoutSeconds)
                FailQueuedShow(ReasonNotReady, "queued_show_timeout");
        }
    }

    IEnumerator InitializeRoutine()
    {
        _initializing = true;

        // Ensures all MobileAds/UMP callbacks are dispatched on the Unity main thread,
        // preventing cross-thread access to UnityEngine objects in event handlers.
        MobileAds.RaiseAdEventsOnUnityMainThread = true;

        string appId = AdMobRuntimeConfig.AndroidAppId;
        string rewardedUnit = AdMobRuntimeConfig.RewardedAdUnitId;
        Debug.Log($"[AdMob] Runtime IDs — appId={appId} rewardedUnit={rewardedUnit}");

#if UNITY_EDITOR
        var cfg = AdMobRuntimeConfig.Load();
        if (!cfg.useTestAdsInEditor)
            Debug.LogWarning("[AdMob] Editor build — real ads may not display. Keep useTestAdsInEditor=true for local testing.");
#endif

        // Gather user consent via UMP before initializing the SDK.
        // Required for GDPR / EEA compliance. Must run before MobileAds.Initialize().
#if !UNITY_EDITOR
        yield return GatherConsentRoutine();
        if (!ConsentInformation.CanRequestAds())
        {
            Debug.LogWarning("[AdMob] CanRequestAds=false after UMP — MobileAds not initialized for this session.");
            _initializing = false;
            yield break;
        }
#endif

        bool done = false;
        MobileAds.Initialize(status =>
        {
            _initialized = true;
            done = true;
            if (status != null)
                Debug.Log($"[AdMob] MobileAds.Initialize complete. Adapters={status.getAdapterStatusMap()?.Count ?? 0}");
            else
                Debug.Log("[AdMob] MobileAds.Initialize complete.");
        });

        while (!done)
            yield return null;

        _initializing = false;
        GameplayRefreshService.RegisterAdsProvider(this);
        RequestLoad("init");
        TryAutoShowAfterLoad();
    }

    /// <summary>
    /// Requests UMP consent status update and shows the consent form if required.
    /// Must be called before MobileAds.Initialize() to comply with GDPR/EEA policy.
    /// Performs one automatic retry if the initial update fails (transient network errors
    /// are common on first launch or after a cold boot). No-op on platforms other than Android/iOS.
    /// </summary>
    IEnumerator GatherConsentRoutine()
    {
        const int MaxAttempts = 2;
        const float RetryDelaySec = 3f;

        var request = new ConsentRequestParameters { TagForUnderAgeOfConsent = false };
        bool updateOk = false;

        for (int attempt = 0; attempt < MaxAttempts && !updateOk; attempt++)
        {
            if (attempt > 0)
            {
                Debug.Log($"[AdMob] UMP update retry ({attempt}/{MaxAttempts - 1}) after {RetryDelaySec}s…");
                yield return new WaitForSecondsRealtime(RetryDelaySec);
            }

            bool updateDone = false;
            ConsentInformation.Update(request, updateError =>
            {
                if (updateError != null)
                    Debug.LogWarning($"[AdMob] UMP consent update failed (attempt {attempt + 1})" +
                                     $" ({updateError.ErrorCode}): {updateError.Message}");
                else
                    updateOk = true;
                updateDone = true;
            });

            while (!updateDone) yield return null;
        }

        if (!updateOk)
        {
            // Both attempts failed. Proceed only if previous consent grants ads (cached status).
            Debug.LogWarning("[AdMob] UMP update failed after retries. Proceeding with cached consent status.");
        }

        // Show the form regardless — UMP is a no-op when consent was already given or not required.
        bool formDone = false;
        ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
        {
            if (formError != null)
                Debug.LogWarning($"[AdMob] UMP form error ({formError.ErrorCode}): {formError.Message}");
            formDone = true;
        });

        while (!formDone) yield return null;
        Debug.Log($"[AdMob] UMP complete. CanRequestAds={ConsentInformation.CanRequestAds()}");
    }

    void RequestLoad(string reason)
    {
        if (!_initialized || _loading || _showInFlight)
            return;

        _loading = true;
        string adUnitId = AdMobRuntimeConfig.RewardedAdUnitId;
        DestroyLoadedAd();

        var request = new AdRequest();
        RewardedAd.Load(adUnitId, request, OnAdLoaded);
        Debug.Log($"[AdMob] RewardedAd.Load ({reason}) unit={adUnitId}");
    }

    void OnAdLoaded(RewardedAd ad, LoadAdError error)
    {
        _loading = false;

        if (error != null || ad == null)
        {
            string message = error != null ? error.GetMessage() : "null_ad";
            int code = error != null ? (int)error.GetCode() : -1;
            Debug.LogWarning($"[AdMob] Load failed — code={code} message={message}");
            FirebaseManager.Instance?.LogAdFailed("preload", $"{ReasonLoadFailed}:{message}");
            ScheduleRetry();
            return;
        }

        _nextRetryDelay = InitialRetrySeconds;
        _rewardedAd = ad;
        RegisterAdEvents(ad);
        Debug.Log("[AdMob] Rewarded ad preloaded and ready.");
        TryAutoShowAfterLoad();
    }

    void TryAutoShowAfterLoad()
    {
        if (!_queuedShow.HasValue || _showInFlight)
            return;

        if (_rewardedAd == null || !_rewardedAd.CanShowAd())
            return;

        var q = _queuedShow.Value;
        _queuedShow = null;
        _currentPlacement = q.Placement ?? "";
        Debug.Log($"[AdMob] Show-on-ready — auto-showing placement={_currentPlacement}");
        ExecuteShow(q.Callback);
    }

    void RegisterAdEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log($"[AdMob] Ad opened placement={_currentPlacement}");
            FirebaseManager.Instance?.LogAdStarted(_currentPlacement);
        };

        ad.OnAdFullScreenContentFailed += err =>
        {
            string message = err != null ? err.GetMessage() : "show_failed";
            Debug.LogWarning($"[AdMob] Show failed placement={_currentPlacement} — {message}");
            FirebaseManager.Instance?.LogAdFailed(_currentPlacement, message);
            NotifyUserFailure(ReasonLoadFailed, message);
            CompleteShow(false);
            DestroyLoadedAd();
            RequestLoad("show_failed");
        };

        ad.OnAdFullScreenContentClosed += () =>
        {
            if (_rewardEarnedThisShow)
            {
                Debug.Log($"[AdMob] Ad completed placement={_currentPlacement}");
                FirebaseManager.Instance?.LogAdCompleted(_currentPlacement);
                FirebaseManager.Instance?.LogAdRewarded(_currentPlacement);
                CompleteShow(true);
            }
            else
            {
                Debug.Log($"[AdMob] Ad closed without reward placement={_currentPlacement}");
                FirebaseManager.Instance?.LogAdFailed(_currentPlacement, "closed_without_reward");
                CompleteShow(false);
            }

            DestroyLoadedAd();
            RequestLoad("post_show");
        };
    }

    public void ShowRewardedAd(string placementId, Action<bool> onComplete)
    {
        string placement = placementId ?? "";
        Debug.Log($"[AdMob] Show requested placement={placement}");

        if (_showInFlight)
        {
            FailShowImmediate(placement, ReasonAlreadyShowing, onComplete);
            return;
        }

        if (!_initialized)
        {
            if (_initializing)
            {
                QueueShow(placement, onComplete, waitingForInit: true);
                Debug.Log($"[AdMob] Show queued reason={ReasonNotInitialized} placement={placement} (init in progress)");
                return;
            }

            FailShowImmediate(placement, ReasonNotInitialized, onComplete);
            return;
        }

        if (_rewardedAd == null || !_rewardedAd.CanShowAd())
        {
            // Fail immediately so the player gets feedback right away instead of a silent wait.
            // Trigger a background reload so the next attempt is more likely to succeed.
            FailShowImmediate(placement, ReasonNotReady, onComplete);
            if (!_loading) RequestLoad("show_not_ready");
            return;
        }

        _currentPlacement = placement;
        ExecuteShow(onComplete);
    }

    void QueueShow(string placement, Action<bool> onComplete, bool waitingForInit)
    {
        _queuedShow = new PendingShowRequest
        {
            Placement = placement,
            Callback = onComplete,
            RequestedAt = Time.unscaledTime,
            WaitingForInit = waitingForInit,
        };
    }

    void ExecuteShow(Action<bool> onComplete)
    {
        _showInFlight = true;
        _rewardEarnedThisShow = false;
        _pendingCallback = onComplete;

        Debug.Log($"[AdMob] Executing show placement={_currentPlacement}");
        _rewardedAd.Show(_ =>
        {
            _rewardEarnedThisShow = true;
        });
    }

    void FailShowImmediate(string placement, string reason, Action<bool> onComplete)
    {
        _currentPlacement = placement;
        Debug.LogWarning($"[AdMob] Show failed reason={reason} placement={placement}");
        FirebaseManager.Instance?.LogAdFailed(placement, reason);
        NotifyUserFailure(reason, null);
        onComplete?.Invoke(false);
    }

    void FailQueuedShow(string reason, string logDetail)
    {
        if (!_queuedShow.HasValue) return;

        var q = _queuedShow.Value;
        _queuedShow = null;
        _currentPlacement = q.Placement ?? "";
        Debug.LogWarning($"[AdMob] Queued show failed reason={reason} detail={logDetail} placement={_currentPlacement}");
        FirebaseManager.Instance?.LogAdFailed(_currentPlacement, $"{reason}:{logDetail}");
        NotifyUserFailure(reason, logDetail);
        q.Callback?.Invoke(false);
    }

    void NotifyUserFailure(string reason, string detail)
    {
        // AdAlreadyShowing is the only reason that needs a different message.
        // All other failures (not ready, not initialized, load failed, no inventory)
        // show the unified "no ads available" message.
        string msg = reason == ReasonAlreadyShowing
            ? Loc.Get(LocKeys.AdAlreadyShowing)
            : Loc.Get(LocKeys.AdUnavailable);

        Debug.Log($"[AdMob] User feedback reason={reason} detail={detail ?? "none"} message={msg}");
        AdRewardUI.ShowMessage(msg);
    }

    void CompleteShow(bool success)
    {
        _showInFlight = false;
        var cb = _pendingCallback;
        _pendingCallback = null;
        cb?.Invoke(success);
    }

    void ScheduleRetry()
    {
        float delay = _nextRetryDelay;
        _retryAt = Time.unscaledTime + delay;
        _nextRetryDelay = Mathf.Min(_nextRetryDelay * 2f, MaxRetrySeconds);
        Debug.Log($"[AdMob] Retry scheduled in {delay:0}s");
    }

    void DestroyLoadedAd()
    {
        if (_rewardedAd == null) return;
        try { _rewardedAd.Destroy(); }
        catch (Exception ex) { Debug.LogWarning("[AdMob] Destroy ad — " + ex.Message); }
        _rewardedAd = null;
    }
}
