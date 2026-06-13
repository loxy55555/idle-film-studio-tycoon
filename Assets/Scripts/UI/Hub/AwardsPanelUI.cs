using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Golden Stars Showcase — Phase 8.6B complete rebuild.
/// Sections: [Header] [Vitrina 4×7 grid] [Progress bar] [Next Objective] [Studio Progression]
/// All UI built at runtime, self-contained. No gameplay logic modified.
/// DOTween-ready: AnimateStarEarned(int slot) stub included.
/// </summary>
public class AwardsPanelUI : MonoBehaviour
{
    // ── Constants ─────────────────────────────────────────────────────────────
    const int SHOWCASE_COLS  = 7;
    const int SHOWCASE_ROWS  = 4;
    const int SHOWCASE_SLOTS = SHOWCASE_COLS * SHOWCASE_ROWS; // 28

    // ── Palette ──────────────────────────────────────────────────────────────
    static readonly Color BG_DEEP     = new Color(0.04f, 0.04f, 0.08f);
    static readonly Color BG_PANEL    = new Color(0.07f, 0.08f, 0.14f);
    static readonly Color BG_VITRINA  = new Color(0.05f, 0.05f, 0.10f);
    static readonly Color GOLD_BRIGHT = new Color(0.97f, 0.82f, 0.18f);
    static readonly Color GOLD_GLOW   = new Color(0.90f, 0.65f, 0.05f, 0.30f);
    static readonly Color AMBER       = new Color(0.85f, 0.55f, 0.05f);
    static readonly Color FRAME_GOLD  = new Color(0.70f, 0.55f, 0.10f);
    static readonly Color STAR_DARK   = new Color(0.20f, 0.20f, 0.26f);
    static readonly Color STAR_DIMFG  = new Color(0.28f, 0.28f, 0.35f);
    static readonly Color TEXT_PRI    = Color.white;
    static readonly Color TEXT_SEC    = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color TEXT_GOLD   = new Color(0.95f, 0.80f, 0.30f);
    static readonly Color BAR_BG      = new Color(0.09f, 0.09f, 0.18f);
    static readonly Color BTN_READY   = new Color(0.68f, 0.48f, 0.06f);
    static readonly Color BTN_WAIT    = new Color(0.14f, 0.15f, 0.24f);

    // ── Live refs ─────────────────────────────────────────────────────────────
    Image[]         _starGlows;
    TextMeshProUGUI[] _starLabels;
    RectTransform   _barFill;
    TextMeshProUGUI _progressLabel;
    TextMeshProUGUI _nextLabel;
    TextMeshProUGUI _nextSubLabel;
    Image           _claimBtnImg;
    TextMeshProUGUI _claimBtnLabel;
    Button          _claimBtn;
    TextMeshProUGUI _studioPanelLabel;

    // ── State ─────────────────────────────────────────────────────────────────
    bool _built;
    PrestigeSystem  _prestige;
    StudioManager   _studio;
    CitySystem      _city;
    StudioLevelSystem _studioLevel;
    bool _bound;

