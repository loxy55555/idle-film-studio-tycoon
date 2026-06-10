using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Collection sub-tab: discovery progress, genres, sagas, catalog grid.</summary>
public class MovieCollectionUI : MonoBehaviour
{
    static readonly Color BG_DEEP   = new Color(0.04f, 0.04f, 0.10f);
    static readonly Color BG_CARD   = new Color(0.10f, 0.10f, 0.19f);
    static readonly Color TEXT_PRI  = Color.white;
    static readonly Color TEXT_DIM  = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color ACCENT_GOLD = new Color(0.95f, 0.77f, 0.06f);
    static readonly Color ACCENT_GREEN = new Color(0.18f, 0.80f, 0.44f);

    [HideInInspector] public MovieConfig[] allMovies;
    [HideInInspector] public RectTransform gridContent;

    TextMeshProUGUI _summaryText;
    TextMeshProUGUI _statsText;
    TextMeshProUGUI _sagaHeaderText;
    RectTransform   _sagaSection;
    RectTransform   _gridSection;

    StudioManager _studio;
    StudioLevelSystem _level;
    CitySystem _city;

    void Awake()
    {
        BuildLayout();
        GameHub.OnGameReady += Bind;
    }

    void OnEnable()
    {
        if (GameHub.Instance != null)
            Bind();
    }

    void OnDestroy() => GameHub.OnGameReady -= Bind;

    void Bind()
    {
        Unbind();
        var hub = GameHub.Instance;
        if (hub == null) return;

        _studio = hub.studio;
        _level  = hub.studioLevel;
        _city   = hub.city;

        if (_studio != null)
            _studio.OnMovieCompleted += OnMovieCompleted;
        if (allMovies == null || allMovies.Length == 0)
        {
            var tab = Object.FindAnyObjectByType<MovieTabUI>();
            if (tab != null) allMovies = tab.allMovies;
        }

        Refresh();
    }

    void Unbind()
    {
        if (_studio != null)
            _studio.OnMovieCompleted -= OnMovieCompleted;
    }

    void OnMovieCompleted(MovieCompletePayload _) => Refresh();

    public void Refresh()
    {
        if (gridContent == null) return;
        if (_studio == null && GameHub.Instance != null)
            Bind();
        if (_studio == null) return;

        var completed = _studio.CompletedMovieKeys;
        var snap = MovieCollectionService.BuildSnapshot(allMovies, completed, _studio.MovieHistory);

        if (_summaryText != null)
        {
            _summaryText.text =
                "PELÍCULAS DESCUBIERTAS\n" +
                $"{snap.discoveredTotal} / {snap.catalogTotal}\n" +
                $"{snap.completionPercent:0}%";
        }

        if (_statsText != null)
        {
            var genreLines = new List<string> { "PROGRESO POR GÉNERO" };
            foreach (var g in snap.genres)
            {
                if (g.total <= 0) continue;
                genreLines.Add($"{MovieCollectionService.GenreLabel(g.genre)}\n{g.discovered} / {g.total}");
            }

            genreLines.Add("");
            genreLines.Add("ESTADÍSTICAS");
            genreLines.Add($"Películas descubiertas\n{snap.discoveredTotal} / {snap.catalogTotal}");
            genreLines.Add($"Sagas completadas\n{snap.sagasCompleted} / {snap.sagasTracked}");
            genreLines.Add($"Género más completado\n{snap.topGenreLabel} ({snap.topGenreDiscovered}/{snap.topGenreTotal})");
            genreLines.Add($"Última película descubierta\n{snap.lastDiscoveredTitle}");

            _statsText.text = string.Join("\n", genreLines);
        }

        RebuildSagaSection(snap.sagas);
        RebuildGrid(completed);
    }

