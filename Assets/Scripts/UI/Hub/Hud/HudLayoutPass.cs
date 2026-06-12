using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Applies compact layout and studio visual stage reservation every play.</summary>
public static class HudLayoutPass
{
    public static void Apply(RectTransform contentSwitcher, StudioHubUI mainNavigation)
    {
        if (contentSwitcher == null) return;

        EliminateCityFromBottomNav();
        HudNavigationCleanup.Run(contentSwitcher, mainNavigation);
        SyncMainNavigationOrder(contentSwitcher, mainNavigation);
        CompactAllSubTabBars(contentSwitcher);
        ApplyEstudioLayout(FindPanel(contentSwitcher, "EstudioPanel"));
        ApplyProductionLayout(FindPanel(contentSwitcher, "ProduccionPanel", "PeliculasPanel"));
        ApplyPlaceholderSections(FindPanel(contentSwitcher, "PremiosPanel"));
        ApplyPlaceholderSections(FindPanel(contentSwitcher, "ColeccionPanel"));
        ApplyPlaceholderSections(FindPanel(contentSwitcher, "MenuPanel", "TiendaPanel"));
    }

    static void SyncMainNavigationOrder(RectTransform switcher, StudioHubUI mainNavigation)
    {
        if (mainNavigation == null) return;

        var studio     = FindPanel(switcher, "EstudioPanel")?.gameObject;
        var production = FindPanel(switcher, "ProduccionPanel", "PeliculasPanel")?.gameObject;
        var awards     = FindPanel(switcher, "PremiosPanel")?.gameObject;
        var collection = FindPanel(switcher, "ColeccionPanel")?.gameObject;
        var menu       = FindPanel(switcher, "MenuPanel", "TiendaPanel")?.gameObject;

        if (studio == null || production == null || awards == null || collection == null || menu == null)
            return;

        mainNavigation.tabPanels = new[] { studio, production, awards, collection, menu };
    }

    static void EliminateCityFromBottomNav()
    {
        var nav = GameObject.Find("BottomNav")?.transform;
        if (nav == null) return;

        for (int i = 0; i < nav.childCount; i++)
        {
            var tab = nav.GetChild(i);
            var label = tab.Find("Label")?.GetComponent<TextMeshProUGUI>();
            var icon  = tab.Find("Icon")?.GetComponent<TextMeshProUGUI>();

            bool labelIsCity = ContainsCityToken(label?.text);
            bool iconIsCity  = ContainsCityToken(icon?.text);
            bool nameIsCity  = ContainsCityToken(tab.name);

            if (!labelIsCity && !iconIsCity && !nameIsCity) continue;

            RenameBottomTab(tab, MainHudTabLabels.BottomNav[(int)MainHudTab.Collection],
                MainHudTabLabels.BottomIcons[(int)MainHudTab.Collection]);
        }
    }

    static bool ContainsCityToken(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        var upper = value.ToUpperInvariant();
        if (upper.Contains("COLECC") || upper.Contains("COL")) return false;
        return upper.Contains("CIUDAD") || upper == "CIU" || upper.Contains("CIU ");
    }

    static void RenameBottomTab(Transform tab, string label, string icon)
    {
        tab.name = "Tab_" + label.Replace(" ", "_");
        var labelTmp = tab.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (labelTmp != null) labelTmp.text = label;
        var iconTmp = tab.Find("Icon")?.GetComponent<TextMeshProUGUI>();
        if (iconTmp != null) iconTmp.text = icon;
    }

    static void CompactAllSubTabBars(RectTransform switcher)
    {
        foreach (var bar in switcher.GetComponentsInChildren<RectTransform>(true))
        {
            if (bar == null || !bar.name.EndsWith("SubTabBar")) continue;
            CompactSubTabBar(bar);
        }
    }

    public static void CompactSubTabBar(RectTransform tabBar)
    {
        if (tabBar == null) return;

        var le = tabBar.GetComponent<LayoutElement>() ?? tabBar.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = HudLayoutConstants.SubTabBarHeight;
        le.minHeight       = HudLayoutConstants.SubTabBarHeight;
        le.flexibleHeight  = 0f;

        foreach (var tmp in tabBar.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            tmp.fontSize = HudLayoutConstants.SubTabFontSize;
            tmp.enableAutoSizing = false;
        }
    }

