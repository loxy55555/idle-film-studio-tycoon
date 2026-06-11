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
        le.flexibleHeight   = HudLayoutConstants.StudioVisualShare;
        le.flexibleWidth    = 0f;
        le.minHeight        = HudLayoutConstants.StudioVisualMinHeight;
        le.preferredHeight  = 0f;

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
    }
}
