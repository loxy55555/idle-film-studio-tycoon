using UnityEngine;
using UnityEngine.UI;

/// <summary>Attaches runtime sorters to hub panels without changing builder layout.</summary>
public static class ContentSortBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (!Application.isPlaying) return;

        foreach (var scroll in Object.FindObjectsByType<ScrollRect>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (scroll.content == null) continue;

            Transform t = scroll.transform;
            while (t != null)
            {
                if (t.name == "DeptScroll")
                {
                    EnsureSorter<DepartmentContentSorter>(scroll.content.gameObject);
                    break;
                }

                if (t.name is "EquipoPanel" or "PersonalPanel" or "InstPanel" or "MktPanel")
                {
                    EnsureSorter<UpgradeContentSorter>(scroll.content.gameObject);
                    break;
                }

                t = t.parent;
            }
        }
    }

    static void EnsureSorter<T>(GameObject go) where T : MonoBehaviour
    {
        if (go.GetComponent<T>() != null) return;
        go.AddComponent<T>();
    }
}

/// <summary>Unlocked departments left, locked departments right.</summary>
public class DepartmentContentSorter : MonoBehaviour
{
    CitySystem _city;
    float _nextSort;

    void OnEnable()
    {
        GameHub.OnGameReady += Bind;
        if (GameHub.Instance != null) Bind();
    }

    void OnDisable() => Unbind();

    void Bind()
    {
        Unbind();
        _city = GameHub.Instance?.city;
        if (_city != null) _city.OnCityLevelChanged += OnCityChanged;
        if (_city != null && GameHub.Instance?.upgrades != null)
            GameHub.Instance.upgrades.OnUpgradePurchased += OnUpgradeChanged;
        SortNow();
    }

    void Unbind()
    {
        if (_city != null)
        {
            _city.OnCityLevelChanged -= OnCityChanged;
            if (GameHub.Instance?.upgrades != null)
                GameHub.Instance.upgrades.OnUpgradePurchased -= OnUpgradeChanged;
        }
    }

    void OnCityChanged(int _) => SortNow();
    void OnUpgradeChanged() => SortNow();

    void Update()
    {
        if (Time.unscaledTime < _nextSort) return;
        _nextSort = Time.unscaledTime + 0.35f;
        SortNow();
    }

    public void SortNow()
    {
        var cards = GetComponentsInChildren<DepartmentMiniCardUI>(true);
        if (cards == null || cards.Length < 2) return;

        _city ??= GameHub.Instance?.city;
        System.Array.Sort(cards, (a, b) => ContentSortOrder.CompareDepartments(a, b, _city));

        for (int i = 0; i < cards.Length; i++)
            cards[i].transform.SetSiblingIndex(i);
    }
}

/// <summary>Purchasable → no money → city locked; cost ascending within group.</summary>
public class UpgradeContentSorter : MonoBehaviour
{
    UpgradeSystem _upgrades;
    StudioManager _studio;
    StudioLevelSystem _level;
    CitySystem _city;
    float _nextSort;

    void OnEnable()
    {
        GameHub.OnGameReady += Bind;
        if (GameHub.Instance != null) Bind();
    }

    void OnDisable() => Unbind();

    void Bind()
    {
        Unbind();
        var hub = GameHub.Instance;
        if (hub == null) return;

        _upgrades = hub.upgrades;
        _studio   = hub.studio;
        _level    = hub.studioLevel;
        _city     = hub.city;

        if (_upgrades != null) _upgrades.OnUpgradePurchased += OnChanged;
        if (_studio != null)
        {
            _studio.OnMoneyChanged += OnMoneyChanged;
            _studio.OnMoneyDisplayChanged += OnMoneyDisplayChanged;
        }
        if (_city != null) _city.OnCityLevelChanged += OnCityChanged;

        SortNow();
    }

    void Unbind()
    {
        if (_upgrades != null) _upgrades.OnUpgradePurchased -= OnChanged;
        if (_studio != null)
        {
            _studio.OnMoneyChanged -= OnMoneyChanged;
            _studio.OnMoneyDisplayChanged -= OnMoneyDisplayChanged;
        }
        if (_city != null) _city.OnCityLevelChanged -= OnCityChanged;
    }

    void OnChanged() => SortNow();
    void OnMoneyChanged(long _) => SortNow();
    void OnMoneyDisplayChanged(double _) => SortNow();
    void OnCityChanged(int _) => SortNow();

    void Update()
    {
        if (Time.unscaledTime < _nextSort) return;
        _nextSort = Time.unscaledTime + 0.35f;
        SortNow();
    }

    public void SortNow()
    {
        var cards = GetComponentsInChildren<UpgradeCardUI>(false);
        if (cards == null || cards.Length < 2) return;

        var hub = GameHub.Instance;
        if (hub == null) return;

        _upgrades ??= hub.upgrades;
        _studio   ??= hub.studio;
        _level    ??= hub.studioLevel;
        _city     ??= hub.city;

        System.Array.Sort(cards, (a, b) =>
            ContentSortOrder.CompareUpgrades(a.upgradeConfig, b.upgradeConfig, _upgrades, _studio, _level, _city));

        for (int i = 0; i < cards.Length; i++)
            cards[i].transform.SetSiblingIndex(i);
    }
}
