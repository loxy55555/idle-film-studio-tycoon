using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Idempotent HUD navigation cleanup — removes legacy tabs, panels, and duplicated shells.
/// </summary>
public static class HudNavigationCleanup
{
    public static void Run(RectTransform contentSwitcher, StudioHubUI mainNavigation)
    {
        if (contentSwitcher == null) return;

        StripProductionSubTabs(contentSwitcher);
        RemoveDuplicateCollectionInstances(contentSwitcher);
        RemoveLegacyPanels(contentSwitcher);
        NormalizeBottomNav(mainNavigation);
        HideTopBarDuplicates();
    }

    public static void StripLegacyProductionSubTabs(RectTransform contentSwitcher) =>
        StripProductionSubTabs(contentSwitcher);

    static void StripProductionSubTabs(RectTransform switcher)
    {
        var production = FindPanel(switcher, "ProduccionPanel", "PeliculasPanel");
        if (production == null) return;

        var subBar = production.Find("PeliculasSubTabBar");
        if (subBar == null) return;

        var producir = production.Find("PeliculasSubContent/ProducirPanel");
        if (producir != null)
        {
            var moves = new Transform[producir.childCount];
            for (int i = 0; i < moves.Length; i++)
                moves[i] = producir.GetChild(i);
            foreach (var child in moves)
                child.SetParent(production, false);
        }

        var nestedCollection = production.Find("PeliculasSubContent/ColeccionPanel");
        if (nestedCollection != null)
            Object.Destroy(nestedCollection.gameObject);

        var subContent = production.Find("PeliculasSubContent");
        if (subContent != null)
            Object.Destroy(subContent.gameObject);

        Object.Destroy(subBar.gameObject);

        foreach (var hub in production.GetComponents<StudioHubUI>())
            Object.Destroy(hub);
    }

    static void RemoveDuplicateCollectionInstances(RectTransform switcher)
    {
        var canonical = FindPanel(switcher, "ColeccionPanel");
        if (canonical == null) return;

        foreach (var collection in Object.FindObjectsByType<MovieCollectionUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (collection == null) continue;
            if (IsUnder(collection.transform, canonical)) continue;
            Object.Destroy(collection.gameObject);
        }
    }

    static void RemoveLegacyPanels(RectTransform switcher)
    {
        DestroyPanel(switcher, "CiudadPanel");

        var menuPanel = FindPanel(switcher, "MenuPanel");
        if (menuPanel != null)
            DestroyPanel(switcher, "TiendaPanel");
    }

    static void NormalizeBottomNav(StudioHubUI mainNavigation)
    {
        var nav = GameObject.Find("BottomNav")?.transform;
        if (nav == null) return;

        while (nav.childCount > 5)
            Object.Destroy(nav.GetChild(nav.childCount - 1).gameObject);

        if (nav.childCount < 5) return;

        var production = FindTab(nav, "PRO", "PRODU");
        var studio     = FindTab(nav, "ESTUDIO", "EST");
        var awards     = FindTab(nav, "PREMIO", "PRE", "OSC");
        var collection = FindTab(nav, "COLECC", "COL");
        var menu       = FindTab(nav, "MENÚ", "MENU", "TIENDA", "TDA");

        if (studio == null)     studio     = nav.GetChild(0);
        if (production == null) production = nav.GetChild(Mathf.Min(1, nav.childCount - 1));
        if (awards == null)     awards     = nav.GetChild(Mathf.Min(2, nav.childCount - 1));
        if (collection == null) collection = nav.GetChild(Mathf.Min(3, nav.childCount - 1));
        if (menu == null)       menu       = nav.GetChild(Mathf.Min(4, nav.childCount - 1));

        var ordered = new[] { studio, production, awards, collection, menu };
        for (int i = 0; i < ordered.Length; i++)
        {
            if (ordered[i] == null) continue;
            ordered[i].SetSiblingIndex(i);
            RenameTab(ordered[i], MainHudTabLabels.BottomNav[i], MainHudTabLabels.BottomIcons[i]);
        }

        if (mainNavigation == null) return;

        var buttons = new Button[5];
        for (int i = 0; i < 5; i++)
            buttons[i] = nav.GetChild(i).GetComponent<Button>();
        mainNavigation.tabButtons = buttons;
    }

    static void HideTopBarDuplicates()
    {
        var stats = GameObject.Find("StatsBlock");
        if (stats == null) return;

        var level = stats.transform.Find("LevelText");
        if (level == null)
        {
            foreach (Transform block in stats.transform)
            {
                level = block.Find("LevelText");
                if (level != null) break;
            }
        }

        if (level != null)
            level.gameObject.SetActive(false);
    }

    static Transform FindTab(Transform nav, params string[] tokens)
    {
        for (int i = 0; i < nav.childCount; i++)
        {
            var child = nav.GetChild(i);
            var haystack = (child.name + " " + ReadTabLabel(child)).ToUpperInvariant();
            foreach (var token in tokens)
            {
                if (haystack.Contains(token.ToUpperInvariant()))
                    return child;
            }
        }

        return null;
    }

    static string ReadTabLabel(Transform tab)
    {
        var label = tab.Find("Label")?.GetComponent<TextMeshProUGUI>();
        return label != null ? label.text : string.Empty;
    }

    static void RenameTab(Transform tab, string label, string icon)
    {
        tab.name = "Tab_" + label.Replace(" ", "_");

        var labelTmp = tab.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (labelTmp != null) labelTmp.text = label;

        var iconTmp = tab.Find("Icon")?.GetComponent<TextMeshProUGUI>();
        if (iconTmp != null) iconTmp.text = icon;
    }

    static RectTransform FindPanel(Transform parent, params string[] names)
    {
        foreach (var name in names)
        {
            var t = parent.Find(name) as RectTransform;
            if (t != null) return t;
        }

        return null;
    }

    static void DestroyPanel(Transform parent, string panelName)
    {
        var panel = parent.Find(panelName);
        if (panel == null) return;

        foreach (var cityUi in panel.GetComponentsInChildren<CityPanelUI>(true))
        {
            if (cityUi != null && cityUi.gameObject != panel.gameObject)
                Object.Destroy(cityUi.gameObject);
        }

        Object.Destroy(panel.gameObject);
    }

    static bool IsUnder(Transform target, Transform ancestor)
    {
        var t = target;
        while (t != null)
        {
            if (t == ancestor) return true;
            t = t.parent;
        }

        return false;
    }
}
