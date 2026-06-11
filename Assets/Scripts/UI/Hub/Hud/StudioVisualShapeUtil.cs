using UnityEngine;
using UnityEngine.UI;

/// <summary>Creates anchor-based UI Image placeholders inside StudioVisualStage layers.</summary>
public static class StudioVisualShapeUtil
{
    public static RectTransform CreateBlock(
        Transform parent,
        string name,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float alpha = 1f)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;

        var img = go.GetComponent<Image>();
        img.color = new Color(color.r, color.g, color.b, alpha);
        img.raycastTarget = false;
        return rt;
    }

    public static void StretchFull(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }
}
