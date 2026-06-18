using UnityEngine;

/// <summary>
/// FASE 15.0C — Global language refresh coordinator.
/// Spawned by DefinitiveHudBootstrap at startup. Listens to UserPrefs.OnLanguageChanged
/// and refreshes every active UI panel so language changes take effect immediately
/// without requiring a game restart.
/// </summary>
public class HudLocalizationBridge : MonoBehaviour
{
    static HudLocalizationBridge _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (!Application.isPlaying) return;
        if (_instance != null) return;

        var go = new GameObject("[HudLocalizationBridge]");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<HudLocalizationBridge>();
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    void OnEnable()
    {
        UserPrefs.OnLanguageChanged += OnLanguageChanged;
    }

    void OnDisable()
    {
        UserPrefs.OnLanguageChanged -= OnLanguageChanged;
    }

    void OnDestroy()
    {
        UserPrefs.OnLanguageChanged -= OnLanguageChanged;
        if (_instance == this) _instance = null;
    }

    void OnLanguageChanged()
    {
        // 1. Refresh bottom nav tabs and mejoras sub-tabs
        DefinitiveHudBootstrap.RefreshNavLabels();

        // 2. Settings overlay is self-refreshing (subscribes internally)

        // 3. Refresh all active panels that expose RefreshLocalization()
        RefreshAll<AwardsPanelUI>(p => p.RefreshLocalization());
        RefreshAll<MovieCollectionUI>(p => p.RefreshLocalization());
        RefreshAll<StorePanelUI>(p => p.RefreshLocalization());

        // 4. Refresh upgrade cards
        RefreshAll<UpgradeCardUI>(p => p.RefreshUI());

        // 5. Refresh department mini-cards
        RefreshAll<DepartmentMiniCardUI>(p => p.RefreshLocalization());

        // 6. Refresh production slots, offers and production shell button
        RefreshAll<MultiProductionPanelUI>(p => p.RefreshLocalization());
        RefreshAll<ProductionHudShell>(p => p.RefreshLocalization());
        RefreshAll<MovieTabUI>(p => p.RefreshLocalization());
        RefreshAll<MovieButtonUI>(p => p.RefreshUI());

        // 7. Refresh contracts panel
        RefreshAll<ContractsPanelUI>(p => p.Rebuild());

        // 8. Refresh mission objective (contract header)
        RefreshAll<MissionObjectiveUI>(p => p.RefreshLocalization());

        // 9. Refresh studio bars
        RefreshAll<BonificationsBarUI>(p => p.Refresh());
        RefreshAll<StudioLevelBarUI>(p => p.RefreshUI());

        // 10. Refresh top bar (DIAM label + Cal/Vel/Nv. abbreviations)
        RefreshAll<TopBarUI>(p => p.RefreshLocalization());

        // 11. Refresh city/installation panel headers + dynamic content
        RefreshAll<CityPanelUI>(p => p.RefreshLocalization());
    }

    static void RefreshAll<T>(System.Action<T> action) where T : MonoBehaviour
    {
        foreach (var comp in FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (comp == null || !comp.gameObject.activeInHierarchy) continue;
            try { action(comp); }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[HudLocalizationBridge] Error refreshing {typeof(T).Name}: {e.Message}");
            }
        }
    }
}
