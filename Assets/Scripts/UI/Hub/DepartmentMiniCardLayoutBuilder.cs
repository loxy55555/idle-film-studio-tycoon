using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Builds premium department list cards — Phase 10.0 full-width rows.</summary>
public static class DepartmentMiniCardLayoutBuilder
{
    public const string LayoutMarkerName = "DeptCardLayout_v3";
    public const float CardHeight = HudLayoutConstants.StudioDeptCardHeight;

    static readonly Color TextPrimary = Color.white;
    static readonly Color TextSecondary = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color AccentGreen = new Color(0.18f, 0.80f, 0.44f);
    static readonly Color AccentBlue = new Color(0.35f, 0.70f, 0.95f);
    static readonly Color BarBg = new Color(0.09f, 0.09f, 0.18f);
    static readonly Color BarFill = new Color(0.15f, 0.68f, 0.38f);

    public struct WireResult
    {
        public Image categoryBadge;
        public TextMeshProUGUI deptNameText;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI effectText;
        public Slider levelProgressBar;
        public Button upgradeButton;
        public TextMeshProUGUI upgradeLabelText;
        public TextMeshProUGUI upgradeCostText;
        public Image themeBackdrop;
        public DepartmentThemeVisual themeVisual;
    }

    public static bool HasPremiumLayout(Transform root) => root.Find(LayoutMarkerName) != null;

    public static WireResult Ensure(DepartmentMiniCardUI card, Color badgeColor)
    {
        if (card == null) return default;

        if (HasPremiumLayout(card.transform))
            return WireExisting(card.transform);

        ClearChildren(card.transform);
        return Build(card.transform as RectTransform, badgeColor);
    }

    public static WireResult Build(RectTransform card, Color badgeColor)
    {
        var result = new WireResult();

        var cardLE = card.GetComponent<LayoutElement>() ?? card.gameObject.AddComponent<LayoutElement>();
        cardLE.preferredHeight = CardHeight;
        cardLE.minHeight = CardHeight;
        cardLE.flexibleWidth = 1f;
        cardLE.preferredWidth = -1f;

        NormalizeParentRowHeight(card);

        HudSkinProvider.ApplyCard(card.GetComponent<Image>(), HudCardVariant.Primary);

        // Left accent strip — 5 px colored bar using the badge/dept color (Phase 9.2)
        ApplyAccentStrip(card, badgeColor);

        var marker = new GameObject(LayoutMarkerName, typeof(RectTransform));
        marker.transform.SetParent(card, false);
        Stretch(marker.GetComponent<RectTransform>());

        var themeRoot = CreatePanel(card, "ThemeBackdrop", Color.clear);
        Stretch(themeRoot);
        result.themeBackdrop = themeRoot.GetComponent<Image>();
        result.themeBackdrop.raycastTarget = false;

        var motif = RuntimeTmpText.Create(themeRoot.transform, "•", 28f, Color.white,
            FontStyles.Normal, TextAlignmentOptions.Center, "ThemeMotif");
        Stretch(motif.rectTransform);
        motif.raycastTarget = false;
        motif.alpha = 0.35f;

        // Content VLG — left padding 13 (5 strip + 8 gap) to avoid overlapping the accent strip
        var content = CreatePanel(card, "Content", Color.clear);
        Stretch(content);
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 10, 8, 8);
        vlg.spacing = 4;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var headerRow = CreatePanel(content.transform, "HeaderRow", Color.clear);
        LE(headerRow, 26f);
        var hdrHLG = headerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        hdrHLG.spacing = 8;
        hdrHLG.childAlignment = TextAnchor.MiddleLeft;
        hdrHLG.childControlWidth = hdrHLG.childControlHeight = true;
        hdrHLG.childForceExpandWidth = false;
        hdrHLG.childForceExpandHeight = false;

        var badge = CreatePanel(headerRow.transform, "Badge", badgeColor);
        LE(badge, 22f, 22f);
        result.categoryBadge = badge.GetComponent<Image>();

        result.deptNameText = CreateLabel(headerRow.transform, "DeptName", string.Empty, 17f, TextPrimary,
            FontStyles.Bold, 26f);
        result.deptNameText.alignment = TextAlignmentOptions.MidlineLeft;
        result.deptNameText.textWrappingMode = TextWrappingModes.NoWrap;
        result.deptNameText.overflowMode = TextOverflowModes.Ellipsis;
        result.deptNameText.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1f;

        result.levelText = CreateLabel(content.transform, "LevelText", string.Empty, 16f, AccentGreen,
            FontStyles.Bold, 20f);
        result.levelText.alignment = TextAlignmentOptions.MidlineLeft;

        result.effectText = CreateLabel(content.transform, "EffectText", string.Empty, 12f, AccentBlue,
            FontStyles.Normal, 18f);
        result.effectText.alignment = TextAlignmentOptions.MidlineLeft;
        result.effectText.textWrappingMode = TextWrappingModes.Normal;
        result.effectText.maxVisibleLines = 2;

        result.levelProgressBar = CreateProgressBar(content.transform, "LevelProgressBar");
        LE(result.levelProgressBar.GetComponent<RectTransform>(), 8f);

