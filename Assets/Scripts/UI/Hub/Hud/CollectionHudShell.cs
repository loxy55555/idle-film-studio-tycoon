using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Collection tab shell — album (L1 genre → L2 movies), sagas, legendaries.
/// Phase 8.5C: MovieCollectionUI handles L1/L2/L3 internally — shell is a thin wrapper only.
/// </summary>
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

    void OnEnable()
    {
        // Ensure the collection UI is shown (not hidden by old EnsureGenreNav logic)
        if (collectionUI != null && !collectionUI.gameObject.activeSelf)
            collectionUI.gameObject.SetActive(true);
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
