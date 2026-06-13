using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Premium movie offer card — tycoon-style production opportunity (Phase 9.0).</summary>
public static class MovieOfferCardLayoutBuilder
{
    public const string LayoutMarkerName = "MovieOfferCard_v2";
    public const float RarityIconWidth   = 56f;
    public const float TitleRowHeight    = 52f;
    public const float CardPreferredHeight = HudLayoutConstants.OfferCardBaseHeight;

    static readonly Color TextPrimary    = Color.white;
    static readonly Color TextSecondary  = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color BarBg          = new Color(0.09f, 0.09f, 0.18f);
    static readonly Color AccentGreen    = new Color(0.18f, 0.80f, 0.44f);
    static readonly Color AccentGold     = new Color(0.95f, 0.77f, 0.06f);
    static readonly Color PanelDark      = new Color(0.06f, 0.06f, 0.12f, 0.92f);

    public struct WireResult
    {
        public Image posterImage;
        public Image rarityFrame;
        public Image genreStrip;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI genreText;
        public TextMeshProUGUI rarityText;
        public TextMeshProUGUI durationText;
        public TextMeshProUGUI rewardText;
        public TextMeshProUGUI repText;
        public TextMeshProUGUI badgesText;
        public TextMeshProUGUI unlockText;
        public Button selectButton;
        public TextMeshProUGUI selectLabelText;
        public GameObject lockedOverlay;
    }

    public static bool HasPremiumLayout(Transform root) => root.Find(LayoutMarkerName) != null;

