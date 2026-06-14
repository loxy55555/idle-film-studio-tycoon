using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>Animates numeric TMP labels (REP, XP, level) without instant jumps.</summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class AnimatedValueText : MonoBehaviour
{
    public enum FormatMode
    {
        Integer,
        OneDecimal,
        CustomPrefixSuffix,
    }

    [SerializeField] FormatMode formatMode = FormatMode.Integer;
    [SerializeField] string prefix = "";
    [SerializeField] string suffix = "";
    [SerializeField] float duration = UIAnimationService.CurrencyDuration;

    TextMeshProUGUI _text;
    float _displayed;
    float _target;
    Tween _tween;

    public FormatMode Mode { get => formatMode; set => formatMode = value; }
    public string Prefix { get => prefix; set => prefix = value; }
    public string Suffix { get => suffix; set => suffix = value; }

    void Awake() => _text = GetComponent<TextMeshProUGUI>();

    public void SetValue(float target, bool animate = true)
    {
        if (_text == null) return;
        _target = target;
        if (!animate || !isActiveAndEnabled)
        {
            _displayed = target;
            ApplyText();
            return;
        }

        _tween?.Kill();
        _tween = UIAnimationService.AnimateFloatText(_text, _displayed, target, duration, Format, v => _displayed = v);
    }

    string Format(float v) => formatMode switch
    {
        FormatMode.OneDecimal => prefix + v.ToString("0.#") + suffix,
        FormatMode.CustomPrefixSuffix => prefix + ((int)Mathf.Round(v)).ToString() + suffix,
        _ => prefix + ((int)Mathf.Round(v)).ToString() + suffix,
    };

    void ApplyText() => _text.text = Format(_displayed);

    void OnDisable()
    {
        _tween?.Kill();
        if (_text != null) _text.text = Format(_target);
    }
}
