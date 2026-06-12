/// <summary>Runtime metrics for production offer audits (Phase 6.3).</summary>
public static class MovieOfferAuditState
{
    public static int rebuildCount;
    public static int lastSlotUiCount;
    public static int lastPickCount;
    public static int lastEmptySlotCount;
    public static int lastDuplicateUiDetected;

    public static void Reset()
    {
        rebuildCount = 0;
        lastSlotUiCount = 0;
        lastPickCount = 0;
        lastEmptySlotCount = 0;
        lastDuplicateUiDetected = 0;
    }
}
