using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Forwards drag gestures to the nearest parent ScrollRect.</summary>
[DisallowMultipleComponent]
public class ScrollDragForwarder : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IInitializePotentialDragHandler
{
    ScrollRect _scroll;

    void Awake() => _scroll = GetComponentInParent<ScrollRect>();

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (_scroll != null)
            ExecuteEvents.Execute(_scroll.gameObject, eventData, ExecuteEvents.initializePotentialDrag);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_scroll != null)
            ExecuteEvents.Execute(_scroll.gameObject, eventData, ExecuteEvents.beginDragHandler);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_scroll != null)
            ExecuteEvents.Execute(_scroll.gameObject, eventData, ExecuteEvents.dragHandler);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_scroll != null)
            ExecuteEvents.Execute(_scroll.gameObject, eventData, ExecuteEvents.endDragHandler);
    }
}
