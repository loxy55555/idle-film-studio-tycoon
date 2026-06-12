using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Builds premium department mini-card layout (Phase 8.6A: compact structured cards).</summary>
public static class DepartmentMiniCardLayoutBuilder
{
    public const string LayoutMarkerName = "DeptCardLayout_v3";
    public const float CardWidth  = 172f;
    public const float CardHeight = 140f; // Phase ESTUDIO-TUNING: compact card — recovers vertical space for the stage

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
        cardLE.preferredWidth = CardWidth;
        cardLE.preferredHeight = CardHeight;
        cardLE.minHeight = CardHeight;

        NormalizeParentRowHeight(card);

        HudSkinProvider.ApplyCard(card.GetComponent<Image>(), HudCardVariant.Primary);

        var marker = new GameObject(LayoutMarkerName, typeof(RectTransform));
        marker.transform.SetParent(card, false);
        Stretch(marker.GetComponent<RectTransform>());

        var themeRoot = CreatePanel(card, "ThemeBackdrop", Color.clear);
        Stretch(themeRoot);
        result.themeBackdrop = themeRoot.GetComponent<Image>();
        result.themeBackdrop.raycastTarget = false;

        var motif = RuntimeTmpText.Create(themeRoot.transform, "•", 44f, Color.white,
            FontStyles.Normal, TextAlignmentOptions.Center, "ThemeMotif");
        Stretch(motif.rectTransform);
        motif.raycastTarget = false;

        var content = CreatePanel(card, "Content", Color.clear);
        Stretch(content);
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 6;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Header row: badge + name side by side (was stacked — saves ~50px)
        var headerRow = CreatePanel(content.transform, "HeaderRow", Color.clear);
        LE(headerRow, 36f);
        var hdrHLG = headerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        hdrHLG.spacing = 8;
        hdrHLG.childAlignment = TextAnchor.MiddleLeft;
        hdrHLG.childControlWidth = hdrHLG.childControlHeight = true;
        hdrHLG.childForceExpandWidth = false;
        hdrHLG.childForceExpandHeight = false;

        var badge = CreatePanel(headerRow.transform, "Badge", badgeColor);
        LE(badge, 32f, 32f);
        result.categoryBadge = badge.GetComponent<Image>();

        result.deptNameText = CreateLabel(headerRow.transform, "DeptName", string.Empty, 13f, TextPrimary,
            FontStyles.Bold, 36f);
        result.deptNameText.alignment = TextAlignmentOptions.MidlineLeft;
        result.deptNameText.textWrappingMode = TextWrappingModes.Normal;
        result.deptNameText.enableAutoSizing = true;
        result.deptNameText.fontSizeMin = 10f;
        result.deptNameText.fontSizeMax = 14f;
        var nameLE = result.deptNameText.GetComponent<LayoutElement>();
        nameLE.flexibleWidth = 1f;

        result.levelText = CreateLabel(content.transform, "LevelText", string.Empty, 17f, AccentGreen,
            FontStyles.Bold, 22f);
        result.levelText.alignment = TextAlignmentOptions.MidlineLeft;
        result.levelText.enableAutoSizing = true;
        result.levelText.fontSizeMin = 13f;
        result.levelText.fontSizeMax = 18f;

        result.levelProgressBar = CreateProgressBar(content.transform, "LevelProgressBar");
        LE(result.levelProgressBar.GetComponent<RectTransform>(), 8f);

        result.effectText = CreateLabel(content.transform, "EffectText", string.Empty, 11f, AccentBlue,
            FontStyles.Normal, 32f);
        result.effectText.alignment = TextAlignmentOptions.TopLeft;
        result.effectText.textWrappingMode = TextWrappingModes.Normal;
        result.effectText.lineSpacing = -2f;
        result.effectText.enableAutoSizing = true;
        result.effectText.fontSizeMin = 9f;
        result.effectText.fontSizeMax = 12f;

        var spacer = CreatePanel(content.transform, "Spacer", Color.clear);
        LE(spacer, 0f, flexibleHeight: 1f);

        var btnGo = new GameObject("UpgradeBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(VerticalLayoutGroup));
        btnGo.transform.SetParent(content.transform, false);
        LE(btnGo.GetComponent<RectTransform>(), 46f);
        HudSkinProvider.ApplyButton(btnGo.GetComponent<Image>(), HudButtonVariant.Success);
        btnGo.AddComponent<UIButtonScale>();
        result.upgradeButton = btnGo.GetComponent<Button>();

        var btnVLG = btnGo.GetComponent<VerticalLayoutGroup>();
        btnVLG.padding = new RectOffset(6, 6, 4, 4);
        btnVLG.spacing = 0;
        btnVLG.childAlignment = TextAnchor.MiddleCenter;
        btnVLG.childControlWidth = btnVLG.childControlHeight = true;
        btnVLG.childForceExpandWidth = btnVLG.childForceExpandHeight = true;

        result.upgradeLabelText = CreateLabel(btnGo.transform, "UpgradeLabel", string.Empty, 13f, TextPrimary,
            FontStyles.Bold, 16f);
        result.upgradeLabelText.enableAutoSizing = true;
        result.upgradeLabelText.fontSizeMin = 10f;
        result.upgradeLabelText.fontSizeMax = 14f;

        result.upgradeCostText = CreateLabel(btnGo.transform, "UpgradeCost", string.Empty, 13f, TextPrimary,
            FontStyles.Bold, 16f);
        result.upgradeCostText.enableAutoSizing = true;
        result.upgradeCostText.fontSizeMin = 10f;
        result.upgradeCostText.fontSizeMax = 14f;

        var themeVisual = card.gameObject.GetComponent<DepartmentThemeVisual>();
        if (themeVisual == null)
            themeVisual = card.gameObject.AddComponent<DepartmentThemeVisual>();
        themeVisual.Configure(default, result.themeBackdrop, motif, null);
        result.themeVisual = themeVisual;

        return result;
    }

    /// <summary>
    /// Baked scenes carry rows sized for the old taller cards — shrink the row so the
    /// freed space goes back to the decorative stage (Phase 8.6A).
    /// </summary>
    static void NormalizeParentRowHeight(RectTransform card)
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
        return new WireResult
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
