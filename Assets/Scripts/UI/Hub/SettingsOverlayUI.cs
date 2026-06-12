using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen settings overlay (modal).
/// Shown when the ⚙ button in the TopBar is tapped.
/// Contains placeholder entries for all future settings categories.
/// Does NOT touch game logic or save data.
/// </summary>
public class SettingsOverlayUI : MonoBehaviour
{
    [Header("Overlay Root")]
    public CanvasGroup canvasGroup;
    public Button closeButton;

    bool _isOpen;

    static readonly Color BG_OVERLAY   = new Color(0f, 0f, 0f, 0.88f);
    static readonly Color BG_PANEL     = new Color(0.09f, 0.09f, 0.18f);
    static readonly Color BG_ROW       = new Color(0.14f, 0.14f, 0.26f);
    static readonly Color TEXT_PRIMARY = Color.white;
    static readonly Color TEXT_DIM     = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color ACCENT_GREEN = new Color(0.18f, 0.80f, 0.44f);

    static readonly (string icon, string label, bool placeholder)[] Entries =
    {
        ("🌐", "Idioma",                  false),
        ("🎮", "Google Play Games",       true),
        ("👤", "Cuenta",                  true),
        ("❓", "Soporte",                 true),
        ("ℹ️",  "Créditos",               false),
        ("🔒", "Política de privacidad",  true),
        ("🔄", "Restaurar compras",       true),
    };

    bool _builtLayout;

    void Awake()
    {
        if (!_builtLayout)
            BuildLayout();
        gameObject.SetActive(false);
    }

    void BuildLayout()
    {
        _builtLayout = true;

        var root = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        Stretch(root);

        // Full-screen dim backdrop
        var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
        backdrop.transform.SetParent(transform, false);
        Stretch(backdrop.GetComponent<RectTransform>());
        backdrop.GetComponent<Image>().color = BG_OVERLAY;
        backdrop.GetComponent<Button>().onClick.AddListener(Close);

        // Centered panel (80% width, auto height)
        var panel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.1f, 0.15f);
        panelRT.anchorMax = new Vector2(0.9f, 0.90f);
        panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;
        SetRounded(panelRT, 20);
        panel.GetComponent<Image>().color = BG_PANEL;

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(0, 0, 0, 8);
        vlg.spacing = 0;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Header
        var header = new GameObject("Header", typeof(RectTransform), typeof(Image));
        header.transform.SetParent(panel.transform, false);
        header.GetComponent<Image>().color = ACCENT_GREEN;
        SetRounded(header.GetComponent<RectTransform>(), 20);
        var headerLE = header.AddComponent<LayoutElement>();
        headerLE.preferredHeight = 72f;

        var headerHLG = header.AddComponent<HorizontalLayoutGroup>();
        headerHLG.padding = new RectOffset(24, 16, 12, 12);
        headerHLG.childAlignment = TextAnchor.MiddleLeft;
        headerHLG.childControlWidth = headerHLG.childControlHeight = true;
        headerHLG.childForceExpandHeight = true;

