using UnityEngine;
using UnityEngine.UI;

/// <summary>Legendary collection card accents — frame + poster treatment (Phase 9.0).</summary>
public static class CollectionLegendaryVisual
{
    static readonly Color FrameGoldBright = new Color(0.92f, 0.76f, 0.18f, 0.95f);
    static readonly Color FrameGoldLocked = new Color(0.62f, 0.48f, 0.14f, 0.85f);
    static readonly Color PosterDim       = new Color(0.55f, 0.55f, 0.55f, 1f);

    public static void ApplyLegendaryFrame(Image frame, bool locked)
    {
        if (frame == null) return;
        var c = locked ? FrameGoldLocked : FrameGoldBright;
        frame.color = c;
    }

    public static void ApplyLegendaryPoster(Image poster, MovieConfig cfg, bool locked, bool discovered)
    {
        if (poster == null || cfg == null) return;

        MoviePosterVisual.Apply(poster, cfg);

        if (locked || !discovered)
        {
            poster.color = poster.color * PosterDim;
            if (poster.sprite == null)
            {
                if (ColorUtility.TryParseHtmlString(cfg.posterColorHex, out Color hint))
                    poster.color = Color.Lerp(new Color(0.08f, 0.08f, 0.12f), hint, locked ? 0.35f : 0.55f);
            }
        }
    }
}
