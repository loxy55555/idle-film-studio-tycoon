using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cinematic visual theme — Phase 11.7 elevation pass.
/// Depth comes from per-component elevation layers, not global overlays.
/// </summary>
public static class CinematicTheme
{
    // ── Palette (Phase 11.8 — premium color pass, muted-green accent system) ──

    public static readonly Color DeepBg        = H("#050C16");
    public static readonly Color PanelBg       = H("#081220");
    public static readonly Color CardBg        = H("#0D1A2C");
    public static readonly Color CardBg2       = H("#101F33");
    public static readonly Color PopupBg       = H("#121F34");
    public static readonly Color CardHighlight = H("#142840");
    public static readonly Color CardShadow    = H("#030A12");

    public static readonly Color BorderSubtle  = H("#163048");
    public static readonly Color BorderAccent  = H("#1F3D5C");
    public static readonly Color BorderGold    = H("#5C3A10");

    // ── Cinematic Gold (Awards, Legendary, Prestigious progress) ─────────────
    public static readonly Color GoldDim       = H("#B8832A");
    public static readonly Color GoldBase      = H("#D9A441");
    public static readonly Color GoldBright    = H("#F0C66A");

    // ── Red Carpet (Premieres, Galas, Special Events) ─────────────────────────
    public static readonly Color RedCarpet     = H("#8B1A2A");
    public static readonly Color RedCarpetBright = H("#C0243A");

    // ── Silver (Nominations, Secondary Prestige) ──────────────────────────────
    public static readonly Color SilverDim     = H("#6B7E8E");
    public static readonly Color SilverBase    = H("#9BAEBB");

    // ── Bronze Action V2 — cinematic dark bronze (Oscar / luxury hotel) ─────────
    public static readonly Color BronzeBase      = H("#8A6A3D");
    public static readonly Color BronzeHover     = H("#A8874F");
    public static readonly Color BronzeHighlight = H("#C9A46A");
    public static readonly Color BronzeShadow    = H("#4D3921");

    // ── Deep Red (TopBar resource blocks) ─────────────────────────────────────
    public static readonly Color DeepRedBase     = H("#4A1820");
    public static readonly Color DeepRedHover    = H("#6A2430");
    public static readonly Color DeepRedShadow   = H("#250C12");

    // ── Premium Dark Green (Progress only — not buttons) ──────────────────────
    public static readonly Color GreenDark     = H("#1E3D2F");
    public static readonly Color GreenMid      = H("#3D7358");
    public static readonly Color GreenBright   = H("#4A7A65");

    public static readonly Color ProgressFill  = H("#556B5D");
    public static readonly Color ButtonSuccess = BronzeBase;
    public static readonly Color ButtonReady   = BronzeHover;
    public static readonly Color ButtonDisabled= H("#3A3228");

    public static readonly Color TextPrimary   = H("#F2F2F2");
    public static readonly Color TextSecondary = H("#B9C2CF");
    public static readonly Color TextDim       = H("#6B7E94");

    // ── Layer names ───────────────────────────────────────────────────────────

    public const string LayerHighlight  = "__CardHighlight";
    public const string LayerBorderHL   = "__BorderHL";
    public const string LayerBottomSh   = "__BottomSh";
    public const string LayerBtnTopHL   = "__BtnTopHL";
    public const string LayerBtnBottom  = "__BtnBottom";

    static readonly string[] AllLayerNames =
    {
        LayerHighlight, LayerBorderHL, LayerBottomSh, LayerBtnTopHL, LayerBtnBottom,
        "__InnerShadow", "__Grain",
    };

    // ── Rarity palette ────────────────────────────────────────────────────────

