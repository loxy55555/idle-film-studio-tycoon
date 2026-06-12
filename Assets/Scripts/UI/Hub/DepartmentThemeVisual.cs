using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Soft thematic backdrop hook per department — placeholder until art assets arrive.</summary>
public class DepartmentThemeVisual : MonoBehaviour
{
    [SerializeField] Image backdropImage;
    [SerializeField] TextMeshProUGUI motifText;
    [SerializeField] Image motifIcon;
    [SerializeField] Sprite themeSprite;
    [SerializeField] float backdropAlpha = 0.07f;

    public void Configure(DepartmentType type, Image backdrop, TextMeshProUGUI motif, Image iconHook = null)
    {
        backdropImage = backdrop;
        motifText = motif;
        motifIcon = iconHook;
        Apply(type);
    }

    public void Apply(DepartmentType type)
    {
        var theme = DepartmentThemeCatalog.Get(type);

        if (backdropImage != null)
        {
            if (themeSprite != null)
            {
                backdropImage.sprite = themeSprite;
                backdropImage.color = new Color(1f, 1f, 1f, backdropAlpha);
            }
            else
            {
                backdropImage.sprite = null;
                backdropImage.color = new Color(theme.tint.r, theme.tint.g, theme.tint.b, backdropAlpha);
            }
        }

        if (motifIcon != null)
        {
            if (themeSprite != null)
            {
                motifIcon.sprite = themeSprite;
                motifIcon.color = new Color(1f, 1f, 1f, backdropAlpha * 1.2f);
                motifIcon.gameObject.SetActive(true);
            }
            else
            {
                motifIcon.sprite = null;
                motifIcon.gameObject.SetActive(false);
            }
        }

        if (motifText != null)
        {
            motifText.text = theme.motifSymbol;
            motifText.color = new Color(theme.tint.r, theme.tint.g, theme.tint.b, backdropAlpha * 2.5f);
            motifText.enableAutoSizing = true;
            motifText.fontSizeMin = 28f;
            motifText.fontSizeMax = 72f;
        }
    }
}

public static class DepartmentThemeCatalog
{
    public readonly struct ThemeDef
    {
        public readonly Color tint;
        public readonly string motifSymbol;

        public ThemeDef(Color tint, string motifSymbol)
        {
            this.tint = tint;
            this.motifSymbol = motifSymbol;
        }
    }

    public static ThemeDef Get(DepartmentType type) => type switch
    {
        DepartmentType.Cinematography => new ThemeDef(new Color(0.20f, 0.45f, 0.75f), "◉"),
        DepartmentType.Sound          => new ThemeDef(new Color(0.25f, 0.55f, 0.85f), "∿"),
        DepartmentType.Director       => new ThemeDef(new Color(0.55f, 0.35f, 0.75f), "▣"),
        DepartmentType.Art            => new ThemeDef(new Color(0.85f, 0.55f, 0.15f), "◐"),
        DepartmentType.Costume        => new ThemeDef(new Color(0.75f, 0.20f, 0.45f), "⌇"),
        DepartmentType.Makeup         => new ThemeDef(new Color(0.85f, 0.25f, 0.55f), "✦"),
        DepartmentType.Editor         => new ThemeDef(new Color(0.55f, 0.35f, 0.85f), "✂"),
        DepartmentType.Actors         => new ThemeDef(new Color(0.85f, 0.25f, 0.25f), "◯◯"),
        DepartmentType.Lighting       => new ThemeDef(new Color(0.95f, 0.75f, 0.25f), "☼"),
        DepartmentType.Grip           => new ThemeDef(new Color(0.80f, 0.45f, 0.15f), "⌁"),
        DepartmentType.Producer       => new ThemeDef(new Color(0.20f, 0.70f, 0.40f), "◎"),
        _                             => new ThemeDef(new Color(0.35f, 0.38f, 0.50f), "•"),
    };
}
