using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Read-only department card — levels come from Personnel upgrades, not direct taps.</summary>
public class DepartmentMiniCardUI : MonoBehaviour
{
    [HideInInspector] public DepartmentType deptType;

    [Header("UI Refs")]
    public Image           categoryBadge;
    public TextMeshProUGUI deptNameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI effectText;

    DepartmentSystem _depts;
    UpgradeSystem    _upgrades;
    CitySystem       _city;
    CanvasGroup      _canvasGroup;
    int              _cachedLevel = -1;
    bool             _cachedLocked;

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        GameHub.OnGameReady += OnGameReady;
    }

    void OnDestroy()
    {
        GameHub.OnGameReady -= OnGameReady;
        Unsubscribe();
    }

    void Unsubscribe()
    {
        if (_upgrades != null)
            _upgrades.OnUpgradePurchased -= Refresh;
        if (_city != null)
            _city.OnCityLevelChanged -= OnCityChanged;
    }

    void OnGameReady()
    {
        Unsubscribe();
        _depts  = GameHub.Instance?.departments;
        _upgrades = GameHub.Instance?.upgrades;
        _city   = GameHub.Instance?.city;
        if (_upgrades != null)
            _upgrades.OnUpgradePurchased += Refresh;
        if (_city != null)
            _city.OnCityLevelChanged += OnCityChanged;
        Refresh();
    }

    void OnCityChanged(int _) => Refresh();

    void Start()
    {
        if (GameHub.Instance != null)
            OnGameReady();
    }

    void OnEnable() => Refresh();

    void Refresh()
    {
        if (_depts == null) return;

        int  idx   = (int)deptType;
        var  data  = DepartmentCardUI.DeptData[idx];
        bool locked = _city != null && !_city.IsDepartmentUnlocked(deptType);
        int  level = locked ? 0 : GetLevel();

        if (deptNameText != null) deptNameText.text = data.name;

        if (locked)
        {
            if (levelText != null)
                levelText.text = CityProgressionRules.GetDepartmentLockLabel(deptType);
            if (effectText != null)
                effectText.text = "Bloqueado";
        }
        else
        {
            if (levelText != null) levelText.text = "Nv." + level;
            if (effectText != null)
                effectText.text = $"{data.category}\n+{data.effectPerLevel:0.##}/nv  Total +{(data.effectPerLevel * level):0.##}";
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = locked ? 0.55f : 1f;
            _canvasGroup.interactable = !locked;
        }

        _cachedLevel = level;
        _cachedLocked = locked;
    }

    void Update()
    {
        if (_depts == null)
        {
            _depts = GameHub.Instance?.departments;
            if (_depts != null) Refresh();
            return;
        }
        if (GetLevel() != _cachedLevel || IsLocked() != _cachedLocked) Refresh();
    }

    bool IsLocked() => _city != null && !_city.IsDepartmentUnlocked(deptType);

    int GetLevel() => deptType switch
    {
        DepartmentType.Editor         => _depts.editor,
        DepartmentType.Director       => _depts.director,
        DepartmentType.Actors         => _depts.actors,
        DepartmentType.Sound          => _depts.sound,
        DepartmentType.Cinematography => _depts.cinematography,
        DepartmentType.Makeup         => _depts.makeup,
        DepartmentType.Costume        => _depts.costume,
        DepartmentType.Art            => _depts.art,
        DepartmentType.Lighting       => _depts.lighting,
        DepartmentType.Grip           => _depts.grip,
        DepartmentType.Producer       => _depts.producer,
        _                             => 0,
    };
}
