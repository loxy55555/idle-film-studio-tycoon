using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Premiere overlay section hooks — used by PremiereOverlayView (Phase 8.2 / 8.5).</summary>
public class ProductionPremiereHooks : MonoBehaviour
{
    public static ProductionPremiereHooks Instance { get; private set; }

    [Header("Future overlay roots — inactive until premiere phases")]
    public RectTransform premiereRoot;
    public RectTransform rewardsRoot;
    public RectTransform discoveryRoot;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        EnsureRoots();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void EnsureOnCanvas()
    {
        if (Instance != null) return;
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("ProductionPremiereHooks", typeof(RectTransform), typeof(ProductionPremiereHooks));
        go.transform.SetParent(canvas.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<ProductionPremiereHooks>().EnsureRoots();
    }

    void EnsureRoots()
    {
        premiereRoot  = premiereRoot  ?? CreateShell("PremiereShell",  Loc.Get(LocKeys.ProdPremiereHook));
        rewardsRoot   = rewardsRoot   ?? CreateShell("RewardsShell",   Loc.Get(LocKeys.ProdRewardsHook));
        discoveryRoot = discoveryRoot ?? CreateShell("DiscoveryShell", Loc.Get(LocKeys.ProdDiscoveryHook));
    }

    RectTransform CreateShell(string name, string placeholder)
    {
        var shell = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        shell.transform.SetParent(transform, false);
        var rt = shell.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        shell.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
        shell.GetComponent<CanvasGroup>().alpha = 0f;
        shell.GetComponent<CanvasGroup>().interactable = false;
        shell.GetComponent<CanvasGroup>().blocksRaycasts = false;
        shell.SetActive(false);

        var label = RuntimeTmpText.Create(shell.transform, placeholder, 14f, Color.white,
            FontStyles.Bold, TextAlignmentOptions.Center, "Placeholder");
        Stretch(label.rectTransform);
        label.enableAutoSizing = true;
        return rt;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
