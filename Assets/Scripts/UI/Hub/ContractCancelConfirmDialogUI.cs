using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>FASE 19 — Confirm before canceling an active contract via rewarded ad.</summary>
public class ContractCancelConfirmDialogUI : MonoBehaviour
{
    public static ContractCancelConfirmDialogUI Instance { get; private set; }

    static readonly Color OverlayDim = new Color(0f, 0f, 0f, 0.72f);
    static readonly Color TextPrimary = Color.white;
    static readonly Color TextSecondary = new Color(0.54f, 0.54f, 0.67f);

    RectTransform _panel;
    TextMeshProUGUI _messageText;
    TextMeshProUGUI _cancelLabel;
    TextMeshProUGUI _confirmLabel;
    Action _onConfirm;
    Action _onCancel;

    public static void Show(bool requiresAd, Action onConfirm, Action onCancel = null)
    {
        EnsureInstance();
        if (Instance == null) return;
        Instance.Open(requiresAd, onConfirm, onCancel);
    }

    static void EnsureInstance()
    {
        if (Instance != null) return;

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("ContractCancelConfirmDialog", typeof(RectTransform), typeof(CanvasGroup), typeof(ContractCancelConfirmDialogUI));
        go.transform.SetParent(canvas.transform, false);
        Stretch(go.GetComponent<RectTransform>());
        Instance = go.GetComponent<ContractCancelConfirmDialogUI>();
        Instance.Build(go.transform);
        go.SetActive(false);
    }

    void Build(Transform root)
    {
        var backdrop = CreatePanel(root, "Backdrop", OverlayDim);
        Stretch(backdrop);
        backdrop.gameObject.AddComponent<Button>().onClick.AddListener(OnCancelClicked);

        _panel = CreatePanel(root, "Panel", Color.clear);
        var panelRT = _panel;
        panelRT.anchorMin = new Vector2(0.06f, 0.5f);
        panelRT.anchorMax = new Vector2(0.94f, 0.5f);
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

        _messageText = RuntimeTmpText.Create(_panel, string.Empty, 17f, TextPrimary,
            FontStyles.Bold, TextAlignmentOptions.Center, "Message");
        _messageText.enableAutoSizing = true;
        _messageText.fontSizeMin = 15f;
        _messageText.fontSizeMax = 19f;
        LE(_messageText.rectTransform, 56f);

        _confirmLabel = CreateActionButton(_panel, LocKeys.RefreshWatchAd, HudButtonVariant.Primary, OnConfirmClicked);
        _cancelLabel = CreateActionButton(_panel, LocKeys.ProdBudgetCancel, HudButtonVariant.Secondary, OnCancelClicked);
    }

    TextMeshProUGUI CreateActionButton(Transform parent, string locKey, HudButtonVariant variant,
        UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + locKey, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        HudSkinProvider.ApplyButton(go.GetComponent<Image>(), variant);
        go.AddComponent<UIButtonScale>();
        LE(go.GetComponent<RectTransform>(), 56f);
        go.GetComponent<Button>().onClick.AddListener(onClick);
        var lbl = RuntimeTmpText.Create(go.transform, Loc.Get(locKey), 16f, TextPrimary,
            FontStyles.Bold, TextAlignmentOptions.Center, "Label");
        lbl.enableAutoSizing = true;
        lbl.fontSizeMin = 14f;
        lbl.fontSizeMax = 18f;
        return lbl;
    }

    void Open(bool requiresAd, Action onConfirm, Action onCancel)
    {
        _onConfirm = onConfirm;
        _onCancel = onCancel;

        if (_messageText != null)
        {
            _messageText.text = requiresAd
                ? Loc.Get(LocKeys.ContractCancelWatchAdPrompt)
                : Loc.Get(LocKeys.ContractCancelConfirmNoAds);
        }

        if (_confirmLabel != null)
        {
            _confirmLabel.text = requiresAd
                ? Loc.Get(LocKeys.RefreshWatchAd)
                : Loc.Get(LocKeys.ContractCancelConfirm);
        }

        if (_cancelLabel != null)
            _cancelLabel.text = Loc.Get(LocKeys.ProdBudgetCancel);

        gameObject.SetActive(true);
        UIAnimationService.PlayPopupOpen(_panel, GetComponent<CanvasGroup>());
    }

    void OnConfirmClicked()
    {
        var cb = _onConfirm;
        Close();
        cb?.Invoke();
    }

    void OnCancelClicked()
    {
        var cb = _onCancel;
        Close();
        cb?.Invoke();
    }

    void Close()
    {
        _onConfirm = null;
        _onCancel = null;
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