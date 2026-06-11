using System.Collections.Generic;
using UnityEngine;using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime contract panel: active contracts only (history kept internally in ContractSystem).
/// </summary>
public class ContractsPanelUI : MonoBehaviour
{
    public const int VisibleSlotCount = 3;
    public const float FixedSlotHeightPublic = 110f;
    const float FixedSlotHeight = FixedSlotHeightPublic;

    [Header("Containers")]
    public RectTransform activeContent;
    public TextMeshProUGUI activeEmptyLabel;
    public TextMeshProUGUI historyEmptyLabel;
    public RectTransform historyContent;

    private ContractSystem _contracts;
    private ScrollRect _activeScroll;
    private readonly List<ContractCardUI> _activeCards = new();
    private readonly List<GameObject> _slotRoots = new();

    private void Awake()
    {
        AutoWireReferences();
        HideHistorySection();
        GameHub.OnGameReady += Bind;
    }

    private void OnEnable()
    {
        if (_contracts != null)
            Rebuild();
    }

    private void OnDestroy()
    {
        GameHub.OnGameReady -= Bind;
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

    private void Bind()
    {
        GameHub.OnGameReady -= Bind;
        _contracts = GameHub.Instance?.contracts;
        if (_contracts == null) return;

        _contracts.OnContractUpdated   += Rebuild;
        _contracts.OnContractCompleted += OnContractEvent;
        _contracts.OnContractClaimed   += OnContractEvent;
        Rebuild();
    }

    private void UnbindEvents()
    {
        if (_contracts == null) return;
        _contracts.OnContractUpdated   -= Rebuild;
        _contracts.OnContractCompleted -= OnContractEvent;
        _contracts.OnContractClaimed   -= OnContractEvent;
    }

    private void OnContractEvent(ContractConfig _) => Rebuild();

    public void Rebuild()
    {
        AutoWireReferences();
        if (_contracts == null || activeContent == null) return;

        ClearSlots();

        var active = new List<ContractConfig>(_contracts.ActiveContracts);
        ContentSortOrder.SortContracts(active, _contracts);

        for (int i = 0; i < VisibleSlotCount; i++)
        {
            if (i < active.Count)
            {
                var card = SpawnCard(active[i], activeContent);
                _activeCards.Add(card);
                _slotRoots.Add(card.gameObject);
            }
            else
            {
                var placeholder = ContractCardFactory.CreatePlaceholder(activeContent, FixedSlotHeight);
                _slotRoots.Add(placeholder);
            }

            ApplyFixedSlotLayout(_slotRoots[i]);
        }

        if (activeEmptyLabel != null)
            CollapseLayoutBranch(activeEmptyLabel.gameObject);

        if (activeContent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(activeContent);
    }

    static void ApplyFixedSlotLayout(GameObject slotGo)
    {
        if (slotGo == null) return;
        var le = slotGo.GetComponent<LayoutElement>() ?? slotGo.AddComponent<LayoutElement>();
        le.preferredHeight = FixedSlotHeight;
        le.minHeight = FixedSlotHeight;
        le.flexibleHeight = 0f;
        le.flexibleWidth = 1f;
    }

    void ClearSlots()
    {
        foreach (var card in _activeCards)
            if (card != null) Destroy(card.gameObject);
        _activeCards.Clear();

        foreach (var root in _slotRoots)
            if (root != null) Destroy(root);
        _slotRoots.Clear();

        if (activeContent == null) return;
        for (int i = activeContent.childCount - 1; i >= 0; i--)
            Destroy(activeContent.GetChild(i).gameObject);
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

    ContractCardUI SpawnCard(ContractConfig cfg, RectTransform parent, bool historyMode = false)
    {
        var card = ContractCardFactory.Create(parent, cfg, historyMode);
        card.Bind();
        return card;
    }
}

/// <summary>Builds contract card UI at runtime (no prefab required).</summary>
public static class ContractCardFactory
{
    static readonly Color BG_CARD    = new Color(0.12f, 0.12f, 0.23f);
    static readonly Color BG_EMPTY   = new Color(0.08f, 0.08f, 0.15f);
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
        vlg.padding = new RectOffset(10, 10, 8, 8);
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = true;

        var title = MakeText(cardGo.transform, "Sin contrato disponible", 16, TEXT_SEC, FontStyles.Italic);
        title.alignment = TextAlignmentOptions.Center;
        title.textWrappingMode = TextWrappingModes.Normal;
        return cardGo;
    }

    public static ContractCardUI Create(RectTransform parent, ContractConfig cfg, bool historyMode)
    {
        var cardGo = new GameObject("Contract_" + cfg.id, typeof(RectTransform), typeof(Image));
        cardGo.transform.SetParent(parent, false);
        HudSkinProvider.ApplyCard(cardGo.GetComponent<Image>(), HudCardVariant.Primary);
        var le = cardGo.AddComponent<LayoutElement>();
        le.preferredHeight = historyMode ? 72 : ContractsPanelUI.FixedSlotHeightPublic;
        le.minHeight = le.preferredHeight;
        le.flexibleHeight = 0f;
        le.flexibleWidth = 1f;

        var vlg = cardGo.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 8, 8);
        vlg.spacing = 4;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var title = MakeText(cardGo.transform, cfg.contractTitle, 18, TEXT_PRI, FontStyles.Bold);
        title.GetComponent<RectTransform>().gameObject.AddComponent<LayoutElement>().preferredHeight = 24;

        if (!historyMode)
        {
            var desc = MakeText(cardGo.transform, cfg.description, 15, TEXT_SEC);
            desc.textWrappingMode = TextWrappingModes.Normal;
            desc.GetComponent<RectTransform>().gameObject.AddComponent<LayoutElement>().preferredHeight = 22;

            var progRow = new GameObject("ProgRow", typeof(RectTransform));
            progRow.transform.SetParent(cardGo.transform, false);
            var hlg = progRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childControlWidth = hlg.childControlHeight = true;
            hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;
            progRow.AddComponent<LayoutElement>().preferredHeight = 14;

            var progGo = new GameObject("ProgBar", typeof(RectTransform), typeof(Image), typeof(Slider));
            progGo.transform.SetParent(progRow.transform, false);
            progGo.GetComponent<Image>().color = BG_SECTION;
            var progLE = progGo.AddComponent<LayoutElement>();
            progLE.flexibleWidth = 1;
            progLE.preferredHeight = 12;
            var slider = progGo.GetComponent<Slider>();
            SetupSliderFill(slider, BTN_GREEN);
            ReadOnlySlider.Configure(slider);

            var progTxt = MakeText(progRow.transform, "0/0", 14, TEXT_SEC);
            progTxt.GetComponent<RectTransform>().gameObject.AddComponent<LayoutElement>().preferredWidth = 70;

            var rewardTxt = MakeText(cardGo.transform, BuildReward(cfg), 15, ACCENT_GOLD);
            rewardTxt.GetComponent<RectTransform>().gameObject.AddComponent<LayoutElement>().preferredHeight = 20;

            var claimGo = new GameObject("ClaimBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            claimGo.transform.SetParent(cardGo.transform, false);
            HudSkinProvider.ApplyButton(claimGo.GetComponent<Image>(), HudButtonVariant.Primary);
            claimGo.AddComponent<LayoutElement>().preferredHeight = 30;
            claimGo.AddComponent<UIButtonScale>();
            var lbl = MakeText(claimGo.transform, "RECLAMAR", 15, TEXT_PRI, FontStyles.Bold);
            lbl.alignment = TextAlignmentOptions.Center;
            Stretch(lbl.rectTransform);
            claimGo.SetActive(false);

            var ui = cardGo.AddComponent<ContractCardUI>();
            ui.contract = cfg;
            ui.titleText = title;
            ui.descText = desc;
            ui.progressText = progTxt;
            ui.progressBar = slider;
            ui.rewardText = rewardTxt;
            ui.claimButton = claimGo.GetComponent<Button>();
            return ui;
        }

        MakeText(cardGo.transform, "Completado — " + BuildReward(cfg), 14, TEXT_SEC);
        var histUI = cardGo.AddComponent<ContractCardUI>();
        histUI.contract = cfg;
        histUI.titleText = title;
        histUI.isHistoryMode = true;
        return histUI;
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
