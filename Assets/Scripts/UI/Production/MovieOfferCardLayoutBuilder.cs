using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Premium movie offer card layout — mobile-first, tall, poster-dominant (Phase 8.5C).</summary>
public static class MovieOfferCardLayoutBuilder
{
    public const string LayoutMarkerName = "MovieOfferCard_v2";
    public const float TitleRowHeight    = 48f;
    public const float CardPreferredHeight = 320f;

    static readonly Color TextPrimary    = Color.white;
    static readonly Color TextSecondary  = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color BarBg          = new Color(0.09f, 0.09f, 0.18f);
    static readonly Color AccentGreen    = new Color(0.18f, 0.80f, 0.44f);
    static readonly Color AccentGold     = new Color(0.95f, 0.77f, 0.06f);

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
        vlg.padding               = new RectOffset(0, 0, 0, 8);
        vlg.spacing               = 0;
        vlg.childAlignment        = TextAnchor.UpperCenter;
        vlg.childControlWidth     = vlg.childControlHeight    = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Card preferred height so VLG container sizes it correctly
        var cardLE = card.GetComponent<LayoutElement>() ?? card.gameObject.AddComponent<LayoutElement>();
        if (cardLE.preferredHeight < CardPreferredHeight)
            cardLE.preferredHeight = CardPreferredHeight;
        if (cardLE.minHeight < 180f)
            cardLE.minHeight = 180f;

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

        // ── Genre color strip ────────────────────────────────────────────────────
        var genreStripGo = new GameObject("GenreStrip", typeof(RectTransform), typeof(Image));
        genreStripGo.transform.SetParent(card, false);
        result.genreStrip         = genreStripGo.GetComponent<Image>();
        result.genreStrip.color   = cfg != null ? GenreAccentColor(cfg.genre) : BarBg;
        result.genreStrip.raycastTarget = false;
        LE(genreStripGo.GetComponent<RectTransform>(), 6f);

        // ── Poster area ──────────────────────────────────────────────────────────
        var posterWrap = CreatePanel(card, "PosterWrap", BarBg);
        LE(posterWrap, 160f, minHeight: 130f);

        result.rarityFrame = CreatePanel(posterWrap.transform, "RarityFrame", cfg != null ? RarityFrameColor(cfg.rarity) : Color.clear).GetComponent<Image>();
        Stretch(result.rarityFrame.rectTransform);
        result.rarityFrame.raycastTarget = false;

        result.posterImage = CreatePanel(posterWrap.transform, "Poster", cfg != null ? ParseHexColor(cfg.posterColorHex) : BarBg).GetComponent<Image>();
        var posterRT = result.posterImage.rectTransform;
        posterRT.anchorMin = new Vector2(0.03f, 0.04f);
        posterRT.anchorMax = new Vector2(0.97f, 0.96f);
        posterRT.offsetMin = posterRT.offsetMax = Vector2.zero;
        result.posterImage.raycastTarget = false;

        // Rarity pill top-right inside poster
        var rarityPill = new GameObject("RarityPill", typeof(RectTransform), typeof(Image));
        rarityPill.transform.SetParent(posterWrap.transform, false);
        rarityPill.GetComponent<Image>().color = cfg != null ? RarityPillColor(cfg.rarity) : BarBg;
        rarityPill.GetComponent<Image>().raycastTarget = false;
        var pillRT = rarityPill.GetComponent<RectTransform>();
        pillRT.anchorMin = new Vector2(1f, 1f);
        pillRT.anchorMax = new Vector2(1f, 1f);
        pillRT.pivot     = new Vector2(1f, 1f);
        pillRT.sizeDelta = new Vector2(72f, 24f);
        pillRT.anchoredPosition = new Vector2(-6f, -6f);

        result.rarityText = RuntimeTmpText.Create(rarityPill.transform, cfg != null ? RarityLabel(cfg.rarity) : string.Empty,
            11f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center, "Rarity");
        result.rarityText.raycastTarget = false;
        Stretch(result.rarityText.rectTransform);

