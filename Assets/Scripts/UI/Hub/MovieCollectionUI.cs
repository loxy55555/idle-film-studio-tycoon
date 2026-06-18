using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Collection screen — self-contained L1/L2/L3 navigation (Phase 8.6A layout rebuild).
/// L1: album-style genre covers with progress bars (2 columns).
/// L2: poster-dominant movie grid with rarity frames (3 columns).
/// L3: album-card detail popup with framed poster and status pill.
/// </summary>
public class MovieCollectionUI : MonoBehaviour
{
    // ── Palette ──────────────────────────────────────────────────────────────────
    static readonly Color BG_DEEP   = new Color(0.06f, 0.07f, 0.12f);
    static readonly Color BG_CARD   = CinematicTheme.CardBg;
    static readonly Color BG_DARK   = CinematicTheme.DeepBg;
    static readonly Color TEXT_PRI  = CinematicTheme.TextPrimary;
    static readonly Color TEXT_SEC  = CinematicTheme.TextSecondary;
    static readonly Color ACCENT_GN = CinematicTheme.GoldDim;
    static readonly Color ACCENT_GL = CinematicTheme.GoldBright;
    static readonly Color BAR_BG    = CinematicTheme.DeepBg;

    // ── Genre definitions ─────────────────────────────────────────────────────
    static readonly (MovieGenre genre, string emoji, Color color)[] Genres =
    {
        (MovieGenre.Action,      "💥", new Color(0.91f, 0.30f, 0.24f)),
        (MovieGenre.Drama,       "🎭", new Color(0.20f, 0.60f, 0.86f)),
        (MovieGenre.Horror,      "💀", new Color(0.56f, 0.27f, 0.68f)),
        (MovieGenre.Comedy,      "😂", new Color(0.95f, 0.61f, 0.07f)),
        (MovieGenre.Romance,     "❤️", new Color(0.91f, 0.12f, 0.55f)),
        (MovieGenre.SciFi,       "🚀", new Color(0.15f, 0.68f, 0.38f)),
        (MovieGenre.Fantasy,     "✨", new Color(0.61f, 0.35f, 0.71f)),
        (MovieGenre.Thriller,    "🔪", new Color(0.83f, 0.33f, 0.00f)),
        (MovieGenre.Animation,   "🎨", new Color(0.18f, 0.80f, 0.72f)),
        (MovieGenre.Documentary, "📽️", new Color(0.50f, 0.55f, 0.60f)),
    };

    // ── Backward-compat API (kept for existing bindings) ─────────────────────
    [HideInInspector] public MovieConfig[] allMovies;
    [HideInInspector] public RectTransform gridContent;

    // ── State ─────────────────────────────────────────────────────────────────
    MovieGenre? _selectedGenre;
    bool        _built;

    // ── L1 views ──────────────────────────────────────────────────────────────
    GameObject _l1Panel;
    ScrollRect _genreScroll;
    LayoutElement _genreHeaderLE;
    readonly List<LayoutElement> _genreRows = new();
    Coroutine _genreLayoutRoutine;
    readonly Dictionary<MovieGenre, TextMeshProUGUI> _genreProgressLabels = new();
    readonly Dictionary<MovieGenre, RectTransform>   _genreBarFills      = new();

    // ── L2 views ──────────────────────────────────────────────────────────────
    GameObject      _l2Panel;
    CanvasGroup     _l2PanelGroup;
    TextMeshProUGUI _l2GenreTitle;
    TextMeshProUGUI _l2CountLabel;
    RectTransform   _l2GridContent;

    // ── L3 views (popup) ──────────────────────────────────────────────────────
    GameObject      _l3Popup;
    CanvasGroup     _l3PopupGroup;
    RectTransform   _l3CardRT;
    Image           _l3PosterFrame;
    Image           _l3PosterImg;
    Image           _l3RarityGlow;    // behind poster, colored per rarity
    TextMeshProUGUI _l3Title;
    TextMeshProUGUI _l3Genre;
    Image           _l3GenreBg;
    TextMeshProUGUI _l3Rarity;
    Image           _l3RarityBg;
    Image           _l3StatusPill;
    TextMeshProUGUI _l3Status;
    TextMeshProUGUI _l3SynopsisHeader;
    TextMeshProUGUI _l3Synopsis;
    TextMeshProUGUI _l3Stats;

    // ── Global progress (L1 header) ───────────────────────────────────────────
    TextMeshProUGUI _globalProgressText;
    RectTransform   _globalBarFill;

    // ── FilterGenre property (kept for external compat) ───────────────────────
    public MovieGenre? FilterGenre
    {
        get => _selectedGenre;
        set
        {
            _selectedGenre = value;
            if (value.HasValue) ShowMovieGrid(value.Value);
            else                ShowGenreGrid();
        }
    }

