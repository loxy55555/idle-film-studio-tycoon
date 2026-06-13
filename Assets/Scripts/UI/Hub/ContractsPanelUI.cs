using System.Collections.Generic;

using UnityEngine;using UnityEngine.UI;

using TMPro;



/// <summary>

/// Contract panel — 3 candidates to choose OR 1 active contract card (Phase 8.0).

/// </summary>

public class ContractsPanelUI : MonoBehaviour

{

    public const int VisibleSlotCount = 3;

    public const float FixedSlotHeightPublic = 200f; // Phase 8.5C: taller contract cards (was 164)

    const float FixedSlotHeight = FixedSlotHeightPublic;

    public const float ActiveCardHeight = 96f;



    [Header("Containers")]

    public RectTransform activeContent;

    public TextMeshProUGUI activeEmptyLabel;

    public TextMeshProUGUI historyEmptyLabel;

    public RectTransform historyContent;



    ContractSystem _contracts;

    ScrollRect _activeScroll;

    bool _eventsSubscribed;

    readonly List<ContractCardUI> _activeCards = new();

    readonly List<GameObject> _slotRoots = new();

    GameObject _refreshButtonRoot;



    public bool IsBound => _contracts != null && _eventsSubscribed;



    void Awake()

    {

        AutoWireReferences();

        HideHistorySection();

        GameHub.OnGameReady += TryBind;

    }



    void Start() => TryBind();



    void Update()

    {

        if (!_eventsSubscribed)

            TryBind();

    }



    void OnEnable() => TryBind();



    void OnDestroy()

    {

        GameHub.OnGameReady -= TryBind;

        UnbindEvents();

    }



    void AutoWireReferences()

    {

        if (activeContent == null)

        {

            var scroll = transform.Find("ActiveScroll");

            if (scroll != null)

            {

                _activeScroll = scroll.GetComponent<ScrollRect>();

                activeContent = _activeScroll != null ? _activeScroll.content : null;

            }

        }



        if (activeEmptyLabel == null)

            activeEmptyLabel = FindChildText("ActiveEmpty");

        if (historyEmptyLabel == null)

            historyEmptyLabel = FindChildText("HistEmpty");



        if (historyContent == null)

        {

            var hist = transform.Find("HistoryScroll");

            if (hist != null)

            {

                var sr = hist.GetComponent<ScrollRect>();

                if (sr != null) historyContent = sr.content;

            }

        }

    }



    static TextMeshProUGUI FindChildText(Transform root, string name)

