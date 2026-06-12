#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menu: IdleFilm / Build Studio UI  (v2 — 5-tab navigation)
///
/// Layout  (1080 × 1920 reference):
///   TopBar          130 h   — money | income | rep | oscars | level | settings
///   MainContent     flex    — content switcher + bottom nav
///     EstudioPanel  flex    — studio scene · dept mini-bar · production · lower tabs
///     PeliculasPanel        — placeholder (movie history)
///     ColeccionPanel        — collection / sagas / legendaries
///     MenuPanel             — settings menu
///   BottomNav        100 h  — Producción | Estudio | Premios | Colección | Menú
/// </summary>
public static class StudioUIBuilder
{
    const string TARGET_SCENE = "Assets/Scenes/MainGame.unity";

    // ─── Palette ──────────────────────────────────────────────────────────────
    static readonly Color BG_DEEP      = Hex("#0E0E1E");
    static readonly Color BG_SECTION   = Hex("#141428");
    static readonly Color BG_CARD      = Hex("#1A1A30");
    static readonly Color BG_CARD2     = Hex("#1F1F3A");
    static readonly Color TOPBAR_BG    = Hex("#090915");
    static readonly Color ACCENT_GREEN = Hex("#2ECC71");
    static readonly Color ACCENT_GOLD  = Hex("#F1C40F");
    static readonly Color ACCENT_BLUE  = Hex("#3498DB");
    static readonly Color ACCENT_RED   = Hex("#E74C3C");
    static readonly Color ACCENT_PURPLE= Hex("#9B59B6");
    static readonly Color TEXT_PRI     = Color.white;
    static readonly Color TEXT_SEC     = Hex("#8A8AAA");
    static readonly Color TEXT_DIM     = Hex("#555577");
    static readonly Color BTN_GREEN    = Hex("#27AE60");
    static readonly Color BTN_DISABLED = Hex("#1E1E34");
    static readonly Color SEP          = Hex("#1C1C35");
    static readonly Color SCENE_BG     = Hex("#12122A");
    static readonly Color NAV_BG       = Hex("#08081A");
    static readonly Color NAV_ACTIVE   = Hex("#27AE60");
    static readonly Color SUBTAB_ACTIVE = Hex("#3498DB");

    // Genre colours for movie badges
    static readonly Color[] GENRE_COLORS =
    {
        Hex("#E74C3C"), // Action
        Hex("#3498DB"), // Drama
        Hex("#8E44AD"), // Horror
        Hex("#F39C12"), // Comedy
        Hex("#E91E8C"), // Romance
        Hex("#27AE60"), // SciFi
    };

    // ─── Sizes ────────────────────────────────────────────────────────────────
    const float TOPBAR_H       = 120f;
    const float STUDIO_SCENE_H = 280f;
    const float DEPT_BAR_H     = 190f;
    const float PROD_WIDGET_H  = 160f;
    const float LOWER_TAB_H    = 56f;
    const float BOTTOM_NAV_H   = 100f;
    const float SUB_TAB_H      = 48f;

    // ─── Entry point ──────────────────────────────────────────────────────────
    [MenuItem("IdleFilm/Build Studio UI")]
    public static void Build()
    {
        if (!File.Exists(TARGET_SCENE))
        {
            Debug.LogError($"[Builder] Scene not found: {TARGET_SCENE}");
            return;
        }

        var scene = EditorSceneManager.OpenScene(TARGET_SCENE, OpenSceneMode.Single);
        Debug.Log($"[Builder] Opened scene: {TARGET_SCENE}");

        Canvas canvas = FindCanvas();
        if (canvas == null) { Debug.LogError("[Builder] Canvas not found."); return; }
        NormalizeCanvas(canvas);

        Undo.SetCurrentGroupName("Build Studio UI v2");
        DestroyLegacySceneRoots();
        CleanupOrphanRoots();

        // Clean canvas
        var old = new List<GameObject>();
        for (int i = 0; i < canvas.transform.childCount; i++)
            old.Add(canvas.transform.GetChild(i).gameObject);
        foreach (var go in old) if (go != null) Undo.DestroyObjectImmediate(go);

        // Canvas scaler
        var cs = canvas.GetComponent<CanvasScaler>();
        if (cs != null)
        {
            cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight  = 0.5f;
        }

        // ── HubRoot (anchor shell — reliable full-screen layout) ──────────────
        var root = MakePanel(canvas.transform, "HubRoot", BG_DEEP);
        Stretch(root);
        root.gameObject.AddComponent<StudioUIShellLayout>();
        if (root.GetComponent<HudSkinBootstrap>() == null)
            root.gameObject.AddComponent<HudSkinBootstrap>();

        // ── TopBar ────────────────────────────────────────────────────────────
        var topBar = BuildTopBar(root);
        AnchorTopBar(topBar, TOPBAR_H);

        // ── Bottom Nav (built early so main content can fill between bars) ────
        var navButtons = new Button[5];
        var bottomNav  = BuildBottomNav(root, navButtons);
        AnchorBottomBar(bottomNav, BOTTOM_NAV_H);

        // ── MainContent fills space between top and bottom bars ───────────────
        var mainContent = MakePanel(root, "MainContent", BG_DEEP);
        AnchorBetweenBars(mainContent, TOPBAR_H, BOTTOM_NAV_H);

        // ContentSwitcher holds the 5 tab panels (only one visible at a time)
        var switcher = MakePanel(mainContent, "ContentSwitcher", BG_DEEP);
        Stretch(switcher);

        // ── 5 Main Tab Panels (definitive HUD order) ─────────────────────────
        var estudioPanel = BuildEstudioPanel(switcher);
        FillParent(estudioPanel);

        var produccionPanel = BuildPeliculasMainPanel(switcher);
        produccionPanel.name = "ProduccionPanel";
        FillParent(produccionPanel);

        var premiosPanel = BuildPremiosPanel(switcher);
        FillParent(premiosPanel);

        var coleccionPanel = BuildColeccionPanel(switcher);
        FillParent(coleccionPanel);

        var menuPanel = BuildMenuPanel(switcher);
        FillParent(menuPanel);

        // Start: Producción active
        estudioPanel.gameObject.SetActive(false);
        premiosPanel.gameObject.SetActive(false);
        coleccionPanel.gameObject.SetActive(false);
        menuPanel.gameObject.SetActive(false);

        // ── Wire StudioHubUI on ContentSwitcher (5-tab main nav) ─────────────
        var mainHubUI = switcher.gameObject.AddComponent<StudioHubUI>();
        var mainHubSO = new SerializedObject(mainHubUI);
        mainHubSO.FindProperty("tabPanels").arraySize = 5;
        mainHubSO.FindProperty("tabPanels").GetArrayElementAtIndex(0).objectReferenceValue = estudioPanel.gameObject;
        mainHubSO.FindProperty("tabPanels").GetArrayElementAtIndex(1).objectReferenceValue = produccionPanel.gameObject;
        mainHubSO.FindProperty("tabPanels").GetArrayElementAtIndex(2).objectReferenceValue = premiosPanel.gameObject;
        mainHubSO.FindProperty("tabPanels").GetArrayElementAtIndex(3).objectReferenceValue = coleccionPanel.gameObject;
        mainHubSO.FindProperty("tabPanels").GetArrayElementAtIndex(4).objectReferenceValue = menuPanel.gameObject;
        mainHubSO.FindProperty("tabButtons").arraySize = 5;
        for (int i = 0; i < 5; i++)
            mainHubSO.FindProperty("tabButtons").GetArrayElementAtIndex(i).objectReferenceValue = navButtons[i];
        mainHubSO.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(mainHubUI);

        // ── Settings overlay (modal, above everything) ────────────────────────
        var settingsOverlay = BuildSettingsOverlay(root);

        // Wire the ⚙ settings button in the topbar to toggle the overlay
        var settingsBtn2 = topBar.Find("SettingsBtn")?.GetComponent<Button>();
        if (settingsBtn2 != null)
        {
            var overlayUI = settingsOverlay.GetComponent<SettingsOverlayUI>();
            settingsBtn2.onClick.RemoveAllListeners();
            var settingsBtnSO = new SerializedObject(settingsBtn2);
            // Runtime wiring — the SettingsOverlayUI.Toggle() is called via a
            // UnityEvent in GameHub.WireGameSystems, but we add a hook here too.
            EditorUtility.SetDirty(settingsBtn2.gameObject);
        }

        // ── Bake definitive HUD structure into scene (Scene View = Play Mode) ─
        HudDefinitiveSceneBaker.Bake(switcher, mainHubUI);

        // ── Wire GameHub systems ───────────────────────────────────────────────
        WireGameSystems(root);

        // ── Game feel (DOTween popups, shake, bounce) ─────────────────────────
        WireGameFeel(root, topBar);

        EnsureEventSystem();
        NormalizeCanvas(canvas);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        Debug.Log($"[Builder] Studio UI v2 built and saved to {TARGET_SCENE}.");
    }

