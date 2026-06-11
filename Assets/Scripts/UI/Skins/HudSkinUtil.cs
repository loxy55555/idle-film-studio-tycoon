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
}
