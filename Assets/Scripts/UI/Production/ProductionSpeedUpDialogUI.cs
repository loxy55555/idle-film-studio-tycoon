using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>FASE 17C — Diamond speed-up chooser for active productions.</summary>
public class ProductionSpeedUpDialogUI : MonoBehaviour
{
    public static ProductionSpeedUpDialogUI Instance { get; private set; }

    static readonly Color OverlayDim = new Color(0f, 0f, 0f, 0.72f);
    static readonly Color TextPrimary = Color.white;
    static readonly Color TextSecondary = new Color(0.54f, 0.54f, 0.67f);

    RectTransform _panel;
    RectTransform _optionsRoot;
    TextMeshProUGUI _titleText;
    TextMeshProUGUI _cancelLabel;
    string _movieKey;
    float _timeLeftSeconds;
    Action _onApplied;

    public static void Show(string movieKey, float timeLeftSeconds, Action onApplied)
    {
        EnsureInstance();
        if (Instance == null) return;
        Instance.Open(movieKey, timeLeftSeconds, onApplied);
    }

    static void EnsureInstance()
    {
        if (Instance != null) return;

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("ProductionSpeedUpDialog", typeof(RectTransform), typeof(CanvasGroup), typeof(ProductionSpeedUpDialogUI));
        go.transform.SetParent(canvas.transform, false);
        Stretch(go.GetComponent<RectTransform>());
        Instance = go.GetComponent<ProductionSpeedUpDialogUI>();
        Instance.Build(go.transform);
        go.SetActive(false);
    }

