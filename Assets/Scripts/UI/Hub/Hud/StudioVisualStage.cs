using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Phase 10.0 — clean hero stage (~38 % of Estudio panel).
/// Background render only; no overlays. Summary lives in BonificationsBar below.
/// </summary>
public class StudioVisualStage : MonoBehaviour
{
    public static StudioVisualStage Instance { get; private set; }

    public RectTransform StageRoot { get; private set; }

    public RectTransform BackgroundLayer { get; private set; }
    public RectTransform SetLayer { get; private set; }
    public RectTransform CharacterLayer { get; private set; }
    public RectTransform EquipmentLayer { get; private set; }
    public RectTransform EffectsLayer { get; private set; }
    public RectTransform OverlayLayer { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        StageRoot = transform as RectTransform;
        ApplyLayout();
        StudioVisualStageLayers.EnsureHierarchy(this);
        if (GetComponent<StudioVisualThemeController>() == null)
            gameObject.AddComponent<StudioVisualThemeController>();
        if (GetComponent<StudioVisualManager>() == null)
            gameObject.AddComponent<StudioVisualManager>();
        HideLegacyContent();
        RemoveStageOverlays();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start() => StartCoroutine(ApplyLayoutWhenReady());

    IEnumerator ApplyLayoutWhenReady()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        ApplyLayout();
    }

    public void ApplyLayout()
    {
        var le = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        var panel = transform.parent as RectTransform;
        float heroH = panel != null && panel.rect.height > 200f
            ? Mathf.Round(panel.rect.height * HudLayoutConstants.StudioVisualShare)
            : HudLayoutConstants.StudioVisualFixedHeight;
        le.preferredHeight  = heroH;
        le.minHeight        = HudLayoutConstants.StudioVisualMinHeight;
        le.flexibleHeight   = 0f;
        le.flexibleWidth    = 0f;

        var image = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        HudSkinProvider.ApplyPanel(image, HudPanelVariant.Stage);
        image.raycastTarget = false;
    }

    internal void BindLayers(RectTransform[] layers)
    {
        if (layers == null || layers.Length < StudioVisualStageLayers.RenderOrder.Length)
            return;

        BackgroundLayer = layers[0];
        SetLayer        = layers[1];
        CharacterLayer  = layers[2];
        EquipmentLayer  = layers[3];
        EffectsLayer    = layers[4];
        OverlayLayer    = layers[5];
    }

    public RectTransform GetLayer(StudioVisualLayerKind kind)
    {
        return kind switch
        {
            StudioVisualLayerKind.Background => BackgroundLayer,
            StudioVisualLayerKind.Set        => SetLayer,
            StudioVisualLayerKind.Character  => CharacterLayer,
            StudioVisualLayerKind.Equipment  => EquipmentLayer,
            StudioVisualLayerKind.Effects    => EffectsLayer,
            StudioVisualLayerKind.Overlay    => OverlayLayer,
            _                                => null,
        };
    }

    void HideLegacyContent()
    {
        foreach (var name in new[] { "ScenePlaceholder", "SceneBorder" })
        {
            var t = transform.Find(name);
            if (t != null) t.gameObject.SetActive(false);
        }

        foreach (var name in new[] { "StageHeaderOverlay", "TopVignette" })
        {
            var t = transform.Find(name);
            if (t != null) Object.Destroy(t.gameObject);
        }
    }

    void RemoveStageOverlays()
    {
        foreach (var name in new[] { "HeroStatsChrome", "HeroFade", "StageTitleChrome", "BottomFadeOverlay" })
        {
            var t = transform.Find(name);
            if (t != null) Object.Destroy(t.gameObject);
        }
    }
}
