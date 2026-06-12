using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Expandable genre section — Genre → Rarity → movie grid (Phase 8.6).</summary>
public class CollectionGenreSectionView : MonoBehaviour
{
    static readonly Color TextPrimary   = Color.white;
    static readonly Color TextSecondary = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color AccentGreen   = new Color(0.18f, 0.80f, 0.44f);
    static readonly Color BarBg         = new Color(0.09f, 0.09f, 0.18f);
    static readonly Color BarFill       = new Color(0.15f, 0.68f, 0.38f);

    RectTransform _body;
    TextMeshProUGUI _chevron;
    bool _expanded = true;

    public static CollectionGenreSectionView Create(Transform parent, CollectionCatalogModel.GenreGroup group)
    {
        var rootGo = new GameObject("Genre_" + group.genre, typeof(RectTransform), typeof(Image), typeof(CollectionGenreSectionView));
        rootGo.transform.SetParent(parent, false);
        HudSkinProvider.ApplyCard(rootGo.GetComponent<Image>(), HudCardVariant.Primary);
        rootGo.AddComponent<LayoutElement>().preferredHeight = 0f;

        var view = rootGo.GetComponent<CollectionGenreSectionView>();
        view.Build(group);
        return view;
    }

    void Build(CollectionCatalogModel.GenreGroup group)
    {
        var vlg = gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 8, 8);
        vlg.spacing = 8;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var headerBtn = new GameObject("Header", typeof(RectTransform), typeof(Button));
        headerBtn.transform.SetParent(transform, false);
        LE(headerBtn.GetComponent<RectTransform>(), 52f);
        headerBtn.GetComponent<Button>().onClick.AddListener(ToggleExpanded);
        var hlg = headerBtn.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(4, 4, 0, 0);
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;

        _chevron = RuntimeTmpText.Create(headerBtn.transform, "▼", 14f, AccentGreen, FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft, "Chevron");
        _chevron.gameObject.AddComponent<LayoutElement>().preferredWidth = 18f;

        var titleCol = new GameObject("TitleCol", typeof(RectTransform));
        titleCol.transform.SetParent(headerBtn.transform, false);
        titleCol.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var titleVLG = titleCol.AddComponent<VerticalLayoutGroup>();
        titleVLG.spacing = 2;
        titleVLG.childControlWidth = titleVLG.childControlHeight = true;
        titleVLG.childForceExpandWidth = true;
        titleVLG.childForceExpandHeight = false;

        var genreTitle = RuntimeTmpText.Create(titleCol.transform,
            CollectionLoc.GetGenreLabel(group.genre), 15f, TextPrimary, FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft);
        genreTitle.enableAutoSizing = true;
        genreTitle.fontSizeMin = 12f;
        genreTitle.fontSizeMax = 15f;

        var progress = RuntimeTmpText.Create(titleCol.transform,
            CollectionLoc.FormatGenreProgress(group.genre, group.discovered, group.total),
            11f, TextSecondary, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        progress.enableAutoSizing = true;
        progress.fontSizeMin = 9f;
        progress.fontSizeMax = 11f;

        var barGo = new GameObject("ProgressBar", typeof(RectTransform), typeof(Image), typeof(Slider));
        barGo.transform.SetParent(transform, false);
        LE(barGo.GetComponent<RectTransform>(), 6f);
        barGo.GetComponent<Image>().color = BarBg;
        var slider = barGo.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.interactable = false;
        float fill = group.total > 0 ? (float)group.discovered / group.total : 0f;
        SetupBarFill(slider, BarFill);
        slider.value = fill;
        ReadOnlySlider.Configure(slider);

        _body = new GameObject("Body", typeof(RectTransform)).GetComponent<RectTransform>();
        _body.SetParent(transform, false);
        var bodyVLG = _body.gameObject.AddComponent<VerticalLayoutGroup>();
        bodyVLG.spacing = 10;
        bodyVLG.childControlWidth = bodyVLG.childControlHeight = true;
        bodyVLG.childForceExpandWidth = true;
        bodyVLG.childForceExpandHeight = false;

        foreach (var rarityGroup in group.rarities)
            BuildRaritySection(_body, rarityGroup);

        _expanded = group.discovered > 0 || HasLegendarySection(group) || CollectionDebugState.RevealAll;
        ApplyExpandedState();
    }

    static bool HasLegendarySection(CollectionCatalogModel.GenreGroup group)
    {
        if (group.rarities == null) return false;
        foreach (var r in group.rarities)
        {
            if (r.rarity == MovieRarity.Legendary && r.movies != null && r.movies.Length > 0)
                return true;
        }
        return false;
    }

    void BuildRaritySection(Transform parent, CollectionCatalogModel.RarityGroup group)
    {
        if (group.movies == null || group.movies.Length == 0) return;

        var section = new GameObject("Rarity_" + group.rarity, typeof(RectTransform));
        section.transform.SetParent(parent, false);
        var vlg = section.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var hdr = RuntimeTmpText.Create(section.transform,
            CollectionLoc.GetRarityLabel(group.rarity) + (group.rarity == MovieRarity.Legendary ? " ★" : string.Empty),
            12f, group.rarity == MovieRarity.Legendary ? new Color(0.95f, 0.77f, 0.06f) : TextSecondary,
            FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        hdr.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        var gridGo = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        gridGo.transform.SetParent(section.transform, false);
        var grid = gridGo.GetComponent<GridLayoutGroup>();
        CollectionMovieCardView.ConfigureGrid(grid);
        gridGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        foreach (var entry in group.movies)
            CollectionMovieCardView.Build(gridGo.transform, entry);
    }

    void ToggleExpanded()
    {
        _expanded = !_expanded;
        ApplyExpandedState();
    }

    void ApplyExpandedState()
    {
        if (_body != null) _body.gameObject.SetActive(_expanded);
        if (_chevron != null) _chevron.text = _expanded ? "▼" : "▶";
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

    static void LE(RectTransform rt, float height)
    {
        var le = rt.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = height;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