        // ── Title + genre row ────────────────────────────────────────────────────
        var titleBg = new GameObject("TitleWrap", typeof(RectTransform), typeof(Image));
        titleBg.transform.SetParent(card, false);
        titleBg.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.12f, 0.85f);
        titleBg.GetComponent<Image>().raycastTarget = false;
        var titleWrapLE = titleBg.AddComponent<LayoutElement>();
        titleWrapLE.preferredHeight = TitleRowHeight;
        titleWrapLE.minHeight = TitleRowHeight;

        var titleVLG = titleBg.AddComponent<VerticalLayoutGroup>();
        titleVLG.padding = new RectOffset(12, 12, 6, 4);
        titleVLG.spacing = 2;
        titleVLG.childControlWidth = titleVLG.childControlHeight = true;
        titleVLG.childForceExpandWidth = true;
        titleVLG.childForceExpandHeight = false;

        result.titleText = RuntimeTmpText.Create(titleBg.transform, cfg?.movieName ?? string.Empty,
            16f, TextPrimary, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "Title");
        result.titleText.enableAutoSizing = true;
        result.titleText.fontSizeMin = 13f;
        result.titleText.fontSizeMax = 18f;
        result.titleText.textWrappingMode = TextWrappingModes.Normal;
        result.titleText.overflowMode = TextOverflowModes.Ellipsis;
        result.titleText.maxVisibleLines = 1;
        result.titleText.raycastTarget = false;
        result.titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        result.genreText = RuntimeTmpText.Create(titleBg.transform, cfg != null ? GenreLabel(cfg.genre) : string.Empty,
            12f, cfg != null ? GenreAccentColor(cfg.genre) : TextSecondary, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "Genre");
        result.genreText.raycastTarget = false;
        result.genreText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

        // ── Info row: duration + badges ──────────────────────────────────────────
        var infoRow = CreatePanel(card, "InfoRow", Color.clear);
        LE(infoRow, 26f);
        var infoHLG = infoRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        infoHLG.padding = new RectOffset(12, 12, 4, 4);
        infoHLG.spacing = 8;
        infoHLG.childAlignment = TextAnchor.MiddleLeft;
        infoHLG.childControlWidth = infoHLG.childControlHeight = true;
        infoHLG.childForceExpandWidth = false;
        infoHLG.childForceExpandHeight = true;

        result.durationText = RuntimeTmpText.Create(infoRow.transform, string.Empty,
            13f, TextSecondary, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, "Duration");
        result.durationText.raycastTarget = false;
        result.durationText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        result.badgesText = RuntimeTmpText.Create(infoRow.transform, string.Empty,
            13f, AccentGold, FontStyles.Bold, TextAlignmentOptions.MidlineRight, "Badges");
        result.badgesText.raycastTarget = false;
        result.badgesText.gameObject.AddComponent<LayoutElement>().preferredWidth = 80f;

        // ── Reward row: money + rep ──────────────────────────────────────────────
        var rewardWrap = new GameObject("RewardWrap", typeof(RectTransform), typeof(Image));
        rewardWrap.transform.SetParent(card, false);
        rewardWrap.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.12f, 0.85f);
        rewardWrap.GetComponent<Image>().raycastTarget = false;
        LE(rewardWrap.GetComponent<RectTransform>(), 32f);

        var rewardRow = rewardWrap;
        var rewardHLG = rewardWrap.AddComponent<HorizontalLayoutGroup>();
        rewardHLG.padding = new RectOffset(12, 12, 6, 6);
        rewardHLG.spacing = 8;
        rewardHLG.childAlignment = TextAnchor.MiddleCenter;
        rewardHLG.childControlWidth = rewardHLG.childControlHeight = true;
        rewardHLG.childForceExpandWidth = rewardHLG.childForceExpandHeight = true;

        result.rewardText = RuntimeTmpText.Create(rewardWrap.transform, string.Empty,
            15f, AccentGreen, FontStyles.Bold, TextAlignmentOptions.Center, "Reward");
        result.rewardText.raycastTarget = false;
        result.rewardText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        result.repText = RuntimeTmpText.Create(rewardWrap.transform, string.Empty,
            15f, AccentGold, FontStyles.Bold, TextAlignmentOptions.Center, "Rep");
        result.repText.raycastTarget = false;
        result.repText.gameObject.AddComponent<LayoutElement>().preferredWidth = 90f;

        // ── Unlock notice (hidden by default) ───────────────────────────────────
        result.unlockText = CreateLabel(card, "Unlock", string.Empty, 12f, new Color(0.95f, 0.45f, 0.35f), 20f);
        result.unlockText.gameObject.SetActive(false);

        // ── Select button ────────────────────────────────────────────────────────
        var btnGo = new GameObject("SelectBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(card, false);
        HudSkinProvider.ApplyButton(btnGo.GetComponent<Image>(), HudButtonVariant.Success);
        btnGo.AddComponent<UIButtonScale>();
        LE(btnGo.GetComponent<RectTransform>(), 52f, minHeight: 52f);

        var btnVLG = btnGo.AddComponent<VerticalLayoutGroup>();
        btnVLG.padding = new RectOffset(12, 12, 0, 0);
        btnVLG.childAlignment = TextAnchor.MiddleCenter;
        btnVLG.childControlWidth = btnVLG.childControlHeight = true;
        btnVLG.childForceExpandWidth = btnVLG.childForceExpandHeight = true;

        result.selectButton = btnGo.GetComponent<Button>();
        result.selectLabelText = RuntimeTmpText.Create(btnGo.transform, Loc.Get(LocKeys.ProdSelect),
            16f, TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center, "Label");
        result.selectLabelText.enableAutoSizing = true;
        result.selectLabelText.fontSizeMin = 13f;
        result.selectLabelText.fontSizeMax = 18f;
        result.selectLabelText.raycastTarget = false;

        // ── Locked overlay ───────────────────────────────────────────────────────
        var overlay = CreatePanel(card, "LockedOverlay", new Color(0f, 0f, 0f, 0.55f));
        Stretch(overlay);
        overlay.gameObject.SetActive(false);
        result.lockedOverlay = overlay.gameObject;

        return result;
    }

    // ── Color helpers ────────────────────────────────────────────────────────────

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
        MovieRarity.Legendary => new Color(1.0f, 0.84f, 0.0f, 0.25f),
        MovieRarity.Epic      => new Color(0.63f, 0.13f, 0.94f, 0.20f),
        MovieRarity.Rare      => new Color(0.20f, 0.60f, 0.86f, 0.20f),
        _                     => Color.clear,
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
            posterImage     = card.Find("PosterWrap/Poster")?.GetComponent<Image>(),
            rarityFrame     = card.Find("PosterWrap/RarityFrame")?.GetComponent<Image>(),
            genreStrip      = card.Find("GenreStrip")?.GetComponent<Image>(),
            titleText       = card.Find("TitleWrap/Title")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("Title")?.GetComponent<TextMeshProUGUI>(),
            genreText       = card.Find("TitleWrap/Genre")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("MetaRow/Genre")?.GetComponent<TextMeshProUGUI>(),
            rarityText      = card.Find("PosterWrap/RarityPill/Rarity")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("MetaRow/Rarity")?.GetComponent<TextMeshProUGUI>(),
            durationText    = card.Find("InfoRow/Duration")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("Duration")?.GetComponent<TextMeshProUGUI>(),
            rewardText      = card.Find("RewardWrap/Reward")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("RewardRow/Reward")?.GetComponent<TextMeshProUGUI>(),
            repText         = card.Find("RewardWrap/Rep")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("RewardRow/Rep")?.GetComponent<TextMeshProUGUI>(),
            badgesText      = card.Find("InfoRow/Badges")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("Badges")?.GetComponent<TextMeshProUGUI>(),
            unlockText      = card.Find("Unlock")?.GetComponent<TextMeshProUGUI>(),
            selectButton    = card.Find("SelectBtn")?.GetComponent<Button>(),
            selectLabelText = card.Find("SelectBtn/Label")?.GetComponent<TextMeshProUGUI>(),
            lockedOverlay   = card.Find("LockedOverlay")?.gameObject,
        };
    }

    public static void ApplyTitleTypography(TextMeshProUGUI titleText, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        if (titleText == null) return;

        titleText.fontStyle = FontStyles.Bold;
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 13f;
        titleText.fontSizeMax = 18f;
        titleText.textWrappingMode = TextWrappingModes.Normal;
        titleText.overflowMode = TextOverflowModes.Ellipsis;
        titleText.maxVisibleLines = 1;
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

    static void LE(RectTransform rt, float preferredHeight, float flexibleHeight = 0f, float minHeight = 0f)
    {
        var le = rt.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = preferredHeight;
        le.flexibleHeight = flexibleHeight;
        if (minHeight > 0f) le.minHeight = minHeight;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