    public static readonly Color RarityCommonBg  = H("#0A1524");
    public static readonly Color RarityCommonFg  = H("#5D7FA0");
    public static readonly Color RarityRareBg    = H("#0C1630");
    public static readonly Color RarityRareFg    = H("#5A7EF5");
    public static readonly Color RarityEpicBg    = H("#160C2E");
    public static readonly Color RarityEpicFg    = H("#A855F7");
    public static readonly Color RarityLegBg     = H("#241603");
    public static readonly Color RarityLegFg     = H("#F0C66A");

    // ── Elevation API ─────────────────────────────────────────────────────────
    //
    // Level 0 — Background: color only (DeepBg). No layers.
    // Level 1 — Panel:     recessed section surface.
    // Level 2 — Card:      sits above panels — gradient + shadow + lift.
    // Level 3 — Button:    interactive volume — top shine + bottom weight.
    // Level 4 — Popup:     highest elevation — strong shadow + bright surface.

    /// <summary>Level 1 — section / panel surface (behind cards). Skipped on structural containers.</summary>
    public static void ApplyElevationPanel(RectTransform panel)
    {
        if (panel == null || ShouldSkipElevationSurface(panel)) return;

        var bg = panel.GetComponent<Image>();
        if (bg != null && bg.sprite == null)
            bg.color = PanelBg;

        RemoveElevationLayers(panel, keepShadow: false);
        AddGradientLayer(panel, CardHighlight, 0.38f, 0.10f);
        AddTopBorder(panel, 0.07f);
        EnsureMaterialLayersBehindContent(panel);
    }

    /// <summary>Level 2 — discrete card (department, upgrade, contract, movie…).</summary>
    public static void ApplyPremiumMaterial(RectTransform card) => ApplyElevationCard(card);

    public static void ApplyPremiumCard(RectTransform card) => ApplyElevationCard(card);

    public static void ApplyElevationCard(RectTransform card)
    {
        if (card == null || ShouldSkipElevationSurface(card)) return;

        var bg = card.GetComponent<Image>();
        if (bg != null && bg.sprite == null)
            bg.color = CardBg;

        RemoveElevationLayers(card, keepShadow: false);
        AddGradientLayer(card, CardHighlight, 0.40f, 0.44f);
        AddTopBorder(card, 0.16f);
        AddBottomInnerShadow(card, 0.18f, 0.22f);
        EnsureDropShadow(bg, 0.34f, new Vector2(0f, -3f));
        EnsureMaterialLayersBehindContent(card);
    }

    /// <summary>Level 3 — action button (upgrade, claim, continue…).</summary>
    public static void ApplyElevationButton(RectTransform btn)
    {
        if (btn == null || ShouldSkipElevationSurface(btn)) return;

        var bg = btn.GetComponent<Image>();
        if (bg != null && bg.sprite != null) return;

        if (bg != null)
            bg.color = BronzeBase;

        RemoveElevationLayers(btn, keepShadow: false);
        AddGradientLayer(btn, BronzeHighlight, 0.58f, 0.42f, LayerBtnTopHL);
        AddColoredBottomShadow(btn, BronzeShadow, 0.38f, 0.62f, LayerBtnBottom);
        AddTopBorder(btn, 0.14f);
        EnsureDropShadow(bg, 0.34f, new Vector2(0f, -2f));
        EnsureMaterialLayersBehindContent(btn, LayerBtnTopHL, LayerBtnBottom);
        EnsureInteractiveContentAboveLayers(btn);
    }

    /// <summary>Level 4 — modal / reward / premiere popup card.</summary>
    public static void ApplyElevationPopup(RectTransform popup)
    {
        if (popup == null || ShouldSkipElevationSurface(popup)) return;

        var bg = popup.GetComponent<Image>();
        if (bg != null && bg.sprite == null)
            bg.color = PopupBg;

        RemoveElevationLayers(popup, keepShadow: false);
        AddGradientLayer(popup, CardHighlight, 0.35f, 0.48f);
        AddTopBorder(popup, 0.22f);
        AddBottomInnerShadow(popup, 0.14f, 0.26f);
        EnsureDropShadow(bg, 0.52f, new Vector2(0f, -6f));
        EnsureMaterialLayersBehindContent(popup);
    }

