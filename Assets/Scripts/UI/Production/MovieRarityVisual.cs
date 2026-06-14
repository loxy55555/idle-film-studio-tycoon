using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Rarity accent colors for production UI — Cinematic 2.0 (Phase 11).</summary>
public static class MovieRarityVisual
{
    static readonly Color CommonGrey     = new Color(0.55f, 0.58f, 0.62f);
    static readonly Color UncommonGreen  = new Color(0.28f, 0.78f, 0.42f);
    static readonly Color RareBlue       = new Color(0.35f, 0.55f, 0.97f);
    static readonly Color EpicPurple     = new Color(0.67f, 0.34f, 0.95f);
    static readonly Color LegendaryGold  = new Color(0.94f, 0.78f, 0.25f);

    public static string GetLocKey(MovieRarity rarity) => rarity switch
    {
        MovieRarity.Rare       => LocKeys.ProdRarityRare,
        MovieRarity.Epic       => LocKeys.ProdRarityEpic,
        MovieRarity.Legendary  => LocKeys.ProdRarityLegendary,
        _                      => LocKeys.ProdRarityCommon,
    };

    public static Color GetAccentColor(MovieRarity rarity) => rarity switch
    {
        MovieRarity.Rare       => RareBlue,
        MovieRarity.Epic       => EpicPurple,
        MovieRarity.Legendary  => LegendaryGold,
        _                      => CommonGrey,
    };

    public static Color GetBadgeTextColor(MovieRarity rarity) => rarity switch
    {
        MovieRarity.Common    => new Color(0.70f, 0.82f, 0.96f),
        MovieRarity.Rare      => new Color(0.62f, 0.78f, 1.00f),
        MovieRarity.Epic      => new Color(0.86f, 0.62f, 1.00f),
        MovieRarity.Legendary => new Color(1.00f, 0.88f, 0.35f),
        _                     => Color.white,
    };

    /// <summary>Legacy frame hook for collection/premiere sub-frames — no full-card border.</summary>
    public static void ApplyFrame(Image frame, MovieRarity rarity)
    {
        if (frame == null) return;
        frame.color = Color.clear;
        RemoveLegacyStrip(frame.transform);
    }

    /// <summary>Phase 13.4A — thin full-card border for production offer/slot cards.</summary>
    public static void ApplyCardBorder(RectTransform cardRoot, MovieRarity rarity)
    {
        if (cardRoot == null) return;

        RemoveLegacyStrip(cardRoot);
        var accent = GetAccentColor(rarity);
        const float thickness = 5f;
        const string borderRoot = "__RarityBorder";

        var root = cardRoot.Find(borderRoot);
        if (root == null)
        {
            var go = new GameObject(borderRoot, typeof(RectTransform));
            go.transform.SetParent(cardRoot, false);
            root = go.transform;
            Stretch(root as RectTransform);
            CreateEdge(root, "Top",    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, thickness));
            CreateEdge(root, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, thickness));
            CreateEdge(root, "Left",   new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(thickness, 0f));
            CreateEdge(root, "Right",  new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(thickness, 0f));
        }

        root.SetAsLastSibling();
        foreach (Transform edge in root)
        {
            var img = edge.GetComponent<Image>();
            if (img == null) continue;
            img.color = new Color(accent.r, accent.g, accent.b, 1f);
            img.raycastTarget = false;
        }
        root.gameObject.SetActive(true);
    }

    static void CreateEdge(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = Vector2.zero;
        var le = go.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
    }

    static void RemoveLegacyStrip(Transform scope)
    {
        var strip = scope.Find("__RarityStrip");
        if (strip == null) return;
        if (Application.isPlaying) Object.Destroy(strip.gameObject);
        else Object.DestroyImmediate(strip.gameObject);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    public static void ApplyBadge(TextMeshProUGUI badge, MovieRarity rarity)
    {
        if (badge == null) return;
        badge.text = ProductionLoc.GetRarityLabel(rarity);
        badge.color = GetBadgeTextColor(rarity);
    }
}
