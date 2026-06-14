using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>Animates a TMP label counting smoothly toward passive income (DOTween + per-frame ticks).</summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class AnimatedMoneyText : MonoBehaviour
{
    [SerializeField] private float snapDuration = 0.35f;
    [SerializeField] private float smoothSpeed  = 14f;

    private TextMeshProUGUI _text;
    private StudioManager   _studio;
    private double          _displayed;
    private double          _target;
    private Tween           _snapTween;
    private bool            _useSmoothTick;

    private void Awake() => _text = GetComponent<TextMeshProUGUI>();

    private void OnEnable()
    {
        GameHub.OnGameReady += Bind;
        if (GameHub.Instance != null) Bind();
    }

    private void OnDisable()
    {
        GameHub.OnGameReady -= Bind;
        Unbind();
    }

    void Bind()
    {
        Unbind();
        _studio = GameHub.Instance?.studio;
        if (_studio == null) return;

        _displayed = _target = _studio.MoneyExact;
        _text.text = FormatMoney((long)_displayed);
        _studio.OnMoneyDisplayChanged += OnDisplayTick;
        _studio.OnMoneyChanged        += OnMoneySnap;
        _useSmoothTick = true;
    }

    void Unbind()
    {
        if (_studio == null) return;
        _studio.OnMoneyDisplayChanged -= OnDisplayTick;
        _studio.OnMoneyChanged        -= OnMoneySnap;
        _studio = null;
    }

    void OnDisplayTick(double exact)
    {
        if (!_useSmoothTick) return;
        _target = exact;
    }

    void OnMoneySnap(long amount)
    {
        if (_studio == null) return;
        _useSmoothTick = false;
        _snapTween?.Kill();
        double from = _displayed;
        _target = _studio.MoneyExact;
        _snapTween = UIAnimationService.AnimateCurrency(_text, from, _target, snapDuration,
            AnimatedMoneyText.FormatMoney, v => _displayed = v)
            .OnComplete(() => _useSmoothTick = true);
    }

    private void Update()
    {
        if (!_useSmoothTick || _text == null) return;
        if (Mathf.Abs((float)(_target - _displayed)) < 0.5f)
        {
            _displayed = _target;
        }
        else
        {
            _displayed += (_target - _displayed) * smoothSpeed * Time.deltaTime;
        }
        _text.text = FormatMoney((long)_displayed);
    }

    /// <summary>Direct set (e.g. fallback when no StudioManager binding).</summary>
    public void SetValue(long target, bool animate = true)
    {
        if (_text == null) return;
        _target = target;
        if (!animate)
        {
            _displayed = target;
            _text.text = FormatMoney(target);
            return;
        }
        OnMoneySnap(target);
    }

    public static string FormatMoney(long v)
    {
        if (v >= 1_000_000_000_000L) return $"${v / 1_000_000_000_000f:0.#}T";
        if (v >= 1_000_000_000L)     return $"${v / 1_000_000_000f:0.#}B";
        if (v >= 1_000_000L)         return $"${v / 1_000_000f:0.#}M";
        if (v >= 1_000L)             return $"${v / 1_000f:0.#}K";
        return $"${v:N0}";
    }
}
