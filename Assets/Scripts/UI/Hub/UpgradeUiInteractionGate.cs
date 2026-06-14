using UnityEngine;

/// <summary>Guards upgrade purchase clicks against overlap/reorder ghost hits (Phase 8.4).</summary>
public static class UpgradeUiInteractionGate
{
    static string _pointerDownId = string.Empty;
    static int _blockSortUntilFrame;
    static int _blockPurchaseUntilFrame;

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

        // Absorb layout-shift ghost taps (1 frame cooldown per purchase)
        _blockSortUntilFrame = Mathf.Max(_blockSortUntilFrame, Time.frameCount + 2);
        _blockPurchaseUntilFrame = Time.frameCount + 1;
        _pointerDownId = string.Empty;
        Debug.Log($"[UpgradeUI] Clicked={upgradeId}");
        return true;
    }

    public static bool CanReorderNow() =>
        Time.frameCount >= _blockSortUntilFrame && string.IsNullOrEmpty(_pointerDownId);

    public static void ClearPointerState()
    {
        _pointerDownId = string.Empty;
        _blockPurchaseUntilFrame = 0;
    }
}
