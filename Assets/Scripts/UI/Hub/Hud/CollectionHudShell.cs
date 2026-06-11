using UnityEngine;

/// <summary>Collection tab shell — album, sagas, legendaries sections.</summary>
public class CollectionHudShell : MonoBehaviour
{
    public RectTransform albumPanel;
    public RectTransform sagasPanel;
    public RectTransform legendariesPanel;
    public MovieCollectionUI collectionUI;
    public StudioHubUI subNavigation;

    public void Configure(CollectionHudShellBuildResult build)
    {
        albumPanel       = build.albumPanel;
        sagasPanel       = build.sagasPanel;
        legendariesPanel = build.legendariesPanel;
        collectionUI     = build.collectionUI;
        subNavigation    = build.subNavigation;
    }
}

public struct CollectionHudShellBuildResult
{
    public RectTransform albumPanel;
    public RectTransform sagasPanel;
    public RectTransform legendariesPanel;
    public MovieCollectionUI collectionUI;
    public StudioHubUI subNavigation;
}
