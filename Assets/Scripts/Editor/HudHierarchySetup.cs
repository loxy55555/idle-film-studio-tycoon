#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class HudHierarchySetup
{
    private const string MenuPath = "IdleFilm/Setup HUD Hierarchy";

    [MenuItem(MenuPath)]
    public static void SetupHudHierarchy()
    {
        Canvas canvas = ResolveCanvas();
        if (canvas == null)
        {
            Debug.LogError("HudHierarchySetup: no se encontró Canvas.");
            return;
        }

        Undo.SetCurrentGroupName("Setup HUD Hierarchy");
        int group = Undo.GetCurrentGroup();

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        SetupFullStretch(canvasRect, Vector2.zero, Vector2.zero);

        Transform topBar = EnsureChild(canvasRect, "TopBar");
        Transform mainArea = EnsureChild(canvasRect, "MainArea");
        Transform bottomBar = EnsureChild(canvasRect, "BottomBar");

        SetupTopBar(topBar as RectTransform);
        SetupBottomBar(bottomBar as RectTransform);
        SetupMainArea(mainArea as RectTransform);

        Transform productionPanel = EnsureChild(mainArea, "ProductionPanel");
        Transform currentProductionPanel = EnsureChild(mainArea, "CurrentProductionPanel");
        Transform departmentPanel = EnsureChild(mainArea, "DepartmentPanel");

        MigrateLegacyPanels(canvasRect, productionPanel, currentProductionPanel, departmentPanel);

        SetupProductionPanel(productionPanel as RectTransform);
        SetupCurrentProductionPanel(currentProductionPanel as RectTransform);
        SetupDepartmentPanel(departmentPanel as RectTransform);

        currentProductionPanel.SetSiblingIndex(0);
        productionPanel.SetSiblingIndex(1);
        departmentPanel.SetSiblingIndex(2);

        CleanupCanvas(canvasRect);
        SetupUIManager(canvas, productionPanel, currentProductionPanel, departmentPanel);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Undo.CollapseUndoOperations(group);

        Debug.Log("HUD Hierarchy configurado según estructura obligatoria.");
    }

    private static Canvas ResolveCanvas()
    {
        GameObject uiRoot = GameObject.Find("UI");
        GameObject canvasGo = GameObject.Find("Canvas");

        if (uiRoot != null && canvasGo != null && canvasGo.transform.parent == uiRoot.transform)
        {
            Canvas innerCanvas = canvasGo.GetComponent<Canvas>();
            if (innerCanvas != null)
            {
                Undo.SetTransformParent(canvasGo.transform, null, "Promote Canvas");
                canvasGo.transform.SetAsFirstSibling();
            }
        }

        if (canvasGo != null)
            return canvasGo.GetComponent<Canvas>();

        if (uiRoot != null)
        {
            Canvas c = uiRoot.GetComponent<Canvas>();
            if (c != null)
            {
                uiRoot.name = "Canvas";
                return c;
            }
        }

        return Object.FindAnyObjectByType<Canvas>();
    }

    private static void MigrateLegacyPanels(
        RectTransform canvasRect,
        Transform productionPanel,
        Transform currentProductionPanel,
        Transform departmentPanel)
    {
        Transform moviePanel = canvasRect.Find("MoviePanel");
        if (moviePanel != null && moviePanel != productionPanel)
        {
            Transform scrollView = moviePanel.Find("Scroll View");
            if (scrollView != null)
            {
                Undo.SetTransformParent(scrollView, productionPanel, "Move Scroll View");
                scrollView.name = "MovieScrollView";
            }

            Transform status = moviePanel.Find("MovieStatusText");
            if (status != null)
                Undo.SetTransformParent(status, currentProductionPanel, "Move MovieStatusText");

            Transform progress = moviePanel.Find("MovieProgressBar");
            if (progress != null)
                Undo.SetTransformParent(progress, currentProductionPanel, "Move MovieProgressBar");

            Undo.DestroyObjectImmediate(moviePanel.gameObject);
        }

        Transform staffPanel = canvasRect.Find("StaffPanel");
        if (staffPanel != null && staffPanel != departmentPanel)
        {
            while (staffPanel.childCount > 0)
                Undo.SetTransformParent(staffPanel.GetChild(0), departmentPanel, "Move Staff child");

            Undo.DestroyObjectImmediate(staffPanel.gameObject);
        }

        Transform debugPanel = canvasRect.Find("DebugPanel");
        if (debugPanel != null)
            debugPanel.gameObject.SetActive(false);
    }

    private static void SetupTopBar(RectTransform topBar)
    {
        SetupTopAnchorStretch(topBar, 140f);

        if (topBar.GetComponent<HorizontalLayoutGroup>() == null)
        {
            HorizontalLayoutGroup layout = Undo.AddComponent<HorizontalLayoutGroup>(topBar.gameObject);
            layout.padding = new RectOffset(20, 20, 10, 10);
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        if (topBar.GetComponent<TopBarUI>() == null)
            Undo.AddComponent<TopBarUI>(topBar.gameObject);
    }

    private static void SetupBottomBar(RectTransform bottomBar)
    {
        SetupBottomAnchorStretch(bottomBar, 120f);

        if (bottomBar.GetComponent<HorizontalLayoutGroup>() == null)
        {
            HorizontalLayoutGroup layout = Undo.AddComponent<HorizontalLayoutGroup>(bottomBar.gameObject);
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
        }

        BottomBarUI bottomBarUI = bottomBar.GetComponent<BottomBarUI>();
        if (bottomBarUI == null)
            bottomBarUI = Undo.AddComponent<BottomBarUI>(bottomBar.gameObject);

        // NOTA: posible mejora — reemplazar botones legacy por tabs de navegación dedicados.
        DeactivateLegacyBottomButtons(bottomBar);
        WireBottomBarTabs(bottomBarUI, bottomBar);
    }

    private static void WireBottomBarTabs(BottomBarUI bottomBarUI, RectTransform bottomBar)
    {
        Button[] buttons = bottomBar.GetComponentsInChildren<Button>(true);
        int activeIndex = 0;

        SerializedObject serialized = new SerializedObject(bottomBarUI);

        foreach (Button button in buttons)
        {
            if (!button.gameObject.activeInHierarchy)
                continue;

            if (activeIndex == 0)
                serialized.FindProperty("productionTabButton").objectReferenceValue = button;
            else if (activeIndex == 1)
                serialized.FindProperty("currentProductionTabButton").objectReferenceValue = button;
            else if (activeIndex == 2)
                serialized.FindProperty("departmentTabButton").objectReferenceValue = button;

            activeIndex++;
            if (activeIndex >= 3)
                break;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void DeactivateLegacyBottomButtons(RectTransform bottomBar)
    {
        string[] legacyNames =
        {
            "Movie_Small_Button",
            "Movier_Medium_Button",
            "Movie_Big_Button",
            "DebugAddMoneyButton"
        };

        foreach (string legacyName in legacyNames)
        {
            Transform legacy = bottomBar.Find(legacyName);
            if (legacy != null)
                legacy.gameObject.SetActive(false);
        }
    }

    private static void SetupMainArea(RectTransform mainArea)
    {
        SetupMiddleStretch(mainArea, 140f, 120f);

        VerticalLayoutGroup layout = mainArea.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = Undo.AddComponent<VerticalLayoutGroup>(mainArea.gameObject);

        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
    }

    private static void SetupProductionPanel(RectTransform productionPanel)
    {
        AddLayoutElement(productionPanel.gameObject, flexibleHeight: 1f, minHeight: 200f);

        Transform scrollView = productionPanel.Find("MovieScrollView");
        if (scrollView == null)
            scrollView = EnsureChild(productionPanel, "MovieScrollView");

        RectTransform scrollRect = scrollView as RectTransform;
        SetupFullStretch(scrollRect, Vector2.zero, Vector2.zero);

        ScrollRect scroll = scrollView.GetComponent<ScrollRect>();
        if (scroll == null)
            scroll = Undo.AddComponent<ScrollRect>(scrollView.gameObject);

        Transform viewport = scrollView.Find("Viewport");
        if (viewport == null)
            viewport = EnsureChild(scrollView, "Viewport");

        SetupFullStretch(viewport as RectTransform, Vector2.zero, Vector2.zero);

        if (viewport.GetComponent<Mask>() == null)
            Undo.AddComponent<Mask>(viewport.gameObject);

        if (viewport.GetComponent<Image>() == null)
            Undo.AddComponent<Image>(viewport.gameObject);

        Transform content = viewport.Find("Content");
        if (content == null)
            content = EnsureChild(viewport, "Content");

        RectTransform contentRect = content as RectTransform;
        SetupContentForVerticalScroll(contentRect);

        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        if (grid != null)
            Undo.DestroyObjectImmediate(grid);

        Transform gridChild = content.Find("Grid");
        if (gridChild != null)
            Undo.DestroyObjectImmediate(gridChild.gameObject);

        scroll.content = contentRect;
        scroll.viewport = viewport as RectTransform;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
    }

    private static void SetupCurrentProductionPanel(RectTransform currentProductionPanel)
    {
        AddLayoutElement(currentProductionPanel.gameObject, flexibleHeight: 0f, minHeight: 180f);

        VerticalLayoutGroup layout = currentProductionPanel.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = Undo.AddComponent<VerticalLayoutGroup>(currentProductionPanel.gameObject);

        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        if (currentProductionPanel.GetComponent<CurrentProductionPanelUI>() == null)
            Undo.AddComponent<CurrentProductionPanelUI>(currentProductionPanel.gameObject);
    }

    private static void SetupDepartmentPanel(RectTransform departmentPanel)
    {
        AddLayoutElement(departmentPanel.gameObject, flexibleHeight: 1f, minHeight: 200f);

        VerticalLayoutGroup layout = departmentPanel.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = Undo.AddComponent<VerticalLayoutGroup>(departmentPanel.gameObject);

        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static void SetupContentForVerticalScroll(RectTransform content)
    {
        SetupTopStretch(content);

        VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
            vlg = Undo.AddComponent<VerticalLayoutGroup>(content.gameObject);

        vlg.padding = new RectOffset(5, 5, 5, 5);
        vlg.spacing = 8;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = Undo.AddComponent<ContentSizeFitter>(content.gameObject);

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static void CleanupCanvas(RectTransform canvasRect)
    {
        for (int i = canvasRect.childCount - 1; i >= 0; i--)
        {
            Transform child = canvasRect.GetChild(i);
            string name = child.name;

            if (name is "TopBar" or "MainArea" or "BottomBar")
                continue;

            if (name == "MovieButtonPrefab")
            {
                Undo.DestroyObjectImmediate(child.gameObject);
                continue;
            }

            if (name is "DebugPanel" or "MoviePanel" or "StaffPanel")
            {
                Undo.DestroyObjectImmediate(child.gameObject);
                continue;
            }
        }

        GameObject uiRoot = GameObject.Find("UI");
        if (uiRoot != null && uiRoot.GetComponent<Canvas>() != null)
        {
            Transform moneyDisplay = uiRoot.transform.Find("MoneyDisplay");
            if (moneyDisplay != null)
                moneyDisplay.gameObject.SetActive(false);

            if (uiRoot.transform.childCount == 0 || uiRoot.name != "Canvas")
                uiRoot.SetActive(false);
        }
    }

    private static void SetupUIManager(
        Canvas canvas,
        Transform productionPanel,
        Transform currentProductionPanel,
        Transform departmentPanel)
    {
        GameObject uiManagerGo = GameObject.Find("UIManager");
        if (uiManagerGo == null)
            uiManagerGo = new GameObject("UIManager");

        Undo.RegisterCreatedObjectUndo(uiManagerGo, "Create UIManager");

        UIManager uiManager = uiManagerGo.GetComponent<UIManager>();
        if (uiManager == null)
            uiManager = Undo.AddComponent<UIManager>(uiManagerGo);

        MovieUIManager movieUi = uiManagerGo.GetComponent<MovieUIManager>();
        if (movieUi == null)
            movieUi = Undo.AddComponent<MovieUIManager>(uiManagerGo);

        MovieUIManager existingMovieUi = Object.FindAnyObjectByType<MovieUIManager>();
        if (existingMovieUi != null && existingMovieUi != movieUi)
        {
            movieUi.studio = existingMovieUi.studio;
            movieUi.container = existingMovieUi.container;
            movieUi.buttonPrefab = existingMovieUi.buttonPrefab;
            movieUi.movies = new System.Collections.Generic.List<MovieConfig>(existingMovieUi.movies);
            Undo.DestroyObjectImmediate(existingMovieUi.gameObject);
        }

        Transform content = productionPanel.Find("MovieScrollView/Viewport/Content");
        if (content != null)
            movieUi.container = content as RectTransform;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MovieButtonPrefab.prefab");
        if (prefab != null)
            movieUi.buttonPrefab = prefab;

        StudioManager studio = Object.FindAnyObjectByType<StudioManager>();
        if (studio != null)
            movieUi.studio = studio;

        uiManager.currentProductionPanel = currentProductionPanel.gameObject;
        uiManager.productionPanel = productionPanel.gameObject;
        uiManager.departmentPanel = departmentPanel.gameObject;
        uiManager.movieUIManager = movieUi;
        uiManager.topBarUI = canvas.transform.Find("TopBar")?.GetComponent<TopBarUI>();
        uiManager.currentProductionPanelUI = currentProductionPanel.GetComponent<CurrentProductionPanelUI>();
        uiManager.bottomBarUI = canvas.transform.Find("BottomBar")?.GetComponent<BottomBarUI>();

        EditorUtility.SetDirty(uiManager);
        EditorUtility.SetDirty(movieUi);
    }

    private static Transform EnsureChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
            return child;

        GameObject go = new GameObject(childName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + childName);
        Undo.SetTransformParent(go.transform, parent, "Parent " + childName);
        go.layer = parent.gameObject.layer;
        return go.transform;
    }

    private static void AddLayoutElement(GameObject go, float flexibleHeight, float minHeight)
    {
        LayoutElement element = go.GetComponent<LayoutElement>();
        if (element == null)
            element = Undo.AddComponent<LayoutElement>(go);

        element.minHeight = minHeight;
        element.flexibleHeight = flexibleHeight;
        element.flexibleWidth = 1f;
    }

    private static void SetupFullStretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;
    }

    private static void SetupTopAnchorStretch(RectTransform rect, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, height);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void SetupBottomAnchorStretch(RectTransform rect, float height)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(0f, height);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void SetupMiddleStretch(RectTransform rect, float topOffset, float bottomOffset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(0f, bottomOffset);
        rect.offsetMax = new Vector2(0f, -topOffset);
        rect.localScale = Vector3.one;
    }

    private static void SetupTopStretch(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 0f);
        rect.localScale = Vector3.one;
    }
}
#endif
