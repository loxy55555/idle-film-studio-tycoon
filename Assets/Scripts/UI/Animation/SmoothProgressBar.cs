using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Smoothly tweens a Slider value instead of snapping.</summary>
[RequireComponent(typeof(Slider))]
public class SmoothProgressBar : MonoBehaviour
{
    [SerializeField] private float duration = 0.25f;

    private Slider _slider;
    private Tween  _tween;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
        ReadOnlySlider.Configure(_slider);
    }

    public void SetTarget(float value, float max)
    {
        if (_slider == null) return;
        _slider.maxValue = max;
        _tween?.Kill();
        _tween = _slider.DOValue(value, duration).SetEase(Ease.OutCubic).SetUpdate(true);
    }

    public void SetNormalized(float normalized)
    {
        if (_slider == null) return;
        _tween?.Kill();
        _tween = _slider.DOValue(normalized, duration).SetEase(Ease.OutCubic).SetUpdate(true);
    }
}