    public static WireResult Build(Transform cardRoot, MovieConfig cfg, bool isEmpty)
    {
        var result = new WireResult();
        var card   = cardRoot as RectTransform;
        if (card == null) return result;

        HudSkinProvider.ApplyCard(card.GetComponent<Image>(), isEmpty ? HudCardVariant.Hero : HudCardVariant.Primary);

        var marker = new GameObject(LayoutMarkerName, typeof(RectTransform));
        marker.transform.SetParent(card, false);
        Stretch(marker.GetComponent<RectTransform>());

        var vlg = card.gameObject.GetComponent<VerticalLayoutGroup>() ?? card.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding               = HudLayoutConstants.SectionPadding;
        vlg.spacing               = 0;
        vlg.childAlignment        = TextAnchor.UpperCenter;
        vlg.childControlWidth     = vlg.childControlHeight    = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var cardLE = card.GetComponent<LayoutElement>() ?? card.gameObject.AddComponent<LayoutElement>();
        cardLE.preferredHeight = CardPreferredHeight;
        cardLE.minHeight = 240f;

        if (isEmpty)
        {
            var emptyGo = new GameObject("EmptyContent", typeof(RectTransform));
            emptyGo.transform.SetParent(card, false);
            emptyGo.AddComponent<LayoutElement>().flexibleHeight = 1f;
            var emptyInner = emptyGo.AddComponent<VerticalLayoutGroup>();
            emptyInner.childAlignment = TextAnchor.MiddleCenter;
            emptyInner.childControlWidth = emptyInner.childControlHeight = true;
            emptyInner.childForceExpandWidth = emptyInner.childForceExpandHeight = true;

            var empty = CreateLabel(emptyGo.transform, "EmptyLabel", Loc.Get(LocKeys.ProdEmptySlot), 14f, TextSecondary, 40f);
            empty.enableAutoSizing = true;
            empty.fontSizeMin = 12f;
            empty.fontSizeMax = 16f;
            return result;
        }

        var genreStripGo = new GameObject("GenreStrip", typeof(RectTransform), typeof(Image));
        genreStripGo.transform.SetParent(card, false);
        result.genreStrip = genreStripGo.GetComponent<Image>();
        result.genreStrip.color = cfg != null ? GenreAccentColor(cfg.genre) : BarBg;
        result.genreStrip.raycastTarget = false;
        LE(genreStripGo.GetComponent<RectTransform>(), 5f);

        var bodyRow = new GameObject("BodyRow", typeof(RectTransform));
        bodyRow.transform.SetParent(card, false);
        var bodyLE = bodyRow.AddComponent<LayoutElement>();
        bodyLE.preferredHeight = CardPreferredHeight - 70f;
        bodyLE.minHeight = 210f;
        bodyLE.flexibleHeight = 1f;

        var bodyHLG = bodyRow.AddComponent<HorizontalLayoutGroup>();
        bodyHLG.padding = new RectOffset(10, 14, 10, 10);
        bodyHLG.spacing = 10;
        bodyHLG.childAlignment = TextAnchor.UpperLeft;
        bodyHLG.childControlWidth = bodyHLG.childControlHeight = true;
        bodyHLG.childForceExpandWidth = true;
        bodyHLG.childForceExpandHeight = true;

        var infoCol = new GameObject("InfoCol", typeof(RectTransform));
        infoCol.transform.SetParent(bodyRow.transform, false);
        infoCol.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var infoVLG = infoCol.AddComponent<VerticalLayoutGroup>();
        infoVLG.padding = new RectOffset(0, 0, 0, 0);
        infoVLG.spacing = 6;
        infoVLG.childAlignment = TextAnchor.UpperLeft;
        infoVLG.childControlWidth = infoVLG.childControlHeight = true;
        infoVLG.childForceExpandWidth = true;
        infoVLG.childForceExpandHeight = false;

        var titleWrap = new GameObject("TitleWrap", typeof(RectTransform), typeof(Image));
        titleWrap.transform.SetParent(infoCol.transform, false);
        titleWrap.GetComponent<Image>().color = PanelDark;
        titleWrap.GetComponent<Image>().raycastTarget = false;
        LE(titleWrap.GetComponent<RectTransform>(), TitleRowHeight);

        var titleVLG = titleWrap.AddComponent<VerticalLayoutGroup>();
        titleVLG.padding = new RectOffset(10, 10, 6, 4);
        titleVLG.spacing = 2;
        titleVLG.childControlWidth = titleVLG.childControlHeight = true;
        titleVLG.childForceExpandWidth = true;
        titleVLG.childForceExpandHeight = false;

        result.titleText = RuntimeTmpText.Create(titleWrap.transform, cfg?.movieName ?? string.Empty,
            18f, TextPrimary, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "Title");
        result.titleText.enableAutoSizing = true;
        result.titleText.fontSizeMin = 14f;
        result.titleText.fontSizeMax = 20f;
        result.titleText.textWrappingMode = TextWrappingModes.Normal;
        result.titleText.overflowMode = TextOverflowModes.Ellipsis;
        result.titleText.maxVisibleLines = 2;
        result.titleText.raycastTarget = false;
        result.titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        result.genreText = RuntimeTmpText.Create(titleWrap.transform, cfg != null ? GenreLabel(cfg.genre) : string.Empty,
            11f, cfg != null ? GenreAccentColor(cfg.genre) : TextSecondary, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "Genre");
        result.genreText.raycastTarget = false;
        result.genreText.gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;

        var infoRow = CreatePanel(infoCol.transform, "InfoRow", Color.clear);
        LE(infoRow, 24f);
        var infoHLG = infoRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        infoHLG.padding = new RectOffset(0, 0, 0, 0);
        infoHLG.spacing = 8;
        infoHLG.childAlignment = TextAnchor.MiddleLeft;
        infoHLG.childControlWidth = infoHLG.childControlHeight = true;
        infoHLG.childForceExpandWidth = false;
        infoHLG.childForceExpandHeight = true;

        result.durationText = RuntimeTmpText.Create(infoRow.transform, "⏱ —",
            12f, TextSecondary, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "Duration");
        result.durationText.raycastTarget = false;
        result.durationText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        result.badgesText = RuntimeTmpText.Create(infoRow.transform, string.Empty,
            11f, AccentGold, FontStyles.Bold, TextAlignmentOptions.MidlineRight, "Badges");
        result.badgesText.raycastTarget = false;
        result.badgesText.gameObject.AddComponent<LayoutElement>().preferredWidth = 90f;

        var rewardWrap = new GameObject("RewardWrap", typeof(RectTransform), typeof(Image));
        rewardWrap.transform.SetParent(infoCol.transform, false);
        rewardWrap.GetComponent<Image>().color = PanelDark;
        rewardWrap.GetComponent<Image>().raycastTarget = false;
        LE(rewardWrap.GetComponent<RectTransform>(), 40f);

        var rewardHLG = rewardWrap.AddComponent<HorizontalLayoutGroup>();
        rewardHLG.padding = new RectOffset(10, 10, 8, 8);
        rewardHLG.spacing = 8;
        rewardHLG.childAlignment = TextAnchor.MiddleCenter;
        rewardHLG.childControlWidth = rewardHLG.childControlHeight = true;
        rewardHLG.childForceExpandWidth = rewardHLG.childForceExpandHeight = true;

        result.rewardText = RuntimeTmpText.Create(rewardWrap.transform, "💵 —",
            14f, AccentGreen, FontStyles.Bold, TextAlignmentOptions.Center, "Reward");
        result.rewardText.raycastTarget = false;
        result.rewardText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        result.repText = RuntimeTmpText.Create(rewardWrap.transform, "🏆 —",
            14f, AccentGold, FontStyles.Bold, TextAlignmentOptions.Center, "Rep");
        result.repText.raycastTarget = false;
        result.repText.gameObject.AddComponent<LayoutElement>().preferredWidth = 100f;

        result.unlockText = CreateLabel(infoCol.transform, "Unlock", string.Empty, 11f, new Color(0.95f, 0.45f, 0.35f), 18f);
        result.unlockText.gameObject.SetActive(false);

        var btnGo = new GameObject("SelectBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(infoCol.transform, false);
        HudSkinProvider.ApplyButton(btnGo.GetComponent<Image>(), HudButtonVariant.Success);
        btnGo.AddComponent<UIButtonScale>();
        LE(btnGo.GetComponent<RectTransform>(), 58f, minHeight: 58f);

        var btnVLG = btnGo.AddComponent<VerticalLayoutGroup>();
        btnVLG.padding = new RectOffset(12, 12, 0, 0);
        btnVLG.childAlignment = TextAnchor.MiddleCenter;
        btnVLG.childControlWidth = btnVLG.childControlHeight = true;
        btnVLG.childForceExpandWidth = btnVLG.childForceExpandHeight = true;

        result.selectButton = btnGo.GetComponent<Button>();
        result.selectLabelText = RuntimeTmpText.Create(btnGo.transform, Loc.Get(LocKeys.ProdSelect),
            17f, TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center, "Label");
        result.selectLabelText.enableAutoSizing = true;
        result.selectLabelText.fontSizeMin = 14f;
        result.selectLabelText.fontSizeMax = 18f;
        result.selectLabelText.raycastTarget = false;

        var overlay = CreatePanel(card, "LockedOverlay", new Color(0f, 0f, 0f, 0.58f));
        Stretch(overlay);
        overlay.gameObject.SetActive(false);
        result.lockedOverlay = overlay.gameObject;
        result.rarityText = null;

        SetupRarityIconColumn(bodyRow.transform as RectTransform, cfg?.rarity ?? MovieRarity.Common, cfg?.genre ?? MovieGenre.Drama, ref result);

        return result;
    }

    static void SetupRarityIconColumn(RectTransform bodyRow, MovieRarity rarity, MovieGenre genre, ref WireResult result)
    {
        if (bodyRow == null) return;

        var iconWrap = CreatePanel(bodyRow, "RarityIconWrap", PanelDark);
        iconWrap.SetAsFirstSibling();
        var iconLE = iconWrap.GetComponent<LayoutElement>() ?? iconWrap.gameObject.AddComponent<LayoutElement>();
        iconLE.preferredWidth = RarityIconWidth;
        iconLE.minWidth = RarityIconWidth;
        iconLE.flexibleHeight = 1f;

        var accent = RarityFrameColor(rarity);
        result.rarityFrame = iconWrap.GetComponent<Image>();
        result.rarityFrame.color = new Color(accent.r, accent.g, accent.b, 0.85f);
        result.rarityFrame.raycastTarget = false;

        result.rarityText = RuntimeTmpText.Create(iconWrap.transform, GetRarityIcon(rarity, genre),
            30f, GetRarityIconColor(rarity, genre), FontStyles.Normal, TextAlignmentOptions.Center, "RarityIcon");
        result.rarityText.raycastTarget = false;
        Stretch(result.rarityText.rectTransform);

        result.posterImage = null;
    }

    /// <summary>Phase 10.1 — replace legacy poster UI with decorative rarity icon.</summary>
    public static void ApplyRarityIconLayout(Transform cardRoot, MovieRarity rarity, MovieGenre genre = MovieGenre.Drama)
    {
        if (cardRoot == null) return;
        var bodyRow = cardRoot.Find("BodyRow") as RectTransform;
        if (bodyRow == null) return;

        var legacyPoster = bodyRow.Find("PosterWrap");
        if (legacyPoster != null)
        {
            if (Application.isPlaying) Object.Destroy(legacyPoster.gameObject);
            else Object.DestroyImmediate(legacyPoster.gameObject);
        }

        if (bodyRow.Find("RarityIconWrap") == null)
        {
            var wire = new WireResult();
            SetupRarityIconColumn(bodyRow, rarity, genre, ref wire);
        }
        else
        {
            var icon = bodyRow.Find("RarityIconWrap/RarityIcon")?.GetComponent<TextMeshProUGUI>();
            if (icon != null)
            {
                icon.text = GetRarityIcon(rarity, genre);
                icon.color = GetRarityIconColor(rarity, genre);
            }

            var frame = bodyRow.Find("RarityIconWrap")?.GetComponent<Image>();
            if (frame != null)
            {
                var accent = RarityFrameColor(rarity);
                frame.color = new Color(accent.r, accent.g, accent.b, 0.85f);
            }
        }

        var hlg = bodyRow.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null)
        {
            hlg.padding = new RectOffset(10, 14, 10, 10);
            hlg.spacing = 10;
        }
    }