    static void ApplyEstudioLayout(RectTransform estudio)
    {
        if (estudio == null) return;

        var header = estudio.Find("StudioHeader") as RectTransform;
        if (header != null)
        {
            var headerLE = header.GetComponent<LayoutElement>() ?? header.gameObject.AddComponent<LayoutElement>();
            headerLE.preferredHeight = HudLayoutConstants.StudioHeaderHeight;
            headerLE.minHeight       = HudLayoutConstants.StudioHeaderHeight;
            headerLE.flexibleHeight  = 0f;
        }

        var stage = EnsureStudioVisualStage(estudio);
        var tabBar = estudio.Find("StudioSubTabBar");
        var subContent = estudio.Find("StudioSubContent");

        if (stage != null && header != null)
        {
            stage.SetSiblingIndex(header.GetSiblingIndex() + 1);
            if (tabBar != null) tabBar.SetSiblingIndex(stage.GetSiblingIndex() + 1);
            if (subContent != null)
                subContent.SetSiblingIndex(tabBar != null ? tabBar.GetSiblingIndex() + 1 : stage.GetSiblingIndex() + 1);
        }

        if (tabBar != null) CompactSubTabBar(tabBar as RectTransform);

        var legacyScene = estudio.Find("StudioScene");
        if (legacyScene != null && legacyScene != stage)
            legacyScene.gameObject.SetActive(false);

        var split = estudio.Find("ManagementSplit");
        if (split != null) split.gameObject.SetActive(false);
    }

    static RectTransform EnsureStudioVisualStage(RectTransform estudio)
    {
        var stage = estudio.Find("StudioVisualStage") as RectTransform;
        if (stage == null)
        {
            var legacy = estudio.Find("StudioScene") as RectTransform;
            if (legacy != null)
            {
                legacy.gameObject.SetActive(true);
                legacy.name = "StudioVisualStage";
                stage = legacy;
            }
            else
            {
                var go = new GameObject("StudioVisualStage", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(estudio, false);
                stage = go.GetComponent<RectTransform>();
            }
        }

        if (stage.GetComponent<StudioVisualStage>() == null)
            stage.gameObject.AddComponent<StudioVisualStage>();
        else
            stage.GetComponent<StudioVisualStage>().ApplyLayout();

        var stageComponent = stage.GetComponent<StudioVisualStage>();
        if (stageComponent != null)
            StudioVisualStageLayers.EnsureHierarchy(stageComponent);

        return stage;
    }

    static void ApplyProductionLayout(RectTransform production)
    {
        if (production == null) return;

        var header = production.Find("Hdr");
        if (header != null)
        {
            var le = header.GetComponent<LayoutElement>() ?? header.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 20f;
        }

        var widget = production.Find("ProductionWidget") as RectTransform;
        if (widget != null)
        {
            var le = widget.GetComponent<LayoutElement>() ?? widget.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = HudLayoutConstants.ProductionWidgetHeight;
            le.flexibleHeight  = 0f;
            widget.SetAsFirstSibling();
        }

        var slots = production.Find("MovieSlotsRow") as RectTransform;
        if (slots != null)
        {
            var le = slots.GetComponent<LayoutElement>() ?? slots.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = HudLayoutConstants.ProductionSlotsHeight;
            le.flexibleHeight  = 0f;
        }

        var newBtn = production.Find("NewProductionBtn");
        if (newBtn != null)
        {
            var le = newBtn.GetComponent<LayoutElement>() ?? newBtn.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = HudLayoutConstants.ProductionActionHeight;
            le.flexibleHeight  = 0f;
        }
    }

    static void ApplyPlaceholderSections(RectTransform panel)
    {
        if (panel == null) return;

        foreach (var section in panel.GetComponentsInChildren<RectTransform>(true))
        {
            if (section == null || !section.name.StartsWith("Section_")) continue;

            var le = section.GetComponent<LayoutElement>() ?? section.gameObject.AddComponent<LayoutElement>();
            le.flexibleHeight = 0f;
            le.preferredHeight = 36f;

            var header = section.Find("SectionHeader")?.GetComponent<TextMeshProUGUI>();
            if (header != null)
            {
                header.fontSize = 14f;
                var headerLE = header.GetComponent<LayoutElement>() ?? header.gameObject.AddComponent<LayoutElement>();
                headerLE.preferredHeight = HudLayoutConstants.SectionHeaderHeight;
            }
        }
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
}
