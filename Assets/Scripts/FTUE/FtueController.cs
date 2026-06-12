using System.Collections;
using UnityEngine;

/// <summary>First-time user experience orchestrator (Phase 8.4).</summary>
public class FtueController : MonoBehaviour
{
    public static FtueController Instance { get; private set; }

    const float TargetResolveTimeoutSeconds = 12f;
    const float TargetResolveRetryInterval  = 0.35f;

    FtueOverlayView _overlay;
    StudioHubUI     _mainNav;
    StudioManager   _studio;
    RectTransform   _highlightTarget;
    FtueStep        _step;
    bool            _bound;
    bool            _loadedCompleted;
    Coroutine       _targetWaitRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void OnEnable() => GameHub.OnGameReady += OnGameReady;

    void OnDestroy()
    {
        GameHub.OnGameReady -= OnGameReady;
        StopTargetWait();
        UnbindEvents();
        if (Instance == this) Instance = null;
    }

    void OnGameReady()
    {
        if (_bound) return;
        _bound = true;

        _studio = GameHub.Instance?.studio;
        _mainNav = FindAnyObjectByType<DefinitiveHudShell>()?.mainNavigation
                ?? FindAnyObjectByType<StudioHubUI>();

        _loadedCompleted = FtueState.Completed;
        FtueLog.State("OnGameReady");

        if (FtueState.Disabled)
        {
            FtueLog.Info("Disabled via debug flag — skipping FTUE.");
            HideOverlayImmediate();
            return;
        }

        if (FtueState.Completed)
        {
            FtueLog.Info("Already completed — skipping FTUE.");
            HideOverlayImmediate();
            return;
        }

        if (ShouldAutoComplete())
        {
            FtueLog.Info("Auto-completing FTUE (profile has completed movies).");
            CompleteFtue(save: true);
            return;
        }

        EnsureOverlay();
        BindEvents();
        ResumeStep(FtueState.Step);
    }

    bool ShouldAutoComplete()
    {
        if (_studio == null) return false;
        return _studio.CompletedMovieKeys.Count > 0;
    }

