using DG.Tweening;

using UnityEngine;

using UnityEngine.EventSystems;
using UnityEngine.UI;



/// <summary>Scales button down on press and back on release (Phase 11 — via UIAnimationService).</summary>

[RequireComponent(typeof(RectTransform))]

public class UIButtonScale : MonoBehaviour,

    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerClickHandler, ICancelHandler

{

    [SerializeField] float pressedScale = UIAnimationService.ButtonPressScale;

    [SerializeField] float duration     = UIAnimationService.ButtonPressDuration;



    RectTransform _rt;

    Vector3       _baseScale = Vector3.one;

    Tween         _tween;

    bool          _pressed;



    void Awake()

    {

        _rt = GetComponent<RectTransform>();

        ResetScale();

    }



    void OnEnable() => ResetScale();



    public void OnPointerDown(PointerEventData eventData)

    {

        if (!IsInteractable()) return;

        _pressed = true;

        TweenScale(_baseScale * pressedScale);

    }



    public void OnPointerUp(PointerEventData eventData)

    {

        _pressed = false;

        TweenScale(_baseScale);

    }



    public void OnPointerExit(PointerEventData eventData)

    {

        if (!_pressed) return;

        _pressed = false;

        TweenScale(_baseScale);

    }



    public void OnPointerClick(PointerEventData eventData)
    {
        ResetScale();
        if (!IsInteractable() || IsBottomNavTab()) return;
        AudioManager.Instance?.PlaySfx("softclick");
    }

    bool IsBottomNavTab()
    {
        var nav = GameObject.Find("BottomNav");
        return nav != null && transform.IsChildOf(nav.transform);
    }



    public void OnCancel(BaseEventData eventData)

    {

        _pressed = false;

        ResetScale();

    }



    void LateUpdate()

    {

        if (_rt == null) return;

        if (_pressed) return;

        if (_tween != null && _tween.IsActive()) return;

        if (_rt.localScale != _baseScale)

            _rt.localScale = _baseScale;

    }



    void TweenScale(Vector3 target)

    {

        _tween?.Kill();

        if (target == _baseScale * pressedScale)

            _tween = UIAnimationService.PlayButtonPress(_rt, _baseScale);

        else

            _tween = UIAnimationService.PlayButtonRelease(_rt, _baseScale);

    }



    void OnDisable() => ResetScale();



    /// <summary>Cancel press tween and restore base scale (e.g. after interactable toggled mid-press).</summary>

    public void ResetScale()

    {

        _pressed = false;

        _tween?.Kill();

        _tween = null;

        if (_rt != null)

        {

            _rt.DOKill(false);

            _rt.localScale = _baseScale;

        }

    }



    bool IsInteractable()

    {

        var selectable = GetComponent<Selectable>();

        return selectable == null || selectable.IsInteractable();

    }

}