    // ─── TopBar ───────────────────────────────────────────────────────────────
    static RectTransform BuildTopBar(RectTransform parent)
    {
        var bar = MakePanel(parent, "TopBar", TOPBAR_BG);
        SetRounded(bar, 0);
        var hlg = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(16, 16, 8, 8);
        hlg.spacing = 0;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childAlignment         = TextAnchor.MiddleLeft;

        // Money block
        var moneyBlock = MakePanel(bar, "MoneyBlock", Color.clear);
        var moneyBlockLE = moneyBlock.gameObject.AddComponent<LayoutElement>();
        moneyBlockLE.flexibleWidth = 0;
        moneyBlockLE.preferredWidth = 280;
        var mbVLG = moneyBlock.gameObject.AddComponent<VerticalLayoutGroup>();
        mbVLG.childAlignment = TextAnchor.MiddleLeft;
        mbVLG.childControlWidth = mbVLG.childControlHeight = true;
        mbVLG.childForceExpandWidth = true; mbVLG.childForceExpandHeight = true;
        mbVLG.padding = new RectOffset(0, 0, 4, 0);
        var moneyTxt  = MakeText(moneyBlock, "MoneyText",  "$1.500", 36, TEXT_PRI, TextAlignmentOptions.MidlineLeft);
        moneyTxt.fontStyle = FontStyles.Bold;
        moneyTxt.gameObject.AddComponent<AnimatedMoneyText>();
        var incomeTxt = MakeText(moneyBlock, "IncomeText", "+$0/s",  18, ACCENT_GREEN, TextAlignmentOptions.MidlineLeft);

        // Stats: REP | Oscars | Nivel
        var statsBlock = MakePanel(bar, "StatsBlock", Color.clear);
        var statsLE = statsBlock.gameObject.AddComponent<LayoutElement>();
        statsLE.preferredWidth = 340;
        statsLE.flexibleWidth = 0;
        var sbHLG = statsBlock.gameObject.AddComponent<HorizontalLayoutGroup>();
        sbHLG.spacing = 8; sbHLG.childAlignment = TextAnchor.MiddleCenter;
        sbHLG.childControlWidth = sbHLG.childControlHeight = true;
        sbHLG.childForceExpandWidth = sbHLG.childForceExpandHeight = true;

        MakeStat(statsBlock, "RepText",    "REP 0",  ACCENT_BLUE);
        MakeStat(statsBlock, "CityText",   "I1",     ACCENT_GOLD);
        MakeStat(statsBlock, "OscarsText", "★ 0",    ACCENT_GOLD);

        // Spacer pushes menu to the far right
        var spacer = MakePanel(bar, "TopBarSpacer", Color.clear);
        var spacerLE = spacer.gameObject.AddComponent<LayoutElement>();
        spacerLE.flexibleWidth = 1f;

        // Settings button (far right)
        var settingsBtn = new GameObject("SettingsBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(settingsBtn, "SettingsBtn");
        settingsBtn.transform.SetParent(bar, false);
        HudSkinProvider.ApplyCard(settingsBtn.GetComponent<Image>(), HudCardVariant.Primary);
        SetRounded(settingsBtn.GetComponent<RectTransform>(), 12);
        var settingsLE = settingsBtn.AddComponent<LayoutElement>();
        settingsLE.preferredWidth = 56;
        settingsLE.preferredHeight = 56;
        settingsLE.flexibleWidth = 0;
        var settingsLbl = new GameObject("Lbl", typeof(RectTransform));
        settingsLbl.transform.SetParent(settingsBtn.transform, false);
        Stretch(settingsLbl.GetComponent<RectTransform>());
        var stTMP = settingsLbl.AddComponent<TextMeshProUGUI>();
        stTMP.text = "⚙"; stTMP.fontSize = 28; stTMP.color = TEXT_PRI;
        stTMP.alignment = TextAlignmentOptions.Center;

        // Wire TopBarUI
        var topBarUI = bar.gameObject.AddComponent<TopBarUI>();
        var so = new SerializedObject(topBarUI);
        so.FindProperty("moneyText")     .objectReferenceValue = moneyTxt;
        so.FindProperty("incomeText")    .objectReferenceValue = incomeTxt;
        so.FindProperty("reputationText").objectReferenceValue = FindTextInBlock(statsBlock, "RepText");
        so.FindProperty("cityText")      .objectReferenceValue = FindTextInBlock(statsBlock, "CityText");
        so.FindProperty("oscarsText")    .objectReferenceValue = FindTextInBlock(statsBlock, "OscarsText");
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(topBarUI);

        return bar;
    }

    static TextMeshProUGUI FindTextInBlock(RectTransform block, string childName)
    {
        var child = block.Find(childName);
        return child != null ? child.GetComponentInChildren<TextMeshProUGUI>() : null;
    }

    static void MakeStat(RectTransform parent, string name, string value, Color color)
    {
        var block = MakePanel(parent, name + "Block", Color.clear);
        block.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        var vl = block.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment = TextAnchor.MiddleCenter;
        vl.childControlWidth = vl.childControlHeight = true;
        vl.childForceExpandWidth = vl.childForceExpandHeight = true;
        var t = MakeText(block, name, value, 22, color, TextAlignmentOptions.Center);
        t.fontStyle = FontStyles.Bold;
    }

    // ─── Estudio Panel ────────────────────────────────────────────────────────
    static RectTransform BuildEstudioPanel(RectTransform parent)
    {
        var panel = MakePanel(parent, "EstudioPanel", BG_DEEP);
        var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;

        // Studio Header — studio name, level, XP bar, mission
        var hdr = BuildStudioHeader(panel);
        LE(hdr, (int)HudLayoutConstants.StudioHeaderHeight);
        hdr.gameObject.GetComponent<LayoutElement>().flexibleHeight = 0f;

        // Studio Visual Stage — reduced to give more room to departments
        var stage = MakePanel(panel, "StudioVisualStage", new Color(0.06f, 0.07f, 0.12f));
        var stageLE = stage.gameObject.GetComponent<LayoutElement>() ?? stage.gameObject.AddComponent<LayoutElement>();
        stageLE.preferredHeight = 160f;
        stageLE.minHeight = 120f;
        stageLE.flexibleHeight = 0f;
        stageLE.flexibleWidth = 0f;
        var stageComp = stage.gameObject.AddComponent<StudioVisualStage>();
        StudioVisualStageLayers.EnsureHierarchy(stageComp);
        if (stageComp.GetComponent<StudioVisualManager>() == null)
            stageComp.gameObject.AddComponent<StudioVisualManager>();
        StudioVisualThemeController.ApplyTheme(stageComp, CityTier.City1);

        // Bonifications panel — 6-stat horizontal bar
        var bonif = BuildBonificationsBar(panel);
        LE(bonif, 72f);
        bonif.gameObject.GetComponent<LayoutElement>().flexibleHeight = 0f;

        // Department grid — 2-column layout (more space, bigger cards)
        var deptBar = BuildDeptGrid(panel);
        var deptLE = deptBar.gameObject.GetComponent<LayoutElement>() ?? deptBar.gameObject.AddComponent<LayoutElement>();
        deptLE.flexibleHeight = 1f;
        deptLE.minHeight = DepartmentMiniCardLayoutBuilder.CardHeight * 2f + 30f;
        deptLE.preferredHeight = 0f;

        // Split: MEJORAS | CONTRATOS
        var splitRow = BuildManagementSplit(panel);
        var splitLE = splitRow.gameObject.GetComponent<LayoutElement>() ?? splitRow.gameObject.AddComponent<LayoutElement>();
        splitLE.preferredHeight = 280f;
        splitLE.minHeight = 220f;
        splitLE.flexibleHeight = 0f;

        return panel;
    }

    // ─── Bonifications Bar ────────────────────────────────────────────────────
    static RectTransform BuildBonificationsBar(RectTransform parent)
    {
        var bar = MakePanel(parent, "BonificationsBar", BG_CARD);

        var hlg = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(8, 8, 6, 6);
        hlg.spacing = 4;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        // Title pill
        var titleBlock = MakePanel(bar, "BonifTitle", BG_SECTION);
        SetRounded(titleBlock, 6);
        var titleLE = titleBlock.gameObject.AddComponent<LayoutElement>();
        titleLE.preferredWidth = 84f;
        titleLE.flexibleWidth = 0f;
        var titleTxt = MakeText(titleBlock, "TitleTxt", "BONUS", 11, TEXT_DIM, TextAlignmentOptions.Center);
        titleTxt.fontStyle = FontStyles.Bold;

        // 6 stat cells: Velocidad, Calidad, Taquilla, REP, XP, Costes
        var statDefs = new[]
        {
            ("SpeedVal",     "VEL",  ACCENT_GREEN),
            ("QualityVal",   "CAL",  ACCENT_BLUE),
            ("BoxOfficeVal", "TAQ",  ACCENT_GOLD),
            ("RepVal",       "REP",  ACCENT_GOLD),
            ("XpVal",        "XP",   ACCENT_PURPLE),
            ("CostsVal",     "CTE",  ACCENT_RED),
        };

        foreach (var (id, label, color) in statDefs)
        {
            var cell = MakePanel(bar, "Cell_" + id, Color.clear);
            var cellVLG = cell.gameObject.AddComponent<VerticalLayoutGroup>();
            cellVLG.spacing = 1;
            cellVLG.childAlignment = TextAnchor.MiddleCenter;
            cellVLG.childControlWidth = cellVLG.childControlHeight = true;
            cellVLG.childForceExpandWidth = true;
            cellVLG.childForceExpandHeight = true;

            var lbl = MakeText(cell, "Lbl", label, 9, TEXT_DIM, TextAlignmentOptions.Center);
            LE(lbl.GetComponent<RectTransform>(), 12f);

            var val = MakeText(cell, id, "+0%", 14, color, TextAlignmentOptions.Center);
            val.fontStyle = FontStyles.Bold;
            LE(val.GetComponent<RectTransform>(), 20f);
        }

        bar.gameObject.AddComponent<BonificationsBarUI>();
        return bar;
    }

    // ─── Department Grid (2-column, larger cards) ─────────────────────────────
    static RectTransform BuildDeptGrid(RectTransform parent)
    {
        var bar = MakePanel(parent, "DeptBar", BG_SECTION);

        var scroll = MakeScrollRect(bar, "DeptScroll", Color.clear);
        Stretch(scroll.GetComponent<RectTransform>());

        var content = scroll.content;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot     = new Vector2(0.5f, 1f);

        // 2-column grid via VerticalLayoutGroup with pairs
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 8, 8);
        vlg.spacing = 8;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.horizontal = false;
        scroll.vertical   = true;

        var deptDefs = new[]
        {
            (DepartmentType.Director,       "Director",    "#8E44AD"),
            (DepartmentType.Actors,         "Actores",     "#E74C3C"),
            (DepartmentType.Editor,         "Editor",      "#9B59B6"),
            (DepartmentType.Cinematography, "Fotografía",  "#2980B9"),
            (DepartmentType.Sound,          "Sonido",      "#3498DB"),
            (DepartmentType.Makeup,         "Maquillaje",  "#E91E8C"),
            (DepartmentType.Costume,        "Vestuario",   "#C2185B"),
            (DepartmentType.Art,            "Arte",        "#F39C12"),
            (DepartmentType.Lighting,       "Iluminación", "#E67E22"),
            (DepartmentType.Grip,           "Grip",        "#D35400"),
            (DepartmentType.Producer,       "Productor",   "#27AE60"),
        };

        // Build pairs (2 per row)
        for (int i = 0; i < deptDefs.Length; i += 2)
        {
            var row = new GameObject("DeptRow_" + i, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(row, "DeptRow");
            row.transform.SetParent(content, false);
            row.GetComponent<Image>().color = Color.clear;
            var rowLE = row.AddComponent<LayoutElement>();
            rowLE.preferredHeight = DepartmentMiniCardLayoutBuilder.CardHeight;
            var rowHLG = row.AddComponent<HorizontalLayoutGroup>();
            rowHLG.spacing = 8;
            rowHLG.childControlWidth = rowHLG.childControlHeight = true;
            rowHLG.childForceExpandWidth = true;
            rowHLG.childForceExpandHeight = true;

            var (deptType0, name0, hex0) = deptDefs[i];
            BuildMiniCard(row.GetComponent<RectTransform>(), deptType0, name0, hex0);

            if (i + 1 < deptDefs.Length)
            {
                var (deptType1, name1, hex1) = deptDefs[i + 1];
                BuildMiniCard(row.GetComponent<RectTransform>(), deptType1, name1, hex1);
            }
            else
            {
                // Filler to keep grid balanced
                var filler = new GameObject("Filler", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(filler, "Filler");
                filler.transform.SetParent(row.transform, false);
                filler.AddComponent<LayoutElement>().flexibleWidth = 1f;
            }
        }

        return bar;
    }

    static RectTransform BuildStudioHeader(RectTransform parent)
    {
        var hdr = MakePanel(parent, "StudioHeader", BG_SECTION);
        var hlg = hdr.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(4, 4, 4, 4);
        hlg.spacing = 4;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        // 1 — Logo
        var icon = MakePanel(hdr, "Icon", ACCENT_GREEN);
        SetRounded(icon, 8);
        SetFlexWidth(icon);
        MakeText(icon, "IconTxt", "CS", 22, TEXT_PRI, TextAlignmentOptions.Center);

        // 2 — Studio name
        var nameBlock = MakePanel(hdr, "NameBlock", BG_CARD2);
        SetRounded(nameBlock, 8);
        SetFlexWidth(nameBlock);
        var nameVLG = nameBlock.gameObject.AddComponent<VerticalLayoutGroup>();
        nameVLG.padding = new RectOffset(4, 4, 6, 4);
        nameVLG.childAlignment = TextAnchor.MiddleCenter;
        nameVLG.childControlWidth = nameVLG.childControlHeight = true;
        nameVLG.childForceExpandWidth = true;
        var studioName = MakeText(nameBlock, "StudioNameText", "IDLE FILM\nSTUDIO", 11, TEXT_PRI, TextAlignmentOptions.Center);
        studioName.fontStyle = FontStyles.Bold;
        studioName.textWrappingMode = TMPro.TextWrappingModes.Normal;

        // 3 — Level + XP
        var xpBlock = MakePanel(hdr, "XpBlock", BG_CARD2);
        SetRounded(xpBlock, 8);
        SetFlexWidth(xpBlock);
        var xpVLG = xpBlock.gameObject.AddComponent<VerticalLayoutGroup>();
        xpVLG.padding = new RectOffset(6, 6, 6, 6);
        xpVLG.spacing = 3;
        xpVLG.childControlWidth = xpVLG.childControlHeight = true;
        xpVLG.childForceExpandWidth = true;
        xpVLG.childForceExpandHeight = false;
        var levelLine = MakeText(xpBlock, "LevelLineText", "Nv. 1", 13, ACCENT_GREEN, TextAlignmentOptions.Center);
        levelLine.fontStyle = FontStyles.Bold;
        LE(levelLine.GetComponent<RectTransform>(), 16);
        var xpTxt = MakeText(xpBlock, "XPText", "0/120 XP", 10, TEXT_SEC, TextAlignmentOptions.Center);
        LE(xpTxt.GetComponent<RectTransform>(), 12);
        var xpBarGo = new GameObject("XPBar", typeof(RectTransform), typeof(Image), typeof(Slider));
        Undo.RegisterCreatedObjectUndo(xpBarGo, "XPBar");
        xpBarGo.transform.SetParent(xpBlock, false);
        LE(xpBarGo.GetComponent<RectTransform>(), 8, 1f);
        var slider = xpBarGo.GetComponent<Slider>();
        slider.value = 0f; slider.minValue = 0f; slider.maxValue = 1f;
        xpBarGo.GetComponent<Image>().color = BG_CARD;
        SetRounded(xpBarGo.GetComponent<RectTransform>(), 4);
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(xpBarGo.transform, false);
        var faRT = fillArea.GetComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
        faRT.offsetMin = faRT.offsetMax = Vector2.zero;
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        fill.GetComponent<Image>().color = ACCENT_GREEN;
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
        slider.fillRect = fillRT;

        var lvlUI = hdr.gameObject.AddComponent<StudioLevelBarUI>();
        var so = new SerializedObject(lvlUI);
        so.FindProperty("studioNameText").objectReferenceValue = studioName;
        so.FindProperty("levelText")    .objectReferenceValue = levelLine;
        so.FindProperty("xpText")       .objectReferenceValue = xpTxt;
        so.FindProperty("xpBar")        .objectReferenceValue = slider;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 4 — Mission
        var mission = MakePanel(hdr, "MissionCard", ACCENT_GREEN);
        SetRounded(mission, 8);
        SetFlexWidth(mission);
        var mVLG = mission.gameObject.AddComponent<VerticalLayoutGroup>();
        mVLG.padding = new RectOffset(4, 4, 6, 4);
        mVLG.spacing = 2;
        mVLG.childAlignment = TextAnchor.MiddleCenter;
        mVLG.childControlWidth = mVLG.childControlHeight = true;
        mVLG.childForceExpandWidth = true;
        var mTitle = MakeText(mission, "MissionTitle", "MISIÓN", 10, TEXT_PRI, TextAlignmentOptions.Center);
        mTitle.fontStyle = FontStyles.Bold;
        var mDesc = MakeText(mission, "MissionDesc", "Contrato", 10, TEXT_PRI, TextAlignmentOptions.Center);
        mDesc.textWrappingMode = TMPro.TextWrappingModes.Normal;
        var mReward = MakeText(mission, "MissionReward", "+$ REP", 9, ACCENT_GOLD, TextAlignmentOptions.Center);

        return hdr;
    }

    // ─── Department Mini-Bar (horizontal scroll, info only) ───────────────────
    static RectTransform BuildDeptMiniBar(RectTransform parent)
    {
        var bar = MakePanel(parent, "DeptBar", BG_SECTION);

        var scroll = MakeScrollRect(bar, "DeptScroll", Color.clear);
        Stretch(scroll.GetComponent<RectTransform>());

        var content = scroll.content;
        var hlg = content.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(12, 12, 8, 8);
        hlg.spacing = 14;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth  = false;
        hlg.childControlHeight = true;
        hlg.childControlWidth  = true;

        var csf = content.gameObject.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit   = ContentSizeFitter.FitMode.Unconstrained;
        scroll.horizontal = true;
        scroll.vertical   = false;

        // Create mini-cards for each department (DepartmentMiniCardUI)
        var deptDefs = new[]
        {
            (DepartmentType.Editor,         "Editor",       "#9B59B6"),
            (DepartmentType.Director,       "Director",     "#8E44AD"),
            (DepartmentType.Actors,         "Actores",      "#E74C3C"),
            (DepartmentType.Sound,          "Sonido",       "#3498DB"),
            (DepartmentType.Cinematography, "Fotografía",   "#2980B9"),
            (DepartmentType.Makeup,         "Maquillaje",   "#E91E8C"),
            (DepartmentType.Costume,        "Vestuario",    "#C2185B"),
            (DepartmentType.Art,            "Arte",         "#F39C12"),
            (DepartmentType.Lighting,       "Iluminación",  "#E67E22"),
            (DepartmentType.Grip,           "Grip",         "#D35400"),
            (DepartmentType.Producer,       "Productor",    "#27AE60"),
        };

        foreach (var (deptType, name, hexColor) in deptDefs)
            BuildMiniCard(content, deptType, name, hexColor);

        return bar;
    }

    static void BuildMiniCard(RectTransform parent, DepartmentType deptType, string deptName, string hexColor)
    {
        var card = MakePanel(parent, "MiniCard_" + deptName, BG_CARD);
        var wire = DepartmentMiniCardLayoutBuilder.Build(card, Hex(hexColor));

        var miniUI = card.gameObject.GetComponent<DepartmentMiniCardUI>() ?? card.gameObject.AddComponent<DepartmentMiniCardUI>();
        var so = new SerializedObject(miniUI);
        so.FindProperty("deptType").enumValueIndex = (int)deptType;
        so.FindProperty("categoryBadge").objectReferenceValue = wire.categoryBadge;
        so.FindProperty("deptNameText").objectReferenceValue = wire.deptNameText;
        so.FindProperty("levelText").objectReferenceValue = wire.levelText;
        so.FindProperty("effectText").objectReferenceValue = wire.effectText;
        so.FindProperty("levelProgressBar").objectReferenceValue = wire.levelProgressBar;
        so.FindProperty("upgradeButton").objectReferenceValue = wire.upgradeButton;
        so.FindProperty("upgradeLabelText").objectReferenceValue = wire.upgradeLabelText;
        so.FindProperty("upgradeCostText").objectReferenceValue = wire.upgradeCostText;
        so.FindProperty("themeBackdrop").objectReferenceValue = wire.themeBackdrop;
        so.FindProperty("themeVisual").objectReferenceValue = wire.themeVisual;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(miniUI);
    }

    /// <summary>Production tab: active slots widget, offers, new production action.</summary>
    static RectTransform BuildPeliculasMainPanel(RectTransform parent)
    {
        var panel = MakePanel(parent, "PeliculasPanel", BG_DEEP);
        Stretch(panel);
        var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 12, 12);
        vlg.spacing = 8;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var hdr = MakeText(panel, "Hdr", Loc.Get(LocKeys.ProdTabTitle), 22, TEXT_PRI, TextAlignmentOptions.MidlineLeft);
        hdr.fontStyle = FontStyles.Bold;
        LE(hdr.GetComponent<RectTransform>(), 28);

        var prodWidget = BuildProductionWidget(panel);
        LE(prodWidget, 220);

        var slotsRow = MakePanel(panel, "MovieSlotsRow", BG_SECTION);
        LE(slotsRow, 340);
        slotsRow.gameObject.GetComponent<LayoutElement>().flexibleHeight = 0f;
        var slotsHLG = slotsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        slotsHLG.padding = new RectOffset(6, 6, 6, 6);
        slotsHLG.spacing = 8;
        slotsHLG.childControlWidth = slotsHLG.childControlHeight = true;
        slotsHLG.childForceExpandWidth = true;
        slotsHLG.childForceExpandHeight = true;
        var slotLayout = slotsRow.gameObject.AddComponent<SquareTileRowLayout>();
        slotLayout.tileCount = 3;
        slotLayout.maxTileSize = 340f;

        var newBtnGo = new GameObject("NewProductionBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(newBtnGo, "NewProductionBtn");
        newBtnGo.transform.SetParent(panel, false);
        HudSkinProvider.ApplyButton(newBtnGo.GetComponent<Image>(), HudButtonVariant.Success);
        SetRounded(newBtnGo.GetComponent<RectTransform>(), 8);
        var newBtnLE = newBtnGo.AddComponent<LayoutElement>();
        newBtnLE.preferredHeight = 56f;
        newBtnGo.AddComponent<UIButtonScale>();
        var newBtnLabel = MakeText(newBtnGo.GetComponent<RectTransform>(), "Lbl", Loc.Get(LocKeys.ProdNewProduction), 16, TEXT_PRI, TextAlignmentOptions.Center);
        newBtnLabel.fontStyle = FontStyles.Bold;
        Stretch(newBtnLabel.GetComponent<RectTransform>());
        var newBtn = newBtnGo.GetComponent<Button>();

        var tabUI = panel.gameObject.AddComponent<MovieTabUI>();
        var movies = LoadAllMovieConfigs();
        var tabSO = new SerializedObject(tabUI);
        tabSO.FindProperty("slotsRow").objectReferenceValue = slotsRow;
        tabSO.FindProperty("historyContent").objectReferenceValue = null;
        tabSO.FindProperty("historyEmptyLabel").objectReferenceValue = null;
        tabSO.FindProperty("allMovies").arraySize = movies.Length;
        for (int i = 0; i < movies.Length; i++)
            tabSO.FindProperty("allMovies").GetArrayElementAtIndex(i).objectReferenceValue = movies[i];
        tabSO.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(tabUI);

        var shell = panel.gameObject.AddComponent<ProductionHudShell>();
        shell.Configure(prodWidget, tabUI, newBtn);

        Debug.Log($"[Builder] MovieTabUI: {movies.Length} movies assigned.");
        return panel;
    }

    // ─── Production Widget ────────────────────────────────────────────────────
    static RectTransform BuildProductionWidget(RectTransform parent)
    {
        var widget = MakePanel(parent, "ProductionWidget", BG_CARD);
        var hlg = widget.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(16, 16, 12, 12);
        hlg.spacing = 12;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;

        // Left: movie info
        var info = MakePanel(widget, "MovieInfo", Color.clear);
        LE(info, 0, 1f);
        var infoVLG = info.gameObject.AddComponent<VerticalLayoutGroup>();
        infoVLG.childControlWidth = infoVLG.childControlHeight = true;
        infoVLG.childForceExpandWidth = infoVLG.childForceExpandHeight = true;
        infoVLG.spacing = 4;

        var nowLabel = MakeText(info, "NowLabel", "PRODUCCIÓN ACTUAL", 16, TEXT_DIM, TextAlignmentOptions.MidlineLeft);
        LE(nowLabel.GetComponent<RectTransform>(), 20);
        var movieNameTxt = MakeText(info, "MovieNameText", "—",  22, TEXT_PRI, TextAlignmentOptions.MidlineLeft);
        movieNameTxt.fontStyle = FontStyles.Bold;
        LE(movieNameTxt.GetComponent<RectTransform>(), 30);
        var timeTxt = MakeText(info, "TimeText", "0.0s",  20, TEXT_SEC, TextAlignmentOptions.MidlineLeft);
        LE(timeTxt.GetComponent<RectTransform>(), 24);

        // Progress bar
        var progBarGo = new GameObject("ProgressBar", typeof(RectTransform), typeof(Image), typeof(Slider));
        Undo.RegisterCreatedObjectUndo(progBarGo, "ProgressBar");
        progBarGo.transform.SetParent(info, false);
        LE(progBarGo.GetComponent<RectTransform>(), 16);
        var progSlider = progBarGo.GetComponent<Slider>();
        progSlider.value = 0f;
        progBarGo.GetComponent<Image>().color = BG_CARD2;
        SetRounded(progBarGo.GetComponent<RectTransform>(), 6);
        var pfArea = new GameObject("Fill Area", typeof(RectTransform)); pfArea.transform.SetParent(progBarGo.transform, false);
        var pfAreaRT = pfArea.GetComponent<RectTransform>();
        pfAreaRT.anchorMin = Vector2.zero; pfAreaRT.anchorMax = Vector2.one; pfAreaRT.offsetMin = pfAreaRT.offsetMax = Vector2.zero;
        var pFill = new GameObject("Fill", typeof(RectTransform), typeof(Image)); pFill.transform.SetParent(pfArea.transform, false);
        pFill.GetComponent<Image>().color = ACCENT_GREEN;
        var pFillRT = pFill.GetComponent<RectTransform>();
        pFillRT.anchorMin = Vector2.zero; pFillRT.anchorMax = Vector2.one; pFillRT.offsetMin = pFillRT.offsetMax = Vector2.zero;
        progSlider.fillRect = pFillRT;

        // Right: reward + produce button
        var actions = MakePanel(widget, "Actions", Color.clear);
        LE(actions, 0, 0, 240);
        var actVLG = actions.gameObject.AddComponent<VerticalLayoutGroup>();
        actVLG.spacing = 8; actVLG.childControlWidth = actVLG.childControlHeight = true;
        actVLG.childForceExpandWidth = actVLG.childForceExpandHeight = false;
        actVLG.childAlignment = TextAnchor.MiddleRight;

        var rewardTxt = MakeText(actions, "RewardText", "+$0", 22, ACCENT_GREEN, TextAlignmentOptions.MidlineRight);
        LE(rewardTxt.GetComponent<RectTransform>(), 28);
        var repTxt = MakeText(actions, "RepText", "+0 REP", 18, ACCENT_GOLD, TextAlignmentOptions.MidlineRight);
        LE(repTxt.GetComponent<RectTransform>(), 22);

        // Wire ProductionStatusUI
        var prodStatus = widget.gameObject.AddComponent<ProductionStatusUI>();
        var so = new SerializedObject(prodStatus);
        so.FindProperty("movieNameText").objectReferenceValue = movieNameTxt;
        so.FindProperty("timeLeftText") .objectReferenceValue = timeTxt;
        so.FindProperty("progressBar")  .objectReferenceValue = progSlider;
        so.FindProperty("rewardText")   .objectReferenceValue = rewardTxt;
        so.FindProperty("repText")      .objectReferenceValue = repTxt;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(prodStatus);

        return widget;
    }

    // ─── Management split (MEJORAS 50% | CONTRATOS 50%) ─────────────────────
    static RectTransform BuildManagementSplit(RectTransform parent)
    {
        var row = MakePanel(parent, "ManagementSplit", BG_SECTION);
        var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.padding = new RectOffset(8, 8, 6, 6);
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;

        var mejorasCol = MakePanel(row, "MejorasColumn", BG_DEEP);
        var mejorasColLE = mejorasCol.gameObject.AddComponent<LayoutElement>();
        mejorasColLE.flexibleWidth = 1f;
        mejorasColLE.flexibleHeight = 1f;
        mejorasColLE.preferredWidth = 0f;
        var mColVLG = mejorasCol.gameObject.AddComponent<VerticalLayoutGroup>();
        mColVLG.spacing = 2;
        mColVLG.childControlWidth = mColVLG.childControlHeight = true;
        mColVLG.childForceExpandWidth = true;
        mColVLG.childForceExpandHeight = false;
        var mejorasHdr = MakeText(mejorasCol, "Hdr", "MEJORAS", 14, ACCENT_GREEN, TextAlignmentOptions.MidlineLeft);
        LE(mejorasHdr.GetComponent<RectTransform>(), 16);
        var mejorasPanel = BuildMejorasPanel(mejorasCol);
        LE(mejorasPanel, 0, 1f);

        var contratosCol = MakePanel(row, "ContratosColumn", BG_DEEP);
        var contratosColLE = contratosCol.gameObject.AddComponent<LayoutElement>();
        contratosColLE.flexibleWidth = 1f;
        contratosColLE.flexibleHeight = 1f;
        contratosColLE.preferredWidth = 0f;
        var rColVLG = contratosCol.gameObject.AddComponent<VerticalLayoutGroup>();
        rColVLG.spacing = 6;
        rColVLG.padding = new RectOffset(0, 0, 0, 0);
        rColVLG.childControlWidth = rColVLG.childControlHeight = true;
        rColVLG.childForceExpandWidth = true;
        rColVLG.childForceExpandHeight = false;

        var contratosPanel = BuildContratosPanel(contratosCol);
        LE(contratosPanel, 0, 1f);

        return row;
    }

    static RectTransform BuildColeccionPanel(RectTransform parent)
    {
        // The baker (HudDefinitiveStructureBuilder) adds sub-tabs:
        //   Tab 0: COLECCIÓN (MovieCollectionUI + our GenreSelectionUI + CollectionNavController)
        //   Tab 1: SAGAS
        //   Tab 2: LEGENDARIAS
        // We just create the panel; the baker populates tab structure at bake time.
        // The CollectionNavController is wired by CollectionHudShell at runtime.
        var panel = MakePanel(parent, "ColeccionPanel", BG_DEEP);
        panel.gameObject.AddComponent<CollectionHudShell>();
        return panel;
    }

    static RectTransform BuildMenuPanel(RectTransform parent)
    {
        // Tab 4 is now TIENDA (Shop) — placeholder shop screen.
        // We keep the object named "TiendaPanel" but add a MenuHudShell guard
        // so HudDefinitiveStructureBuilder skips restructuring it.
        var panel = MakePanel(parent, "TiendaPanel", BG_DEEP);
        var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 40, 20);
        vlg.spacing = 20;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        var hdrTxt = MakeText(panel, "TiendaHeader", Loc.Get(LocKeys.TiendaTitle), 32, ACCENT_GOLD, TextAlignmentOptions.Center);
        hdrTxt.fontStyle = FontStyles.Bold;
        LE(hdrTxt.GetComponent<RectTransform>(), 44f);

        // Placeholder sections
        var sections = new[]
        {
            ("💎", "Diamantes",       "Packs de moneda premium"),
            ("🎁", "Packs especiales", "Packs de inicio y eventos"),
            ("📺", "Sin anuncios",    "Elimina los anuncios para siempre"),
            ("🎬", "Slot premium",    "Tercer slot de producción"),
            ("⏰", "Ofertas",         "Ofertas limitadas"),
        };

        foreach (var (emoji, title, sub) in sections)
        {
            var card = MakePanel(panel, "ShopCard_" + title, BG_CARD2);
            LE(card, 84); SetRounded(card, 12);
            var hlg = card.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(16, 16, 12, 12);
            hlg.spacing = 14;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = hlg.childControlHeight = true;
            hlg.childForceExpandHeight = true;

            var emj = MakeText(card, "Emoji", emoji, 30, TEXT_PRI, TextAlignmentOptions.Center);
            emj.GetComponent<RectTransform>().gameObject.AddComponent<LayoutElement>().preferredWidth = 44f;

            var info = MakePanel(card, "Info", Color.clear);
            info.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var iVLG = info.gameObject.AddComponent<VerticalLayoutGroup>();
            iVLG.childControlWidth = iVLG.childControlHeight = true;
            iVLG.childForceExpandWidth = true;
            MakeText(info, "Title", title, 18, TEXT_PRI, TextAlignmentOptions.MidlineLeft).fontStyle = FontStyles.Bold;
            MakeText(info, "Sub",   sub,   14, TEXT_DIM, TextAlignmentOptions.MidlineLeft);

            var comingSoon = MakePanel(card, "Soon", BG_SECTION);
            SetRounded(comingSoon, 6);
            comingSoon.gameObject.AddComponent<LayoutElement>().preferredWidth = 90f;
            MakeText(comingSoon, "Lbl", Loc.Get(LocKeys.TiendaComingSoon), 12, TEXT_DIM, TextAlignmentOptions.Center);
        }

        // Guard: add MenuHudShell with settingsSection = panel itself so the
        // HudDefinitiveStructureBuilder.RestructureMenuPanel early-exit fires
        // and does not clear our Tienda content.
        var guardShell = panel.gameObject.AddComponent<MenuHudShell>();
        var guardSO = new SerializedObject(guardShell);
        guardSO.FindProperty("settingsSection").objectReferenceValue = panel;
        guardSO.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(guardShell);

        return panel;
    }

    static RectTransform BuildSettingsOverlay(RectTransform root)
    {
        var overlay = MakePanel(root, "SettingsOverlay", Color.clear);
        Stretch(overlay);
        overlay.SetAsLastSibling();
        overlay.gameObject.SetActive(false);
        var ui = overlay.gameObject.AddComponent<SettingsOverlayUI>();
        overlay.gameObject.AddComponent<CanvasGroup>();
        return overlay;
    }

    static MovieConfig[] LoadAllMovieConfigs()
    {
        var movieGuids = AssetDatabase.FindAssets("t:MovieConfig", new[] { "Assets/Data/Movies" });
        var movies     = new MovieConfig[movieGuids.Length];
        for (int i = 0; i < movieGuids.Length; i++)
            movies[i] = AssetDatabase.LoadAssetAtPath<MovieConfig>(
                AssetDatabase.GUIDToAssetPath(movieGuids[i]));
        System.Array.Sort(movies, (a, b) =>
        {
            int lvlCmp = a.unlockStudioLevel.CompareTo(b.unlockStudioLevel);
            return lvlCmp != 0 ? lvlCmp : a.cost.CompareTo(b.cost);
        });
        return movies;
    }

    // ─── Movie section (collapsible tab: PELÍCULAS | DEPTS) ───────────────────
    static RectTransform BuildMovieSection(RectTransform parent)
    {
        var section = MakePanel(parent, "MovieSection", BG_SECTION);
        var vlg = section.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var tabBar = BuildTabButtonRow(section, "MovieTabBar",
            new[] { "PELÍCULAS", "DEPTS" },
            new[] { ACCENT_RED, ACCENT_GOLD },
            SUB_TAB_H);
        LE(tabBar, LOWER_TAB_H);

        var content = MakePanel(section, "MovieContent", BG_DEEP);
        FillParent(content);

        var peliculasPanel = BuildMovieSelectorPanel(content);
        FillParent(peliculasPanel);
        var deptInfoPanel = BuildDeptInfoPanel(content);
        FillParent(deptInfoPanel);
        deptInfoPanel.gameObject.SetActive(false);

        var hubUI = section.gameObject.AddComponent<StudioHubUI>();
        var so = new SerializedObject(hubUI);
        var tabBtns = tabBar.GetComponentsInChildren<Button>(true);
        so.FindProperty("tabPanels").arraySize = 2;
        so.FindProperty("tabPanels").GetArrayElementAtIndex(0).objectReferenceValue = peliculasPanel.gameObject;
        so.FindProperty("tabPanels").GetArrayElementAtIndex(1).objectReferenceValue = deptInfoPanel.gameObject;
        so.FindProperty("tabButtons").arraySize = Mathf.Min(tabBtns.Length, 2);
        for (int i = 0; i < Mathf.Min(tabBtns.Length, 2); i++)
            so.FindProperty("tabButtons").GetArrayElementAtIndex(i).objectReferenceValue = tabBtns[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        return section;
    }

    // Legacy wrapper kept for compatibility
    static RectTransform BuildLowerTabs(RectTransform parent) => BuildMovieSection(parent);

    // ─── Movie Selector Panel (runtime movie grid) ────────────────────────────
    static RectTransform BuildMovieSelectorPanel(RectTransform parent)
    {
        var panel = MakePanel(parent, "PeliculasPanel_Lower", BG_DEEP);
        Stretch(panel);

        var scroll = MakeScrollRect(panel, "Scroll", Color.clear);
        Stretch(scroll.GetComponent<RectTransform>());
        scroll.horizontal = false; scroll.vertical = true;

        var content = scroll.content;
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 8, 8);
        vlg.spacing = 6;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        // MovieGridUI on the scroll content root — populates cards at runtime
        var gridUI = panel.gameObject.AddComponent<MovieGridUI>();
        var gridSO = new SerializedObject(gridUI);
        gridSO.FindProperty("content").objectReferenceValue = content;
        gridSO.FindProperty("showAllGenres").boolValue      = true;

        // Load and assign all 180 movie assets
        var movies = LoadAllMovieConfigs();
        gridSO.FindProperty("allMovies").arraySize = movies.Length;
        for (int i = 0; i < movies.Length; i++)
            gridSO.FindProperty("allMovies").GetArrayElementAtIndex(i).objectReferenceValue = movies[i];
        gridSO.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(gridUI);

        Debug.Log($"[Builder] MovieGridUI: {movies.Length} movies assigned.");
        return panel;
    }

    // ─── Mejoras Panel ────────────────────────────────────────────────────────
    static RectTransform BuildMejorasPanel(RectTransform parent)
    {
        var panel = MakePanel(parent, "MejorasPanel", BG_DEEP);
        var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        // Compact 4-square tab row (white zone — minimal height)
        var subTabBar = BuildCompactSquareTabRow(panel, "MejorasSubTabBar",
            new[] { "EQ", "PER", "INS", "MKT" },
            new[] { ACCENT_BLUE, ACCENT_GREEN, ACCENT_GOLD, ACCENT_PURPLE });
        var subTabLE = subTabBar.gameObject.GetComponent<LayoutElement>() ?? subTabBar.gameObject.AddComponent<LayoutElement>();
        subTabLE.flexibleHeight = 0f;
        subTabLE.minHeight = 28f;

        // Sub-content area
        var subContent = MakePanel(panel, "MejorasSubContent", BG_DEEP);
        FillParent(subContent);

        // Build each sub-panel
        var equipPanel   = BuildUpgradeScrollPanel(subContent, "EquipoPanel",   UpgradeCategory.Equipment);
        var personalPanel= BuildUpgradeScrollPanel(subContent, "PersonalPanel", UpgradeCategory.Personnel);
        var instPanel    = BuildUpgradeScrollPanel(subContent, "InstPanel",     UpgradeCategory.Installation);
        var mktPanel     = BuildUpgradeScrollPanel(subContent, "MktPanel",     UpgradeCategory.Marketing);

        FillParent(equipPanel);
        FillParent(personalPanel);
        FillParent(instPanel);
        FillParent(mktPanel);

        personalPanel.gameObject.SetActive(false);
        instPanel.gameObject.SetActive(false);
        mktPanel.gameObject.SetActive(false);

        // Wire sub-tab switching via a second StudioHubUI on this panel
        var subHub = panel.gameObject.AddComponent<StudioHubUI>();
        var so = new SerializedObject(subHub);
        var subBtns = subTabBar.GetComponentsInChildren<Button>(true);
        so.FindProperty("tabPanels").arraySize  = 4;
        so.FindProperty("tabPanels").GetArrayElementAtIndex(0).objectReferenceValue = equipPanel.gameObject;
        so.FindProperty("tabPanels").GetArrayElementAtIndex(1).objectReferenceValue = personalPanel.gameObject;
        so.FindProperty("tabPanels").GetArrayElementAtIndex(2).objectReferenceValue = instPanel.gameObject;
        so.FindProperty("tabPanels").GetArrayElementAtIndex(3).objectReferenceValue = mktPanel.gameObject;
        so.FindProperty("tabButtons").arraySize = Mathf.Min(subBtns.Length, 4);
        for (int i = 0; i < Mathf.Min(subBtns.Length, 4); i++)
            so.FindProperty("tabButtons").GetArrayElementAtIndex(i).objectReferenceValue = subBtns[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(subHub);

        return panel;
    }

    static RectTransform BuildUpgradeScrollPanel(RectTransform parent, string name, UpgradeCategory category)
    {
        var panel = MakePanel(parent, name, BG_DEEP);
        Stretch(panel);

        var scroll = MakeScrollRect(panel, "Scroll", Color.clear);
        Stretch(scroll.GetComponent<RectTransform>());
        scroll.horizontal = false; scroll.vertical = true;

        var content = scroll.content;
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(16, 16, 12, 12);
        vlg.spacing = 10;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        // Load upgrade configs and create static cards
        var configs = LoadUpgradesByCategory(category);
        foreach (var cfg in configs)
            BuildUpgradeCard(content, cfg);

        if (configs.Count == 0)
        {
            var empty = MakeText(content, "EmptyLabel",
                $"No hay mejoras de tipo {category} en la base de datos.\nEjecuta: IdleFilm > Generate Upgrade Database",
                22, TEXT_DIM, TextAlignmentOptions.Center);
            LE(empty.GetComponent<RectTransform>(), 80);
            empty.textWrappingMode = TMPro.TextWrappingModes.Normal;
        }

        return panel;
    }

    static List<UpgradeConfig> LoadUpgradesByCategory(UpgradeCategory category)
    {
        var folder = category switch
        {
            UpgradeCategory.Equipment     => "Assets/Data/Upgrades/Equipment",
            UpgradeCategory.Personnel     => "Assets/Data/Upgrades/Personnel",
            UpgradeCategory.Installation  => "Assets/Data/Upgrades/Installations",
            UpgradeCategory.Marketing     => "Assets/Data/Upgrades/Marketing",
            UpgradeCategory.ContentFeature => "Assets/Data/Upgrades/ContentFeatures",
            _                             => "Assets/Data/Upgrades",
        };

        var guids  = AssetDatabase.FindAssets("t:UpgradeConfig", new[] { folder });
        var result = new List<UpgradeConfig>();
        foreach (var guid in guids)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UpgradeConfig>(
                AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) result.Add(asset);
        }
        result.Sort((a, b) => a.unlockStudioLevel.CompareTo(b.unlockStudioLevel));
        return result;
    }

    static void BuildUpgradeCard(RectTransform content, UpgradeConfig cfg)
    {
        ColorUtility.TryParseHtmlString(cfg.badgeColorHex, out Color badgeColor);

        var card = MakePanel(content, "Upgrade_" + cfg.id, BG_CARD2);
        LE(card, 100); SetRounded(card, 10);
        var vlg = card.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 10, 10);
        vlg.spacing = 5;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperLeft;

        // Top row: badge + name + level pill
        var top = MakeHGroup(card, "TopRow", 10, 0, 0, 0, 0);
        LE(top, 32);
        var badge = MakePanel(top, "Badge", badgeColor);
        LE(badge, 28, 0, 28, 28); SetRounded(badge, 5);
        var nameTxt = MakeText(top, "NameText", cfg.displayName, 22, TEXT_PRI, TextAlignmentOptions.MidlineLeft);
        nameTxt.fontStyle = FontStyles.Bold;
        LE(nameTxt.GetComponent<RectTransform>(), 28, 1f);
        var levelPill = MakeText(top, "LevelText", "Nv.0", 19, TEXT_SEC, TextAlignmentOptions.MidlineRight);
        LE(levelPill.GetComponent<RectTransform>(), 26, 0, 80);

        // Effect row
        var effectTxt = MakeText(card, "EffectText", BuildEffectPreview(cfg), 18, ACCENT_BLUE, TextAlignmentOptions.MidlineLeft);
        LE(effectTxt.GetComponent<RectTransform>(), 22);

        // Desc + cost + buy row
        var bottom = MakeHGroup(card, "BotRow", 10, 0, 0, 0, 0);
        LE(bottom, 36);
        var costTxt = MakeText(bottom, "CostText", "$" + cfg.baseCost, 19, ACCENT_GOLD, TextAlignmentOptions.MidlineLeft);
        LE(costTxt.GetComponent<RectTransform>(), 30, 1f);
        var btnGo = new GameObject("BuyBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(btnGo, "BuyBtn");
        btnGo.transform.SetParent(bottom, false);
        HudSkinProvider.ApplyButton(btnGo.GetComponent<Image>(), HudButtonVariant.Primary);
        SetRounded(btnGo.GetComponent<RectTransform>(), 8);
        LE(btnGo.GetComponent<RectTransform>(), 36, 0, 172, 36);
        btnGo.AddComponent<UIButtonScale>();
        var btnLbl = new GameObject("Lbl", typeof(RectTransform));
        btnLbl.transform.SetParent(btnGo.transform, false);
        Stretch(btnLbl.GetComponent<RectTransform>());
        var btnTMP = btnLbl.AddComponent<TextMeshProUGUI>();
        btnTMP.text = "MEJORAR"; btnTMP.fontSize = 17; btnTMP.fontStyle = FontStyles.Bold;
        btnTMP.alignment = TextAlignmentOptions.Center; btnTMP.color = TEXT_PRI;

        // Level bar under everything
        var lvlBarGo = new GameObject("LevelBar", typeof(RectTransform), typeof(Image), typeof(Slider));
        Undo.RegisterCreatedObjectUndo(lvlBarGo, "LevelBar");
        lvlBarGo.transform.SetParent(card, false);
        LE(lvlBarGo.GetComponent<RectTransform>(), 8);
        lvlBarGo.GetComponent<Image>().color = BG_SECTION;
        SetRounded(lvlBarGo.GetComponent<RectTransform>(), 4);
        var lvlSlider = lvlBarGo.GetComponent<Slider>();
        var lfArea = new GameObject("Fill Area", typeof(RectTransform)); lfArea.transform.SetParent(lvlBarGo.transform, false);
        var lfAreaRT = lfArea.GetComponent<RectTransform>();
        lfAreaRT.anchorMin = Vector2.zero; lfAreaRT.anchorMax = Vector2.one; lfAreaRT.offsetMin = lfAreaRT.offsetMax = Vector2.zero;
        var lfFill = new GameObject("Fill", typeof(RectTransform), typeof(Image)); lfFill.transform.SetParent(lfArea.transform, false);
        lfFill.GetComponent<Image>().color = badgeColor;
        var lfFillRT = lfFill.GetComponent<RectTransform>();
        lfFillRT.anchorMin = Vector2.zero; lfFillRT.anchorMax = Vector2.one; lfFillRT.offsetMin = lfFillRT.offsetMax = Vector2.zero;
        lvlSlider.fillRect = lfFillRT;

        // Wire UpgradeCardUI
        var upgradeUI = card.gameObject.AddComponent<UpgradeCardUI>();
        var so = new SerializedObject(upgradeUI);
        so.FindProperty("upgradeConfig").objectReferenceValue = cfg;
        so.FindProperty("nameText")     .objectReferenceValue = nameTxt;
        so.FindProperty("levelText")    .objectReferenceValue = levelPill;
        so.FindProperty("effectText")   .objectReferenceValue = effectTxt;
        so.FindProperty("costText")     .objectReferenceValue = costTxt;
        so.FindProperty("buyButton")    .objectReferenceValue = btnGo.GetComponent<Button>();
        so.FindProperty("badgeImage")   .objectReferenceValue = badge.GetComponent<Image>();
        so.FindProperty("levelBar")     .objectReferenceValue = lvlSlider;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(upgradeUI);
    }

    static string BuildEffectPreview(UpgradeConfig cfg)
    {
        if (cfg.effects == null || cfg.effects.Length == 0) return "";
        var sb = new System.Text.StringBuilder();
        foreach (var e in cfg.effects)
        {
            float v = e.valuePerLevel;
            sb.Append(e.type switch {
                UpgradeEffectType.Quality           => $"+{v:0.##} Cal  ",
                UpgradeEffectType.Speed             => $"+{v:0.##} Vel  ",
                UpgradeEffectType.CostReduction     => $"-{v*100:0.#}% Coste  ",
                UpgradeEffectType.ReputationBonus   => $"+{v*100:0.#}% REP  ",
                UpgradeEffectType.PassiveIncomeBonus=> $"+${v:0}/s  ",
                UpgradeEffectType.MaxMovieSlots     => $"+1 Slot  ",
                UpgradeEffectType.XPBonus           => $"+{v*100:0.#}% XP  ",
                _                                  => "",
            });
        }
        return sb.ToString().TrimEnd() + " por nivel";
    }

    // ─── Contratos Panel (runtime — active only + historial) ──────────────────
    static RectTransform BuildContratosPanel(RectTransform parent)
    {
        var panel = MakePanel(parent, "ContratosPanel", BG_DEEP);
        Stretch(panel);
        var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(8, 8, 4, 4);
        vlg.spacing = 4;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var hdr = MakeText(panel, "Hdr", "CONTRATOS", 16, ACCENT_BLUE, TextAlignmentOptions.MidlineLeft);
        LE(hdr.GetComponent<RectTransform>(), 22);

        // Active contracts scroll
        var activeScroll = MakeScrollRect(panel, "ActiveScroll", Color.clear);
        LE(activeScroll.GetComponent<RectTransform>(), 0, 1f);
        activeScroll.horizontal = false;
        activeScroll.vertical = true;
        var activeContent = activeScroll.content;
        var activeVLG = activeContent.gameObject.AddComponent<VerticalLayoutGroup>();
        activeVLG.spacing = 6;
        activeVLG.childControlWidth = activeVLG.childControlHeight = true;
        activeVLG.childForceExpandWidth = true;
        activeVLG.childForceExpandHeight = false;
        activeContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        var activeEmpty = MakeText(panel, "ActiveEmpty", "Sin contratos activos", 14, TEXT_DIM, TextAlignmentOptions.Center);
        LE(activeEmpty.GetComponent<RectTransform>(), 20);

        var histHdr = MakeText(panel, "HistHdr", "HISTORIAL", 14, TEXT_DIM, TextAlignmentOptions.MidlineLeft);
        LE(histHdr.GetComponent<RectTransform>(), 18);

        var histScroll = MakeScrollRect(panel, "HistoryScroll", Color.clear);
        LE(histScroll.GetComponent<RectTransform>(), 80);
        histScroll.horizontal = false;
        histScroll.vertical = true;
        var histContent = histScroll.content;
        var histVLG = histContent.gameObject.AddComponent<VerticalLayoutGroup>();
        histVLG.spacing = 4;
        histVLG.childControlWidth = histVLG.childControlHeight = true;
        histVLG.childForceExpandWidth = true;
        histVLG.childForceExpandHeight = false;
        histContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        var histEmpty = MakeText(panel, "HistEmpty", "Aún no hay contratos reclamados", 13, TEXT_DIM, TextAlignmentOptions.Center);
        LE(histEmpty.GetComponent<RectTransform>(), 18);

        var panelUI = panel.gameObject.AddComponent<ContractsPanelUI>();
        var pso = new SerializedObject(panelUI);
        pso.FindProperty("activeContent").objectReferenceValue = activeContent;
        pso.FindProperty("historyContent").objectReferenceValue = histContent;
        pso.FindProperty("activeEmptyLabel").objectReferenceValue = activeEmpty;
        pso.FindProperty("historyEmptyLabel").objectReferenceValue = histEmpty;
        pso.ApplyModifiedPropertiesWithoutUndo();

        return panel;
    }

    static List<ContractConfig> LoadContracts()
    {
        var guids  = AssetDatabase.FindAssets("t:ContractConfig", new[] { "Assets/Data/Contracts" });
        var result = new List<ContractConfig>();
        foreach (var guid in guids)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ContractConfig>(
                AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) result.Add(asset);
        }
        result.Sort((a, b) => a.unlockStudioLevel.CompareTo(b.unlockStudioLevel));
        return result;
    }

