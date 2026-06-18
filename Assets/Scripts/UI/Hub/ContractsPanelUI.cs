using System.Collections.Generic;

using UnityEngine;using UnityEngine.UI;

using TMPro;



/// <summary>

/// Contract panel — 3 candidates to choose OR 1 active contract card (Phase 8.0).

/// </summary>

public class ContractsPanelUI : MonoBehaviour

{

    public const int VisibleSlotCount = 3;

    public const float FixedSlotHeightPublic = 280f; // FASE 16.1 A: larger for mobile readability (was 220)

    const float FixedSlotHeight = FixedSlotHeightPublic;

    public const float ActiveCardHeight = 148f; // FASE 16.1 A4: larger for mobile readability (was 110)



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

    static readonly Color BG_SECTION = CinematicTheme.PanelBg;

    static readonly Color BTN_GREEN  = CinematicTheme.ProgressFill;

    static readonly Color TEXT_PRI   = CinematicTheme.TextPrimary;

    static readonly Color TEXT_SEC   = CinematicTheme.TextSecondary;

    static readonly Color ACCENT_GOLD = CinematicTheme.GoldBright;



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



        var title = MakeText(cardGo.transform, Loc.Get(LocKeys.ContractNoneAvailable), 16, TEXT_SEC, FontStyles.Italic);

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
        CinematicTheme.ApplyPremiumMaterial(cardGo.GetComponent<RectTransform>());

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



        var title = MakeText(cardGo.transform, activeMode ? Loc.Get(LocKeys.ContractActive) : cfg.contractTitle,
            activeMode ? 13f : 22f, activeMode ? ACCENT_GOLD : TEXT_PRI, FontStyles.Bold);

        title.GetComponent<RectTransform>().gameObject.AddComponent<LayoutElement>().preferredHeight = activeMode ? 18f : 30f;



        if (historyMode)

        {

            MakeText(cardGo.transform, Loc.Get(LocKeys.ContractCompletedPfx) + BuildReward(cfg), 14, TEXT_SEC);

            var histUI = cardGo.AddComponent<ContractCardUI>();

            histUI.contract = cfg;

            histUI.titleText = title;

            histUI.isHistoryMode = true;

            return histUI;

        }



        var desc = MakeText(cardGo.transform, cfg.description, activeMode ? 12f : 17f, TEXT_SEC);

        desc.textWrappingMode = TextWrappingModes.Normal;

        desc.GetComponent<RectTransform>().gameObject.AddComponent<LayoutElement>().preferredHeight = activeMode ? 0f : 26f;
        if (activeMode) desc.gameObject.SetActive(false);



        var objective = MakeText(cardGo.transform, Loc.Format(LocKeys.ContractActiveObjective,

            ContractSystem.BuildObjectiveLabel(cfg)), activeMode ? 14f : 16f, ACCENT_GOLD);

        objective.textWrappingMode = TextWrappingModes.Normal;

        objective.GetComponent<RectTransform>().gameObject.AddComponent<LayoutElement>().preferredHeight = activeMode ? 20f : 22f;



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

            progRow.AddComponent<LayoutElement>().preferredHeight = 8;

            var progGo = new GameObject("ProgBar", typeof(RectTransform), typeof(Image), typeof(Slider));

            progGo.transform.SetParent(progRow.transform, false);

            progGo.GetComponent<Image>().color = BG_SECTION;

            var progLE = progGo.AddComponent<LayoutElement>();

            progLE.flexibleWidth = 1;

            progLE.preferredHeight = 8;
            progLE.minHeight = 8;

            slider = progGo.GetComponent<Slider>();

            SetupSliderFill(slider, BTN_GREEN);

            ReadOnlySlider.Configure(slider);



            progTxt = MakeText(progRow.transform, "0/0", activeMode ? 13f : 15f, TEXT_SEC);

