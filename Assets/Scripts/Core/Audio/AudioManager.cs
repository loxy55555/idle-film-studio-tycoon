using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FASE 15.8C — persistent 2D audio service (music + SFX).
/// Clips are indexed by asset name from Assets/Audio/music and Assets/Audio/sfx.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    readonly Dictionary<string, AudioClip> _clips = new();
    readonly List<string> _musicKeys = new();

    AudioSource _musicSource;
    AudioSource _sfxSource;

    string _lastMusicKey;
    bool _musicStarted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("[AudioManager]");
        go.AddComponent<AudioManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _musicSource = gameObject.AddComponent<AudioSource>();
        _sfxSource   = gameObject.AddComponent<AudioSource>();

        ConfigureSource(_musicSource);
        ConfigureSource(_sfxSource);

        LoadCatalog();
        ApplyUserPrefs();
        UserPrefs.OnAudioPrefsChanged += ApplyUserPrefs;
    }

    void OnDestroy()
    {
        UserPrefs.OnAudioPrefsChanged -= ApplyUserPrefs;
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!_musicStarted || _musicSource == null || _musicKeys.Count == 0) return;
        if (_musicSource.isPlaying) return;
        PlayRandomMusic();
    }

    static void ConfigureSource(AudioSource source)
    {
        source.playOnAwake   = false;
        source.loop          = false;
        source.spatialBlend  = 0f;
        source.ignoreListenerPause = false;
    }

    void LoadCatalog()
    {
        _clips.Clear();
        _musicKeys.Clear();

#if UNITY_EDITOR
        LoadFromEditorFolders();
#endif
        LoadFromClipIndex();

        foreach (var key in _clips.Keys)
        {
            if (key.StartsWith("music", System.StringComparison.OrdinalIgnoreCase))
                _musicKeys.Add(key);
        }

        _musicKeys.Sort();
    }

#if UNITY_EDITOR
    void LoadFromEditorFolders()
    {
        foreach (var folder in new[] { "Assets/Audio/music", "Assets/Audio/sfx" })
        {
            if (!UnityEditor.AssetDatabase.IsValidFolder(folder)) continue;

            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new[] { folder }))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                Register(UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path));
            }
        }
    }
#endif

    void LoadFromClipIndex()
    {
        var index = Resources.Load<AudioClipIndex>("Audio/AudioClipIndex");
        if (index?.clips == null) return;
        foreach (var clip in index.clips)
            Register(clip);
    }

    void Register(AudioClip clip)
    {
        if (clip == null || string.IsNullOrEmpty(clip.name)) return;
        _clips[clip.name] = clip;
    }

    void ApplyUserPrefs()
    {
        if (_musicSource != null) _musicSource.mute = !UserPrefs.MusicEnabled;
        if (_sfxSource != null)   _sfxSource.mute   = !UserPrefs.SfxEnabled;
    }

    public void RefreshUserPrefs() => ApplyUserPrefs();

    public void BeginSessionMusic()
    {
        if (_musicKeys.Count == 0) return;
        _musicStarted = true;
        PlayRandomMusic();
    }

    public void PlayMusic(string clipName)
    {
        if (string.IsNullOrEmpty(clipName) || _musicSource == null) return;
        if (!_clips.TryGetValue(clipName, out var clip))
        {
            LogMissing(clipName);
            return;
        }

        _musicSource.Stop();
        _musicSource.clip = clip;
        _musicSource.loop = false;
        _musicSource.Play();
        _lastMusicKey = clipName;
        _musicStarted = true;
    }

    public void PlaySfx(string clipName)
    {
        if (string.IsNullOrEmpty(clipName) || _sfxSource == null) return;
        if (!_clips.TryGetValue(clipName, out var clip))
        {
            LogMissing(clipName);
            return;
        }

        _sfxSource.PlayOneShot(clip);
    }

    void PlayRandomMusic()
    {
        if (_musicKeys.Count == 0) return;

        string next;
        if (_musicKeys.Count == 1)
        {
            next = _musicKeys[0];
        }
        else
        {
            do { next = _musicKeys[Random.Range(0, _musicKeys.Count)]; }
            while (next == _lastMusicKey);
        }

        PlayMusic(next);
    }

    static void LogMissing(string clipName)
    {
#if UNITY_EDITOR
        Debug.LogWarning($"[AudioManager] Clip not found: \"{clipName}\"");
#endif
    }
}
