using System;
using UnityEngine;

/// <summary>Per-city visual scale and placement profile for StudioVisualStage (Phase 7B.4).</summary>
[Serializable]
public class CityVisualProfile
{
    public CityTier tier;
    public float characterScale = 1f;
    public VisualAnchorPoint characterAnchor = VisualAnchorPoint.CenterBottom;
    public float equipmentScale = 1f;
    public VisualAnchorPoint equipmentAnchor = VisualAnchorPoint.LeftBottom;
    public float groundLineY = 0.30f;
}
