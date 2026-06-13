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
    static readonly Color BG_CARD   = new Color(0.10f, 0.10f, 0.19f);
    static readonly Color BG_DARK   = new Color(0.04f, 0.04f, 0.08f);
    static readonly Color TEXT_PRI  = Color.white;
    static readonly Color TEXT_SEC  = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color ACCENT_GN = new Color(0.18f, 0.80f, 0.44f);
    static readonly Color ACCENT_GL = new Color(0.95f, 0.77f, 0.06f);
    static readonly Color BAR_BG    = new Color(0.09f, 0.09f, 0.18f);

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
    TextMeshProUGUI _l2GenreTitle;
    TextMeshProUGUI _l2CountLabel;
    RectTransform   _l2GridContent;

    // ── L3 views (popup) ──────────────────────────────────────────────────────
    GameObject      _l3Popup;
    Image           _l3PosterFrame;
    Image           _l3PosterImg;
    TextMeshProUGUI _l3Title;
    TextMeshProUGUI _l3Genre;
    Image           _l3GenreBg;
    TextMeshProUGUI _l3Rarity;
    Image           _l3RarityBg;
    Image           _l3StatusPill;
    TextMeshProUGUI _l3Status;
    TextMeshProUGUI _l3Synopsis;

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
        Unbind();
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

        var hdrTitle = RuntimeTmpText.Create(titleRow.transform, "FILMOTECA",
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

        // Album cover area — genre-tinted, big emoji centered
        var cover = new GameObject("Cover", typeof(RectTransform), typeof(Image));
        cover.transform.SetParent(card.transform, false);
        cover.GetComponent<Image>().color = Color.Lerp(def.color, BG_DARK, 0.62f);
        cover.GetComponent<Image>().raycastTarget = false;
        cover.AddComponent<LayoutElement>().flexibleHeight = 1f;

        var emojiTxt = RuntimeTmpText.Create(cover.transform, def.emoji,
            42f, Color.white, FontStyles.Normal, TextAlignmentOptions.Center, "Emoji");
        emojiTxt.raycastTarget = false;
        Stretch(emojiTxt.rectTransform);

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
            13f, def.color, FontStyles.Bold, TextAlignmentOptions.MidlineRight, "Progress");
        progressTxt.raycastTarget = false;
        progressTxt.gameObject.AddComponent<LayoutElement>().preferredWidth = 64f;
        _genreProgressLabels[def.genre] = progressTxt;

        // Progress fill bar — bottom edge, genre colored
        var barBg = new GameObject("Bar", typeof(RectTransform), typeof(Image));
        barBg.transform.SetParent(card.transform, false);
        barBg.GetComponent<Image>().color = BAR_BG;
        barBg.GetComponent<Image>().raycastTarget = false;
        barBg.AddComponent<LayoutElement>().preferredHeight = 6f;

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(barBg.transform, false);
        fill.GetComponent<Image>().color = def.color;
        fill.GetComponent<Image>().raycastTarget = false;
        var fillRT = fill.GetComponent<RectTransform>();
        SetFill(fillRT, 0f);
        _genreBarFills[def.genre] = fillRT;

        var genre = def.genre;
        card.GetComponent<Button>().onClick.AddListener(() => ShowMovieGrid(genre));
    }

    // ── L2: Poster grid ───────────────────────────────────────────────────────

    void BuildL2MovieGrid(RectTransform parent)
    {
        _l2Panel = new GameObject("L2_MovieGrid", typeof(RectTransform), typeof(Image));
        _l2Panel.transform.SetParent(parent, false);
        _l2Panel.GetComponent<Image>().color = Color.clear;
        Stretch(_l2Panel.GetComponent<RectTransform>());

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

        _l2GenreTitle = RuntimeTmpText.Create(backBar.transform, "GÉNERO",
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

    // ── L3: Movie detail popup ────────────────────────────────────────────────

    void BuildL3Popup(RectTransform parent)
    {
        _l3Popup = new GameObject("L3_Popup", typeof(RectTransform), typeof(Image));
        _l3Popup.transform.SetParent(parent, false);
        var popupImg = _l3Popup.GetComponent<Image>();
        popupImg.color = new Color(0f, 0f, 0f, 0.82f);
        Stretch(_l3Popup.GetComponent<RectTransform>());

        // Close on backdrop tap
        popupImg.raycastTarget = true;
        var backDrop = _l3Popup.AddComponent<Button>();
        backDrop.onClick.AddListener(ClosePopup);

        // Panel centered — image blocks clicks so they don't reach the backdrop
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(_l3Popup.transform, false);
        var panelImg = panel.GetComponent<Image>();
        panelImg.color = new Color(0.09f, 0.10f, 0.17f);
        panelImg.raycastTarget = true;
        var panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.06f, 0.10f);
        panelRT.anchorMax = new Vector2(0.94f, 0.92f);
        panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;

        var panelVLG = panel.AddComponent<VerticalLayoutGroup>();
        panelVLG.padding = new RectOffset(18, 18, 12, 18);
        panelVLG.spacing = 10;
        panelVLG.childControlWidth = panelVLG.childControlHeight = true;
        panelVLG.childForceExpandWidth = true; panelVLG.childForceExpandHeight = false;

        // Close button row
        var closeRow = new GameObject("CloseRow", typeof(RectTransform));
        closeRow.transform.SetParent(panel.transform, false);
        closeRow.AddComponent<LayoutElement>().preferredHeight = 36f;
        var closeHLG = closeRow.AddComponent<HorizontalLayoutGroup>();
        closeHLG.childControlWidth = closeHLG.childControlHeight = true;
        closeHLG.childForceExpandWidth = false; closeHLG.childForceExpandHeight = true;

        var spacerGo = new GameObject("Spacer", typeof(RectTransform));
        spacerGo.transform.SetParent(closeRow.transform, false);
        spacerGo.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var closeBtnGo = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnGo.transform.SetParent(closeRow.transform, false);
        closeBtnGo.GetComponent<Image>().color = new Color(0.30f, 0.10f, 0.10f);
        closeBtnGo.AddComponent<LayoutElement>().preferredWidth = 44f;
        var closeLbl = RuntimeTmpText.Create(closeBtnGo.transform, "✕",
            16f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center, "Lbl");
        closeLbl.raycastTarget = false;
        Stretch(closeLbl.rectTransform);
        closeBtnGo.GetComponent<Button>().onClick.AddListener(ClosePopup);

        // Framed poster — outer frame colored by rarity
        var frameGo = new GameObject("PosterFrame", typeof(RectTransform), typeof(Image));
        frameGo.transform.SetParent(panel.transform, false);
        _l3PosterFrame = frameGo.GetComponent<Image>();
        _l3PosterFrame.raycastTarget = false;
        frameGo.AddComponent<LayoutElement>().preferredHeight = 200f;

        var posterGo = new GameObject("Poster", typeof(RectTransform), typeof(Image));
        posterGo.transform.SetParent(frameGo.transform, false);
        _l3PosterImg = posterGo.GetComponent<Image>();
        _l3PosterImg.raycastTarget = false;
        var posterRT = posterGo.GetComponent<RectTransform>();
        Stretch(posterRT);
        posterRT.offsetMin = new Vector2(4f, 4f);
        posterRT.offsetMax = new Vector2(-4f, -4f);

        // Movie title
        _l3Title = RuntimeTmpText.Create(panel.transform, "—",
            22f, TEXT_PRI, FontStyles.Bold, TextAlignmentOptions.Center, "L3Title");
        _l3Title.raycastTarget = false;
        _l3Title.textWrappingMode = TextWrappingModes.Normal;
        _l3Title.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;

        // Badge row: genre | rarity
        var badgeRow = new GameObject("BadgeRow", typeof(RectTransform));
        badgeRow.transform.SetParent(panel.transform, false);
        badgeRow.AddComponent<LayoutElement>().preferredHeight = 28f;
        var bdHLG = badgeRow.AddComponent<HorizontalLayoutGroup>();
        bdHLG.spacing = 8; bdHLG.childAlignment = TextAnchor.MiddleCenter;
        bdHLG.childControlWidth = bdHLG.childControlHeight = true;
        bdHLG.childForceExpandWidth = bdHLG.childForceExpandHeight = false;

        (_l3Genre,  _l3GenreBg)  = BuildBadge(badgeRow.transform, "GenreBadge",  "—", new Color(0.20f, 0.50f, 0.80f));
        (_l3Rarity, _l3RarityBg) = BuildBadge(badgeRow.transform, "RarityBadge", "—", new Color(0.50f, 0.10f, 0.75f));

        // Status pill — full width
        var pillGo = new GameObject("StatusPill", typeof(RectTransform), typeof(Image));
        pillGo.transform.SetParent(panel.transform, false);
        _l3StatusPill = pillGo.GetComponent<Image>();
        _l3StatusPill.raycastTarget = false;
        pillGo.AddComponent<LayoutElement>().preferredHeight = 32f;

        _l3Status = RuntimeTmpText.Create(pillGo.transform, "—",
            14f, TEXT_PRI, FontStyles.Bold, TextAlignmentOptions.Center, "Lbl");
        _l3Status.raycastTarget = false;
        Stretch(_l3Status.rectTransform);

        // Synopsis
        _l3Synopsis = RuntimeTmpText.Create(panel.transform, "—",
            12f, TEXT_SEC, FontStyles.Normal, TextAlignmentOptions.Center, "L3Synopsis");
        _l3Synopsis.raycastTarget = false;
        _l3Synopsis.textWrappingMode = TextWrappingModes.Normal;
        _l3Synopsis.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
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
        if (_l2Panel != null) _l2Panel.SetActive(true);
        if (_l3Popup != null) _l3Popup.SetActive(false);

        if (_l2GenreTitle != null)
            _l2GenreTitle.text = GenreLabel(genre).ToUpper();

        PopulateMovieGrid(genre);
    }

    void ShowMoviePopup(MovieConfig cfg)
    {
        if (_l3Popup == null || cfg == null) return;
        _l3Popup.SetActive(true);

        var completed = _studio?.CompletedMovieKeys;
        bool done     = completed != null && completed.Contains(cfg.movieName);

        if (_l3PosterFrame != null) _l3PosterFrame.color = RarityColor(cfg.rarity);
        if (_l3PosterImg   != null) _l3PosterImg.color   = ParseHexColor(cfg.posterColorHex);
        if (_l3Title       != null) _l3Title.text        = cfg.movieName;
        if (_l3Genre       != null) _l3Genre.text        = GenreLabel(cfg.genre).ToUpper();
        if (_l3GenreBg     != null) _l3GenreBg.color     = Color.Lerp(GenreColor(cfg.genre), BG_DARK, 0.35f);
        if (_l3Rarity      != null) _l3Rarity.text       = RarityLabel(cfg.rarity);
        if (_l3RarityBg    != null) _l3RarityBg.color    = Color.Lerp(RarityColor(cfg.rarity), BG_DARK, 0.30f);
        if (_l3Status      != null)
        {
            _l3Status.text = done ? "✔ COMPLETADA" : "○ PENDIENTE";
            if (_l3StatusPill != null)
                _l3StatusPill.color = done
                    ? new Color(0.10f, 0.40f, 0.22f)
                    : new Color(0.14f, 0.15f, 0.24f);
        }
        if (_l3Synopsis    != null) _l3Synopsis.text = BuildSynopsis(cfg);
    }

    void ClosePopup()
    {
        if (_l3Popup != null) _l3Popup.SetActive(false);
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
            if (completed != null && completed.Contains(m.movieName)) doneCount++;
        }

        // Phase 8.6B: group by rarity — Legendary first, then Epic, Rare, Common
        filtered.Sort((a, b) => RarityOrder(b.rarity).CompareTo(RarityOrder(a.rarity)));

        if (_l2CountLabel != null)
            _l2CountLabel.text = $"{doneCount} / {filtered.Count}";

        const int cols = 3;
        for (int i = 0; i < filtered.Count; i += cols)
        {
            var row = new GameObject("Row" + i, typeof(RectTransform));
            row.transform.SetParent(_l2GridContent, false);
            row.AddComponent<LayoutElement>().preferredHeight = 172f;
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
                }
            }
        }
    }

    void BuildMovieCell(Transform parent, MovieConfig cfg, IReadOnlyCollection<string> completed)
    {
        bool done = completed != null && completed.Contains(cfg.movieName);

        // Cell — background acts as rarity frame (3px border via padding)
        var cell = new GameObject("Movie_" + cfg.movieName, typeof(RectTransform), typeof(Image), typeof(Button));
        cell.transform.SetParent(parent, false);
        cell.GetComponent<Image>().color = done
            ? RarityColor(cfg.rarity)
            : Color.Lerp(RarityColor(cfg.rarity), BG_DARK, 0.55f);

        var vlg = cell.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(3, 3, 3, 3);
        vlg.spacing = 0;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        // Poster — dominant area; dimmed when not completed
        var posterColor = ParseHexColor(cfg.posterColorHex);
        var poster = new GameObject("Poster", typeof(RectTransform), typeof(Image));
        poster.transform.SetParent(cell.transform, false);
        poster.GetComponent<Image>().color = done ? posterColor : Color.Lerp(posterColor, BG_DARK, 0.65f);
        poster.GetComponent<Image>().raycastTarget = false;
        poster.AddComponent<LayoutElement>().flexibleHeight = 1f;

        if (done)
        {
            // ✔ badge anchored to poster top-right
            var badge = new GameObject("Done", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(poster.transform, false);
            badge.GetComponent<Image>().color = new Color(0.12f, 0.62f, 0.32f, 0.95f);
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

        // Name strip
        var nameStrip = new GameObject("NameStrip", typeof(RectTransform), typeof(Image));
        nameStrip.transform.SetParent(cell.transform, false);
        nameStrip.GetComponent<Image>().color = BG_DARK;
        nameStrip.GetComponent<Image>().raycastTarget = false;
        nameStrip.AddComponent<LayoutElement>().preferredHeight = 34f;

        var nameTxt = RuntimeTmpText.Create(nameStrip.transform, cfg.movieName,
            10f, done ? TEXT_PRI : TEXT_SEC, FontStyles.Bold, TextAlignmentOptions.Center, "Name");
        nameTxt.textWrappingMode = TextWrappingModes.Normal;
        nameTxt.overflowMode = TextOverflowModes.Ellipsis;
        nameTxt.maxVisibleLines = 2;
        nameTxt.raycastTarget = false;
        Stretch(nameTxt.rectTransform);
        nameTxt.rectTransform.offsetMin = new Vector2(3f, 1f);
        nameTxt.rectTransform.offsetMax = new Vector2(-3f, -1f);

        var m = cfg;
        cell.GetComponent<Button>().onClick.AddListener(() => ShowMoviePopup(m));
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
            if (completed != null && completed.Contains(m.movieName)) done++;
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
            if (completed != null && completed.Contains(m.movieName))
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
        }
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
        if (!string.IsNullOrEmpty(cfg.tagline)) return cfg.tagline;
        var dur = cfg.duration > 0f ? $"{cfg.duration:0} min" : "—";
        return $"{GenreLabel(cfg.genre)} · {dur} · Nv.{cfg.unlockStudioLevel}";
    }

    static Color GenreColor(MovieGenre g)
    {
        foreach (var def in Genres)
            if (def.genre == g) return def.color;
        return new Color(0.20f, 0.50f, 0.80f);
    }

    static string GenreLabel(MovieGenre g) => g switch
    {
        MovieGenre.Action      => "Acción",
        MovieGenre.Drama       => "Drama",
        MovieGenre.Horror      => "Terror",
        MovieGenre.Comedy      => "Comedia",
        MovieGenre.Romance     => "Romance",
        MovieGenre.SciFi       => "Sci-Fi",
        MovieGenre.Fantasy     => "Fantasía",
        MovieGenre.Thriller    => "Thriller",
        MovieGenre.Animation   => "Animación",
        MovieGenre.Documentary => "Documental",
        _                      => g.ToString(),
    };

    static string RarityLabel(MovieRarity r) => r switch
    {
        MovieRarity.Legendary => "★ LEGENDARIA",
        MovieRarity.Epic      => "◆ ÉPICA",
        MovieRarity.Rare      => "● RARA",
        _                     => "COMÚN",
    };

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
