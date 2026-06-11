using UnityEngine;

/// <summary>Marker — scene uses the Phase 7A definitive HUD architecture.</summary>
public class DefinitiveHudShell : MonoBehaviour
{
    public const int MigrationVersion = 3;

    public int appliedMigrationVersion;
    public StudioHubUI mainNavigation;
    public RectTransform productionPanel;
    public RectTransform studioPanel;
    public RectTransform awardsPanel;
    public RectTransform collectionPanel;
    public RectTransform menuPanel;
}