    static void BuildContractCard(RectTransform parent, ContractConfig cfg)
    {
        var card = MakePanel(parent, "Contract_" + cfg.id, BG_CARD2);
        LE(card, 120); SetRounded(card, 10);
        var vlg = card.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 10, 10);
        vlg.spacing = 6;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        // Title row
        var title = MakeText(card, "Title", cfg.contractTitle, 24, TEXT_PRI, TextAlignmentOptions.MidlineLeft);
        title.fontStyle = FontStyles.Bold;
        LE(title.GetComponent<RectTransform>(), 30);

        // Description
        var desc = MakeText(card, "Desc", cfg.description, 20, TEXT_SEC, TextAlignmentOptions.MidlineLeft);
        desc.textWrappingMode = TMPro.TextWrappingModes.Normal;
        LE(desc.GetComponent<RectTransform>(), 28);

        // Progress + reward row
        var bot = MakeHGroup(card, "BotRow", 10, 0, 0, 0, 0);
        LE(bot, 32);

        // Progress bar
        var progGo = new GameObject("ProgBar", typeof(RectTransform), typeof(Image), typeof(Slider));
        Undo.RegisterCreatedObjectUndo(progGo, "ProgBar");
        progGo.transform.SetParent(bot, false);
        LE(progGo.GetComponent<RectTransform>(), 12, 1f);
        progGo.GetComponent<Image>().color = BG_SECTION;
        SetRounded(progGo.GetComponent<RectTransform>(), 5);
        var slider = progGo.GetComponent<Slider>();
        var paArea = new GameObject("Fill Area", typeof(RectTransform)); paArea.transform.SetParent(progGo.transform, false);
        var paRT = paArea.GetComponent<RectTransform>(); paRT.anchorMin = Vector2.zero; paRT.anchorMax = Vector2.one; paRT.offsetMin = paRT.offsetMax = Vector2.zero;
        var paFill = new GameObject("Fill", typeof(RectTransform), typeof(Image)); paFill.transform.SetParent(paArea.transform, false);
        paFill.GetComponent<Image>().color = ACCENT_GREEN;
        var paFillRT = paFill.GetComponent<RectTransform>(); paFillRT.anchorMin = Vector2.zero; paFillRT.anchorMax = Vector2.one; paFillRT.offsetMin = paFillRT.offsetMax = Vector2.zero;
        slider.fillRect = paFillRT;

