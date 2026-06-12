using UnityEngine;
using UnityEngine.UI;

/// <summary>Legendary collection card accents — frame + poster treatment (Phase 9.0).</summary>
public static class CollectionLegendaryVisual
{
    static readonly Color FrameGoldBright = new Color(0.95f, 0.78f, 0.12f, 0.95f);
    static readonly Color FrameGoldLocked = new Color(0.72f, 0.58f, 0.10f, 0.88f);
    static readonly Color PosterDim       = new Color(0.55f, 0.55f, 0.55f, 1f);
    static readonly Color Silhouette      = new Color(0.06f, 0.06f, 0.12f);

    public static void ApplyLegendaryFrame(Image frame, bool locked)
    {
        if (frame == null) return;
        var c = locked ? FrameGoldLocked : FrameGoldBright;
        frame.color = c;
    }

    /// <summary>Locked legendaries use silhouette only — no real poster (Phase 6.8).</summary>
    public static void ApplyLegendaryPoster(Image poster, MovieConfig cfg, bool locked, bool discovered)
    {
        if (poster == null || cfg == null) return;

        if (locked)
        {
            ApplySilhouette(poster, cfg);
            return;
        }

        MoviePosterVisual.Apply(poster, cfg);

        if (!discovered)
        {
            poster.color = poster.color * PosterDim;
            if (poster.sprite == null &&
                ColorUtility.TryParseHtmlString(cfg.posterColorHex, out Color hint))
                poster.color = Color.Lerp(new Color(0.08f, 0.08f, 0.12f), hint, 0.55f);
        }
    }

    public static void ApplySilhouette(Image poster, MovieConfig cfg)
    {
        if (poster == null) return;
        poster.sprite = null;
        poster.color = Silhouette;
        if (cfg != null &&
            ColorUtility.TryParseHtmlString(cfg.posterColorHex, out Color hint))
            poster.color = Color.Lerp(Silhouette, hint, 0.18f);
    }
}
