using UnityEngine;

/// <summary>Builds and maintains the StudioVisualStage layer stack (back → front).</summary>
public static class StudioVisualStageLayers
{
    public static readonly StudioVisualLayerKind[] RenderOrder =
    {
        StudioVisualLayerKind.Background,
        StudioVisualLayerKind.Set,
        StudioVisualLayerKind.Character,
        StudioVisualLayerKind.Equipment,
        StudioVisualLayerKind.Effects,
        StudioVisualLayerKind.Overlay,
    };

    public static string GetLayerName(StudioVisualLayerKind kind) => kind switch
    {
        StudioVisualLayerKind.Background => "BackgroundLayer",
        StudioVisualLayerKind.Set        => "SetLayer",
        StudioVisualLayerKind.Character  => "CharacterLayer",
        StudioVisualLayerKind.Equipment  => "EquipmentLayer",
        StudioVisualLayerKind.Effects    => "EffectsLayer",
        StudioVisualLayerKind.Overlay    => "OverlayLayer",
        _                                => kind.ToString(),
    };

    public static void EnsureHierarchy(StudioVisualStage stage)
    {
        if (stage == null) return;

        var root = stage.transform as RectTransform;
        if (root == null) return;

        var resolved = new RectTransform[RenderOrder.Length];
        for (int i = 0; i < RenderOrder.Length; i++)
            resolved[i] = EnsureLayer(root, RenderOrder[i], i);

        stage.BindLayers(resolved);
    }

    static RectTransform EnsureLayer(RectTransform stageRoot, StudioVisualLayerKind kind, int siblingIndex)
    {
        var layerName = GetLayerName(kind);
        var existing = stageRoot.Find(layerName) as RectTransform;
        if (existing != null)
        {
            var marker = existing.GetComponent<StudioVisualStageLayer>();
            if (marker == null)
                marker = existing.gameObject.AddComponent<StudioVisualStageLayer>();
            marker.Configure(kind, siblingIndex);
            return existing;
        }

        var go = new GameObject(layerName, typeof(RectTransform));
        go.transform.SetParent(stageRoot, false);
        var layer = go.GetComponent<RectTransform>();
        var layerMarker = go.AddComponent<StudioVisualStageLayer>();
        layerMarker.Configure(kind, siblingIndex);
        return layer;
    }

    public static void ApplyStretch(RectTransform rt)
    {
        if (rt == null) return;

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.anchoredPosition = Vector2.zero;
    }
}
