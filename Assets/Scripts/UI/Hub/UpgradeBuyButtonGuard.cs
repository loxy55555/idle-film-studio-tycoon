using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Tracks pointer down/up on a single upgrade buy button (Phase 8.4).</summary>
public class UpgradeBuyButtonGuard : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    Func<string> _getUpgradeId;

    public void Bind(Func<string> getUpgradeId) => _getUpgradeId = getUpgradeId;

    public void OnPointerDown(PointerEventData eventData)
    {
        UpgradeUiInteractionGate.RegisterPointerDown(_getUpgradeId?.Invoke() ?? name);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        UpgradeUiInteractionGate.RegisterPointerUp(_getUpgradeId?.Invoke() ?? name);
    }
}
