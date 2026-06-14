using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Premium movie offer card — tycoon-style production opportunity (Phase 9.0).</summary>
public static class MovieOfferCardLayoutBuilder
{
    public const string LayoutMarkerName = "MovieOfferCard_v5";
    public const float RarityIconWidth   = 84f;
    public const float TitleRowHeight    = 34f;
    public const float CompactBodyHeight = 248f;
    public const float CardPreferredHeight = HudLayoutConstants.OfferCardBaseHeight;

    static readonly Color TextPrimary    = Color.white;
    static readonly Color TextSecondary  = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color BarBg          = new Color(0.09f, 0.09f, 0.18f);
    static readonly Color AccentGreen    = CinematicTheme.GoldBase;
    static readonly Color AccentGold     = CinematicTheme.GoldBase;
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

    public static bool HasPremiumLayout(Transform root)
    {
        if (root.Find(LayoutMarkerName) != null) return true;

        // Remove legacy v3/v4 markers so the card rebuilds with v5 layout
        foreach (var legacyName in new[] { "MovieOfferCard_v4", "MovieOfferCard_v3" })
        {
            var legacy = root.Find(legacyName);
            if (legacy == null) continue;
            if (Application.isPlaying) Object.Destroy(legacy.gameObject);
            else Object.DestroyImmediate(legacy.gameObject);
        }
        return false;
    }