    public static string GetRarityIcon(MovieRarity rarity, MovieGenre genre = MovieGenre.Drama) =>
        ProductionDecorIcon.GetIcon(rarity, genre);

    public static Color GetRarityIconColor(MovieRarity rarity, MovieGenre genre = MovieGenre.Drama) =>
        ProductionDecorIcon.GetIconColor(rarity, genre);

    /// <summary>Phase 9.1: expand card internals when few offers leave free vertical space.</summary>
    public static void ApplyExpandedLayout(RectTransform card, float targetHeight)
    {
        if (card == null) return;

        float scale = Mathf.Clamp(targetHeight / CardPreferredHeight, 1f, HudLayoutConstants.OfferCardMaxExpanded / CardPreferredHeight);

        var bodyRow = card.Find("BodyRow") as RectTransform;
        if (bodyRow != null)
        {
            var le = bodyRow.GetComponent<LayoutElement>() ?? bodyRow.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = Mathf.Max(210f, targetHeight - 64f);
            le.minHeight = 210f;
        }

        var title = card.Find("BodyRow/InfoCol/TitleWrap/Title")?.GetComponent<TextMeshProUGUI>();
        if (title != null)
        {
            title.fontSizeMax = 20f * Mathf.Min(scale, 1.18f);
            title.fontSizeMin = 14f;
        }

        var reward = card.Find("BodyRow/InfoCol/RewardWrap/Reward")?.GetComponent<TextMeshProUGUI>();
        if (reward != null) reward.fontSize = 14f * Mathf.Min(scale, 1.12f);

        var rep = card.Find("BodyRow/InfoCol/RewardWrap/Rep")?.GetComponent<TextMeshProUGUI>();
        if (rep != null) rep.fontSize = 14f * Mathf.Min(scale, 1.12f);

        var selectBtn = card.Find("BodyRow/InfoCol/SelectBtn") as RectTransform;
        if (selectBtn != null)
        {
            var le = selectBtn.GetComponent<LayoutElement>();
            if (le != null) le.preferredHeight = 58f * Mathf.Min(scale, 1.12f);
        }
    }