    {

        foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))

        {

            if (tmp.name == name) return tmp;

        }

        return null;

    }



    TextMeshProUGUI FindChildText(string name) => FindChildText(transform, name);



    void TryBind()

    {

        var contracts = GameHub.Instance?.contracts;

        if (contracts == null) return;



        if (_eventsSubscribed && _contracts == contracts)

        {

            Rebuild();

            return;

        }



        UnbindEvents();

        _contracts = contracts;

        _contracts.OnContractUpdated   += Rebuild;

        _contracts.OnContractCompleted += OnContractEvent;

        _contracts.OnContractClaimed   += OnContractEvent;

        _contracts.OnContractSelected  += OnContractEvent;

        _eventsSubscribed = true;

        Rebuild();

    }



    void UnbindEvents()

    {

        if (!_eventsSubscribed || _contracts == null) return;

        _contracts.OnContractUpdated   -= Rebuild;

        _contracts.OnContractCompleted -= OnContractEvent;

        _contracts.OnContractClaimed   -= OnContractEvent;

        _contracts.OnContractSelected  -= OnContractEvent;

        _eventsSubscribed = false;

    }



    void OnContractEvent(ContractConfig _) => Rebuild();



    public void Rebuild()

    {

        AutoWireReferences();

        if (_contracts == null)

            TryBind();

        if (_contracts == null || activeContent == null) return;



        ClearSlots();



        if (_contracts.HasActiveContract)

            BuildActiveView();

        else

            BuildSelectionView();



        if (activeEmptyLabel != null)

            CollapseLayoutBranch(activeEmptyLabel.gameObject);



        if (activeContent != null)

            LayoutRebuilder.ForceRebuildLayoutImmediate(activeContent);

    }



    void BuildActiveView()

    {

        var active = new List<ContractConfig>(_contracts.ActiveContracts);

        if (active.Count == 0) return;



        ContentSortOrder.SortContracts(active, _contracts);

        var card = SpawnCard(active[0], activeContent, ContractCardMode.Active);

        _activeCards.Add(card);

        _slotRoots.Add(card.gameObject);

        ApplyFixedSlotLayout(card.gameObject, ActiveCardHeight);

        DestroyRefreshButton();

    }



    void BuildSelectionView()

    {

        var promptGo = new GameObject("ContractPrompt", typeof(RectTransform));

        promptGo.transform.SetParent(activeContent, false);

        var prompt = RuntimeTmpText.Create(promptGo.transform, Loc.Get(LocKeys.ContractChoosePrompt), 15f,

            new Color(0.54f, 0.54f, 0.67f), FontStyles.Italic, TextAlignmentOptions.Center, "Label");

        prompt.textWrappingMode = TextWrappingModes.Normal;

        ApplyFixedSlotLayout(promptGo, 22f);

        _slotRoots.Add(promptGo);



        var candidates = new List<ContractConfig>(_contracts.CandidateContracts);

        ContentSortOrder.SortContracts(candidates, _contracts);



        for (int i = 0; i < VisibleSlotCount; i++)

        {

            if (i < candidates.Count)

            {

                var card = SpawnCard(candidates[i], activeContent, ContractCardMode.Candidate);

                _activeCards.Add(card);

                _slotRoots.Add(card.gameObject);

            }

            else

            {

                var placeholder = ContractCardFactory.CreatePlaceholder(activeContent, FixedSlotHeight);

                _slotRoots.Add(placeholder);

            }



            ApplyFixedSlotLayout(_slotRoots[_slotRoots.Count - 1], FixedSlotHeight);

        }



        EnsureRefreshButton();

    }



    void EnsureRefreshButton()

    {

        if (_refreshButtonRoot != null) return;



        _refreshButtonRoot = new GameObject("RefreshContractsBtn", typeof(RectTransform), typeof(Image), typeof(Button));

        _refreshButtonRoot.transform.SetParent(activeContent, false);

        HudSkinProvider.ApplyButton(_refreshButtonRoot.GetComponent<Image>(), HudButtonVariant.Secondary);

        _refreshButtonRoot.AddComponent<UIButtonScale>();

        ApplyFixedSlotLayout(_refreshButtonRoot, 36f);



        var lbl = RuntimeTmpText.Create(_refreshButtonRoot.transform, Loc.Get(LocKeys.ContractRefresh), 13f,

            Color.white, FontStyles.Bold, TextAlignmentOptions.Center, "Label");

        lbl.enableAutoSizing = true;



        _refreshButtonRoot.GetComponent<Button>().onClick.AddListener(() =>

            GameplayRefreshService.RequestContractCandidateRefresh(Rebuild));

        _slotRoots.Add(_refreshButtonRoot);

    }



    void DestroyRefreshButton()

    {

        if (_refreshButtonRoot == null) return;

        DestroySlot(_refreshButtonRoot);

        _refreshButtonRoot = null;

    }



    static void ApplyFixedSlotLayout(GameObject slotGo, float height)

    {

        if (slotGo == null) return;

        var le = slotGo.GetComponent<LayoutElement>() ?? slotGo.AddComponent<LayoutElement>();

        le.preferredHeight = height;

        le.minHeight = height;

        le.flexibleHeight = 0f;

        le.flexibleWidth = 1f;

    }



    void ClearSlots()

    {

        foreach (var card in _activeCards)

            if (card != null) DestroySlot(card.gameObject);

        _activeCards.Clear();



        foreach (var root in _slotRoots)

            if (root != null) DestroySlot(root);

        _slotRoots.Clear();

        _refreshButtonRoot = null;



        if (activeContent == null) return;

        for (int i = activeContent.childCount - 1; i >= 0; i--)

            DestroySlot(activeContent.GetChild(i).gameObject);

    }



    static void DestroySlot(GameObject go)

    {

        if (go == null) return;

        go.transform.SetParent(null, false);

        Destroy(go);

    }



    void HideHistorySection()

    {

        AutoWireReferences();



        if (historyContent != null)

        {

            Transform node = historyContent;

            while (node != null && node.name != "HistoryScroll" && node.name != "HistScroll")

                node = node.parent;

            if (node != null)

                CollapseLayoutBranch(node.gameObject);

        }



        if (historyEmptyLabel != null)

            CollapseLayoutBranch(historyEmptyLabel.gameObject);



        foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))

        {

            if (tmp.name == "HistHdr")

                CollapseLayoutBranch(tmp.gameObject);

        }



        ExpandActiveContractsScroll();

        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);

    }



    static void CollapseLayoutBranch(GameObject go)

    {

        if (go == null) return;

        go.SetActive(false);

        ResetLayoutElement(go, 0f, true);

    }



    static void ResetLayoutElement(GameObject go, float preferredHeight, bool ignoreLayout)

    {

        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();

        le.preferredHeight = preferredHeight;

        le.minHeight = 0f;

        le.flexibleHeight = 0f;

        le.ignoreLayout = ignoreLayout;

    }



    void ExpandActiveContractsScroll()

    {

        Transform scrollNode = transform.Find("ActiveScroll");

        if (scrollNode == null && activeContent != null)

        {

            scrollNode = activeContent;

            while (scrollNode != null && scrollNode.name != "ActiveScroll")

                scrollNode = scrollNode.parent;

        }

        if (scrollNode == null) return;



        var le = scrollNode.GetComponent<LayoutElement>() ?? scrollNode.gameObject.AddComponent<LayoutElement>();

        le.flexibleHeight = 1f;

        le.flexibleWidth = 1f;

        le.preferredHeight = 0f;

        le.minHeight = 0f;

        le.ignoreLayout = false;

    }



    ContractCardUI SpawnCard(ContractConfig cfg, RectTransform parent, ContractCardMode mode)

    {

        var card = ContractCardFactory.Create(parent, cfg, mode);

        card.Bind();

        return card;

    }

}



