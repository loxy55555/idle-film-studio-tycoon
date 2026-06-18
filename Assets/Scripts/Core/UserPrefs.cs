using System;
using UnityEngine;

/// <summary>
/// Lightweight user preferences — language and audio settings.
/// Completely separate from SaveSystem (gameplay progress).
///
/// Language is loaded via [RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]
/// so Loc.LanguageCode is set before any Awake / Start runs,
/// guaranteeing all procedurally-built UI uses the correct language
/// from the very first frame.
/// </summary>
public static class UserPrefs
{
    const string KeyLanguage = "idlefilm.pref.language";
    const string KeyMusic    = "idlefilm.audio.music";
    const string KeySfx      = "idlefilm.audio.sfx";

    /// <summary>Fired on the main thread whenever the language changes.</summary>
    public static event Action OnLanguageChanged;

    /// <summary>Fired when music or SFX enabled state changes.</summary>
    public static event Action OnAudioPrefsChanged;

    static string _language = Loc.DefaultLanguage;
    static bool _musicEnabled = true;
    static bool _sfxEnabled   = true;

    public static bool MusicEnabled
    {
        get => _musicEnabled;
        set
        {
            bool on = value;
            if (_musicEnabled == on) return;
            _musicEnabled = on;
            PlayerPrefs.SetInt(KeyMusic, on ? 1 : 0);
            PlayerPrefs.Save();
            OnAudioPrefsChanged?.Invoke();
        }
    }

    public static bool SfxEnabled
    {
        get => _sfxEnabled;
        set
        {
            bool on = value;
            if (_sfxEnabled == on) return;
            _sfxEnabled = on;
            PlayerPrefs.SetInt(KeySfx, on ? 1 : 0);
            PlayerPrefs.Save();
            OnAudioPrefsChanged?.Invoke();
        }
    }

    /// <summary>
    /// ISO 639-1 language code ("es", "en", "fr", "de", "ja").
    /// Setting this updates Loc.LanguageCode, persists to PlayerPrefs,
    /// and fires OnLanguageChanged.
    /// </summary>
    public static string Language
    {
        get => _language;
        set
        {
            var code = string.IsNullOrWhiteSpace(value) ? Loc.DefaultLanguage : value;
            if (_language == code) return;
            _language = code;
            Loc.LanguageCode = code;
            PlayerPrefs.SetString(KeyLanguage, code);
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke();
        }
    }

    /// <summary>
    /// Runs before any scene script so all procedural UI builds
    /// in Awake/Start already see the correct language.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Load()
    {
        _language = PlayerPrefs.GetString(KeyLanguage, Loc.DefaultLanguage);
        Loc.LanguageCode = _language;
        _musicEnabled = PlayerPrefs.GetInt(KeyMusic, 1) != 0;
        _sfxEnabled   = PlayerPrefs.GetInt(KeySfx, 1) != 0;
    }
}
