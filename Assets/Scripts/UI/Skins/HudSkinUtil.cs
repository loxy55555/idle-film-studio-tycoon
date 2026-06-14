using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared sprite/color application for HUD skin assets.</summary>
public static class HudSkinUtil
{
    public static void ApplySpriteOrColor(Image image, Sprite sprite, Color fallbackColor)
    {
        if (image == null) return;

        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            image.color = Color.white;
            image.preserveAspect = false;
        }
        else
        {
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = fallbackColor;
        }
    }

    /// <summary>
    /// Sets the base color on a card background image — color only, no material layers.
    /// Individual card components apply CinematicTheme.ApplyPremiumMaterial() themselves.
    /// </summary>
    public static void ApplyPremiumSurface(Image image, Color baseColor)
    {
        if (image == null) return;
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = baseColor;
    }
}
