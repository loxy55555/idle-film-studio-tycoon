using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Single movie card in the collection filmoteca (Phase 8.6 / 9.0 legendaries).</summary>
public static class CollectionMovieCardView
{
    const float CellWidth  = 156f;
    const float CellHeight = 248f;
    const float PosterHeight = 112f;

    static readonly Color BgPoster      = new Color(0.08f, 0.08f, 0.14f);
    static readonly Color TextPrimary   = Color.white;
    static readonly Color TextSecondary = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color LockRed       = new Color(0.95f, 0.45f, 0.35f);
    static readonly Color DiscGreen     = new Color(0.18f, 0.80f, 0.44f);

    public static RectTransform Build(Transform parent, CollectionCatalogModel.MovieEntry entry)
    {
        var cfg = entry.config;
        bool isLegendary = cfg.rarity == MovieRarity.Legendary;

        var card = new GameObject("Movie_" + cfg.name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        card.transform.SetParent(parent, false);
        HudSkinProvider.ApplyCard(card.GetComponent<Image>(), HudCardVariant.Primary);

        var le = card.GetComponent<LayoutElement>();
        le.preferredWidth = le.minWidth = CellWidth;
        le.preferredHeight = le.minHeight = isLegendary ? CellHeight : CellHeight - 20f;

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
        }
        else if (entry.discovered)
        {
            MoviePosterVisual.Apply(poster, cfg);
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
            var title = CreateLabel(card.transform, cfg.movieName, 13f, TextPrimary, FontStyles.Bold, 32f);
            title.enableAutoSizing = true;
            title.fontSizeMin = 10f;
            title.fontSizeMax = 13f;
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

        var title = CreateLabel(card, cfg.movieName, 13f, TextPrimary, FontStyles.Bold, 36f);
        title.enableAutoSizing = true;
        title.fontSizeMin = 10f;
        title.fontSizeMax = 13f;
        title.textWrappingMode = TextWrappingModes.Normal;
        title.overflowMode = TextOverflowModes.Ellipsis;

        if (entry.discovered)
        {
            CreateLabel(card, CollectionLoc.DiscoveredLabel(), 11f, DiscGreen, FontStyles.Bold, 16f);
            CreateLabel(card, CollectionLoc.GetGenreLabel(cfg.genre), 10f, TextSecondary, FontStyles.Normal, 14f);
        }
    }

    public static void ConfigureGrid(GridLayoutGroup grid)
    {
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.cellSize = new Vector2(CellWidth, CellHeight);
        grid.spacing = new Vector2(8f, 8f);
        grid.childAlignment = TextAnchor.UpperCenter;
    }

    static TextMeshProUGUI CreateLabel(Transform parent, string text, float size, Color color, FontStyles style, float height)
    {
        var tmp = RuntimeTmpText.Create(parent, text, size, color, style, TextAlignmentOptions.Center);
        tmp.textWrappingMode = TextWrappingModes.Normal;
        LE(tmp.rectTransform, height);
        return tmp;
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
