using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays a single contract card (active, candidate, or history).
/// </summary>
public class ContractCardUI : MonoBehaviour
{
    public ContractConfig contract;
    public bool isHistoryMode;
    public bool isCandidateMode;

    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI objectiveText;
    public TextMeshProUGUI rewardText;
    public TextMeshProUGUI progressText;
    public Slider          progressBar;
    public Button          claimButton;
    public Button          selectButton;
    public Button          rerollDiamondsButton;   // Phase 13.4D: reroll via 5 diamonds
    public Button          cancelAdButton;          // FASE 16.1 D3: cancel active contract via ad
    public GameObject      completedBadge;

    const int RerollDiamondCost = 5;

    ContractSystem    _contracts;
    StudioManager     _studio;
    StudioLevelSystem _studioLevel;
    SmoothProgressBar _smoothBar;
    bool              _eventsBound;
    bool              _wasReadyToClaim;
    bool              _rewardRevealPlayed;

    void Awake()
    {
        GameHub.OnGameReady += Bind;
        WireButtons();
        if (progressBar != null)
        {
            _smoothBar = progressBar.gameObject.AddComponent<SmoothProgressBar>();
            // Override baked green fill with premium palette
            if (progressBar.fillRect != null)
            {
                var fillImg = progressBar.fillRect.GetComponent<Image>();
                if (fillImg != null) fillImg.color = CinematicTheme.ProgressFill;
            }
        }
    }

    void OnEnable()
    {
        if (GameHub.Instance != null)
            Bind();
    }

    void Start()
    {
        if (GameHub.Instance != null)
            Bind();
    }

    void OnDestroy()
    {
        GameHub.OnGameReady -= Bind;
        UnbindContractEvents();
        if (claimButton != null)
            claimButton.onClick.RemoveListener(OnClaimClicked);
        if (selectButton != null)
            selectButton.onClick.RemoveListener(OnSelectClicked);
        if (cancelAdButton != null)
            cancelAdButton.onClick.RemoveListener(OnCancelAdClicked);
    }

    void OnContractsUpdated() => RefreshUI();
    void OnContractDone(ContractConfig _) => RefreshUI();

    public void Bind()
    {
        var hub = GameHub.Instance;
        if (hub == null) return;

        _contracts   = hub.contracts;
        _studio      = hub.studio;
        _studioLevel = hub.studioLevel;

        UnbindContractEvents();
        if (_contracts != null && !isHistoryMode)
            BindContractEvents();

        WireButtons();
        RefreshUI();
    }

