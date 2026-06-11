using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Builds or migrates the definitive HUD structure (shared by editor bake and legacy runtime migration).</summary>
public static class HudDefinitiveStructureBuilder
{
    public static bool Apply(RectTransform switcher, bool showProductionTab = true)
    {
        if (switcher == null) return false;

        if (HudBakedSceneRules.IsBaked(switcher))
            return true;

        var estudioPanel   = FindChildPanel(switcher, "EstudioPanel");
        var peliculasPanel = FindChildPanel(switcher, "ProduccionPanel", "PeliculasPanel");
        var premiosPanel   = FindChildPanel(switcher, "PremiosPanel");
        var menuPanel      = FindChildPanel(switcher, "MenuPanel", "TiendaPanel");

        if (estudioPanel == null || peliculasPanel == null || premiosPanel == null)
            return false;

        if (menuPanel == null)
            menuPanel = CreateMenuPanel(switcher);

        var collectionPanel = FindChildPanel(switcher, "ColeccionPanel");
        if (collectionPanel == null)
            collectionPanel = CreateCollectionPanel(switcher);

        RestructureProductionPanel(peliculasPanel, estudioPanel);
        RestructureStudioPanel(estudioPanel);
        RestructurePremiosPanel(premiosPanel);
        RestructureCollectionPanel(collectionPanel, peliculasPanel);
        RestructureMenuPanel(menuPanel);

        var mainHub = switcher.GetComponent<StudioHubUI>() ?? switcher.gameObject.AddComponent<StudioHubUI>();
        mainHub.tabPanels = new[]
        {
            estudioPanel.gameObject,
            peliculasPanel.gameObject,
            premiosPanel.gameObject,
            collectionPanel.gameObject,
            menuPanel.gameObject,
        };

        HudNavigationCleanup.Run(switcher, mainHub);
        HudLayoutPass.Apply(switcher, mainHub);

        var shell = switcher.GetComponent<DefinitiveHudShell>() ?? switcher.gameObject.AddComponent<DefinitiveHudShell>();
        shell.mainNavigation = mainHub;
        shell.productionPanel = peliculasPanel;
        shell.studioPanel = estudioPanel;
        shell.awardsPanel = premiosPanel;
        shell.collectionPanel = collectionPanel;
        shell.menuPanel = menuPanel;
        shell.appliedMigrationVersion = DefinitiveHudShell.MigrationVersion;

        if (showProductionTab && Application.isPlaying)
            mainHub.ShowTab((int)MainHudTab.Production, instant: true);

        return true;
    }

    static RectTransform FindChildPanel(RectTransform parent, params string[] names)
    {
        foreach (var name in names)
        {
            var child = parent.Find(name) as RectTransform;
            if (child != null) return child;
        }

        return null;
    }

    static void RestructureProductionPanel(RectTransform panel, RectTransform estudioPanel)
    {
        panel.name = "ProduccionPanel";

        if (panel.GetComponent<ProductionHudShell>() is { movieTab: not null })
            return;

        EnsureVerticalLayout(panel);

        HideByName(panel, "HistHdr", "HistEmpty", "HistScroll");
        DestroyByName(panel, "HistHdr", "HistEmpty", "HistScroll");

        var movieTab = panel.GetComponent<MovieTabUI>();
        if (movieTab != null)
        {
            movieTab.historyContent = null;
            movieTab.historyEmptyLabel = null;
        }

        var productionWidget = panel.Find("ProductionWidget") as RectTransform;
        if (productionWidget == null)
        {
            productionWidget = estudioPanel.Find("ManagementSplit/ContratosColumn/ProductionWidget") as RectTransform;
            if (productionWidget != null)
                productionWidget.SetParent(panel, false);
        }

        if (productionWidget != null)
        {
            productionWidget.SetAsFirstSibling();
            var le = productionWidget.GetComponent<LayoutElement>() ?? productionWidget.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = HudLayoutConstants.ProductionWidgetHeight;
            le.flexibleHeight = 0f;
            le.minHeight = 120f;
        }

        Button newBtn = panel.Find("NewProductionBtn")?.GetComponent<Button>();
        if (newBtn == null)
            newBtn = CreateActionButton(panel, "NewProductionBtn", "NUEVA PRODUCCIÓN", HudLayoutConstants.ProductionActionHeight);

        var shell = panel.gameObject.GetComponent<ProductionHudShell>() ?? panel.gameObject.AddComponent<ProductionHudShell>();
        shell.Configure(productionWidget, movieTab, newBtn);
    }