    /// <summary>Strip elevation layers from containers that received the wrong pass.</summary>
    public static void RemoveMaterialLayers(RectTransform rt) =>
        RemoveElevationLayers(rt, keepShadow: false);

    // ── Department card neutralisation ────────────────────────────────────────

    public static void NeutralizeDepartmentCard(RectTransform card)
    {
        if (card == null) return;
        RemoveAccentStrip(card);
    }

    public static void RemoveAccentStrip(RectTransform card)
    {
        if (card == null) return;
        var strip = card.Find("AccentStrip");
        if (strip != null)
            strip.gameObject.SetActive(false);
    }

    // ── Layer builders ────────────────────────────────────────────────────────

    static void AddGradientLayer(RectTransform rt, Color topColor, float anchorY, float alpha,
        string layerName = LayerHighlight)
    {
        var existing = rt.Find(layerName);
        if (existing == null)
        {
            var go = new GameObject(layerName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(rt, false);
            var layerRt = go.GetComponent<RectTransform>();
            layerRt.anchorMin = new Vector2(0f, anchorY);
            layerRt.anchorMax = Vector2.one;
            layerRt.offsetMin = layerRt.offsetMax = Vector2.zero;
            existing = go.transform;
        }

        var img = existing.GetComponent<Image>();
        img.color = new Color(topColor.r, topColor.g, topColor.b, alpha);
        img.raycastTarget = false;

        // Exclude from any parent LayoutGroup so the layer doesn't participate
        // as a layout cell (e.g. HorizontalLayoutGroup on ProdSlot).
        var le = existing.GetComponent<LayoutElement>() ?? existing.gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
    }

    static void AddTopBorder(RectTransform rt, float alpha)
    {
        var existing = rt.Find(LayerBorderHL);
        if (existing == null)
        {
            var go = new GameObject(LayerBorderHL, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(rt, false);
            var layerRt = go.GetComponent<RectTransform>();
            layerRt.anchorMin = new Vector2(0f, 1f);
            layerRt.anchorMax = new Vector2(1f, 1f);
            layerRt.pivot = new Vector2(0.5f, 1f);
            layerRt.anchoredPosition = Vector2.zero;
            layerRt.sizeDelta = new Vector2(0f, 1f);
            existing = go.transform;
        }

        existing.GetComponent<Image>().color = new Color(1f, 1f, 1f, alpha);
        existing.GetComponent<Image>().raycastTarget = false;

        var le = existing.GetComponent<LayoutElement>() ?? existing.gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
    }

    static void AddBottomInnerShadow(RectTransform rt, float anchorY, float alpha,
        string layerName = LayerBottomSh)
    {
        AddColoredBottomShadow(rt, Color.black, anchorY, alpha, layerName);
    }

    static void AddColoredBottomShadow(RectTransform rt, Color shadowColor, float anchorY, float alpha,
        string layerName = LayerBottomSh)
    {
        var existing = rt.Find(layerName);
        if (existing == null)
        {
            var go = new GameObject(layerName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(rt, false);
            var layerRt = go.GetComponent<RectTransform>();
            layerRt.anchorMin = Vector2.zero;
            layerRt.anchorMax = new Vector2(1f, anchorY);
            layerRt.offsetMin = layerRt.offsetMax = Vector2.zero;
            existing = go.transform;
        }

        existing.GetComponent<Image>().color = new Color(shadowColor.r, shadowColor.g, shadowColor.b, alpha);
        existing.GetComponent<Image>().raycastTarget = false;

        var le = existing.GetComponent<LayoutElement>() ?? existing.gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
    }

    static void EnsureDropShadow(Graphic graphic, float alpha, Vector2 distance)
    {
        if (graphic == null) return;

        var shadow = graphic.GetComponent<Shadow>() ?? graphic.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, alpha);
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    static void EnsureInteractiveContentAboveLayers(RectTransform rt)
    {
        if (rt == null) return;

        foreach (var tmp in rt.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            tmp.enabled = true;
            tmp.transform.SetAsLastSibling();
        }
    }

    static void EnsureMaterialLayersBehindContent(RectTransform rt, params string[] extraLayerNames)
    {
        if (rt == null) return;

        // Material layers must render behind all content siblings (factory or scene-built).
        int index = 0;
        PlaceLayerAt(rt, LayerHighlight, ref index);
        PlaceLayerAt(rt, LayerBottomSh, ref index);
        foreach (var name in extraLayerNames)
            PlaceLayerAt(rt, name, ref index);
        PlaceLayerAt(rt, LayerBorderHL, ref index);
    }

    static void PlaceLayerAt(RectTransform rt, string layerName, ref int index)
    {
        var t = rt.Find(layerName);
        if (t == null) return;
        t.SetSiblingIndex(index++);
    }

    /// <summary>
    /// Scroll areas, viewports and layout shells are not visual surfaces — no elevation layers.
    /// </summary>
    public static bool ShouldSkipElevationSurface(RectTransform rt)
    {
        if (rt == null) return true;
        if (rt.GetComponent<ScrollRect>() != null) return true;
        if (rt.GetComponent<RectMask2D>() != null) return true;

        switch (rt.name)
        {
            case "Viewport":
            case "Content":
            case "SlotsContent":
            case "MultiProdScroll":
                return true;
        }

        return false;
    }

    static void RemoveElevationLayers(RectTransform rt, bool keepShadow)
    {
        if (rt == null) return;

        foreach (var name in AllLayerNames)
        {
            var t = rt.Find(name);
            if (t == null) continue;
            // Rename before Destroy so that deferred-destroy "zombie" objects
            // are invisible to subsequent rt.Find() calls in the same frame.
            t.gameObject.name = "__REMOVED__";
            if (Application.isPlaying) Object.Destroy(t.gameObject);
            else Object.DestroyImmediate(t.gameObject);
        }

        if (keepShadow) return;

        var shadow = rt.GetComponent<Shadow>();
        if (shadow == null) return;
        if (Application.isPlaying) Object.Destroy(shadow);
        else Object.DestroyImmediate(shadow);
    }

    // ── Rarity helpers ────────────────────────────────────────────────────────

    public static Color GetRarityAccent(MovieRarity r) => r switch
    {
        MovieRarity.Rare      => RarityRareFg,
        MovieRarity.Epic      => RarityEpicFg,
        MovieRarity.Legendary => RarityLegFg,
        _                     => RarityCommonFg,
    };

    public static Color GetRarityBg(MovieRarity r) => r switch
    {
        MovieRarity.Rare      => RarityRareBg,
        MovieRarity.Epic      => RarityEpicBg,
        MovieRarity.Legendary => RarityLegBg,
        _                     => RarityCommonBg,
    };

    static Color H(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }

    // ── Legacy baked-color detection (Phase 12.3B audit) ─────────────────────

    static readonly Color LegacyAccentGreen = H("#2ECC71");
    static readonly Color LegacyBtnGreen     = H("#27AE60");

    public static bool IsLegacyGreen(Color c) =>
        ColorsClose(c, LegacyAccentGreen) ||
        ColorsClose(c, LegacyBtnGreen) ||
        ColorsClose(c, GreenMid);

    public static bool IsLegacyActionButtonGreen(Color c) =>
        ColorsClose(c, LegacyBtnGreen) ||
        ColorsClose(c, LegacyAccentGreen);

    static bool ColorsClose(Color a, Color b, float epsilon = 0.03f) =>
        Mathf.Abs(a.r - b.r) < epsilon &&
        Mathf.Abs(a.g - b.g) < epsilon &&
        Mathf.Abs(a.b - b.b) < epsilon;
}
