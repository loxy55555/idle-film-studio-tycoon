using UnityEngine;

/// <summary>Studio tab shell — departments, upgrades, contracts sub-tabs.</summary>
public class StudioHudShell : MonoBehaviour
{
    public RectTransform departmentsPanel;
    public RectTransform upgradesPanel;
    public RectTransform contractsPanel;
    public StudioHubUI subNavigation;

    public void Configure(StudioHudShellBuildResult build)
    {
        departmentsPanel = build.departmentsPanel;
        upgradesPanel    = build.upgradesPanel;
        contractsPanel   = build.contractsPanel;
        subNavigation    = build.subNavigation;
    }
}

public struct StudioHudShellBuildResult
{
    public RectTransform departmentsPanel;
    public RectTransform upgradesPanel;
    public RectTransform contractsPanel;
    public StudioHubUI subNavigation;
}