    void WireButtons()
    {
        // Always rewire — buttons are assigned by the factory after Awake(), so the
        // first call (from Awake) finds nulls and the guard must NOT block later calls.
        if (claimButton != null)
        {
            claimButton.onClick.RemoveListener(OnClaimClicked);
            claimButton.onClick.AddListener(OnClaimClicked);
        }
        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(OnSelectClicked);
            selectButton.onClick.AddListener(OnSelectClicked);
        }
        if (rerollDiamondsButton != null)
        {
            rerollDiamondsButton.onClick.RemoveListener(OnRerollDiamondsClicked);
            rerollDiamondsButton.onClick.AddListener(OnRerollDiamondsClicked);
        }
        if (cancelAdButton != null)
        {
            cancelAdButton.onClick.RemoveListener(OnCancelAdClicked);
            cancelAdButton.onClick.AddListener(OnCancelAdClicked);
        }
    }

    void BindContractEvents()
    {
        if (_contracts == null || _eventsBound) return;
        _contracts.OnContractUpdated   += OnContractsUpdated;
        _contracts.OnContractCompleted += OnContractDone;
        _eventsBound = true;
    }

    void UnbindContractEvents()
    {
        if (_contracts == null || !_eventsBound) return;
        _contracts.OnContractUpdated   -= OnContractsUpdated;
        _contracts.OnContractCompleted -= OnContractDone;
        _eventsBound = false;
    }

    public void RefreshUI()
    {
        if (contract == null) return;

        if (titleText != null)
            titleText.text = isCandidateMode || isHistoryMode ? contract.contractTitle : Loc.Get(LocKeys.ContractActive);

        if (isHistoryMode) return;

        if (_contracts == null)
        {
            if (GameHub.Instance != null)
                Bind();
            if (_contracts == null) return;
        }

        if (descText != null)
        {
            descText.text = contract.description;
            descText.gameObject.SetActive(isCandidateMode);
        }
        if (objectiveText != null)
            objectiveText.text = Loc.Format(LocKeys.ContractActiveObjective, ContractSystem.BuildObjectiveLabel(contract));
        if (rewardText != null)
        {
            rewardText.text = BuildRewardString();
            rewardText.gameObject.SetActive(isCandidateMode);
            if (isCandidateMode && !_rewardRevealPlayed)
            {
                _rewardRevealPlayed = true;
                UIAnimationService.PlayContractRewardReveal(rewardText.rectTransform);
            }
        }

        if (isCandidateMode)
        {
            if (progressText != null) progressText.gameObject.SetActive(false);
            if (progressBar != null) progressBar.gameObject.SetActive(false);
            if (claimButton != null) claimButton.gameObject.SetActive(false);
            if (selectButton != null)
            {
                selectButton.gameObject.SetActive(true);
                selectButton.interactable = _contracts.IsCandidate(contract);
            }
            return;
        }

        float progress = _contracts.GetProgress(contract);
        bool  ready    = _contracts.IsReadyToClaim(contract);

        if (progressText != null)
        {
            progressText.gameObject.SetActive(true);
            progressText.text = $"{progress:0}/{contract.goalAmount:0}";
        }

        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(true);
            if (_smoothBar != null)
                _smoothBar.SetTarget(progress, contract.goalAmount);
            else
            {
                progressBar.maxValue = contract.goalAmount;
                progressBar.value = progress;
            }
        }

        if (completedBadge != null) completedBadge.SetActive(ready);

        if (ready && !_wasReadyToClaim)
            UIAnimationService.PlayContractCompleteHighlight(transform as RectTransform);
        _wasReadyToClaim = ready;

        if (claimButton != null)
        {
            claimButton.gameObject.SetActive(ready);
            claimButton.interactable = ready;
        }

        if (selectButton != null)
            selectButton.gameObject.SetActive(false);
    }

    string BuildRewardString()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (contract.rewardMoney > 0)      parts.Add(Loc.Format(LocKeys.ContractRewardMoney, AnimatedMoneyText.FormatMoney(contract.rewardMoney)));
        if (contract.rewardDiamonds > 0)   parts.Add(Loc.Format(LocKeys.ContractRewardDiam, contract.rewardDiamonds));
        if (contract.rewardReputation > 0) parts.Add(Loc.Format(LocKeys.ContractRewardRep, contract.rewardReputation));
        if (contract.rewardStudioXP > 0)   parts.Add(Loc.Format(LocKeys.ContractRewardXP, contract.rewardStudioXP));
        return parts.Count > 0 ? string.Join("  ", parts) : "—";
    }

    static void NavigateToStore()
    {
        // Navigate to the store tab by clicking its BottomNav button
        var nav = GameObject.Find("BottomNav");
        if (nav == null) return;
        foreach (var btn in nav.GetComponentsInChildren<UnityEngine.UI.Button>(true))
        {
            if (btn.name.Contains("Shop") || btn.name.Contains("Tienda") || btn.name.Contains("Store"))
            {
                btn.onClick.Invoke();
                return;
            }
        }
    }

    void OnClaimClicked()
    {
        if (contract == null || _contracts == null) return;
        _contracts.ClaimReward(contract, _studio, _studioLevel);
    }

    void OnSelectClicked()
    {
        if (contract == null || _contracts == null) return;
        _contracts.SelectCandidate(contract);
    }

    void OnRerollDiamondsClicked()
    {
        if (contract == null || _contracts == null) return;

        var diamonds = GameHub.Instance?.diamonds;
        if (diamonds == null) return;

        if (diamonds.Balance < RerollDiamondCost)
        {
            InsufficientDiamondsDialog.Show(NavigateToStore);
            return;
        }

        if (!diamonds.TrySpend(RerollDiamondCost))
            return;

        int level = _studioLevel?.Level ?? 1;
        bool ok = _contracts.RerollCandidate(contract, level);
        if (!ok)
        {
            diamonds.Add(RerollDiamondCost);
            Debug.Log("[ContractCardUI] Reroll refunded: no alternatives available.");
        }
    }

    // ── FASE 16.1 — D3: Cancel active contract via ad ─────────────────────────

    void OnCancelAdClicked()
    {
        if (_contracts == null || !_contracts.HasActiveContract) return;

        bool noAds = PremiumFeatures.Instance?.NoAdsPurchased ?? false;
        int level  = _studioLevel?.Level ?? 1;

        if (noAds)
        {
            ExecuteContractCancel(level);
        }
        else
        {
            GameplayRefreshService.TryShowAd(AdRewardSystem.PlacementCancelContract, ok =>
            {
                if (!ok) return;
                ExecuteContractCancel(level);
            });
        }
    }

    void ExecuteContractCancel(int studioLevel)
    {
        bool ok = _contracts?.CancelActiveContract(studioLevel) ?? false;
        if (ok)
            AdRewardUI.ShowMessage(Loc.Get(LocKeys.ContractCancelDone));
    }
}
