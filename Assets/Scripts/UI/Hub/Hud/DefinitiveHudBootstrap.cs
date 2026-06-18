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
    static readonly string[] MainStudioTabKeys =
    {
        LocKeys.StudTabDepts,
        LocKeys.StudTabMejoras,
        LocKeys.StudTabContratos,
    };

    /// <summary>Refresh all navigation label text (bottom nav + studio subtabs) for a language change.</summary>
    public static void RefreshNavLabels()
    {
        PatchBottomNavLabels();

        var switcher = FindContentSwitcher();
        if (switcher == null) return;
        var estudio = FindChildPanel(switcher, "EstudioPanel");
        if (estudio == null) return;

        // Refresh main studio subtabs (DEPARTAMENTOS / MEJORAS / CONTRATOS)
        RefreshMainStudioSubTabs(estudio);

        // Refresh mejoras sub-tab labels (PRODUCCIÓN / STAFF / INVESTIGACIÓN / MARKETING)
        var mejoras = estudio.Find("StudioSubContent/StudioPanel_MEJORAS");
        if (mejoras != null) PatchMejorasSubTabs(mejoras);
    }

    static void RefreshMainStudioSubTabs(Transform estudio)
    {
        var subTabBar = estudio.Find("StudioSubTabBar") as RectTransform;
        if (subTabBar == null) return;

        int tabIndex = 0;
        for (int i = 0; i < subTabBar.childCount && tabIndex < MainStudioTabKeys.Length; i++)
        {
            var tab = subTabBar.GetChild(i);
            if (tab.GetComponent<Button>() == null) continue;
            var lbl = tab.Find("Label")?.GetComponent<TextMeshProUGUI>()
                   ?? tab.GetComponentInChildren<TextMeshProUGUI>(true);
            if (lbl != null) lbl.text = Loc.Get(MainStudioTabKeys[tabIndex]);
            tabIndex++;
        }
    }

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
        }
        else
        {
            HudDefinitiveStructureBuilder.Apply(switcher, showProductionTab: true);
        }

        RemoveLegacyCollectionSubTabs(switcher);
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

        PatchStudioVisualFinal(switcher);
        PatchStudioSubTabs(switcher);
        // Localize main studio subtabs (DEPARTAMENTOS / MEJORAS / CONTRATOS)
        var estudioForSubTabs = FindChildPanel(switcher, "EstudioPanel");
        if (estudioForSubTabs != null) RefreshMainStudioSubTabs(estudioForSubTabs);
        PatchStudioDepartmentCards(switcher);
        PatchStudioLayoutExpansion(switcher);
        PatchDeptScrollHandling(switcher);
        PatchMejorasScrollHandling(switcher);
        PatchProductionLayout(switcher);
        PatchProductionRemovePosters(switcher);
        PatchVisualGridConsistency(switcher);

        PatchLegacyHudColors(switcher);
        PatchCinematicChrome();

        var hub = switcher.GetComponent<StudioHubUI>();
        if (hub != null)
            HudNavigationCleanup.NormalizeBottomNav(hub);
    }

    /// <summary>
    /// Apply dark palette to chrome only (top bar, bottom nav).
    /// Per-component materials are applied by each card's own init — no global image scan.
    /// Phase 11.5B: global overlay scan removed; it caused grain/shadow over large containers.
    /// </summary>
    static void PatchCinematicChrome()
    {
        var bottomNav = GameObject.Find("BottomNav");
        if (bottomNav != null)
        {
            var bg = bottomNav.GetComponent<Image>() ?? bottomNav.GetComponentInParent<Image>(false);
            if (bg != null)
                HudSkinProvider.ApplyPanel(bg, HudPanelVariant.Nav);
        }

        var topBarGo = GameObject.Find("TopBar");
        if (topBarGo != null)
        {
            var bg = topBarGo.GetComponent<Image>();
            if (bg != null)
                HudSkinProvider.ApplyPanel(bg, HudPanelVariant.TopBar);
        }
    }

    /// <summary>
    /// COLLECTION-FIX: MovieCollectionUI owns L1/L2/L3 navigation.
    /// The baked CollectionSubTabBar has no parent VLG and floats at screen center,
    /// rendering ghost Tab_COLECCIÓN / Tab_SAGAS / Tab_LEGENDARIAS over the genre grid.
    /// </summary>
    static void RemoveLegacyCollectionSubTabs(RectTransform switcher)
    {
        var coleccion = FindChildPanel(switcher, "ColeccionPanel");
        if (coleccion == null) return;

        var tabBar = coleccion.Find("CollectionSubTabBar");
        if (tabBar != null)
            Object.Destroy(tabBar.gameObject);

        var subContent = coleccion.Find("CollectionSubContent") as RectTransform;
        if (subContent == null) return;

        StretchRect(subContent);

        var album = subContent.Find("CollectionPanel_COLECCIÓN");
        if (album != null)
            album.gameObject.SetActive(true);
    }

    static void StretchRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    static void PatchStudioDepartmentCards(RectTransform switcher)
    {
        var estudio = FindChildPanel(switcher, "EstudioPanel");
        if (estudio == null) return;

        foreach (var card in estudio.GetComponentsInChildren<DepartmentMiniCardUI>(true))
        {
            var rt = card.transform as RectTransform;
            if (rt == null) continue;

            card.ForcePremiumLayoutRebuild();
            DepartmentMiniCardLayoutBuilder.LogLayoutAudit(card);

            DepartmentMiniCardLayoutBuilder.NormalizeParentRowHeight(rt);
            var le = rt.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = DepartmentMiniCardLayoutBuilder.CardHeight;
            le.minHeight = DepartmentMiniCardLayoutBuilder.CardHeight;
            le.flexibleWidth = 1f;
            le.preferredWidth = -1f;

            // Phase 11.5: uniform premium material — no per-dept background colors
            CinematicTheme.NeutralizeDepartmentCard(rt);
            CinematicTheme.ApplyPremiumMaterial(rt);

            var badge = rt.Find("Content/CardMainRow/IconColumn/Badge")?.GetComponent<Image>()
                     ?? rt.Find("Content/CardMainRow/InfoColumn/HeaderRow/Badge")?.GetComponent<Image>()
                     ?? rt.Find("Content/HeaderRow/Badge")?.GetComponent<Image>();
            if (badge != null)
                badge.color = Color.clear;
        }

        var bar = estudio.GetComponentInChildren<BonificationsBarUI>(true);
        if (bar != null)
            bar.SendMessage("EnsureBuilt", SendMessageOptions.DontRequireReceiver);

        var stage = estudio.GetComponentInChildren<StudioVisualStage>(true);
        stage?.ApplyLayout();
    }

    static void PatchStudioLayoutExpansion(RectTransform switcher)
    {
        var estudio = FindChildPanel(switcher, "EstudioPanel");
        if (estudio == null) return;

        var stage = estudio.Find("StudioVisualStage");
        var bar = estudio.Find("BonificationsBar");
        if (stage != null && bar != null)
            bar.SetSiblingIndex(stage.GetSiblingIndex() + 1);

        if (bar != null)
        {
            var barLE = bar.GetComponent<LayoutElement>() ?? bar.gameObject.AddComponent<LayoutElement>();
            barLE.preferredHeight = HudLayoutConstants.StudioSummaryHeight;
            barLE.minHeight = 100f;
            barLE.flexibleHeight = 0f;
            bar.gameObject.SetActive(true);
        }

        var deptBar = estudio.Find("StudioSubContent/StudioPanel_DEPARTAMENTOS/DeptBar") as RectTransform
                   ?? estudio.Find("DeptBar") as RectTransform;
        if (deptBar != null)
        {
            var deptLE = deptBar.GetComponent<LayoutElement>() ?? deptBar.gameObject.AddComponent<LayoutElement>();
            deptLE.minHeight = 0f;
            deptLE.preferredHeight = -1f;
            deptLE.flexibleHeight = 1f;

            PatchDeptSingleColumn(deptBar);

            var content = deptBar.Find("DeptScroll/Viewport/Content");
            var deptVLG = content != null ? content.GetComponent<VerticalLayoutGroup>() : null;
            if (deptVLG != null)
            {
                deptVLG.padding = new RectOffset(12, 12, 8, 12);
                deptVLG.spacing = 10;
                deptVLG.childForceExpandWidth = true;
            }
        }

        var subContent = estudio.Find("StudioSubContent") as RectTransform;
        if (subContent != null)
        {
            var subLE = subContent.GetComponent<LayoutElement>() ?? subContent.gameObject.AddComponent<LayoutElement>();
            subLE.flexibleHeight = 1f;
        }
    }

    static void PatchDeptScrollHandling(RectTransform switcher)
    {
        var estudio = FindChildPanel(switcher, "EstudioPanel");
        if (estudio == null) return;

        var deptBar = estudio.Find("StudioSubContent/StudioPanel_DEPARTAMENTOS/DeptBar")
                   ?? estudio.Find("DeptBar");
        if (deptBar == null) return;

        var scroll = deptBar.Find("DeptScroll")?.GetComponent<ScrollRect>();
        if (scroll == null) return;

        if (scroll.viewport != null)
        {
            var vpImg = scroll.viewport.GetComponent<Image>() ?? scroll.viewport.gameObject.AddComponent<Image>();
            vpImg.color = new Color(0f, 0f, 0f, 0f);
            vpImg.raycastTarget = true;
        }

        scroll.vertical = true;
        scroll.horizontal = false;
        scroll.scrollSensitivity = 35f;

        foreach (var card in deptBar.GetComponentsInChildren<DepartmentMiniCardUI>(true))
        {
            if (card.GetComponent<ScrollDragForwarder>() == null)
                card.gameObject.AddComponent<ScrollDragForwarder>();
            UpgradeUiRaycastPolicy.ApplyDepartmentCard(card.transform, card.upgradeButton);
        }
    }

    static void PatchMejorasScrollHandling(RectTransform switcher)
    {
        var estudio = FindChildPanel(switcher, "EstudioPanel");
        if (estudio == null) return;

        var mejoras = estudio.Find("StudioSubContent/StudioPanel_MEJORAS")
                   ?? estudio.Find("StudioPanel_MEJORAS");
        if (mejoras == null) return;

        foreach (var scroll in mejoras.GetComponentsInChildren<ScrollRect>(true))
        {
            if (scroll.content == null) continue;

            var panel = scroll.transform.parent;
            if (panel == null) continue;
            if (panel.name is not ("EquipoPanel" or "PersonalPanel" or "InstPanel" or "MktPanel"))
                continue;

            if (scroll.viewport != null)
            {
                var vpImg = scroll.viewport.GetComponent<Image>() ?? scroll.viewport.gameObject.AddComponent<Image>();
                vpImg.color = new Color(0f, 0f, 0f, 0f);
                vpImg.raycastTarget = true;
            }

            scroll.vertical = true;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 35f;

            foreach (var card in scroll.content.GetComponentsInChildren<UpgradeCardUI>(true))
            {
                if (card.GetComponent<ScrollDragForwarder>() == null)
                    card.gameObject.AddComponent<ScrollDragForwarder>();
                card.RefreshUI();
                UpgradeUiRaycastPolicy.ApplyDepartmentCard(card.transform, card.buyButton);
            }
        }
    }

    static void PatchProductionRemovePosters(RectTransform switcher)
    {
        var prod = FindChildPanel(switcher, "ProduccionPanel");
        if (prod == null) return;

        foreach (var ui in prod.GetComponentsInChildren<MovieButtonUI>(true))
        {
            var cfg = ui.movieConfig;
            MovieOfferCardLayoutBuilder.ApplyRarityIconLayout(
                ui.transform,
                cfg?.rarity ?? MovieRarity.Common,
                cfg?.genre ?? MovieGenre.Drama);
        }
    }

    static void PatchVisualGridConsistency(RectTransform switcher)
    {
        ApplyPanelPadding(FindChildPanel(switcher, "EstudioPanel"));
        ApplyPanelPadding(FindChildPanel(switcher, "ProduccionPanel"));
        ApplyPanelPadding(FindChildPanel(switcher, "ColeccionPanel"));
        ApplyPanelPadding(FindChildPanel(switcher, "PremiosPanel"));
        ApplyPanelPadding(FindChildPanel(switcher, "MenuPanel", "TiendaPanel"));
    }

    static void ApplyPanelPadding(RectTransform panel)
    {
        if (panel == null) return;

        var vlg = panel.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            vlg.padding = new RectOffset(
                (int)HudLayoutConstants.ScreenPaddingH,
                (int)HudLayoutConstants.ScreenPaddingH,
                (int)HudLayoutConstants.ScreenPaddingV,
                (int)HudLayoutConstants.ScreenPaddingV);
            vlg.spacing = HudLayoutConstants.SectionSpacing;
        }
    }

    static void PatchProductionLayout(RectTransform switcher)
    {
        var prod = FindChildPanel(switcher, "ProduccionPanel");
        if (prod == null) return;

        var prodVLG = prod.GetComponent<VerticalLayoutGroup>();
        if (prodVLG != null)
        {
            prodVLG.padding = HudLayoutConstants.SectionPadding;
            prodVLG.spacing = HudLayoutConstants.SectionSpacing;
        }

        var widget = prod.Find("ProductionWidget") as RectTransform;
        if (widget != null)
        {
            var widgetLE = widget.GetComponent<LayoutElement>() ?? widget.gameObject.AddComponent<LayoutElement>();
            widgetLE.preferredHeight = HudLayoutConstants.ProductionWidgetHeight;
            widgetLE.minHeight = 180f;
            widgetLE.flexibleHeight = 0f;
        }

        var slotsRow = prod.Find("MovieSlotsRow") as RectTransform;
        if (slotsRow != null)
        {
            var rowLE = slotsRow.GetComponent<LayoutElement>() ?? slotsRow.gameObject.AddComponent<LayoutElement>();
            rowLE.flexibleHeight = 2f;
        }

        var newBtn = prod.Find("NewProductionBtn");
        if (newBtn != null)
        {
            var btnLE = newBtn.GetComponent<LayoutElement>() ?? newBtn.gameObject.AddComponent<LayoutElement>();
            btnLE.preferredHeight = HudLayoutConstants.ProductionActionHeight;
            btnLE.minHeight = HudLayoutConstants.ProductionActionHeight;
        }

        var movieTab = prod.GetComponent<MovieTabUI>();
        if (movieTab?.slotsRow != null)
        {
            for (int i = 0; i < movieTab.slotsRow.childCount; i++)
            {
                var card = movieTab.slotsRow.GetChild(i);
                var ui = card.GetComponent<MovieButtonUI>();
                var rarity = ui?.movieConfig?.rarity ?? MovieRarity.Common;
                var genre  = ui?.movieConfig?.genre ?? MovieGenre.Drama;
                MovieOfferCardLayoutBuilder.ApplyCompactOfferLayout(card);
                MovieOfferCardLayoutBuilder.ApplyRarityIconLayout(card, rarity, genre);
            }
        }

        PatchProductionProgressBars(prod);
    }

    static void PatchProductionProgressBars(RectTransform prod)
    {
        if (prod == null) return;

        foreach (var slot in prod.GetComponentsInChildren<RectTransform>(true))
        {
            if (slot.name != "ProdSlot") continue;

            var pc = slot.Find("ProgressContainer") as RectTransform
                  ?? slot.Find("ProgressRow") as RectTransform;
            if (pc == null) continue;

            if (pc.parent != slot)
            {
                pc.SetParent(slot, false);
                pc.SetAsLastSibling();
            }

            ApplyUnifiedProgressContainerLayout(pc);
        }
    }

    static void ApplyUnifiedProgressContainerLayout(RectTransform pc)
    {
        var pcLE = pc.GetComponent<LayoutElement>() ?? pc.gameObject.AddComponent<LayoutElement>();
        pcLE.preferredHeight = 8f;
        pcLE.minHeight = 8f;
        pcLE.flexibleWidth = 1f;
        pcLE.minWidth = 0f;
        pcLE.ignoreLayout = false;

        bool legacyCentered = Mathf.Approximately(pc.anchorMin.x, pc.anchorMax.x)
                           && pc.anchorMin.x > 0.1f && pc.anchorMin.x < 0.9f;
        if (legacyCentered)
        {
            float y = pc.anchorMin.y;
            pc.anchorMin = new Vector2(0f, y);
            pc.anchorMax = new Vector2(1f, y);
            pc.pivot = new Vector2(0.5f, 0.5f);
            pc.sizeDelta = new Vector2(0f, 8f);
        }

        pc.offsetMin = new Vector2(8f, pc.offsetMin.y);
        pc.offsetMax = new Vector2(-8f, pc.offsetMax.y);

        var pad = pc.GetComponent<HorizontalLayoutGroup>() ?? pc.gameObject.AddComponent<HorizontalLayoutGroup>();
        pad.padding = new RectOffset(8, 8, 0, 0);
        pad.childControlWidth = pad.childControlHeight = true;
        pad.childForceExpandWidth = true;
        pad.childForceExpandHeight = true;

        var bar = pc.Find("ProgressBar") as RectTransform;
        if (bar == null) return;
        var barLE = bar.GetComponent<LayoutElement>() ?? bar.gameObject.AddComponent<LayoutElement>();
        barLE.preferredHeight = 4f;
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

            // Update LayoutElement — Phase 9.2: 72 px for mobile tap targets
            var barLE = rt.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
            barLE.preferredHeight = HudLayoutConstants.StudioMainTabHeight;
            barLE.minHeight       = 60f;
            barLE.flexibleHeight  = 0f;

            // Upscale tab labels — Phase 9.2: 15 px bold
            foreach (var tmp in rt.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                tmp.fontSize = HudLayoutConstants.StudioMainTabFontSize;
                tmp.enableAutoSizing = false;
                tmp.fontStyle = FontStyles.Bold;
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

    /// <summary>Phase 10.0 — restore existing studio panels; remove dashboard artifacts.</summary>
    static void PatchStudioVisualFinal(RectTransform switcher)
    {
        var estudio = FindChildPanel(switcher, "EstudioPanel");
        if (estudio == null) return;

        DestroyIfExists(estudio, "StudioDashContent");
        DestroyIfExists(estudio, "StudioHomeBlock");
        DestroyIfExists(estudio, "StudioLowerSpacer");

        var subTabBar = estudio.Find("StudioSubTabBar");
        var subContent = estudio.Find("StudioSubContent");
        if (subTabBar != null) subTabBar.gameObject.SetActive(true);
        if (subContent != null) subContent.gameObject.SetActive(true);

        var bar = estudio.Find("BonificationsBar");
        if (bar != null) bar.gameObject.SetActive(true);

        EnsureStudioChildOrder(estudio);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(estudio);
        var stage = estudio.GetComponentInChildren<StudioVisualStage>(true);
        stage?.ApplyLayout();

        PatchMejorasReadability(estudio);
        PatchContractProgressBars(estudio);
        PatchContractActiveCards(estudio);
    }

    static void EnsureStudioChildOrder(RectTransform estudio)
    {
        var header     = estudio.Find("StudioHeader");
        var stage      = estudio.Find("StudioVisualStage");
        var bar        = estudio.Find("BonificationsBar");
        var subTabBar  = estudio.Find("StudioSubTabBar");
        var subContent = estudio.Find("StudioSubContent");

        int idx = 0;
        if (header     != null) header.SetSiblingIndex(idx++);
        if (stage      != null) stage.SetSiblingIndex(idx++);
        if (bar        != null) bar.SetSiblingIndex(idx++);
        if (subTabBar  != null) subTabBar.SetSiblingIndex(idx++);
        if (subContent != null) subContent.SetSiblingIndex(idx++);
    }

    static void PatchDeptSingleColumn(Transform deptBar)
    {
        var content = deptBar.Find("DeptScroll/Viewport/Content");
        if (content == null) return;

        var grid = content.GetComponent<GridLayoutGroup>();
        if (grid != null) Object.Destroy(grid);

        var rowHLG = content.GetComponent<HorizontalLayoutGroup>();
        if (rowHLG != null) Object.Destroy(rowHLG);

        var cards = content.GetComponentsInChildren<DepartmentMiniCardUI>(true);
        foreach (var card in cards)
            card.transform.SetParent(content, false);

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            var child = content.GetChild(i);
            if (child.GetComponent<DepartmentMiniCardUI>() == null)
                Object.Destroy(child.gameObject);
        }
    }

    static void PatchMejorasReadability(RectTransform estudio)
    {
        var mejoras = estudio.Find("StudioSubContent/StudioPanel_MEJORAS");
        if (mejoras == null) return;

        PatchMejorasSubTabs(mejoras);

        foreach (var vlg in mejoras.GetComponentsInChildren<VerticalLayoutGroup>(true))
        {
            vlg.padding = new RectOffset(12, 12, 10, 12);
            vlg.spacing = 10;
        }

        foreach (var tmp in mejoras.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp.name.Contains("Name") || tmp.name == "Title")
                tmp.fontSize = Mathf.Max(tmp.fontSize, 15f);
            else if (tmp.name.Contains("Desc") || tmp.name.Contains("Effect"))
                tmp.fontSize = Mathf.Max(tmp.fontSize, 12f);

            // Fix baked legacy green text colors — restore cinematic palette
            if (CinematicTheme.IsLegacyGreen(tmp.color))
                tmp.color = CinematicTheme.GoldBase;
        }

        // Fix baked green image backgrounds in Mejoras panel — use transparent so no colored box appears
        foreach (var img in mejoras.GetComponentsInChildren<Image>(true))
        {
            if (img.GetComponent<Button>() != null) continue;
            if (img.GetComponent<Slider>() != null) continue;
            if (!CinematicTheme.IsLegacyGreen(img.color)) continue;

            var name = img.gameObject.name;
            // Card root backgrounds should keep a visible color — everything else (overlays, badges) → clear
            var isCardRoot = img.GetComponent<UpgradeCardUI>() != null
                          || img.GetComponentInParent<UpgradeCardUI>(false) == null;
            img.color = isCardRoot ? CinematicTheme.CardBg : Color.clear;
        }
    }

    static string[] MejorasSubTabLabels => new[]
    {
        Loc.Get(LocKeys.MejorasProduction),
        Loc.Get(LocKeys.MejorasStaff),
        Loc.Get(LocKeys.MejorasResearch),
        Loc.Get(LocKeys.MejorasMarketing),
    };

    static void PatchMejorasSubTabs(Transform mejorasRoot)
    {
        var tabBar = mejorasRoot.Find("MejorasPanel/MejorasSubTabBar") as RectTransform
                  ?? mejorasRoot.Find("MejorasSubTabBar") as RectTransform;
        if (tabBar == null) return;

        var squareLayout = tabBar.GetComponent<SquareTileRowLayout>();
        if (squareLayout != null) Object.Destroy(squareLayout);

        var barLE = tabBar.GetComponent<LayoutElement>() ?? tabBar.gameObject.AddComponent<LayoutElement>();
        barLE.preferredHeight = HudLayoutConstants.MejorasSubTabBarHeight;
        barLE.minHeight       = HudLayoutConstants.MejorasSubTabBarHeight;
        barLE.flexibleHeight  = 0f;

        var hlg = tabBar.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null)
        {
            hlg.padding = new RectOffset(4, 4, 4, 4);
            hlg.spacing = 6;
            hlg.childForceExpandWidth  = true;
            hlg.childForceExpandHeight = true;
        }

        int tabIndex = 0;
        for (int i = 0; i < tabBar.childCount; i++)
        {
            var tab = tabBar.GetChild(i) as RectTransform;
            if (tab == null || tab.GetComponent<Button>() == null) continue;

            var tabLE = tab.GetComponent<LayoutElement>() ?? tab.gameObject.AddComponent<LayoutElement>();
            tabLE.flexibleWidth   = 1f;
            tabLE.preferredWidth  = -1f;
            tabLE.preferredHeight = HudLayoutConstants.MejorasSubTabBarHeight - 8f;
            tabLE.minHeight       = 56f;

            var label = tab.Find("Label")?.GetComponent<TextMeshProUGUI>()
                     ?? tab.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null && tabIndex < MejorasSubTabLabels.Length)
            {
                label.text = MejorasSubTabLabels[tabIndex];
                label.fontSize = HudLayoutConstants.MejorasSubTabFontSize;
                label.fontStyle = FontStyles.Bold;
                label.enableAutoSizing = false;
                label.overflowMode = TextOverflowModes.Overflow;
                label.textWrappingMode = TextWrappingModes.NoWrap;
            }

            tabIndex++;
        }
    }

    static void PatchContractProgressBars(RectTransform estudio)
    {
        var contratos = estudio.Find("StudioSubContent/StudioPanel_CONTRATOS");
        if (contratos == null) return;

        foreach (var row in contratos.GetComponentsInChildren<Transform>(true))
        {
            if (row.name != "ProgRow") continue;
            var le = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 6f;
        }

        foreach (var slider in contratos.GetComponentsInChildren<Slider>(true))
        {
            if (slider.name != "ProgBar") continue;
            var le = slider.GetComponent<LayoutElement>() ?? slider.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 6f;
            le.minHeight = 6f;
        }
    }

    static void PatchContractActiveCards(RectTransform estudio)
    {
        var contratos = estudio.Find("StudioSubContent/StudioPanel_CONTRATOS");
        if (contratos == null) return;

        foreach (var card in contratos.GetComponentsInChildren<ContractCardUI>(true))
        {
            if (card.isCandidateMode || card.isHistoryMode) continue;

            var cardLE = card.GetComponent<LayoutElement>();
            if (cardLE != null)
            {
                cardLE.preferredHeight = ContractsPanelUI.ActiveCardHeight;
                cardLE.minHeight = ContractsPanelUI.ActiveCardHeight;
            }

            foreach (var row in card.GetComponentsInChildren<Transform>(true))
            {
                if (row.name != "ProgRow") continue;
                var le = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 6f;
            }

            if (card.rewardText != null)
                card.rewardText.gameObject.SetActive(false);
            if (card.descText != null)
                card.descText.gameObject.SetActive(false);
        }
    }

    static void DestroyIfExists(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) Object.Destroy(t.gameObject);
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
            if (i < labels.Length)
                UIIconGraphic.ApplyTabIcon(tab, UIIconCatalog.GetNavigation((MainHudTab)i));
        }
    }

    static void PatchTopBarSettingsButton()
    {
        var topBar = Object.FindAnyObjectByType<TopBarUI>(FindObjectsInactive.Include);
        topBar?.PatchSettingsIcon();
        topBar?.PatchTopBarVisuals();
    }

    /// <summary>
    /// Phase 12.3B — root-cause color enforcement for baked scenes.
    /// Fixes stale GameHudSkins.asset colors AND scene-baked #2ECC71 / #27AE60 that ignore CinematicTheme.
    /// </summary>
    static void PatchLegacyHudColors(RectTransform switcher)
    {
        PatchStudioHeaderAccentPanels(switcher);
        PatchLegacyGreenActionButtons();
    }

    static void PatchStudioHeaderAccentPanels(RectTransform switcher)
    {
        var estudio = FindChildPanel(switcher, "EstudioPanel");
        if (estudio == null) return;

        var header = estudio.Find("StudioHeader");
        if (header == null) return;

        // CS logo block — city name badge with premium framing
        var iconPanel = header.Find("Icon");
        SetPanelColor(iconPanel, CinematicTheme.DeepRedBase);
        if (iconPanel != null)
        {
            // Ensure the city badge auto-sizes: remove fixed width constraints
            var iconLE = iconPanel.GetComponent<LayoutElement>();
            if (iconLE != null)
            {
                iconLE.preferredWidth = -1f;
                iconLE.minWidth = 100f;
                iconLE.flexibleWidth = 0f;
            }
        }

        // Mission block — GameObject "MissionCard"
        SetPanelColor(header.Find("MissionCard"), CinematicTheme.DeepRedBase);

        var iconTxt = header.Find("Icon/IconTxt")?.GetComponent<TextMeshProUGUI>();
        if (iconTxt != null)
        {
            iconTxt.fontSize = 20f;
            iconTxt.fontStyle = FontStyles.Bold;
            iconTxt.color = CinematicTheme.TextPrimary;
            // Ensure icon text has enough width for city names
            var iconTxtLE = iconTxt.GetComponent<LayoutElement>() ?? iconTxt.gameObject.AddComponent<LayoutElement>();
            iconTxtLE.flexibleWidth = 1f;
            iconTxt.overflowMode = TextOverflowModes.Ellipsis;
            iconTxt.enableAutoSizing = true;
            iconTxt.fontSizeMax = 20f;
            iconTxt.fontSizeMin = 14f;
        }

        // XP bar fill — functional progress, gold not green
        var xpFill = header.Find("XpBlock/XPBar/Fill Area/Fill")?.GetComponent<Image>();
        if (xpFill != null) xpFill.color = CinematicTheme.GoldBase;

        var levelLine = header.Find("XpBlock/LevelLineText")?.GetComponent<TextMeshProUGUI>();
        if (levelLine != null)
        {
            levelLine.color = CinematicTheme.GoldBright;
            levelLine.alignment = TextAlignmentOptions.MidlineLeft;
        }

        var xpText = header.Find("XpBlock/XPText")?.GetComponent<TextMeshProUGUI>();
        if (xpText != null)
            xpText.alignment = TextAlignmentOptions.MidlineLeft;

        var xpBlock = header.Find("XpBlock");
        if (xpBlock != null)
        {
            var xpVLG = xpBlock.GetComponent<VerticalLayoutGroup>();
            if (xpVLG != null)
                xpVLG.childAlignment = TextAnchor.UpperLeft;
        }

        var headerHLG = header.GetComponent<HorizontalLayoutGroup>();
        if (headerHLG != null)
            headerHLG.childAlignment = TextAnchor.MiddleLeft;
    }

    static void SetPanelColor(Transform t, Color color)
    {
        if (t == null) return;
        var img = t.GetComponent<Image>();
        if (img != null) img.color = color;
    }

    static void PatchLegacyGreenActionButtons()
    {
        foreach (var btn in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (btn == null) continue;
            var img = btn.GetComponent<Image>();
            if (img == null || !CinematicTheme.IsLegacyActionButtonGreen(img.color)) continue;

            // Re-apply through skin pipeline so elevation layers stay consistent.
            HudSkinProvider.ApplyButton(img, HudButtonVariant.Success);
        }

        // Also patch any non-button Image components using the legacy green (backgrounds, fills, etc.)
        foreach (var img in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (img == null) continue;
            if (img.GetComponent<Button>() != null) continue;
            if (img.GetComponent<Slider>() != null) continue;
            if (!CinematicTheme.IsLegacyActionButtonGreen(img.color)) continue;

            var name = img.gameObject.name;
            // Only patch structural background images, not intentional fill/bar elements
            if (name.Contains("Fill") || name.Contains("Bar") || name.Contains("Progress")) continue;

            img.color = CinematicTheme.CardBg2;
        }
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