    void RebuildSagaSection(MovieCollectionService.SagaProgress[] sagas)
    {
        if (_sagaSection == null) return;

        for (int i = _sagaSection.childCount - 1; i >= 0; i--)
            Destroy(_sagaSection.GetChild(i).gameObject);

        if (sagas == null || sagas.Length == 0)
        {
            if (_sagaHeaderText != null) _sagaHeaderText.text = "SAGAS";
            AddBodyText(_sagaSection, "Sin sagas definidas todavía.", TEXT_DIM);
            return;
        }

        if (_sagaHeaderText != null) _sagaHeaderText.text = "SAGAS";
        foreach (var saga in sagas)
        {
            string line = $"{saga.displayName}\n{saga.discovered} / {saga.total} películas";
            AddBodyText(_sagaSection, line, saga.discovered >= saga.total ? ACCENT_GREEN : TEXT_PRI);
        }
    }

    void RebuildGrid(IReadOnlyCollection<string> completedKeys)
    {
        for (int i = gridContent.childCount - 1; i >= 0; i--)
            Destroy(gridContent.GetChild(i).gameObject);

        var sorted = MovieCollectionService.SortForDisplay(allMovies, completedKeys, _studio, _level, _city);
        foreach (var cfg in sorted)
            BuildCollectionCard(cfg, MovieCollectionService.IsDiscovered(cfg, completedKeys));
    }

    void BuildCollectionCard(MovieConfig cfg, bool discovered)
    {
        var card = new GameObject("Col_" + cfg.name, typeof(RectTransform), typeof(Image));
        card.transform.SetParent(gridContent, false);
        card.GetComponent<Image>().color = BG_CARD;
        var le = card.AddComponent<LayoutElement>();
        le.preferredHeight = 96f;
        le.minHeight = 96f;

        var hlg = card.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(10, 10, 8, 8);
        hlg.spacing = 10;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;

        var posterGo = new GameObject("Poster", typeof(RectTransform), typeof(Image));
        posterGo.transform.SetParent(card.transform, false);
        var posterImg = posterGo.GetComponent<Image>();
        var posterLE = posterGo.AddComponent<LayoutElement>();
        posterLE.preferredWidth = 56f;
        posterLE.preferredHeight = 72f;
        posterLE.flexibleWidth = 0f;

        if (discovered)
        {
            if (cfg.posterSprite != null)
            {
                posterImg.sprite = cfg.posterSprite;
                posterImg.color = Color.white;
            }
            else
            {
                ColorUtility.TryParseHtmlString(cfg.posterColorHex, out Color c);
                posterImg.color = c;
            }
        }
        else
        {
            posterImg.color = new Color(0.12f, 0.12f, 0.20f);
        }

        var info = new GameObject("Info", typeof(RectTransform));
        info.transform.SetParent(card.transform, false);
        info.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var vlg = info.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperLeft;

        if (discovered)
        {
            int cityReq = CityProgressionRules.GetMovieRequiredCity(cfg);
            AddCardLine(info.transform, cfg.movieName, 17, TEXT_PRI, FontStyles.Bold);
            AddCardLine(info.transform,
                $"{MovieCollectionService.GenreLabel(cfg.genre)} · {MovieCollectionService.TierLabel(cfg.catalogTier)}",
                14, TEXT_DIM);
            AddCardLine(info.transform,
                $"Ciudad {cityReq} · {CityProgressionRules.GetCityDisplayName(cityReq)}",
                13, ACCENT_GOLD);
            if (!string.IsNullOrEmpty(cfg.sagaId))
                AddCardLine(info.transform, $"Saga: {cfg.sagaId}", 12, TEXT_DIM);
        }
        else
        {
            AddCardLine(info.transform, "🔒 Desconocida", 18, TEXT_DIM, FontStyles.Bold);
            AddCardLine(info.transform, "??????", 16, TEXT_DIM);
        }
    }

