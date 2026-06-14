using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Smoothly tweens a Slider value via UIAnimationService.</summary>
[RequireComponent(typeof(Slider))]
public class SmoothProgressBar : MonoBehaviour
{
    [SerializeField] float duration = UIAnimationService.BarDuration;

    Slider _slider;
    Tween  _tween;

    void Awake()
    {
        _slider = GetComponent<Slider>();
        ReadOnlySlider.Configure(_slider);
    }

    public void SetTarget(float value, float max)
    {
        if (_slider == null) return;
        _tween?.Kill();
        _tween = UIAnimationService.AnimateProgressBar(_slider, value, max, duration);
    }

    public void SetNormalized(float normalized)
    {
        if (_slider == null) return;
        _tween?.Kill();
        _tween = UIAnimationService.AnimateProgressBarNormalized(_slider, normalized, duration);
    }
}
