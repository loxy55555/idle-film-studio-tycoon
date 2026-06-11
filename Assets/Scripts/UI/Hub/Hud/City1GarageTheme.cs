using TMPro;
using UnityEngine;

/// <summary>City 1 — Garaje (full placeholder from Phase 7B.2).</summary>
public sealed class City1GarageTheme : CityVisualTheme
{
    public const string ThemeIdValue = "City1_Garage";
    public const string BackgroundMarker = "Garage_WallBack";

    public override CityTier Tier => CityTier.City1;
    public override string ThemeId => ThemeIdValue;
    public override string DisplayLabel => "GARAGE STUDIO";

    static readonly Color WallDark       = new Color(0.14f, 0.16f, 0.19f);
    static readonly Color WallAccent     = new Color(0.10f, 0.11f, 0.14f);
    static readonly Color FloorCement    = new Color(0.36f, 0.38f, 0.42f);
    static readonly Color FloorLine      = new Color(0.26f, 0.28f, 0.31f);
    static readonly Color DoorPanel      = new Color(0.20f, 0.22f, 0.26f);
    static readonly Color WoodTable      = new Color(0.36f, 0.25f, 0.16f);
    static readonly Color BoxCardboard   = new Color(0.55f, 0.45f, 0.32f);
    static readonly Color BoxCardboard2  = new Color(0.48f, 0.39f, 0.28f);
    static readonly Color PosterFrame    = new Color(0.55f, 0.12f, 0.10f);
    static readonly Color PosterScreen   = new Color(0.08f, 0.10f, 0.18f);
    static readonly Color ShelfMetal     = new Color(0.34f, 0.36f, 0.40f);
    static readonly Color Silhouette     = new Color(0.07f, 0.08f, 0.10f);
    static readonly Color CameraBody     = new Color(0.12f, 0.12f, 0.14f);
    static readonly Color CameraLens     = new Color(0.18f, 0.20f, 0.26f);
    static readonly Color TripodMetal    = new Color(0.22f, 0.23f, 0.26f);
    static readonly Color LightHead      = new Color(0.95f, 0.88f, 0.55f);
    static readonly Color LightBeam      = new Color(0.95f, 0.88f, 0.40f, 0.18f);
    static readonly Color OverlayText    = new Color(0.72f, 0.74f, 0.80f, 0.55f);

