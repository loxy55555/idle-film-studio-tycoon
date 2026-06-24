using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Ad or diamond payment chooser for gameplay refreshes (Phase 8.0).</summary>
public class RefreshChoiceDialogUI : MonoBehaviour
{
    public static RefreshChoiceDialogUI Instance { get; private set; }

    static readonly Color OverlayDim = new Color(0f, 0f, 0f, 0.72f);
    static readonly Color TextPrimary = Color.white;
    static readonly Color TextSecondary = new Color(0.54f, 0.54f, 0.67f);

    RectTransform _panel;
    TextMeshProUGUI _titleText;
    TextMeshProUGUI _subtitleText;
    TextMeshProUGUI _adLabel;
    Transform _diamondButtonRoot;
    TextMeshProUGUI _cancelLabel;
    Action<bool> _onAd;
    Action _onDiamonds;
    string _placementId;
    int _diamondCost;

    public static void Show(string title, int diamondCost, string placementId, Action<bool> onAd, Action onDiamonds)
    {
        EnsureInstance();
        if (Instance == null) return;
        Instance.Open(title, diamondCost, placementId, onAd, onDiamonds);
    }

    static void EnsureInstance()
    {
        if (Instance != null) return;

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("RefreshChoiceDialog", typeof(RectTransform), typeof(CanvasGroup), typeof(RefreshChoiceDialogUI));
        go.transform.SetParent(canvas.transform, false);
        Stretch(go.GetComponent<RectTransform>());
        Instance = go.GetComponent<RefreshChoiceDialogUI>();
        Instance.Build(go.transform);
        go.SetActive(false);
    }

    void Build(Transform root)
    {
        var backdrop = CreatePanel(root, "Backdrop", OverlayDim);
        Stretch(backdrop);
        backdrop.gameObject.AddComponent<Button>().onClick.AddListener(Close);

        // FASE 16.5G — compact popup: full width, height fits content, vertically centered
        _panel = CreatePanel(root, "Panel", Color.clear);
        var panelRT = _panel;
        panelRT.anchorMin = new Vector2(0.04f, 0.5f);
        panelRT.anchorMax = new Vector2(0.96f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.sizeDelta = Vector2.zero;
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

        _titleText = RuntimeTmpText.Create(_panel, string.Empty, 18f, TextPrimary,
            FontStyles.Bold, TextAlignmentOptions.Center, "Title");
        _titleText.enableAutoSizing = true;
        _titleText.fontSizeMin = 16f;
        _titleText.fontSizeMax = 20f;
        LE(_titleText.rectTransform, 40f);

        _subtitleText = RuntimeTmpText.Create(_panel, Loc.Get(LocKeys.RefreshPaymentHint), 14f, TextSecondary,
            FontStyles.Normal, TextAlignmentOptions.Center, "Subtitle");
        _subtitleText.enableAutoSizing = true;
        _subtitleText.fontSizeMin = 13f;
        _subtitleText.fontSizeMax = 16f;
        LE(_subtitleText.rectTransform, 32f);

        _adLabel = CreateTextActionButton(_panel, LocKeys.RefreshWatchAd, HudButtonVariant.Primary, OnAdClicked);
        _diamondButtonRoot = CreateDiamondActionButton(_panel, HudButtonVariant.Primary, OnDiamondClicked);

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

    TextMeshProUGUI CreateTextActionButton(Transform parent, string locKey, HudButtonVariant variant, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + locKey, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        HudSkinProvider.ApplyButton(go.GetComponent<Image>(), variant);
        go.AddComponent<UIButtonScale>();
        LE(go.GetComponent<RectTransform>(), 80f);
        go.GetComponent<Button>().onClick.AddListener(onClick);
        var lbl = RuntimeTmpText.Create(go.transform, Loc.Get(locKey), 16f, TextPrimary,
            FontStyles.Bold, TextAlignmentOptions.Center, "Label");
        lbl.enableAutoSizing = true;
        lbl.fontSizeMin = 16f;
        lbl.fontSizeMax = 18f;
        return lbl;
    }

    Transform CreateDiamondActionButton(Transform parent, HudButtonVariant variant, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_DiamondCost", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        HudSkinProvider.ApplyButton(go.GetComponent<Image>(), variant);
        go.AddComponent<UIButtonScale>();
        LE(go.GetComponent<RectTransform>(), 80f);
        go.GetComponent<Button>().onClick.AddListener(onClick);
        return go.transform;
    }

    void RefreshDiamondButton()
    {
        if (_diamondButtonRoot == null) return;
        DiamondCostButtonLayout.Build(_diamondButtonRoot, Loc.Get(LocKeys.RefreshSpendDiamonds), _diamondCost, 24f, 16f, TextPrimary);
    }

    void RefreshLocalization()
    {
        if (_subtitleText) _subtitleText.text = Loc.Get(LocKeys.RefreshPaymentHint);
        if (_adLabel) _adLabel.text = Loc.Get(LocKeys.RefreshWatchAd);
        RefreshDiamondButton();
        if (_cancelLabel) _cancelLabel.text = Loc.Get(LocKeys.ProdBudgetCancel);
    }

    void Open(string title, int diamondCost, string placementId, Action<bool> onAd, Action onDiamonds)
    {
        _diamondCost = diamondCost;
        _placementId = placementId;
        _onAd = onAd;
        _onDiamonds = onDiamonds;
        if (_titleText != null) _titleText.text = title;
        RefreshLocalization();
        gameObject.SetActive(true);
        UIAnimationService.PlayPopupOpen(_panel, GetComponent<CanvasGroup>());
    }

    void OnAdClicked()
    {
        var adCallback = _onAd;
        Close();
        GameplayRefreshService.TryShowAd(_placementId, adCallback);
    }

    void OnDiamondClicked()
    {
        if (!GameplayRefreshService.TrySpendDiamonds(_diamondCost))
        {
            Close();
            InsufficientDiamondsDialog.Show();
            return;
        }

        var diamondCallback = _onDiamonds;
        Close();
        diamondCallback?.Invoke();
    }

    void Close()
    {
        _onAd = null;
        _onDiamonds = null;
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
