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

        _panel = CreatePanel(root, "Panel", Color.clear);
        var panelRT = _panel;
        panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(300f, 260f);
        HudSkinProvider.ApplyPanel(_panel.GetComponent<Image>(), HudPanelVariant.Card);

        var vlg = _panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(16, 16, 16, 16);
        vlg.spacing = 10;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        _titleText = RuntimeTmpText.Create(_panel, string.Empty, 16f, TextPrimary,
            FontStyles.Bold, TextAlignmentOptions.Center, "Title");
        LE(_titleText.rectTransform, 28f);

        var sub = RuntimeTmpText.Create(_panel, Loc.Get(LocKeys.RefreshChoosePayment), 11f, TextSecondary,
            FontStyles.Normal, TextAlignmentOptions.Center, "Subtitle");
        LE(sub.rectTransform, 24f);

        CreateActionButton(_panel, LocKeys.RefreshWatchAd, HudButtonVariant.Primary, OnAdClicked);
        CreateActionButton(_panel, LocKeys.RefreshSpendDiamonds, HudButtonVariant.Primary, OnDiamondClicked);

        var cancelGo = new GameObject("CancelBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        cancelGo.transform.SetParent(_panel, false);
        HudSkinProvider.ApplyButton(cancelGo.GetComponent<Image>(), HudButtonVariant.Secondary);
        cancelGo.AddComponent<UIButtonScale>();
        LE(cancelGo.GetComponent<RectTransform>(), 34f);
        cancelGo.GetComponent<Button>().onClick.AddListener(Close);
        RuntimeTmpText.Create(cancelGo.transform, Loc.Get(LocKeys.ProdBudgetCancel), 12f, TextPrimary,
            FontStyles.Bold, TextAlignmentOptions.Center, "Label");
    }

    void CreateActionButton(Transform parent, string locKey, HudButtonVariant variant, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + locKey, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        HudSkinProvider.ApplyButton(go.GetComponent<Image>(), variant);
        go.AddComponent<UIButtonScale>();
        LE(go.GetComponent<RectTransform>(), 40f);
        go.GetComponent<Button>().onClick.AddListener(onClick);
        RuntimeTmpText.Create(go.transform, Loc.Get(locKey), 13f, TextPrimary,
            FontStyles.Bold, TextAlignmentOptions.Center, "Label");
    }

    void Open(string title, int diamondCost, string placementId, Action<bool> onAd, Action onDiamonds)
    {
        _diamondCost = diamondCost;
        _placementId = placementId;
        _onAd = onAd;
        _onDiamonds = onDiamonds;
        if (_titleText != null) _titleText.text = title;
        gameObject.SetActive(true);
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
            Debug.Log("[RefreshChoice] Not enough diamonds.");
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
