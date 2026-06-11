using UnityEngine;

/// <summary>Lookup table of visual scale profiles for cities 1–8.</summary>
public static class CityVisualDatabase
{
    public const int MaxLevel = 8;

    static CityVisualProfile[] _profiles;

    public static CityVisualProfile[] AllProfiles
    {
        get
        {
            if (_profiles == null) _profiles = BuildProfiles();
            return _profiles;
        }
    }

    public static CityVisualProfile Get(CityTier tier) =>
        Get(tier.ToLevel());

    public static CityVisualProfile Get(int cityLevel)
    {
        cityLevel = Mathf.Clamp(cityLevel, 1, MaxLevel);
        return AllProfiles[cityLevel - 1];
    }

    static CityVisualProfile[] BuildProfiles() => new[]
    {
        Def(CityTier.City1, 0.88f, VisualAnchorPoint.CenterBottom, 0.82f, VisualAnchorPoint.LeftBottom,    0.30f),
        Def(CityTier.City2, 0.90f, VisualAnchorPoint.CenterBottom, 0.84f, VisualAnchorPoint.LeftBottom,    0.30f),
        Def(CityTier.City3, 0.92f, VisualAnchorPoint.CenterBottom, 0.86f, VisualAnchorPoint.LeftBottom,    0.30f),
        Def(CityTier.City4, 0.94f, VisualAnchorPoint.CenterBottom, 0.88f, VisualAnchorPoint.StageLeft,     0.31f),
        Def(CityTier.City5, 0.96f, VisualAnchorPoint.CenterBottom, 0.90f, VisualAnchorPoint.StageLeft,     0.31f),
        Def(CityTier.City6, 1.00f, VisualAnchorPoint.CenterBottom, 0.93f, VisualAnchorPoint.LeftCenter,    0.32f),
        Def(CityTier.City7, 1.05f, VisualAnchorPoint.CenterBottom, 0.98f, VisualAnchorPoint.LeftCenter,    0.32f),
        Def(CityTier.City8, 1.10f, VisualAnchorPoint.CenterBottom, 1.05f, VisualAnchorPoint.StageLeft,     0.33f),
    };

    static CityVisualProfile Def(
        CityTier tier,
        float characterScale,
        VisualAnchorPoint characterAnchor,
        float equipmentScale,
        VisualAnchorPoint equipmentAnchor,
        float groundLineY) =>
        new CityVisualProfile
        {
            tier             = tier,
            characterScale   = characterScale,
            characterAnchor  = characterAnchor,
            equipmentScale   = equipmentScale,
            equipmentAnchor  = equipmentAnchor,
            groundLineY      = groundLineY,
        };
}