        var btnGo = new GameObject("UpgradeBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(VerticalLayoutGroup));
        btnGo.transform.SetParent(content.transform, false);
        LE(btnGo.GetComponent<RectTransform>(), 34f);
        HudSkinProvider.ApplyButton(btnGo.GetComponent<Image>(), HudButtonVariant.Success);
        btnGo.AddComponent<UIButtonScale>();
        result.upgradeButton = btnGo.GetComponent<Button>();

        var btnVLG = btnGo.GetComponent<VerticalLayoutGroup>();
        btnVLG.padding = new RectOffset(4, 4, 2, 2);
        btnVLG.spacing = 0;
        btnVLG.childAlignment = TextAnchor.MiddleCenter;
        btnVLG.childControlWidth = btnVLG.childControlHeight = true;
        btnVLG.childForceExpandWidth = btnVLG.childForceExpandHeight = true;

        result.upgradeLabelText = CreateLabel(btnGo.transform, "UpgradeLabel", string.Empty, 12f, TextPrimary,
            FontStyles.Bold, 16f);
        result.upgradeCostText = CreateLabel(btnGo.transform, "UpgradeCost", string.Empty, 11f, TextPrimary,
            FontStyles.Bold, 14f);

        var themeVisual = card.gameObject.GetComponent<DepartmentThemeVisual>();
        if (themeVisual == null)
            themeVisual = card.gameObject.AddComponent<DepartmentThemeVisual>();
        themeVisual.Configure(default, result.themeBackdrop, motif, null);
        result.themeVisual = themeVisual;

        return result;
    }

    /// <summary>
    /// Adds or updates a 5 px colored left accent strip to an existing (baked) card.
    /// Called from the bootstrap patch so baked cards also receive the visual treatment.
    /// </summary>
    public static void ApplyAccentStrip(RectTransform card, Color color)
    {
        if (card == null) return;
        const string StripName = "AccentStrip";
        var existing = card.Find(StripName) as RectTransform;
        if (existing != null)
        {
            var existImg = existing.GetComponent<Image>();
            if (existImg != null) existImg.color = color;
            return;
        }

        var strip = new GameObject(StripName, typeof(RectTransform), typeof(Image));
        strip.transform.SetParent(card, false);
        strip.transform.SetAsLastSibling();

        var rt = strip.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(5f, 0f);
        rt.anchoredPosition = Vector2.zero;

        strip.GetComponent<Image>().color = color;
        strip.GetComponent<Image>().raycastTarget = false;
    }

    /// <summary>
    /// Baked scenes carry rows sized for the old taller cards — shrink the row so the
    /// freed space goes back to the decorative stage (Phase 8.6A).
    /// </summary>
    public static void NormalizeParentRowHeight(RectTransform card)
    {
        var parent = card.parent as RectTransform;
        if (parent == null) return;

        var rowLE = parent.GetComponent<LayoutElement>();
        if (rowLE != null && rowLE.preferredHeight > CardHeight)
        {
            rowLE.preferredHeight = CardHeight;
            if (rowLE.minHeight > CardHeight) rowLE.minHeight = CardHeight;
        }
    }

    public static WireResult WireExisting(Transform card)
    {
        var result = new WireResult
        {
            categoryBadge = card.Find("Content/HeaderRow/Badge")?.GetComponent<Image>(),
            deptNameText = card.Find("Content/HeaderRow/DeptName")?.GetComponent<TextMeshProUGUI>(),
            levelText = card.Find("Content/LevelText")?.GetComponent<TextMeshProUGUI>(),
            effectText = card.Find("Content/EffectText")?.GetComponent<TextMeshProUGUI>(),
            levelProgressBar = card.Find("Content/LevelProgressBar")?.GetComponent<Slider>(),
            upgradeButton = card.Find("Content/UpgradeBtn")?.GetComponent<Button>(),
            upgradeLabelText = card.Find("Content/UpgradeBtn/UpgradeLabel")?.GetComponent<TextMeshProUGUI>(),
            upgradeCostText = card.Find("Content/UpgradeBtn/UpgradeCost")?.GetComponent<TextMeshProUGUI>(),
            themeBackdrop = card.Find("ThemeBackdrop")?.GetComponent<Image>(),
            themeVisual = card.GetComponent<DepartmentThemeVisual>(),
        };

        NormalizeParentRowHeight(card as RectTransform);
        ApplyCompactSizing(card as RectTransform);
        return result;
    }

    static void ApplyCompactSizing(RectTransform card)
    {
        if (card == null) return;

        var cardLE = card.GetComponent<LayoutElement>();
        if (cardLE != null)
        {
            cardLE.preferredHeight = CardHeight;
            cardLE.minHeight = CardHeight;
            cardLE.flexibleWidth = 1f;
            cardLE.preferredWidth = -1f;
        }
    }

    static Slider CreateProgressBar(Transform parent, string name)
    {
        var barGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Slider));
        barGo.transform.SetParent(parent, false);
        barGo.GetComponent<Image>().color = BarBg;

        var slider = barGo.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.interactable = false;

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(barGo.transform, false);
        Stretch(fillArea.GetComponent<RectTransform>());

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        fill.GetComponent<Image>().color = BarFill;
        Stretch(fill.GetComponent<RectTransform>());
        slider.fillRect = fill.GetComponent<RectTransform>();
        ReadOnlySlider.Configure(slider);
        return slider;
    }

    static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, float size, Color color,
        FontStyles style, float preferredHeight)
    {
        var tmp = RuntimeTmpText.Create(parent, text, size, color, style, TextAlignmentOptions.Center, name);
        tmp.raycastTarget = false;
        LE(tmp.rectTransform, preferredHeight);
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

    static void LE(RectTransform rt, float preferredHeight, float preferredWidth = -1f, float flexibleHeight = 0f)
    {
        var le = rt.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = preferredHeight;
        if (preferredWidth > 0f)
        {
            le.preferredWidth = preferredWidth;
            le.minWidth = preferredWidth;
        }
        le.flexibleHeight = flexibleHeight;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            var child = root.GetChild(i);
            if (Application.isPlaying)
                Object.Destroy(child.gameObject);
            else
                Object.DestroyImmediate(child.gameObject);
        }
    }
}
