using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen settings overlay (modal).
/// Opened via TopBar gear button → Toggle().
///
/// FASE 15.0B — functional rows:
///   • Idioma   → language picker (5 languages, PlayerPrefs persistence)
///   • Soporte  → mailto:support@idlefilmstudio.com
///   • Créditos → simple credits sub-panel
///   • Google Play Games / Cuenta / Privacidad → PRONTO badge (not yet implemented)
///   • Restaurar compras → PurchaseManager.RestorePurchases()
///
/// Language changes are applied instantly: Loc.LanguageCode updates, all stored
/// text references refresh, and the overlay rebuilds its own texts in-place.
/// The rest of the game UI uses the new language on the next scene load / panel open.
/// </summary>
public class SettingsOverlayUI : MonoBehaviour
{
    [Header("Overlay Root")]
    public CanvasGroup canvasGroup;
    public Button closeButton;

    bool _isOpen;
    bool _builtLayout;
    RectTransform _settingsPanelRT;

    // Currently-shown sub-panel (language picker or credits)
    GameObject _subPanel;

    // Stored TMP references for live language refresh
    TextMeshProUGUI _titleTMP;
    TextMeshProUGUI _bigCloseTMP;
    readonly List<(string locKey, TextMeshProUGUI tmp)> _localizedRows = new();

    (string locKey, TextMeshProUGUI indicator, bool isOn) _musicToggle;
    (string locKey, TextMeshProUGUI indicator, bool isOn) _sfxToggle;

    // ── Colours ────────────────────────────────────────────────────────

    static readonly Color BG_OVERLAY    = new Color(0f,    0f,    0f,    0.88f);
    static readonly Color BG_PANEL      = CinematicTheme.CardBg;
    static readonly Color TEXT_PRIMARY  = CinematicTheme.TextPrimary;
    static readonly Color TEXT_DIM      = CinematicTheme.SilverDim;
    static readonly Color ACCENT_HEADER = CinematicTheme.CardBg2;
    static readonly Color GREEN_BTN     = CinematicTheme.ButtonSuccess;  // BronzeBase — aligned with game palette

    // ── Lifecycle ──────────────────────────────────────────────────────

    void Awake()
    {
        if (!_builtLayout) BuildLayout();
        gameObject.SetActive(false);
        UserPrefs.OnLanguageChanged += RefreshTexts;
        UserPrefs.OnAudioPrefsChanged += RefreshAudioToggles;
    }

    void OnDestroy()
    {
        UserPrefs.OnLanguageChanged -= RefreshTexts;
        UserPrefs.OnAudioPrefsChanged -= RefreshAudioToggles;
    }

    // ── Layout ─────────────────────────────────────────────────────────

