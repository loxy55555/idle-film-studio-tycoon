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
    const int SHOWCASE_COLS  = 4;
    const int SHOWCASE_ROWS  = 7;
    const int SHOWCASE_SLOTS = SHOWCASE_COLS * SHOWCASE_ROWS; // 28
    const float SHOWCASE_CELL_HEIGHT   = 96f;
    const float SHOWCASE_CELL_SPACING  = 0f;
    const float SHOWCASE_FRAME_PADDING = 0f;
    const float SHOWCASE_FRAME_BORDER  = 0f;
    const float SHOWCASE_STAR_FONT     = 46f;

    // ── Palette (aligned with CinematicTheme) ────────────────────────────────
    static readonly Color BG_DEEP     = CinematicTheme.DeepBg;
    static readonly Color BG_PANEL    = CinematicTheme.CardBg;
    static readonly Color BG_VITRINA  = CinematicTheme.PanelBg;
    static readonly Color GOLD_BRIGHT = CinematicTheme.GoldBright;
    static readonly Color GOLD_GLOW   = new Color(CinematicTheme.GoldBase.r, CinematicTheme.GoldBase.g, CinematicTheme.GoldBase.b, 0.28f);
    static readonly Color AMBER       = CinematicTheme.GoldBase;
    static readonly Color FRAME_GOLD  = CinematicTheme.BorderGold;
    static readonly Color STAR_DARK   = CinematicTheme.CardBg2;
    static readonly Color STAR_DIMFG  = CinematicTheme.TextDim;
    static readonly Color SLOT_LOCKED_BG  = new Color(CinematicTheme.CardBg2.r, CinematicTheme.CardBg2.g, CinematicTheme.CardBg2.b, 0.92f);
    // FASE 15.7B — earned cells: dark cinematic black; star icon is the hero, not the tile
    static readonly Color SLOT_EARNED_BG  = CinematicTheme.DeepBg;
    static readonly Color FRAME_LOCKED    = CinematicTheme.BorderSubtle;
    static readonly Color STAR_LOCKED_FG  = new Color(CinematicTheme.SilverDim.r, CinematicTheme.SilverDim.g, CinematicTheme.SilverDim.b, 0.62f);
    static readonly Color TEXT_PRI    = CinematicTheme.TextPrimary;
    static readonly Color TEXT_SEC    = CinematicTheme.TextSecondary;
    static readonly Color TEXT_GOLD   = CinematicTheme.GoldBright;
    static readonly Color BAR_BG      = CinematicTheme.DeepBg;
    static readonly Color BTN_READY   = CinematicTheme.GoldBase;
    static readonly Color BTN_WAIT    = CinematicTheme.CardBg2;

    // ── Live refs ─────────────────────────────────────────────────────────────
    LayoutElement   _vitrinaFrameLE;
    Image[]         _slotBgs;
    Image[]         _slotFrames;
    Image[]         _starGlows;
    Image[]         _starIcons;
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
    ScrollRect _scrollRect;
    RectTransform _scrollContent;

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
        UserPrefs.OnLanguageChanged += RefreshLocalization;
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
        PatchCompactVitrina();
        ResetScrollToTop();
        // On every re-entry, force LateUpdate to re-sync the responsive vitrina height.
        // RefreshStarGrid/PatchCompactVitrina reset it to a static 672px, but LateUpdate
        // would normally skip the fix because grid cellSize hasn't changed.
        _vitrinaLayoutDirty = true;
    }

    void OnDestroy()
    {
        GameHub.OnGameReady -= Bind;
        UserPrefs.OnLanguageChanged -= RefreshLocalization;
        if (_prestige != null) _prestige.OnOscarGained -= OnOscarGained;
    }

    public void RefreshLocalization()
    {
        _built = false;
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var ch = transform.GetChild(i);
            if (Application.isPlaying) Destroy(ch.gameObject);
            else DestroyImmediate(ch.gameObject);
        }
        EnsureBuilt();
        Bind();
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
        PatchCompactVitrina();
    }

    void OnOscarGained()
    {
        int current = _prestige?.oscars ?? 0;
        int slot    = Mathf.Clamp(current - 1, 0, SHOWCASE_SLOTS - 1);

        // RefreshAll updates grid state first; AnimateStarEarned then animates the glow
        // on the already-updated slot — prevents DOTween from being overridden by RefreshAll.
        RefreshAll();
        AnimateStarEarned(slot);
    }

    // ── DOTween stub ─────────────────────────────────────────────────────────
    // Replace body with DOTween calls when available (8.6C+ polish pass).
    void AnimateStarEarned(int slotIndex)
    {
        if (_starGlows == null || slotIndex < 0 || slotIndex >= _starGlows.Length) return;
        var glow = _starGlows[slotIndex];
        TextMeshProUGUI label = _starLabels != null && slotIndex < _starLabels.Length ? _starLabels[slotIndex] : null;
        var rt = glow != null ? glow.rectTransform : label?.rectTransform;
        UIAnimationService.PlayStarEarned(rt, glow, label);
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    void EnsureBuilt()
    {
        if (_built) return;
        _built = true;

        // BUG FIX: DestroyImmediate in Play mode immediately destroys objects, breaking
        // any active DOTween animations that hold references to them.
        // Use Destroy() (deferred) in Play mode; DestroyImmediate only in edit mode.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var ch = transform.GetChild(i);
            if (Application.isPlaying) Destroy(ch.gameObject);
            else DestroyImmediate(ch.gameObject);
        }

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
        _scrollRect = sr;

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
        _scrollContent = cRT;

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

        Tmp(row.transform, "●", 28f, GOLD_BRIGHT, FontStyles.Normal).gameObject
            .AddComponent<LayoutElement>().preferredWidth = 36f;

        var t = Tmp(row.transform, Loc.Get(LocKeys.AwardsTitle), 22f, TEXT_PRI, FontStyles.Bold);
        t.alignment = TextAlignmentOptions.Center;
        t.gameObject.AddComponent<LayoutElement>().flexibleWidth = 0f;

        Tmp(row.transform, "●", 28f, GOLD_BRIGHT, FontStyles.Normal).gameObject
            .AddComponent<LayoutElement>().preferredWidth = 36f;

        _studioPanelLabel = Tmp(hdr.transform, "—", 14f, TEXT_GOLD, FontStyles.Normal);
        _studioPanelLabel.alignment = TextAlignmentOptions.Center;
        _studioPanelLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
    }

    // ── Section 1: Vitrina ────────────────────────────────────────────────────

    GridLayoutGroup _vitrinaGrid;

    static float ComputeVitrinaInnerHeight()
    {
        float rowsHeight = SHOWCASE_ROWS * SHOWCASE_CELL_HEIGHT;
        float glgGaps = (SHOWCASE_ROWS - 1) * SHOWCASE_CELL_SPACING;
        return rowsHeight + glgGaps + SHOWCASE_FRAME_PADDING * 2f;
    }

    static float ComputeVitrinaFrameHeight()
        => ComputeVitrinaInnerHeight() + SHOWCASE_FRAME_BORDER * 2f;

    void BuildVitrina(Transform parent)
    {
        // Outer frame — gold-tinted border; height drives panel growth (not just star font).
        var frame = new GameObject("VitrinaFrame", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(parent, false);
        frame.GetComponent<Image>().color = BG_VITRINA;
        _vitrinaFrameLE = frame.AddComponent<LayoutElement>();
        ApplyVitrinaFrameLayout(_vitrinaFrameLE);

        var frameVLG = frame.AddComponent<VerticalLayoutGroup>();
        frameVLG.padding = new RectOffset(
            (int)SHOWCASE_FRAME_BORDER, (int)SHOWCASE_FRAME_BORDER,
            (int)SHOWCASE_FRAME_BORDER, (int)SHOWCASE_FRAME_BORDER);
        frameVLG.childControlWidth = frameVLG.childControlHeight = true;
        frameVLG.childForceExpandWidth = true;
        frameVLG.childForceExpandHeight = false;

        // Inner vitrina panel — collection backdrop
        var vitrina = new GameObject("VitrinaInner", typeof(RectTransform), typeof(Image));
        vitrina.transform.SetParent(frame.transform, false);
        vitrina.GetComponent<Image>().color = BG_VITRINA;
        vitrina.AddComponent<LayoutElement>().preferredHeight = ComputeVitrinaInnerHeight();

        // ── Star grid: SHOWCASE_ROWS rows × SHOWCASE_COLS cols (fixed square cells, single layout group) ──
        _slotBgs    = new Image[SHOWCASE_SLOTS];
        _slotFrames = new Image[SHOWCASE_SLOTS];
        _starGlows  = new Image[SHOWCASE_SLOTS];
        _starIcons  = new Image[SHOWCASE_SLOTS];
        _starLabels = new TextMeshProUGUI[SHOWCASE_SLOTS];
        int slot = 0;

        _vitrinaGrid = vitrina.AddComponent<GridLayoutGroup>();
        _vitrinaGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        _vitrinaGrid.constraintCount = SHOWCASE_COLS;
        _vitrinaGrid.cellSize = new Vector2(SHOWCASE_CELL_HEIGHT, SHOWCASE_CELL_HEIGHT);
        _vitrinaGrid.spacing = new Vector2(SHOWCASE_CELL_SPACING, SHOWCASE_CELL_SPACING);
        _vitrinaGrid.childAlignment = TextAnchor.UpperLeft;
        _vitrinaGrid.padding = new RectOffset(0, 0, 0, 0);

        for (int i = 0; i < SHOWCASE_SLOTS; i++)
        {
            BuildStarCell(vitrina.transform, slot);
            slot++;
        }
    }

    void LateUpdate()
    {
        if (_vitrinaGrid == null) return;
        var vitrinaRt = _vitrinaGrid.transform as RectTransform;
        if (vitrinaRt == null) return;
        // Read parent (VitrinaFrame) width — vitrina's own rect is constrained by GridLayoutGroup preferred size
        var parentRt = vitrinaRt.parent as RectTransform;
        float availW = parentRt != null ? parentRt.rect.width : vitrinaRt.rect.width;
        if (availW < 10f) return;
        float cellW = (availW - SHOWCASE_CELL_SPACING * (SHOWCASE_COLS - 1)) / SHOWCASE_COLS;
        if (cellW < 10f) return;
        // Skip only when cell size is already correct AND no re-entry sync is pending.
        // _vitrinaLayoutDirty is set on every OnEnable so the frame height is always
        // corrected after RefreshStarGrid resets it to the static 672px constant.
        if (!_vitrinaLayoutDirty && Mathf.Approximately(_vitrinaGrid.cellSize.x, cellW)) return;
        _vitrinaLayoutDirty = false;
        // Square cells that fill the full width
        _vitrinaGrid.cellSize = new Vector2(cellW, cellW);
        // Update the vitrina frame's preferred height accordingly
        float totalH = SHOWCASE_ROWS * cellW + (SHOWCASE_ROWS - 1) * SHOWCASE_CELL_SPACING;
        if (_vitrinaFrameLE != null)
        {
            _vitrinaFrameLE.preferredHeight = totalH;
            _vitrinaFrameLE.minHeight = totalH;
        }
        // Resize star icons to match the new cell size so they fill the cell properly
        float iconSize = cellW - 4f;
        if (_starIcons != null)
        {
            foreach (var icon in _starIcons)
            {
                if (icon == null) continue;
                CenterSquare(icon.rectTransform, iconSize);
            }
        }
        if (_starLabels != null)
        {
            foreach (var lbl in _starLabels)
            {
                if (lbl == null) continue;
                lbl.fontSize = iconSize * 0.55f;  // scale font with cell
            }
        }
    }

    void BuildStarCell(Transform row, int slotIndex)
    {
        var cell = new GameObject($"Star_{slotIndex}", typeof(RectTransform));
        cell.transform.SetParent(row, false);

        var cellLE = cell.AddComponent<LayoutElement>();
        cellLE.flexibleWidth = 0f;
        cellLE.minWidth = SHOWCASE_CELL_HEIGHT;
        cellLE.preferredWidth = SHOWCASE_CELL_HEIGHT;
        cellLE.preferredHeight = SHOWCASE_CELL_HEIGHT;
        cellLE.minHeight = SHOWCASE_CELL_HEIGHT;

        var bg = new GameObject("SlotBg", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(cell.transform, false);
        Stretch(bg.GetComponent<RectTransform>());
        _slotBgs[slotIndex] = bg.GetComponent<Image>();
        _slotBgs[slotIndex].raycastTarget = false;

        var frame = new GameObject("SlotFrame", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(cell.transform, false);
        Stretch(frame.GetComponent<RectTransform>());
        _slotFrames[slotIndex] = frame.GetComponent<Image>();
        _slotFrames[slotIndex].color = Color.clear;
        _slotFrames[slotIndex].raycastTarget = false;

        var glow = new GameObject("Glow", typeof(RectTransform), typeof(Image));
        glow.transform.SetParent(cell.transform, false);
        Stretch(glow.GetComponent<RectTransform>());
        _starGlows[slotIndex] = glow.GetComponent<Image>();
        _starGlows[slotIndex].color = Color.clear;
        _starGlows[slotIndex].raycastTarget = false;

        var lbl = Tmp(cell.transform, "·", SHOWCASE_STAR_FONT, STAR_LOCKED_FG, FontStyles.Normal);
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.raycastTarget = false;
        lbl.enableAutoSizing = false;
        lbl.margin = Vector4.zero;
        Stretch(lbl.rectTransform);
        _starLabels[slotIndex] = lbl;

        var iconWrap = new GameObject("StarIconWrap", typeof(RectTransform));
        iconWrap.transform.SetParent(cell.transform, false);
        Stretch(iconWrap.GetComponent<RectTransform>());
        var icon = UIIconGraphic.EnsureChildIcon(iconWrap.transform, "StarIcon", SHOWCASE_CELL_HEIGHT - 8f);
        CenterSquare(icon.rectTransform, SHOWCASE_CELL_HEIGHT - 8f);
        icon.preserveAspect = true;
        _starIcons[slotIndex] = icon;
    }

    static void ApplyVitrinaFrameLayout(LayoutElement le)
    {
        float h = ComputeVitrinaFrameHeight();
        le.preferredHeight = h;
        le.minHeight = h;
    }

    // ── Section 2: Progress bar ───────────────────────────────────────────────

    void BuildProgressBar(Transform parent)
    {
        var section = new GameObject("ProgressSection", typeof(RectTransform), typeof(Image));
        section.transform.SetParent(parent, false);
        section.GetComponent<Image>().color = BG_PANEL;
        section.AddComponent<LayoutElement>().preferredHeight = 68f;
        CinematicTheme.ApplyElevationPanel(section.GetComponent<RectTransform>());

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

        _progressLabel = Tmp(labelRow.transform, "0 / 28 OSC", 17f, TEXT_GOLD, FontStyles.Bold);
        _progressLabel.alignment = TextAlignmentOptions.MidlineLeft;
        _progressLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var capLbl = Tmp(labelRow.transform, Loc.Get(LocKeys.AwardsTitle), 14f, TEXT_SEC, FontStyles.Bold);
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
        CinematicTheme.ApplyElevationPanel(section.GetComponent<RectTransform>());

        var vlg = section.AddComponent<VerticalLayoutGroup>();
        vlg.padding = HudLayoutConstants.TightPadding;
        vlg.spacing = 6;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        var sectionHeader = Tmp(section.transform, Loc.Get(LocKeys.AwardsNextStar), 14f, TEXT_SEC, FontStyles.Bold);
        sectionHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

        _nextLabel = Tmp(section.transform, "—", 18f, TEXT_PRI, FontStyles.Bold);
        _nextLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        _nextSubLabel = Tmp(section.transform, "—", 14f, AMBER, FontStyles.Normal);
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

        _claimBtnLabel = Tmp(btnGo.transform, Loc.Get(LocKeys.AwardsKeepProducing), 14f, TEXT_PRI, FontStyles.Bold);
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
        CinematicTheme.ApplyElevationPanel(section.GetComponent<RectTransform>());

        var vlg = section.AddComponent<VerticalLayoutGroup>();
        vlg.padding = HudLayoutConstants.TightPadding;
        vlg.spacing = 8;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        Tmp(section.transform, Loc.Get(LocKeys.AwardsProgressSect), 12f, TEXT_SEC, FontStyles.Bold)
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

        // U+2605 "★" is not in the project TMP font atlas — it renders as a white square.
        // Use the award star sprite from UIIconCatalog instead of a TMP character.
        var levelIconGo = new GameObject("LevelIcon", typeof(RectTransform), typeof(Image));
        levelIconGo.transform.SetParent(row1.transform, false);
        var levelIconImg = levelIconGo.GetComponent<Image>();
        levelIconImg.raycastTarget = false;
        levelIconImg.preserveAspect = true;
        UIIconGraphic.Apply(levelIconImg, UIIconCatalog.GetAwardStar());
        levelIconGo.AddComponent<LayoutElement>().preferredWidth = 36f;

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

        var studioLvlLbl = Tmp(starsRow.transform, "—", 14f, TEXT_SEC, FontStyles.Normal);
        studioLvlLbl.alignment = TextAlignmentOptions.MidlineRight;
        studioLvlLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 140f;

        // Store refs for refresh
        var hintLbl = Tmp(section.transform, Loc.Get(LocKeys.AwardsHint),
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
        if (_starLabels == null) return;
        int display = Mathf.Min(earned, SHOWCASE_SLOTS);

        for (int i = 0; i < SHOWCASE_SLOTS; i++)
        {
            bool lit = i < display;

            // FASE 15.7B — unlocked: black tile; locked: transparent (embossed sprite owns the tile)
            if (_slotBgs != null && _slotBgs[i] != null)
            {
                if (lit) HudSkinUtil.ApplyPremiumSurface(_slotBgs[i], SLOT_EARNED_BG);
                else _slotBgs[i].color = Color.clear;
            }

            if (_slotFrames != null && _slotFrames[i] != null)
                _slotFrames[i].color = Color.clear;

            // No full-cell gold wash — keeps earned tiles dark; glow only via earn animation
            if (_starGlows != null && _starGlows[i] != null)
                _starGlows[i].color = Color.clear;

            if (_starLabels[i] != null)
            {
                _starLabels[i].text = lit ? "●" : "·";
                _starLabels[i].color = lit ? GOLD_BRIGHT : STAR_LOCKED_FG;
                _starLabels[i].fontStyle = lit ? FontStyles.Bold : FontStyles.Normal;
                _starLabels[i].alpha = 1f;
            }

            if (_starIcons != null && _starIcons[i] != null)
            {
                var sprite = lit ? UIIconCatalog.GetAwardStar() : UIIconCatalog.GetAwardStarLocked();
                UIIconGraphic.Apply(_starIcons[i], sprite, lit ? Color.white : STAR_LOCKED_FG);
                _starIcons[i].preserveAspect = true;
                if (sprite != null && _starLabels[i] != null)
                    _starLabels[i].gameObject.SetActive(false);
                else if (_starLabels[i] != null)
                    _starLabels[i].gameObject.SetActive(true);
            }
        }

        if (_vitrinaFrameLE != null)
            ApplyVitrinaFrameLayout(_vitrinaFrameLE);
    }

    void RefreshProgressBar(int earned)
    {
        float pct = SHOWCASE_SLOTS > 0 ? Mathf.Clamp01((float)earned / SHOWCASE_SLOTS) : 0f;
        if (_barFill != null)   SetFill(_barFill, pct);
        if (_progressLabel != null) _progressLabel.text = $"{earned} / {SHOWCASE_SLOTS}";
    }

    void RefreshNextObjective(int earned, float rep, float threshold, bool canClaim)
    {
        if (_nextLabel == null) return;

        if (earned >= SHOWCASE_SLOTS)
        {
            _nextLabel.text    = Loc.Get(LocKeys.AwardsComplete);
            _nextSubLabel.text = Loc.Get(LocKeys.AwardsAllEarned);
            SetClaimButton(false, Loc.Get(LocKeys.AwardsEmpireMax));
            return;
        }

        if (canClaim)
        {
            _nextLabel.text    = Loc.Get(LocKeys.AwardsReadyClaim);
            _nextSubLabel.text = Loc.Format(LocKeys.AwardsRepReadyFmt, rep, threshold);
            SetClaimButton(true, Loc.Get(LocKeys.AwardsClaimBtn));
        }
        else
        {
            _nextLabel.text    = Loc.Format(LocKeys.AwardsStarTitleFmt, earned + 1);
            _nextSubLabel.text = string.Format(Loc.Get(LocKeys.AwardsRepNeeded), rep, threshold);
            SetClaimButton(false, Loc.Get(LocKeys.AwardsKeepProducing));
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

        string curName   = CityLevelDatabase.GetLocalizedName(_city.Level);
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
                ? Loc.Format(LocKeys.AwardsUnlockHintFmt, earned, nextOscars - earned)
                : $"{earned}  ·  {Loc.Get(LocKeys.AwardsEmpireMax)}";
        }
        if (studLvlL != null)
            studLvlL.text = string.Format(Loc.Get(LocKeys.AwardsStudioLevel), _studioLevel.Level);
        if (_studioPanelLabel != null)
            _studioPanelLabel.text = curName;
    }

    void SetClaimButton(bool ready, string label)
    {
        if (_claimBtnImg   != null)
        {
            _claimBtnImg.color = ready ? BTN_READY : BTN_WAIT;
            if (ready)
                CinematicTheme.ApplyElevationButton(_claimBtnImg.rectTransform);
            else
                CinematicTheme.RemoveMaterialLayers(_claimBtnImg.rectTransform);
        }
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

    /// <summary>Phase 13.4C — reset scroll position to top and force layout rebuild on re-entry.</summary>
    void ResetScrollToTop()
    {
        // Recover refs if they were lost (e.g. domain reload in editor)
        if (_scrollRect == null)
            _scrollRect = GetComponentInChildren<ScrollRect>(true);
        if (_scrollContent == null && _scrollRect != null)
            _scrollContent = _scrollRect.content;

        if (_scrollRect != null)
        {
            _scrollRect.StopMovement();
            _scrollRect.normalizedPosition = new Vector2(0f, 1f);
        }

        if (_scrollContent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollContent);
    }

    bool _vitrinaCompactPatched;
    // Set to true on every OnEnable so LateUpdate bypasses the cellSize early-exit guard
    // and re-syncs the vitrina frame height (root-cause fix for re-entry layout break).
    bool _vitrinaLayoutDirty;

    /// <summary>Phase 13.3C — remove glass overlays and gaps on already-built vitrinas.</summary>
    void PatchCompactVitrina()
    {
        // Allow re-run on each OnEnable so grid layout is always correct after re-entry.
        if (_vitrinaCompactPatched && _vitrinaGrid != null
            && _vitrinaGrid.spacing == Vector2.zero
            && _vitrinaGrid.padding.horizontal == 0) return;
        _vitrinaCompactPatched = true;

        var vitrina = transform.Find("Scroll/Viewport/Content/VitrinaFrame/VitrinaInner");
        if (vitrina == null) return;

        foreach (var overlayName in new[] { "GlassOverlay", "GlassShine" })
        {
            var overlay = vitrina.Find(overlayName);
            if (overlay != null) Destroy(overlay.gameObject);
        }

        var innerVLG = vitrina.GetComponent<VerticalLayoutGroup>();
        if (innerVLG != null)
        {
            innerVLG.padding = new RectOffset(0, 0, 0, 0);
            innerVLG.spacing = 0f;
        }

        // Guarantee grid has zero gaps on every re-entry
        if (_vitrinaGrid != null)
        {
            _vitrinaGrid.spacing = Vector2.zero;
            _vitrinaGrid.padding = new RectOffset(0, 0, 0, 0);
        }

        var frame = vitrina.parent;
        if (frame != null)
        {
            var frameImg = frame.GetComponent<Image>();
            if (frameImg != null) frameImg.color = BG_VITRINA;

            var frameVLG = frame.GetComponent<VerticalLayoutGroup>();
            if (frameVLG != null)
                frameVLG.padding = new RectOffset(0, 0, 0, 0);
        }

        if (_slotFrames != null)
        {
            for (int i = 0; i < _slotFrames.Length; i++)
            {
                if (_slotFrames[i] == null) continue;
                _slotFrames[i].color = Color.clear;
                var rt = _slotFrames[i].rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
        }

        // Slot backgrounds are owned by RefreshStarGrid (FASE 15.7B)

        if (_vitrinaFrameLE != null)
            ApplyVitrinaFrameLayout(_vitrinaFrameLE);
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

    static void CenterSquare(RectTransform rt, float size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = Vector2.zero;
    }
}