        var progTxt = MakeText(bot, "ProgText", $"0/{cfg.goalAmount:0}", 17, TEXT_SEC, TextAlignmentOptions.MidlineRight);
        LE(progTxt.GetComponent<RectTransform>(), 28, 0, 80);

        // Reward
        var rewardTxt = MakeText(card, "Reward", BuildRewardStr(cfg), 20, ACCENT_GOLD, TextAlignmentOptions.MidlineLeft);
        LE(rewardTxt.GetComponent<RectTransform>(), 26);

        // Claim button (hidden initially)
        var claimGo = new GameObject("ClaimBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(claimGo, "ClaimBtn");
        claimGo.transform.SetParent(card, false);
        HudSkinProvider.ApplyButton(claimGo.GetComponent<Image>(), HudButtonVariant.Primary);
        SetRounded(claimGo.GetComponent<RectTransform>(), 8);
        LE(claimGo.GetComponent<RectTransform>(), 44, 1f);
        var claimLbl = new GameObject("Lbl", typeof(RectTransform)); claimLbl.transform.SetParent(claimGo.transform, false);
        Stretch(claimLbl.GetComponent<RectTransform>());
        var claimTMP = claimLbl.AddComponent<TextMeshProUGUI>();
        claimTMP.text = "RECLAMAR"; claimTMP.fontSize = 20; claimTMP.fontStyle = FontStyles.Bold;
        claimTMP.alignment = TextAlignmentOptions.Center; claimTMP.color = TEXT_PRI;
        claimGo.SetActive(false);

        // Wire ContractCardUI
        var contractUI = card.gameObject.AddComponent<ContractCardUI>();
        var so = new SerializedObject(contractUI);
        so.FindProperty("contract")    .objectReferenceValue = cfg;
        so.FindProperty("titleText")   .objectReferenceValue = title;
        so.FindProperty("descText")    .objectReferenceValue = desc;
        so.FindProperty("progressText").objectReferenceValue = progTxt;
        so.FindProperty("progressBar") .objectReferenceValue = slider;
        so.FindProperty("rewardText")  .objectReferenceValue = rewardTxt;
        so.FindProperty("claimButton") .objectReferenceValue = claimGo.GetComponent<Button>();
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(contractUI);
    }