    static void RestructureStudioPanel(RectTransform panel)
    {
        if (panel.GetComponent<StudioHudShell>() is { subNavigation: not null })
            return;

        var deptBar = panel.Find("DeptBar") as RectTransform;
        var splitRow = panel.Find("ManagementSplit") as RectTransform;
        var mejorasCol = splitRow != null ? splitRow.Find("MejorasColumn") as RectTransform : null;
        var contratosCol = splitRow != null ? splitRow.Find("ContratosColumn") as RectTransform : null;
        var mejorasPanel = mejorasCol != null ? mejorasCol.Find("MejorasPanel") as RectTransform : null;
        var contratosPanel = contratosCol != null ? contratosCol.Find("ContratosPanel") as RectTransform : null;

        var subTabs = HudSubTabShell.Create(panel, "Studio", new[] { "DEPARTAMENTOS", "MEJORAS", "CONTRATOS" });

        if (deptBar != null)
        {
            deptBar.SetParent(subTabs.panels[0].transform, false);
            Stretch(deptBar);
            var deptLE = deptBar.GetComponent<LayoutElement>() ?? deptBar.gameObject.AddComponent<LayoutElement>();
            deptLE.flexibleHeight = 1f;
        }

        if (mejorasPanel != null)
        {
            mejorasPanel.SetParent(subTabs.panels[1].transform, false);
            Stretch(mejorasPanel);
        }

        if (contratosPanel != null)
        {
            contratosPanel.SetParent(subTabs.panels[2].transform, false);
            Stretch(contratosPanel);
        }

        if (splitRow != null) splitRow.gameObject.SetActive(false);

        var shell = panel.gameObject.GetComponent<StudioHudShell>() ?? panel.gameObject.AddComponent<StudioHudShell>();
        shell.Configure(new StudioHudShellBuildResult
        {
            departmentsPanel = subTabs.panels[0].GetComponent<RectTransform>(),
            upgradesPanel    = subTabs.panels[1].GetComponent<RectTransform>(),
            contractsPanel   = subTabs.panels[2].GetComponent<RectTransform>(),
            subNavigation    = subTabs.navigation,
        });
    }

    static void RestructurePremiosPanel(RectTransform panel)
    {
        if (panel.GetComponent<PremiosHudShell>() is { subNavigation: not null })
            return;

        var existingChildren = CaptureChildren(panel);
        var subTabs = HudSubTabShell.Create(panel, "Premios",
            new[] { "NOMINACIONES", "PRESENTACIONES", "ESTRELLAS DE ORO", "CIUDAD" });

        HudSubTabShell.CreateSection(subTabs.panels[0], "NOMINACIONES", flexibleHeight: false);
        HudSubTabShell.CreateSection(subTabs.panels[1], "PRESENTACIONES", flexibleHeight: false);

        foreach (var child in existingChildren)
        {
            if (child == null) continue;
            child.SetParent(subTabs.panels[2].transform, false);
            if (child is RectTransform rt)
                Stretch(rt);
        }

        var citySection = new GameObject("PremiosCiudadSection", typeof(RectTransform));
        citySection.transform.SetParent(subTabs.panels[3].transform, false);
        Stretch(citySection.GetComponent<RectTransform>());
        if (citySection.GetComponent<CityPanelUI>() == null)
            citySection.AddComponent<CityPanelUI>();

        var shell = panel.gameObject.GetComponent<PremiosHudShell>() ?? panel.gameObject.AddComponent<PremiosHudShell>();
        shell.Configure(new PremiosHudShellBuildResult
        {
            nominationsPanel   = subTabs.panels[0].GetComponent<RectTransform>(),
            presentationsPanel = subTabs.panels[1].GetComponent<RectTransform>(),
            goldenStarsPanel   = subTabs.panels[2].GetComponent<RectTransform>(),
            cityPanel          = citySection.GetComponent<RectTransform>(),
            subNavigation      = subTabs.navigation,
        });
    }

