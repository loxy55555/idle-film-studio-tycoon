using UnityEngine;

/// <summary>Awards tab shell — nominations, presentations, golden stars, city sections.</summary>
public class PremiosHudShell : MonoBehaviour
{
    public RectTransform nominationsPanel;
    public RectTransform presentationsPanel;
    public RectTransform goldenStarsPanel;
    public RectTransform cityPanel;
    public StudioHubUI subNavigation;

    public void Configure(PremiosHudShellBuildResult build)
    {
        nominationsPanel  = build.nominationsPanel;
        presentationsPanel = build.presentationsPanel;
        goldenStarsPanel  = build.goldenStarsPanel;
        cityPanel         = build.cityPanel;
        subNavigation     = build.subNavigation;
    }
}

public struct PremiosHudShellBuildResult
{
    public RectTransform nominationsPanel;
    public RectTransform presentationsPanel;
    public RectTransform goldenStarsPanel;
    public RectTransform cityPanel;
    public StudioHubUI subNavigation;
}
