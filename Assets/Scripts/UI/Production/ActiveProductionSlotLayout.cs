using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Premium active production slot layout (Phase 9.0 / 10.3 discovery).</summary>
public static class ActiveProductionSlotLayout
{
    public const float SlotHeight = 140f;

    static readonly Color TextPrimary   = CinematicTheme.TextPrimary;
    static readonly Color TextSecondary = CinematicTheme.TextSecondary;
    static readonly Color BarBg         = CinematicTheme.DeepBg;
    static readonly Color BarFill       = CinematicTheme.ProgressFill;
    static readonly Color StatusGreen   = CinematicTheme.GoldBase;
    static readonly Color StatusGold    = CinematicTheme.GoldBright;
    static readonly Color AccentGreen   = CinematicTheme.GoldBase;

    public struct SlotRefs
    {
        public RectTransform root;
        public Image posterImage;
        public TextMeshProUGUI rarityIconText;
        public Image rarityFrame;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI timeText;
        public TextMeshProUGUI rewardText;
        public Slider progressBar;
        public SmoothProgressBar smoothBar;
        public RectTransform progressRow;
        public RectTransform metaRow;
        public RectTransform bodyRow;
        public Button discoverButton;
        public TextMeshProUGUI discoverLabel;
        public CanvasGroup discoverGroup;
    }

