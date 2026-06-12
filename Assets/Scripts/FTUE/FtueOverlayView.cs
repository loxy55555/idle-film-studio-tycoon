using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>FTUE overlay card + tab/offer highlights (Phase 8.4).</summary>
public class FtueOverlayView : MonoBehaviour
{
    static readonly Color OverlayDim    = new Color(0f, 0f, 0f, 0.55f);
    static readonly Color CardBg        = new Color(0.08f, 0.08f, 0.16f, 0.96f);
    static readonly Color TextPrimary   = Color.white;
    static readonly Color TextSecondary = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color HighlightColor = new Color(0.18f, 0.80f, 0.44f, 0.35f);

    RectTransform _root;
    RectTransform _card;
    RectTransform _dim;
    RectTransform _escapeBar;
    TextMeshProUGUI _titleText;
    TextMeshProUGUI _bodyText;
    RectTransform _highlightLayer;
    RectTransform _activeHighlight;
    TweenPulse _highlightPulse;
    System.Action _escapeSkipAction;

    public static FtueOverlayView Create(Transform canvasTransform)
    {
        var go = new GameObject("FtueOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(FtueOverlayView));
        go.transform.SetParent(canvasTransform, false);
        var rt = go.GetComponent<RectTransform>();
        Stretch(rt);

        var canvas = go.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 9000;
        go.AddComponent<GraphicRaycaster>();

        var view = go.GetComponent<FtueOverlayView>();
        view.Build();
        go.SetActive(false);
        return view;
    }

    void Build()
    {
        _root = transform as RectTransform;

        var dim = CreatePanel(_root, "Dim", OverlayDim);
        Stretch(dim);
        _dim = dim;
        dim.GetComponent<Image>().raycastTarget = true;

        _highlightLayer = CreatePanel(_root, "HighlightLayer", Color.clear);
        Stretch(_highlightLayer);
        _highlightLayer.GetComponent<Image>().raycastTarget = false;

        _card = CreatePanel(_root, "Card", CardBg);
        var cardRT = _card;
        cardRT.anchorMin = cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(320f, 0f);
        cardRT.anchoredPosition = new Vector2(0f, 40f);
        HudSkinProvider.ApplyPanel(_card.GetComponent<Image>(), HudPanelVariant.Card);

        var vlg = _card.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.spacing = 12;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        _card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _titleText = CreateLabel(_card, "Title", string.Empty, 17f, TextPrimary, FontStyles.Bold, 24f);
        _bodyText = CreateLabel(_card, "Body", string.Empty, 13f, TextSecondary, FontStyles.Normal, 0f);
        _bodyText.enableAutoSizing = true;
        _bodyText.fontSizeMin = 11f;
        _bodyText.fontSizeMax = 14f;
        _bodyText.textWrappingMode = TextWrappingModes.Normal;

        var btnRow = CreatePanel(_card, "BtnRow", Color.clear);
        btnRow.GetComponent<Image>().raycastTarget = false;
        LE(btnRow, 40f);
        var hlg = btnRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;

        CreateButton(btnRow, "ContinueBtn", LocKeys.FtueContinue, HudButtonVariant.Success, null);
        CreateButton(btnRow, "SkipBtn", LocKeys.FtueSkip, HudButtonVariant.Ghost, null);

        BuildEscapeBar();
    }

    void BuildEscapeBar()
    {
        _escapeBar = CreatePanel(_root, "EscapeBar", Color.clear);
        _escapeBar.anchorMin = _escapeBar.anchorMax = new Vector2(1f, 1f);
        _escapeBar.pivot = new Vector2(1f, 1f);
        _escapeBar.anchoredPosition = new Vector2(-12f, -12f);
        _escapeBar.sizeDelta = new Vector2(100f, 36f);
        _escapeBar.GetComponent<Image>().raycastTarget = false;

        CreateButton(_escapeBar, "EscapeSkipBtn", LocKeys.FtueSkip, HudButtonVariant.Ghost, null);
        _escapeBar.gameObject.SetActive(false);
    }

    public void RestoreMessageCard()
    {
        if (_card != null) _card.gameObject.SetActive(true);
        if (_dim != null)
        {
            _dim.GetComponent<Image>().color = OverlayDim;
            _dim.GetComponent<Image>().raycastTarget = true;
        }
        HideEscapeBar();
    }

    public void ShowMessage(string titleKey, string bodyKey, System.Action onContinue, System.Action onSkip)
    {
        RestoreMessageCard();
        if (_titleText != null)
        {
            _titleText.text = string.IsNullOrEmpty(titleKey) ? string.Empty : Loc.Get(titleKey);
            _titleText.gameObject.SetActive(!string.IsNullOrEmpty(_titleText.text));
        }
        if (_bodyText != null) _bodyText.text = Loc.Get(bodyKey);

        WireButtons(onContinue, onSkip);
        gameObject.SetActive(true);
    }

    /// <summary>Highlight-only mode: clicks pass through; escape skip stays available.</summary>
    public void EnterHighlightPassThrough(System.Action onSkip)
    {
        HideMessageCard();
        ShowEscapeBar(onSkip);
        gameObject.SetActive(true);
    }

    public void HideMessageCard()
    {
        if (_card != null) _card.gameObject.SetActive(false);
        if (_dim != null)
        {
            _dim.GetComponent<Image>().color = Color.clear;
            _dim.GetComponent<Image>().raycastTarget = false;
        }
    }

    void ShowEscapeBar(System.Action onSkip)
    {
        _escapeSkipAction = onSkip;
        if (_escapeBar == null) return;

        var skipBtn = _escapeBar.Find("EscapeSkipBtn")?.GetComponent<Button>();
        if (skipBtn != null)
        {
            skipBtn.onClick.RemoveAllListeners();
            skipBtn.onClick.AddListener(() => _escapeSkipAction?.Invoke());
        }
        _escapeBar.gameObject.SetActive(true);
    }

    void HideEscapeBar()
    {
        _escapeSkipAction = null;
        if (_escapeBar != null)
            _escapeBar.gameObject.SetActive(false);
    }

    public void Hide()
    {
        ClearHighlight();
        HideEscapeBar();
        if (_card != null) _card.gameObject.SetActive(true);
        if (_dim != null)
        {
            _dim.GetComponent<Image>().color = OverlayDim;
            _dim.GetComponent<Image>().raycastTarget = true;
        }
        gameObject.SetActive(false);
    }

    public void HighlightRectTransform(RectTransform target)
    {
        ClearHighlight();
        if (target == null || _highlightLayer == null) return;

        var ring = CreatePanel(_highlightLayer, "TabHighlight", HighlightColor);
        _activeHighlight = ring;
        ring.SetAsFirstSibling();

        var ringImg = ring.GetComponent<Image>();
        ringImg.raycastTarget = false;

        var pulseGo = ring.gameObject;
        _highlightPulse = pulseGo.GetComponent<TweenPulse>() ?? pulseGo.AddComponent<TweenPulse>();
        _highlightPulse.Configure(0.92f, 1.06f, 0.85f);

        UpdateHighlightPosition(target);
    }

    public void RefreshHighlightPosition(RectTransform target)
    {
        UpdateHighlightPosition(target);
    }

    void UpdateHighlightPosition(RectTransform target)
    {
        if (_activeHighlight == null || target == null) return;

        var corners = new Vector3[4];
        target.GetWorldCorners(corners);
        var min = corners[0];
        var max = corners[2];
        for (int i = 0; i < 4; i++)
        {
            min = Vector3.Min(min, corners[i]);
            max = Vector3.Max(max, corners[i]);
        }

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(_highlightLayer, min, cam, out var localMin);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_highlightLayer, max, cam, out var localMax);

        _activeHighlight.anchorMin = _activeHighlight.anchorMax = new Vector2(0.5f, 0.5f);
        _activeHighlight.pivot = new Vector2(0.5f, 0.5f);
        _activeHighlight.anchoredPosition = (localMin + localMax) * 0.5f;
        _activeHighlight.sizeDelta = new Vector2(
            Mathf.Abs(localMax.x - localMin.x) + 12f,
            Mathf.Abs(localMax.y - localMin.y) + 12f);
    }