    static string BuildRewardStr(ContractConfig c)
    {
        var p = new List<string>();
        if (c.rewardMoney    > 0) p.Add($"${FormatMoney(c.rewardMoney)}");
        if (c.rewardDiamonds > 0) p.Add($"[D]{c.rewardDiamonds}");
        if (c.rewardReputation > 0) p.Add($"+{c.rewardReputation:0} REP");
        if (c.rewardStudioXP > 0) p.Add($"+{c.rewardStudioXP} XP");
        return "Recompensa: " + string.Join(" · ", p);
    }

    // ─── Department Info Panel (no upgrade buttons) ───────────────────────────
    static RectTransform BuildDeptInfoPanel(RectTransform parent)
    {
        var panel = MakePanel(parent, "DeptInfoPanel", BG_DEEP);
        Stretch(panel);

        var scroll = MakeScrollRect(panel, "Scroll", Color.clear);
        Stretch(scroll.GetComponent<RectTransform>());
        scroll.horizontal = false; scroll.vertical = true;

        var content = scroll.content;
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(16, 16, 12, 12);
        vlg.spacing = 8;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        // Header
        var hdr = MakeText(content, "Header", "ESTADO DE LOS DEPARTAMENTOS", 18, TEXT_DIM, TextAlignmentOptions.MidlineLeft);
        hdr.fontStyle = FontStyles.Bold;
        LE(hdr.GetComponent<RectTransform>(), 26);

        var depts = new (DepartmentType dt, string name, string effect, string color)[]
        {
            (DepartmentType.Director,       "Director",          "Calidad +0.20/nv",   "#8E44AD"),
            (DepartmentType.Actors,         "Actores",           "Calidad +0.20/nv",   "#E74C3C"),
            (DepartmentType.Editor,         "Editor",            "Calidad +0.15/nv",   "#9B59B6"),
            (DepartmentType.Cinematography, "Fotografía",        "Calidad +0.15/nv",   "#2980B9"),
            (DepartmentType.Sound,          "Sonido",            "Calidad +0.10/nv",   "#3498DB"),
            (DepartmentType.Makeup,         "Maquillaje",        "Calidad +0.10/nv",   "#E91E8C"),
            (DepartmentType.Costume,        "Vestuario",         "Calidad +0.10/nv",   "#C2185B"),
            (DepartmentType.Art,            "Arte",              "Calidad +0.10/nv",   "#F39C12"),
            (DepartmentType.Lighting,       "Iluminación",       "Velocidad +0.10/nv", "#E67E22"),
            (DepartmentType.Grip,           "Grip",              "Velocidad +0.10/nv", "#D35400"),
            (DepartmentType.Producer,       "Productor",         "Coste -3%/nv",       "#27AE60"),
        };

        foreach (var (dt, name, effect, hex) in depts)
            BuildDeptInfoRow(content, dt, name, effect, hex);

        return panel;
    }