    // FASE 15.6C: Full layout redesign — visual groups, real icon, premium feel
    void BuildLayout()
    {
        _builtLayout = true;
        _localizedRows.Clear();
        _titleTMP    = null;
        _bigCloseTMP = null;

        var root = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        Stretch(root);

        // Full-screen dim backdrop — clicking it closes the overlay
        var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
        backdrop.transform.SetParent(transform, false);
        Stretch(backdrop.GetComponent<RectTransform>());
        backdrop.GetComponent<Image>().color = BG_OVERLAY;
        backdrop.GetComponent<Button>().onClick.AddListener(Close);

        // Centred card panel — slightly taller to accommodate the new grouped layout
        var panel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        _settingsPanelRT = panel.GetComponent<RectTransform>();
        _settingsPanelRT.anchorMin = new Vector2(0.08f, 0.08f);
        _settingsPanelRT.anchorMax = new Vector2(0.92f, 0.93f);
        _settingsPanelRT.offsetMin = _settingsPanelRT.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = BG_PANEL;

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.spacing = 0;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        CinematicTheme.ApplyElevationPopup(_settingsPanelRT);

        BuildHeader(panel.transform);
        AddGoldLine(panel.transform);

        // ── Grupo AUDIO ───────────────────────────────────────────────────────
        BuildGroupHeader(panel.transform, LocKeys.SettingsGroupAudio);
        _musicToggle = BuildAudioRow(panel.transform, LocKeys.SettingsMusic, UserPrefs.MusicEnabled);
        AddRowDivider(panel.transform);
        _sfxToggle = BuildAudioRow(panel.transform, LocKeys.SettingsSfx, UserPrefs.SfxEnabled);
        AddRowDivider(panel.transform);
        AddGroupSeparator(panel.transform);

        // ── Grupo IDIOMA ──────────────────────────────────────────────────────
        BuildGroupHeader(panel.transform, LocKeys.SettingsGroupLang);
        BuildFunctionalRow(panel.transform, LocKeys.SettingsLanguage, OpenLanguagePicker);
        AddRowDivider(panel.transform);
        AddGroupSeparator(panel.transform);

        // ── Grupo INFORMACIÓN ─────────────────────────────────────────────────
        BuildGroupHeader(panel.transform, LocKeys.SettingsGroupInfo);
        BuildFunctionalRow(panel.transform, LocKeys.SettingsSupport, OnSupportPressed);
        AddRowDivider(panel.transform);
        BuildFunctionalRow(panel.transform, LocKeys.SettingsCredits, OpenCredits);
        AddRowDivider(panel.transform);
        BuildFunctionalRow(panel.transform, LocKeys.SettingsRestore, OnRestorePurchases);
        AddRowDivider(panel.transform);
        BuildFunctionalRow(panel.transform, LocKeys.SettingsPrivacy, OnPrivacyPressed);
        AddRowDivider(panel.transform);
        AddGroupSeparator(panel.transform);

        AddSeparator(panel.transform, 6f);
        BuildBigCloseButton(panel.transform);
        AddSeparator(panel.transform, 10f);

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
    }

    // ── Group header — label identifying a settings category ──────────────────

    void BuildGroupHeader(Transform parent, string locKey)
    {
        var hdr = new GameObject("GrpHdr_" + locKey, typeof(RectTransform), typeof(Image));
        hdr.transform.SetParent(parent, false);
        hdr.GetComponent<Image>().color = new Color(ACCENT_HEADER.r, ACCENT_HEADER.g, ACCENT_HEADER.b, 0.55f);
        hdr.GetComponent<Image>().raycastTarget = false;
        hdr.AddComponent<LayoutElement>().preferredHeight = 42f;

        var hlg = hdr.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(24, 24, 6, 6);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        var lbl = MakeTMP(hdr.transform, "Label", Loc.Get(locKey),
            20f, FontStyles.Bold, CinematicTheme.GoldDim, TextAlignmentOptions.MidlineLeft);
        lbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        _localizedRows.Add((locKey, lbl));
    }

    // ── Audio row — functional toggle wired to UserPrefs ─────────────────────

    (string locKey, TextMeshProUGUI indicator, bool isOn) BuildAudioRow(
        Transform parent, string locKey, bool isOn)
    {
        var row = new GameObject("Row_" + locKey, typeof(RectTransform), typeof(Image));
        row.transform.SetParent(parent, false);
        row.GetComponent<Image>().color = CinematicTheme.CardBg2;
        row.GetComponent<Image>().raycastTarget = false;
        row.AddComponent<LayoutElement>().preferredHeight = 96f; // A1: scaled for mobile

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(24, 24, 10, 10);
        hlg.spacing = 16;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        var labelTmp = MakeTMP(row.transform, "Label", Loc.Get(locKey),
            28f, FontStyles.Normal, TEXT_PRIMARY, TextAlignmentOptions.MidlineLeft);
        labelTmp.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        _localizedRows.Add((locKey, labelTmp));

        var indicator = BuildCircleToggle(row.transform, isOn);
        var btn = row.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        bool isMusicRow = locKey == LocKeys.SettingsMusic;
        btn.onClick.AddListener(() =>
        {
            if (isMusicRow) UserPrefs.MusicEnabled = !UserPrefs.MusicEnabled;
            else            UserPrefs.SfxEnabled   = !UserPrefs.SfxEnabled;
            RefreshAudioToggles();
        });

        return (locKey, indicator, isOn);
    }