    static Color GenreAccentColor(MovieGenre g) => g switch
    {
        MovieGenre.Action      => new Color(0.91f, 0.30f, 0.24f),
        MovieGenre.Drama       => new Color(0.20f, 0.60f, 0.86f),
        MovieGenre.Horror      => new Color(0.56f, 0.27f, 0.68f),
        MovieGenre.Comedy      => new Color(0.95f, 0.61f, 0.07f),
        MovieGenre.Romance     => new Color(0.91f, 0.12f, 0.55f),
        MovieGenre.SciFi       => new Color(0.15f, 0.68f, 0.38f),
        MovieGenre.Fantasy     => new Color(0.61f, 0.35f, 0.71f),
        MovieGenre.Thriller    => new Color(0.83f, 0.33f, 0.00f),
        MovieGenre.Animation   => new Color(0.18f, 0.80f, 0.72f),
        MovieGenre.Documentary => new Color(0.50f, 0.55f, 0.60f),
        _                      => new Color(0.40f, 0.40f, 0.50f),
    };

    static string GenreLabel(MovieGenre g) => g switch
    {
        MovieGenre.Action      => "ACCIÓN",
        MovieGenre.Drama       => "DRAMA",
        MovieGenre.Horror      => "TERROR",
        MovieGenre.Comedy      => "COMEDIA",
        MovieGenre.Romance     => "ROMANCE",
        MovieGenre.SciFi       => "SCI-FI",
        MovieGenre.Fantasy     => "FANTASÍA",
        MovieGenre.Thriller    => "THRILLER",
        MovieGenre.Animation   => "ANIMACIÓN",
        MovieGenre.Documentary => "DOCUMENTAL",
        _                      => g.ToString().ToUpper(),
    };

    static Color RarityFrameColor(MovieRarity r) => r switch
    {
        MovieRarity.Legendary => new Color(1.0f, 0.84f, 0.0f, 0.35f),
        MovieRarity.Epic      => new Color(0.63f, 0.13f, 0.94f, 0.28f),
        MovieRarity.Rare      => new Color(0.20f, 0.60f, 0.86f, 0.28f),
        _                     => new Color(0.20f, 0.22f, 0.30f, 0.20f),
    };