    static void BuildDeptInfoRow(RectTransform parent, DepartmentType deptType,
        string name, string effect, string hexColor)
    {
        var row = MakePanel(parent, "DeptRow_" + name, BG_CARD);
        LE(row, 56); SetRounded(row, 8);
        var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(12, 12, 8, 8);
        hlg.spacing = 10; hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;

        var badge = MakePanel(row, "Badge", Hex(hexColor));
        LE(badge, 0, 0, 28, 28); SetRounded(badge, 5);
        var nameTxt = MakeText(row, "Name", name, 20, TEXT_PRI, TextAlignmentOptions.MidlineLeft);
        nameTxt.fontStyle = FontStyles.Bold;
        LE(nameTxt.GetComponent<RectTransform>(), 0, 1f);
        var effectTxt = MakeText(row, "Effect", effect, 17, TEXT_SEC, TextAlignmentOptions.MidlineRight);
        LE(effectTxt.GetComponent<RectTransform>(), 0, 0, 180);
        var lvlTxt = MakeText(row, "Level", "Nv.0", 17, ACCENT_GREEN, TextAlignmentOptions.MidlineRight);
        LE(lvlTxt.GetComponent<RectTransform>(), 0, 0, 60);

        var miniUI = row.gameObject.AddComponent<DepartmentMiniCardUI>();
        var so = new SerializedObject(miniUI);
        so.FindProperty("deptType")    .enumValueIndex      = (int)deptType;
        so.FindProperty("deptNameText").objectReferenceValue = nameTxt;
        so.FindProperty("levelText")   .objectReferenceValue = lvlTxt;
        so.FindProperty("effectText")  .objectReferenceValue = effectTxt;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(miniUI);
    }

