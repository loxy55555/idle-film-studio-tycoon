using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Helpers to apply catalog sprites to runtime-built UI.</summary>
public static class UIIconGraphic
{
    public static void Apply(Image image, Sprite sprite, Color tint = default)
    {
        if (image == null) return;

        if (sprite != null)
        {
            HudSkinUtil.ApplySpriteOrColor(image, sprite, Color.white);
            image.preserveAspect = true;
            if (tint.a > 0f) image.color = tint;
            image.gameObject.SetActive(true);
        }
        else
        {
            image.sprite = null;
            image.gameObject.SetActive(false);
        }
    }

    public static Image EnsureChildIcon(Transform parent, string name, float size = 28f)
    {
        if (parent == null) return null;

        var existing = parent.Find(name)?.GetComponent<Image>();
        if (existing != null) return existing;

        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    public static void ApplyTabIcon(Transform tab, Sprite sprite, float size = 26f)
    {
        if (tab == null) return;

        var iconTmp = tab.Find("Icon")?.GetComponent<TextMeshProUGUI>();
        var iconImg = EnsureChildIcon(tab, "IconSprite", size);

        if (sprite != null)
        {
            Apply(iconImg, sprite);
            if (iconTmp != null) iconTmp.gameObject.SetActive(false);
        }
        else if (iconTmp != null)
        {
            if (iconImg != null) iconImg.gameObject.SetActive(false);
            iconTmp.gameObject.SetActive(true);
        }
    }

    public static Image ApplyCoverIcon(Transform cover, Sprite sprite, string name = "CoverIcon", float size = 56f)
    {
        if (cover == null) return null;

        var iconImg = EnsureChildIcon(cover, name, size);
        var emoji = cover.Find("Emoji")?.GetComponent<TextMeshProUGUI>();

        if (sprite != null)
        {
            Stretch(iconImg.rectTransform);
            Apply(iconImg, sprite);
            if (emoji != null) emoji.gameObject.SetActive(false);
        }
        else if (emoji != null)
        {
            if (iconImg != null) iconImg.gameObject.SetActive(false);
            emoji.gameObject.SetActive(true);
        }

        return iconImg;
    }

    public static Image ApplyInlineIcon(RectTransform textRt, Sprite sprite, string name = "LeadingIcon", float size = 22f)
    {
        if (textRt == null || sprite == null) return null;

        var parent = textRt.parent;
        if (parent == null) return null;

        var icon = EnsureChildIcon(parent, name, size);
        Apply(icon, sprite);

        var iconLE = icon.GetComponent<LayoutElement>() ?? icon.gameObject.AddComponent<LayoutElement>();
        iconLE.preferredWidth = iconLE.preferredHeight = size;
        iconLE.minWidth = iconLE.minHeight = size;
        iconLE.flexibleWidth = 0f;

        icon.transform.SetSiblingIndex(textRt.GetSiblingIndex());
        return icon;
    }

    public static void ApplyGenreRarityIcon(Transform iconWrap, Sprite sprite, TextMeshProUGUI fallbackText)
    {
        if (iconWrap == null) return;

        var iconImg = EnsureChildIcon(iconWrap, "RarityIconSprite", 36f);
        if (sprite != null)
        {
            Stretch(iconImg.rectTransform);
            Apply(iconImg, sprite);
            if (fallbackText != null) fallbackText.gameObject.SetActive(false);
        }
        else if (fallbackText != null)
        {
            if (iconImg != null) iconImg.gameObject.SetActive(false);
            fallbackText.gameObject.SetActive(true);
        }
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
