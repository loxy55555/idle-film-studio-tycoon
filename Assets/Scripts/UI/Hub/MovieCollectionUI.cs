using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Collection sub-tab — filmoteca layout Genre → Rarity → Movies (Phase 8.6).</summary>
public class MovieCollectionUI : MonoBehaviour
{
    static readonly Color TextPrimary   = Color.white;
    static readonly Color TextSecondary = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color AccentGreen   = new Color(0.18f, 0.80f, 0.44f);
    static readonly Color BarBg         = new Color(0.09f, 0.09f, 0.18f);
    static readonly Color BarFill       = new Color(0.15f, 0.68f, 0.38f);

    [HideInInspector] public MovieConfig[] allMovies;
    [HideInInspector] public RectTransform gridContent;

    TextMeshProUGUI _progressText;
    TextMeshProUGUI _percentText;
    Slider          _globalBar;
    RectTransform   _genreSectionsRoot;
    ScrollRect      _scroll;

    StudioManager _studio;

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

    void OnDestroy() => GameHub.OnGameReady -= Bind;

    void Bind()
    {
        Unbind();
        var hub = GameHub.Instance;
        if (hub == null) return;

        _studio = hub.studio;
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
        if (_genreSectionsRoot == null) return;

        if (_studio == null && GameHub.Instance != null)
            Bind();
        if (_studio == null) return;

        var completed = _studio.CompletedMovieKeys;
        var catalog = CollectionCatalogModel.Build(allMovies, completed);

        if (_progressText != null)
        {
            _progressText.text = CollectionLoc.FormatGlobalProgress(catalog.discoveredTotal, catalog.targetTotal);
            if (CollectionDebugState.RevealAll)
                _progressText.text += " · DEV REVEAL";
        }
        if (_percentText != null)
            _percentText.text = CollectionLoc.FormatPercent(catalog.targetPercent);
        if (_globalBar != null)
            _globalBar.value = Mathf.Clamp01(catalog.targetPercent / 100f);

        RebuildGenreSections(catalog);
    }

    void RebuildGenreSections(CollectionCatalogModel.CatalogView catalog)
    {
        ClearChildren(_genreSectionsRoot);

        if (catalog.genres == null) return;
        foreach (var group in catalog.genres)
            CollectionGenreSectionView.Create(_genreSectionsRoot, group);
    }

    void EnsureLayout()
    {
        if (_genreSectionsRoot != null) return;
        BuildLayout();
    }

    void BuildLayout()
    {
        var root = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        Stretch(root);

        var scrollGo = new GameObject("CollectionScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollGo.transform.SetParent(transform, false);
        Stretch(scrollGo.GetComponent<RectTransform>());
        HudSkinProvider.ApplyPanel(scrollGo.GetComponent<Image>(), HudPanelVariant.Deep);

        _scroll = scrollGo.GetComponent<ScrollRect>();
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.movementType = ScrollRect.MovementType.Elastic;
        _scroll.scrollSensitivity = 28f;
        _scroll.inertia = true;
        _scroll.decelerationRate = 0.135f;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scrollGo.transform, false);
        Stretch(viewport.GetComponent<RectTransform>());
        _scroll.viewport = viewport.GetComponent<RectTransform>();

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.offsetMin = contentRT.offsetMax = Vector2.zero;
        _scroll.content = contentRT;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 12, 12);
        vlg.spacing = 12;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        BuildProgressHeader(content.transform);

        _genreSectionsRoot = new GameObject("GenreSections", typeof(RectTransform)).GetComponent<RectTransform>();
        _genreSectionsRoot.SetParent(content.transform, false);
        var sectionsVLG = _genreSectionsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        sectionsVLG.spacing = 10;
        sectionsVLG.childControlWidth = sectionsVLG.childControlHeight = true;
        sectionsVLG.childForceExpandWidth = true;
        sectionsVLG.childForceExpandHeight = false;
        _genreSectionsRoot.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        gridContent = _genreSectionsRoot;
    }

    void BuildProgressHeader(Transform parent)
    {
        var card = new GameObject("ProgressHeader", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(parent, false);
        HudSkinProvider.ApplyCard(card.GetComponent<Image>(), HudCardVariant.Primary);
        card.AddComponent<LayoutElement>().preferredHeight = 0f;

        var vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 12, 12);
        vlg.spacing = 6;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;

        var title = RuntimeTmpText.Create(card.transform, CollectionLoc.Title(), 14f, TextSecondary,
            FontStyles.Bold, TextAlignmentOptions.Center);
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        _progressText = RuntimeTmpText.Create(card.transform, "—", 28f, TextPrimary,
            FontStyles.Bold, TextAlignmentOptions.Center);
        _progressText.enableAutoSizing = true;
        _progressText.fontSizeMin = 22f;
        _progressText.fontSizeMax = 28f;
        _progressText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        _percentText = RuntimeTmpText.Create(card.transform, "—", 14f, AccentGreen,
            FontStyles.Normal, TextAlignmentOptions.Center);
        _percentText.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

        var barGo = new GameObject("GlobalBar", typeof(RectTransform), typeof(Image), typeof(Slider));
        barGo.transform.SetParent(card.transform, false);
        barGo.GetComponent<Image>().color = BarBg;
        barGo.AddComponent<LayoutElement>().preferredHeight = 8f;
        var slider = barGo.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.interactable = false;
        SetupBarFill(slider, BarFill);
        ReadOnlySlider.Configure(slider);
        _globalBar = slider;
    }

    static void SetupBarFill(Slider slider, Color fillColor)
    {
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(slider.transform, false);
        Stretch(fillArea.GetComponent<RectTransform>());
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        fill.GetComponent<Image>().color = fillColor;
        Stretch(fill.GetComponent<RectTransform>());
        slider.fillRect = fill.GetComponent<RectTransform>();
    }

    static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            DestroyImmediate(parent.GetChild(i).gameObject);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