            progTxt.GetComponent<RectTransform>().gameObject.AddComponent<LayoutElement>().preferredWidth = activeMode ? 64f : 76f;

        }



        var rewardTxt = MakeText(cardGo.transform, BuildReward(cfg), activeMode ? 13f : 19f, ACCENT_GOLD);

        rewardTxt.GetComponent<RectTransform>().gameObject.AddComponent<LayoutElement>().preferredHeight = activeMode ? 18f : 26f;
        if (activeMode) rewardTxt.gameObject.SetActive(false);



        Button claimBtn = null;

        Button selectBtn = null;

        Button rerollBtn = null;



        if (candidateMode)

        {

            // Phase 13.4D — action row: [REROLL 💎5] [SELECCIONAR]
            var actionRow = new GameObject("ActionRow", typeof(RectTransform));

            actionRow.transform.SetParent(cardGo.transform, false);

            var actionHLG = actionRow.AddComponent<HorizontalLayoutGroup>();

            actionHLG.spacing = 6;

            actionHLG.childControlWidth = actionHLG.childControlHeight = true;

            actionHLG.childForceExpandWidth = false;

            actionHLG.childForceExpandHeight = true;

            actionRow.AddComponent<LayoutElement>().preferredHeight = 48f;



            var rerollGo = new GameObject("RerollBtn", typeof(RectTransform), typeof(Image), typeof(Button));

            rerollGo.transform.SetParent(actionRow.transform, false);

            HudSkinProvider.ApplyButton(rerollGo.GetComponent<Image>(), HudButtonVariant.Secondary);

            rerollGo.AddComponent<UIButtonScale>();

            var rerollLE = rerollGo.AddComponent<LayoutElement>();

            rerollLE.preferredWidth = 100f;

            rerollLE.minWidth = 90f;

            rerollLE.flexibleWidth = 0f;

            var rerollLbl = MakeText(rerollGo.transform, Loc.Get(LocKeys.ContractRerollBtn), 14, TEXT_PRI, FontStyles.Bold);

            rerollLbl.alignment = TextAlignmentOptions.Center;

            Stretch(rerollLbl.rectTransform);

            rerollBtn = rerollGo.GetComponent<Button>();



            var selectGo = new GameObject("SelectBtn", typeof(RectTransform), typeof(Image), typeof(Button));

            selectGo.transform.SetParent(actionRow.transform, false);

            HudSkinProvider.ApplyButton(selectGo.GetComponent<Image>(), HudButtonVariant.Primary);

            selectGo.AddComponent<UIButtonScale>();

            selectGo.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var selectLbl = MakeText(selectGo.transform, Loc.Get(LocKeys.ContractSelect), 18, TEXT_PRI, FontStyles.Bold); // Phase 12.3

            selectLbl.alignment = TextAlignmentOptions.Center;

            Stretch(selectLbl.rectTransform);

            selectBtn = selectGo.GetComponent<Button>();

        }

        else

        {

            var claimGo = new GameObject("ClaimBtn", typeof(RectTransform), typeof(Image), typeof(Button));

            claimGo.transform.SetParent(cardGo.transform, false);

            HudSkinProvider.ApplyButton(claimGo.GetComponent<Image>(), HudButtonVariant.Primary);

            claimGo.AddComponent<LayoutElement>().preferredHeight = activeMode ? 40f : 48f; // Phase 12.3

            claimGo.AddComponent<UIButtonScale>();

            var lbl = MakeText(claimGo.transform, Loc.Get(LocKeys.ContractClaim), 18, TEXT_PRI, FontStyles.Bold); // Phase 12.3

            lbl.alignment = TextAlignmentOptions.Center;

            Stretch(lbl.rectTransform);

            claimGo.SetActive(false);

            claimBtn = claimGo.GetComponent<Button>();

        }



        // ── FASE 16.1 D3: Cancel-via-ad button (active mode only) ─────────────
        Button cancelAdBtn = null;
        if (activeMode)
        {
            var cancelGo = new GameObject("CancelAdBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            cancelGo.transform.SetParent(cardGo.transform, false);
            HudSkinProvider.ApplyButton(cancelGo.GetComponent<Image>(), HudButtonVariant.Secondary);
            cancelGo.AddComponent<LayoutElement>().preferredHeight = 36f;
            cancelGo.AddComponent<UIButtonScale>();
            var cancelLbl = MakeText(cancelGo.transform, Loc.Get(LocKeys.ContractCancelWithAd),
                13, TEXT_SEC, FontStyles.Normal);
            cancelLbl.alignment = TextAlignmentOptions.Center;
            Stretch(cancelLbl.rectTransform);
            cancelAdBtn = cancelGo.GetComponent<Button>();
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

        ui.rerollDiamondsButton = rerollBtn;

        ui.cancelAdButton = cancelAdBtn;

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

        if (c.rewardMoney > 0)      p.Add(Loc.Format(LocKeys.ContractRewardMoney, AnimatedMoneyText.FormatMoney(c.rewardMoney)));

        if (c.rewardDiamonds > 0)   p.Add(Loc.Format(LocKeys.ContractRewardDiam, c.rewardDiamonds));

        if (c.rewardReputation > 0) p.Add(Loc.Format(LocKeys.ContractRewardRep, c.rewardReputation));

        if (c.rewardStudioXP > 0)   p.Add(Loc.Format(LocKeys.ContractRewardXP, c.rewardStudioXP));

        return p.Count > 0 ? string.Join("  ", p) : "—";

    }

}


