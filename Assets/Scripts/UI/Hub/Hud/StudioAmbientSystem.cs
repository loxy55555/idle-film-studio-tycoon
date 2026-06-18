using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Phase 14.3B.3 — Minimal cinematic studio ambient system.
///
/// Active systems:
///   – Parallax: BackgroundLayer shifts ±3 px horizontally over a 25–35 s cycle.
///     Only the background moves; UI, bars, and buttons are unaffected.
///   – Dust: 3–5 warm motes, 20–50 px, alpha 0.025–0.055, slow rising drift.
///     Evoke illuminated dust visible through a studio window — not game particles.
///
/// Removed in 14.3B.3:
///   – Light Streaks (BuildLightStreaks): rotated rectangle beams visible as
///     translucent UI elements over the illustration, breaking immersion.
///   – Depth Layer (BuildDepthLayer): large white blobs perceived as overlays
///     rather than depth, especially noticeable on the Garage background.
///
/// No ParticleSystem. No shaders. No overlays. No bloom.
/// City-agnostic: both systems survive all 8 city theme swaps automatically.
/// </summary>
[DisallowMultipleComponent]
public class StudioAmbientSystem : MonoBehaviour
{
    const string RootName = "__AmbientSystem__";

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Start() => Build();

    void OnDestroy() => DOTween.Kill(this);

    // ── Build ─────────────────────────────────────────────────────────────

    void Build()
    {
        var prev = transform.Find(RootName);
        if (prev != null) Destroy(prev.gameObject);

        var rootGo = new GameObject(RootName, typeof(RectTransform));
        rootGo.transform.SetParent(transform, false);
        var root = rootGo.GetComponent<RectTransform>();
        StretchFill(root);

        var overlayLayer = transform.Find("OverlayLayer");
        if (overlayLayer != null)
            rootGo.transform.SetSiblingIndex(overlayLayer.GetSiblingIndex());

        // Sistema 1: Parallax — tween on BackgroundLayer itself, not on its children.
        // CityVisualThemeCleanup only destroys children, so the tween survives city swaps.
        var stage = GetComponent<StudioVisualStage>();
        if (stage?.BackgroundLayer != null)
            BuildParallax(stage.BackgroundLayer);

        // Sistema 2: Dust — 3-5 cinematic illuminated motes
        BuildDustLayer(root);
    }

    // ── Sistema 1: Parallax ───────────────────────────────────────────────
    // Moves BackgroundLayer ±3 px horizontally. Full cycle: 25–35 s.
    // InOutSine spends most time near the extremes → mid-range crossing
    // is so brief it is never consciously perceived.

    void BuildParallax(RectTransform bgLayer)
    {
        float halfCycle = Random.Range(25f, 35f) * 0.5f;
        bgLayer.anchoredPosition = new Vector2(-3f, 0f);

        bgLayer.DOAnchorPos(new Vector2(3f, 0f), halfCycle)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true)
            .SetId(this);
    }

    // ── Sistema 2: Dust Layer ─────────────────────────────────────────────
    // 3–5 motes, 20–50 px, warm tint. Slow diagonal drift rising upward.
    // Each mote has an independent alpha oscillation to avoid synchronised
    // breathing. All values kept well below perceptible threshold.

    void BuildDustLayer(RectTransform parent)
    {
        int count = Random.Range(3, 6);
        for (int i = 0; i < count; i++)
        {
            float ax    = Random.Range(0.05f, 0.95f);
            float ay    = Random.Range(0.10f, 0.85f);
            float size  = Random.Range(20f,   50f);
            float dur   = Random.Range(15f,   30f);
            float alpha = Random.Range(0.025f, 0.055f);

            var rt = MakeElement(parent, $"Dust{i}", ax, ay, size, size * Random.Range(0.75f, 1.25f),
                new Color(0.98f, 0.94f, 0.85f, alpha));

            rt.DOAnchorPos(
                    new Vector2(Random.Range(-12f, 12f), Random.Range(8f, 25f)), dur)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetDelay(Random.Range(0f, dur * 0.8f))
                .SetUpdate(true)
                .SetId(this);

            float peak = Mathf.Min(alpha * Random.Range(1.2f, 1.6f), 0.08f);
            rt.GetComponent<Image>()
                .DOFade(peak, Random.Range(10f, 22f))
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetDelay(Random.Range(2f, 12f))
                .SetUpdate(true)
                .SetId(this);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static RectTransform MakeElement(RectTransform parent, string name,
        float anchorX, float anchorY, float w, float h, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = rt.anchorMax = new Vector2(anchorX, anchorY);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(w, h);
        rt.anchoredPosition = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color         = color;
        img.raycastTarget = false;
        return rt;
    }

    static void StretchFill(RectTransform rt)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.offsetMin        = rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }
}