    void RefreshAudioToggles()
    {
        if (_musicToggle.indicator != null)
        {
            _musicToggle.isOn = UserPrefs.MusicEnabled;
            ApplyCircleToggleVisual(_musicToggle.indicator, _musicToggle.isOn);
        }
        if (_sfxToggle.indicator != null)
        {
            _sfxToggle.isOn = UserPrefs.SfxEnabled;
            ApplyCircleToggleVisual(_sfxToggle.indicator, _sfxToggle.isOn);
        }
        AudioManager.Instance?.RefreshUserPrefs();
    }

    // ── Circle toggle — ON = ● (filled), OFF = ○ (empty) ─────────────────────

    static TextMeshProUGUI BuildCircleToggle(Transform parent, bool isOn)
    {
        var go = new GameObject("CircleToggle", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = 40f;
        le.preferredHeight = 40f;
        le.flexibleWidth   = 0f;

        var indicator = go.AddComponent<TextMeshProUGUI>();
        indicator.raycastTarget = false;
        indicator.fontSize = 32f;
        indicator.fontStyle = FontStyles.Normal;
        indicator.alignment = TextAlignmentOptions.Center;
        ApplyCircleToggleVisual(indicator, isOn);
        return indicator;
    }

    static void ApplyCircleToggleVisual(TextMeshProUGUI indicator, bool isOn)
    {
        indicator.text  = isOn ? "\u25CF" : "\u25CB"; // ● / ○
        indicator.color = isOn ? CinematicTheme.GoldBright : new Color(0.40f, 0.40f, 0.40f, 1f);
    }

    // ── Functional row — arrow right, wired action ────────────────────────────

    void BuildFunctionalRow(Transform parent, string locKey, System.Action action)
    {
        var row = new GameObject("Row_" + locKey, typeof(RectTransform), typeof(Image));
        row.transform.SetParent(parent, false);
        var rowImg = row.GetComponent<Image>();
        rowImg.color = CinematicTheme.CardBg2;
        rowImg.raycastTarget = true;
        row.AddComponent<LayoutElement>().preferredHeight = 96f; // A1: scaled for mobile

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(24, 24, 10, 10);
        hlg.spacing = 16;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        var labelTmp = MakeTMP(row.transform, "Label", Loc.Get(locKey),
            28f, FontStyles.Normal, TEXT_PRIMARY, TextAlignmentOptions.MidlineLeft);
        labelTmp.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        _localizedRows.Add((locKey, labelTmp));

        var arrow = MakeTMP(row.transform, "Arrow", "›", 32f, FontStyles.Normal,
            TEXT_DIM, TextAlignmentOptions.Center);
        arrow.gameObject.AddComponent<LayoutElement>().preferredWidth = 30f;

        if (action != null)
        {
            var btn = row.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = rowImg;
            btn.onClick.AddListener(() => action());
        }
    }

    // ── Placeholder row — PRONTO badge, no action ─────────────────────────────

    void BuildPlaceholderRow(Transform parent, string locKey)
    {
        var row = new GameObject("Row_" + locKey, typeof(RectTransform), typeof(Image));
        row.transform.SetParent(parent, false);
        row.GetComponent<Image>().color = CinematicTheme.CardBg2;
        row.GetComponent<Image>().raycastTarget = false;
        row.AddComponent<LayoutElement>().preferredHeight = 76f;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(24, 24, 10, 10);
        hlg.spacing = 16;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        var labelTmp = MakeTMP(row.transform, "Label", Loc.Get(locKey),
            26f, FontStyles.Normal, TEXT_DIM, TextAlignmentOptions.MidlineLeft);
        labelTmp.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        _localizedRows.Add((locKey, labelTmp));

        var badge = new GameObject("Soon", typeof(RectTransform), typeof(Image));
        badge.transform.SetParent(row.transform, false);
        badge.GetComponent<Image>().color = CinematicTheme.BorderAccent;
        var badgeLE = badge.AddComponent<LayoutElement>();
        badgeLE.preferredWidth  = 80f;
        badgeLE.preferredHeight = 30f;
        badgeLE.flexibleWidth   = 0f;
        var badgeTxt = MakeTMP(badge.transform, "Txt", Loc.Get(LocKeys.SettingsComing),
            13f, FontStyles.Bold, CinematicTheme.SilverBase, TextAlignmentOptions.Center);
        Stretch(badgeTxt.rectTransform);
    }

    // ── Dividers / separators ─────────────────────────────────────────────────

    void AddRowDivider(Transform parent)
    {
        var div = new GameObject("Div", typeof(RectTransform), typeof(Image));
        div.transform.SetParent(parent, false);
        div.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.04f);
        div.GetComponent<Image>().raycastTarget = false;
        div.AddComponent<LayoutElement>().preferredHeight = 1f;
    }

