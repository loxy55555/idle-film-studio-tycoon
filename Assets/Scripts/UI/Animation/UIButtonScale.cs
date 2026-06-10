using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Scales button down on press and back on release.</summary>
[RequireComponent(typeof(RectTransform))]
public class UIButtonScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float pressedScale = 0.92f;
    [SerializeField] private float duration     = 0.08f;

    private RectTransform _rt;
    private Vector3     _baseScale;
    private Tween       _tween;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _baseScale = _rt.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)  => TweenScale(_baseScale * pressedScale);
    public void OnPointerUp(PointerEventData eventData)   => TweenScale(_baseScale);
    public void OnPointerExit(PointerEventData eventData) => TweenScale(_baseScale);

    void TweenScale(Vector3 target)
    {
        _tween?.Kill();
        _tween = _rt.DOScale(target, duration).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    private void OnDisable()
    {
        _tween?.Kill();
        if (_rt != null) _rt.localScale = _baseScale;
    }
}
