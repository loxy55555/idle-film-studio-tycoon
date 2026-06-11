using UnityEngine;

/// <summary>Menu tab shell — settings, save, help, credits sections.</summary>
public class MenuHudShell : MonoBehaviour
{
    public RectTransform settingsSection;
    public RectTransform saveSection;
    public RectTransform helpSection;
    public RectTransform creditsSection;

    public void Configure(MenuHudShellBuildResult build)
    {
        settingsSection = build.settingsSection;
        saveSection     = build.saveSection;
        helpSection     = build.helpSection;
        creditsSection  = build.creditsSection;
    }
}

public struct MenuHudShellBuildResult
{
    public RectTransform settingsSection;
    public RectTransform saveSection;
    public RectTransform helpSection;
    public RectTransform creditsSection;
}