    public static SlotRefs Create(Transform parent)
    {
        var refs = new SlotRefs();

        var go = new GameObject("ProdSlot", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        HudSkinProvider.ApplyCard(go.GetComponent<Image>(), HudCardVariant.Primary);
        CinematicTheme.ApplyPremiumMaterial(go.GetComponent<RectTransform>());

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = SlotHeight;
        le.minHeight = SlotHeight;

        var rootVLG = go.AddComponent<VerticalLayoutGroup>();
        rootVLG.padding = new RectOffset(0, 0, 8, 6);
        rootVLG.spacing = 6;
        rootVLG.childControlWidth = rootVLG.childControlHeight = true;
        rootVLG.childForceExpandWidth = true;
        rootVLG.childForceExpandHeight = false;

        // HeaderRow — icon + info only (no progress here)
        refs.bodyRow = CreatePanel(go.transform, "HeaderRow", Color.clear);
        refs.bodyRow.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        var headerHLG = refs.bodyRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerHLG.padding = new RectOffset(10, 10, 0, 0);
        headerHLG.spacing = 10;
        headerHLG.childControlWidth = headerHLG.childControlHeight = true;
        headerHLG.childForceExpandWidth = false;
        headerHLG.childForceExpandHeight = true;
        headerHLG.childAlignment = TextAnchor.UpperLeft;

        var iconWrap = CreatePanel(refs.bodyRow, "RarityIconWrap", Color.clear);
        var iconLE = iconWrap.gameObject.AddComponent<LayoutElement>();
        iconLE.preferredWidth  = 70f;
        iconLE.minWidth        = 70f;
        iconLE.flexibleWidth   = 0f;
        iconLE.preferredHeight = 70f;
        iconLE.minHeight       = 70f;
        iconLE.flexibleHeight  = 0f;
        refs.rarityFrame = iconWrap.GetComponent<Image>();
        refs.rarityFrame.color = Color.clear;
        refs.posterImage = null;
        refs.rarityIconText = RuntimeTmpText.Create(iconWrap.transform, string.Empty, 32f, TextPrimary,
            FontStyles.Normal, TextAlignmentOptions.Center, "RarityIcon");
        refs.rarityIconText.raycastTarget = false;
        refs.rarityIconText.gameObject.SetActive(false);
        Stretch(refs.rarityIconText.rectTransform);
        iconWrap.gameObject.SetActive(false);

        var content = CreatePanel(refs.bodyRow, "InfoColumn", Color.clear);
        content.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var topRow = CreatePanel(content, "TopRow", Color.clear);
        LE(topRow, 28f);
        var topHLG = topRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        topHLG.spacing = 6;
        topHLG.childControlWidth = topHLG.childControlHeight = true;
        topHLG.childForceExpandWidth = topHLG.childForceExpandHeight = true;

        refs.titleText = CreateLabel(topRow.transform, "Title", string.Empty, 18f, TextPrimary, 44f);
        MovieOfferCardLayoutBuilder.ApplyTitleTypography(refs.titleText, TextAlignmentOptions.MidlineLeft);
        refs.titleText.enableAutoSizing = true;
        refs.titleText.fontSizeMin = 14f * RuntimeTmpText.MobileScale;
        refs.titleText.fontSizeMax = 18f * RuntimeTmpText.MobileScale;
        refs.titleText.textWrappingMode = TextWrappingModes.NoWrap;
        refs.titleText.overflowMode = TextOverflowModes.Ellipsis;
        refs.titleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        refs.statusText = CreateLabel(topRow.transform, "Status", string.Empty, 14f, StatusGreen, 22f);
        refs.statusText.fontStyle = FontStyles.Bold;
        refs.statusText.alignment = TextAlignmentOptions.MidlineRight;
        refs.statusText.gameObject.AddComponent<LayoutElement>().preferredWidth = 120f;

        var discoverGo = new GameObject("DiscoverBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
        discoverGo.transform.SetParent(content, false);
        HudSkinProvider.ApplyButton(discoverGo.GetComponent<Image>(), HudButtonVariant.Success);
        discoverGo.AddComponent<UIButtonScale>();
        LE(discoverGo.GetComponent<RectTransform>(), 42f);
        refs.discoverGroup = discoverGo.GetComponent<CanvasGroup>();
        refs.discoverButton = discoverGo.GetComponent<Button>();
        refs.discoverLabel = RuntimeTmpText.Create(discoverGo.transform, Loc.Get(LocKeys.ProdDiscoverButton),
            15f, TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center, "Label");
        refs.discoverLabel.raycastTarget = false;
        Stretch(refs.discoverLabel.rectTransform);
        discoverGo.SetActive(false);

        refs.metaRow = CreatePanel(content, "MetaRow", Color.clear);
        LE(refs.metaRow, 22f);
        var metaHLG = refs.metaRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        metaHLG.spacing = 8;
        metaHLG.childControlWidth = metaHLG.childControlHeight = true;
        metaHLG.childForceExpandWidth = true;
        metaHLG.childForceExpandHeight = true;

        refs.timeText = CreateLabel(refs.metaRow.transform, "Time", string.Empty, 16f, TextSecondary, 36f);
        refs.timeText.alignment = TextAlignmentOptions.MidlineLeft;
        refs.timeText.enableAutoSizing = true;
        refs.timeText.fontSizeMin = 13f * RuntimeTmpText.MobileScale;
        refs.timeText.fontSizeMax = 16f * RuntimeTmpText.MobileScale;
        refs.timeText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        refs.rewardText = CreateLabel(refs.metaRow.transform, "Reward", string.Empty, 16f, AccentGreen, 36f);
        refs.rewardText.alignment = TextAlignmentOptions.MidlineRight;
        refs.rewardText.enableAutoSizing = true;
        refs.rewardText.fontSizeMin = 12f * RuntimeTmpText.MobileScale;
        refs.rewardText.fontSizeMax = 16f * RuntimeTmpText.MobileScale;
        refs.rewardText.textWrappingMode = TextWrappingModes.NoWrap;
        refs.rewardText.overflowMode = TextOverflowModes.Ellipsis;
        refs.rewardText.gameObject.AddComponent<LayoutElement>().preferredWidth = 200f;

        // ProgressContainer — full slot width (~92%), independent of header columns
        refs.progressRow = CreatePanel(go.transform, "ProgressContainer", Color.clear);
        var pcLE = refs.progressRow.gameObject.AddComponent<LayoutElement>();
        pcLE.preferredHeight = 8f;
        pcLE.flexibleWidth = 1f;
        pcLE.minWidth = 0f;

        var pcPad = refs.progressRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        pcPad.padding = new RectOffset(8, 8, 0, 0);
        pcPad.childControlWidth = pcPad.childControlHeight = true;
        pcPad.childForceExpandWidth = true;
        pcPad.childForceExpandHeight = true;

        var barGo = new GameObject("ProgressBar", typeof(RectTransform), typeof(Image), typeof(Slider));
        barGo.transform.SetParent(refs.progressRow, false);
        barGo.GetComponent<Image>().color = BarBg;
        LE(barGo.GetComponent<RectTransform>(), 4f);
        refs.progressBar = barGo.GetComponent<Slider>();
        SetupSlider(refs.progressBar);
        ReadOnlySlider.Configure(refs.progressBar);
        refs.smoothBar = barGo.AddComponent<SmoothProgressBar>();

        refs.root = go.GetComponent<RectTransform>();
        return refs;
    }

    public static void SetDiscoverPulse(SlotRefs refs, bool active)
    {
        if (refs.discoverButton == null) return;
        var rt = refs.discoverButton.transform as RectTransform;
        if (active) UIAnimationService.PlayDiscoverPulse(rt);
        else UIAnimationService.StopDiscoverPulse(rt);
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

    static void SetupSlider(Slider slider)
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.direction = Slider.Direction.LeftToRight;

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(slider.transform, false);
        Stretch(fillArea.GetComponent<RectTransform>());

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        fill.GetComponent<Image>().color = BarFill;
        Stretch(fill.GetComponent<RectTransform>());
        slider.fillRect = fill.GetComponent<RectTransform>();
    }

    static void LE(RectTransform rt, float preferredHeight, float preferredWidth = -1f)
    {
        var le = rt.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = preferredHeight;
        if (preferredWidth > 0f)
        {
            le.preferredWidth = preferredWidth;
            le.minWidth = preferredWidth;
        }
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
