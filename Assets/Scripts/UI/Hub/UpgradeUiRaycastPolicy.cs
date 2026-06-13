using UnityEngine;
using UnityEngine.UI;

/// <summary>Limits upgrade card raycasts to the buy button only (Phase 8.4).</summary>
public static class UpgradeUiRaycastPolicy
{
    public static void ApplyCard(Transform cardRoot, Button buyButton)
    {
        ApplyCard(cardRoot, buyButton, allowScrollDrag: false);
    }

    /// <summary>Department cards: buy button stays clickable; root forwards scroll drags.</summary>
    public static void ApplyDepartmentCard(Transform cardRoot, Button buyButton)
    {
        ApplyCard(cardRoot, buyButton, allowScrollDrag: true);
    }

    static void ApplyCard(Transform cardRoot, Button buyButton, bool allowScrollDrag)
    {
        if (cardRoot == null) return;

        if (allowScrollDrag && cardRoot.GetComponent<ScrollDragForwarder>() == null)
            cardRoot.gameObject.AddComponent<ScrollDragForwarder>();

        foreach (var graphic in cardRoot.GetComponentsInChildren<Graphic>(true))
        {
            if (buyButton != null &&
                (graphic.transform == buyButton.transform || graphic.transform.IsChildOf(buyButton.transform)))
            {
                graphic.raycastTarget = graphic == buyButton.GetComponent<Image>();
                continue;
            }

            if (allowScrollDrag &&
                graphic.transform == cardRoot &&
                cardRoot.GetComponent<ScrollDragForwarder>() != null)
            {
                graphic.raycastTarget = true;
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