    void EnsureOverlay()
    {
        if (_overlay != null) return;
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            FtueLog.Warn("No Canvas found — cannot create FTUE overlay.");
            return;
        }
        _overlay = FtueOverlayView.Create(canvas.transform);
        FtueLog.Info("Overlay created.");
    }

    void HideOverlayImmediate()
    {
        ClearHighlight();
        if (_overlay != null)
            _overlay.Hide();
    }

    void BindEvents()
    {
        if (_studio == null) return;
        _studio.OnProductionsChanged += OnProductionsChanged;
        _studio.OnMovieCompleted += OnMovieCompleted;
        PremiereSequenceController.OnPremiereDismissed += OnPremiereDismissed;
    }

    void UnbindEvents()
    {
        if (_studio == null) return;
        _studio.OnProductionsChanged -= OnProductionsChanged;
        _studio.OnMovieCompleted -= OnMovieCompleted;
        PremiereSequenceController.OnPremiereDismissed -= OnPremiereDismissed;
    }

    void ResumeStep(FtueStep step)
    {
        _step = step;
        FtueLog.Info($"ResumeStep → {_step}");

        if (step == FtueStep.Done)
        {
            CompleteFtue(save: !_loadedCompleted);
            return;
        }

        if (step == FtueStep.FirstMovieComplete)
            _step = FtueStep.StudioDuringProduction;

        if (_studio != null && _studio.ActiveProductionCount > 0 && step <= FtueStep.ProductionGuide)
        {
            EnterStudioDuringProduction();
            return;
        }

        switch (_step)
        {
            case FtueStep.Welcome:
                ShowWelcome();
                break;
            case FtueStep.ProductionGuide:
                EnterProductionGuide();
                break;
            case FtueStep.StudioDuringProduction:
                EnterStudioDuringProduction();
                break;
        }
    }

    void ShowWelcome()
    {
        ClearHighlight();
        StopTargetWait();
        _overlay?.ShowMessage(null, LocKeys.FtueWelcomeBody, OnWelcomeContinue, SkipAll);
    }

    void OnWelcomeContinue()
    {
        AdvanceTo(FtueStep.ProductionGuide);
        EnterProductionGuide();
    }

    void EnterProductionGuide()
    {
        StopTargetWait();
        _mainNav?.ShowTab((int)MainHudTab.Production, instant: true);
        HighlightMainTab(MainHudTab.Production);
        _overlay?.ShowMessage(LocKeys.FtueProductionTitle, LocKeys.FtueProductionBody, OnProductionGuideContinue, SkipAll);
    }

    void OnProductionGuideContinue()
    {
        _overlay?.EnterHighlightPassThrough(SkipAll);
        BeginWaitForFirstOffer();
    }

    void BeginWaitForFirstOffer()
    {
        StopTargetWait();
        _targetWaitRoutine = StartCoroutine(WaitForUiTarget(
            TryHighlightFirstOffer,
            onResolved: null,
            onTimeout: OnFirstOfferTargetTimeout,
            context: "first production offer"));
    }

    void OnFirstOfferTargetTimeout()
    {
        FtueLog.Warn("First offer target not found — showing recovery message.");
        ClearHighlight();
        _overlay?.ShowMessage(
            LocKeys.FtueProductionTitle,
            LocKeys.FtueProductionBody,
            OnProductionGuideTimeoutContinue,
            SkipAll);
    }

    void OnProductionGuideTimeoutContinue()
    {
        FtueLog.Warn("Continuing without highlight — waiting for first production.");
        _overlay?.EnterHighlightPassThrough(SkipAll);
    }

    void EnterStudioDuringProduction()
    {
        StopTargetWait();
        _step = FtueStep.StudioDuringProduction;
        PersistStep();
        _mainNav?.ShowTab((int)MainHudTab.Studio, instant: true);
        HighlightMainTab(MainHudTab.Studio);
        _overlay?.ShowMessage(LocKeys.FtueStudioTitle, LocKeys.FtueStudioBody, OnStudioGuideContinue, SkipAll);
    }

    void OnStudioGuideContinue()
    {
        StopTargetWait();
        ClearHighlight();
        _overlay?.Hide();
    }

    void OnProductionsChanged()
    {
        if (FtueState.Completed || _studio == null) return;
        if (_studio.ActiveProductionCount <= 0) return;
        if (_step >= FtueStep.StudioDuringProduction) return;

        FtueLog.Info("Production started — advancing to studio guide.");
        StopTargetWait();
        EnterStudioDuringProduction();
    }

    void OnMovieCompleted(MovieCompletePayload payload)
    {
        if (FtueState.Completed || _studio == null) return;
        if (_studio.CompletedMovieKeys.Count != 1) return;

        FtueLog.Info("First movie completed — waiting for premiere dismiss.");
        StopTargetWait();
        _step = FtueStep.FirstMovieComplete;
        PersistStep();
        ClearHighlight();
        _overlay?.Hide();
    }

    void OnPremiereDismissed()
    {
        if (FtueState.Completed) return;
        if (_step == FtueStep.FirstMovieComplete)
            CompleteFtue(save: true);
    }

    void AdvanceTo(FtueStep step)
    {
        _step = step;
        FtueState.Step = step;
        PersistStep();
        FtueLog.Info($"Advanced to {_step}");
    }

    void PersistStep()
    {
        FtueState.Step = _step;
        GameHub.Instance?.save?.Save();
        FtueLog.Info($"Persisted step={_step} completed={FtueState.Completed}");
    }

    void SkipAll()
    {
        FtueLog.Info("Skip requested.");
        CompleteFtue(save: true);
    }

    void CompleteFtue(bool save)
    {
        StopTargetWait();
        FtueState.MarkCompleted();
        _step = FtueStep.Done;
        ClearHighlight();
        _overlay?.Hide();
        UnbindEvents();
        FtueLog.Info($"CompleteFtue save={save}");
        if (save) GameHub.Instance?.save?.Save();
    }

    bool TryHighlightMainTab(MainHudTab tab)
    {
        if (_mainNav?.tabButtons == null)
        {
            FtueLog.Warn($"Main nav tab buttons missing for {tab}.");
            return false;
        }

        int idx = (int)tab;
        if (idx < 0 || idx >= _mainNav.tabButtons.Length)
        {
            FtueLog.Warn($"Tab index out of range: {tab} ({idx}).");
            return false;
        }

        var btn = _mainNav.tabButtons[idx];
        if (btn == null)
        {
            FtueLog.Warn($"Tab button null for {tab}.");
            return false;
        }

        _highlightTarget = btn.transform as RectTransform;
        ActivateOverlayIfNeeded();
        _overlay?.HighlightRectTransform(_highlightTarget);
        return _highlightTarget != null;
    }

    void HighlightMainTab(MainHudTab tab)
    {
        if (!TryHighlightMainTab(tab))
            FtueLog.Warn($"HighlightMainTab failed for {tab}.");
    }

    bool TryHighlightFirstOffer()
    {
        var tab = FindAnyObjectByType<MovieTabUI>(FindObjectsInactive.Include);
        if (tab == null)
        {
            FtueLog.Warn("MovieTabUI not found while resolving first offer.");
            return false;
        }

        if (tab.slotsRow == null)
        {
            FtueLog.Warn("MovieTabUI.slotsRow is null.");
            return false;
        }

        if (tab.slotsRow.childCount == 0)
        {
            FtueLog.Warn("MovieTabUI slots not built yet.");
            return false;
        }

        for (int i = 0; i < tab.slotsRow.childCount; i++)
        {
            var child = tab.slotsRow.GetChild(i) as RectTransform;
            if (child == null) continue;
            if (child.GetComponent<MovieButtonUI>() == null) continue;

            _highlightTarget = child;
            ActivateOverlayIfNeeded();
            _overlay?.HighlightRectTransform(_highlightTarget);
            FtueLog.Info($"Highlighting offer slot {i}.");
            return true;
        }

        FtueLog.Warn("No MovieButtonUI found in offer slots.");
        return false;
    }

    void ActivateOverlayIfNeeded()
    {
        if (_overlay != null && !_overlay.gameObject.activeSelf)
            _overlay.gameObject.SetActive(true);
    }

    IEnumerator WaitForUiTarget(System.Func<bool> tryResolve, System.Action onResolved, System.Action onTimeout, string context)
    {
        float elapsed = 0f;
        FtueLog.Info($"Waiting for UI target: {context}");

        while (elapsed < TargetResolveTimeoutSeconds)
        {
            if (FtueState.Completed || FtueState.Disabled)
                yield break;

            if (tryResolve())
            {
                FtueLog.Info($"Resolved UI target: {context}");
                onResolved?.Invoke();
                yield break;
            }

            yield return new WaitForSecondsRealtime(TargetResolveRetryInterval);
            elapsed += TargetResolveRetryInterval;
        }

        FtueLog.Warn($"Timeout waiting for UI target: {context} ({TargetResolveTimeoutSeconds:0}s)");
        onTimeout?.Invoke();
    }

    void StopTargetWait()
    {
        if (_targetWaitRoutine == null) return;
        StopCoroutine(_targetWaitRoutine);
        _targetWaitRoutine = null;
    }

    void ClearHighlight()
    {
        _highlightTarget = null;
        _overlay?.ClearHighlight();
    }

    void Update()
    {
        if (_highlightTarget != null && _overlay != null)
            _overlay.RefreshHighlightPosition(_highlightTarget);
    }

    // ─── Debug API ───────────────────────────────────────────────────────────

    public void DebugCompleteFtue()
    {
        FtueLog.Info("DebugCompleteFtue invoked.");
        if (!_bound)
        {
            _studio = GameHub.Instance?.studio;
            EnsureOverlay();
        }
        CompleteFtue(save: true);
    }

    public void DebugResetFtue()
    {
        FtueLog.Info("DebugResetFtue invoked.");
        StopTargetWait();
        UnbindEvents();
        FtueState.Reset();
        FtueState.Disabled = false;
        _step = FtueStep.Welcome;
        _loadedCompleted = false;
        _bound = false;
        ClearHighlight();
        _overlay?.Hide();
        GameHub.Instance?.save?.Save();
        _bound = true;
        EnsureOverlay();
        BindEvents();
        ResumeStep(FtueStep.Welcome);
    }

    public void DebugDisableFtue()
    {
        FtueLog.Info("DebugDisableFtue invoked.");
        FtueState.Disabled = true;
        StopTargetWait();
        CompleteFtue(save: false);
    }

    public void DebugEnableFtue()
    {
        FtueLog.Info("DebugEnableFtue invoked.");
        FtueState.Disabled = false;
    }
}
