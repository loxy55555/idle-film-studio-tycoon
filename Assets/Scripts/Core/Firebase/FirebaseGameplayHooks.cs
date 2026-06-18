using UnityEngine;

/// <summary>
/// FASE 16.0B — wires core gameplay signals to Firebase Analytics (observability only).
/// Mirrors AudioGameplayHooks pattern; no gameplay/UI changes.
/// </summary>
public class FirebaseGameplayHooks : MonoBehaviour
{
    StudioManager  _studio;
    PrestigeSystem _prestige;
    CitySystem     _city;

    int  _cityLevel;
    bool _cityBound;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindAnyObjectByType<FirebaseGameplayHooks>() != null) return;
        var go = new GameObject("[FirebaseGameplayHooks]");
        go.AddComponent<FirebaseGameplayHooks>();
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

        _studio   = hub.studio;
        _prestige = hub.prestige;
        _city     = hub.city;

        if (_studio != null)   _studio.OnMovieCompleted += OnMovieCompleted;
        if (_prestige != null) _prestige.OnOscarGained  += OnOscarGained;

        if (_city != null)
        {
            _city.OnCityLevelChanged += OnCityLevelChanged;
            _cityLevel = _city.Level;
            _cityBound = true;
        }
    }

    void Unbind()
    {
        if (_studio != null)   _studio.OnMovieCompleted -= OnMovieCompleted;
        if (_prestige != null) _prestige.OnOscarGained  -= OnOscarGained;
        if (_city != null)     _city.OnCityLevelChanged  -= OnCityLevelChanged;
        _cityBound = false;
    }

    void OnMovieCompleted(MovieCompletePayload payload)
    {
        string movieId = payload.config != null ? payload.config.name : payload.movieName;
        FirebaseManager.Instance?.LogMovieCompleted(movieId);
    }

    void OnOscarGained()
    {
        int total = _prestige != null ? _prestige.oscars : 0;
        FirebaseManager.Instance?.LogStarEarned(total);
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
            FirebaseManager.Instance?.LogCityUnlocked(level);

        _cityLevel = level;
    }
}