    void Build(Transform root)
    {
        var backdrop = CreatePanel(root, "Backdrop", OverlayDim);
        Stretch(backdrop);
        backdrop.gameObject.AddComponent<Button>().onClick.AddListener(Close);

        _panel = CreatePanel(root, "Panel", Color.clear);
        _panel.anchorMin = new Vector2(0.04f, 0.5f);
        _panel.anchorMax = new Vector2(0.96f, 0.5f);
        _panel.pivot = new Vector2(0.5f, 0.5f);
        _panel.anchoredPosition = Vector2.zero;
        _panel.sizeDelta = Vector2.zero;
        HudSkinProvider.ApplyPanel(_panel.GetComponent<Image>(), HudPanelVariant.Card);

        var panelCsf = _panel.gameObject.AddComponent<ContentSizeFitter>();
        panelCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        panelCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var vlg = _panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(16, 16, 16, 16);
        vlg.spacing = 12;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var titleRow = new GameObject("TitleRow", typeof(RectTransform));
        titleRow.transform.SetParent(_panel, false);
        LE(titleRow.GetComponent<RectTransform>(), 40f);
        var titleHLG = titleRow.AddComponent<HorizontalLayoutGroup>();
        titleHLG.padding = new RectOffset(0, 0, 0, 0);
        titleHLG.spacing = 8;
        titleHLG.childAlignment = TextAnchor.MiddleCenter;
        titleHLG.childControlWidth = titleHLG.childControlHeight = true;
        titleHLG.childForceExpandWidth = titleHLG.childForceExpandHeight = false;

        var titleIconWrap = new GameObject("TitleIconWrap", typeof(RectTransform));
        titleIconWrap.transform.SetParent(titleRow.transform, false);
        var titleIconLE = titleIconWrap.AddComponent<LayoutElement>();
        titleIconLE.preferredWidth = titleIconLE.preferredHeight = 24f;
        titleIconLE.minWidth = titleIconLE.minHeight = 24f;
        titleIconLE.flexibleWidth = 0f;
        UIIconGraphic.Apply(
            UIIconGraphic.EnsureChildIcon(titleIconWrap.transform, "TitleIcon", 24f),
            UIIconCatalog.GetUtilitySpeedProduction());

        _titleText = RuntimeTmpText.Create(titleRow.transform, string.Empty, 18f, TextPrimary,
            FontStyles.Bold, TextAlignmentOptions.Center, "Title");
        _titleText.enableAutoSizing = true;
        _titleText.fontSizeMin = 16f;
        _titleText.fontSizeMax = 20f;
        _titleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var optionsGo = new GameObject("Options", typeof(RectTransform));
        optionsGo.transform.SetParent(_panel, false);
        _optionsRoot = optionsGo.GetComponent<RectTransform>();
        var optionsVLG = optionsGo.AddComponent<VerticalLayoutGroup>();
        optionsVLG.spacing = 8;
        optionsVLG.childControlWidth = optionsVLG.childControlHeight = true;
        optionsVLG.childForceExpandWidth = true;
        optionsVLG.childForceExpandHeight = false;

        var cancelGo = new GameObject("CancelBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        cancelGo.transform.SetParent(_panel, false);
        HudSkinProvider.ApplyButton(cancelGo.GetComponent<Image>(), HudButtonVariant.Secondary);
        cancelGo.AddComponent<UIButtonScale>();
        LE(cancelGo.GetComponent<RectTransform>(), 56f);
        cancelGo.GetComponent<Button>().onClick.AddListener(Close);
        _cancelLabel = RuntimeTmpText.Create(cancelGo.transform, Loc.Get(LocKeys.ProdBudgetCancel), 16f, TextPrimary,
            FontStyles.Bold, TextAlignmentOptions.Center, "Label");
        _cancelLabel.enableAutoSizing = true;
        _cancelLabel.fontSizeMin = 14f;
        _cancelLabel.fontSizeMax = 18f;
    }

    void Open(string movieKey, float timeLeftSeconds, Action onApplied)
    {
        _movieKey = movieKey;
        _timeLeftSeconds = timeLeftSeconds;
        _onApplied = onApplied;
        if (_titleText != null) _titleText.text = Loc.Get(LocKeys.ProdSpeedUpTitle);
        if (_cancelLabel != null) _cancelLabel.text = Loc.Get(LocKeys.ProdBudgetCancel);
        RebuildOptions();
        gameObject.SetActive(true);
        UIAnimationService.PlayPopupOpen(_panel, GetComponent<CanvasGroup>());
    }

    void RebuildOptions()
    {
        foreach (Transform child in _optionsRoot)
            Destroy(child.gameObject);

        if (ProductionSpeedUpService.IsOptionValid(_timeLeftSeconds, ProductionSpeedUpService.Reduce5MinutesSeconds))
        {
            CreateOptionButton(
                Loc.Get(LocKeys.ProdSpeedUpOption5Fmt),
                ProductionSpeedUpService.Reduce5DiamondCost,
                () => OnOptionClicked(ProductionSpeedUpService.Reduce5MinutesSeconds, ProductionSpeedUpService.Reduce5DiamondCost));
        }

        if (ProductionSpeedUpService.IsOptionValid(_timeLeftSeconds, ProductionSpeedUpService.Reduce15MinutesSeconds))
        {
            CreateOptionButton(
                Loc.Get(LocKeys.ProdSpeedUpOption15Fmt),
                ProductionSpeedUpService.Reduce15DiamondCost,
                () => OnOptionClicked(ProductionSpeedUpService.Reduce15MinutesSeconds, ProductionSpeedUpService.Reduce15DiamondCost));
        }
    }

    void CreateOptionButton(string prefixLabel, int diamondCost, Action onClick)
    {
        var go = new GameObject("SpeedUpOption", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(_optionsRoot, false);
        HudSkinProvider.ApplyButton(go.GetComponent<Image>(), HudButtonVariant.Primary);
        go.AddComponent<UIButtonScale>();
        LE(go.GetComponent<RectTransform>(), 80f);
        go.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());
        DiamondCostButtonLayout.Build(go.transform, prefixLabel, diamondCost, 24f, 16f, TextPrimary);
    }

    void OnOptionClicked(float reductionSeconds, int diamondCost)
    {
        var wallet = GameHub.Instance?.diamonds;
        if (wallet == null || wallet.Balance < diamondCost)
        {
            Close();
            InsufficientDiamondsDialog.Show();
            return;
        }

        if (!ProductionSpeedUpService.TryApply(_movieKey, reductionSeconds, diamondCost))
        {
            Close();
            InsufficientDiamondsDialog.Show();
            return;
        }

        var callback = _onApplied;
        Close();
        callback?.Invoke();
    }

    void Close()
    {
        _onApplied = null;
        _movieKey = null;
        if (!gameObject.activeSelf) return;
        var cg = GetComponent<CanvasGroup>();
        _panel?.DOKill();
        cg?.DOKill();
        UIAnimationService.PlayPopupClose(_panel, cg, () => gameObject.SetActive(false));
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
