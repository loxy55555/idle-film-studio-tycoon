using UnityEngine;
using UnityEngine.UI;

/// <summary>Reserved central area for future studio visuals (background, crew, animations).</summary>
public class StudioVisualStage : MonoBehaviour
{
    public static StudioVisualStage Instance { get; private set; }

    public RectTransform StageRoot { get; private set; }

    /// <summary>Future city backgrounds — bottom-most layer.</summary>
    public RectTransform BackgroundLayer { get; private set; }

    /// <summary>Future studio set dressing and environment props.</summary>
    public RectTransform SetLayer { get; private set; }

    /// <summary>Future character silhouettes / crew on stage.</summary>
    public RectTransform CharacterLayer { get; private set; }

    /// <summary>Future department equipment overlays.</summary>
    public RectTransform EquipmentLayer { get; private set; }

    /// <summary>Future VFX / particles (DOTween-friendly container).</summary>
    public RectTransform EffectsLayer { get; private set; }

    /// <summary>Future HUD-adjacent stage overlays (labels, highlights).</summary>
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
        HideLegacyPlaceholderText();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void ApplyLayout()
    {
        var le = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        // Phase 8.5C: fixed height — stage is decorative, not dominant
        le.preferredHeight  = HudLayoutConstants.StudioVisualFixedHeight;
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

    void HideLegacyPlaceholderText()
    {
        var placeholder = transform.Find("ScenePlaceholder");
        if (placeholder != null)
            placeholder.gameObject.SetActive(false);

        var border = transform.Find("SceneBorder");
        if (border != null)
            border.gameObject.SetActive(false);

        EnsureBottomFade();
    }

    /// <summary>
    /// Phase 8.6B: gradient fade at the bottom edge of the poster peek strip so it looks
    /// intentional (teaser) rather than abruptly cropped.
    /// </summary>
    void EnsureBottomFade()
    {
        const string FadeName = "BottomFadeOverlay";
        if (transform.Find(FadeName) != null) return;

        var fade = new GameObject(FadeName, typeof(RectTransform), typeof(Image));
        fade.transform.SetParent(transform, false);

        var rt = fade.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0.55f); // covers bottom 55% of the stage strip
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Opaque-at-bottom dark overlay — simulates the poster fading into darkness
        var img = fade.GetComponent<Image>();
        img.color = new Color(0.04f, 0.04f, 0.08f, 0.92f);
        img.raycastTarget = false;
    }
}
