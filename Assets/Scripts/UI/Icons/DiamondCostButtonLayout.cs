using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FASE 17C.1 — Diamond cost buttons using the same sprite as store / wallet.
/// Avoids Unicode gem emoji (renders as □ in LiberationSans TMP on mobile).
/// </summary>
public static class DiamondCostButtonLayout
{
    const float DefaultIconSize = 24f;
    const float DefaultFontSize = 16f;

    /// <summary>Builds [prefix?] + diamond sprite + cost number inside a button root.</summary>
    public static void Build(Transform buttonRoot, string prefixLabel, int cost,
        float iconSize = DefaultIconSize, float fontSize = DefaultFontSize, Color textColor = default)
    {
        if (buttonRoot == null) return;

        var color = textColor.a > 0f ? textColor : Color.white;

        for (int i = buttonRoot.childCount - 1; i >= 0; i--)
            Object.Destroy(buttonRoot.GetChild(i).gameObject);

        var hlg = buttonRoot.GetComponent<HorizontalLayoutGroup>()
                  ?? buttonRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(10, 10, 6, 6);
        hlg.spacing = 6;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = false;

        if (!string.IsNullOrEmpty(prefixLabel))
        {
            var prefix = RuntimeTmpText.Create(buttonRoot, prefixLabel, fontSize, color,
                FontStyles.Bold, TextAlignmentOptions.Center, "PrefixLabel");
            prefix.raycastTarget = false;
            prefix.enableAutoSizing = true;
            prefix.fontSizeMin = fontSize - 2f;
            prefix.fontSizeMax = fontSize + 2f;
        }

        var iconWrap = new GameObject("DiamondIconWrap", typeof(RectTransform));
        iconWrap.transform.SetParent(buttonRoot, false);
        var iconLE = iconWrap.AddComponent<LayoutElement>();
        iconLE.preferredWidth = iconLE.preferredHeight = iconSize;
        iconLE.minWidth = iconLE.minHeight = iconSize;
        iconLE.flexibleWidth = 0f;

        var icon = UIIconGraphic.EnsureChildIcon(iconWrap.transform, "DiamondIcon", iconSize);
        UIIconGraphic.Apply(icon, UIIconCatalog.GetResourceDiamonds());

        var costLbl = RuntimeTmpText.Create(buttonRoot, cost.ToString(), fontSize, color,
            FontStyles.Bold, TextAlignmentOptions.Center, "CostLabel");
        costLbl.raycastTarget = false;
        costLbl.enableAutoSizing = true;
        costLbl.fontSizeMin = fontSize - 2f;
        costLbl.fontSizeMax = fontSize + 2f;
    }
}
