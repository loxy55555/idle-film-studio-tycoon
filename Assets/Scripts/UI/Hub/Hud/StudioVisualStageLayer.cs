using UnityEngine;

/// <summary>Marker for a single full-bleed layer inside StudioVisualStage.</summary>
[DisallowMultipleComponent]
public class StudioVisualStageLayer : MonoBehaviour
{
    [SerializeField] StudioVisualLayerKind layerKind;

    public StudioVisualLayerKind LayerKind => layerKind;
    public RectTransform RectTransform => transform as RectTransform;

    internal void Configure(StudioVisualLayerKind kind, int siblingIndex)
    {
        layerKind = kind;
        gameObject.name = StudioVisualStageLayers.GetLayerName(kind);
        transform.SetSiblingIndex(siblingIndex);
        StudioVisualStageLayers.ApplyStretch(RectTransform);
    }
}
