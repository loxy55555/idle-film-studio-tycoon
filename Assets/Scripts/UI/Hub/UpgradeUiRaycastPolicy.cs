using UnityEngine;
using UnityEngine.UI;

/// <summary>Limits upgrade card raycasts to the buy button only (Phase 8.4).</summary>
public static class UpgradeUiRaycastPolicy
{
    public static void ApplyCard(Transform cardRoot, Button buyButton)
    {
        if (cardRoot == null) return;

        foreach (var graphic in cardRoot.GetComponentsInChildren<Graphic>(true))
        {
            if (buyButton != null &&
                (graphic.transform == buyButton.transform || graphic.transform.IsChildOf(buyButton.transform)))
            {
                graphic.raycastTarget = graphic == buyButton.GetComponent<Image>();
                continue;
            }

            graphic.raycastTarget = false;
        }
    }

    public static void EnsureCardHeight(LayoutElement cardLE, float height)
    {
        if (cardLE == null) return;
        cardLE.preferredHeight = height;
        cardLE.minHeight = height;
        cardLE.flexibleHeight = 0f;
    }
}
