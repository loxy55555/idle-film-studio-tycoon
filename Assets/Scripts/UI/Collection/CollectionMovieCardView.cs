using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Single movie card in the collection filmoteca (Phase 8.6 / 9.0 legendaries).</summary>
public static class CollectionMovieCardView
{
    // Ivory colour shared by cinematic title overlays
    static readonly Color TitleIvory = new Color(0.96f, 0.94f, 0.87f);
    const float CellWidth   = 108f;
    const float CellHeight  = 190f;
    const float PosterHeight = 90f;

    static readonly Color BgPoster      = CinematicTheme.PanelBg;
    static readonly Color TextPrimary   = CinematicTheme.TextPrimary;
    static readonly Color TextSecondary = CinematicTheme.TextSecondary;
    static readonly Color LockRed       = new Color(0.95f, 0.45f, 0.35f);
    static readonly Color DiscGold      = CinematicTheme.GoldBase;

    public static RectTransform Build(Transform parent, CollectionCatalogModel.MovieEntry entry)
    {
        var cfg = entry.config;
        bool isLegendary = cfg.rarity == MovieRarity.Legendary;

        var card = new GameObject("Movie_" + cfg.name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        card.transform.SetParent(parent, false);
        HudSkinProvider.ApplyCard(card.GetComponent<Image>(), HudCardVariant.Primary);
        CinematicTheme.ApplyPremiumMaterial(card.GetComponent<RectTransform>());

        var le = card.GetComponent<LayoutElement>();
        le.preferredWidth = le.minWidth = 0f;
        le.flexibleWidth = 1f;
        le.preferredHeight = le.minHeight = CellHeight; // uniform height for all rarities (3-col grid)

        var vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.spacing = 4;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var posterWrap = CreatePanel(card.transform, "PosterWrap", BgPoster);
        LE(posterWrap, PosterHeight);

        var rarityFrame = CreatePanel(posterWrap, "RarityFrame", Color.clear).GetComponent<Image>();
        Stretch(rarityFrame.rectTransform);
        if (isLegendary)
            CollectionLegendaryVisual.ApplyLegendaryFrame(rarityFrame, entry.legendaryLocked);
        else
            MovieRarityVisual.ApplyFrame(rarityFrame, cfg.rarity);

        var poster = CreatePanel(posterWrap, "Poster", Color.clear).GetComponent<Image>();
        InsetPoster(poster.rectTransform, isLegendary ? 0.05f : 0.06f);

        if (isLegendary)
        {
            CollectionLegendaryVisual.ApplyLegendaryPoster(poster, cfg, entry.legendaryLocked, entry.discovered);
            if (entry.legendaryLocked)
            {
                var lockOverlay = CreatePanel(posterWrap, "LockOverlay", new Color(0f, 0f, 0f, 0.35f));
                Stretch(lockOverlay);
            }
            else
            {
                AddCinematicTitleOverlay(posterWrap, cfg.movieName);
            }
        }
        else if (entry.discovered)
        {
            MoviePosterVisual.Apply(poster, cfg);
            AddCinematicTitleOverlay(posterWrap, cfg.movieName);
        }
        else
        {
            CollectionLegendaryVisual.ApplySilhouette(poster, cfg);
        }

        var rarityLbl = CreateLabel(card.transform, CollectionLoc.GetRarityLabel(cfg.rarity), 10f, TextSecondary, FontStyles.Bold, 14f);
        MovieRarityVisual.ApplyBadge(rarityLbl, cfg.rarity);

        if (isLegendary)
            BuildLegendaryLabels(card.transform, cfg, entry);
        else if (entry.discovered)
        {
            var title = CreateLabel(card.transform, cfg.movieName, 16f, TextPrimary, FontStyles.Bold, 32f);
            title.enableAutoSizing = true;
            title.fontSizeMin = 14f;
            title.fontSizeMax = 18f;
            title.textWrappingMode = TextWrappingModes.Normal;
            title.overflowMode = TextOverflowModes.Ellipsis;
            CreateLabel(card.transform, CollectionLoc.GetGenreLabel(cfg.genre), 10f, TextSecondary, FontStyles.Normal, 14f);
        }
        else
        {
            CreateLabel(card.transform, CollectionLoc.UnknownTitle(), 14f, TextSecondary, FontStyles.Bold, 20f);
        }

        return card.GetComponent<RectTransform>();
    }

    static void BuildLegendaryLabels(Transform card, MovieConfig cfg, CollectionCatalogModel.MovieEntry entry)
    {
        if (entry.legendaryLocked)
        {
            CreateLabel(card, CollectionLoc.UnknownTitle(), 14f, TextSecondary, FontStyles.Bold, 20f);
            CreateLabel(card, CollectionLoc.LockedLabel(), 11f, LockRed, FontStyles.Bold, 16f);
            var cond = CreateLabel(card, CollectionLoc.LegendaryLockCondition(), 9f, TextSecondary, FontStyles.Normal, 40f);
            cond.enableAutoSizing = true;
            cond.fontSizeMin = 8f;
            cond.fontSizeMax = 10f;
            cond.textWrappingMode = TextWrappingModes.Normal;
            return;
        }

        var title = CreateLabel(card, cfg.movieName, 16f, TextPrimary, FontStyles.Bold, 36f);
        title.enableAutoSizing = true;
        title.fontSizeMin = 14f;
        title.fontSizeMax = 18f;
        title.textWrappingMode = TextWrappingModes.Normal;
        title.overflowMode = TextOverflowModes.Ellipsis;

        if (entry.discovered)
        {
            CreateLabel(card, CollectionLoc.DiscoveredLabel(), 11f, DiscGold, FontStyles.Bold, 16f);
            CreateLabel(card, CollectionLoc.GetGenreLabel(cfg.genre), 10f, TextSecondary, FontStyles.Normal, 14f);
        }
    }

    public static void ConfigureGrid(GridLayoutGroup grid)
    {
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3; // RULE: always 3 columns — no cell changes size regardless of rarity
        grid.cellSize = new Vector2(CellWidth, CellHeight);
        grid.spacing = new Vector2(6f, 6f);
        grid.childAlignment = TextAnchor.UpperLeft;
    }

    static TextMeshProUGUI CreateLabel(Transform parent, string text, float size, Color color, FontStyles style, float height)
    {
        var tmp = RuntimeTmpText.Create(parent, text, size, color, style, TextAlignmentOptions.Center);
        tmp.textWrappingMode = TextWrappingModes.Normal;
        LE(tmp.rectTransform, height);
        return tmp;
    }

    /// <summary>
    /// Adds a cinematic bottom-gradient + uppercase title overlay inside <paramref name="posterWrap"/>.
    /// Uses absolute anchoring so it does not interfere with the parent LayoutGroup.
    /// </summary>
    static void AddCinematicTitleOverlay(RectTransform posterWrap, string movieName)
    {
        // Opaque dark band at the bottom 40 % of the poster
        var gradGo = new GameObject("TitleGradient", typeof(RectTransform), typeof(Image));
        gradGo.transform.SetParent(posterWrap, false);
        var gradImg = gradGo.GetComponent<Image>();
        gradImg.color = new Color(0f, 0f, 0f, 0.88f);
        gradImg.raycastTarget = false;
        var gradRT = gradGo.GetComponent<RectTransform>();
        gradRT.anchorMin = Vector2.zero;
        gradRT.anchorMax = new Vector2(1f, 0.40f);
        gradRT.offsetMin = gradRT.offsetMax = Vector2.zero;

        // Uppercase bold title — large and prominent, anchored over the dark band
        var titleGo = new GameObject("TitleTMP", typeof(RectTransform));
        titleGo.transform.SetParent(posterWrap, false);
        var titleTMP = titleGo.AddComponent<TextMeshProUGUI>();
        var titleRT = titleGo.GetComponent<RectTransform>();
        titleRT.anchorMin = Vector2.zero;
        titleRT.anchorMax = new Vector2(1f, 0.40f);
        titleRT.offsetMin = new Vector2(4f, 4f);
        titleRT.offsetMax = new Vector2(-4f, 0f);
        // C3 (FASE 16.1): NoWrap prevents TMP from breaking words mid-character on narrow cards
        titleTMP.text = movieName.ToUpper();
        titleTMP.fontSize = 16f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.BottomLeft;
        titleTMP.enableAutoSizing = true;
        titleTMP.fontSizeMin = 14f;
        titleTMP.fontSizeMax = 18f;
        titleTMP.overflowMode = TextOverflowModes.Ellipsis;
        titleTMP.color = TitleIvory;
        titleTMP.raycastTarget = false;
        titleTMP.textWrappingMode = TextWrappingModes.NoWrap;
        titleTMP.characterSpacing = 0f;
    }

    static RectTransform CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        go.GetComponent<Image>().raycastTarget = false;
        return go.GetComponent<RectTransform>();
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

    static void InsetPoster(RectTransform rt, float inset)
    {
        rt.anchorMin = new Vector2(inset, inset);
        rt.anchorMax = new Vector2(1f - inset, 1f - inset);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