    void AddGroupSeparator(Transform parent) => AddSeparator(parent, 10f);

    void BuildHeader(Transform parent)
    {
        var header = new GameObject("Header", typeof(RectTransform), typeof(Image));
        header.transform.SetParent(parent, false);
        header.GetComponent<Image>().color = ACCENT_HEADER;
        header.AddComponent<LayoutElement>().preferredHeight = 84f;

        var hlg = header.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(16, 12, 6, 6);
        hlg.spacing = 12;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;  // all children fill the 72px content height

        // Real settings icon — fills ~90% of content height (84 - 12 padding = 72px)
        var settingsSprite = UIIconCatalog.GetResourceSettings();
        if (settingsSprite != null)
        {
            var iconImg = UIIconGraphic.EnsureChildIcon(header.transform, "SettingsIcon", 68f);
            UIIconGraphic.Apply(iconImg, settingsSprite, CinematicTheme.GoldBright);
            var iconLE = iconImg.GetComponent<LayoutElement>() ?? iconImg.gameObject.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 68f;
            iconLE.minWidth       = 68f;
            iconLE.flexibleWidth  = 0f;
        }

        _titleTMP = MakeTMP(header.transform, "Title",
            Loc.Get(LocKeys.SettingsTitle), 22f, FontStyles.Bold, CinematicTheme.GoldBright, TextAlignmentOptions.MidlineLeft);
        _titleTMP.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        // Close button — transparent container, dominant X
        var closeGo = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        closeGo.transform.SetParent(header.transform, false);
        closeGo.GetComponent<Image>().color = Color.clear;
        var closeLE = closeGo.AddComponent<LayoutElement>();
        closeLE.preferredWidth = 64f;
        closeLE.minWidth       = 64f;
        closeLE.flexibleWidth  = 0f;

        var closeLbl = MakeTMP(closeGo.transform, "X", "X", 36f, FontStyles.Bold, TEXT_PRIMARY, TextAlignmentOptions.Center);
        Stretch(closeLbl.rectTransform);

        closeButton = closeGo.GetComponent<Button>();
        closeButton.onClick.AddListener(CloseSubPanelOrOverlay);
    }

