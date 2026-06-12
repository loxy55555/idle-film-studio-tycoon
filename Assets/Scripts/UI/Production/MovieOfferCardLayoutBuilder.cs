using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Premium movie offer card layout — poster-first (Phase 8.2).</summary>
public static class MovieOfferCardLayoutBuilder
{
    public const string LayoutMarkerName = "MovieOfferCard_v2";
    public const float TitleRowHeight = 38f;

    static readonly Color TextPrimary   = Color.white;
    static readonly Color TextSecondary = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color BarBg         = new Color(0.09f, 0.09f, 0.18f);

    public struct WireResult
    {
        public Image posterImage;
        public Image rarityFrame;
        public Image genreStrip;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI genreText;
        public TextMeshProUGUI rarityText;
        public TextMeshProUGUI durationText;
        public TextMeshProUGUI unlockText;
        public Button selectButton;
        public TextMeshProUGUI selectLabelText;
        public GameObject lockedOverlay;
    }

    public static bool HasPremiumLayout(Transform root) => root.Find(LayoutMarkerName) != null;

    public static WireResult Build(Transform cardRoot, MovieConfig cfg, bool isEmpty)
    {
        var result = new WireResult();

        var card = cardRoot as RectTransform;
        if (card == null) return result;

        HudSkinProvider.ApplyCard(card.GetComponent<Image>(), isEmpty ? HudCardVariant.Hero : HudCardVariant.Primary);

        var marker = new GameObject(LayoutMarkerName, typeof(RectTransform));
        marker.transform.SetParent(card, false);
        Stretch(marker.GetComponent<RectTransform>());

        var vlg = card.gameObject.GetComponent<VerticalLayoutGroup>() ?? card.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.spacing = 4;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        if (isEmpty)
        {
            var empty = CreateLabel(card, "EmptyLabel", Loc.Get(LocKeys.ProdEmptySlot), 12f, TextSecondary, 40f);
            empty.enableAutoSizing = true;
            empty.fontSizeMin = 10f;
            empty.fontSizeMax = 13f;
            return result;
        }

        var posterWrap = CreatePanel(card, "PosterWrap", BarBg);
        LE(posterWrap, 0f, flexibleHeight: 1f, minHeight: 72f);

        result.rarityFrame = CreatePanel(posterWrap.transform, "RarityFrame", Color.clear).GetComponent<Image>();
        Stretch(result.rarityFrame.rectTransform);
        result.rarityFrame.raycastTarget = false;

        result.posterImage = CreatePanel(posterWrap.transform, "Poster", Color.clear).GetComponent<Image>();
        var posterRT = result.posterImage.rectTransform;
        posterRT.anchorMin = new Vector2(0.04f, 0.04f);
        posterRT.anchorMax = new Vector2(0.96f, 0.96f);
        posterRT.offsetMin = posterRT.offsetMax = Vector2.zero;
        result.posterImage.raycastTarget = false;

        result.titleText = CreateLabel(card, "Title", cfg?.movieName ?? string.Empty, 12f, TextPrimary, TitleRowHeight);
        ApplyTitleTypography(result.titleText);

        var metaRow = CreatePanel(card, "MetaRow", Color.clear);
        LE(metaRow, 16f);
        var metaHLG = metaRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        metaHLG.spacing = 4;
        metaHLG.childAlignment = TextAnchor.MiddleCenter;
        metaHLG.childControlWidth = metaHLG.childControlHeight = true;
        metaHLG.childForceExpandWidth = metaHLG.childForceExpandHeight = true;

        result.genreText = CreateLabel(metaRow.transform, "Genre", string.Empty, 9f, TextSecondary, 14f);
        result.genreText.enableAutoSizing = true;
        result.genreText.fontSizeMin = 8f;
        result.genreText.fontSizeMax = 10f;
        result.genreText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        result.rarityText = CreateLabel(metaRow.transform, "Rarity", string.Empty, 9f, TextSecondary, 14f);
        result.rarityText.fontStyle = FontStyles.Bold;
        result.rarityText.enableAutoSizing = true;
        result.rarityText.fontSizeMin = 8f;
        result.rarityText.fontSizeMax = 10f;
        result.rarityText.gameObject.AddComponent<LayoutElement>().preferredWidth = 52f;

        result.durationText = CreateLabel(card, "Duration", string.Empty, 10f, TextSecondary, 14f);
        result.durationText.enableAutoSizing = true;
        result.durationText.fontSizeMin = 9f;
        result.durationText.fontSizeMax = 11f;

        result.unlockText = CreateLabel(card, "Unlock", string.Empty, 9f, new Color(0.95f, 0.45f, 0.35f), 16f);
        result.unlockText.gameObject.SetActive(false);

        var btnGo = new GameObject("SelectBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(card, false);
        HudSkinProvider.ApplyButton(btnGo.GetComponent<Image>(), HudButtonVariant.Success);
        btnGo.AddComponent<UIButtonScale>();
        LE(btnGo.GetComponent<RectTransform>(), 32f);
        result.selectButton = btnGo.GetComponent<Button>();
        result.selectLabelText = CreateLabel(btnGo.transform, "Label", Loc.Get(LocKeys.ProdSelect), 11f, TextPrimary, 14f);
        result.selectLabelText.fontStyle = FontStyles.Bold;
        result.selectLabelText.enableAutoSizing = true;
        result.selectLabelText.fontSizeMin = 9f;
        result.selectLabelText.fontSizeMax = 12f;

        var overlay = CreatePanel(card, "LockedOverlay", new Color(0f, 0f, 0f, 0.55f));
        Stretch(overlay);
        overlay.gameObject.SetActive(false);
        result.lockedOverlay = overlay.gameObject;

        return result;
    }

    public static WireResult WireExisting(Transform card)
    {
        return new WireResult
        {
            posterImage = card.Find("PosterWrap/Poster")?.GetComponent<Image>(),
            rarityFrame = card.Find("PosterWrap/RarityFrame")?.GetComponent<Image>(),
            titleText = card.Find("Title")?.GetComponent<TextMeshProUGUI>(),
            genreText = card.Find("MetaRow/Genre")?.GetComponent<TextMeshProUGUI>(),
            rarityText = card.Find("MetaRow/Rarity")?.GetComponent<TextMeshProUGUI>(),
            durationText = card.Find("Duration")?.GetComponent<TextMeshProUGUI>(),
            unlockText = card.Find("Unlock")?.GetComponent<TextMeshProUGUI>(),
            selectButton = card.Find("SelectBtn")?.GetComponent<Button>(),
            selectLabelText = card.Find("SelectBtn/Label")?.GetComponent<TextMeshProUGUI>(),
            lockedOverlay = card.Find("LockedOverlay")?.gameObject,
        };
    }

    public static void ApplyTitleTypography(TextMeshProUGUI titleText, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        if (titleText == null) return;

        titleText.fontStyle = FontStyles.Bold;
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 10f;
        titleText.fontSizeMax = 13f;
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
