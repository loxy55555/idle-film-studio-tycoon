using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Department identity via icon/motif only — no card background tint (Phase 11.5).</summary>
public class DepartmentThemeVisual : MonoBehaviour
{
    [SerializeField] Image backdropImage;
    [SerializeField] TextMeshProUGUI motifText;
    [SerializeField] Image motifIcon;
    [SerializeField] Sprite themeSprite;

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

        if (motifIcon == null)
        {
            var iconT = transform.Find("Content/CardMainRow/IconColumn/Badge/IconImage")
                     ?? transform.Find("Content/CardMainRow/InfoColumn/HeaderRow/Badge/IconImage");
            if (iconT != null) motifIcon = iconT.GetComponent<Image>();
        }

        if (backdropImage != null)
        {
            backdropImage.sprite = null;
            backdropImage.color = Color.clear;
        }

        var sprite = UIIconCatalog.GetDepartment(type);
        if (motifIcon != null)
        {
            // Use Color.white so the icon renders in its natural colors without a tint filter
            UIIconGraphic.Apply(motifIcon, sprite, Color.white);
            if (sprite != null && motifText != null)
                motifText.gameObject.SetActive(false);
        }

        if (motifText != null)
        {
            if (sprite == null)
                motifText.gameObject.SetActive(true);
            motifText.text = theme.motifSymbol;
            motifText.color = theme.tint;
            motifText.enableAutoSizing = false;
            motifText.fontSize = 28f;
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