    protected override void BuildBackground(CityVisualThemeContext ctx)
    {
        var root = ctx.CreateLayerRoot(StudioVisualLayerKind.Background);

        StudioVisualShapeUtil.CreateBlock(root, BackgroundMarker, WallDark,
            new Vector2(0f, 0.30f), new Vector2(1f, 1f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_WallAccentLeft", WallAccent,
            new Vector2(0f, 0.30f), new Vector2(0.035f, 1f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_WallAccentRight", WallAccent,
            new Vector2(0.965f, 0.30f), new Vector2(1f, 1f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_Floor", FloorCement,
            new Vector2(0f, 0f), new Vector2(1f, 0.32f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_FloorLine", FloorLine,
            new Vector2(0f, 0.29f), new Vector2(1f, 0.315f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_DoorPanel", DoorPanel,
            new Vector2(0.22f, 0.72f), new Vector2(0.78f, 0.84f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_DoorPanelLine", FloorLine,
            new Vector2(0.24f, 0.78f), new Vector2(0.76f, 0.785f));
    }

    protected override void BuildSet(CityVisualThemeContext ctx)
    {
        var root = ctx.CreateLayerRoot(StudioVisualLayerKind.Set);

        StudioVisualShapeUtil.CreateBlock(root, "Garage_TableTop", WoodTable,
            new Vector2(0.36f, 0.20f), new Vector2(0.64f, 0.26f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_TableLegL", WoodTable,
            new Vector2(0.39f, 0.08f), new Vector2(0.43f, 0.20f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_TableLegR", WoodTable,
            new Vector2(0.57f, 0.08f), new Vector2(0.61f, 0.20f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_BoxA", BoxCardboard,
            new Vector2(0.05f, 0.08f), new Vector2(0.15f, 0.22f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_BoxB", BoxCardboard2,
            new Vector2(0.13f, 0.08f), new Vector2(0.21f, 0.17f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_PosterFrame", PosterFrame,
            new Vector2(0.60f, 0.46f), new Vector2(0.78f, 0.78f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_PosterScreen", PosterScreen,
            new Vector2(0.62f, 0.48f), new Vector2(0.76f, 0.76f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_ShelfBack", ShelfMetal,
            new Vector2(0.82f, 0.34f), new Vector2(0.93f, 0.74f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_ShelfPlankTop", ShelfMetal,
            new Vector2(0.81f, 0.68f), new Vector2(0.94f, 0.71f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_ShelfPlankMid", ShelfMetal,
            new Vector2(0.81f, 0.54f), new Vector2(0.94f, 0.57f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_ShelfPlankBot", ShelfMetal,
            new Vector2(0.81f, 0.40f), new Vector2(0.94f, 0.43f));

        var posterLabel = RuntimeTmpText.Create(root, "FILM", 14f, new Color(0.85f, 0.85f, 0.90f, 0.65f),
            FontStyles.Bold, TextAlignmentOptions.Center, "Garage_PosterLabel");
        StretchAnchored(posterLabel.rectTransform, new Vector2(0.62f, 0.58f), new Vector2(0.76f, 0.68f));
    }

    protected override void BuildCharacter(CityVisualThemeContext ctx)
    {
        var root = ctx.CreateLayerRoot(StudioVisualLayerKind.Character);

        StudioVisualShapeUtil.CreateBlock(root, "Garage_CharacterBody", Silhouette,
            new Vector2(0.43f, 0.10f), new Vector2(0.57f, 0.40f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_CharacterHead", Silhouette,
            new Vector2(0.445f, 0.38f), new Vector2(0.555f, 0.50f));
    }

    protected override void BuildEquipment(CityVisualThemeContext ctx)
    {
        var root = ctx.CreateLayerRoot(StudioVisualLayerKind.Equipment);

        StudioVisualShapeUtil.CreateBlock(root, "Garage_CameraBody", CameraBody,
            new Vector2(0.09f, 0.20f), new Vector2(0.19f, 0.32f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_CameraLens", CameraLens,
            new Vector2(0.17f, 0.24f), new Vector2(0.25f, 0.30f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_CameraGrip", TripodMetal,
            new Vector2(0.11f, 0.18f), new Vector2(0.15f, 0.22f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_TripodHead", TripodMetal,
            new Vector2(0.26f, 0.28f), new Vector2(0.30f, 0.32f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_TripodLegL", TripodMetal,
            new Vector2(0.255f, 0.08f), new Vector2(0.265f, 0.29f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_TripodLegC", TripodMetal,
            new Vector2(0.285f, 0.08f), new Vector2(0.295f, 0.29f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_TripodLegR", TripodMetal,
            new Vector2(0.315f, 0.08f), new Vector2(0.325f, 0.29f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_LightStand", TripodMetal,
            new Vector2(0.785f, 0.08f), new Vector2(0.805f, 0.36f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_LightHead", LightHead,
            new Vector2(0.74f, 0.38f), new Vector2(0.86f, 0.48f));
        StudioVisualShapeUtil.CreateBlock(root, "Garage_LightBeam", LightBeam,
            new Vector2(0.70f, 0.26f), new Vector2(0.90f, 0.38f));
    }

    protected override void BuildOverlay(CityVisualThemeContext ctx)
    {
        var root = ctx.CreateLayerRoot(StudioVisualLayerKind.Overlay);

        var title = RuntimeTmpText.Create(root, DisplayLabel, 13f, OverlayText,
            FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "Garage_OverlayTitle");
        StretchAnchored(title.rectTransform, new Vector2(0.03f, 0.86f), new Vector2(0.55f, 0.97f));

        var city = RuntimeTmpText.Create(root, "CIUDAD 1", 11f, OverlayText,
            FontStyles.Normal, TextAlignmentOptions.MidlineLeft, "Garage_OverlayCity");
        StretchAnchored(city.rectTransform, new Vector2(0.03f, 0.78f), new Vector2(0.35f, 0.86f));
    }

    static void StretchAnchored(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (rt == null) return;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }
}