    void BuildLayout()
    {
        var root = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        Stretch(root);

        var scrollGo = new GameObject("CollectionScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollGo.transform.SetParent(transform, false);
        Stretch(scrollGo.GetComponent<RectTransform>());
        scrollGo.GetComponent<Image>().color = BG_DEEP;

        var scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scrollGo.transform, false);
        Stretch(viewport.GetComponent<RectTransform>());
        scroll.viewport = viewport.GetComponent<RectTransform>();

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.offsetMin = contentRT.offsetMax = Vector2.zero;
        scroll.content = contentRT;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 12, 12);
        vlg.spacing = 10;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var summaryCard = MakeSection(content.transform, "SummaryCard", out _summaryText, "COLECCIÓN");
        _summaryText.fontSize = 20;
        _summaryText.alignment = TextAlignmentOptions.Center;

        MakeSection(content.transform, "StatsCard", out _statsText, "DETALLE");
        _statsText.fontSize = 15;

        var sagaCard = MakeSection(content.transform, "SagaCard", out _sagaHeaderText, "SAGAS");
        _sagaSection = sagaCard;

        var gridCard = MakeSection(content.transform, "GridCard", out var gridHdr, "CATÁLOGO");
        _gridSection = gridCard;

        var gridScroll = new GameObject("GridScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        gridScroll.transform.SetParent(gridCard, false);
        gridScroll.GetComponent<Image>().color = Color.clear;
        var gridScrollLE = gridScroll.AddComponent<LayoutElement>();
        gridScrollLE.preferredHeight = 420f;
        gridScrollLE.flexibleHeight = 1f;

        var gridSR = gridScroll.GetComponent<ScrollRect>();
        gridSR.horizontal = false;
        gridSR.vertical = true;

        var gridViewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        gridViewport.transform.SetParent(gridScroll.transform, false);
        Stretch(gridViewport.GetComponent<RectTransform>());
        gridSR.viewport = gridViewport.GetComponent<RectTransform>();

        var gridContentGo = new GameObject("GridContent", typeof(RectTransform));
        gridContentGo.transform.SetParent(gridViewport.transform, false);
        gridContent = gridContentGo.GetComponent<RectTransform>();
        gridContent.anchorMin = new Vector2(0f, 1f);
        gridContent.anchorMax = new Vector2(1f, 1f);
        gridContent.pivot = new Vector2(0.5f, 1f);
        gridContent.offsetMin = gridContent.offsetMax = Vector2.zero;
        gridSR.content = gridContent;

        var gridVLG = gridContentGo.AddComponent<VerticalLayoutGroup>();
        gridVLG.spacing = 6;
        gridVLG.childControlWidth = gridVLG.childControlHeight = true;
        gridVLG.childForceExpandWidth = true;
        gridVLG.childForceExpandHeight = false;
        gridContentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    static RectTransform MakeSection(Transform parent, string name, out TextMeshProUGUI body, string header)
    {
        var card = new GameObject(name, typeof(RectTransform), typeof(Image));
        card.transform.SetParent(parent, false);
        card.GetComponent<Image>().color = BG_CARD;
        card.AddComponent<LayoutElement>().preferredHeight = 0f;

        var vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 10, 10);
        vlg.spacing = 6;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var hdr = RuntimeTmpText.Create(card.transform, header, 14, TEXT_DIM, FontStyles.Bold);
        hdr.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        body = RuntimeTmpText.Create(card.transform, "—", 16, TEXT_PRI);
        body.textWrappingMode = TextWrappingModes.Normal;
        body.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;

        return card.GetComponent<RectTransform>();
    }

    static void AddBodyText(RectTransform parent, string text, Color color)
    {
        var tmp = RuntimeTmpText.Create(parent, text, 15, color);
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
    }

    static void AddCardLine(Transform parent, string text, float size, Color color, FontStyles style = FontStyles.Normal)
    {
        var tmp = RuntimeTmpText.Create(parent, text, size, color, style);
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.gameObject.AddComponent<LayoutElement>().preferredHeight = size + 4f;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
