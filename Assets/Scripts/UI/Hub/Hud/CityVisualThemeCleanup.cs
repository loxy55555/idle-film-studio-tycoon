using UnityEngine;

/// <summary>Clears theme content from StudioVisualStage layers without removing layer shells.</summary>
public static class CityVisualThemeCleanup
{
    public static void ClearAllLayers(StudioVisualStage stage)
    {
        if (stage == null) return;

        ClearLayerChildren(stage.BackgroundLayer);
        ClearLayerChildren(stage.SetLayer);
        ClearLayerChildren(stage.CharacterLayer);
        ClearLayerChildren(stage.EquipmentLayer);
        ClearLayerChildren(stage.EffectsLayer);
        ClearLayerChildren(stage.OverlayLayer);
    }

    public static void ClearLayerChildren(RectTransform layer)
    {
        if (layer == null) return;

        for (int i = layer.childCount - 1; i >= 0; i--)
            DestroyObject(layer.GetChild(i).gameObject);
    }

    static void DestroyObject(Object obj)
    {
        if (obj == null) return;
        // Rename immediately so CreateLayerRoot cannot reuse this root
        // during the same frame before Destroy is processed.
        if (obj is GameObject go)
            go.name = $"__pending_destroy_{go.GetInstanceID()}";
        if (Application.isPlaying)
            Object.Destroy(obj);
        else
            Object.DestroyImmediate(obj);
    }
}
