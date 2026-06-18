using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Smoothly tweens a Slider value via UIAnimationService.
/// Adds a subtle shimmer pulse on the fill area whenever the bar is active (0 &lt; value &lt; 1).</summary>
[RequireComponent(typeof(Slider))]
public class SmoothProgressBar : MonoBehaviour
{
    [SerializeField] float duration = UIAnimationService.BarDuration;

    Slider _slider;
    Tween  _tween;
    Image  _shimmerImg;
    Tween  _shimmerLoop;

    void Awake()
    {
        _slider = GetComponent<Slider>();
        ReadOnlySlider.Configure(_slider);
        BuildShimmer();
    }

    void BuildShimmer()
    {
        var fill = _slider?.fillRect;
        if (fill == null) return;
        var go = new GameObject("__Shimmer", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(fill, false);
        _shimmerImg = go.GetComponent<Image>();
        _shimmerImg.color = new Color(1f, 1f, 1f, 0f);
        _shimmerImg.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    void StartShimmer()
    {
        if (_shimmerImg == null) return;
        if (_shimmerLoop != null && _shimmerLoop.IsActive()) return;
        _shimmerLoop = _shimmerImg.DOFade(0.18f, 1.4f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    void StopShimmer()
    {
        _shimmerLoop?.Kill();
        _shimmerLoop = null;
        if (_shimmerImg != null) _shimmerImg.color = new Color(1f, 1f, 1f, 0f);
    }

    public void SetTarget(float value, float max)
    {
        if (_slider == null) return;
        _tween?.Kill();
        _tween = UIAnimationService.AnimateProgressBar(_slider, value, max, duration);
        float normalized = max > 0 ? Mathf.Clamp01(value / max) : 0f;
        if (normalized > 0f && normalized < 1f) StartShimmer();
        else StopShimmer();
    }

    public void SetNormalized(float normalized)
    {
        if (_slider == null) return;
        _tween?.Kill();
        _tween = UIAnimationService.AnimateProgressBarNormalized(_slider, normalized, duration);
        if (normalized > 0f && normalized < 1f) StartShimmer();
        else StopShimmer();
    }

    void OnDisable()
    {
        StopShimmer();
    }

    void OnDestroy()
    {
        StopShimmer();
    }
}
