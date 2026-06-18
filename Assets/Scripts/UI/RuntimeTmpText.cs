using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Creates runtime TMP labels with the project default font so text is actually visible.
/// Provides a multilingual font (NotoSansJP) that covers CJK + Latin in one font asset,
/// eliminating fallback-chain timing issues that caused chars=0 for Japanese text.
/// </summary>
public static class RuntimeTmpText
{
    /// <summary>
    /// Per-screen scale factor for targeted adjustments; kept at 1.0 after FASE 16.2
    /// determined that a global multiplier caused cascading overflow across all panels.
    /// Use screen-specific LE heights and explicit fontSizeMin/Max instead.
    /// </summary>
    public const float MobileScale = 1.0f;

    static TMP_FontAsset _multilingualFont;

    /// <summary>
    /// Returns NotoSansJP_SDF from the global fallback list.
    /// NotoSansJP covers both Latin and CJK characters and is used directly
    /// for any TMP that must render Japanese without relying on the fallback chain.
    /// </summary>
    public static TMP_FontAsset GetMultilingualFont()
    {
        if (_multilingualFont != null) return _multilingualFont;

        var fallbacks = TMP_Settings.fallbackFontAssets;
        if (fallbacks != null)
            foreach (var fb in fallbacks)
                if (fb != null && (fb.name.Contains("NotoSans") || fb.name.Contains("Noto")))
                    return _multilingualFont = fb;

        return TMP_Settings.defaultFontAsset;
    }

    /// <summary>
    /// Creates a TMP label using the project default font (Latin text).
    /// </summary>
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
        tmp.text = text;
        tmp.fontSize = fontSize * MobileScale;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        return tmp;
    }

    /// <summary>
    /// Creates a TMP label using NotoSansJP as the primary font, which supports both
    /// Latin and CJK characters. The font MUST be set before text to avoid chars=0
    /// for Japanese (TMP caches the glyph lookup at the moment text is assigned).
    /// </summary>
    public static TextMeshProUGUI CreateMultilingual(
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
        // Font MUST be assigned before text — TMP's glyph lookup is cached at text-set time.
        // If the primary font cannot render a character at that moment, chars=0 persists
        // even after font changes + ForceMeshUpdate. NotoSansJP covers both Latin and CJK.
        var mlFont = GetMultilingualFont();
        if (mlFont != null) tmp.font = mlFont;
        tmp.text = text;
        tmp.fontSize = fontSize * MobileScale;
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
