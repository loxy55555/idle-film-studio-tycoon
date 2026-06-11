/// <summary>Normalized placement anchors inside StudioVisualStage (0–1 stage space).</summary>
public enum VisualAnchorPoint
{
    Center,
    CenterBottom,
    CenterTop,
    LeftBottom,
    LeftCenter,
    RightBottom,
    RightCenter,
    StageLeft,
    StageRight,
}

public static class VisualAnchorPointExtensions
{
    /// <summary>Maps anchor to normalized X/Y within the stage (origin bottom-left).</summary>
    public static UnityEngine.Vector2 ToNormalizedPoint(this VisualAnchorPoint anchor) => anchor switch
    {
        VisualAnchorPoint.Center        => new UnityEngine.Vector2(0.50f, 0.50f),
        VisualAnchorPoint.CenterBottom  => new UnityEngine.Vector2(0.50f, 0.18f),
        VisualAnchorPoint.CenterTop     => new UnityEngine.Vector2(0.50f, 0.82f),
        VisualAnchorPoint.LeftBottom    => new UnityEngine.Vector2(0.14f, 0.16f),
        VisualAnchorPoint.LeftCenter    => new UnityEngine.Vector2(0.14f, 0.50f),
        VisualAnchorPoint.RightBottom   => new UnityEngine.Vector2(0.86f, 0.16f),
        VisualAnchorPoint.RightCenter   => new UnityEngine.Vector2(0.86f, 0.50f),
        VisualAnchorPoint.StageLeft     => new UnityEngine.Vector2(0.08f, 0.22f),
        VisualAnchorPoint.StageRight    => new UnityEngine.Vector2(0.92f, 0.22f),
        _                               => new UnityEngine.Vector2(0.50f, 0.50f),
    };
}
