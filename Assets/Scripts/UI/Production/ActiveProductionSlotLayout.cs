using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Premium active production slot layout (Phase 8.2).</summary>
public static class ActiveProductionSlotLayout
{
    public const float SlotHeight = 96f;

    static readonly Color TextPrimary   = Color.white;
    static readonly Color TextSecondary = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color BarBg         = new Color(0.09f, 0.09f, 0.18f);
    static readonly Color BarFill       = new Color(0.15f, 0.68f, 0.38f);
    static readonly Color StatusGreen   = new Color(0.18f, 0.80f, 0.44f);

    public struct SlotRefs
    {
        public RectTransform root;
        public Image posterImage;
        public Image rarityFrame;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI timeText;
        public Slider progressBar;
        public SmoothProgressBar smoothBar;
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
        hlg.padding = new RectOffset(8, 8, 8, 8);
        hlg.spacing = 10;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        var posterWrap = CreatePanel(go.transform, "PosterWrap", BarBg);
        LE(posterWrap, 72f, 52f);
        refs.rarityFrame = CreatePanel(posterWrap, "RarityFrame", Color.clear).GetComponent<Image>();
        Stretch(refs.rarityFrame.rectTransform);
        refs.posterImage = CreatePanel(posterWrap, "Poster", Color.clear).GetComponent<Image>();
        var posterRT = refs.posterImage.rectTransform;
        posterRT.anchorMin = new Vector2(0.06f, 0.06f);
        posterRT.anchorMax = new Vector2(0.94f, 0.94f);
        posterRT.offsetMin = posterRT.offsetMax = Vector2.zero;

        var content = CreatePanel(go.transform, "Content", Color.clear);
        content.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 3;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var topRow = CreatePanel(content, "TopRow", Color.clear);
        LE(topRow, MovieOfferCardLayoutBuilder.TitleRowHeight);
        var topHLG = topRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        topHLG.spacing = 6;
        topHLG.childControlWidth = topHLG.childControlHeight = true;
        topHLG.childForceExpandWidth = topHLG.childForceExpandHeight = true;

        refs.titleText = CreateLabel(topRow.transform, "Title", string.Empty, 13f, TextPrimary, MovieOfferCardLayoutBuilder.TitleRowHeight);
        MovieOfferCardLayoutBuilder.ApplyTitleTypography(refs.titleText, TextAlignmentOptions.MidlineLeft);
        refs.titleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        refs.statusText = CreateLabel(topRow.transform, "Status", string.Empty, 10f, StatusGreen, 16f);
        refs.statusText.fontStyle = FontStyles.Bold;
        refs.statusText.alignment = TextAlignmentOptions.MidlineRight;
        refs.statusText.gameObject.AddComponent<LayoutElement>().preferredWidth = 72f;

        var barGo = new GameObject("ProgressBar", typeof(RectTransform), typeof(Image), typeof(Slider));
        barGo.transform.SetParent(content, false);
        barGo.GetComponent<Image>().color = BarBg;
        LE(barGo.GetComponent<RectTransform>(), 10f);
        refs.progressBar = barGo.GetComponent<Slider>();
        SetupSlider(refs.progressBar);
        ReadOnlySlider.Configure(refs.progressBar);
        refs.smoothBar = barGo.AddComponent<SmoothProgressBar>();

        refs.timeText = CreateLabel(content, "Time", string.Empty, 11f, TextSecondary, 16f);
        refs.timeText.alignment = TextAlignmentOptions.MidlineLeft;

        refs.root = go.GetComponent<RectTransform>();
        return refs;
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
