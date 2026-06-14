using TMPro;

using UnityEngine;

using UnityEngine.UI;



/// <summary>Builds premium department list cards — Phase 10.0 full-width rows.</summary>

public static class DepartmentMiniCardLayoutBuilder

{

    public const string LayoutMarkerName = "DeptCardLayout_v6";

    public const float CardHeight = HudLayoutConstants.StudioDeptCardHeight;

    public const float UpgradeBtnWidth = 170f;

    public const float UpgradeBtnHeight = 44f;



    static readonly Color TextPrimary  = CinematicTheme.TextPrimary;

    static readonly Color TextSecondary = CinematicTheme.TextSecondary;

    static readonly Color AccentGreen  = CinematicTheme.GoldBright;

    static readonly Color AccentBlue   = CinematicTheme.TextSecondary;

    static readonly Color BarBg        = CinematicTheme.DeepBg;

    static readonly Color BarFill      = CinematicTheme.ProgressFill;



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



    public static bool HasPremiumLayout(Transform root)

    {

        if (root.Find(LayoutMarkerName) != null) return true;



        foreach (var legacyName in new[] { "DeptCardLayout_v5", "DeptCardLayout_v4", "DeptCardLayout_v3" })

        {

            var legacy = root.Find(legacyName);

            if (legacy == null) continue;

            if (Application.isPlaying) Object.Destroy(legacy.gameObject);

            else Object.DestroyImmediate(legacy.gameObject);

        }



        return false;

    }



    public static WireResult EnsurePremiumLayout(DepartmentMiniCardUI card)

    {

        if (card == null) return default;



        if (HasPremiumLayout(card.transform))

            return WireExisting(card.transform);



        ClearChildren(card.transform);

        return Build(card.transform as RectTransform, card.deptType);

    }



    [System.Obsolete("Use EnsurePremiumLayout")]

    public static WireResult Ensure(DepartmentMiniCardUI card) => EnsurePremiumLayout(card);



    [System.Obsolete("Badge color ignored since Phase 11.5")]

    public static WireResult Ensure(DepartmentMiniCardUI card, Color legacyBadgeColor) => EnsurePremiumLayout(card);



    public static WireResult Build(RectTransform card, DepartmentType deptType)

