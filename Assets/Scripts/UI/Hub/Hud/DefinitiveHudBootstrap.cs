using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Legacy runtime migration for scenes not yet baked (Phase 7A.3).
/// Phase 8.5C: also applies visual patches (nav labels, production layout) for baked scenes.
/// </summary>
[DefaultExecutionOrder(-160)]
public static class DefinitiveHudBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (!Application.isPlaying) return;

        var switcher = FindContentSwitcher();
        if (switcher == null) return;

        if (HudBakedSceneRules.IsBaked(switcher))
        {
            // Phase 8.5C: always patch nav labels and production layout even for baked scenes
            ApplyBakedSceneVisualPatches(switcher);
            return;
        }

        HudDefinitiveStructureBuilder.Apply(switcher, showProductionTab: true);
    }

    /// <summary>
    /// Applies visible UX patches to baked scenes without restructuring anything.
    /// Runs every play session so labels and layout are always up to date.
    /// </summary>
    static void ApplyBakedSceneVisualPatches(RectTransform switcher)
    {
        PatchBottomNavLabels();
        PatchTopBarSettingsButton();
        EnsureBonificationsBar(switcher);
        EnsureStorePanel(switcher);
        PatchStudioSubTabs(switcher);

        var hub = switcher.GetComponent<StudioHubUI>();
        if (hub != null)
            HudNavigationCleanup.NormalizeBottomNav(hub);
    }

    /// <summary>
    /// Phase 8.6B: make studio sub-tabs fill full width and increases their touch targets.
    /// </summary>
    static void PatchStudioSubTabs(RectTransform switcher)
    {
        // Find every sub-tab bar under the estudio panel
        var estudio = FindChildPanel(switcher, "EstudioPanel");
        if (estudio == null) return;

        // Search for any HorizontalLayoutGroup that is inside a *SubTabBar named object
        foreach (var rt in estudio.GetComponentsInChildren<RectTransform>(true))
        {
            if (rt == null) continue;
            if (!rt.name.EndsWith("SubTabBar") && !rt.name.EndsWith("TabBar")) continue;

            // Force the bar to fill the full width
            var hlg = rt.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.childForceExpandWidth = true;
                hlg.childForceExpandHeight = true;
                hlg.padding = new RectOffset(0, 0, 0, 0);
                hlg.spacing = 0;
            }

            // Update LayoutElement
            var barLE = rt.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
            barLE.preferredHeight = 64f;   // ESTUDIO-TUNING: taller tabs (was 52)
            barLE.minHeight       = 48f;
            barLE.flexibleHeight  = 0f;

            // Upscale tab labels
            foreach (var tmp in rt.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                tmp.fontSize = 13f;
                tmp.enableAutoSizing = false;
                tmp.fontStyle = FontStyles.Bold;
                // Ensure the label rect fills its button
                var le = tmp.GetComponent<LayoutElement>() ?? tmp.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 0f;
            }

            // Force each tab button to fill equally (remove fixed preferredWidth)
            for (int i = 0; i < rt.childCount; i++)
            {
                var tab = rt.GetChild(i);
                var le  = tab.GetComponent<LayoutElement>() ?? tab.gameObject.AddComponent<LayoutElement>();
                le.flexibleWidth = 1f;
                le.preferredWidth = -1f;
            }
        }
    }

    /// <summary>
    /// Phase 8.6A: guarantees the Studio screen has a live bonifications strip
    /// right below the visual stage, even in baked scenes.
    /// </summary>
    static void EnsureBonificationsBar(RectTransform switcher)
    {
        var estudio = FindChildPanel(switcher, "EstudioPanel");
        if (estudio == null) return;

        if (estudio.GetComponentInChildren<BonificationsBarUI>(true) != null) return;

        var barGo = new GameObject("BonificationsBar", typeof(RectTransform));
        barGo.transform.SetParent(estudio, false);

        var stage = estudio.Find("StudioVisualStage");
        if (stage != null)
            barGo.transform.SetSiblingIndex(stage.GetSiblingIndex() + 1);

        barGo.AddComponent<BonificationsBarUI>();
    }

    /// <summary>
    /// Phase 8.6A/B: guarantees the store tab shows the commercial store screen.
    /// Also ensures the panel is active and has proper RectTransform stretch.
    /// </summary>
    static void EnsureStorePanel(RectTransform switcher)
    {
        var menu = FindChildPanel(switcher, "MenuPanel", "TiendaPanel");
        if (menu == null) return;

        // Ensure the panel itself stretches (it may have been built with fixed size)
        menu.anchorMin = Vector2.zero; menu.anchorMax = Vector2.one;
        menu.offsetMin = menu.offsetMax = Vector2.zero;

        var store = menu.GetComponent<StorePanelUI>();
        if (store == null)
            store = menu.gameObject.AddComponent<StorePanelUI>();

        // Force an immediate build in case OnEnable already fired before we added the component
        if (menu.gameObject.activeInHierarchy)
            store.SendMessage("EnsureBuilt", SendMessageOptions.DontRequireReceiver);
    }

    static RectTransform FindChildPanel(RectTransform switcher, params string[] names)
    {
        foreach (var n in names)
        {
            var t = switcher.Find(n) as RectTransform;
            if (t != null) return t;
        }
        return null;
    }

    static void PatchBottomNavLabels()
    {
        var nav = GameObject.Find("BottomNav")?.transform;
        if (nav == null) return;

        var labels = MainHudTabLabels.BottomNav;
        var icons  = MainHudTabLabels.BottomIcons;

        for (int i = 0; i < nav.childCount && i < labels.Length; i++)
        {
            var tab      = nav.GetChild(i);
            var labelTmp = tab.Find("Label")?.GetComponent<TextMeshProUGUI>();
            var iconTmp  = tab.Find("Icon")?.GetComponent<TextMeshProUGUI>();
            if (labelTmp != null) labelTmp.text = labels[i];
            if (iconTmp  != null) iconTmp.text  = icons[i];
        }
    }

    static void PatchTopBarSettingsButton()
    {
        var topBar = Object.FindAnyObjectByType<TopBarUI>(FindObjectsInactive.Include);
        topBar?.PatchSettingsIcon();
    }

    static RectTransform FindContentSwitcher()
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == "ContentSwitcher")
                return t as RectTransform;
        }

        return null;
    }
}