    // ─── Películas / Ciudad / Tienda / Premios Placeholders ───────────────────
    static RectTransform BuildPlaceholderPanel(RectTransform parent, string objName,
        string title, string subtitle, Color accent)
    {
        var panel = MakePanel(parent, objName, BG_DEEP);
        var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = vlg.childForceExpandHeight = true;

        var icon = MakePanel(panel, "Icon", accent);
        LE(icon, 0, 0, 80, 80); SetRounded(icon, 20);
        MakeText(icon, "Lbl", title.Length >= 3 ? title.Substring(0, 3) : title, 26, TEXT_PRI, TextAlignmentOptions.Center);

        var titleTxt = MakeText(panel, "Title", title, 30, TEXT_PRI, TextAlignmentOptions.Center);
        titleTxt.fontStyle = FontStyles.Bold;
        LE(titleTxt.GetComponent<RectTransform>(), 40);

        var sub = MakeText(panel, "Subtitle", subtitle, 22, TEXT_DIM, TextAlignmentOptions.Center);
        sub.textWrappingMode = TMPro.TextWrappingModes.Normal;
        LE(sub.GetComponent<RectTransform>(), 80);

        return panel;
    }

    static RectTransform BuildPremiosPanel(RectTransform parent)
    {
        var panel = MakePanel(parent, "PremiosPanel", BG_DEEP);
        var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.spacing = 16;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        // Oscars counter (large, prominent)
        var oscCard = MakePanel(panel, "OscarsCard", BG_CARD2);
        LE(oscCard, 130); SetRounded(oscCard, 14);
        var ovlg = oscCard.gameObject.AddComponent<VerticalLayoutGroup>();
        ovlg.padding = new RectOffset(20, 20, 16, 16);
        ovlg.spacing = 6;
        ovlg.childAlignment = TextAnchor.MiddleCenter;
        ovlg.childControlWidth = ovlg.childControlHeight = true;
        ovlg.childForceExpandWidth = true; ovlg.childForceExpandHeight = false;
        MakeText(oscCard, "OscarsCount", "0 Oscars", 44, ACCENT_GOLD, TextAlignmentOptions.Center)
            .fontStyle = FontStyles.Bold;
        MakeText(oscCard, "MultText", "Instalación 1 · ×1.00", 20, TEXT_SEC, TextAlignmentOptions.Center);

        // Threshold progress card
        var thrCard = MakePanel(panel, "ThresholdCard", BG_CARD);
        LE(thrCard, 100); SetRounded(thrCard, 10);
        var tvlg = thrCard.gameObject.AddComponent<VerticalLayoutGroup>();
        tvlg.padding = new RectOffset(20, 20, 14, 14);
        tvlg.spacing = 6;
        tvlg.childControlWidth = tvlg.childControlHeight = true;
        tvlg.childForceExpandWidth = true; tvlg.childForceExpandHeight = false;
        MakeText(thrCard, "ThrLabel", "Próximo Oscar:", 16, TEXT_SEC, TextAlignmentOptions.Center);
        MakeText(thrCard, "ThrValue", "300 REP", 32, ACCENT_RED, TextAlignmentOptions.Center)
            .fontStyle = FontStyles.Bold;

        panel.gameObject.AddComponent<AwardsPanelUI>();

        return panel;
    }

    // ─── Bottom Nav ───────────────────────────────────────────────────────────
    /// <summary>
    /// Builds the 5-tab nav bar. Fills outButtons[] with the created Button components
    /// so the caller can wire them to a StudioHubUI for serializable click handling.
    /// Does NOT add any AddListener calls here — those belong in StudioHubUI.Start().
    /// </summary>
    static RectTransform BuildBottomNav(RectTransform parent, Button[] outButtons)
    {
        var nav = MakePanel(parent, "BottomNav", NAV_BG);
        var hlg = nav.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;
        hlg.childControlWidth     = hlg.childControlHeight     = true;

        var labels = MainHudTabLabels.BottomNav;
        var icons  = MainHudTabLabels.BottomIcons;
        var colors = new[] { ACCENT_BLUE, ACCENT_GREEN, ACCENT_GOLD, ACCENT_GOLD, ACCENT_PURPLE };

        for (int i = 0; i < 5; i++)
        {
            var tab = MakePanel(nav, "Tab_" + labels[i], Color.clear);
            var btn = tab.gameObject.AddComponent<Button>();
            outButtons[i] = btn;

            var tabVLG = tab.gameObject.AddComponent<VerticalLayoutGroup>();
            tabVLG.childAlignment = TextAnchor.MiddleCenter;
            tabVLG.padding = new RectOffset(4, 4, 6, 6);
            tabVLG.childControlWidth = tabVLG.childControlHeight = true;
            tabVLG.childForceExpandWidth = tabVLG.childForceExpandHeight = true;

            var ico = MakeText(tab, "Icon", icons[i], 22, colors[i], TextAlignmentOptions.Center);
            LE(ico.GetComponent<RectTransform>(), 26);
            var lbl = MakeText(tab, "Label", labels[i], 14, TEXT_DIM, TextAlignmentOptions.Center);
            lbl.fontStyle = FontStyles.Bold;
            LE(lbl.GetComponent<RectTransform>(), 18);

            // Indicator line at bottom
            var indicator = MakePanel(tab, "ActiveLine", i == 0 ? colors[i] : Color.clear);
            LE(indicator, 3);
            SetRounded(indicator, 2);
        }

        return nav;
    }

    // ─── Peliculas Panel (stub – movie grid via MovieGridUI) ──────────────────
    // Full movie grid uses MovieGridUI which requires Resources/Movies/ folder.
    // Activated from BottomNav "Películas" (currently shows placeholder).

    // ─── Helpers ──────────────────────────────────────────────────────────────

