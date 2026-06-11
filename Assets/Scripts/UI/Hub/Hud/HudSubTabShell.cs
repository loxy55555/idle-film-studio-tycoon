using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Builds a sub-tab bar + content stack wired to StudioHubUI.</summary>
public static class HudSubTabShell
{
    public struct BuildResult
    {
        public RectTransform host;
        public RectTransform tabBar;
        public RectTransform content;
        public GameObject[] panels;
        public Button[] buttons;
        public StudioHubUI navigation;
    }

    public static BuildResult Create(RectTransform host, string idPrefix, string[] tabLabels, float tabBarHeight = 36f)
    {
        var result = new BuildResult { host = host, panels = new GameObject[tabLabels.Length], buttons = new Button[tabLabels.Length] };

        var tabBar = new GameObject(idPrefix + "SubTabBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        tabBar.transform.SetParent(host, false);
        tabBar.transform.SetAsFirstSibling();
        var tabBarLE = tabBar.AddComponent<LayoutElement>();
        tabBarLE.preferredHeight = tabBarHeight;
        tabBarLE.minHeight = tabBarHeight;
        var tabHLG = tabBar.GetComponent<HorizontalLayoutGroup>();
        tabHLG.spacing = 6;
        tabHLG.childControlWidth = tabHLG.childControlHeight = true;
        tabHLG.childForceExpandWidth = true;
        tabHLG.childForceExpandHeight = true;
        result.tabBar = tabBar.GetComponent<RectTransform>();

        var subContent = new GameObject(idPrefix + "SubContent", typeof(RectTransform));
        subContent.transform.SetParent(host, false);
        var subContentLE = subContent.AddComponent<LayoutElement>();
        subContentLE.flexibleHeight = 1f;
        subContentLE.flexibleWidth = 1f;
        Stretch(subContent.GetComponent<RectTransform>());
        result.content = subContent.GetComponent<RectTransform>();

        for (int i = 0; i < tabLabels.Length; i++)
        {
            var panel = new GameObject(idPrefix + "Panel_" + tabLabels[i], typeof(RectTransform));
            panel.transform.SetParent(subContent.transform, false);
            Stretch(panel.GetComponent<RectTransform>());
            panel.SetActive(i == 0);
            result.panels[i] = panel;

            result.buttons[i] = MakeSubTabButton(tabBar.transform, tabLabels[i]);
        }

        var hub = host.gameObject.GetComponent<StudioHubUI>() ?? host.gameObject.AddComponent<StudioHubUI>();
        hub.tabPanels = result.panels;
        hub.tabButtons = result.buttons;
        result.navigation = hub;

        return result;
    }

    public static RectTransform CreateSection(GameObject parent, string title, bool flexibleHeight = true)
    {
        var section = new GameObject("Section_" + title, typeof(RectTransform), typeof(VerticalLayoutGroup));
        section.transform.SetParent(parent.transform, false);
        var le = section.AddComponent<LayoutElement>();
        if (flexibleHeight) le.flexibleHeight = 1f;
        le.flexibleWidth = 1f;
        Stretch(section.GetComponent<RectTransform>());

        var vlg = section.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 8, 8);
        vlg.spacing = 8;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var hdr = RuntimeTmpText.Create(section.transform, title, 16, Color.white, FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft, "SectionHeader");
        hdr.gameObject.AddComponent<LayoutElement>().preferredHeight = 22;

        return section.GetComponent<RectTransform>();
    }

    static Button MakeSubTabButton(Transform parent, string label)
    {
        var go = new GameObject("Tab_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.09f, 0.09f, 0.18f);
        go.AddComponent<UIButtonScale>();

        var tmp = RuntimeTmpText.Create(go.transform, label, 13, new Color(0.55f, 0.55f, 0.70f), FontStyles.Bold,
            TextAlignmentOptions.Center);
        Stretch(tmp.rectTransform);
        return go.GetComponent<Button>();
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
