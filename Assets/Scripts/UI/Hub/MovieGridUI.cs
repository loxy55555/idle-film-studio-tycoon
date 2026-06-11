using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime movie grid.  Movies come from the serialized allMovies[] array
/// (populated by StudioUIBuilder) — no Resources folder required.
/// Creates cards procedurally on GameReady so no prefab is needed.
/// </summary>
public class MovieGridUI : MonoBehaviour
{
    [Header("Movie Catalog (set by builder)")]
    public MovieConfig[] allMovies;

    [Header("Scroll content root")]
    public RectTransform content;

    [Header("Filter")]
    public MovieGenre filterGenre;
    public bool       showAllGenres = true;

    // ── Colors (matching StudioUIBuilder palette) ──
    static readonly Color BG_CARD   = new Color(0.10f, 0.10f, 0.19f);
    static readonly Color TEXT_PRI  = Color.white;
    static readonly Color TEXT_SEC  = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color ACCENT_GREEN = new Color(0.18f, 0.80f, 0.44f);

    static Color GenreColor(MovieGenre g) => g switch
    {
        MovieGenre.Action  => new Color(0.91f, 0.30f, 0.24f),
        MovieGenre.Drama   => new Color(0.20f, 0.60f, 0.86f),
        MovieGenre.Horror  => new Color(0.56f, 0.27f, 0.68f),
        MovieGenre.Comedy  => new Color(0.95f, 0.61f, 0.07f),
        MovieGenre.Romance => new Color(0.91f, 0.12f, 0.55f),
        MovieGenre.SciFi   => new Color(0.15f, 0.68f, 0.38f),
        _                  => Color.gray,
    };

    static string GenreLabel(MovieGenre g) => g switch
    {
        MovieGenre.Action  => "ACCIÓN",
        MovieGenre.Drama   => "DRAMA",
        MovieGenre.Horror  => "TERROR",
        MovieGenre.Comedy  => "COMEDIA",
        MovieGenre.Romance => "ROMANCE",
        MovieGenre.SciFi   => "SCI-FI",
        _                  => g.ToString().ToUpper(),
    };

    private readonly List<MovieButtonUI> _cards = new();

    private void Awake()
    {
        GameHub.OnGameReady += OnGameReady;
    }

    private void OnDestroy()
    {
        GameHub.OnGameReady -= OnGameReady;
    }

    private void OnGameReady()
    {
        GameHub.OnGameReady -= OnGameReady;
        GenerateGrid();
    }

    public void GenerateGrid()
    {
        if (content == null) return;

        // Clear existing
        foreach (var c in _cards) if (c != null) Destroy(c.gameObject);
        _cards.Clear();
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        // Fallback: try Resources if no movies were injected
        MovieConfig[] movies = allMovies;
        if (movies == null || movies.Length == 0)
        {
            movies = Resources.LoadAll<MovieConfig>("Movies");
            if (movies == null || movies.Length == 0)
            {
                var lbl = MakeText(content,
                    "No hay películas disponibles.\nEjecuta: IdleFilm > Generate Movie Database",
                    22, TEXT_SEC);
                lbl.textWrappingMode = TMPro.TextWrappingModes.Normal;
                return;
            }
        }

        // Sort by availability bucket then cost
        var sorted = new List<MovieConfig>(movies.Length);
        foreach (var cfg in movies)
        {
            if (!showAllGenres && cfg.genre != filterGenre) continue;
            sorted.Add(cfg);
        }

        var hub = GameHub.Instance;
        ContentSortOrder.SortMovies(sorted, hub?.studio, hub?.studioLevel, hub?.city);

        foreach (var cfg in sorted)
        {
            var card = BuildMovieCard(cfg);
            _cards.Add(card);
        }
    }

    // ── Runtime card creation ───────────────────────────────────────────────

