using UnityEngine;

/// <summary>Guards upgrade purchase clicks against overlap/reorder ghost hits (Phase 8.4).</summary>
public static class UpgradeUiInteractionGate
{
    static string _pointerDownId = string.Empty;
    static int    _blockSortUntilFrame;
    static int    _blockPurchaseUntilFrame;

    // After a successful purchase the sorter must wait at least this long before
    // reordering cards so the user can see which card actually changed level.
    const float PostPurchaseGraceSec = 2f;
    static float _lastPurchaseRealtime = -999f;

    public static void RegisterPointerDown(string upgradeId)
    {
        _pointerDownId = upgradeId ?? string.Empty;
        Debug.Log($"[UpgradeUI] PointerDown={_pointerDownId}");
    }

    public static void RegisterPointerUp(string upgradeId)
    {
        Debug.Log($"[UpgradeUI] PointerUp={upgradeId ?? string.Empty}");
        _blockSortUntilFrame = Mathf.Max(_blockSortUntilFrame, Time.frameCount + 3);
    }

    public static bool TryConsumeClick(string upgradeId)
    {
        upgradeId ??= string.Empty;

        if (Time.frameCount <= _blockPurchaseUntilFrame)
        {
            Debug.LogWarning($"[UpgradeUI] TryConsumeClick=false motivo=cooldown frame={Time.frameCount} blockUntil={_blockPurchaseUntilFrame} id={upgradeId}");
            return false;
        }

        // Reject if pointer was pressed on a DIFFERENT card (layout-shift ghost purchase).
        if (!string.IsNullOrEmpty(_pointerDownId) && _pointerDownId != upgradeId)
        {
            Debug.LogWarning($"[UpgradeUI] TryConsumeClick=false motivo=id_mismatch down={_pointerDownId} up={upgradeId}");
            _pointerDownId = string.Empty;
            return false;
        }

        // Absorb layout-shift ghost taps (1 frame cooldown per purchase)
        _blockSortUntilFrame = Mathf.Max(_blockSortUntilFrame, Time.frameCount + 2);
        _blockPurchaseUntilFrame = Time.frameCount + 1;
        _pointerDownId = string.Empty;
        Debug.Log($"[UpgradeUI] Clicked={upgradeId}");
        return true;
    }

    /// <summary>
    /// Called after a successful upgrade purchase so the sorter waits before reordering.
    /// This is the root-cause fix for cards visually appearing in the wrong position after
    /// a purchase: the sorter now holds off for PostPurchaseGraceSec so the user can see
    /// which card changed before cards shuffle to their new sort positions.
    /// </summary>
    public static void RegisterPurchaseComplete(string upgradeId)
    {
        _lastPurchaseRealtime = Time.realtimeSinceStartup;
        Debug.Log($"[UpgradeUI] PurchaseComplete={upgradeId} sortBlockedFor={PostPurchaseGraceSec}s");
    }

    /// <summary>Remaining seconds of post-purchase grace (0 if none active).</summary>
    public static float GetPostPurchaseGraceRemaining() =>
        Mathf.Max(0f, _lastPurchaseRealtime + PostPurchaseGraceSec - Time.realtimeSinceStartup);

    /// <summary>
    /// True only when it is safe to reorder cards:
    /// no pointer is held, no layout-shift cooldown, and the post-purchase
    /// grace window has expired so the user has had time to read the result.
    /// </summary>
    public static bool CanReorderNow() =>
        Time.frameCount >= _blockSortUntilFrame &&
        string.IsNullOrEmpty(_pointerDownId) &&
        Time.realtimeSinceStartup >= _lastPurchaseRealtime + PostPurchaseGraceSec;

    public static void ClearPointerState()
    {
        _pointerDownId = string.Empty;
        _blockPurchaseUntilFrame = 0;
    }
}