    static void RestructureCollectionPanel(RectTransform panel, RectTransform productionPanel)
    {
        if (panel.GetComponent<CollectionHudShell>() is { subNavigation: not null })
            return;

        var subTabs = HudSubTabShell.Create(panel, "Collection", new[] { "COLECCIÓN", "SAGAS", "LEGENDARIAS" });

        var movieTab = productionPanel.GetComponent<MovieTabUI>();
        var collectionUI = subTabs.panels[0].GetComponent<MovieCollectionUI>();
        if (collectionUI == null)
            collectionUI = subTabs.panels[0].AddComponent<MovieCollectionUI>();
        if (movieTab != null)
            collectionUI.allMovies = movieTab.allMovies;

        HudSubTabShell.CreateSection(subTabs.panels[1], "SAGAS", flexibleHeight: false);
        HudSubTabShell.CreateSection(subTabs.panels[2], "LEGENDARIAS", flexibleHeight: false);

        var shell = panel.gameObject.GetComponent<CollectionHudShell>() ?? panel.gameObject.AddComponent<CollectionHudShell>();
        shell.Configure(new CollectionHudShellBuildResult
        {
            albumPanel       = subTabs.panels[0].GetComponent<RectTransform>(),
            sagasPanel       = subTabs.panels[1].GetComponent<RectTransform>(),
            legendariesPanel = subTabs.panels[2].GetComponent<RectTransform>(),
            collectionUI     = collectionUI,
            subNavigation    = subTabs.navigation,
        });
    }

    static void RestructureMenuPanel(RectTransform panel)
    {
        if (panel.GetComponent<MenuHudShell>() is { settingsSection: not null })
            return;

        panel.name = "MenuPanel";
        ClearChildren(panel);

        var root = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        root.padding = new RectOffset(16, 16, 16, 16);
        root.spacing = 12;
        root.childControlWidth = root.childControlHeight = true;
        root.childForceExpandWidth = true;
        root.childForceExpandHeight = false;

        var build = new MenuHudShellBuildResult
        {
            settingsSection = HudSubTabShell.CreateSection(panel.gameObject, "AJUSTES", flexibleHeight: false),
            saveSection     = HudSubTabShell.CreateSection(panel.gameObject, "GUARDAR", flexibleHeight: false),
            helpSection     = HudSubTabShell.CreateSection(panel.gameObject, "AYUDA", flexibleHeight: false),
            creditsSection  = HudSubTabShell.CreateSection(panel.gameObject, "CRÉDITOS", flexibleHeight: false),
        };

        var shell = panel.gameObject.GetComponent<MenuHudShell>() ?? panel.gameObject.AddComponent<MenuHudShell>();
        shell.Configure(build);
    }

    static RectTransform CreateCollectionPanel(RectTransform switcher)
    {
        var panel = new GameObject("ColeccionPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(switcher, false);
        panel.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.10f);
        Stretch(panel.GetComponent<RectTransform>());
        panel.SetActive(false);
        EnsureVerticalLayout(panel.GetComponent<RectTransform>());
        return panel.GetComponent<RectTransform>();
    }

    static RectTransform CreateMenuPanel(RectTransform switcher)
    {
        var panel = new GameObject("MenuPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(switcher, false);
        panel.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.10f);
        Stretch(panel.GetComponent<RectTransform>());
        panel.SetActive(false);
        return panel.GetComponent<RectTransform>();
    }

    static Button CreateActionButton(RectTransform parent, string name, string label, float height)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.15f, 0.68f, 0.38f);
        go.AddComponent<UIButtonScale>();
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleHeight = 0f;

        var tmp = RuntimeTmpText.Create(go.transform, label, 16, Color.white, FontStyles.Bold,
            TextAlignmentOptions.Center);
        Stretch(tmp.rectTransform);
        return go.GetComponent<Button>();
    }

    static Transform[] CaptureChildren(RectTransform panel)
    {
        var list = new Transform[panel.childCount];
        for (int i = 0; i < panel.childCount; i++)
            list[i] = panel.GetChild(i);
        return list;
    }

    static void ClearChildren(RectTransform panel)
    {
        for (int i = panel.childCount - 1; i >= 0; i--)
            DestroyObject(panel.GetChild(i).gameObject);
    }

    static void HideByName(RectTransform root, params string[] names)
    {
        foreach (var name in names)
        {
            var t = root.Find(name);
            if (t != null) t.gameObject.SetActive(false);
        }
    }

    static void DestroyByName(RectTransform root, params string[] names)
    {
        foreach (var name in names)
        {
            var t = root.Find(name);
            if (t != null) DestroyObject(t.gameObject);
        }
    }

    static void DestroyObject(Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying)
            Object.Destroy(obj);
        else
            Object.DestroyImmediate(obj);
    }

    static void EnsureVerticalLayout(RectTransform panel)
    {
        if (panel.GetComponent<VerticalLayoutGroup>() != null) return;

        var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 12, 12);
        vlg.spacing = 8;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
    }

    static void Stretch(RectTransform rt) => HudSubTabShell.Stretch(rt);
}
