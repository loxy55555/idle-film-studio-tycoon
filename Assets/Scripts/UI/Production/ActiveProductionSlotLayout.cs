using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Premium active production slot layout (Phase 9.0 / 10.3 discovery).</summary>
public static class ActiveProductionSlotLayout
{
    public const float SlotHeight = 132f;

    static readonly Color TextPrimary   = Color.white;
    static readonly Color TextSecondary = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color BarBg         = new Color(0.09f, 0.09f, 0.18f);
    static readonly Color BarFill       = new Color(0.15f, 0.68f, 0.38f);
    static readonly Color StatusGreen   = new Color(0.18f, 0.80f, 0.44f);
    static readonly Color StatusGold    = new Color(0.95f, 0.77f, 0.06f);
    static readonly Color AccentGreen   = new Color(0.18f, 0.80f, 0.44f);
    static readonly Color DiscoverBg    = new Color(0.12f, 0.38f, 0.24f);

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

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = SlotHeight;
        le.minHeight = SlotHeight;

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(10, 10, 10, 10);
        hlg.spacing = 12;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        var iconWrap = CreatePanel(go.transform, "RarityIconWrap", BarBg);
        LE(iconWrap, 108f, 56f);
        refs.rarityFrame = iconWrap.GetComponent<Image>();
        refs.posterImage = null;
        refs.rarityIconText = RuntimeTmpText.Create(iconWrap.transform, "🎬", 28f, TextPrimary,
            FontStyles.Normal, TextAlignmentOptions.Center, "RarityIcon");
        refs.rarityIconText.raycastTarget = false;
        Stretch(refs.rarityIconText.rectTransform);

        var content = CreatePanel(go.transform, "Content", Color.clear);
        content.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var topRow = CreatePanel(content, "TopRow", Color.clear);
        LE(topRow, 22f);
        var topHLG = topRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        topHLG.spacing = 6;
        topHLG.childControlWidth = topHLG.childControlHeight = true;
        topHLG.childForceExpandWidth = topHLG.childForceExpandHeight = true;

        refs.titleText = CreateLabel(topRow.transform, "Title", string.Empty, 14f, TextPrimary, 22f);
        MovieOfferCardLayoutBuilder.ApplyTitleTypography(refs.titleText, TextAlignmentOptions.MidlineLeft);
        refs.titleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        refs.statusText = CreateLabel(topRow.transform, "Status", string.Empty, 10f, StatusGreen, 18f);
        refs.statusText.fontStyle = FontStyles.Bold;
        refs.statusText.alignment = TextAlignmentOptions.MidlineRight;
        refs.statusText.gameObject.AddComponent<LayoutElement>().preferredWidth = 120f;

        refs.progressRow = CreatePanel(content, "ProgressRow", Color.clear);
        LE(refs.progressRow, 14f);
        var barGo = new GameObject("ProgressBar", typeof(RectTransform), typeof(Image), typeof(Slider));
        barGo.transform.SetParent(refs.progressRow, false);
        barGo.GetComponent<Image>().color = BarBg;
        LE(barGo.GetComponent<RectTransform>(), 14f);
        refs.progressBar = barGo.GetComponent<Slider>();
        SetupSlider(refs.progressBar);
        ReadOnlySlider.Configure(refs.progressBar);
        refs.smoothBar = barGo.AddComponent<SmoothProgressBar>();

        var discoverGo = new GameObject("DiscoverBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
        discoverGo.transform.SetParent(content, false);
        discoverGo.GetComponent<Image>().color = DiscoverBg;
        LE(discoverGo.GetComponent<RectTransform>(), 36f);
        refs.discoverGroup = discoverGo.GetComponent<CanvasGroup>();
        refs.discoverButton = discoverGo.GetComponent<Button>();
        refs.discoverLabel = RuntimeTmpText.Create(discoverGo.transform, Loc.Get(LocKeys.ProdDiscoverButton),
            12f, TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center, "Label");
        refs.discoverLabel.raycastTarget = false;
        Stretch(refs.discoverLabel.rectTransform);
        discoverGo.SetActive(false);

        refs.metaRow = CreatePanel(content, "MetaRow", Color.clear);
        LE(refs.metaRow, 18f);
        var metaHLG = refs.metaRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        metaHLG.spacing = 8;
        metaHLG.childControlWidth = metaHLG.childControlHeight = true;
        metaHLG.childForceExpandWidth = true;
        metaHLG.childForceExpandHeight = true;

        refs.timeText = CreateLabel(refs.metaRow.transform, "Time", string.Empty, 11f, TextSecondary, 18f);
        refs.timeText.alignment = TextAlignmentOptions.MidlineLeft;
        refs.timeText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        refs.rewardText = CreateLabel(refs.metaRow.transform, "Reward", string.Empty, 11f, AccentGreen, 18f);
        refs.rewardText.alignment = TextAlignmentOptions.MidlineRight;
        refs.rewardText.gameObject.AddComponent<LayoutElement>().preferredWidth = 160f;

        refs.root = go.GetComponent<RectTransform>();
        return refs;
    }

    public static void SetDiscoverPulse(SlotRefs refs, bool active)
    {
        if (refs.discoverButton == null) return;
        refs.discoverButton.transform.DOKill();
        if (!active) return;
        refs.discoverButton.transform.localScale = Vector3.one;
        refs.discoverButton.transform.DOScale(1.04f, 0.85f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
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