    static RectTransform MakePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, name);
        go.transform.SetParent(parent, false);
        HudSkinProvider.ApplyPanelFromColor(go.GetComponent<Image>(), color);
        return go.GetComponent<RectTransform>();
    }

    static TextMeshProUGUI MakeText(RectTransform parent, string name, string text,
        float size, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color; tmp.alignment = align;
        tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Truncate;
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        return tmp;
    }

    static ScrollRect MakeScrollRect(RectTransform parent, string name, Color bgColor)
    {
        var scrollGo = new GameObject(name, typeof(RectTransform), typeof(ScrollRect));
        Undo.RegisterCreatedObjectUndo(scrollGo, name);
        scrollGo.transform.SetParent(parent, false);
        Stretch(scrollGo.GetComponent<RectTransform>());

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scrollGo.transform, false);
        Stretch(viewport.GetComponent<RectTransform>());

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1); contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot     = new Vector2(0.5f, 1);
        contentRT.offsetMin = contentRT.offsetMax = Vector2.zero;
        contentRT.sizeDelta = Vector2.zero;

        var sr = scrollGo.GetComponent<ScrollRect>();
        sr.viewport = viewport.GetComponent<RectTransform>();
        sr.content  = contentRT;
        sr.horizontal = false; sr.vertical = true;
        sr.scrollSensitivity = 30f;

        return sr;
    }

    static RectTransform BuildCompactSquareTabRow(RectTransform parent, string rowName,
        string[] labels, Color[] colors)
    {
        var row = MakePanel(parent, rowName, BG_CARD);
        var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(2, 2, 2, 2);
        hlg.spacing = 3;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = false;

        var layout = row.gameObject.AddComponent<SquareTileRowLayout>();
        layout.tileCount = labels.Length;
        layout.maxTileSize = 34f;

        for (int i = 0; i < labels.Length; i++)
        {
            var tab = MakePanel(row, "Tab_" + labels[i], i == 0 ? BG_CARD2 : BG_CARD);
            SetRounded(tab, 4);
            tab.gameObject.AddComponent<Button>();
            tab.gameObject.AddComponent<UIButtonScale>();
            var vlg = tab.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(0, 0, 2, 0);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = vlg.childControlHeight = true;
            vlg.childForceExpandWidth = vlg.childForceExpandHeight = true;

            var txt = MakeText(tab, "Label", labels[i], 10,
                i == 0 ? TEXT_PRI : TEXT_DIM, TextAlignmentOptions.Center);
            txt.fontStyle = FontStyles.Bold;

            var line = MakePanel(tab, "Line", i == 0 ? colors[i] : Color.clear);
            LE(line, 2);
        }

        return row;
    }

    static RectTransform BuildFullWidthTabRow(RectTransform parent, string rowName,
        string[] labels, Color[] colors, float height)
    {
        var row = MakePanel(parent, rowName, BG_CARD);
        var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.spacing = 4;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        for (int i = 0; i < labels.Length; i++)
        {
            var tab = MakePanel(row, "Tab_" + labels[i], i == 0 ? BG_CARD2 : BG_CARD);
            SetRounded(tab, 6);
            var tabLE = tab.gameObject.GetComponent<LayoutElement>() ?? tab.gameObject.AddComponent<LayoutElement>();
            tabLE.flexibleWidth = 1f;
            tabLE.preferredHeight = height;
            tabLE.minHeight = height;

            tab.gameObject.AddComponent<Button>();
            tab.gameObject.AddComponent<UIButtonScale>();
            var vlg = tab.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(2, 2, 4, 2);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = vlg.childControlHeight = true;
            vlg.childForceExpandWidth = vlg.childForceExpandHeight = true;

            var txt = MakeText(tab, "Label", labels[i], 12,
                i == 0 ? TEXT_PRI : TEXT_DIM, TextAlignmentOptions.Center);
            txt.fontStyle = FontStyles.Bold;

            var line = MakePanel(tab, "Line", i == 0 ? colors[i] : Color.clear);
            LE(line, 3);
        }

        return row;
    }

    static RectTransform BuildSquareTabRow(RectTransform parent, string rowName,
        string[] labels, Color[] colors, float size)
    {
        var row = MakePanel(parent, rowName, BG_CARD);
        var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(4, 4, 4, 4);
        hlg.spacing = 6;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        for (int i = 0; i < labels.Length; i++)
        {
            var tab = MakePanel(row, "Tab_" + labels[i], i == 0 ? BG_CARD2 : BG_CARD);
            LE(tab, size, 0, size, size);
            SetRounded(tab, 6);
            tab.gameObject.AddComponent<Button>();
            tab.gameObject.AddComponent<UIButtonScale>();
            var vlg = tab.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = vlg.childControlHeight = true;
            vlg.childForceExpandWidth = vlg.childForceExpandHeight = true;

            var txt = MakeText(tab, "Label", labels[i], 11,
                i == 0 ? TEXT_PRI : TEXT_DIM, TextAlignmentOptions.Center);
            txt.fontStyle = FontStyles.Bold;

            var line = MakePanel(tab, "Line", i == 0 ? colors[i] : Color.clear);
            LE(line, 2);
        }

        var flex = MakePanel(row, "FlexSpacer", Color.clear);
        var flexLE = flex.gameObject.AddComponent<LayoutElement>();
        flexLE.flexibleWidth = 1f;
        flexLE.preferredHeight = size;

        return row;
    }

    static RectTransform BuildTabButtonRow(RectTransform parent, string rowName,
        string[] labels, Color[] colors, float height)
    {
        var row = MakePanel(parent, rowName, BG_CARD);
        var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;
        hlg.childControlWidth     = hlg.childControlHeight     = true;

        for (int i = 0; i < labels.Length; i++)
        {
            var tab = MakePanel(row, "Tab_" + labels[i], i == 0 ? BG_CARD2 : BG_CARD);
            var btn = tab.gameObject.AddComponent<Button>();
            var vlg = tab.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = vlg.childControlHeight = true;
            vlg.childForceExpandWidth = vlg.childForceExpandHeight = true;

            var txt = MakeText(tab, "Label", labels[i], 17, i == 0 ? TEXT_PRI : TEXT_DIM,
                TextAlignmentOptions.Center);
            txt.fontStyle = FontStyles.Bold;

            // Active line
            var line = MakePanel(tab, "Line", i == 0 ? colors[i] : Color.clear);
            LE(line, 3);
        }

        return row;
    }

    static RectTransform MakeHGroup(RectTransform parent, string name,
        float spacing, int padL, int padR, int padT, int padB)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, name);
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = Color.clear;
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = spacing;
        hlg.padding = new RectOffset(padL, padR, padT, padB);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = false;
        return go.GetComponent<RectTransform>();
    }

    static void SetFlexWidth(RectTransform rt)
    {
        var le = rt.gameObject.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;
    }

    static void LE(RectTransform rt, float preferredH = 0, float flexH = 0,
        float preferredW = 0, float minH = 0)
    {
        var le = rt.gameObject.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
        if (preferredH > 0) le.preferredHeight = preferredH;
        if (minH > 0)       le.minHeight       = minH;
        if (flexH > 0)      le.flexibleHeight  = flexH;
        if (preferredW > 0)
        {
            le.preferredWidth  = preferredW;
            le.flexibleWidth   = 0f;
        }
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    /// <summary>Tab panels must fill their parent — default MakePanel size is 100×100 centered.</summary>
    static void FillParent(RectTransform rt)
    {
        Stretch(rt);
        var le = rt.gameObject.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
        le.flexibleWidth  = 1f;
        le.flexibleHeight = 1f;
    }

    static void AnchorTopBar(RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, height);
        rt.anchoredPosition = Vector2.zero;
        StripLayoutElement(rt);
    }

    static void AnchorBottomBar(RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(0f, height);
        rt.anchoredPosition = Vector2.zero;
        StripLayoutElement(rt);
    }

    static void AnchorBetweenBars(RectTransform rt, float topInset, float bottomInset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(0f, bottomInset);
        rt.offsetMax = new Vector2(0f, -topInset);
        rt.anchoredPosition = Vector2.zero;
        StripLayoutElement(rt);
    }

    static void StripLayoutElement(RectTransform rt)
    {
        var le = rt.GetComponent<LayoutElement>();
        if (le != null) Undo.DestroyObjectImmediate(le);
    }

    static void SetRounded(RectTransform rt, float radius)
    {
        // Placeholder: Unity doesn't natively support corner radius in Image.
        // Sprite replacement will be done via art pipeline.
        _ = rt; _ = radius;
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }

    static string FormatMoney(long v) => AnimatedMoneyText.FormatMoney(v).TrimStart('$');

    // ─── System GameObjects ───────────────────────────────────────────────────
    static void WireGameSystems(RectTransform root)
    {
        var hub = Object.FindAnyObjectByType<GameHub>();
        if (hub == null)
        {
            var hubGo = new GameObject("GameHub", typeof(GameHub));
            Undo.RegisterCreatedObjectUndo(hubGo, "GameHub");
            hub = hubGo.GetComponent<GameHub>();
        }

        EnsureComponent<StudioManager>(hub.gameObject,     ref hub.studio);
        EnsureComponent<DepartmentSystem>(hub.gameObject,  ref hub.departments);
        EnsureComponent<PrestigeSystem>(hub.gameObject,    ref hub.prestige);
        EnsureComponent<SaveSystem>(hub.gameObject,        ref hub.save);
        EnsureComponent<UpgradeSystem>(hub.gameObject,     ref hub.upgrades);
        EnsureComponent<StudioLevelSystem>(hub.gameObject, ref hub.studioLevel);
        EnsureComponent<ContractSystem>(hub.gameObject,    ref hub.contracts);

        // Populate UpgradeSystem.allUpgrades from ScriptableObject assets
        PopulateUpgradeAssets(hub.upgrades);

        // Populate ContractSystem.allContracts from ScriptableObject assets
        PopulateContractAssets(hub.contracts);

        EditorUtility.SetDirty(hub);
    }

    static void PopulateUpgradeAssets(UpgradeSystem upgradeSystem)
    {
        if (upgradeSystem == null) return;
        var guids  = AssetDatabase.FindAssets("t:UpgradeConfig", new[] { "Assets/Data/Upgrades" });
        var assets = new UpgradeConfig[guids.Length];
        for (int i = 0; i < guids.Length; i++)
            assets[i] = AssetDatabase.LoadAssetAtPath<UpgradeConfig>(AssetDatabase.GUIDToAssetPath(guids[i]));
        // Sort: Equipment → Personnel → Installation → Marketing, then by unlockStudioLevel
        System.Array.Sort(assets, (a, b) =>
        {
            int catCmp = ((int)a.category).CompareTo((int)b.category);
            return catCmp != 0 ? catCmp : a.unlockStudioLevel.CompareTo(b.unlockStudioLevel);
        });
        var so = new SerializedObject(upgradeSystem);
        var prop = so.FindProperty("allUpgrades");
        prop.arraySize = assets.Length;
        for (int i = 0; i < assets.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(upgradeSystem);
        Debug.Log($"[Builder] UpgradeSystem: {assets.Length} upgrade assets assigned.");
    }

    static void PopulateContractAssets(ContractSystem contractSystem)
    {
        if (contractSystem == null) return;
        var guids  = AssetDatabase.FindAssets("t:ContractConfig", new[] { "Assets/Data/Contracts" });
        var assets = new ContractConfig[guids.Length];
        for (int i = 0; i < guids.Length; i++)
            assets[i] = AssetDatabase.LoadAssetAtPath<ContractConfig>(AssetDatabase.GUIDToAssetPath(guids[i]));
        System.Array.Sort(assets, (a, b) => a.unlockStudioLevel.CompareTo(b.unlockStudioLevel));
        var so = new SerializedObject(contractSystem);
        var prop = so.FindProperty("allContracts");
        prop.arraySize = assets.Length;
        for (int i = 0; i < assets.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(contractSystem);
        Debug.Log($"[Builder] ContractSystem: {assets.Length} contract assets assigned.");
    }

    static void EnsureComponent<T>(GameObject go, ref T field) where T : Component
    {
        if (field == null)
        {
            field = go.GetComponent<T>() ?? go.AddComponent<T>();
            EditorUtility.SetDirty(go);
        }
    }

    static void WireGameFeel(RectTransform root, RectTransform topBar)
    {
        var feel = root.gameObject.GetComponent<GameFeelUI>() ?? root.gameObject.AddComponent<GameFeelUI>();
        var fso = new SerializedObject(feel);
        fso.FindProperty("popupParent").objectReferenceValue = root;

        var prod = GameObject.Find("ProductionWidget");
        if (prod != null)
            fso.FindProperty("productionWidget").objectReferenceValue = prod.GetComponent<RectTransform>();

        var topBarUI = topBar != null ? topBar.GetComponent<TopBarUI>() : null;
        if (topBarUI != null)
            fso.FindProperty("oscarTarget").objectReferenceValue = topBarUI.GetOscarRect();

        fso.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(feel);
    }

    // ─── Legacy cleanup ───────────────────────────────────────────────────────
    static void DestroyLegacySceneRoots()
    {
        var legacyNames = new[]
        {
            "HubRoot", "StudioHub",
            "TopBar", "DepartmentPanel", "ProductionPanel",
            "MejorasPanel", "BottomBar", "CurrentProductionPanel",
        };
        foreach (var n in legacyNames)
        {
            var go = GameObject.Find(n);
            if (go != null && go.GetComponent<Canvas>() == null)
                Undo.DestroyObjectImmediate(go);
        }
    }

    static void CleanupOrphanRoots()
    {
        foreach (var name in new[] { "MovieUIManager", "UIManager" })
        {
            var go = GameObject.Find(name);
            if (go != null) Undo.DestroyObjectImmediate(go);
        }
    }

    static Canvas FindCanvas()
    {
        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null) return canvas;

        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler),
                                typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(go, "Canvas");
        var newCanvas = go.GetComponent<Canvas>();
        newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        return newCanvas;
    }

    /// <summary>Canvas scale 0 makes the entire UI invisible — reset every build.</summary>
    static void NormalizeCanvas(Canvas canvas)
    {
        var t = canvas.transform;
        t.localScale = Vector3.one;
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;

        var rt = canvas.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.enabled    = true;

        var cs = canvas.GetComponent<CanvasScaler>();
        if (cs != null)
        {
            cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight  = 0.5f;
        }

        if (canvas.GetComponent<GraphicRaycaster>() == null)
            Undo.AddComponent<GraphicRaycaster>(canvas.gameObject);
    }

    /// <summary>UI buttons require an EventSystem + input module (New Input System in this project).</summary>
    static void EnsureEventSystem()
    {
        var existing = Object.FindAnyObjectByType<EventSystem>();
        if (existing != null)
        {
            EnsureInputModule(existing.gameObject);
            return;
        }

        var go = new GameObject("EventSystem", typeof(EventSystem));
        Undo.RegisterCreatedObjectUndo(go, "EventSystem");
        EnsureInputModule(go);
        Debug.Log("[Builder] EventSystem created.");
    }

    static void EnsureInputModule(GameObject eventSystemGo)
    {
        var inputSystemModule = System.Type.GetType(
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemModule != null)
        {
            if (eventSystemGo.GetComponent(inputSystemModule) == null)
            {
                Undo.AddComponent(eventSystemGo, inputSystemModule);
                Debug.Log("[Builder] InputSystemUIInputModule added.");
            }
            // Remove legacy module if both exist
            var legacy = eventSystemGo.GetComponent<StandaloneInputModule>();
            if (legacy != null) Undo.DestroyObjectImmediate(legacy);
            return;
        }

        if (eventSystemGo.GetComponent<StandaloneInputModule>() == null)
            Undo.AddComponent<StandaloneInputModule>(eventSystemGo);
    }
}
#endif
