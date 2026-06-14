using UnityEngine;

/// <summary>
/// Inspector-assigned background sprites for the 8 city tiers.
/// Loaded at runtime via Resources.Load — place asset at:
///   Assets/Resources/CityBackgroundRegistry.asset
/// Leave a slot empty to fall back to the procedural placeholder theme.
/// </summary>
[CreateAssetMenu(menuName = "IdleFilm/City Background Registry", fileName = "CityBackgroundRegistry")]
public class CityBackgroundRegistry : ScriptableObject
{
    public const string ResourceName = "CityBackgroundRegistry";
    public const string AssetPath    = "Assets/Resources/CityBackgroundRegistry.asset";

    [Header("Assign one Sprite per city — leave empty to use procedural placeholder")]
    [Tooltip("City 1 — Garaje")]
    public Sprite city1Background;

    [Tooltip("City 2 — Estudio Independiente")]
    public Sprite city2Background;

    [Tooltip("City 3 — Estudio Local")]
    public Sprite city3Background;

    [Tooltip("City 4 — Estudio Regional")]
    public Sprite city4Background;

    [Tooltip("City 5 — Gran Estudio")]
    public Sprite city5Background;

    [Tooltip("City 6 — Hollywood Boulevard")]
    public Sprite city6Background;

    [Tooltip("City 7 — Major Studio")]
    public Sprite city7Background;

    [Tooltip("City 8 — Imperio Cinematográfico")]
    public Sprite city8Background;

    /// <summary>Returns the background sprite for the given city tier, or null if unassigned.</summary>
    public Sprite GetBackground(CityTier tier) => tier switch
    {
        CityTier.City1 => city1Background,
        CityTier.City2 => city2Background,
        CityTier.City3 => city3Background,
        CityTier.City4 => city4Background,
        CityTier.City5 => city5Background,
        CityTier.City6 => city6Background,
        CityTier.City7 => city7Background,
        CityTier.City8 => city8Background,
        _              => null,
    };

    /// <summary>Loads the registry from Resources. Returns null if asset not yet created.</summary>
    public static CityBackgroundRegistry Load() =>
        Resources.Load<CityBackgroundRegistry>(ResourceName);
}
