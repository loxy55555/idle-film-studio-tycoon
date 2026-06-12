using UnityEngine;

/// <summary>Dev-only collection preview flags — does not persist (Phase DEV TOOLS).</summary>
public static class CollectionDebugState
{
    public static bool RevealAll { get; private set; }

    public static void SetRevealAll(bool value)
    {
        if (RevealAll == value) return;
        RevealAll = value;
        Debug.Log($"[CollectionDebug] RevealAll={value}");
    }
}