    // ── Legacy serialized refs (kept for any remaining scene references) ───────
    [HideInInspector] public TextMeshProUGUI oscarCountText;
    [HideInInspector] public TextMeshProUGUI multiplierText;
    [HideInInspector] public TextMeshProUGUI thresholdLabel;
    [HideInInspector] public Slider prestigeProgressBar;
    [HideInInspector] public TextMeshProUGUI progressText;
    [HideInInspector] public Button prestigeButton;
    [HideInInspector] public TextMeshProUGUI prestigeButtonLabel;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        GameHub.OnGameReady += Bind;
        EnsureBuilt();
    }

    void Start()
    {
        EnsureBuilt();
        Bind();
    }

    void OnEnable()
    {
        EnsureBuilt();
        Bind();
    }

    void OnDestroy()
    {
        GameHub.OnGameReady -= Bind;
        if (_prestige != null) _prestige.OnOscarGained -= OnOscarGained;
    }

    // ── Bind ──────────────────────────────────────────────────────────────────

    void Bind()
    {
        var hub = GameHub.Instance;
        if (hub == null) return;

        if (_bound && _prestige == hub.prestige) { RefreshAll(); return; }

        if (_prestige != null) _prestige.OnOscarGained -= OnOscarGained;

        _prestige    = hub.prestige;
        _studio      = hub.studio;
        _city        = hub.city;
        _studioLevel = hub.studioLevel;
        _bound       = _prestige != null;

        if (_prestige != null) _prestige.OnOscarGained += OnOscarGained;

        // Keep legacy refs wired for any external code still using them
        if (_claimBtn != null) prestigeButton = _claimBtn;
        if (_claimBtnLabel != null) prestigeButtonLabel = _claimBtnLabel;

        RefreshAll();
    }

    void OnOscarGained()
    {
        int current = _prestige?.oscars ?? 0;
        int slot    = Mathf.Clamp(current - 1, 0, SHOWCASE_SLOTS - 1);
        AnimateStarEarned(slot);
        RefreshAll();
    }

    // ── DOTween stub ─────────────────────────────────────────────────────────
    // Replace body with DOTween calls when available (8.6C+ polish pass).
    void AnimateStarEarned(int slotIndex)
    {
        Debug.Log($"[ShowcaseUI] AnimateStarEarned slot={slotIndex} — DOTween hook ready");
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    void EnsureBuilt()
    {
        if (_built) return;
        _built = true;

        // Clear any legacy children from pre-8.6B builds
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        Stretch(rt);
        var bg = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        bg.color = BG_DEEP;

        var scroll = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scroll.transform.SetParent(transform, false);
        Stretch(scroll.GetComponent<RectTransform>());
        scroll.GetComponent<Image>().color = Color.clear;
        // Parent PremiosPanel has VLG with childForceExpandHeight=false — must claim space explicitly.
        var scrollLE = scroll.AddComponent<LayoutElement>();
        scrollLE.flexibleWidth = 1f;
        scrollLE.flexibleHeight = 1f;
        var sr = scroll.GetComponent<ScrollRect>();
        sr.horizontal = false; sr.vertical = true;
        sr.scrollSensitivity = 36f; sr.inertia = true; sr.decelerationRate = 0.135f;

        var vp = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        vp.transform.SetParent(scroll.transform, false);
        Stretch(vp.GetComponent<RectTransform>());
        sr.viewport = vp.GetComponent<RectTransform>();

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(vp.transform, false);
        var cRT = content.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0f, 1f); cRT.anchorMax = new Vector2(1f, 1f);
        cRT.pivot = new Vector2(0.5f, 1f); cRT.offsetMin = cRT.offsetMax = Vector2.zero;
        sr.content = cRT;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 12, 16);
        vlg.spacing = 14;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        BuildHeader(content.transform);
        BuildVitrina(content.transform);
        BuildProgressBar(content.transform);
        BuildNextObjective(content.transform);
        BuildStudioProgression(content.transform);
    }

    // ── Section 0: Header ─────────────────────────────────────────────────────

    void BuildHeader(Transform parent)
    {
        var hdr = new GameObject("Header", typeof(RectTransform), typeof(Image));
        hdr.transform.SetParent(parent, false);
        hdr.GetComponent<Image>().color = Color.clear;
        hdr.AddComponent<LayoutElement>().preferredHeight = 62f;

        var vlg = hdr.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2; vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        // Decorative title row
        var row = new GameObject("TitleRow", typeof(RectTransform));
        row.transform.SetParent(hdr.transform, false);
        row.AddComponent<LayoutElement>().preferredHeight = 40f;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10; hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

        Tmp(row.transform, "★", 28f, GOLD_BRIGHT, FontStyles.Normal).gameObject
            .AddComponent<LayoutElement>().preferredWidth = 32f;

        var t = Tmp(row.transform, "ESTRELLAS DE ORO", 22f, TEXT_PRI, FontStyles.Bold);
        t.alignment = TextAlignmentOptions.Center;
        t.gameObject.AddComponent<LayoutElement>().flexibleWidth = 0f;

        Tmp(row.transform, "★", 28f, GOLD_BRIGHT, FontStyles.Normal).gameObject
            .AddComponent<LayoutElement>().preferredWidth = 32f;

        _studioPanelLabel = Tmp(hdr.transform, "—", 14f, TEXT_GOLD, FontStyles.Normal);
        _studioPanelLabel.alignment = TextAlignmentOptions.Center;
        _studioPanelLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
    }

    // ── Section 1: Vitrina ────────────────────────────────────────────────────

    void BuildVitrina(Transform parent)
    {
        // Outer frame — gold-tinted border
        var frame = new GameObject("VitrinaFrame", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(parent, false);
        frame.GetComponent<Image>().color = FRAME_GOLD;
        frame.AddComponent<LayoutElement>().preferredHeight = 316f;

        var frameVLG = frame.AddComponent<VerticalLayoutGroup>();
        frameVLG.padding = new RectOffset(3, 3, 3, 3);
        frameVLG.childControlWidth = frameVLG.childControlHeight = true;
        frameVLG.childForceExpandWidth = true; frameVLG.childForceExpandHeight = true;

        // Inner vitrina panel — very dark, premium
        var vitrina = new GameObject("VitrinaInner", typeof(RectTransform), typeof(Image));
        vitrina.transform.SetParent(frame.transform, false);
        vitrina.GetComponent<Image>().color = BG_VITRINA;

        var vInnerVLG = vitrina.AddComponent<VerticalLayoutGroup>();
        vInnerVLG.padding = new RectOffset(12, 12, 12, 12);
        vInnerVLG.spacing = 8;
        vInnerVLG.childControlWidth = vInnerVLG.childControlHeight = true;
        vInnerVLG.childForceExpandWidth = true; vInnerVLG.childForceExpandHeight = false;

        // Glass reflection strip at top — very subtle white horizontal line
        var shine = new GameObject("GlassShine", typeof(RectTransform), typeof(Image));
        shine.transform.SetParent(vitrina.transform, false);
        shine.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.07f);
        shine.GetComponent<Image>().raycastTarget = false;
        shine.AddComponent<LayoutElement>().preferredHeight = 3f;

        // ── Star grid: SHOWCASE_ROWS rows × SHOWCASE_COLS cols ──
        _starGlows  = new Image[SHOWCASE_SLOTS];
        _starLabels = new TextMeshProUGUI[SHOWCASE_SLOTS];
        int slot = 0;

        for (int r = 0; r < SHOWCASE_ROWS; r++)
        {
            var row = new GameObject("Row" + r, typeof(RectTransform));
            row.transform.SetParent(vitrina.transform, false);
            row.AddComponent<LayoutElement>().preferredHeight = 62f;
            var rowHLG = row.AddComponent<HorizontalLayoutGroup>();
            rowHLG.spacing = 8;
            rowHLG.childControlWidth = rowHLG.childControlHeight = true;
            rowHLG.childForceExpandWidth = true; rowHLG.childForceExpandHeight = true;

            for (int c = 0; c < SHOWCASE_COLS; c++)
            {
                var cell = new GameObject($"Star_{slot}", typeof(RectTransform), typeof(Image));
                cell.transform.SetParent(row.transform, false);
                _starGlows[slot] = cell.GetComponent<Image>();
                _starGlows[slot].raycastTarget = false;

                var lbl = Tmp(cell.transform, "★", 26f, STAR_DIMFG, FontStyles.Normal);
                lbl.alignment = TextAlignmentOptions.Center;
                lbl.raycastTarget = false;
                Stretch(lbl.rectTransform);
                _starLabels[slot] = lbl;

                slot++;
            }
        }

        // Glass overlay — barely-there white tint over the whole vitrina
        var glassOverlay = new GameObject("GlassOverlay", typeof(RectTransform), typeof(Image));
        glassOverlay.transform.SetParent(vitrina.transform, false);
        glassOverlay.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.025f);
        glassOverlay.GetComponent<Image>().raycastTarget = false;
        // Stretch to cover the full vitrina
        var goRT = glassOverlay.GetComponent<RectTransform>();
        Stretch(goRT);
        // Pull it out of the VLG by removing layout participation
        glassOverlay.AddComponent<LayoutElement>().ignoreLayout = true;
    }

    // ── Section 2: Progress bar ───────────────────────────────────────────────

    void BuildProgressBar(Transform parent)
    {
        var section = new GameObject("ProgressSection", typeof(RectTransform), typeof(Image));
        section.transform.SetParent(parent, false);
        section.GetComponent<Image>().color = BG_PANEL;
        section.AddComponent<LayoutElement>().preferredHeight = 68f;

        var vlg = section.AddComponent<VerticalLayoutGroup>();
        vlg.padding = HudLayoutConstants.SectionPadding;
        vlg.spacing = 6;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        // Label row: count on left, "ESTRELLAS" label on right
        var labelRow = new GameObject("LabelRow", typeof(RectTransform));
        labelRow.transform.SetParent(section.transform, false);
        labelRow.AddComponent<LayoutElement>().preferredHeight = 22f;
        var lHLG = labelRow.AddComponent<HorizontalLayoutGroup>();
        lHLG.childControlWidth = lHLG.childControlHeight = true;
        lHLG.childForceExpandWidth = false; lHLG.childForceExpandHeight = true;

        _progressLabel = Tmp(labelRow.transform, "0 / 28 ★", 17f, TEXT_GOLD, FontStyles.Bold);
        _progressLabel.alignment = TextAlignmentOptions.MidlineLeft;
        _progressLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var capLbl = Tmp(labelRow.transform, "ESTRELLAS DE ORO", 11f, TEXT_SEC, FontStyles.Bold);
        capLbl.alignment = TextAlignmentOptions.MidlineRight;
        capLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 160f;

        // Progress bar — amber/gold themed
        var barBg = new GameObject("BarBg", typeof(RectTransform), typeof(Image));
        barBg.transform.SetParent(section.transform, false);
        barBg.GetComponent<Image>().color = BAR_BG;
        barBg.GetComponent<Image>().raycastTarget = false;
        barBg.AddComponent<LayoutElement>().preferredHeight = 12f;

        var fill = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(barBg.transform, false);
        fill.GetComponent<Image>().color = AMBER;
        fill.GetComponent<Image>().raycastTarget = false;
        _barFill = fill.GetComponent<RectTransform>();
        SetFill(_barFill, 0f);
    }

    // ── Section 3: Next objective ─────────────────────────────────────────────

    void BuildNextObjective(Transform parent)
    {
        var section = new GameObject("NextObjectiveSection", typeof(RectTransform), typeof(Image));
        section.transform.SetParent(parent, false);
        section.GetComponent<Image>().color = BG_PANEL;
        section.AddComponent<LayoutElement>().preferredHeight = 112f;

        var vlg = section.AddComponent<VerticalLayoutGroup>();
        vlg.padding = HudLayoutConstants.TightPadding;
        vlg.spacing = 6;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        var sectionHeader = Tmp(section.transform, "SIGUIENTE ESTRELLA", 12f, TEXT_SEC, FontStyles.Bold);
        sectionHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

        _nextLabel = Tmp(section.transform, "—", 18f, TEXT_PRI, FontStyles.Bold);
        _nextLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        _nextSubLabel = Tmp(section.transform, "—", 13f, AMBER, FontStyles.Normal);
        _nextSubLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        // Claim button
        var btnGo = new GameObject("ClaimBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(section.transform, false);
        btnGo.AddComponent<LayoutElement>().preferredHeight = 46f;
        _claimBtnImg = btnGo.GetComponent<Image>();
        _claimBtnImg.color = BTN_WAIT;
        btnGo.AddComponent<UIButtonScale>();
        _claimBtn = btnGo.GetComponent<Button>();
        _claimBtn.onClick.AddListener(OnClaimClick);

        _claimBtnLabel = Tmp(btnGo.transform, "Sigue produciendo...", 14f, TEXT_PRI, FontStyles.Bold);
        _claimBtnLabel.alignment = TextAlignmentOptions.Center;
        Stretch(_claimBtnLabel.rectTransform);

        // Wire legacy refs
        prestigeButton      = _claimBtn;
        prestigeButtonLabel = _claimBtnLabel;
    }

    // ── Section 4: Studio progression ────────────────────────────────────────

    void BuildStudioProgression(Transform parent)
    {
        var section = new GameObject("StudioProgressSection", typeof(RectTransform), typeof(Image));
        section.transform.SetParent(parent, false);
        section.GetComponent<Image>().color = BG_PANEL;
        section.AddComponent<LayoutElement>().preferredHeight = 130f;

        var vlg = section.AddComponent<VerticalLayoutGroup>();
        vlg.padding = HudLayoutConstants.TightPadding;
        vlg.spacing = 8;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        Tmp(section.transform, "PROGRESO DEL ESTUDIO", 12f, TEXT_SEC, FontStyles.Bold)
            .gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

        // Divider
        var divider = new GameObject("Div", typeof(RectTransform), typeof(Image));
        divider.transform.SetParent(section.transform, false);
        divider.GetComponent<Image>().color = FRAME_GOLD;
        divider.GetComponent<Image>().raycastTarget = false;
        divider.AddComponent<LayoutElement>().preferredHeight = 1f;

        // Level info row
        var row1 = new GameObject("LevelRow", typeof(RectTransform));
        row1.transform.SetParent(section.transform, false);
        row1.AddComponent<LayoutElement>().preferredHeight = 36f;
        var r1hlg = row1.AddComponent<HorizontalLayoutGroup>();
        r1hlg.childControlWidth = r1hlg.childControlHeight = true;
        r1hlg.childForceExpandWidth = false; r1hlg.childForceExpandHeight = true;

        var installIcon = Tmp(row1.transform, "🏛", 22f, TEXT_GOLD, FontStyles.Normal);
        installIcon.gameObject.AddComponent<LayoutElement>().preferredWidth = 36f;

        var installName = Tmp(row1.transform, "—", 18f, TEXT_PRI, FontStyles.Bold);
        installName.alignment = TextAlignmentOptions.MidlineLeft;
        installName.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var starsRow = new GameObject("StarsRow", typeof(RectTransform));
        starsRow.transform.SetParent(section.transform, false);
        starsRow.AddComponent<LayoutElement>().preferredHeight = 24f;
        var srhlg = starsRow.AddComponent<HorizontalLayoutGroup>();
        srhlg.childControlWidth = srhlg.childControlHeight = true;
        srhlg.childForceExpandWidth = false; srhlg.childForceExpandHeight = true;

        var starsLbl = Tmp(starsRow.transform, "—", 14f, AMBER, FontStyles.Bold);
        starsLbl.alignment = TextAlignmentOptions.MidlineLeft;
        starsLbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var studioLvlLbl = Tmp(starsRow.transform, "—", 13f, TEXT_SEC, FontStyles.Normal);
        studioLvlLbl.alignment = TextAlignmentOptions.MidlineRight;
        studioLvlLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 140f;

        // Store refs for refresh
        var hintLbl = Tmp(section.transform, "Las Estrellas de Oro impulsan el crecimiento del estudio.",
            11f, TEXT_SEC, FontStyles.Italic);
        hintLbl.textWrappingMode = TextWrappingModes.Normal;
        hintLbl.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

        // Store for refresh — use tag names to find them
        installName.name  = "InstallName";
        starsLbl.name     = "StarsLbl";
        studioLvlLbl.name = "StudioLvlLbl";
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    void RefreshAll()
    {
        if (!_built) return;
        if (_prestige == null || _studio == null) return;

        int   currentStars = _prestige.oscars;
        float rep           = _studio.reputation;
        float threshold     = _prestige.NextOscarThreshold;
        bool  canClaim      = _prestige.CanClaimOscar(rep);

        RefreshStarGrid(currentStars);
        RefreshProgressBar(currentStars);
        RefreshNextObjective(currentStars, rep, threshold, canClaim);
        RefreshStudioPanel(currentStars);
    }

    void RefreshStarGrid(int earned)
    {
        if (_starGlows == null) return;
        int display = Mathf.Min(earned, SHOWCASE_SLOTS);

        for (int i = 0; i < SHOWCASE_SLOTS; i++)
        {
            bool lit = i < display;
            if (_starGlows[i]  != null) _starGlows[i].color  = lit ? GOLD_GLOW   : new Color(STAR_DARK.r, STAR_DARK.g, STAR_DARK.b, 0.60f);
            if (_starLabels[i] != null) _starLabels[i].color = lit ? GOLD_BRIGHT : STAR_DIMFG;
        }
    }

    void RefreshProgressBar(int earned)
    {
        float pct = SHOWCASE_SLOTS > 0 ? Mathf.Clamp01((float)earned / SHOWCASE_SLOTS) : 0f;
        if (_barFill != null)   SetFill(_barFill, pct);
        if (_progressLabel != null) _progressLabel.text = $"{earned} / {SHOWCASE_SLOTS} ★";
    }

    void RefreshNextObjective(int earned, float rep, float threshold, bool canClaim)
    {
        if (_nextLabel == null) return;

        if (earned >= SHOWCASE_SLOTS)
        {
            _nextLabel.text    = "¡Colección completa!";
            _nextSubLabel.text = "Todas las Estrellas de Oro conseguidas";
            SetClaimButton(false, "¡Imperio Cinematográfico!");
            return;
        }

        if (canClaim)
        {
            _nextLabel.text    = "¡LISTA PARA RECLAMAR!";
            _nextSubLabel.text = $"Reputación: {rep:N0} / {threshold:N0} REP ✓";
            SetClaimButton(true, "✦ CONSEGUIR ESTRELLA DE ORO");
        }
        else
        {
            _nextLabel.text    = $"Estrella #{earned + 1}";
            _nextSubLabel.text = $"REP necesaria: {rep:N0} / {threshold:N0}";
            SetClaimButton(false, "Sigue produciendo...");
        }
    }

    void RefreshStudioPanel(int earned)
    {
        if (_city == null || _studioLevel == null) return;

        var installName  = transform.GetComponentsInChildren<TextMeshProUGUI>(true);
        TextMeshProUGUI nameL = null, starsL = null, studLvlL = null;
        foreach (var t in installName)
        {
            if      (t.name == "InstallName")  nameL    = t;
            else if (t.name == "StarsLbl")     starsL   = t;
            else if (t.name == "StudioLvlLbl") studLvlL = t;
        }

        string curName   = CityLevelDatabase.GetLevel(_city.Level).displayName;
        int    nextOscars = -1;
        if (!_city.IsMaxLevel)
        {
            var nextDef = _city.GetNextDefinition();
            if (nextDef != null) nextOscars = nextDef.requiredOscars;
        }

        if (nameL    != null) nameL.text    = curName;
        if (starsL   != null)
        {
            starsL.text = nextOscars >= 0
                ? $"{earned} ★  ·  {nextOscars - earned} para desbloq. siguiente"
                : $"{earned} ★  ·  ¡Imperio máximo alcanzado!";
        }
        if (studLvlL != null)
            studLvlL.text = $"Estudio Nv. {_studioLevel.Level}";
        if (_studioPanelLabel != null)
            _studioPanelLabel.text = curName;
    }

    void SetClaimButton(bool ready, string label)
    {
        if (_claimBtnImg   != null) _claimBtnImg.color  = ready ? BTN_READY : BTN_WAIT;
        if (_claimBtn      != null) _claimBtn.interactable = ready;
        if (_claimBtnLabel != null) _claimBtnLabel.text  = label;
    }

    // ── Actions ───────────────────────────────────────────────────────────────

    void OnClaimClick()
    {
        if (_prestige == null || _studio == null) return;
        if (!_prestige.CanClaimOscar(_studio.reputation)) return;
        GameHub.Instance.ClaimOscar();
    }

    void Update()
    {
        if (!_bound) Bind();
        if (_prestige == null || _studio == null) return;
        // Throttled refresh (not every frame)
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static void SetFill(RectTransform fill, float pct)
    {
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(Mathf.Clamp01(pct), 1f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
    }

    static TextMeshProUGUI Tmp(Transform parent, string text, float size, Color color, FontStyles style)
    {
        var go = new GameObject("Tmp", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size;
        tmp.color = color; tmp.fontStyle = style;
        tmp.raycastTarget = false;
        return tmp;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
