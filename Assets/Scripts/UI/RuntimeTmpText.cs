using TMPro;
using UnityEngine;

/// <summary>
/// Creates runtime TMP labels with the project default font so text is actually visible.
/// </summary>
public static class RuntimeTmpText
{
    public static TextMeshProUGUI Create(
        Transform parent,
        string text,
        float fontSize,
        Color color,
        FontStyles style = FontStyles.Normal,
        TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft,
        string objectName = "Txt")
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        ApplyDefaultFont(tmp);
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        return tmp;
    }

    public static void ApplyDefaultFont(TextMeshProUGUI tmp)
    {
        if (tmp == null || tmp.font != null) return;
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
    }
}