    {

        var result = new WireResult();

        var theme = DepartmentThemeCatalog.Get(deptType);



        var cardLE = card.GetComponent<LayoutElement>() ?? card.gameObject.AddComponent<LayoutElement>();

        cardLE.preferredHeight = CardHeight;

        cardLE.minHeight       = CardHeight;

        cardLE.flexibleWidth = 1f;

        cardLE.preferredWidth = -1f;



        NormalizeParentRowHeight(card);



        HudSkinProvider.ApplyCard(card.GetComponent<Image>(), HudCardVariant.Primary);

        CinematicTheme.ApplyPremiumMaterial(card);



        var marker = new GameObject(LayoutMarkerName, typeof(RectTransform));

        marker.transform.SetParent(card, false);

        Stretch(marker.GetComponent<RectTransform>());



        var content = CreatePanel(card, "Content", Color.clear);

        Stretch(content);

        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();

        vlg.padding = new RectOffset(12, 10, 8, 8);

        vlg.spacing = 4;

        vlg.childAlignment = TextAnchor.UpperLeft;

        vlg.childControlWidth = vlg.childControlHeight = true;

        vlg.childForceExpandWidth = true;

        vlg.childForceExpandHeight = false;



        var mainRow = CreatePanel(content.transform, "CardMainRow", Color.clear);

        LE(mainRow, CardHeight - 28f, flexibleHeight: 0f);

        var mainHLG = mainRow.gameObject.AddComponent<HorizontalLayoutGroup>();

        mainHLG.padding = new RectOffset(0, 0, 0, 0);

        mainHLG.spacing = 10;

        mainHLG.childAlignment = TextAnchor.MiddleLeft;

        mainHLG.childControlWidth = mainHLG.childControlHeight = true;

        mainHLG.childForceExpandWidth = false;

        mainHLG.childForceExpandHeight = false;



        var iconCol = CreatePanel(mainRow.transform, "IconColumn", Color.clear);

        LE(iconCol, 64f, 64f);



        var badge = CreatePanel(iconCol.transform, "Badge", Color.clear);

        Stretch(badge);

        result.categoryBadge = badge.GetComponent<Image>();



        var iconImgGo = new GameObject("IconImage", typeof(RectTransform), typeof(Image));

        iconImgGo.transform.SetParent(badge.transform, false);

        var iconImg = iconImgGo.GetComponent<Image>();

        iconImg.color = Color.clear;

        iconImg.preserveAspect = true;

        iconImg.raycastTarget = false;

        Stretch(iconImgGo.GetComponent<RectTransform>());



        var badgeMotif = RuntimeTmpText.Create(badge.transform, theme.motifSymbol, 28f, theme.tint,

            FontStyles.Bold, TextAlignmentOptions.Center, "BadgeMotif");

        badgeMotif.raycastTarget = false;

        Stretch(badgeMotif.rectTransform);



        var infoCol = CreatePanel(mainRow.transform, "InfoColumn", Color.clear);

        var infoLE = infoCol.gameObject.AddComponent<LayoutElement>();

        infoLE.flexibleWidth = 1f;

        infoLE.minWidth = 0f;

        var infoVLG = infoCol.gameObject.AddComponent<VerticalLayoutGroup>();

        infoVLG.spacing = 2;

        infoVLG.childAlignment = TextAnchor.UpperLeft;

        infoVLG.childControlWidth = infoVLG.childControlHeight = true;

        infoVLG.childForceExpandWidth = true;

        infoVLG.childForceExpandHeight = false;



        result.deptNameText = CreateLabel(infoCol.transform, "DeptName", string.Empty, 20f, TextPrimary,

            FontStyles.Bold, 24f);

        result.deptNameText.alignment = TextAlignmentOptions.MidlineLeft;

        result.deptNameText.textWrappingMode = TextWrappingModes.NoWrap;

        result.deptNameText.overflowMode = TextOverflowModes.Ellipsis;



        result.levelText = CreateLabel(infoCol.transform, "LevelText", string.Empty, 19f, AccentGreen,

            FontStyles.Bold, 22f);

        result.levelText.alignment = TextAlignmentOptions.MidlineLeft;



        result.upgradeCostText = CreateLabel(infoCol.transform, "UpgradeCost", string.Empty, 18f, CinematicTheme.GoldBase,

            FontStyles.Bold, 22f);

        result.upgradeCostText.alignment = TextAlignmentOptions.MidlineLeft;



        result.effectText = CreateLabel(infoCol.transform, "EffectText", string.Empty, 13f, AccentBlue,

            FontStyles.Normal, 18f);

        result.effectText.alignment = TextAlignmentOptions.MidlineLeft;

        result.effectText.textWrappingMode = TextWrappingModes.Normal;

        result.effectText.maxVisibleLines = 1;



        var btnGo = new GameObject("UpgradeBtn", typeof(RectTransform), typeof(Image), typeof(Button));

        btnGo.transform.SetParent(mainRow.transform, false);

        EnsureUpgradeButtonLayout(btnGo.GetComponent<RectTransform>());

        HudSkinProvider.ApplyButton(btnGo.GetComponent<Image>(), HudButtonVariant.Success);

        btnGo.AddComponent<UIButtonScale>();

        result.upgradeButton = btnGo.GetComponent<Button>();



        result.upgradeLabelText = CreateLabel(btnGo.transform, "UpgradeLabel", string.Empty, 14f, TextPrimary,

            FontStyles.Bold, UpgradeBtnHeight);

        result.upgradeLabelText.alignment = TextAlignmentOptions.Center;



        result.levelProgressBar = CreateProgressBar(content.transform, "LevelProgressBar");

        LE(result.levelProgressBar.GetComponent<RectTransform>(), 10f);



        var themeVisual = card.gameObject.GetComponent<DepartmentThemeVisual>();

        if (themeVisual == null)

            themeVisual = card.gameObject.AddComponent<DepartmentThemeVisual>();

        themeVisual.Configure(deptType, null, badgeMotif, iconImg);

        result.themeVisual = themeVisual;



        return result;

    }



    public static void EnsureUpgradeButtonLayout(RectTransform btnRt)

    {

        if (btnRt == null) return;



        var le = btnRt.GetComponent<LayoutElement>() ?? btnRt.gameObject.AddComponent<LayoutElement>();

        le.preferredWidth = UpgradeBtnWidth;

        le.minWidth = UpgradeBtnWidth;

        le.flexibleWidth = 0f;

        le.preferredHeight = UpgradeBtnHeight;

        le.minHeight = UpgradeBtnHeight;

        le.flexibleHeight = 0f;

    }



    /// <summary>Phase 12.4C — mandatory layout audit for missing MEJORAR buttons.</summary>

    public static void LogLayoutAudit(DepartmentMiniCardUI card)

