using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Forces the main HUD shell to fill the screen using anchors.
/// Guards against LayoutGroup sizing bugs from programmatic UI builds.
/// </summary>
[DefaultExecutionOrder(-200)]
public class StudioUIShellLayout : MonoBehaviour
{
    const float TopBarHeight    = 120f;
    const float BottomNavHeight = 100f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        var hub = FindAnyObjectByType<StudioUIShellLayout>();
        if (hub != null) hub.Apply();
    }

    private void Awake() => Apply();

    public void Apply()
    {
        FixCanvasScale();

        var topBar = transform.Find("TopBar") as RectTransform;
        var main   = transform.Find("MainContent") as RectTransform;
        var bottom = transform.Find("BottomNav") as RectTransform;

        if (topBar != null) AnchorTop(topBar, TopBarHeight);
        if (bottom != null) AnchorBottom(bottom, BottomNavHeight);
        if (main != null)   AnchorFill(main, TopBarHeight, BottomNavHeight);

        var switcher = main != null ? main.Find("ContentSwitcher") as RectTransform : null;
        if (switcher != null) Stretch(switcher);

        Canvas.ForceUpdateCanvases();
        if (main != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(main);
    }

    static void FixCanvasScale()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;
        if (canvas.transform.localScale.sqrMagnitude < 0.001f)
        {
            canvas.transform.localScale = Vector3.one;
            Debug.LogWarning("[StudioUIShellLayout] Canvas scale was 0 — fixed.");
        }
    }

    static void AnchorTop(RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, height);
        rt.anchoredPosition = Vector2.zero;
    }

    static void AnchorBottom(RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(0f, height);
        rt.anchoredPosition = Vector2.zero;
    }

    static void AnchorFill(RectTransform rt, float topInset, float bottomInset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(0f, bottomInset);
        rt.offsetMax = new Vector2(0f, -topInset);
        rt.anchoredPosition = Vector2.zero;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }
}