    void BuildBigCloseButton(Transform parent)
    {
        var go = new GameObject("BigCloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 66f;

        CinematicTheme.ApplyElevationButton(go.GetComponent<RectTransform>());
        go.GetComponent<Image>().color = GREEN_BTN;     // BronzeBase — restore after elevation pass

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(16, 16, 4, 4);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = hlg.childControlHeight = true;

        _bigCloseTMP = MakeTMP(go.transform, "Lbl",
            Loc.Get(LocKeys.SettingsClose), 28f, FontStyles.Bold, TEXT_PRIMARY, TextAlignmentOptions.Center);
        _bigCloseTMP.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        go.GetComponent<Button>().onClick.AddListener(Close);
    }

    void BuildRow(Transform parent, string icon, string locKey, bool placeholder, System.Action action)
    {
        var row = new GameObject("Row_" + locKey, typeof(RectTransform), typeof(Image));
        row.transform.SetParent(parent, false);
        var rowImg = row.GetComponent<Image>();
        rowImg.color = CinematicTheme.CardBg2;
        rowImg.raycastTarget = true;
        row.AddComponent<LayoutElement>().preferredHeight = 64f;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(24, 24, 8, 8);
        hlg.spacing = 16;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandHeight = true;

        // Icon — gold for active rows, dimmed silver for placeholder
        var iconColor = placeholder ? CinematicTheme.SilverDim : CinematicTheme.GoldBright;
        var iconTmp = MakeTMP(row.transform, "Icon", icon, 22f, FontStyles.Normal, iconColor, TextAlignmentOptions.Center);
        iconTmp.gameObject.AddComponent<LayoutElement>().preferredWidth = 36f;

        // Label — stored for language refresh
        var labelTmp = MakeTMP(row.transform, "Label", Loc.Get(locKey),
            placeholder ? 18f : 20f,
            FontStyles.Normal,
            placeholder ? TEXT_DIM : TEXT_PRIMARY,
            TextAlignmentOptions.MidlineLeft);
        labelTmp.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        _localizedRows.Add((locKey, labelTmp));

        if (placeholder)
        {
            // PRONTO badge (BLOQUE 6: only on Google Play, Cuenta, Privacidad, Restaurar)
            var badge = new GameObject("Soon", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(row.transform, false);
            badge.GetComponent<Image>().color = CinematicTheme.BorderAccent;
            var badgeLE = badge.AddComponent<LayoutElement>();
            badgeLE.preferredWidth  = 80f;
            badgeLE.preferredHeight = 26f;
            badgeLE.flexibleWidth   = 0f;
            var badgeTxt = MakeTMP(badge.transform, "Txt", Loc.Get(LocKeys.SettingsComing), 11f, FontStyles.Bold, CinematicTheme.SilverBase, TextAlignmentOptions.Center);
            Stretch(badgeTxt.rectTransform);
        }
        else
        {
            // Arrow indicator
            var arrow = MakeTMP(row.transform, "Arrow", "›", 28f, FontStyles.Normal, TEXT_DIM, TextAlignmentOptions.Center);
            var arrowLE = arrow.gameObject.AddComponent<LayoutElement>();
            arrowLE.preferredWidth = 24f;
            arrowLE.flexibleWidth  = 0f;

            // Make the entire row tappable
            if (action != null)
            {
                var btn = row.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.targetGraphic = rowImg;
                btn.onClick.AddListener(() => action());
            }
        }

        // Full-width divider below the row
        var div = new GameObject("Div", typeof(RectTransform), typeof(Image));
        div.transform.SetParent(parent, false);
        div.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);
        div.AddComponent<LayoutElement>().preferredHeight = 1f;
    }

    // ── Language refresh ───────────────────────────────────────────────

    /// <summary>Updates all in-overlay text elements when UserPrefs.Language changes.</summary>
    void RefreshTexts()
    {
        if (_titleTMP   != null) _titleTMP.text   = Loc.Get(LocKeys.SettingsTitle);
        if (_bigCloseTMP != null) _bigCloseTMP.text = Loc.Get(LocKeys.SettingsClose);

        foreach (var (locKey, tmp) in _localizedRows)
            if (tmp != null) tmp.text = Loc.Get(locKey);
    }

    // ── BLOQUE 2 — Language Picker ─────────────────────────────────────

    static readonly (string code, string name)[] Languages =
    {
        ("es", "Español"),
        ("en", "English"),
        ("fr", "Français"),
        ("de", "Deutsch"),
        ("ja", "日本語"),
    };

    void OpenLanguagePicker()
    {
        DestroySubPanel();

        _subPanel = new GameObject("LangPicker", typeof(RectTransform), typeof(Image));
        _subPanel.transform.SetParent(transform, false);

        var rt = _subPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.15f);
        rt.anchorMax = new Vector2(0.9f, 0.90f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        _subPanel.GetComponent<Image>().color = BG_PANEL;

        var vlg = _subPanel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(0, 0, 0, 16);
        vlg.spacing = 0;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        BuildSubHeader(_subPanel.transform, Loc.Get(LocKeys.SettingsLanguage), CloseSubPanel);
        AddSeparator(_subPanel.transform, 8f);

        foreach (var (code, name) in Languages)
            BuildLangOption(_subPanel.transform, code, name, UserPrefs.Language == code);

        AnimateSubPanelIn(_subPanel);
    }

    void BuildLangOption(Transform parent, string code, string name, bool isActive)
    {
        var row = new GameObject("Lang_" + code, typeof(RectTransform), typeof(Image), typeof(Button));
        row.transform.SetParent(parent, false);

        var rowImg = row.GetComponent<Image>();
        rowImg.color = isActive
            ? new Color(CinematicTheme.GoldBase.r, CinematicTheme.GoldBase.g, CinematicTheme.GoldBase.b, 0.14f)
            : CinematicTheme.CardBg2;
        rowImg.raycastTarget = true;
        row.AddComponent<LayoutElement>().preferredHeight = 64f;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(20, 24, 8, 8);
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;    // prevents badge from bloating
        hlg.childForceExpandHeight = true;

        // Left gold bar for the active language — sits outside layout flow
        if (isActive)
        {
            var bar = new GameObject("ActiveBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(row.transform, false);
            var barRT = bar.GetComponent<RectTransform>();
            barRT.anchorMin = Vector2.zero;
            barRT.anchorMax = new Vector2(0f, 1f);
            barRT.pivot     = new Vector2(0f, 0.5f);
            barRT.anchoredPosition = Vector2.zero;
            barRT.sizeDelta = new Vector2(3f, 0f);
            bar.GetComponent<Image>().color = CinematicTheme.GoldBright;
            bar.AddComponent<LayoutElement>().ignoreLayout = true;
        }

        // Language code badge (ES / EN / FR / DE / JA)
        var codeBadge = new GameObject("CodeBadge", typeof(RectTransform), typeof(Image));
        codeBadge.transform.SetParent(row.transform, false);
        codeBadge.GetComponent<Image>().color = isActive ? CinematicTheme.GoldBase : CinematicTheme.BorderSubtle;
        var badgeLE = codeBadge.AddComponent<LayoutElement>();
        badgeLE.preferredWidth  = 30f;
        badgeLE.preferredHeight = 22f;
        badgeLE.flexibleWidth   = 0f;
        var badgeTxt = MakeTMP(codeBadge.transform, "Code", code.ToUpper(),
            9f, FontStyles.Bold,
            isActive ? CinematicTheme.CardBg : CinematicTheme.SilverDim,
            TextAlignmentOptions.Center);
        Stretch(badgeTxt.rectTransform);

        // Language name
        var lbl = MakeTMP(row.transform, "Lbl", name, 18f, FontStyles.Normal,
            isActive ? CinematicTheme.GoldBright : TEXT_PRIMARY, TextAlignmentOptions.MidlineLeft);
        lbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        if (isActive)
        {
            var check = MakeTMP(row.transform, "Check", "›", 20f, FontStyles.Bold,
                CinematicTheme.GoldBright, TextAlignmentOptions.Center);
            check.gameObject.AddComponent<LayoutElement>().preferredWidth = 24f;
        }

        var btn = row.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = rowImg;

        var capturedCode = code;
        btn.onClick.AddListener(() =>
        {
            UserPrefs.Language = capturedCode;
            OpenLanguagePicker();
        });

        // Divider
        var div = new GameObject("Div", typeof(RectTransform), typeof(Image));
        div.transform.SetParent(parent, false);
        div.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);
        div.AddComponent<LayoutElement>().preferredHeight = 1f;
    }

    // ── BLOQUE 4 — Credits Panel ───────────────────────────────────────

    void OpenCredits()
    {
        DestroySubPanel();

        _subPanel = new GameObject("Credits", typeof(RectTransform), typeof(Image));
        _subPanel.transform.SetParent(transform, false);

        var rt = _subPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.15f);
        rt.anchorMax = new Vector2(0.9f, 0.90f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        _subPanel.GetComponent<Image>().color = BG_PANEL;

        var vlg = _subPanel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(0, 0, 0, 32);
        vlg.spacing = 0;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        BuildSubHeader(_subPanel.transform, Loc.Get(LocKeys.SettingsCredits), CloseSubPanel);
        AddSeparator(_subPanel.transform, 48f);

        // Credits content — proper nouns and game name are not localized
        var lines = new[]
        {
            ("FILM PRODUCER TYCOON",                      28f, FontStyles.Bold,   TEXT_PRIMARY, 44f),
            ("",                                          10f, FontStyles.Normal, TEXT_DIM,      14f),
            (Loc.Get(LocKeys.CreditsCreatedBy),           16f, FontStyles.Normal, TEXT_DIM,      24f),
            ("Vosnos Games",                              26f, FontStyles.Bold,   TEXT_PRIMARY,  40f),
            ("",                                          28f, FontStyles.Normal, TEXT_DIM,      28f),
            (Loc.Get(LocKeys.CreditsPoweredBy),           15f, FontStyles.Normal, TEXT_DIM,      24f),
            ("",                                           8f, FontStyles.Normal, TEXT_DIM,       8f),
            (Loc.Format(LocKeys.CreditsVersion, "1.0"),   15f, FontStyles.Normal, TEXT_DIM,      24f),
        };

        foreach (var (text, size, style, color, height) in lines)
        {
            var lbl = MakeTMP(_subPanel.transform, "Line", text, size, style, color, TextAlignmentOptions.Center);
            lbl.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        }

        AnimateSubPanelIn(_subPanel);
    }

    // ── BLOQUE 5 — Support / Restore ───────────────────────────────────

    void OnRestorePurchases()
    {
        if (PurchaseManager.Instance == null)
        {
            AdRewardUI.ShowMessage(Loc.Get(LocKeys.IapRestoreFailed));
            return;
        }
        PurchaseManager.Instance.RestorePurchases();
    }

    void OnSupportPressed()
    {
        const string mailtoUrl = "mailto:vosnosgames@gmail.com";
        try
        {
            Application.OpenURL(mailtoUrl);
        }
        catch (System.Exception ex)
        {
            Debug.Log($"[Settings] mailto not supported on this platform: {ex.Message}");
        }
    }

    void OnPrivacyPressed()
    {
        const string privacyUrl = "https://sites.google.com/view/filmproducertycoon/inicio/privacy-policy?authuser=0";
        try
        {
            Application.OpenURL(privacyUrl);
        }
        catch (System.Exception ex)
        {
            Debug.Log($"[Settings] URL open failed: {ex.Message}");
        }
    }

    // ── Sub-panel helpers ──────────────────────────────────────────────

    /// <summary>Shared header row for sub-panels (language picker, credits).</summary>
    void BuildSubHeader(Transform parent, string title, System.Action onBack)
    {
        var hdr = new GameObject("SubHeader", typeof(RectTransform), typeof(Image));
        hdr.transform.SetParent(parent, false);
        hdr.GetComponent<Image>().color = ACCENT_HEADER;
        hdr.AddComponent<LayoutElement>().preferredHeight = 72f;

        var hlg = hdr.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(8, 16, 12, 12);
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;   // prevents back button from bloating
        hlg.childForceExpandHeight = false;  // prevents HLG from reporting flexH=1 to parent VLG

        // Back ‹ button
        var backGo = new GameObject("BackBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        backGo.transform.SetParent(hdr.transform, false);
        backGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.2f);
        var backLE = backGo.AddComponent<LayoutElement>();
        backLE.preferredWidth  = 48f;
        backLE.preferredHeight = 48f;
        backLE.flexibleWidth   = 0f;
        var backLbl = MakeTMP(backGo.transform, "Arrow", "‹", 26f, FontStyles.Normal, TEXT_PRIMARY, TextAlignmentOptions.Center);
        Stretch(backLbl.rectTransform);
        backGo.GetComponent<Button>().onClick.AddListener(() => onBack());

        // Title
        var titleTmp = MakeTMP(hdr.transform, "Title", title, 22f, FontStyles.Bold, TEXT_PRIMARY, TextAlignmentOptions.MidlineLeft);
        titleTmp.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
    }

    void AnimateSubPanelIn(GameObject panel)
    {
        var cg = panel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.DOFade(1f, 0.18f).SetUpdate(true);

        var rt = panel.GetComponent<RectTransform>();
        rt.localScale = Vector3.one * 0.94f;
        rt.DOScale(1f, 0.18f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    void DestroySubPanel()
    {
        if (_subPanel == null) return;
        Destroy(_subPanel);
        _subPanel = null;
    }

    void CloseSubPanel()
    {
        if (_subPanel == null) return;
        var toDestroy = _subPanel;
        _subPanel = null;
        var cg = toDestroy.GetComponent<CanvasGroup>() ?? toDestroy.AddComponent<CanvasGroup>();
        cg.DOFade(0f, 0.14f).SetUpdate(true).OnComplete(() =>
        {
            if (toDestroy != null) Destroy(toDestroy);
        });
    }

    void CloseSubPanelOrOverlay()
    {
        if (_subPanel != null) CloseSubPanel();
        else Close();
    }

    // ── Open / Close / Toggle ─────────────────────────────────────────

    public void Open()
    {
        if (!_builtLayout) BuildLayout();
        DestroySubPanel();
        gameObject.SetActive(true);
        _isOpen = true;
        UIAnimationService.PlayPopupOpen(_settingsPanelRT, canvasGroup);
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;
        DestroySubPanel();
        _settingsPanelRT?.DOKill();
        canvasGroup?.DOKill();
        UIAnimationService.PlayPopupClose(_settingsPanelRT, canvasGroup,
            () => gameObject.SetActive(false));
    }

    public void Toggle()
    {
        if (_isOpen) Close();
        else Open();
    }

    // ── Utilities ─────────────────────────────────────────────────────

    void AddSeparator(Transform parent, float height)
    {
        var sep = new GameObject("Sep", typeof(RectTransform));
        sep.transform.SetParent(parent, false);
        sep.AddComponent<LayoutElement>().preferredHeight = height;
    }

    void AddGoldLine(Transform parent)
    {
        var line = new GameObject("GoldLine", typeof(RectTransform), typeof(Image));
        line.transform.SetParent(parent, false);
        line.GetComponent<Image>().color = CinematicTheme.BorderGold;
        line.GetComponent<Image>().raycastTarget = false;
        line.AddComponent<LayoutElement>().preferredHeight = 2f;
    }

    static TextMeshProUGUI MakeTMP(Transform parent, string name, string text,
        float size, FontStyles style, Color color, TextAlignmentOptions alignment)
    {
        var tmp = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        tmp.transform.SetParent(parent, false);
        tmp.text      = text;
        tmp.fontSize  = size * RuntimeTmpText.MobileScale;
        tmp.fontStyle = style;
        tmp.color     = color;
        tmp.alignment = alignment;
        return tmp;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.offsetMin        = rt.offsetMax = Vector2.zero;
    }
}
