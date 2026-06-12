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
    public GameObject      completedBadge;

    ContractSystem    _contracts;
    StudioManager     _studio;
    StudioLevelSystem _studioLevel;
    SmoothProgressBar _smoothBar;
    bool              _eventsBound;

    void Awake()
    {
        GameHub.OnGameReady += Bind;
        WireButtons();
        if (progressBar != null)
            _smoothBar = progressBar.gameObject.AddComponent<SmoothProgressBar>();
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
            titleText.text = contract.contractTitle;

        if (isHistoryMode) return;

        if (_contracts == null)
        {
            if (GameHub.Instance != null)
                Bind();
            if (_contracts == null) return;
        }

        if (descText != null) descText.text = contract.description;
        if (objectiveText != null)
            objectiveText.text = Loc.Format(LocKeys.ContractActiveObjective, ContractSystem.BuildObjectiveLabel(contract));
        if (rewardText != null) rewardText.text = BuildRewardString();

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
        if (contract.rewardMoney > 0) parts.Add(AnimatedMoneyText.FormatMoney(contract.rewardMoney));
        if (contract.rewardDiamonds > 0) parts.Add($"[D]{contract.rewardDiamonds}");
        if (contract.rewardReputation > 0) parts.Add($"+{contract.rewardReputation:0} REP");
        if (contract.rewardStudioXP > 0) parts.Add($"+{contract.rewardStudioXP} XP");
        return string.Join("  ", parts);
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
}
