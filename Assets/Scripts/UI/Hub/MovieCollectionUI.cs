using System.Collections.Generic;

using TMPro;

using UnityEngine;

using UnityEngine.UI;



/// <summary>Collection sub-tab — album-style movie grid (300-cap catalog).</summary>

public class MovieCollectionUI : MonoBehaviour

{

    const int AlbumColumns   = 2;

    const float CellWidth    = 168f;

    const float CellHeight   = 248f;

    const float GridSpacing  = 10f;



    static readonly Color BG_DEEP      = new Color(0.04f, 0.04f, 0.10f);

    static readonly Color BG_CARD      = new Color(0.10f, 0.10f, 0.19f);

    static readonly Color BG_POSTER    = new Color(0.12f, 0.12f, 0.20f);

    static readonly Color TEXT_PRI     = Color.white;

    static readonly Color TEXT_DIM     = new Color(0.54f, 0.54f, 0.67f);

    static readonly Color ACCENT_GOLD  = new Color(0.95f, 0.77f, 0.06f);

    static readonly Color ACCENT_GREEN = new Color(0.18f, 0.80f, 0.44f);



    [HideInInspector] public MovieConfig[] allMovies;

    [HideInInspector] public RectTransform gridContent;



    TextMeshProUGUI _summaryText;

    TextMeshProUGUI _statsText;

    TextMeshProUGUI _sagaHeaderText;

    RectTransform   _sagaSection;



    StudioManager _studio;

    StudioLevelSystem _level;

    CitySystem _city;



    void Awake()

    {

        EnsureLayout();

        GameHub.OnGameReady += Bind;

    }



    void OnEnable()

    {

        EnsureLayout();

        if (GameHub.Instance != null)

            Bind();

    }



    void EnsureLayout()

    {

        if (gridContent != null) return;

        BuildLayout();

        Debug.Log("[Collection] Layout built");

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

            var tab = Object.FindAnyObjectByType<MovieTabUI>(FindObjectsInactive.Include);

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

        EnsureLayout();

        if (gridContent == null)

        {

            Debug.LogWarning("[Collection] Refresh aborted — gridContent is null after EnsureLayout.");

            return;

        }



        if (_studio == null && GameHub.Instance != null)

            Bind();

        if (_studio == null)

        {

            Debug.LogWarning("[Collection] Refresh deferred — studio not ready.");

            return;

        }



        var completed = _studio.CompletedMovieKeys;

        var snap = MovieCollectionService.BuildSnapshot(allMovies, completed, _studio.MovieHistory);



        if (_summaryText != null)

        {

            _summaryText.text =

                "Descubiertas:\n" +

                $"{snap.discoveredTotal} / {snap.targetCatalogTotal}\n\n" +

                "Porcentaje:\n" +

                $"{snap.targetCompletionPercent:0}%";

        }



        if (_statsText != null)

        {

            var genreLines = new List<string> { "Por género:" };

            foreach (var g in snap.genres)

            {

                if (g.total <= 0) continue;

                genreLines.Add($"{MovieCollectionService.GenreLabel(g.genre)}\n{g.discovered} / {g.total}");

            }



            genreLines.Add("");

            genreLines.Add("ESTADÍSTICAS");

            genreLines.Add($"Sagas completadas\n{snap.sagasCompleted} / {snap.sagasTracked}");

            genreLines.Add($"Género más completado\n{snap.topGenreLabel} ({snap.topGenreDiscovered}/{snap.topGenreTotal})");

            genreLines.Add($"Última descubierta\n{snap.lastDiscoveredTitle}");



            _statsText.text = string.Join("\n", genreLines);

        }



        RebuildSagaSection(snap.sagas);

        RebuildAlbum(completed);



        Debug.Log("[Collection] Refresh");

        Debug.Log($"[Collection] Movies detected: {snap.catalogTotal}");

        Debug.Log($"[Collection] Discovered: {snap.discoveredTotal}");

        Debug.Log($"[Collection] Cards generated: {gridContent.childCount}");

    }



    void RebuildSagaSection(MovieCollectionService.SagaProgress[] sagas)

    {

        if (_sagaSection == null) return;



        ClearChildren(_sagaSection);



        if (sagas == null || sagas.Length == 0)

        {

            if (_sagaHeaderText != null) _sagaHeaderText.text = "SAGAS";

            AddBodyText(_sagaSection, "Sin sagas definidas todavía.", TEXT_DIM);

            return;

        }



        if (_sagaHeaderText != null) _sagaHeaderText.text = "SAGAS";

        foreach (var saga in sagas)

        {

            string suffix = saga.discovered >= saga.total && saga.total > 0 ? " COMPLETADA" : "";

            string line = $"{saga.displayName}\n{saga.discovered} / {saga.total}{suffix}";

            AddBodyText(_sagaSection, line, saga.discovered >= saga.total ? ACCENT_GREEN : TEXT_PRI);

        }

    }



    void RebuildAlbum(IReadOnlyCollection<string> completedKeys)

    {

        ClearChildren(gridContent);



        var sorted = MovieCollectionService.SortForDisplay(allMovies, completedKeys, _studio, _level, _city);

        foreach (var cfg in sorted)

            BuildAlbumCard(cfg, MovieCollectionService.IsDiscovered(cfg, completedKeys));

    }