    public static WireResult Build(Transform cardRoot, MovieConfig cfg, bool isEmpty)
    {
        var result = new WireResult();
        var card   = cardRoot as RectTransform;
        if (card == null) return result;

        HudSkinProvider.ApplyCard(card.GetComponent<Image>(), isEmpty ? HudCardVariant.Hero : HudCardVariant.Primary);
        CinematicTheme.ApplyPremiumMaterial(card);

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
        cardLE.minHeight = CardPreferredHeight;
        cardLE.flexibleHeight = 0f;

        if (isEmpty)
        {
            var emptyGo = new GameObject("EmptyContent", typeof(RectTransform));
            emptyGo.transform.SetParent(card, false);
            LE(emptyGo.GetComponent<RectTransform>(), CardPreferredHeight - 20f);
            var emptyInner = emptyGo.AddComponent<VerticalLayoutGroup>();
            emptyInner.childAlignment = TextAnchor.MiddleCenter;
            emptyInner.childControlWidth = emptyInner.childControlHeight = true;
            emptyInner.childForceExpandWidth = emptyInner.childForceExpandHeight = false;

            var empty = CreateLabel(emptyGo.transform, "EmptyLabel", "BLOQUEADO", 28f, TextSecondary, 80f);
            empty.fontStyle = FontStyles.Bold;
            empty.alignment = TextAlignmentOptions.Center;
            empty.enableAutoSizing = true;
            empty.fontSizeMin = 22f;
            empty.fontSizeMax = 32f;
            return result;
        }

        var genreStripGo = new GameObject("GenreStrip", typeof(RectTransform), typeof(Image));
        genreStripGo.transform.SetParent(card, false);
        result.genreStrip = genreStripGo.GetComponent<Image>();
        result.genreStrip.color = Color.clear;
        result.genreStrip.raycastTarget = false;
        LE(genreStripGo.GetComponent<RectTransform>(), 0f);
        genreStripGo.SetActive(false);

        var bodyRow = new GameObject("BodyRow", typeof(RectTransform));
        bodyRow.transform.SetParent(card, false);
        var bodyLE = bodyRow.AddComponent<LayoutElement>();
        bodyLE.preferredHeight = CompactBodyHeight;
        bodyLE.minHeight = CompactBodyHeight;
        bodyLE.flexibleHeight = 0f;

        var bodyVLG = bodyRow.AddComponent<VerticalLayoutGroup>();
        bodyVLG.padding = new RectOffset(10, 10, 8, 8);
        bodyVLG.spacing = 4;
        bodyVLG.childAlignment = TextAnchor.UpperLeft;
        bodyVLG.childControlWidth = bodyVLG.childControlHeight = true;
        bodyVLG.childForceExpandWidth = true;
        bodyVLG.childForceExpandHeight = false;

        SetupRarityIconColumn(bodyRow.transform as RectTransform, cfg?.rarity ?? MovieRarity.Common,
            cfg?.genre ?? MovieGenre.Drama, ref result);

        result.titleText = RuntimeTmpText.Create(bodyRow.transform, cfg?.movieName ?? string.Empty,
            22f, TextPrimary, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "Title");
        result.titleText.textWrappingMode = TextWrappingModes.Normal;
        result.titleText.overflowMode = TextOverflowModes.Ellipsis;
        result.titleText.maxVisibleLines = 2;
        result.titleText.raycastTarget = false;
        LE(result.titleText.rectTransform, TitleRowHeight);

        result.genreText = RuntimeTmpText.Create(bodyRow.transform, cfg != null ? GenreLabel(cfg.genre) : string.Empty,
            13f, cfg != null ? GenreAccentColor(cfg.genre) : TextSecondary, FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft, "Genre");
        result.genreText.raycastTarget = false;
        LE(result.genreText.rectTransform, 18f);

        result.durationText = RuntimeTmpText.Create(bodyRow.transform, "⏱ —",
            15f, TextSecondary, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "Duration");
        result.durationText.raycastTarget = false;
        LE(result.durationText.rectTransform, 18f);

        var rewardRow = CreatePanel(bodyRow.transform, "RewardRow", Color.clear);
        LE(rewardRow, 20f);
        var rewardHLG = rewardRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        rewardHLG.spacing = 8;
        rewardHLG.childAlignment = TextAnchor.MiddleLeft;
        rewardHLG.childControlWidth = rewardHLG.childControlHeight = true;
        rewardHLG.childForceExpandWidth = false;
        rewardHLG.childForceExpandHeight = false;

        result.rewardText = RuntimeTmpText.Create(rewardRow.transform, "💵 —",
            14f, AccentGold, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "Reward");
        result.rewardText.raycastTarget = false;
        result.rewardText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        result.repText = RuntimeTmpText.Create(rewardRow.transform, "🏆 —",
            14f, AccentGold, FontStyles.Bold, TextAlignmentOptions.MidlineRight, "Rep");
        result.repText.raycastTarget = false;
        result.repText.gameObject.AddComponent<LayoutElement>().preferredWidth = 72f;

        result.badgesText = RuntimeTmpText.Create(bodyRow.transform, string.Empty,
            11f, AccentGold, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "Badges");
        result.badgesText.raycastTarget = false;
        LE(result.badgesText.rectTransform, 0f);
        result.badgesText.gameObject.SetActive(false);

        result.unlockText = CreateLabel(bodyRow.transform, "Unlock", string.Empty, 12f, new Color(0.95f, 0.45f, 0.35f), 0f);
        result.unlockText.gameObject.SetActive(false);

        var btnGo = new GameObject("SelectBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(bodyRow.transform, false);
        HudSkinProvider.ApplyButton(btnGo.GetComponent<Image>(), HudButtonVariant.Success);
        btnGo.AddComponent<UIButtonScale>();
        LE(btnGo.GetComponent<RectTransform>(), 48f, minHeight: 48f);

        result.selectButton = btnGo.GetComponent<Button>();
        result.selectLabelText = RuntimeTmpText.Create(btnGo.transform, Loc.Get(LocKeys.ProdSelect),
            17f, TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center, "Label");
        result.selectLabelText.raycastTarget = false;
        Stretch(result.selectLabelText.rectTransform);

        var overlay = CreatePanel(card, "LockedOverlay", new Color(0f, 0f, 0f, 0.58f));
        Stretch(overlay);
        overlay.gameObject.SetActive(false);
        result.lockedOverlay = overlay.gameObject;

        return result;
    }

    static void SetupRarityIconColumn(RectTransform bodyRow, MovieRarity rarity, MovieGenre genre, ref WireResult result)
    {
        if (bodyRow == null) return;

        var iconWrap = CreatePanel(bodyRow, "RarityIconWrap", Color.clear);
        iconWrap.SetAsFirstSibling();
        var iconLE = iconWrap.GetComponent<LayoutElement>() ?? iconWrap.gameObject.AddComponent<LayoutElement>();
        iconLE.preferredWidth  = RarityIconWidth;
        iconLE.minWidth        = RarityIconWidth;
        iconLE.flexibleWidth   = 0f;
        iconLE.preferredHeight = RarityIconWidth;
        iconLE.minHeight       = RarityIconWidth;
        iconLE.flexibleHeight  = 0f;

        result.rarityFrame = iconWrap.GetComponent<Image>();
        result.rarityFrame.color = Color.clear;
        result.rarityFrame.raycastTarget = false;

        result.rarityText = RuntimeTmpText.Create(iconWrap.transform, GetRarityIcon(rarity, genre),
            44f, GetRarityIconColor(rarity, genre), FontStyles.Normal, TextAlignmentOptions.Center, "RarityIcon");
        result.rarityText.raycastTarget = false;
        Stretch(result.rarityText.rectTransform);
        UIIconGraphic.ApplyGenreRarityIcon(iconWrap.transform, UIIconCatalog.GetProductionIcon(rarity, genre), result.rarityText);

        result.posterImage = null;
    }

    /// <summary>Phase 10.1 — replace legacy poster UI with decorative rarity icon.</summary>
    public static void ApplyRarityIconLayout(Transform cardRoot, MovieRarity rarity, MovieGenre genre = MovieGenre.Drama)
    {
        if (cardRoot == null) return;
        ApplyCompactOfferLayout(cardRoot);

        var bodyRow = cardRoot.Find("BodyRow") as RectTransform;
        if (bodyRow == null) return;

        foreach (var legacyName in new[] { "PosterWrap", "PosterArea", "PosterSlot", "PosterPlaceholder", "IconCol", "EmptySpace", "EmptyContent" })
        {
            var legacy = bodyRow.Find(legacyName) ?? cardRoot.Find(legacyName);
            if (legacy == null) continue;
            if (Application.isPlaying) Object.Destroy(legacy.gameObject);
            else Object.DestroyImmediate(legacy.gameObject);
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
            UIIconGraphic.ApplyGenreRarityIcon(bodyRow.Find("RarityIconWrap"), UIIconCatalog.GetProductionIcon(rarity, genre), icon);

            var frame = bodyRow.Find("RarityIconWrap")?.GetComponent<Image>();
            if (frame != null)
                frame.color = Color.clear;
        }

        RemoveLateralDecorIcons(bodyRow);

        // Phase 13.4B — full-card rarity border on every code path
        MovieRarityVisual.ApplyCardBorder(cardRoot as RectTransform, rarity);
    }

    /// <summary>Phase 13.3C — strip temporary lateral decor and colored icon boxes.</summary>
    public static void RemoveLateralDecorIcons(Transform scope)
    {
        if (scope == null) return;

        foreach (var legacyName in new[] { "IconCol", "DecorLeft", "DecorRight", "SideDecor", "PosterWrap", "PosterArea", "PosterSlot", "PosterPlaceholder", "EmptySpace" })
        {
            var legacy = scope.Find(legacyName);
            if (legacy == null) continue;
            if (Application.isPlaying) Object.Destroy(legacy.gameObject);
            else Object.DestroyImmediate(legacy.gameObject);
        }

        var iconWrap = scope.Find("RarityIconWrap");
        if (iconWrap != null)
        {
            var frame = iconWrap.GetComponent<Image>();
            if (frame != null) frame.color = Color.clear;

            var sprite = iconWrap.Find("RarityIconSprite")?.GetComponent<Image>();
            if (sprite != null)
            {
                var rt = sprite.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
        }
    }

    /// <summary>Phase 12.4C — remove poster dead space on baked offer cards.</summary>
    public static void ApplyCompactOfferLayout(Transform cardRoot)
    {
        if (cardRoot == null) return;
        var card = cardRoot as RectTransform;
        if (card == null) return;

        var cardLE = card.GetComponent<LayoutElement>();
        if (cardLE != null)
        {
            cardLE.preferredHeight = CardPreferredHeight;
            cardLE.minHeight = CardPreferredHeight;
            cardLE.flexibleHeight = 0f;
        }

        var bodyRow = card.Find("BodyRow") as RectTransform;
        if (bodyRow == null) return;

        foreach (var legacyName in new[] { "PosterWrap", "PosterArea", "PosterSlot", "PosterPlaceholder", "EmptySpace" })
        {
            var legacy = bodyRow.Find(legacyName);
            if (legacy == null) continue;
            if (Application.isPlaying) Object.Destroy(legacy.gameObject);
            else Object.DestroyImmediate(legacy.gameObject);
        }

        var bodyLE = bodyRow.GetComponent<LayoutElement>() ?? bodyRow.gameObject.AddComponent<LayoutElement>();
        bodyLE.preferredHeight = CompactBodyHeight;
        bodyLE.minHeight = CompactBodyHeight;
        bodyLE.flexibleHeight = 0f;

        var bodyHLG = bodyRow.GetComponent<HorizontalLayoutGroup>();
        var bodyVLG = bodyRow.GetComponent<VerticalLayoutGroup>();
        if (bodyHLG != null && bodyVLG != null && bodyRow.Find("InfoCol") == null)
        {
            if (Application.isPlaying) Object.Destroy(bodyHLG);
            else Object.DestroyImmediate(bodyHLG);
            bodyHLG = null;
        }

        if (bodyHLG != null)
        {
            bodyHLG.childForceExpandHeight = false;
            bodyHLG.padding = new RectOffset(8, 8, 6, 6);
        }

        if (bodyVLG != null)
        {
            bodyVLG.spacing = 4;
            bodyVLG.padding = new RectOffset(10, 10, 8, 8);
            bodyVLG.childForceExpandHeight = false;
            bodyVLG.childControlHeight = true;
        }

        EnsureDirectTextRowHeights(bodyRow);
        RemoveLateralDecorIcons(bodyRow);

        var genreStrip = card.Find("GenreStrip") as RectTransform;
        if (genreStrip != null)
        {
            var gsImg = genreStrip.GetComponent<Image>();
            if (gsImg != null) gsImg.color = Color.clear;
            var gsLE = genreStrip.GetComponent<LayoutElement>() ?? genreStrip.gameObject.AddComponent<LayoutElement>();
            gsLE.preferredHeight = 0f;
            gsLE.minHeight = 0f;
            genreStrip.gameObject.SetActive(false);
        }

        var titleWrap = bodyRow.Find("InfoCol/TitleWrap") ?? bodyRow.Find("TitleWrap");
        if (titleWrap != null)
        {
            var twLE = titleWrap.GetComponent<LayoutElement>() ?? titleWrap.gameObject.AddComponent<LayoutElement>();
            twLE.preferredHeight = TitleRowHeight;
            twLE.minHeight = TitleRowHeight;
        }

        var rewardWrap = bodyRow.Find("InfoCol/RewardWrap") ?? bodyRow.Find("RewardWrap");
        if (rewardWrap != null)
        {
            var rwLE = rewardWrap.GetComponent<LayoutElement>() ?? rewardWrap.gameObject.AddComponent<LayoutElement>();
            rwLE.preferredHeight = 24f;
            rwLE.minHeight = 20f;
        }

        var selectBtn = bodyRow.Find("InfoCol/SelectBtn") ?? bodyRow.Find("SelectBtn");
        if (selectBtn != null)
        {
            var sle = selectBtn.GetComponent<LayoutElement>() ?? selectBtn.gameObject.AddComponent<LayoutElement>();
            sle.preferredHeight = 48f;
            sle.minHeight = 48f;
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
        ApplyCompactOfferLayout(card);

        float scale = Mathf.Clamp(targetHeight / CardPreferredHeight, 1f,
            HudLayoutConstants.OfferCardMaxExpanded / CardPreferredHeight);

        var title = card.Find("BodyRow/Title")?.GetComponent<TextMeshProUGUI>()
                 ?? card.Find("BodyRow/InfoCol/TitleWrap/Title")?.GetComponent<TextMeshProUGUI>();
        if (title != null)
        {
            title.fontSizeMax = 20f * Mathf.Min(scale, 1.12f);
            title.fontSizeMin = 14f;
        }

        var reward = card.Find("BodyRow/RewardRow/Reward")?.GetComponent<TextMeshProUGUI>()
                  ?? card.Find("BodyRow/InfoCol/RewardWrap/Reward")?.GetComponent<TextMeshProUGUI>();
        if (reward != null) reward.fontSize = 14f * Mathf.Min(scale, 1.08f);

        var rep = card.Find("BodyRow/RewardRow/Rep")?.GetComponent<TextMeshProUGUI>()
               ?? card.Find("BodyRow/InfoCol/RewardWrap/Rep")?.GetComponent<TextMeshProUGUI>();
        if (rep != null) rep.fontSize = 14f * Mathf.Min(scale, 1.08f);

        var selectBtn = card.Find("BodyRow/SelectBtn") as RectTransform
                     ?? card.Find("BodyRow/InfoCol/SelectBtn") as RectTransform;
        if (selectBtn != null)
        {
            var le = selectBtn.GetComponent<LayoutElement>();
            if (le != null) le.preferredHeight = 48f * Mathf.Min(scale, 1.08f);
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
            titleText       = card.Find("BodyRow/Title")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("BodyRow/InfoCol/TitleWrap/Title")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("TitleWrap/Title")?.GetComponent<TextMeshProUGUI>(),
            genreText       = card.Find("BodyRow/Genre")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("BodyRow/InfoCol/TitleWrap/Genre")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("TitleWrap/Genre")?.GetComponent<TextMeshProUGUI>(),
            rarityText      = card.Find("BodyRow/RarityIconWrap/RarityIcon")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("RarityIconWrap/RarityIcon")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("BodyRow/PosterWrap/RarityPill/Rarity")?.GetComponent<TextMeshProUGUI>(),
            durationText    = card.Find("BodyRow/Duration")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("BodyRow/InfoCol/InfoRow/Duration")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("InfoRow/Duration")?.GetComponent<TextMeshProUGUI>(),
            rewardText      = card.Find("BodyRow/RewardRow/Reward")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("BodyRow/InfoCol/RewardWrap/Reward")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("RewardWrap/Reward")?.GetComponent<TextMeshProUGUI>(),
            repText         = card.Find("BodyRow/RewardRow/Rep")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("BodyRow/InfoCol/RewardWrap/Rep")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("RewardWrap/Rep")?.GetComponent<TextMeshProUGUI>(),
            badgesText      = card.Find("BodyRow/Badges")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("BodyRow/InfoCol/InfoRow/Badges")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("InfoRow/Badges")?.GetComponent<TextMeshProUGUI>(),
            unlockText      = card.Find("BodyRow/Unlock")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("BodyRow/InfoCol/Unlock")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("Unlock")?.GetComponent<TextMeshProUGUI>(),
            selectButton    = card.Find("BodyRow/SelectBtn")?.GetComponent<Button>()
                           ?? card.Find("BodyRow/InfoCol/SelectBtn")?.GetComponent<Button>()
                           ?? card.Find("SelectBtn")?.GetComponent<Button>(),
            selectLabelText = card.Find("BodyRow/SelectBtn/Label")?.GetComponent<TextMeshProUGUI>()
                           ?? card.Find("BodyRow/InfoCol/SelectBtn/Label")?.GetComponent<TextMeshProUGUI>()
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

    static void EnsureDirectTextRowHeights(RectTransform bodyRow)
    {
        if (bodyRow == null) return;

        EnsureRowLE(bodyRow.Find("Title") as RectTransform, TitleRowHeight);
        EnsureRowLE(bodyRow.Find("Genre") as RectTransform, 14f);
        EnsureRowLE(bodyRow.Find("Duration") as RectTransform, 18f);

        var rewardRow = bodyRow.Find("RewardRow") as RectTransform;
        if (rewardRow != null)
        {
            var rwLE = rewardRow.GetComponent<LayoutElement>() ?? rewardRow.gameObject.AddComponent<LayoutElement>();
            rwLE.preferredHeight = 20f;
            rwLE.minHeight = 20f;
            rwLE.flexibleHeight = 0f;
        }
    }

    static void EnsureRowLE(RectTransform rt, float height)
    {
        if (rt == null) return;
        var le = rt.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
        le.flexibleHeight = 0f;
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

    /// <summary>Emoji fallback shown until a real icon sprite is assigned.</summary>
    static string GenreEmoji(MovieGenre g) => g switch
    {
        MovieGenre.Action      => "⚔️",
        MovieGenre.Drama       => "🎭",
        MovieGenre.Horror      => "💀",
        MovieGenre.Comedy      => "😂",
        MovieGenre.Romance     => "💘",
        MovieGenre.SciFi       => "🚀",
        MovieGenre.Fantasy     => "🔮",
        MovieGenre.Thriller    => "🔪",
        MovieGenre.Animation   => "🎨",
        MovieGenre.Documentary => "📽️",
        _                      => "🎬",
    };
}
