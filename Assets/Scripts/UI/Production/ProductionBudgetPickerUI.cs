using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Budget decision overlay — BAJO / NORMAL / PREMIUM (Phase 8.2).</summary>
public class ProductionBudgetPickerUI : MonoBehaviour
{
    public static ProductionBudgetPickerUI Instance { get; private set; }

    static readonly Color OverlayDim = new Color(0f, 0f, 0f, 0.72f);
    static readonly Color TextPrimary = Color.white;
    static readonly Color TextSecondary = new Color(0.54f, 0.54f, 0.67f);

    RectTransform _panel;
    TextMeshProUGUI _decorIconText;
    TextMeshProUGUI _titleText;
    Action<ProductionBudget> _onConfirm;
    MovieConfig _pendingConfig;

    // FASE 16.2 — Tiempo producción: one stats label per budget option, updated in Open()
    readonly TextMeshProUGUI[] _budgetStatsLabels = new TextMeshProUGUI[3]; // Cheap, Standard, Premium

    public static bool IsOpen => Instance != null && Instance.gameObject.activeSelf;
    public static event Action<bool> OnVisibilityChanged;

    public static void Show(MovieConfig config, Action<ProductionBudget> onConfirm)
    {
        if (config == null) return;
        EnsureInstance();
        if (Instance == null) return;
        Instance.Open(config, onConfirm);
    }

    static void EnsureInstance()
    {
        if (Instance != null) return;

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("ProductionBudgetPicker", typeof(RectTransform), typeof(CanvasGroup), typeof(ProductionBudgetPickerUI));
        go.transform.SetParent(canvas.transform, false);
        var rt = go.GetComponent<RectTransform>();
        Stretch(rt);
        Instance = go.GetComponent<ProductionBudgetPickerUI>();
        Instance.Build(go.transform);
        go.SetActive(false);
    }

