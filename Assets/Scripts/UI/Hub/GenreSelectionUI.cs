using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Collection L1 screen — 2-column grid of large genre cards.
/// Tapping a card calls OnGenreSelected (hooked to MovieCollectionUI to filter by genre).
/// Placed above MovieCollectionUI in the Collection panel; MovieCollectionUI shows below when a genre is selected.
/// </summary>
public class GenreSelectionUI : MonoBehaviour
{
    public System.Action<MovieGenre?> OnGenreSelected;

    // ── Genre definitions ──────────────────────────────────────────────────────
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

    static readonly Color BG_CARD      = new Color(0.10f, 0.10f, 0.18f);
    static readonly Color TEXT_PRIMARY = Color.white;
    static readonly Color TEXT_DIM     = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color PROGRESS_BG  = new Color(0.06f, 0.06f, 0.12f);

    RectTransform _gridRoot;
    bool _built;

    void Awake()
    {
        EnsureLayout();
    }

    void OnEnable()
    {
        EnsureLayout();
        RefreshProgress();
    }

    void EnsureLayout()
    {
        if (_built) return;
        _built = true;
        BuildLayout();
    }

    void BuildLayout()
    {
        var root = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        Stretch(root);

        var scroll = new GameObject("GenreScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scroll.transform.SetParent(transform, false);
        Stretch(scroll.GetComponent<RectTransform>());
        scroll.GetComponent<Image>().color = Color.clear;
        var sr = scroll.GetComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 30f;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scroll.transform, false);
        Stretch(viewport.GetComponent<RectTransform>());
        sr.viewport = viewport.GetComponent<RectTransform>();

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.offsetMin = contentRT.offsetMax = Vector2.zero;
        sr.content = contentRT;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 14, 14);
        vlg.spacing = 12;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Header
        var hdr = new GameObject("Header", typeof(RectTransform));
        hdr.transform.SetParent(content.transform, false);
        hdr.AddComponent<LayoutElement>().preferredHeight = 40f;
        var hdrTxt = hdr.AddComponent<TextMeshProUGUI>();
        hdrTxt.text = Loc.Get(LocKeys.CollectionSelectGenre);
        hdrTxt.fontSize = 22;
        hdrTxt.fontStyle = FontStyles.Bold;
        hdrTxt.color = TEXT_DIM;
        hdrTxt.alignment = TextAlignmentOptions.MidlineLeft;

