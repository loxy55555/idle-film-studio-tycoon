using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FASE 16.1 — D1: Modal shown when the player attempts a diamond-costing action
/// without sufficient balance. Offers "Go to store" and "Cancel" buttons.
/// Self-instantiating singleton — call InsufficientDiamondsDialog.Show().
/// </summary>
public class InsufficientDiamondsDialog : MonoBehaviour
{
    public static InsufficientDiamondsDialog Instance { get; private set; }

    static readonly Color OverlayDim  = new Color(0f, 0f, 0f, 0.78f);
    static readonly Color TextPrimary = CinematicTheme.TextPrimary;
    static readonly Color GoldAccent  = CinematicTheme.GoldBright;

    RectTransform   _panel;
    CanvasGroup     _cg;

    System.Action _onGoToStore;

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Shows the "Not enough diamonds" dialog.
    /// If <paramref name="onGoToStore"/> is null the "Go to store" button
    /// automatically navigates to the store tab via BottomNav.
    /// </summary>
    public static void Show(System.Action onGoToStore = null)
    {
        EnsureInstance();
        if (Instance == null) return;
        Instance.Open(onGoToStore ?? NavigateToStore);
    }

    /// <summary>
    /// Default "Go to store" navigation used when no custom callback is provided.
    /// Finds the first BottomNav button whose name contains "Shop", "Tienda" or "Store"
    /// and invokes its onClick — this mirrors the app's own tab-switch gesture.
    /// </summary>
    static void NavigateToStore()
    {
        var nav = GameObject.Find("BottomNav");
        if (nav == null)
        {
            Debug.LogWarning("[InsufficientDiamondsDialog] BottomNav not found — cannot open store.");
            return;
        }
        foreach (var btn in nav.GetComponentsInChildren<Button>(true))
        {
            if (btn.name.Contains("Shop") || btn.name.Contains("Tienda") || btn.name.Contains("Store"))
            {
                btn.onClick.Invoke();
                return;
            }
        }
        Debug.LogWarning("[InsufficientDiamondsDialog] Store button not found in BottomNav.");
    }

    // ── Instance bootstrap ─────────────────────────────────────────────────────

    static void EnsureInstance()
    {
        if (Instance != null) return;

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("InsufficientDiamondsDialog",
            typeof(RectTransform), typeof(CanvasGroup), typeof(InsufficientDiamondsDialog));
        go.transform.SetParent(canvas.transform, false);
        Stretch(go.GetComponent<RectTransform>());
        Instance = go.GetComponent<InsufficientDiamondsDialog>();
        Instance._cg = go.GetComponent<CanvasGroup>();
        Instance.Build(go.transform);
        go.SetActive(false);
    }

    void Build(Transform root)
    {
        var backdrop = MakeImage(root, "Backdrop", OverlayDim);
        Stretch(backdrop);
        backdrop.gameObject.AddComponent<Button>().onClick.AddListener(Close);

        _panel = MakeImage(root, "Panel", Color.clear);
        _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0.5f);
        _panel.sizeDelta = new Vector2(300f, 220f);
        _panel.anchoredPosition = Vector2.zero;
        HudSkinProvider.ApplyPanel(_panel.GetComponent<Image>(), HudPanelVariant.Card);

        var vlg = _panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.spacing = 12;
        vlg.childAlignment     = TextAnchor.UpperCenter;
        vlg.childControlWidth  = vlg.childControlHeight  = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // Diamond icon header
        var icon = MakeTMP(_panel, "Icon", "\u25C6", 36f, FontStyles.Bold, GoldAccent, TextAlignmentOptions.Center);
        icon.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

        // Message
        var msg = MakeTMP(_panel, "Message",
            Loc.Get(LocKeys.UxNotEnoughDiamonds), 18f, FontStyles.Normal, TextPrimary, TextAlignmentOptions.Center);
        msg.textWrappingMode = TextWrappingModes.Normal;
        msg.gameObject.AddComponent<LayoutElement>().preferredHeight = 46f;

        // Spacer
        MakeSpacer(_panel, 4f);

        // "Go to store" button
        BuildBtn(_panel, LocKeys.UxGoToStore, HudButtonVariant.Primary, OnGoToStoreClicked);

        // "Cancel" button
        BuildBtn(_panel, LocKeys.UxCancelAction, HudButtonVariant.Secondary, Close);
    }

    void BuildBtn(RectTransform parent, string locKey, HudButtonVariant variant, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + locKey, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        HudSkinProvider.ApplyButton(go.GetComponent<Image>(), variant);
        go.AddComponent<UIButtonScale>();
        go.AddComponent<LayoutElement>().preferredHeight = 44f;
        go.GetComponent<Button>().onClick.AddListener(onClick);

        var lbl = MakeTMP(go.transform, "Lbl",
            Loc.Get(locKey), 16f, FontStyles.Bold, TextPrimary, TextAlignmentOptions.Center);
        Stretch(lbl.rectTransform);
    }

    // ── Logic ──────────────────────────────────────────────────────────────────

    void Open(System.Action onGoToStore)
    {
        _onGoToStore = onGoToStore;
        gameObject.SetActive(true);
        UIAnimationService.PlayPopupOpen(_panel, _cg);
    }

    void OnGoToStoreClicked()
    {
        var callback = _onGoToStore;
        Close();
        callback?.Invoke();
    }

    void Close()
    {
        _onGoToStore = null;
        if (!gameObject.activeSelf) return;
        _panel?.DOKill();
        _cg?.DOKill();
        UIAnimationService.PlayPopupClose(_panel, _cg, () => gameObject.SetActive(false));
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    static RectTransform MakeImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go.GetComponent<RectTransform>();
    }

    static TextMeshProUGUI MakeTMP(Transform parent, string name, string text,
        float size, FontStyles style, Color color, TextAlignmentOptions align)
    {
        var tmp = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        tmp.transform.SetParent(parent, false);
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.fontStyle = style;
        tmp.color     = color;
        tmp.alignment = align;
        return tmp;
    }

    static void MakeSpacer(Transform parent, float height)
    {
        var go = new GameObject("Spacer", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().preferredHeight = height;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