    static Color RarityPillColor(MovieRarity r) => r switch
    {
        MovieRarity.Legendary => new Color(0.80f, 0.60f, 0.00f),
        MovieRarity.Epic      => new Color(0.50f, 0.10f, 0.75f),
        MovieRarity.Rare      => new Color(0.10f, 0.40f, 0.70f),
        _                     => new Color(0.20f, 0.22f, 0.30f),
    };

    static string RarityLabel(MovieRarity r) => r switch
    {
        MovieRarity.Legendary => "★ LEGENDARIA",
        MovieRarity.Epic      => "◆ ÉPICA",
        MovieRarity.Rare      => "● RARA",
        _                     => "COMÚN",
    };

    static Color ParseHexColor(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return new Color(0.15f, 0.18f, 0.28f);
        if (!hex.StartsWith("#")) hex = "#" + hex;
        return ColorUtility.TryParseHtmlString(hex, out var c) ? c : new Color(0.15f, 0.18f, 0.28f);
    }

    public static WireResult WireExisting(Transform card)
    {
        return new WireResult
        {
            posterImage     = null,
            rarityFrame     = card.Find("BodyRow/RarityIconWrap")?.GetComponent<Image>()
                           ?? card.Find("RarityIconWrap")?.GetComponent<Image>(),
            genreStrip      = card.Find("GenreStrip")?.GetComponent<Image>(),
            titleText       = card.Find("BodyRow/InfoCol/TitleWrap/Title")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("TitleWrap/Title")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("Title")?.GetComponent<TextMeshProUGUI>(),
            genreText       = card.Find("BodyRow/InfoCol/TitleWrap/Genre")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("TitleWrap/Genre")?.GetComponent<TextMeshProUGUI>(),
            rarityText      = card.Find("BodyRow/RarityIconWrap/RarityIcon")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("RarityIconWrap/RarityIcon")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("BodyRow/PosterWrap/RarityPill/Rarity")?.GetComponent<TextMeshProUGUI>(),
            durationText    = card.Find("BodyRow/InfoCol/InfoRow/Duration")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("InfoRow/Duration")?.GetComponent<TextMeshProUGUI>(),
            rewardText      = card.Find("BodyRow/InfoCol/RewardWrap/Reward")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("RewardWrap/Reward")?.GetComponent<TextMeshProUGUI>(),
            repText         = card.Find("BodyRow/InfoCol/RewardWrap/Rep")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("RewardWrap/Rep")?.GetComponent<TextMeshProUGUI>(),
            badgesText      = card.Find("BodyRow/InfoCol/InfoRow/Badges")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("InfoRow/Badges")?.GetComponent<TextMeshProUGUI>(),
            unlockText      = card.Find("BodyRow/InfoCol/Unlock")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("Unlock")?.GetComponent<TextMeshProUGUI>(),
            selectButton    = card.Find("BodyRow/InfoCol/SelectBtn")?.GetComponent<Button>()
                           ?? card.Find("SelectBtn")?.GetComponent<Button>(),
            selectLabelText = card.Find("BodyRow/InfoCol/SelectBtn/Label")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("SelectBtn/Label")?.GetComponent<TextMeshProUGUI>(),
            lockedOverlay   = card.Find("LockedOverlay")?.gameObject,
        };
    }

    public static void ApplyTitleTypography(TextMeshProUGUI titleText, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        if (titleText == null) return;

        titleText.fontStyle = FontStyles.Bold;
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 14f;
        titleText.fontSizeMax = 20f;
        titleText.textWrappingMode = TextWrappingModes.Normal;
        titleText.overflowMode = TextOverflowModes.Ellipsis;
        titleText.maxVisibleLines = 2;
        titleText.alignment = alignment;

        var le = titleText.GetComponent<LayoutElement>() ?? titleText.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = TitleRowHeight;
        le.minHeight = TitleRowHeight;
    }

    static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, float size, Color color, float height)
    {
        var tmp = RuntimeTmpText.Create(parent, text, size, color, FontStyles.Normal, TextAlignmentOptions.Center, name);
        tmp.raycastTarget = false;
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

    static void LE(RectTransform rt, float preferredHeight, float flexibleHeight = 0f, float minHeight = 0f,
        float preferredWidth = -1f)
    {
        var le = rt.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = preferredHeight;
        le.flexibleHeight = flexibleHeight;
        if (minHeight > 0f) le.minHeight = minHeight;
        if (preferredWidth > 0f)
        {
            le.preferredWidth = preferredWidth;
            le.minWidth = preferredWidth;
            le.flexibleWidth = 0f;
        }
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