    void BuildAlbumCard(MovieConfig cfg, bool discovered)

    {

        var card = new GameObject("Album_" + cfg.name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));

        card.transform.SetParent(gridContent, false);

        card.GetComponent<Image>().color = BG_CARD;

        var cardLE = card.GetComponent<LayoutElement>();

        cardLE.preferredWidth = CellWidth;

        cardLE.preferredHeight = CellHeight;

        cardLE.minWidth = CellWidth;

        cardLE.minHeight = CellHeight;



        var vlg = card.AddComponent<VerticalLayoutGroup>();

        vlg.padding = new RectOffset(8, 8, 8, 8);

        vlg.spacing = 4;

        vlg.childControlWidth = vlg.childControlHeight = true;

        vlg.childForceExpandWidth = true;

        vlg.childForceExpandHeight = false;

        vlg.childAlignment = TextAnchor.UpperCenter;



        var posterGo = new GameObject("Poster", typeof(RectTransform), typeof(Image));

        posterGo.transform.SetParent(card.transform, false);

        var posterImg = posterGo.GetComponent<Image>();

        var posterLE = posterGo.AddComponent<LayoutElement>();

        posterLE.preferredHeight = 118f;

        posterLE.minHeight = 118f;



        if (discovered)

        {

            if (cfg.posterSprite != null)

            {

                posterImg.sprite = cfg.posterSprite;

                posterImg.color = Color.white;

                posterImg.preserveAspect = true;

            }

            else

            {

                ColorUtility.TryParseHtmlString(cfg.posterColorHex, out Color c);

                posterImg.color = c;

            }

        }

        else

        {

            posterImg.color = BG_POSTER;

        }



        if (discovered)

        {

            int cityReq = CityProgressionRules.GetMovieRequiredCity(cfg);

            AddCardLine(card.transform, cfg.movieName, 15, TEXT_PRI, FontStyles.Bold, 22f);

            AddCardLine(card.transform, MovieCollectionService.GenreLabel(cfg.genre), 13, TEXT_DIM, FontStyles.Normal, 18f);

            AddCardLine(card.transform,

                $"Ciudad {cityReq}",

                12, ACCENT_GOLD, FontStyles.Normal, 16f);



            string sagaLabel = SagaProgressionRules.GetCardSagaProgressLabel(cfg, _studio.CompletedMovieKeys, allMovies);

            if (!string.IsNullOrEmpty(sagaLabel))

            {

                bool complete = sagaLabel.Contains("COMPLETADA");

                AddCardLine(card.transform, "Saga " + sagaLabel, 12,

                    complete ? ACCENT_GREEN : TEXT_DIM, FontStyles.Bold, 18f);

            }

        }

        else

        {

            AddCardLine(card.transform, "??????", 16, TEXT_DIM, FontStyles.Bold, 22f);

            AddCardLine(card.transform, "Desconocida", 13, TEXT_DIM, FontStyles.Normal, 18f);

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

        scroll.movementType = ScrollRect.MovementType.Clamped;



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

        _summaryText.fontSize = 18;

        _summaryText.alignment = TextAlignmentOptions.Center;



        MakeSection(content.transform, "StatsCard", out _statsText, "DETALLE");

        _statsText.fontSize = 14;



        var sagaCard = MakeSection(content.transform, "SagaCard", out _sagaHeaderText, "SAGAS");

        _sagaSection = sagaCard;



        var albumHdr = RuntimeTmpText.Create(content.transform, "ÁLBUM", 14, TEXT_DIM, FontStyles.Bold);

        albumHdr.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;



        var gridGo = new GameObject("AlbumGrid", typeof(RectTransform));

        gridGo.transform.SetParent(content.transform, false);

        gridContent = gridGo.GetComponent<RectTransform>();

        var gridLE = gridGo.AddComponent<LayoutElement>();

        gridLE.flexibleWidth = 1f;



        var grid = gridGo.AddComponent<GridLayoutGroup>();

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;

        grid.constraintCount = AlbumColumns;

        grid.cellSize = new Vector2(CellWidth, CellHeight);

        grid.spacing = new Vector2(GridSpacing, GridSpacing);

        grid.childAlignment = TextAnchor.UpperCenter;

        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;

        grid.startAxis = GridLayoutGroup.Axis.Horizontal;



        var gridFitter = gridGo.AddComponent<ContentSizeFitter>();

        gridFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        gridFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

    }



    static void ClearChildren(Transform parent)

    {

        for (int i = parent.childCount - 1; i >= 0; i--)

            DestroyImmediate(parent.GetChild(i).gameObject);

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



    static void AddCardLine(Transform parent, string text, float size, Color color, FontStyles style, float height)

    {

        var tmp = RuntimeTmpText.Create(parent, text, size, color, style, TextAlignmentOptions.Center);

        tmp.textWrappingMode = TextWrappingModes.Normal;

        var le = tmp.gameObject.AddComponent<LayoutElement>();

        le.preferredHeight = height;

        le.minHeight = height;

    }



    static void Stretch(RectTransform rt)

    {

        rt.anchorMin = Vector2.zero;

        rt.anchorMax = Vector2.one;

        rt.offsetMin = rt.offsetMax = Vector2.zero;

    }

}