public enum ContractCardMode

{

    Active,

    Candidate,

    History,

}



/// <summary>Builds contract card UI at runtime (no prefab required).</summary>

public static class ContractCardFactory

{

    static readonly Color BG_SECTION = new Color(0.09f, 0.09f, 0.18f);

    static readonly Color BTN_GREEN  = new Color(0.15f, 0.68f, 0.38f);

    static readonly Color TEXT_PRI   = Color.white;

    static readonly Color TEXT_SEC   = new Color(0.54f, 0.54f, 0.67f);

    static readonly Color ACCENT_GOLD = new Color(0.95f, 0.77f, 0.06f);



    public static GameObject CreatePlaceholder(RectTransform parent, float height)

    {

        var cardGo = new GameObject("Contract_Empty", typeof(RectTransform), typeof(Image));

        cardGo.transform.SetParent(parent, false);

        HudSkinProvider.ApplyCard(cardGo.GetComponent<Image>(), HudCardVariant.Empty);



        var le = cardGo.AddComponent<LayoutElement>();

        le.preferredHeight = height;

        le.minHeight = height;

        le.flexibleHeight = 0f;

        le.flexibleWidth = 1f;



        var vlg = cardGo.AddComponent<VerticalLayoutGroup>();

        vlg.padding = HudLayoutConstants.SectionPadding;
        vlg.spacing = HudLayoutConstants.SectionSpacing;

        vlg.childAlignment = TextAnchor.MiddleCenter;

        vlg.childControlWidth = vlg.childControlHeight = true;

        vlg.childForceExpandWidth = true;

        vlg.childForceExpandHeight = true;



        var title = MakeText(cardGo.transform, "Sin contrato disponible", 16, TEXT_SEC, FontStyles.Italic);

        title.alignment = TextAlignmentOptions.Center;

        title.textWrappingMode = TextWrappingModes.Normal;

        return cardGo;

    }



    public static ContractCardUI Create(RectTransform parent, ContractConfig cfg, ContractCardMode mode)