    {

        if (card == null) return;



        var btn = card.upgradeButton;

        var btnRt = btn != null ? btn.transform as RectTransform : null;

        var cg = card.GetComponent<CanvasGroup>();

        var parentLayout = btn != null && btn.transform.parent != null

            ? btn.transform.parent.GetComponent<LayoutGroup>()?.GetType().Name ?? "none"

            : "none";



        string cardName = card.deptNameText != null && !string.IsNullOrEmpty(card.deptNameText.text)

            ? card.deptNameText.text

            : DepartmentLoc.GetName(card.deptType);



        Debug.Log(

            $"[DeptCardAudit] Card Name={cardName} | " +

            $"Button Active={(btn != null && btn.gameObject.activeSelf)} | " +

            $"Button Width={(btnRt != null ? btnRt.rect.width : 0f):F0} | " +

            $"Button Height={(btnRt != null ? btnRt.rect.height : 0f):F0} | " +

            $"Anchored Position={(btnRt != null ? btnRt.anchoredPosition.ToString() : "null")} | " +

            $"CanvasGroup Alpha={(cg != null ? cg.alpha : 1f):F2} | " +

            $"Parent Layout={parentLayout}");

    }



    /// <summary>Legacy hook — accent strips removed in Phase 11.5.</summary>

    public static void ApplyAccentStrip(RectTransform card, Color color)

    {

        CinematicTheme.RemoveAccentStrip(card);

    }



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

            categoryBadge = card.Find("Content/CardMainRow/IconColumn/Badge")?.GetComponent<Image>()

                         ?? card.Find("Content/CardMainRow/InfoColumn/HeaderRow/Badge")?.GetComponent<Image>()

                         ?? card.Find("Content/HeaderRow/Badge")?.GetComponent<Image>(),

            deptNameText = card.Find("Content/CardMainRow/InfoColumn/DeptName")?.GetComponent<TextMeshProUGUI>()

                        ?? card.Find("Content/CardMainRow/InfoColumn/HeaderRow/DeptName")?.GetComponent<TextMeshProUGUI>()

                        ?? card.Find("Content/HeaderRow/DeptName")?.GetComponent<TextMeshProUGUI>(),

            levelText = card.Find("Content/CardMainRow/InfoColumn/LevelText")?.GetComponent<TextMeshProUGUI>()

                     ?? card.Find("Content/LevelText")?.GetComponent<TextMeshProUGUI>(),

            effectText = card.Find("Content/CardMainRow/InfoColumn/EffectText")?.GetComponent<TextMeshProUGUI>()

                      ?? card.Find("Content/EffectText")?.GetComponent<TextMeshProUGUI>(),

            levelProgressBar = card.Find("Content/LevelProgressBar")?.GetComponent<Slider>(),

            upgradeButton = card.Find("Content/CardMainRow/UpgradeBtn")?.GetComponent<Button>()

                         ?? card.Find("Content/UpgradeBtn")?.GetComponent<Button>(),

            upgradeLabelText = card.Find("Content/CardMainRow/UpgradeBtn/UpgradeLabel")?.GetComponent<TextMeshProUGUI>()

                            ?? card.Find("Content/UpgradeBtn/UpgradeLabel")?.GetComponent<TextMeshProUGUI>(),

            upgradeCostText = card.Find("Content/CardMainRow/InfoColumn/UpgradeCost")?.GetComponent<TextMeshProUGUI>()

                           ?? card.Find("Content/UpgradeBtn/UpgradeCost")?.GetComponent<TextMeshProUGUI>(),

            themeBackdrop = card.Find("ThemeBackdrop")?.GetComponent<Image>(),

            themeVisual = card.GetComponent<DepartmentThemeVisual>(),

        };



        NormalizeParentRowHeight(card as RectTransform);

        ApplyCompactSizing(card as RectTransform);

        PatchExistingMainRow(card as RectTransform, result.upgradeButton);

        CinematicTheme.NeutralizeDepartmentCard(card as RectTransform);

        CinematicTheme.ApplyPremiumMaterial(card as RectTransform);

        return result;

    }



    static void PatchExistingMainRow(RectTransform card, Button upgradeButton)

    {

        if (card == null) return;



        var mainRow = card.Find("Content/CardMainRow") as RectTransform;

        if (mainRow == null) return;



        LE(mainRow, CardHeight - 28f, flexibleHeight: 0f);



        var mainHLG = mainRow.GetComponent<HorizontalLayoutGroup>();

        if (mainHLG != null)

            mainHLG.childForceExpandHeight = false;



        var infoCol = mainRow.Find("InfoColumn");

        if (infoCol != null)

        {

            var infoLE = infoCol.GetComponent<LayoutElement>() ?? infoCol.gameObject.AddComponent<LayoutElement>();

            infoLE.flexibleWidth = 1f;

            infoLE.minWidth = 0f;

        }



        if (upgradeButton != null)

            EnsureUpgradeButtonLayout(upgradeButton.transform as RectTransform);

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