        var titleText = new GameObject("Title", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        titleText.transform.SetParent(header.transform, false);
        titleText.text = Loc.Get(LocKeys.SettingsTitle);
        titleText.fontSize = 26;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = TEXT_PRIMARY;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        // Close button in header
        var closeGo = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        closeGo.transform.SetParent(header.transform, false);
        closeGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.2f);
        SetRounded(closeGo.GetComponent<RectTransform>(), 8);
        var closeLE = closeGo.AddComponent<LayoutElement>();
        closeLE.preferredWidth = 48f;
        closeLE.preferredHeight = 48f;
        closeLE.flexibleWidth = 0f;
        var closeLbl = new GameObject("X", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        closeLbl.transform.SetParent(closeGo.transform, false);
        closeLbl.text = "✕";
        closeLbl.fontSize = 20;
        closeLbl.color = TEXT_PRIMARY;
        closeLbl.alignment = TextAlignmentOptions.Center;
        Stretch(closeLbl.rectTransform);
        closeButton = closeGo.GetComponent<Button>();
        closeButton.onClick.AddListener(Close);

        // Row separator
        AddSeparator(panel.transform, 12);

        // Settings rows
        foreach (var (icon, label, isPlaceholder) in Entries)
            BuildRow(panel.transform, icon, label, isPlaceholder);

        // Bottom close button
        AddSeparator(panel.transform, 16);
        var bigCloseGo = new GameObject("BigCloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        bigCloseGo.transform.SetParent(panel.transform, false);
        bigCloseGo.GetComponent<Image>().color = new Color(0.18f, 0.50f, 0.35f);
        SetRounded(bigCloseGo.GetComponent<RectTransform>(), 12);
        var bigCloseLE = bigCloseGo.AddComponent<LayoutElement>();
        bigCloseLE.preferredHeight = 54f;
        bigCloseLE.flexibleWidth = 1f;
        var bigCloseMx = bigCloseGo.AddComponent<HorizontalLayoutGroup>();
        bigCloseMx.padding = new RectOffset(16, 16, 4, 4);
        bigCloseMx.childAlignment = TextAnchor.MiddleCenter;

        var bigCloseLbl = new GameObject("Lbl", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        bigCloseLbl.transform.SetParent(bigCloseGo.transform, false);
        bigCloseLbl.text = Loc.Get(LocKeys.SettingsClose);
        bigCloseLbl.fontSize = 20;
        bigCloseLbl.fontStyle = FontStyles.Bold;
        bigCloseLbl.color = TEXT_PRIMARY;
        bigCloseLbl.alignment = TextAlignmentOptions.Center;
        bigCloseLbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        bigCloseGo.GetComponent<Button>().onClick.AddListener(Close);

        AddSeparator(panel.transform, 8);

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
    }

    void BuildRow(Transform parent, string icon, string label, bool placeholder)
    {
        var row = new GameObject("Row_" + label, typeof(RectTransform), typeof(Image));
        row.transform.SetParent(parent, false);
        row.GetComponent<Image>().color = Color.clear;
        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 64f;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(24, 24, 8, 8);
        hlg.spacing = 16;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandHeight = true;

        var iconText = new GameObject("Icon", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        iconText.transform.SetParent(row.transform, false);
        iconText.text = icon;
        iconText.fontSize = 24;
        iconText.alignment = TextAlignmentOptions.Center;
        var iconLE = iconText.gameObject.AddComponent<LayoutElement>();
        iconLE.preferredWidth = 36f;
        iconLE.flexibleWidth = 0f;

        var labelText = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        labelText.transform.SetParent(row.transform, false);
        labelText.text = label;
        labelText.fontSize = placeholder ? 18 : 20;
        labelText.color = placeholder ? TEXT_DIM : TEXT_PRIMARY;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        if (placeholder)
        {
            var badge = new GameObject("Soon", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(row.transform, false);
            badge.GetComponent<Image>().color = new Color(0.20f, 0.20f, 0.35f);
            SetRounded(badge.GetComponent<RectTransform>(), 6);
            var badgeLE = badge.AddComponent<LayoutElement>();
            badgeLE.preferredWidth = 80f;
            badgeLE.preferredHeight = 26f;
            badgeLE.flexibleWidth = 0f;
            var badgeTxt = new GameObject("Txt", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            badgeTxt.transform.SetParent(badge.transform, false);
            badgeTxt.text = "PRONTO";
            badgeTxt.fontSize = 11;
            badgeTxt.fontStyle = FontStyles.Bold;
            badgeTxt.color = TEXT_DIM;
            badgeTxt.alignment = TextAlignmentOptions.Center;
            Stretch(badgeTxt.rectTransform);
        }
        else
        {
            var arrow = new GameObject("Arrow", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            arrow.transform.SetParent(row.transform, false);
            arrow.text = "›";
            arrow.fontSize = 28;
            arrow.color = TEXT_DIM;
            arrow.alignment = TextAlignmentOptions.Center;
            var arrowLE = arrow.gameObject.AddComponent<LayoutElement>();
            arrowLE.preferredWidth = 24f;
            arrowLE.flexibleWidth = 0f;
        }

        // Full-row divider
        var div = new GameObject("Div", typeof(RectTransform), typeof(Image));
        div.transform.SetParent(parent, false);
        div.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);
        div.AddComponent<LayoutElement>().preferredHeight = 1f;
    }

    void AddSeparator(Transform parent, float height)
    {
        var sep = new GameObject("Sep", typeof(RectTransform));
        sep.transform.SetParent(parent, false);
        sep.AddComponent<LayoutElement>().preferredHeight = height;
    }

    public void Open()
    {
        if (!_builtLayout) BuildLayout();
        gameObject.SetActive(true);
        _isOpen = true;
    }

    public void Close()
    {
        gameObject.SetActive(false);
        _isOpen = false;
    }

    public void Toggle()
    {
        if (_isOpen) Close();
        else Open();
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void SetRounded(RectTransform rt, float radius)
    {
        var img = rt.GetComponent<Image>();
        if (img == null) return;
        img.sprite = null;
        img.type   = Image.Type.Sliced;
    }
}
