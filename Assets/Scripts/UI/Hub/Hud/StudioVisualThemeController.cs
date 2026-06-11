using UnityEngine;

/// <summary>
/// Applies city visual themes to StudioVisualStage based on CitySystem level (Phase 7B.3).
/// Read-only integration — does not modify CitySystem logic.
/// </summary>
[DisallowMultipleComponent]
public class StudioVisualThemeController : MonoBehaviour
{
    [SerializeField] StudioVisualStage stage;

    CitySystem _city;
    CityTier _activeTier = (CityTier)(-1);
    string _activeThemeId;

    public CityTier ActiveTier => _activeTier;
    public string ActiveThemeId => _activeThemeId;

    void Awake()
    {
        if (stage == null)
            stage = GetComponent<StudioVisualStage>();

        if (stage != null)
        {
            StudioVisualStageLayers.EnsureHierarchy(stage);
            DetectBakedTheme();
        }
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
    }

    void DetectBakedTheme()
    {
        foreach (CityTier tier in System.Enum.GetValues(typeof(CityTier)))
        {
            var theme = CityVisualThemeRegistry.Get(tier);
            if (!IsThemeBuilt(stage, theme.ThemeId)) continue;
            _activeTier = tier;
            _activeThemeId = theme.ThemeId;
            return;
        }
    }

    void Bind()
    {
        if (_city != null)
            _city.OnCityLevelChanged -= OnCityLevelChanged;

        _city = GameHub.Instance?.city;
        if (_city != null)
        {
            _city.OnCityLevelChanged += OnCityLevelChanged;
            ApplyTheme(CityTierExtensions.FromLevel(_city.Level));
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying) return;
#endif
        ApplyTheme(CityTier.City1);
    }

    void OnCityLevelChanged(int level) =>
        ApplyTheme(CityTierExtensions.FromLevel(level));

    public void ApplyTheme(CityTier tier, bool force = false)
    {
        if (stage == null)
            stage = GetComponent<StudioVisualStage>();
        if (stage == null) return;

        var theme = CityVisualThemeRegistry.Get(tier);
        if (!force && _activeTier == tier && _activeThemeId == theme.ThemeId && IsThemeBuilt(stage, theme.ThemeId))
            return;

        CityVisualThemeCleanup.ClearAllLayers(stage);
        theme.Apply(stage);

        _activeTier = tier;
        _activeThemeId = theme.ThemeId;

        if (StudioVisualManager.Instance != null)
            StudioVisualManager.Instance.RefreshProfile(tier);
    }

    public void ApplyThemeForCurrentCity(bool force = false)
    {
        if (_city != null)
            ApplyTheme(CityTierExtensions.FromLevel(_city.Level), force);
        else
            ApplyTheme(CityTier.City1, force);
    }

    public static bool IsThemeBuilt(StudioVisualStage stage, string themeId)
    {
        if (stage == null || string.IsNullOrEmpty(themeId)) return false;
        var rootName = $"ThemeRoot_{themeId}";
        return stage.BackgroundLayer != null && stage.BackgroundLayer.Find(rootName) != null;
    }

    public static void ApplyTheme(StudioVisualStage stage, CityTier tier, bool force = true)
    {
        if (stage == null) return;

        var controller = stage.GetComponent<StudioVisualThemeController>();
        if (controller == null)
            controller = stage.gameObject.AddComponent<StudioVisualThemeController>();

        controller.stage = stage;
        controller.ApplyTheme(tier, force);
    }
}
