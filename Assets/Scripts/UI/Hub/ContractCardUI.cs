using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays a single contract card (active or history).
/// </summary>
public class ContractCardUI : MonoBehaviour
{
    public ContractConfig contract;
    public bool isHistoryMode;

    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI rewardText;
    public TextMeshProUGUI progressText;
    public Slider          progressBar;
    public Button          claimButton;
    public GameObject      completedBadge;

    private ContractSystem    _contracts;
    private StudioManager     _studio;
    private StudioLevelSystem _studioLevel;
    private SmoothProgressBar _smoothBar;
    private bool              _eventsBound;
    private bool              _buttonsWired;

    private void Awake()
    {
        GameHub.OnGameReady += Bind;
        WireButtons();
        if (progressBar != null)
            _smoothBar = progressBar.gameObject.AddComponent<SmoothProgressBar>();
    }

    private void OnEnable()
    {
        if (GameHub.Instance != null)
            Bind();
    }

    private void Start()
    {
        if (GameHub.Instance != null)
            Bind();
    }

    private void OnDestroy()
    {
        GameHub.OnGameReady -= Bind;
        UnbindContractEvents();
        if (claimButton != null)
            claimButton.onClick.RemoveListener(OnClaimClicked);
    }

    private void OnContractsUpdated() => RefreshUI();
    private void OnContractDone(ContractConfig _) => RefreshUI();

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
        if (_buttonsWired || claimButton == null) return;
        claimButton.onClick.RemoveListener(OnClaimClicked);
        claimButton.onClick.AddListener(OnClaimClicked);
        _buttonsWired = true;
    }

    private void BindContractEvents()
    {
        if (_contracts == null || _eventsBound) return;
        _contracts.OnContractUpdated   += OnContractsUpdated;
        _contracts.OnContractCompleted += OnContractDone;
        _eventsBound = true;
    }

    private void UnbindContractEvents()
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

        float progress = _contracts.GetProgress(contract);
        bool  ready    = _contracts.IsReadyToClaim(contract);

        if (descText != null) descText.text = contract.description;
        if (rewardText != null) rewardText.text = BuildRewardString();
        if (progressText != null) progressText.text = $"{progress:0}/{contract.goalAmount:0}";

        if (_smoothBar != null)
            _smoothBar.SetTarget(progress, contract.goalAmount);
        else if (progressBar != null)
        {
            progressBar.maxValue = contract.goalAmount;
            progressBar.value = progress;
        }

        if (completedBadge != null) completedBadge.SetActive(ready);

        if (claimButton != null)
        {
            claimButton.gameObject.SetActive(ready);
            claimButton.interactable = ready;
        }
    }

    private string BuildRewardString()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (contract.rewardMoney > 0) parts.Add(AnimatedMoneyText.FormatMoney(contract.rewardMoney));
        if (contract.rewardDiamonds > 0) parts.Add($"[D]{contract.rewardDiamonds}");
        if (contract.rewardReputation > 0) parts.Add($"+{contract.rewardReputation:0} REP");
        if (contract.rewardStudioXP > 0) parts.Add($"+{contract.rewardStudioXP} XP");
        return string.Join("  ", parts);
    }

    private void OnClaimClicked()
    {
        if (contract == null || _contracts == null) return;
        _contracts.ClaimReward(contract, _studio, _studioLevel);
    }
}