    {

        bool historyMode = mode == ContractCardMode.History;

        bool candidateMode = mode == ContractCardMode.Candidate;

        bool activeMode = mode == ContractCardMode.Active;

        float cardHeight = activeMode
            ? ContractsPanelUI.ActiveCardHeight
            : ContractsPanelUI.FixedSlotHeightPublic;



        var cardGo = new GameObject("Contract_" + cfg.id, typeof(RectTransform), typeof(Image));

        cardGo.transform.SetParent(parent, false);

        HudSkinProvider.ApplyCard(cardGo.GetComponent<Image>(), HudCardVariant.Primary);

        var le = cardGo.AddComponent<LayoutElement>();

        le.preferredHeight = historyMode ? 72 : cardHeight;

        le.minHeight = le.preferredHeight;

        le.flexibleHeight = 0f;

        le.flexibleWidth = 1f;



        var vlg = cardGo.AddComponent<VerticalLayoutGroup>();

        vlg.padding = activeMode ? new RectOffset(12, 12, 8, 8) : HudLayoutConstants.SectionPadding;
        vlg.spacing = activeMode ? 2 : HudLayoutConstants.SectionSpacing;

        vlg.spacing = activeMode ? 2 : 4;

        vlg.childControlWidth = vlg.childControlHeight = true;

        vlg.childForceExpandWidth = true;

        vlg.childForceExpandHeight = false;



        var title = MakeText(cardGo.transform, activeMode ? "CONTRATO ACTIVO" : cfg.contractTitle,
            activeMode ? 12f : 20f, activeMode ? ACCENT_GOLD : TEXT_PRI, FontStyles.Bold);

        title.GetComponent<RectTransform>().gameObject.AddComponent<LayoutElement>().preferredHeight = activeMode ? 16f : 28f;



        if (historyMode)

        {

            MakeText(cardGo.transform, "Completado — " + BuildReward(cfg), 14, TEXT_SEC);

            var histUI = cardGo.AddComponent<ContractCardUI>();

            histUI.contract = cfg;

            histUI.titleText = title;

            histUI.isHistoryMode = true;

            return histUI;

        }



        var desc = MakeText(cardGo.transform, cfg.description, activeMode ? 11f : 16f, TEXT_SEC);

        desc.textWrappingMode = TextWrappingModes.Normal;

        desc.GetComponent<RectTransform>().gameObject.AddComponent<LayoutElement>().preferredHeight = activeMode ? 0f : 24f;
        if (activeMode) desc.gameObject.SetActive(false);



        var objective = MakeText(cardGo.transform, Loc.Format(LocKeys.ContractActiveObjective,

            ContractSystem.BuildObjectiveLabel(cfg)), activeMode ? 13f : 14f, ACCENT_GOLD);

        objective.textWrappingMode = TextWrappingModes.Normal;

        objective.GetComponent<RectTransform>().gameObject.AddComponent<LayoutElement>().preferredHeight = activeMode ? 18f : 20f;



        Slider slider = null;

        TextMeshProUGUI progTxt = null;



        if (!candidateMode)

        {

            var progRow = new GameObject("ProgRow", typeof(RectTransform));

            progRow.transform.SetParent(cardGo.transform, false);

            var hlg = progRow.AddComponent<HorizontalLayoutGroup>();

            hlg.spacing = 8;

            hlg.childControlWidth = hlg.childControlHeight = true;

            hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;

            progRow.AddComponent<LayoutElement>().preferredHeight = 6;

            var progGo = new GameObject("ProgBar", typeof(RectTransform), typeof(Image), typeof(Slider));

            progGo.transform.SetParent(progRow.transform, false);

            progGo.GetComponent<Image>().color = BG_SECTION;

            var progLE = progGo.AddComponent<LayoutElement>();

            progLE.flexibleWidth = 1;

            progLE.preferredHeight = 6;
            progLE.minHeight = 6;

            slider = progGo.GetComponent<Slider>();

            SetupSliderFill(slider, BTN_GREEN);

            ReadOnlySlider.Configure(slider);



            progTxt = MakeText(progRow.transform, "0/0", activeMode ? 12f : 14f, TEXT_SEC);

            progTxt.GetComponent<RectTransform>().gameObject.AddComponent<LayoutElement>().preferredWidth = activeMode ? 56f : 70f;

        }



        var rewardTxt = MakeText(cardGo.transform, BuildReward(cfg), activeMode ? 12f : 17f, ACCENT_GOLD);

        rewardTxt.GetComponent<RectTransform>().gameObject.AddComponent<LayoutElement>().preferredHeight = activeMode ? 16f : 24f;
        if (activeMode) rewardTxt.gameObject.SetActive(false);



        Button claimBtn = null;

        Button selectBtn = null;



        if (candidateMode)

        {

            var selectGo = new GameObject("SelectBtn", typeof(RectTransform), typeof(Image), typeof(Button));

            selectGo.transform.SetParent(cardGo.transform, false);

            HudSkinProvider.ApplyButton(selectGo.GetComponent<Image>(), HudButtonVariant.Primary);

            selectGo.AddComponent<LayoutElement>().preferredHeight = 42; // Phase 8.5C

            selectGo.AddComponent<UIButtonScale>();

            var selectLbl = MakeText(selectGo.transform, Loc.Get(LocKeys.ContractSelect), 16, TEXT_PRI, FontStyles.Bold); // Phase 8.5C

            selectLbl.alignment = TextAlignmentOptions.Center;

            Stretch(selectLbl.rectTransform);

            selectBtn = selectGo.GetComponent<Button>();

        }

        else

        {

            var claimGo = new GameObject("ClaimBtn", typeof(RectTransform), typeof(Image), typeof(Button));

            claimGo.transform.SetParent(cardGo.transform, false);

            HudSkinProvider.ApplyButton(claimGo.GetComponent<Image>(), HudButtonVariant.Primary);

            claimGo.AddComponent<LayoutElement>().preferredHeight = activeMode ? 34f : 42f;

            claimGo.AddComponent<UIButtonScale>();

            var lbl = MakeText(claimGo.transform, "RECLAMAR", 16, TEXT_PRI, FontStyles.Bold); // Phase 8.5C

            lbl.alignment = TextAlignmentOptions.Center;

            Stretch(lbl.rectTransform);

            claimGo.SetActive(false);

            claimBtn = claimGo.GetComponent<Button>();

        }



        var ui = cardGo.AddComponent<ContractCardUI>();

        ui.contract = cfg;

        ui.titleText = title;

        ui.descText = desc;

        ui.objectiveText = objective;

        ui.progressText = progTxt;

        ui.progressBar = slider;

        ui.rewardText = rewardTxt;

        ui.claimButton = claimBtn;

        ui.selectButton = selectBtn;

        ui.isCandidateMode = candidateMode;

        return ui;

    }