        // 2-column genre grid (pairs of rows)
        _gridRoot = new GameObject("GenreGrid", typeof(RectTransform)).GetComponent<RectTransform>();
        _gridRoot.SetParent(content.transform, false);
        var gridVLG = _gridRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        gridVLG.spacing = 12;
        gridVLG.childControlWidth = gridVLG.childControlHeight = true;
        gridVLG.childForceExpandWidth = true;
        gridVLG.childForceExpandHeight = false;
        _gridRoot.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        for (int i = 0; i < Genres.Length; i += 2)
        {
            var row = new GameObject("Row" + i, typeof(RectTransform));
            row.transform.SetParent(_gridRoot, false);
            var rowLE = row.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 120f;
            var rowHLG = row.AddComponent<HorizontalLayoutGroup>();
            rowHLG.spacing = 12;
            rowHLG.childControlWidth = rowHLG.childControlHeight = true;
            rowHLG.childForceExpandWidth = true;
            rowHLG.childForceExpandHeight = true;

            BuildGenreCard(row.transform, Genres[i]);
            if (i + 1 < Genres.Length)
                BuildGenreCard(row.transform, Genres[i + 1]);
            else
                new GameObject("Spacer", typeof(RectTransform)).transform.SetParent(row.transform, false);
        }
    }

    void BuildGenreCard(Transform parent, (MovieGenre genre, string emoji, Color color) def)
    {
        var card = new GameObject("Genre_" + def.genre, typeof(RectTransform), typeof(Image), typeof(Button));
        card.transform.SetParent(parent, false);
        var img = card.GetComponent<Image>();
        img.color = BG_CARD;
        SetRounded(card.GetComponent<RectTransform>(), 14);

        var vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 12, 12);
        vlg.spacing = 4;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Color accent stripe at top
        var stripe = new GameObject("Stripe", typeof(RectTransform), typeof(Image));
        stripe.transform.SetParent(card.transform, false);
        stripe.GetComponent<Image>().color = def.color;
        stripe.AddComponent<LayoutElement>().preferredHeight = 5f;

        // Emoji icon
        var emojiTxt = new GameObject("Emoji", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        emojiTxt.transform.SetParent(card.transform, false);
        emojiTxt.text = def.emoji;
        emojiTxt.fontSize = 32;
        emojiTxt.alignment = TextAlignmentOptions.Center;
        emojiTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;

        // Genre name
        var nameTxt = new GameObject("Name", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        nameTxt.transform.SetParent(card.transform, false);
        nameTxt.text = Loc.Get(GenreLocKey(def.genre));
        nameTxt.fontSize = 16;
        nameTxt.fontStyle = FontStyles.Bold;
        nameTxt.color = TEXT_PRIMARY;
        nameTxt.alignment = TextAlignmentOptions.Center;
        nameTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        // Progress text placeholder
        var progressTxt = new GameObject("Progress", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        progressTxt.transform.SetParent(card.transform, false);
        progressTxt.text = "0 / 0";
        progressTxt.fontSize = 13;
        progressTxt.color = TEXT_DIM;
        progressTxt.alignment = TextAlignmentOptions.Center;
        progressTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        progressTxt.name = "Progress_" + def.genre;

        // Wire click
        var genre = def.genre;
        card.GetComponent<Button>().onClick.AddListener(() => OnGenreSelected?.Invoke(genre));
    }

    public void RefreshProgress()
    {
        if (_gridRoot == null) return;

        var studio = GameHub.Instance?.studio;
        if (studio == null) return;

        var completed = studio.CompletedMovieKeys;
        var allMovies = MovieCatalogRuntime.AllMovies;
        if (allMovies == null) return;

        var countByGenre = new Dictionary<MovieGenre, int>();
        var totalByGenre = new Dictionary<MovieGenre, int>();

        foreach (var m in allMovies)
        {
            if (!totalByGenre.ContainsKey(m.genre)) totalByGenre[m.genre] = 0;
            totalByGenre[m.genre]++;
            if (completed != null && completed.Contains(m.movieName))
            {
                if (!countByGenre.ContainsKey(m.genre)) countByGenre[m.genre] = 0;
                countByGenre[m.genre]++;
            }
        }

        foreach (var (genre, _, _) in Genres)
        {
            int disc  = countByGenre.TryGetValue(genre, out var c) ? c : 0;
            int total = totalByGenre.TryGetValue(genre, out var t) ? t : 0;

            var progressLabel = FindDeep(_gridRoot, "Progress_" + genre);
            if (progressLabel != null)
                progressLabel.text = $"{disc} / {total}";
        }
    }

    TextMeshProUGUI FindDeep(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            if (t.name == name) return t;
        return null;
    }

    static string GenreLocKey(MovieGenre g) => g switch
    {
        MovieGenre.Action      => LocKeys.GenreAction,
        MovieGenre.Drama       => LocKeys.GenreDrama,
        MovieGenre.Horror      => LocKeys.GenreHorror,
        MovieGenre.Comedy      => LocKeys.GenreComedy,
        MovieGenre.Romance     => LocKeys.GenreRomance,
        MovieGenre.SciFi       => LocKeys.GenreSciFi,
        MovieGenre.Fantasy     => LocKeys.GenreFantasy,
        MovieGenre.Thriller    => LocKeys.GenreThriller,
        MovieGenre.Animation   => LocKeys.GenreAnimation,
        MovieGenre.Documentary => LocKeys.GenreDocumentary,
        _                      => LocKeys.GenreAction,
    };

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
        img.type = Image.Type.Sliced;
    }
}