    StudioManager _studio;
    bool          _eventsSubscribed;
    public bool   IsBound => _studio != null && _eventsSubscribed;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        EnsureBuilt();
        GameHub.OnGameReady += TryBind;
        UserPrefs.OnLanguageChanged += RefreshLocalization;
    }

    void Start()  => TryBind();

    void OnEnable()
    {
        EnsureBuilt();
        TryBind();
        if (_l1Panel != null && _l2Panel != null && !_selectedGenre.HasValue)
            ShowGenreGrid();
        RequestGenreViewportFill();
    }

    void Update()
    {
        if (!_eventsSubscribed) TryBind();
    }

    void OnDestroy()
    {
        GameHub.OnGameReady -= TryBind;
        UserPrefs.OnLanguageChanged -= RefreshLocalization;
        Unbind();
    }

    public void RefreshLocalization()
    {
        _built = false;
        _selectedGenre = null;
        _genreProgressLabels.Clear();
        _genreBarFills.Clear();
        _genreRows.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var ch = transform.GetChild(i);
            if (Application.isPlaying) Destroy(ch.gameObject);
            else DestroyImmediate(ch.gameObject);
        }

        EnsureBuilt();
        TryBind();
    }

    // ── Bind ──────────────────────────────────────────────────────────────────

    void TryBind()
    {
        var studio = GameHub.Instance?.studio;
        if (studio == null) return;

        if (_eventsSubscribed && _studio == studio) { Refresh(); return; }

        Unbind();
        _studio = studio;
        _studio.OnMovieCompleted += OnMovieCompletedHandler;
        _eventsSubscribed = true;
        Refresh();
    }

    void OnMovieCompletedHandler(MovieCompletePayload _) => Refresh();

    void Unbind()
    {
        if (!_eventsSubscribed || _studio == null) return;
        _studio.OnMovieCompleted -= OnMovieCompletedHandler;
        _eventsSubscribed = false;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Refresh()
    {
        EnsureBuilt();
        RefreshGlobalProgress();
        RefreshGenreProgress();
        if (_selectedGenre.HasValue)
            PopulateMovieGrid(_selectedGenre.Value);
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    void EnsureBuilt()
    {
        if (_built) return;
        _built = true;
        BuildAllLevels();
    }

    void BuildAllLevels()
    {
        var root = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();

        // Phase 8.6B: remove any legacy children (orphan filter buttons, old grids)
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            var ch = root.GetChild(i);
            if (Application.isPlaying)  Object.Destroy(ch.gameObject);
            else                        Object.DestroyImmediate(ch.gameObject);
        }

        Stretch(root);

        BuildL1GenreGrid(root);
        BuildL2MovieGrid(root);
        BuildL3Popup(root);

        ShowGenreGrid();
    }

    // ── L1: Genre album covers ────────────────────────────────────────────────

    void BuildL1GenreGrid(RectTransform parent)
    {
        _l1Panel = new GameObject("L1_GenreGrid", typeof(RectTransform), typeof(Image));
        _l1Panel.transform.SetParent(parent, false);
        _l1Panel.GetComponent<Image>().color = Color.clear;
        Stretch(_l1Panel.GetComponent<RectTransform>());

        var scroll = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scroll.transform.SetParent(_l1Panel.transform, false);
        Stretch(scroll.GetComponent<RectTransform>());
        scroll.GetComponent<Image>().color = Color.clear;
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
        HudLayoutConstants.ApplySectionPadding(vlg);
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _genreRows.Clear();
        _genreScroll = sr;

        // ── Header: title + global progress bar ──
        var hdrGo = new GameObject("Hdr", typeof(RectTransform));
        hdrGo.transform.SetParent(content.transform, false);
        _genreHeaderLE = hdrGo.AddComponent<LayoutElement>();
        _genreHeaderLE.preferredHeight = 72f;
        var hdrVLG = hdrGo.AddComponent<VerticalLayoutGroup>();
        hdrVLG.spacing = 6;
        hdrVLG.childControlWidth = hdrVLG.childControlHeight = true;
        hdrVLG.childForceExpandWidth = true; hdrVLG.childForceExpandHeight = false;
        hdrVLG.childAlignment = TextAnchor.MiddleLeft;

        var titleRow = new GameObject("TitleRow", typeof(RectTransform));
        titleRow.transform.SetParent(hdrGo.transform, false);
        titleRow.AddComponent<LayoutElement>().preferredHeight = 32f;
        var trHLG = titleRow.AddComponent<HorizontalLayoutGroup>();
        trHLG.childControlWidth = trHLG.childControlHeight = true;
        trHLG.childForceExpandWidth = false; trHLG.childForceExpandHeight = true;

        var hdrTitle = RuntimeTmpText.Create(titleRow.transform, Loc.Get(LocKeys.CollectionScreenTitle),
            24f, TEXT_PRI, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "Title");
        hdrTitle.raycastTarget = false;
        hdrTitle.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        _globalProgressText = RuntimeTmpText.Create(titleRow.transform, "—",
            14f, ACCENT_GL, FontStyles.Bold, TextAlignmentOptions.MidlineRight, "GlobalProgress");
        _globalProgressText.raycastTarget = false;
        _globalProgressText.gameObject.AddComponent<LayoutElement>().preferredWidth = 150f;

        // Global progress bar
        var gBarBg = new GameObject("GlobalBar", typeof(RectTransform), typeof(Image));
        gBarBg.transform.SetParent(hdrGo.transform, false);
        gBarBg.GetComponent<Image>().color = BAR_BG;
        gBarBg.GetComponent<Image>().raycastTarget = false;
        gBarBg.AddComponent<LayoutElement>().preferredHeight = 8f;

        var gFill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        gFill.transform.SetParent(gBarBg.transform, false);
        gFill.GetComponent<Image>().color = ACCENT_GL;
        gFill.GetComponent<Image>().raycastTarget = false;
        _globalBarFill = gFill.GetComponent<RectTransform>();
        SetFill(_globalBarFill, 0f);

        // ── 2-column genre album grid (5 rows) ──
        for (int i = 0; i < Genres.Length; i += 2)
        {
            var row = new GameObject("Row" + i, typeof(RectTransform));
            row.transform.SetParent(content.transform, false);
            var rowLE = row.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 150f;
            _genreRows.Add(rowLE);
            var rowHLG = row.AddComponent<HorizontalLayoutGroup>();
            rowHLG.spacing = 12;
            rowHLG.childControlWidth = rowHLG.childControlHeight = true;
            rowHLG.childForceExpandWidth = true; rowHLG.childForceExpandHeight = true;

            BuildGenreCard(row.transform, Genres[i]);
            if (i + 1 < Genres.Length)
                BuildGenreCard(row.transform, Genres[i + 1]);
            else
            {
                var spacer = new GameObject("Spacer", typeof(RectTransform));
                spacer.transform.SetParent(row.transform, false);
            }
        }

        gridContent = cRT; // backward compat
    }

    void BuildGenreCard(Transform parent, (MovieGenre genre, string emoji, Color color) def)
    {
        var card = new GameObject("Genre_" + def.genre, typeof(RectTransform), typeof(Image), typeof(Button));
        card.transform.SetParent(parent, false);
        card.GetComponent<Image>().color = BG_CARD;

        var vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.spacing = 0;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        // Album cover area — neutral premium dark surface, genre identity via emoji only
        var cover = new GameObject("Cover", typeof(RectTransform), typeof(Image));
        cover.transform.SetParent(card.transform, false);
        cover.GetComponent<Image>().color = CinematicTheme.PanelBg;
        cover.GetComponent<Image>().raycastTarget = false;
        cover.AddComponent<LayoutElement>().flexibleHeight = 1f;

        var emojiTxt = RuntimeTmpText.Create(cover.transform, def.emoji,
            42f, Color.white, FontStyles.Normal, TextAlignmentOptions.Center, "Emoji");
        emojiTxt.raycastTarget = false;
        Stretch(emojiTxt.rectTransform);
        var coverIcon = UIIconGraphic.ApplyCoverIcon(cover.transform, UIIconCatalog.GetGenre(def.genre), "CoverIcon", 56f);
        if (coverIcon != null)
            coverIcon.preserveAspect = true;

        // Info strip — name left, count right
        var info = new GameObject("Info", typeof(RectTransform), typeof(Image));
        info.transform.SetParent(card.transform, false);
        info.GetComponent<Image>().color = BG_DARK;
        info.GetComponent<Image>().raycastTarget = false;
        info.AddComponent<LayoutElement>().preferredHeight = 40f;
        var infoHLG = info.AddComponent<HorizontalLayoutGroup>();
        infoHLG.padding = new RectOffset(10, 10, 4, 4);
        infoHLG.spacing = 4;
        infoHLG.childControlWidth = infoHLG.childControlHeight = true;
        infoHLG.childForceExpandWidth = false; infoHLG.childForceExpandHeight = true;

        var nameTxt = RuntimeTmpText.Create(info.transform, GenreLabel(def.genre),
            14f, TEXT_PRI, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "Name");
        nameTxt.raycastTarget = false;
        nameTxt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var progressTxt = RuntimeTmpText.Create(info.transform, "0 / 0",
            13f, ACCENT_GL, FontStyles.Bold, TextAlignmentOptions.MidlineRight, "Progress");
        progressTxt.raycastTarget = false;
        progressTxt.gameObject.AddComponent<LayoutElement>().preferredWidth = 64f;
        _genreProgressLabels[def.genre] = progressTxt;

        // Progress fill bar — bottom edge, unified premium green
        var barBg = new GameObject("Bar", typeof(RectTransform), typeof(Image));
        barBg.transform.SetParent(card.transform, false);
        barBg.GetComponent<Image>().color = BAR_BG;
        barBg.GetComponent<Image>().raycastTarget = false;
        barBg.AddComponent<LayoutElement>().preferredHeight = 6f;

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(barBg.transform, false);
        fill.GetComponent<Image>().color = CinematicTheme.GoldDim;
        fill.GetComponent<Image>().raycastTarget = false;
        var fillRT = fill.GetComponent<RectTransform>();
        SetFill(fillRT, 0f);
        _genreBarFills[def.genre] = fillRT;

        var genre = def.genre;
        card.GetComponent<Button>().onClick.AddListener(() => ShowMovieGrid(genre));
        card.AddComponent<UIButtonScale>();
    }

    // ── L2: Poster grid ───────────────────────────────────────────────────────

    void BuildL2MovieGrid(RectTransform parent)
    {
        _l2Panel = new GameObject("L2_MovieGrid", typeof(RectTransform), typeof(Image));
        _l2Panel.transform.SetParent(parent, false);
        _l2Panel.GetComponent<Image>().color = Color.clear;
        Stretch(_l2Panel.GetComponent<RectTransform>());
        _l2PanelGroup = _l2Panel.AddComponent<CanvasGroup>();

        var outerVLG = _l2Panel.AddComponent<VerticalLayoutGroup>();
        outerVLG.spacing = 0;
        outerVLG.childControlWidth = outerVLG.childControlHeight = true;
        outerVLG.childForceExpandWidth = true; outerVLG.childForceExpandHeight = false;

        // Back bar: square back button + title + count
        var backBar = new GameObject("BackBar", typeof(RectTransform), typeof(Image));
        backBar.transform.SetParent(_l2Panel.transform, false);
        backBar.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.16f);
        backBar.AddComponent<LayoutElement>().preferredHeight = 56f;

        var backHLG = backBar.AddComponent<HorizontalLayoutGroup>();
        backHLG.padding = new RectOffset(8, 14, 6, 6);
        backHLG.spacing = 12;
        backHLG.childControlWidth = backHLG.childControlHeight = true;
        backHLG.childForceExpandWidth = false; backHLG.childForceExpandHeight = true;

        var backBtnGo = new GameObject("BackBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        backBtnGo.transform.SetParent(backBar.transform, false);
        backBtnGo.GetComponent<Image>().color = BG_CARD;
        var bbLE = backBtnGo.AddComponent<LayoutElement>();
        bbLE.preferredWidth = 48f; bbLE.minWidth = 48f; bbLE.flexibleWidth = 0f;

        var bbLbl = RuntimeTmpText.Create(backBtnGo.transform, "←",
            22f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center, "Label");
        bbLbl.raycastTarget = false;
        Stretch(bbLbl.rectTransform);
        backBtnGo.GetComponent<Button>().onClick.AddListener(ShowGenreGrid);

        _l2GenreTitle = RuntimeTmpText.Create(backBar.transform, Loc.Get(LocKeys.CollectionGenreLabel),
            18f, TEXT_PRI, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "GenreTitle");
        _l2GenreTitle.raycastTarget = false;
        _l2GenreTitle.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        _l2CountLabel = RuntimeTmpText.Create(backBar.transform, "0 / 0",
            14f, ACCENT_GL, FontStyles.Bold, TextAlignmentOptions.MidlineRight, "Count");
        _l2CountLabel.raycastTarget = false;
        _l2CountLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 70f;

        // Scroll area for movie grid
        var scrollGo = new GameObject("MovieScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollGo.transform.SetParent(_l2Panel.transform, false);
        scrollGo.GetComponent<Image>().color = Color.clear;
        scrollGo.AddComponent<LayoutElement>().flexibleHeight = 1f;
        var sr = scrollGo.GetComponent<ScrollRect>();
        sr.horizontal = false; sr.vertical = true;
        sr.scrollSensitivity = 40f; sr.inertia = true; sr.decelerationRate = 0.135f;

        var vp = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        vp.transform.SetParent(scrollGo.transform, false);
        Stretch(vp.GetComponent<RectTransform>());
        sr.viewport = vp.GetComponent<RectTransform>();

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(vp.transform, false);
        _l2GridContent = content.GetComponent<RectTransform>();
        _l2GridContent.anchorMin = new Vector2(0f, 1f); _l2GridContent.anchorMax = new Vector2(1f, 1f);
        _l2GridContent.pivot = new Vector2(0.5f, 1f); _l2GridContent.offsetMin = _l2GridContent.offsetMax = Vector2.zero;
        sr.content = _l2GridContent;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 12, 16);
        vlg.spacing = 10;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    // ── L3: Movie detail popup (Phase 12.2 — premium collectible card) ──────────

    void BuildL3Popup(RectTransform parent)
    {
        // Full-screen overlay
        _l3Popup = new GameObject("L3_Popup", typeof(RectTransform), typeof(Image));
        _l3Popup.transform.SetParent(parent, false);
        var popupImg = _l3Popup.GetComponent<Image>();
        popupImg.color = new Color(0f, 0f, 0f, 0.88f);
        Stretch(_l3Popup.GetComponent<RectTransform>());
        popupImg.raycastTarget = true;
        _l3PopupGroup = _l3Popup.AddComponent<CanvasGroup>();
        _l3Popup.AddComponent<Button>().onClick.AddListener(ClosePopup);

        // Gold border frame (rarity will override at runtime)
        var frameBorder = new GameObject("FrameBorder", typeof(RectTransform), typeof(Image));
        frameBorder.transform.SetParent(_l3Popup.transform, false);
        _l3CardRT     = frameBorder.GetComponent<RectTransform>();
        _l3PosterFrame = frameBorder.GetComponent<Image>();
        _l3PosterFrame.raycastTarget = false;
        var fbRT = frameBorder.GetComponent<RectTransform>();
        fbRT.anchorMin = new Vector2(0.06f, 0.07f);
        fbRT.anchorMax = new Vector2(0.94f, 0.95f);
        fbRT.offsetMin = fbRT.offsetMax = Vector2.zero;

        // Card panel — sits inside the border (2px inset each side)
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(frameBorder.transform, false);
        panel.GetComponent<Image>().color = CinematicTheme.CardBg;
        panel.GetComponent<Image>().raycastTarget = true;
        panel.AddComponent<Button>().onClick.AddListener(() => {}); // absorb taps
        var panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = new Vector2(2f, 2f);
        panelRT.offsetMax = new Vector2(-2f, -2f);
        CinematicTheme.ApplyPremiumMaterial(panelRT);

        // ── POSTER AREA — full-bleed (covers entire card; info overlays at bottom) ──
        var posterArea = new GameObject("PosterArea", typeof(RectTransform), typeof(Image));
        posterArea.transform.SetParent(panel.transform, false);
        posterArea.GetComponent<Image>().color = CinematicTheme.DeepBg;
        posterArea.GetComponent<Image>().raycastTarget = false;
        var posterRT = posterArea.GetComponent<RectTransform>();
        posterRT.anchorMin = Vector2.zero;
        posterRT.anchorMax = Vector2.one;
        posterRT.offsetMin = posterRT.offsetMax = Vector2.zero;

        // Rarity glow — behind poster, tinted per rarity in ShowMoviePopup
        var glowGo = new GameObject("RarityGlow", typeof(RectTransform), typeof(Image));
        glowGo.transform.SetParent(posterArea.transform, false);
        glowGo.transform.SetSiblingIndex(0);
        _l3RarityGlow = glowGo.GetComponent<Image>();
        _l3RarityGlow.color = Color.clear;
        _l3RarityGlow.raycastTarget = false;
        Stretch(glowGo.GetComponent<RectTransform>());

        var posterGo = new GameObject("Poster", typeof(RectTransform), typeof(Image));
        posterGo.transform.SetParent(posterArea.transform, false);
        _l3PosterImg = posterGo.GetComponent<Image>();
        _l3PosterImg.raycastTarget = false;
        _l3PosterImg.preserveAspect = true;
        Stretch(posterGo.GetComponent<RectTransform>());

        // ── DARK BAND — solid strip at bottom; all text lives inside this band ──
        // FASE 16.1 E1: reduced height by ~27% (0.30 → 0.22) for less visual weight
        var gradSolidGo = new GameObject("GradSolid", typeof(RectTransform), typeof(Image));
        gradSolidGo.transform.SetParent(panel.transform, false);
        gradSolidGo.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.10f, 0.92f);
        gradSolidGo.GetComponent<Image>().raycastTarget = false;
        var gradSolidRT = gradSolidGo.GetComponent<RectTransform>();
        gradSolidRT.anchorMin = Vector2.zero;
        gradSolidRT.anchorMax = new Vector2(1f, 0.22f);
        gradSolidRT.offsetMin = gradSolidRT.offsetMax = Vector2.zero;

        // ── INFO AREA — entirely inside GradSolid; no text over the poster ────
        var infoArea = new GameObject("InfoArea", typeof(RectTransform));
        infoArea.transform.SetParent(panel.transform, false);
        var infoRT = infoArea.GetComponent<RectTransform>();
        infoRT.anchorMin = Vector2.zero;
        infoRT.anchorMax = new Vector2(1f, 0.21f);
        infoRT.offsetMin = infoRT.offsetMax = Vector2.zero;

        var infoVLG = infoArea.AddComponent<VerticalLayoutGroup>();
        infoVLG.padding = new RectOffset(14, 14, 6, 10);
        infoVLG.spacing = 5;
        infoVLG.childControlWidth = infoVLG.childControlHeight = true;
        infoVLG.childForceExpandWidth = true;
        infoVLG.childForceExpandHeight = false;

        // Movie title — left-aligned over the dark overlay
        _l3Title = RuntimeTmpText.Create(infoArea.transform, "—",
            24f, TEXT_PRI, FontStyles.Bold, TextAlignmentOptions.Left, "L3Title");
        _l3Title.raycastTarget = false;
        _l3Title.textWrappingMode = TextWrappingModes.Normal;
        _l3Title.enableAutoSizing = true;
        _l3Title.fontSizeMin = 24f * RuntimeTmpText.MobileScale;
        _l3Title.fontSizeMax = 40f * RuntimeTmpText.MobileScale;
        _l3Title.gameObject.AddComponent<LayoutElement>().preferredHeight = 90f;

        // Rarity badge only
        (_l3Rarity, _l3RarityBg) = BuildBadge(infoArea.transform, "RarityBadge", "—", CinematicTheme.CardBg2);
        _l3Rarity.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        // Separator line
        var sep1 = new GameObject("Sep1", typeof(RectTransform), typeof(Image));
        sep1.transform.SetParent(infoArea.transform, false);
        sep1.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
        sep1.GetComponent<Image>().raycastTarget = false;
        sep1.AddComponent<LayoutElement>().preferredHeight = 1f;

        // "SINOPSIS" gold header
        _l3SynopsisHeader = RuntimeTmpText.Create(infoArea.transform, Loc.Get(LocKeys.CollectionSynopsisHdr),
            14f, CinematicTheme.GoldBase, FontStyles.Bold, TextAlignmentOptions.Left, "L3SynopsisHeader");
        _l3SynopsisHeader.raycastTarget = false;
        _l3SynopsisHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        // Synopsis body — expanded space since info now overlays full-height card
        _l3Synopsis = RuntimeTmpText.Create(infoArea.transform, "—",
            16f, new Color(0.88f, 0.90f, 0.95f), FontStyles.Normal, TextAlignmentOptions.TopLeft, "L3Synopsis");
        _l3Synopsis.raycastTarget = false;
        _l3Synopsis.textWrappingMode = TextWrappingModes.Normal;
        _l3Synopsis.overflowMode = TextOverflowModes.Overflow;
        _l3Synopsis.enableAutoSizing = true;
        _l3Synopsis.fontSizeMin = 18f * RuntimeTmpText.MobileScale;
        _l3Synopsis.fontSizeMax = 26f * RuntimeTmpText.MobileScale;
        _l3Synopsis.lineSpacing = 4f;
        var synopsisLE = _l3Synopsis.gameObject.AddComponent<LayoutElement>();
        synopsisLE.flexibleHeight = 1f;
        synopsisLE.minHeight = 80f;

        // Close button — full width, bronze
        var closeBtnGo = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnGo.transform.SetParent(infoArea.transform, false);
        closeBtnGo.GetComponent<Image>().color = CinematicTheme.BronzeBase;
        closeBtnGo.AddComponent<LayoutElement>().preferredHeight = 38f;
        CinematicTheme.ApplyElevationButton(closeBtnGo.GetComponent<RectTransform>());
        var closeLbl = RuntimeTmpText.Create(closeBtnGo.transform, Loc.Get(LocKeys.CollectionClose),
            14f, CinematicTheme.TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center, "Lbl");
        closeLbl.raycastTarget = false;
        Stretch(closeLbl.rectTransform);
        closeBtnGo.GetComponent<Button>().onClick.AddListener(ClosePopup);
    }

    (TextMeshProUGUI, Image) BuildBadge(Transform parent, string name, string text, Color bg)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = bg;
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 120f; le.preferredHeight = 26f; le.flexibleWidth = 0f;

        var tmp = RuntimeTmpText.Create(go.transform, text,
            11f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center, "Lbl");
        tmp.raycastTarget = false;
        Stretch(tmp.rectTransform);
        return (tmp, img);
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    void ShowGenreGrid()
    {
        _selectedGenre = null;
        if (_l1Panel  != null) _l1Panel.SetActive(true);
        if (_l2Panel  != null) _l2Panel.SetActive(false);
        if (_l3Popup  != null) _l3Popup.SetActive(false);
        RefreshGlobalProgress();
        RefreshGenreProgress();
        RequestGenreViewportFill();
    }

    void RequestGenreViewportFill()
    {
        if (!isActiveAndEnabled || _l1Panel == null || !_l1Panel.activeSelf) return;
        if (_genreLayoutRoutine != null) StopCoroutine(_genreLayoutRoutine);
        _genreLayoutRoutine = StartCoroutine(ApplyGenreViewportFillDeferred());
    }

    IEnumerator ApplyGenreViewportFillDeferred()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        ApplyGenreViewportFill();
        _genreLayoutRoutine = null;
    }

    void ApplyGenreViewportFill()
    {
        if (_genreScroll == null || _genreRows.Count == 0 || _genreScroll.viewport == null) return;

        float viewportH = _genreScroll.viewport.rect.height;
        if (viewportH < 200f) return;

        float headerH = _genreHeaderLE != null ? _genreHeaderLE.preferredHeight : 72f;
        int rowCount = _genreRows.Count;
        float pad = HudLayoutConstants.SectionPadding.top + HudLayoutConstants.SectionPadding.bottom;
        float spacing = HudLayoutConstants.SectionSpacing * Mathf.Max(0, rowCount - 1);
        float rowH = Mathf.Max(132f, (viewportH - headerH - pad - spacing) / rowCount);

        foreach (var rowLE in _genreRows)
        {
            rowLE.preferredHeight = rowH;
            rowLE.minHeight = rowH;
            rowLE.flexibleHeight = 0f;
        }

        float contentH = headerH + pad + rowCount * rowH + spacing;
        _genreScroll.vertical = contentH > viewportH + 2f;

        if (_genreScroll.content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_genreScroll.content);
    }

    void ShowMovieGrid(MovieGenre genre)
    {
        _selectedGenre = genre;
        if (_l1Panel != null) _l1Panel.SetActive(false);
        if (_l2Panel != null)
        {
            _l2Panel.SetActive(true);
            UIAnimationService.PlayPanelOpen(_l2Panel.GetComponent<RectTransform>(), _l2PanelGroup);
        }
        if (_l3Popup != null) _l3Popup.SetActive(false);

        if (_l2GenreTitle != null)
            _l2GenreTitle.text = GenreLabel(genre).ToUpper();

        PopulateMovieGrid(genre);
    }

    void ShowMoviePopup(MovieConfig cfg)
    {
        if (_l3Popup == null || cfg == null) return;
        _l3Popup.SetActive(true);

        UIAnimationService.PlayPopupOpen(_l3CardRT, _l3PopupGroup);

        var completed = _studio?.CompletedMovieKeys;
        bool done     = completed != null && completed.Contains(cfg.name);

        // Poster frame border — rarity color
        var rarityCol = RarityFrameColor(cfg.rarity);
        if (_l3PosterFrame != null) _l3PosterFrame.color = rarityCol;

        // Rarity glow (behind poster)
        if (_l3RarityGlow != null) _l3RarityGlow.color = RarityPopupGlow(cfg.rarity);

        // Poster image — actual sprite if available, else posterColorHex placeholder
        if (_l3PosterImg != null)
        {
            if (done)
                MoviePosterVisual.Apply(_l3PosterImg, cfg);
            else
            {
                _l3PosterImg.sprite = null;
                _l3PosterImg.color  = Color.Lerp(ParseHexColor(cfg.posterColorHex), BG_DARK, 0.55f);
            }
        }

        // Title — gold for legendary, primary otherwise
        if (_l3Title != null)
        {
            _l3Title.text  = cfg.movieName;
            _l3Title.color = cfg.rarity == MovieRarity.Legendary ? CinematicTheme.GoldBright : TEXT_PRI;
        }

        if (_l3Genre   != null) _l3Genre.text   = GenreLabel(cfg.genre).ToUpper();
        if (_l3GenreBg != null) _l3GenreBg.color = CinematicTheme.CardBg2;
        if (_l3Rarity  != null) _l3Rarity.text  = RarityLabel(cfg.rarity).ToUpper();
        if (_l3RarityBg != null) _l3RarityBg.color = Color.Lerp(rarityCol, BG_DARK, 0.45f);

        if (_l3Status != null)
        {
            _l3Status.text  = done ? Loc.Get(LocKeys.CollectionFilmCompleted) : Loc.Get(LocKeys.CollectionFilmPending);
            _l3Status.color = done ? CinematicTheme.GoldBright : CinematicTheme.TextDim;
            if (_l3StatusPill != null)
                _l3StatusPill.color = done
                    ? new Color(CinematicTheme.GoldDim.r, CinematicTheme.GoldDim.g, CinematicTheme.GoldDim.b, 0.55f)
                    : new Color(0.10f, 0.12f, 0.20f);
        }

        if (_l3Synopsis != null) _l3Synopsis.text = BuildSynopsis(cfg);

        if (_l3Stats != null)
        {
            var reward   = cfg.baseReward > 0        ? Loc.Format(LocKeys.CollStatMoneyFmt, AnimatedMoneyText.FormatMoney(cfg.baseReward)) : "";
            var duration = cfg.duration > 0f         ? Loc.Format(LocKeys.CollStatDurFmt,   cfg.duration) : "";
            var lvl      = cfg.unlockStudioLevel > 0 ? Loc.Format(LocKeys.CollStatLvlFmt,   cfg.unlockStudioLevel) : "";
            var parts    = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(reward))   parts.Add(reward);
            if (!string.IsNullOrEmpty(duration)) parts.Add(duration);
            if (!string.IsNullOrEmpty(lvl))      parts.Add(lvl);
            _l3Stats.text = string.Join("  ·  ", parts);
        }
    }

    /// <summary>Returns the border/frame color for each rarity level (L3 popup + cell outer border).</summary>
    static Color RarityFrameColor(MovieRarity r) => r switch
    {
        MovieRarity.Common    => new Color(0.26f, 0.32f, 0.42f),   // steel gray-blue
        MovieRarity.Rare      => new Color(0.20f, 0.44f, 0.88f),   // blue metallic
        MovieRarity.Epic      => new Color(0.52f, 0.16f, 0.80f),   // purple premium
        MovieRarity.Legendary => CinematicTheme.GoldBright,        // cinematic gold
        _                     => new Color(0.26f, 0.32f, 0.42f),
    };

    /// <summary>Glow color (behind poster) for Epic/Legendary cards.</summary>
    static Color RarityGlowColor(MovieRarity r) => r switch
    {
        MovieRarity.Epic      => new Color(0.50f, 0.15f, 0.78f, 0.28f),
        MovieRarity.Legendary => new Color(0.80f, 0.58f, 0.08f, 0.32f),
        _                     => Color.clear,
    };

    /// <summary>L3 popup glow (tints poster background per rarity).</summary>
    static Color RarityPopupGlow(MovieRarity r) => r switch
    {
        MovieRarity.Legendary => new Color(0.50f, 0.36f, 0.02f, 0.40f),
        MovieRarity.Epic      => new Color(0.35f, 0.08f, 0.55f, 0.32f),
        MovieRarity.Rare      => new Color(0.10f, 0.22f, 0.55f, 0.20f),
        _                     => Color.clear,
    };

    /// <summary>Adds 4 thin edge strips on top of posterWrap children (inner frame).</summary>
    static void AddInnerFrame(Transform parent, Color color, float thickness = 1.5f)
    {
        MakeEdge(parent, "IFT", color, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, thickness));
        MakeEdge(parent, "IFB", color, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, thickness));
        MakeEdge(parent, "IFL", color, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(thickness, 0f));
        MakeEdge(parent, "IFR", color, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(thickness, 0f));
    }

    static void MakeEdge(Transform parent, string name, Color color,
                         Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        go.GetComponent<Image>().raycastTarget = false;
        go.AddComponent<LayoutElement>().ignoreLayout = true;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.sizeDelta = sizeDelta;
    }

    /// <summary>Adds 9×9 accent squares at each corner (Legendary only).</summary>
    static void AddCornerAccents(Transform parent, Color color, float size = 9f)
    {
        var configs = new (Vector2 anchor, Vector2 pivot)[]
        {
            (new Vector2(0f, 1f), new Vector2(0f, 1f)),
            (new Vector2(1f, 1f), new Vector2(1f, 1f)),
            (new Vector2(0f, 0f), new Vector2(0f, 0f)),
            (new Vector2(1f, 0f), new Vector2(1f, 0f)),
        };
        foreach (var (anch, piv) in configs)
        {
            var go = new GameObject("Corner", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            go.GetComponent<Image>().raycastTarget = false;
            go.AddComponent<LayoutElement>().ignoreLayout = true;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anch; rt.pivot = piv;
            rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(size, size);
        }
    }

    void ClosePopup()
    {
        if (_l3Popup == null || !_l3Popup.activeSelf) return;
        _l3CardRT?.DOKill();
        _l3PopupGroup?.DOKill();
        UIAnimationService.PlayPopupClose(_l3CardRT, _l3PopupGroup,
            () => { if (_l3Popup != null) _l3Popup.SetActive(false); });
    }

    // ── Data population ───────────────────────────────────────────────────────

    void PopulateMovieGrid(MovieGenre genre)
    {
        if (_l2GridContent == null) return;

        for (int i = _l2GridContent.childCount - 1; i >= 0; i--)
            Destroy(_l2GridContent.GetChild(i).gameObject);

        var all       = MovieCatalogRuntime.AllMovies ?? allMovies;
        var completed = _studio?.CompletedMovieKeys;

        if (all == null || all.Length == 0) return;

        var filtered = new List<MovieConfig>();
        int doneCount = 0;
        foreach (var m in all)
        {
            if (m == null || m.genre != genre) continue;
            filtered.Add(m);
            if (completed != null && completed.Contains(m.name)) doneCount++;
        }

        // Phase 8.6B: group by rarity — Legendary first, then Epic, Rare, Common
        filtered.Sort((a, b) => RarityOrder(b.rarity).CompareTo(RarityOrder(a.rarity)));

        if (_l2CountLabel != null)
            _l2CountLabel.text = $"{doneCount} / {filtered.Count}";

        const int cols = 3; // RULE: always 3 columns, uniform cell size
        for (int i = 0; i < filtered.Count; i += cols)
        {
            var row = new GameObject("Row" + i, typeof(RectTransform));
            row.transform.SetParent(_l2GridContent, false);
            var rowLE = row.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 185f; // phase 12.3: slightly taller for mobile readability
            rowLE.minHeight = 185f;
            var rowHLG = row.AddComponent<HorizontalLayoutGroup>();
            rowHLG.spacing = 10;
            rowHLG.childControlWidth = rowHLG.childControlHeight = true;
            rowHLG.childForceExpandWidth = true; rowHLG.childForceExpandHeight = true;

            for (int c = 0; c < cols; c++)
            {
                int idx = i + c;
                if (idx < filtered.Count)
                    BuildMovieCell(row.transform, filtered[idx], completed);
                else
                {
                    var spacer = new GameObject("Spacer", typeof(RectTransform));
                    spacer.transform.SetParent(row.transform, false);
                    var spacerLE = spacer.AddComponent<LayoutElement>();
                    spacerLE.flexibleWidth = 1f;
                    spacerLE.minWidth = 0f;
                }
            }
        }
    }

    void BuildMovieCell(Transform parent, MovieConfig cfg, IReadOnlyCollection<string> completed)
    {
        bool done = completed != null && completed.Contains(cfg.name);

        // Cell — uniform width in 3-column row (discovered or not)
        var cell = new GameObject("Movie_" + cfg.name, typeof(RectTransform), typeof(Image), typeof(Button));
        cell.transform.SetParent(parent, false);
        var borderCol = RarityFrameColor(cfg.rarity);
        cell.GetComponent<Image>().color = done
            ? borderCol
            : Color.Lerp(borderCol, BG_DARK, 0.62f);

        var cellLE = cell.AddComponent<LayoutElement>();
        cellLE.flexibleWidth  = 1f;
        cellLE.minWidth       = 0f;
        cellLE.flexibleHeight = 1f;

        var vlg = cell.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(3, 3, 3, 3);
        vlg.spacing = 0;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Poster area — discovered cells expand to fill entire row; undiscovered keep fixed 130px
        var posterWrap = new GameObject("PosterWrap", typeof(RectTransform));
        posterWrap.transform.SetParent(cell.transform, false);
        var wrapLE = posterWrap.AddComponent<LayoutElement>();
        wrapLE.flexibleWidth  = 1f;
        wrapLE.minWidth       = 0f;
        wrapLE.minHeight      = 130f;
        if (done)
        {
            wrapLE.flexibleHeight  = 1f;   // expand to fill cell — no NameStrip below
            wrapLE.preferredHeight = 175f;
        }
        else
        {
            wrapLE.flexibleHeight  = 0f;
            wrapLE.preferredHeight = 130f;
        }

        var poster = new GameObject("Poster", typeof(RectTransform), typeof(Image));
        poster.transform.SetParent(posterWrap.transform, false);
        var posterImg = poster.GetComponent<Image>();
        posterImg.raycastTarget = false;
        Stretch(poster.GetComponent<RectTransform>());
        poster.AddComponent<LayoutElement>().ignoreLayout = true;
        if (done)
            MoviePosterVisual.Apply(posterImg, cfg);
        else
            posterImg.color = Color.Lerp(ParseHexColor(cfg.posterColorHex), BG_DARK, 0.65f);

        if (done)
        {
            // Cinematic gradient — tall, opaque band covering bottom 38 % of poster
            var gradGo = new GameObject("TitleGradient", typeof(RectTransform), typeof(Image));
            gradGo.transform.SetParent(posterWrap.transform, false);
            var gradImg = gradGo.GetComponent<Image>();
            gradImg.color = new Color(0f, 0f, 0f, 0.88f);
            gradImg.raycastTarget = false;
            var gradRT = gradGo.GetComponent<RectTransform>();
            gradRT.anchorMin = Vector2.zero;
            gradRT.anchorMax = new Vector2(1f, 0.38f);
            gradRT.offsetMin = gradRT.offsetMax = Vector2.zero;

            // Cinematic title — large, bold, ivory, anchored to the gradient band
            var titleGo = new GameObject("TitleTMP", typeof(RectTransform));
            titleGo.transform.SetParent(posterWrap.transform, false);
            var titleTMP = titleGo.AddComponent<TextMeshProUGUI>();
            var titleRT = titleGo.GetComponent<RectTransform>();
            titleRT.anchorMin = Vector2.zero;
            titleRT.anchorMax = new Vector2(1f, 0.38f);
            titleRT.offsetMin = new Vector2(6f, 6f);
            titleRT.offsetMax = new Vector2(-6f, 0f);
            // C3 (FASE 16.1): NoWrap prevents TMP from breaking words mid-character on narrow posters
            titleTMP.text = cfg.movieName.ToUpper();
            titleTMP.fontSize = 16f;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.alignment = TextAlignmentOptions.BottomLeft;
            titleTMP.enableAutoSizing = true;
            titleTMP.fontSizeMin = 10f;
            titleTMP.fontSizeMax = 18f;
            titleTMP.overflowMode = TextOverflowModes.Ellipsis;
            titleTMP.color = new Color(0.96f, 0.94f, 0.87f);
            titleTMP.raycastTarget = false;
            titleTMP.textWrappingMode = TextWrappingModes.NoWrap;
            titleTMP.characterSpacing = 0f;

            // ✔ badge anchored to poster top-right
            var badge = new GameObject("Done", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(poster.transform, false);
            badge.GetComponent<Image>().color = new Color(CinematicTheme.GoldBase.r, CinematicTheme.GoldBase.g, CinematicTheme.GoldBase.b, 0.95f);
            badge.GetComponent<Image>().raycastTarget = false;
            var bRT = badge.GetComponent<RectTransform>();
            bRT.anchorMin = bRT.anchorMax = new Vector2(1f, 1f);
            bRT.pivot = new Vector2(1f, 1f);
            bRT.anchoredPosition = new Vector2(-4f, -4f);
            bRT.sizeDelta = new Vector2(22f, 22f);

            var badgeLbl = RuntimeTmpText.Create(badge.transform, "✔",
                12f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center, "Lbl");
            badgeLbl.raycastTarget = false;
            Stretch(badgeLbl.rectTransform);
        }
        else
        {
            var lockLbl = RuntimeTmpText.Create(poster.transform, "?",
                26f, new Color(1f, 1f, 1f, 0.30f), FontStyles.Bold, TextAlignmentOptions.Center, "Unknown");
            lockLbl.raycastTarget = false;
            Stretch(lockLbl.rectTransform);
        }

        // Name strip — only for undiscovered movies (title is in the overlay for discovered ones)
        if (!done)
        {
            var nameStrip = new GameObject("NameStrip", typeof(RectTransform), typeof(Image));
            nameStrip.transform.SetParent(cell.transform, false);
            nameStrip.GetComponent<Image>().color = BG_DARK;
            nameStrip.GetComponent<Image>().raycastTarget = false;
            var nameStripLE = nameStrip.AddComponent<LayoutElement>();
            nameStripLE.preferredHeight = 40f;
            nameStripLE.flexibleWidth = 1f;
            nameStripLE.minWidth = 0f;

            var nameTxt = RuntimeTmpText.Create(nameStrip.transform, cfg.movieName,
                12f, TEXT_SEC, FontStyles.Bold, TextAlignmentOptions.Center, "Name");
            nameTxt.enableAutoSizing = true;
            nameTxt.fontSizeMin = 9f;
            nameTxt.fontSizeMax = 12f;
            nameTxt.textWrappingMode = TextWrappingModes.Normal;
            nameTxt.overflowMode = TextOverflowModes.Ellipsis;
            nameTxt.maxVisibleLines = 2;
            nameTxt.raycastTarget = false;
            Stretch(nameTxt.rectTransform);
            nameTxt.rectTransform.offsetMin = new Vector2(3f, 1f);
            nameTxt.rectTransform.offsetMax = new Vector2(-3f, -1f);
        }

        // ── Rarity premium treatment (discovered movies only) ────────────────
        if (done)
        {
            var glowCol = RarityGlowColor(cfg.rarity);

            // Glow aura — behind all poster content, extends beyond posterWrap
            if (glowCol.a > 0.01f)
            {
                var glowGo = new GameObject("RarityGlow", typeof(RectTransform), typeof(Image));
                glowGo.transform.SetParent(posterWrap.transform, false);
                glowGo.transform.SetSiblingIndex(0);
                glowGo.GetComponent<Image>().color = glowCol;
                glowGo.GetComponent<Image>().raycastTarget = false;
                glowGo.AddComponent<LayoutElement>().ignoreLayout = true;
                float glowPad = cfg.rarity == MovieRarity.Legendary ? 8f : 6f;
                var glowRT = glowGo.GetComponent<RectTransform>();
                glowRT.anchorMin = Vector2.zero; glowRT.anchorMax = Vector2.one;
                glowRT.offsetMin = new Vector2(-glowPad, -glowPad);
                glowRT.offsetMax = new Vector2(glowPad, glowPad);
            }

            // Rare: subtle blue highlight at top of poster
            if (cfg.rarity == MovieRarity.Rare)
            {
                var hl = new GameObject("TopHL", typeof(RectTransform), typeof(Image));
                hl.transform.SetParent(posterWrap.transform, false);
                hl.GetComponent<Image>().color = new Color(0.32f, 0.60f, 1f, 0.20f);
                hl.GetComponent<Image>().raycastTarget = false;
                hl.AddComponent<LayoutElement>().ignoreLayout = true;
                var hlRT = hl.GetComponent<RectTransform>();
                hlRT.anchorMin = new Vector2(0f, 0.78f); hlRT.anchorMax = Vector2.one;
                hlRT.offsetMin = hlRT.offsetMax = Vector2.zero;
            }

            // Epic/Legendary: inner frame (1.5px strips on top of poster content)
            if (cfg.rarity == MovieRarity.Epic || cfg.rarity == MovieRarity.Legendary)
                AddInnerFrame(posterWrap.transform, new Color(borderCol.r, borderCol.g, borderCol.b, 0.85f), 1.5f);

            // Legendary: golden corner accent squares
            if (cfg.rarity == MovieRarity.Legendary)
                AddCornerAccents(posterWrap.transform, new Color(borderCol.r, borderCol.g, borderCol.b, 0.95f), 9f);

        }

        var m = cfg;
        cell.GetComponent<Button>().onClick.AddListener(() => ShowMoviePopup(m));
        cell.AddComponent<UIButtonScale>();

        // Legendary: slow glow pulse on posterWrap (1.00 ↔ 1.03)
        if (done && cfg.rarity == MovieRarity.Legendary)
        {
            var wrapRT = posterWrap.GetComponent<RectTransform>();
            wrapRT.localScale = Vector3.one;
            wrapRT.DOScale(1.03f, 2.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }
    }

    // ── Progress refresh ──────────────────────────────────────────────────────

    void RefreshGlobalProgress()
    {
        if (_globalProgressText == null) return;

        var all       = MovieCatalogRuntime.AllMovies ?? allMovies;
        var completed = _studio?.CompletedMovieKeys;
        if (all == null) return;

        int total = 0, done = 0;
        foreach (var m in all)
        {
            if (m == null) continue;
            total++;
            if (completed != null && completed.Contains(m.name)) done++;
        }
        _globalProgressText.text = $"{done} / {total}";
        if (_globalBarFill != null)
            SetFill(_globalBarFill, total > 0 ? (float)done / total : 0f);
    }

    void RefreshGenreProgress()
    {
        var all       = MovieCatalogRuntime.AllMovies ?? allMovies;
        var completed = _studio?.CompletedMovieKeys;
        if (all == null || _genreProgressLabels.Count == 0) return;

        var countByGenre = new Dictionary<MovieGenre, int>();
        var totalByGenre = new Dictionary<MovieGenre, int>();

        foreach (var m in all)
        {
            if (m == null) continue;
            totalByGenre.TryAdd(m.genre, 0); totalByGenre[m.genre]++;
            if (completed != null && completed.Contains(m.name))
            { countByGenre.TryAdd(m.genre, 0); countByGenre[m.genre]++; }
        }

        foreach (var kv in _genreProgressLabels)
        {
            if (kv.Value == null) continue;
            int disc  = countByGenre.TryGetValue(kv.Key, out var c) ? c : 0;
            int total = totalByGenre.TryGetValue(kv.Key, out var t) ? t : 0;
            kv.Value.text = $"{disc} / {total}";

            if (_genreBarFills.TryGetValue(kv.Key, out var fill) && fill != null)
                SetFill(fill, total > 0 ? (float)disc / total : 0f);

            RefreshGenreCoverIcon(kv.Key);
        }
    }

    void RefreshGenreCoverIcon(MovieGenre genre)
    {
        // Search deep because cards are nested inside Scroll/Viewport/Content/Row
        var genreName = "Genre_" + genre;
        Transform card = null;
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name == genreName) { card = t; break; }
        }
        if (card == null) return;

        var cover = card.Find("Cover");
        if (cover == null) return;

        var sprite = UIIconCatalog.GetGenre(genre);
        UIIconGraphic.ApplyCoverIcon(cover, sprite, "CoverIcon", 56f);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static void SetFill(RectTransform fill, float pct)
    {
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(Mathf.Clamp01(pct), 1f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
    }

    static string BuildSynopsis(MovieConfig cfg)
    {
        // Use per-language synopsis fields when available; fall back to the
        // canonical synopsis (Spanish), then tagline, then the "none" placeholder.
        string langSynopsis = Loc.LanguageCode switch
        {
            "en" => cfg.synopsisEn,
            "fr" => cfg.synopsisFr,
            "de" => cfg.synopsisDe,
            "ja" => cfg.synopsisJa,
            _    => null,
        };
        if (!string.IsNullOrEmpty(langSynopsis)) return langSynopsis;
        if (!string.IsNullOrEmpty(cfg.synopsis))  return cfg.synopsis;
        if (!string.IsNullOrEmpty(cfg.tagline))   return cfg.tagline;
        return Loc.Get(LocKeys.CollectionSynopsisNone);
    }

    static Color GenreColor(MovieGenre g)
    {
        foreach (var def in Genres)
            if (def.genre == g) return def.color;
        return new Color(0.20f, 0.50f, 0.80f);
    }

    static string GenreLabel(MovieGenre g) => GenreLoc.GetLabel(g);

    static string RarityLabel(MovieRarity r) => ProductionLoc.GetRarityLabel(r);

    static Color RarityColor(MovieRarity r) => r switch
    {
        MovieRarity.Legendary => new Color(0.95f, 0.77f, 0.06f),
        MovieRarity.Epic      => new Color(0.61f, 0.35f, 0.71f),
        MovieRarity.Rare      => new Color(0.25f, 0.55f, 0.90f),
        _                     => new Color(0.28f, 0.30f, 0.40f),
    };

    static int RarityOrder(MovieRarity r) => r switch
    {
        MovieRarity.Legendary => 3,
        MovieRarity.Epic      => 2,
        MovieRarity.Rare      => 1,
        _                     => 0,
    };

    static Color ParseHexColor(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return new Color(0.15f, 0.18f, 0.28f);
        if (!hex.StartsWith("#")) hex = "#" + hex;
        return ColorUtility.TryParseHtmlString(hex, out var c) ? c : new Color(0.15f, 0.18f, 0.28f);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
