using UnityEngine;

/// <summary>
/// Public facade for StudioVisualStage scale/placement profiles (Phase 7B.4).
/// Read-only CitySystem integration — no HUD or gameplay changes.
/// </summary>
[DisallowMultipleComponent]
public class StudioVisualManager : MonoBehaviour
{
    public static StudioVisualManager Instance { get; private set; }

    [SerializeField] StudioVisualStage stage;

    CitySystem _city;
    CityVisualProfile _activeProfile;
    CityTier _activeTier = CityTier.City1;

    public CityVisualProfile ActiveProfile => _activeProfile;
    public CityTier ActiveTier => _activeTier;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        if (stage == null)
            stage = GetComponent<StudioVisualStage>();

        RefreshProfile(CityTier.City1);
    }

    void Start()
    {
        GameHub.OnGameReady += Bind;
        if (GameHub.Instance != null)
            Bind();
    }

    void OnDestroy()
    {
        GameHub.OnGameReady -= Bind;
        if (_city != null)
            _city.OnCityLevelChanged -= OnCityLevelChanged;

        if (Instance == this) Instance = null;
    }

    void Bind()
    {
        if (_city != null)
            _city.OnCityLevelChanged -= OnCityLevelChanged;

        _city = GameHub.Instance?.city;
        if (_city != null)
        {
            _city.OnCityLevelChanged += OnCityLevelChanged;
            RefreshProfile(CityTierExtensions.FromLevel(_city.Level));
            return;
        }

        RefreshProfile(CityTier.City1);
    }

    void OnCityLevelChanged(int level) =>
        RefreshProfile(CityTierExtensions.FromLevel(level));

    public void RefreshProfile(CityTier tier)
    {
        _activeTier = tier;
        _activeProfile = CityVisualDatabase.Get(tier);
    }

    public void RefreshProfileForCurrentCity()
    {
        if (_city != null)
            RefreshProfile(CityTierExtensions.FromLevel(_city.Level));
        else
            RefreshProfile(CityTier.City1);
    }

    /// <summary>Normalized stage position (0–1) for character placement.</summary>
    public Vector2 GetCharacterAnchor() =>
        (_activeProfile ?? CityVisualDatabase.Get(CityTier.City1)).characterAnchor.ToNormalizedPoint();

    /// <summary>Normalized stage position (0–1) for equipment placement.</summary>
    public Vector2 GetEquipmentAnchor() =>
        (_activeProfile ?? CityVisualDatabase.Get(CityTier.City1)).equipmentAnchor.ToNormalizedPoint();

    public float GetCharacterScale() =>
        (_activeProfile ?? CityVisualDatabase.Get(CityTier.City1)).characterScale;

    public float GetEquipmentScale() =>
        (_activeProfile ?? CityVisualDatabase.Get(CityTier.City1)).equipmentScale;

    /// <summary>Normalized Y of the ground line (floor contact) for this city profile.</summary>
    public float GetGroundLineY() =>
        (_activeProfile ?? CityVisualDatabase.Get(CityTier.City1)).groundLineY;

    /// <summary>Converts a normalized anchor to local stage coordinates.</summary>
    public Vector2 ResolveStageLocalPoint(Vector2 normalizedAnchor)
    {
        if (stage == null)
            stage = GetComponent<StudioVisualStage>();

        var root = stage != null ? stage.StageRoot : transform as RectTransform;
        if (root == null) return normalizedAnchor;

        var rect = root.rect;
        return new Vector2(
            rect.xMin + rect.width * normalizedAnchor.x,
            rect.yMin + rect.height * normalizedAnchor.y);
    }
}
