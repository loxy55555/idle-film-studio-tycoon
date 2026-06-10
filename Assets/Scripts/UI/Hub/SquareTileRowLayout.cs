using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sizes a horizontal tile row so each child is a square: height = width / tileCount.
/// Only recalculates when width actually changes (avoids layout feedback loops).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SquareTileRowLayout : MonoBehaviour
{
    public int   tileCount = 4;
    public float maxTileSize;

    HorizontalLayoutGroup _hlg;
    LayoutElement         _rowLE;
    RectTransform         _rt;
    float                 _lastWidth = -1f;
    Coroutine             _deferredApply;

    void Awake()
    {
        _rt    = GetComponent<RectTransform>();
        _hlg   = GetComponent<HorizontalLayoutGroup>();
        _rowLE = GetComponent<LayoutElement>();
        if (_hlg != null)
            _hlg.childForceExpandHeight = false;
    }

    void OnEnable()
    {
        _lastWidth = -1f;
        Apply();
        if (_rt != null && _rt.rect.width < 1f)
            RequestDeferredApply();
    }

    void OnDisable()
    {
        if (_deferredApply != null)
        {
            StopCoroutine(_deferredApply);
            _deferredApply = null;
        }
    }

    public void Apply()
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        if (_rt.rect.width < 1f)
        {
            RequestDeferredApply();
            return;
        }

        if (Mathf.Abs(_rt.rect.width - _lastWidth) < 0.5f) return;
        _lastWidth = _rt.rect.width;

        if (_hlg == null) _hlg = GetComponent<HorizontalLayoutGroup>();
        if (_rowLE == null) _rowLE = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();

        int count = Mathf.Max(tileCount, 1);
        float pad = _hlg != null ? _hlg.padding.left + _hlg.padding.right : 0f;
        float gap = _hlg != null ? _hlg.spacing * (count - 1) : 0f;
        float side = (_rt.rect.width - pad - gap) / count;
        if (maxTileSize > 0f) side = Mathf.Min(side, maxTileSize);
        side = Mathf.Max(side, 28f);

        _rowLE.preferredHeight = side;
        _rowLE.minHeight       = side;
        _rowLE.flexibleHeight  = 0f;

        if (_hlg != null)
        {
            _hlg.childForceExpandWidth  = true;
            _hlg.childForceExpandHeight = false;
        }

        for (int i = 0; i < _rt.childCount; i++)
        {
            var child = _rt.GetChild(i);
            var le = child.GetComponent<LayoutElement>();
            if (le == null) le = child.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth   = 1f;
            le.flexibleHeight  = 0f;
            le.preferredHeight = side;
            le.minHeight       = side;
        }
    }

    public void RequestDeferredApply()
    {
        if (!isActiveAndEnabled) return;
        if (_deferredApply != null) return;
        _deferredApply = StartCoroutine(DeferredApplyRoutine());
    }

    IEnumerator DeferredApplyRoutine()
    {
        for (int i = 0; i < 4; i++)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            _lastWidth = -1f;
            Apply();
            if (_rt != null && _rt.rect.width >= 1f)
                break;
        }
        _deferredApply = null;
    }
}