    public void ClearHighlight()
    {
        if (_activeHighlight != null)
        {
            Destroy(_activeHighlight.gameObject);
            _activeHighlight = null;
        }
        _highlightPulse = null;
    }

    void WireButtons(System.Action onContinue, System.Action onSkip)
    {
        var continueBtn = _card.Find("BtnRow/ContinueBtn")?.GetComponent<Button>();
        var skipBtn = _card.Find("BtnRow/SkipBtn")?.GetComponent<Button>();

        if (continueBtn != null)
        {
            continueBtn.onClick.RemoveAllListeners();
            continueBtn.onClick.AddListener(() => onContinue?.Invoke());
        }
        if (skipBtn != null)
        {
            skipBtn.onClick.RemoveAllListeners();
            skipBtn.onClick.AddListener(() => onSkip?.Invoke());
        }
    }

    Button CreateButton(Transform parent, string name, string locKey, HudButtonVariant variant, System.Action onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        HudSkinProvider.ApplyButton(go.GetComponent<Image>(), variant);
        go.AddComponent<UIButtonScale>();
        go.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var lbl = CreateLabel(go.transform, "Label", Loc.Get(locKey), 12f, TextPrimary, FontStyles.Bold, 32f);
        lbl.enableAutoSizing = true;
        lbl.fontSizeMin = 10f;
        lbl.fontSizeMax = 13f;

        var btn = go.GetComponent<Button>();
        if (onClick != null) btn.onClick.AddListener(() => onClick());
        return btn;
    }

    static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, float size, Color color,
        FontStyles style, float height)
    {
        var tmp = RuntimeTmpText.Create(parent, text, size, color, style, TextAlignmentOptions.Center, name);
        tmp.raycastTarget = false;
        if (height > 0f) LE(tmp.rectTransform, height);
        return tmp;
    }

    static RectTransform CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
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

/// <summary>Simple scale pulse for FTUE highlights.</summary>
public class TweenPulse : MonoBehaviour
{
    float _min = 0.95f;
    float _max = 1.05f;
    float _speed = 1.2f;
    RectTransform _rt;

    public void Configure(float min, float max, float speed)
    {
        _min = min;
        _max = max;
        _speed = speed;
        _rt = transform as RectTransform;
    }

    void Update()
    {
        if (_rt == null) _rt = transform as RectTransform;
        float t = (Mathf.Sin(Time.unscaledTime * _speed) + 1f) * 0.5f;
        float scale = Mathf.Lerp(_min, _max, t);
        _rt.localScale = Vector3.one * scale;
    }
}