    MovieButtonUI BuildMovieCard(MovieConfig cfg)
    {
        Color genreCol = GenreColor(cfg.genre);

        // Card root
        var cardGo = new GameObject("Movie_" + cfg.movieName, typeof(RectTransform), typeof(Image));
        cardGo.transform.SetParent(content, false);
        HudSkinProvider.ApplyCard(cardGo.GetComponent<Image>(), HudCardVariant.Primary);
        var cardRT = cardGo.GetComponent<RectTransform>();
        var cardLE = cardGo.AddComponent<LayoutElement>();
        cardLE.preferredHeight = 110;
        cardLE.flexibleWidth   = 1;

        // Horizontal layout inside card
        var hlg = cardGo.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(0, 12, 0, 0);
        hlg.spacing = 10;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;

        // Genre color strip
        var strip = new GameObject("Strip", typeof(RectTransform), typeof(Image));
        strip.transform.SetParent(cardGo.transform, false);
        strip.GetComponent<Image>().color = genreCol;
        var stripLE = strip.AddComponent<LayoutElement>();
        stripLE.preferredWidth  = 8;
        stripLE.flexibleWidth   = 0;
        stripLE.flexibleHeight  = 1;

        // Info block
        var info = new GameObject("Info", typeof(RectTransform), typeof(Image));
        info.transform.SetParent(cardGo.transform, false);
        info.GetComponent<Image>().color = Color.clear;
        var infoLE = info.AddComponent<LayoutElement>();
        infoLE.flexibleWidth = 1;
        var infoVLG = info.AddComponent<VerticalLayoutGroup>();
        infoVLG.padding = new RectOffset(6, 0, 10, 10);
        infoVLG.spacing = 4;
        infoVLG.childControlWidth = infoVLG.childControlHeight = true;
        infoVLG.childForceExpandWidth = true;
        infoVLG.childForceExpandHeight = false;
        infoVLG.childAlignment = TextAnchor.UpperLeft;

        // Genre badge row
        var badgeRow = new GameObject("BadgeRow", typeof(RectTransform), typeof(Image));
        badgeRow.transform.SetParent(info.transform, false);
        badgeRow.GetComponent<Image>().color = Color.clear;
        var brHLG = badgeRow.AddComponent<HorizontalLayoutGroup>();
        brHLG.spacing = 6; brHLG.childAlignment = TextAnchor.MiddleLeft;
        brHLG.childControlWidth = brHLG.childControlHeight = true;
        brHLG.childForceExpandWidth = brHLG.childForceExpandHeight = false;
        var brLE = badgeRow.AddComponent<LayoutElement>(); brLE.preferredHeight = 22;

        var genreLblGo = new GameObject("GenreLbl", typeof(RectTransform), typeof(Image));
        genreLblGo.transform.SetParent(badgeRow.transform, false);
        genreLblGo.GetComponent<Image>().color = genreCol;
        var genreLblLE = genreLblGo.AddComponent<LayoutElement>();
        genreLblLE.preferredWidth = 80; genreLblLE.preferredHeight = 20;
        // Text is a child of the badge (can't add TMP + Image to same object)
        var genreTxtGo = new GameObject("Lbl", typeof(RectTransform));
        genreTxtGo.transform.SetParent(genreLblGo.transform, false);
        genreTxtGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        genreTxtGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
        genreTxtGo.GetComponent<RectTransform>().offsetMin = genreTxtGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        var genreTxt = genreTxtGo.AddComponent<TextMeshProUGUI>();
        genreTxt.text = GenreLabel(cfg.genre); genreTxt.fontSize = 14; genreTxt.color = TEXT_PRI;
        genreTxt.fontStyle = FontStyles.Bold; genreTxt.alignment = TextAlignmentOptions.Center;
        genreTxt.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

        // Title
        var titleTxt = MakeText(info.GetComponent<RectTransform>(), cfg.movieName, 20, TEXT_PRI);
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.GetComponent<RectTransform>().gameObject.AddComponent<LayoutElement>().preferredHeight = 26;

        // Stats row (cost | reward | time)
        var statsRow = new GameObject("StatsRow", typeof(RectTransform), typeof(Image));
        statsRow.transform.SetParent(info.transform, false);
        statsRow.GetComponent<Image>().color = Color.clear;
        var srHLG = statsRow.AddComponent<HorizontalLayoutGroup>();
        srHLG.spacing = 12; srHLG.childAlignment = TextAnchor.MiddleLeft;
        srHLG.childControlWidth = srHLG.childControlHeight = true;
        srHLG.childForceExpandWidth = srHLG.childForceExpandHeight = false;
        var srLE = statsRow.AddComponent<LayoutElement>(); srLE.preferredHeight = 20;

        var costTxt    = MakeText(statsRow.GetComponent<RectTransform>(), AnimatedMoneyText.FormatMoney((long)cfg.cost), 16, TEXT_SEC);
        var rewardTxt  = MakeText(statsRow.GetComponent<RectTransform>(), "+" + AnimatedMoneyText.FormatMoney((long)cfg.baseReward), 16, ACCENT_GREEN);
        var durationTxt= MakeText(statsRow.GetComponent<RectTransform>(), $"{cfg.duration:0.0}s", 16, TEXT_SEC);

        // Right: produce button
        var btnGo = new GameObject("ProduceBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(cardGo.transform, false);
        HudSkinProvider.ApplyButton(btnGo.GetComponent<Image>(), HudButtonVariant.Success);
        btnGo.AddComponent<UIButtonScale>();
        var btnLE = btnGo.AddComponent<LayoutElement>();
        btnLE.preferredWidth  = 110;
        btnLE.flexibleWidth   = 0;
        btnLE.flexibleHeight  = 1;
        var btnLbl = new GameObject("Lbl", typeof(RectTransform));
        btnLbl.transform.SetParent(btnGo.transform, false);
        btnLbl.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        btnLbl.GetComponent<RectTransform>().anchorMax = Vector2.one;
        btnLbl.GetComponent<RectTransform>().offsetMin = btnLbl.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        var btnTMP = btnLbl.AddComponent<TextMeshProUGUI>();
        btnTMP.text = "PRODUCIR"; btnTMP.fontSize = 15; btnTMP.fontStyle = FontStyles.Bold;
        btnTMP.alignment = TextAlignmentOptions.Center; btnTMP.color = TEXT_PRI;
        btnTMP.textWrappingMode = TMPro.TextWrappingModes.Normal;

        // Wire MovieButtonUI
        var mbUI = cardGo.AddComponent<MovieButtonUI>();
        mbUI.movieConfig  = cfg;
        mbUI.titleText    = titleTxt;
        mbUI.costText     = costTxt;
        mbUI.rewardText   = rewardTxt;
        mbUI.durationText = durationTxt;
        mbUI.genreBadge   = genreLblGo.GetComponent<Image>();
        mbUI.produceButton = btnGo.GetComponent<Button>();

        return mbUI;
    }

    // ── Mini helpers ──────────────────────────────────────────────────────────

    TextMeshProUGUI MakeText(RectTransform parent, string text, float size, Color color)
    {
        var go = new GameObject("Txt", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Truncate;
        return tmp;
    }

}