    static TextMeshProUGUI MakeText(Transform parent, string text, float size, Color color, FontStyles style = FontStyles.Normal)

    {

        var tmp = RuntimeTmpText.Create(parent, text, size, color, style);

        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        tmp.overflowMode = TextOverflowModes.Truncate;

        return tmp;

    }



    static void SetupSliderFill(Slider slider, Color fillColor)

    {

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));

        fillArea.transform.SetParent(slider.transform, false);

        var faRT = fillArea.GetComponent<RectTransform>();

        faRT.anchorMin = Vector2.zero;

        faRT.anchorMax = Vector2.one;

        faRT.offsetMin = faRT.offsetMax = Vector2.zero;

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));

        fill.transform.SetParent(fillArea.transform, false);

        fill.GetComponent<Image>().color = fillColor;

        var fillRT = fill.GetComponent<RectTransform>();

        fillRT.anchorMin = Vector2.zero;

        fillRT.anchorMax = Vector2.one;

        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

        slider.fillRect = fillRT;

    }



    static void Stretch(RectTransform rt)

    {

        rt.anchorMin = Vector2.zero;

        rt.anchorMax = Vector2.one;

        rt.offsetMin = rt.offsetMax = Vector2.zero;

    }



    static string BuildReward(ContractConfig c)

    {

        var p = new List<string>();

        if (c.rewardMoney > 0) p.Add("$" + c.rewardMoney);

        if (c.rewardDiamonds > 0) p.Add("[D]" + c.rewardDiamonds);

        if (c.rewardReputation > 0) p.Add("+" + c.rewardReputation + " REP");

        if (c.rewardStudioXP > 0) p.Add("+" + c.rewardStudioXP + " XP");

        return string.Join(" · ", p);

    }

}


