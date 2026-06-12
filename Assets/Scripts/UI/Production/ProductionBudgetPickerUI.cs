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
    Image _posterImage;
    TextMeshProUGUI _titleText;
    Action<ProductionBudget> _onConfirm;
    MovieConfig _pendingConfig;

    public static void Show(MovieConfig config, Action<ProductionBudget> onConfirm)
    {
        if (config == null) return;
        EnsureInstance();
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
        panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(320f, 420f);
        panelRT.anchoredPosition = Vector2.zero;
        HudSkinProvider.ApplyPanel(_panel.GetComponent<Image>(), HudPanelVariant.Card);

        var vlg = _panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(16, 16, 16, 16);
        vlg.spacing = 10;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var hdr = RuntimeTmpText.Create(_panel, Loc.Get(LocKeys.ProdBudgetTitle), 18f, TextPrimary,
            FontStyles.Bold, TextAlignmentOptions.Center, "Title");
        hdr.enableAutoSizing = true;
        hdr.fontSizeMin = 14f;
        hdr.fontSizeMax = 20f;
        LE(hdr.rectTransform, 24f);

        var sub = RuntimeTmpText.Create(_panel, Loc.Get(LocKeys.ProdBudgetSubtitle), 11f, TextSecondary,
            FontStyles.Normal, TextAlignmentOptions.Center, "Subtitle");
        sub.enableAutoSizing = true;
        sub.fontSizeMin = 9f;
        sub.fontSizeMax = 12f;
        LE(sub.rectTransform, 28f);

        var posterWrap = CreatePanel(_panel, "PosterWrap", new Color(0.09f, 0.09f, 0.18f));
        LE(posterWrap, 120f);
        _posterImage = CreatePanel(posterWrap, "Poster", Color.clear).GetComponent<Image>();
        var posterRT = _posterImage.rectTransform;
        posterRT.anchorMin = new Vector2(0.06f, 0.06f);
        posterRT.anchorMax = new Vector2(0.94f, 0.94f);
        posterRT.offsetMin = posterRT.offsetMax = Vector2.zero;

        _titleText = RuntimeTmpText.Create(_panel, string.Empty, 15f, TextPrimary,
            FontStyles.Bold, TextAlignmentOptions.Center, "MovieTitle");
        _titleText.enableAutoSizing = true;
        _titleText.fontSizeMin = 12f;
        _titleText.fontSizeMax = 16f;
        LE(_titleText.rectTransform, 36f);

        CreateBudgetOption(_panel, LocKeys.ProdBudgetCheap, ProductionBudget.Cheap, HudCardVariant.Secondary);
        CreateBudgetOption(_panel, LocKeys.ProdBudgetStandard, ProductionBudget.Standard, HudCardVariant.Primary);
        CreateBudgetOption(_panel, LocKeys.ProdBudgetPremium, ProductionBudget.Premium, HudCardVariant.Hero);

        var cancelGo = new GameObject("CancelBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        cancelGo.transform.SetParent(_panel, false);
        HudSkinProvider.ApplyButton(cancelGo.GetComponent<Image>(), HudButtonVariant.Secondary);
        cancelGo.AddComponent<UIButtonScale>();
        LE(cancelGo.GetComponent<RectTransform>(), 36f);
        cancelGo.GetComponent<Button>().onClick.AddListener(Close);
        var cancelLbl = RuntimeTmpText.Create(cancelGo.transform, Loc.Get(LocKeys.ProdBudgetCancel), 12f, TextPrimary,
            FontStyles.Bold, TextAlignmentOptions.Center, "Label");
        cancelLbl.enableAutoSizing = true;
    }

    void CreateBudgetOption(Transform parent, string locKey, ProductionBudget budget, HudCardVariant variant)
    {
        var go = new GameObject("Budget_" + budget, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        HudSkinProvider.ApplyCard(go.GetComponent<Image>(), variant);
        go.AddComponent<UIButtonScale>();
        LE(go.GetComponent<RectTransform>(), 44f);
        go.GetComponent<Button>().onClick.AddListener(() => Confirm(budget));

        var lbl = RuntimeTmpText.Create(go.transform, Loc.Get(locKey), 14f, TextPrimary,
            FontStyles.Bold, TextAlignmentOptions.Center, "Label");
        lbl.enableAutoSizing = true;
        lbl.fontSizeMin = 11f;
        lbl.fontSizeMax = 15f;
    }

    void Open(MovieConfig config, Action<ProductionBudget> onConfirm)
    {
        _pendingConfig = config;
        _onConfirm = onConfirm;
        if (_titleText != null) _titleText.text = config.movieName;
        MoviePosterVisual.Apply(_posterImage, config);
        gameObject.SetActive(true);
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
        gameObject.SetActive(false);
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
