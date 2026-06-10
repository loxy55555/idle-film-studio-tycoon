using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Adds PRODUCIR | COLECCIÓN sub-tabs inside PeliculasPanel at runtime.</summary>
public static class MovieTabShellBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (!Application.isPlaying) return;

        foreach (var panel in Object.FindObjectsByType<MovieTabUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (panel.transform.Find("PeliculasSubTabBar") != null) continue;
            Restructure(panel);
        }
    }

    static void Restructure(MovieTabUI movieTab)
    {
        var panel = movieTab.transform as RectTransform;
        if (panel == null) return;

        var children = new Transform[panel.childCount];
        for (int i = 0; i < panel.childCount; i++)
            children[i] = panel.GetChild(i);

        var tabBar = new GameObject("PeliculasSubTabBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        tabBar.transform.SetParent(panel, false);
        tabBar.transform.SetAsFirstSibling();
        var tabBarLE = tabBar.AddComponent<LayoutElement>();
        tabBarLE.preferredHeight = 36f;
        tabBarLE.minHeight = 36f;
        var tabHLG = tabBar.GetComponent<HorizontalLayoutGroup>();
        tabHLG.spacing = 6;
        tabHLG.childControlWidth = tabHLG.childControlHeight = true;
        tabHLG.childForceExpandWidth = true;
        tabHLG.childForceExpandHeight = true;

        var subContent = new GameObject("PeliculasSubContent", typeof(RectTransform));
        subContent.transform.SetParent(panel, false);
        subContent.transform.SetSiblingIndex(1);
        var subContentLE = subContent.AddComponent<LayoutElement>();
        subContentLE.flexibleHeight = 1f;
        subContentLE.flexibleWidth = 1f;
        Stretch(subContent.GetComponent<RectTransform>());

        var producirPanel = new GameObject("ProducirPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
        producirPanel.transform.SetParent(subContent.transform, false);
        Stretch(producirPanel.GetComponent<RectTransform>());
        var prodLE = producirPanel.AddComponent<LayoutElement>();
        prodLE.flexibleHeight = 1f;
        prodLE.flexibleWidth = 1f;
        var prodVLG = producirPanel.GetComponent<VerticalLayoutGroup>();
        prodVLG.padding = new RectOffset(0, 0, 0, 0);
        prodVLG.spacing = 8;
        prodVLG.childControlWidth = prodVLG.childControlHeight = true;
        prodVLG.childForceExpandWidth = true;
        prodVLG.childForceExpandHeight = false;

        foreach (var child in children)
            child.SetParent(producirPanel.transform, false);

        var coleccionPanel = new GameObject("ColeccionPanel", typeof(RectTransform));
        coleccionPanel.transform.SetParent(subContent.transform, false);
        Stretch(coleccionPanel.GetComponent<RectTransform>());
        coleccionPanel.SetActive(false);

        var collectionUI = coleccionPanel.AddComponent<MovieCollectionUI>();
        collectionUI.allMovies = movieTab.allMovies;

        var btnProducir = MakeSubTabButton(tabBar.transform, "PRODUCIR");
        var btnColeccion = MakeSubTabButton(tabBar.transform, "COLECCIÓN");

        var hub = panel.gameObject.GetComponent<StudioHubUI>() ?? panel.gameObject.AddComponent<StudioHubUI>();
        hub.tabPanels = new[] { producirPanel, coleccionPanel };
        hub.tabButtons = new[] { btnProducir, btnColeccion };
    }

    static Button MakeSubTabButton(Transform parent, string label)
    {
        var go = new GameObject("Tab_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.09f, 0.09f, 0.18f);
        go.AddComponent<UIButtonScale>();

        var tmp = RuntimeTmpText.Create(go.transform, label, 14, new Color(0.55f, 0.55f, 0.70f), FontStyles.Bold,
            TextAlignmentOptions.Center);
        Stretch(tmp.rectTransform);
        return go.GetComponent<Button>();
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
