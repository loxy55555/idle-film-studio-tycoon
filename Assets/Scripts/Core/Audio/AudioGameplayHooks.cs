using UnityEngine;

/// <summary>FASE 15.8C — wires approved gameplay events to AudioManager (minimal SFX only).</summary>
public class AudioGameplayHooks : MonoBehaviour
{
    StudioManager  _studio;
    ContractSystem _contracts;
    CitySystem     _city;

    int  _cityLevel;
    bool _cityBound;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindAnyObjectByType<AudioGameplayHooks>() != null) return;
        var go = new GameObject("[AudioGameplayHooks]");
        go.AddComponent<AudioGameplayHooks>();
    }

    void Start()
    {
        GameHub.OnGameReady += Bind;
        if (GameHub.Instance != null) Bind();
    }

    void OnDestroy() => GameHub.OnGameReady -= Bind;

    void Bind()
    {
        Unbind();
        var hub = GameHub.Instance;
        if (hub == null) return;

        _studio    = hub.studio;
        _contracts = hub.contracts;
        _city      = hub.city;

        if (_studio != null)    _studio.OnMovieCompleted    += OnMovieCompleted;
        if (_contracts != null) _contracts.OnContractClaimed += OnContractClaimed;

        if (_city != null)
        {
            _city.OnCityLevelChanged += OnCityLevelChanged;
            _cityLevel = _city.Level;
            _cityBound   = true;
        }

        AudioManager.Instance?.BeginSessionMusic();
    }

    void Unbind()
    {
        if (_studio != null)    _studio.OnMovieCompleted    -= OnMovieCompleted;
        if (_contracts != null) _contracts.OnContractClaimed -= OnContractClaimed;
        if (_city != null)      _city.OnCityLevelChanged     -= OnCityLevelChanged;
        _cityBound = false;
    }

    void OnMovieCompleted(MovieCompletePayload payload)
    {
        AudioManager.Instance?.PlaySfx("moviecomplete");

        if (payload.config == null || string.IsNullOrEmpty(payload.config.sagaId)) return;
        if (!payload.isFirstDiscovery) return;

        var hub = GameHub.Instance;
        if (hub?.studio == null) return;

        var catalog = MovieCatalogRuntime.AllMovies;
        if (catalog == null) return;

        var keys = hub.studio.CompletedMovieKeys;
        int total = SagaProgressionRules.GetSagaTotal(payload.config, catalog);
        if (total <= 0) return;

        int discovered = SagaProgressionRules.CountDiscoveredInSaga(payload.config.sagaId, keys, catalog);
        if (discovered >= total)
            AudioManager.Instance?.PlaySfx("collectioncomplete");
    }

    void OnContractClaimed(ContractConfig contract)
    {
        if (contract == null) return;
        AudioManager.Instance?.PlaySfx("contractsuccess");
    }

    void OnCityLevelChanged(int level)
    {
        if (!_cityBound)
        {
            _cityLevel = level;
            _cityBound = true;
            return;
        }

        if (level > _cityLevel)
            AudioManager.Instance?.PlaySfx("cityunlock");

        _cityLevel = level;
    }
}