    void Build(Transform root)
    {
        var backdrop = CreatePanel(root, "Backdrop", OverlayDim);
        Stretch(backdrop);
        var backdropBtn = backdrop.gameObject.AddComponent<Button>();
        backdropBtn.onClick.AddListener(Close);

        _panel = CreatePanel(root, "Panel", Color.clear);
        var panelRT = _panel;
        // A2: Doubled visual size for mobile legibility
        panelRT.anchorMin = new Vector2(0.04f, 0.05f);
        panelRT.anchorMax = new Vector2(0.96f, 0.95f);
        panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;
        HudSkinProvider.ApplyPanel(_panel.GetComponent<Image>(), HudPanelVariant.Card);

        var vlg = _panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(16, 16, 16, 16);
        vlg.spacing = 10;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        float sc = 1.0f;
        var hdr = RuntimeTmpText.Create(_panel, Loc.Get(LocKeys.ProdBudgetTitle), 18f, TextPrimary,
            FontStyles.Bold, TextAlignmentOptions.Center, "Title");
        hdr.enableAutoSizing = true;
        hdr.fontSizeMin = 14f * sc;
        hdr.fontSizeMax = 20f * sc;
        LE(hdr.rectTransform, 44f);

        var sub = RuntimeTmpText.Create(_panel, Loc.Get(LocKeys.ProdBudgetSubtitle), 14f, TextSecondary,
            FontStyles.Normal, TextAlignmentOptions.Center, "Subtitle");
        sub.enableAutoSizing = true;
        sub.fontSizeMin = 14f * sc;
        sub.fontSizeMax = 16f * sc;
        LE(sub.rectTransform, 44f);

        var iconWrap = CreatePanel(_panel, "DecorIconWrap", new Color(0.09f, 0.09f, 0.18f));
        LE(iconWrap, 96f);
        _decorIconText = RuntimeTmpText.Create(iconWrap.transform, string.Empty, 44f, TextPrimary,
            FontStyles.Normal, TextAlignmentOptions.Center, "DecorIcon");
        Stretch(_decorIconText.rectTransform);

        _titleText = RuntimeTmpText.Create(_panel, string.Empty, 20f, TextPrimary,
            FontStyles.Bold, TextAlignmentOptions.Center, "MovieTitle");
        _titleText.enableAutoSizing = true;
        _titleText.fontSizeMin = 18f * sc;
        _titleText.fontSizeMax = 22f * sc;
        LE(_titleText.rectTransform, 60f);

        _budgetStatsLabels[0] = CreateBudgetOption(_panel, LocKeys.ProdBudgetCheap,    ProductionBudget.Cheap,    HudCardVariant.Secondary);
        _budgetStatsLabels[1] = CreateBudgetOption(_panel, LocKeys.ProdBudgetStandard, ProductionBudget.Standard, HudCardVariant.Primary);
        _budgetStatsLabels[2] = CreateBudgetOption(_panel, LocKeys.ProdBudgetPremium,  ProductionBudget.Premium,  HudCardVariant.Hero);

        var cancelGo = new GameObject("CancelBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        cancelGo.transform.SetParent(_panel, false);
        HudSkinProvider.ApplyButton(cancelGo.GetComponent<Image>(), HudButtonVariant.Secondary);
        cancelGo.AddComponent<UIButtonScale>();
        LE(cancelGo.GetComponent<RectTransform>(), 36f);
        cancelGo.GetComponent<Button>().onClick.AddListener(Close);
        var cancelLbl = RuntimeTmpText.Create(cancelGo.transform, Loc.Get(LocKeys.ProdBudgetCancel), 14f, TextPrimary,
            FontStyles.Bold, TextAlignmentOptions.Center, "Label");
        cancelLbl.enableAutoSizing = true;
        cancelLbl.fontSizeMin = 14f;
        LE(cancelGo.GetComponent<RectTransform>(), 44f);
    }

    // Returns the stats label so it can be refreshed in Open() with actual duration
    TextMeshProUGUI CreateBudgetOption(Transform parent, string locKey, ProductionBudget budget, HudCardVariant variant)
    {
        var go = new GameObject("Budget_" + budget, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        HudSkinProvider.ApplyCard(go.GetComponent<Image>(), variant);
        go.AddComponent<UIButtonScale>();
        LE(go.GetComponent<RectTransform>(), 84f);
        go.GetComponent<Button>().onClick.AddListener(() => Confirm(budget));

        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(8, 8, 6, 6);
        vlg.spacing = 2;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var lbl = RuntimeTmpText.Create(go.transform, Loc.Get(locKey), 16f, TextPrimary,
            FontStyles.Bold, TextAlignmentOptions.Center, "Label");
        lbl.enableAutoSizing = true;
        lbl.fontSizeMin = 16f;
        lbl.fontSizeMax = 18f;
        lbl.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        var stats = RuntimeTmpText.Create(go.transform, ProductionBudgetRules.FormatStatBlock(budget), 16f, TextSecondary,
            FontStyles.Normal, TextAlignmentOptions.Center, "Stats");
        stats.enableAutoSizing = true;
        stats.fontSizeMin = 16f;
        stats.fontSizeMax = 18f;
        stats.textWrappingMode = TextWrappingModes.Normal;
        stats.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;

        return stats;
    }

    void Open(MovieConfig config, Action<ProductionBudget> onConfirm)
    {
        _pendingConfig = config;
        _onConfirm = onConfirm;
        if (_titleText != null) _titleText.text = config.movieName;

        // FASE 16.2 — show real production time per budget option
        var budgets = new[] { ProductionBudget.Cheap, ProductionBudget.Standard, ProductionBudget.Premium };
        for (int i = 0; i < _budgetStatsLabels.Length && i < budgets.Length; i++)
        {
            var lbl = _budgetStatsLabels[i];
            if (lbl == null) continue;
            var mod = ProductionBudgetRules.GetModifiers(budgets[i]);
            float dur = config.duration * mod.durationMultiplier;
            string durStr = ProductionLoc.FormatTimeRemaining(dur);
            lbl.text = $"{durStr}\n{ProductionBudgetRules.FormatStatBlock(budgets[i])}";
        }
        if (_decorIconText != null)
        {
            // Apply sprite icon (same as offer cards); emoji text is hidden behind the sprite
            UIIconGraphic.ApplyGenreRarityIcon(
                _decorIconText.transform.parent,
                UIIconCatalog.GetProductionIcon(config.rarity, config.genre),
                _decorIconText);
            _decorIconText.color = ProductionDecorIcon.GetIconColor(config.rarity, config.genre);
        }
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            OnVisibilityChanged?.Invoke(true);
            UIAnimationService.PlayPopupOpen(_panel, GetComponent<CanvasGroup>());
        }
    }

    void Confirm(ProductionBudget budget)
    {
        _onConfirm?.Invoke(budget);
        Close();
    }

    void Close()
    {
        _pendingConfig = null;
        _onConfirm = null;
        if (!gameObject.activeSelf) return;
        var cg = GetComponent<CanvasGroup>();
        _panel?.DOKill();
        cg?.DOKill();
        UIAnimationService.PlayPopupClose(_panel, cg, () =>
        {
            gameObject.SetActive(false);
            OnVisibilityChanged?.Invoke(false);
        });
    }

    static RectTransform CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go.GetComponent<RectTransform>();
    }

    static void LE(RectTransform rt, float height)
    {
        var le = rt.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = height;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
