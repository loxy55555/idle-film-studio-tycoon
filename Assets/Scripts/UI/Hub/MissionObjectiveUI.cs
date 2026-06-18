using TMPro;
using UnityEngine;

/// <summary>
/// Shows a summary of the first active contract in the header MissionCard.
/// Auto-attaches to MissionCard at runtime (no StudioUIBuilder changes).
/// </summary>
public class MissionObjectiveUI : MonoBehaviour
{
    TextMeshProUGUI _title;
    TextMeshProUGUI _desc;
    TextMeshProUGUI _reward;

    ContractSystem _contracts;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (!Application.isPlaying) return;
        if (FindAnyObjectByType<MissionObjectiveUI>() != null) return;

        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name != "MissionCard") continue;
            if (t.GetComponent<MissionObjectiveUI>() != null) return;
            t.gameObject.AddComponent<MissionObjectiveUI>();
            return;
        }
    }

    void Awake()
    {
        AutoWire();
        GameHubReadyGate.SubscribeOrInvokeNow(Bind);
    }

    void OnEnable()
    {
        GameHubReadyGate.RefreshIfReady(Bind);
    }

    void OnDestroy()
    {
        GameHub.OnGameReady -= Bind;
        Unbind();
    }

    void AutoWire()
    {
        _title  = FindTmp("MissionTitle");
        _desc   = FindTmp("MissionDesc");
        _reward = FindTmp("MissionReward");
    }

    TextMeshProUGUI FindTmp(string name)
    {
        foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp.name == name)
                return tmp;
        }
        return null;
    }

    void Bind()
    {
        Unbind();
        _contracts = GameHub.Instance?.contracts;
        if (_contracts != null)
        {
            _contracts.OnContractUpdated   += Refresh;
            _contracts.OnContractCompleted += OnContractEvent;
            _contracts.OnContractClaimed   += OnContractEvent;
        }
        Refresh();
    }

    void Unbind()
    {
        if (_contracts == null) return;
        _contracts.OnContractUpdated   -= Refresh;
        _contracts.OnContractCompleted -= OnContractEvent;
        _contracts.OnContractClaimed   -= OnContractEvent;
    }

    void OnContractEvent(ContractConfig _) => Refresh();
    public void RefreshLocalization() => Refresh();

    void Refresh()
    {
        if (_contracts == null)
        {
            if (GameHub.Instance != null) Bind();
            if (_contracts == null) return;
        }

        ContractConfig target = null;
        foreach (var c in _contracts.ActiveContracts)
        {
            if (c == null) continue;
            if (_contracts.IsReadyToClaim(c))
            {
                target = c;
                break;
            }
        }
        if (target == null)
        {
            var active = _contracts.ActiveContracts;
            if (active.Count > 0)
                target = active[0];
        }

        if (target == null)
        {
            if (_contracts.IsSelectionMode)
                SetTexts(Loc.Get(LocKeys.ContractObjective), Loc.Get(LocKeys.ContractChoosePrompt), "");
            else
                SetTexts(Loc.Get(LocKeys.ContractObjective), Loc.Get(LocKeys.ContractNoActive), "");
            return;
        }

        float progress = _contracts.GetProgress(target);
        bool ready = _contracts.IsReadyToClaim(target);
        string progressLine = ready
            ? Loc.Get(LocKeys.ContractReadyClaim)
            : $"{progress:0}/{target.goalAmount:0}";

        var locTitle = Loc.Get(LocKeys.ContractTitlePfx + target.id);
        if (locTitle == LocKeys.ContractTitlePfx + target.id) locTitle = target.contractTitle;
        SetTexts(
            ready ? Loc.Get(LocKeys.ContractClaim) : Loc.Get(LocKeys.ContractObjective),
            $"{locTitle}\n{progressLine}",
            BuildRewardLine(target));
    }

    static string BuildRewardLine(ContractConfig c)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (c.rewardMoney > 0)      parts.Add(Loc.Format(LocKeys.ContractRewardMoney, AnimatedMoneyText.FormatMoney(c.rewardMoney)));
        if (c.rewardReputation > 0) parts.Add(Loc.Format(LocKeys.ContractRewardRep,   c.rewardReputation));
        if (c.rewardDiamonds > 0)   parts.Add(Loc.Format(LocKeys.ContractRewardDiam,  c.rewardDiamonds));
        if (c.rewardStudioXP > 0)   parts.Add(Loc.Format(LocKeys.ContractRewardXP,    c.rewardStudioXP));
        return string.Join("  ", parts);
    }

    void SetTexts(string title, string desc, string reward)
    {
        if (_title != null) _title.text = title;
        if (_desc != null) _desc.text = desc;
        if (_reward != null) _reward.text = reward;
    }
}
