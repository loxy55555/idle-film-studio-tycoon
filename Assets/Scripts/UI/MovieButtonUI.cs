using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MovieButtonUI : MonoBehaviour
{
    [Header("Config")]
    public MovieConfig movieConfig;

    [Header("UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI genreText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI rewardText;
    public TextMeshProUGUI durationText;
    public TextMeshProUGUI repText;
    public TextMeshProUGUI unlockText;
    public TextMeshProUGUI taglineText;
    public GameObject      lockedOverlay;
    public Image           genreBadge;
    public Button          produceButton;

    private StudioManager     _studio;
    private StudioLevelSystem _studioLevel;
    private DepartmentSystem  _depts;
    private Button            _btn;
    private bool              _initialized;

    private void Awake()
    {
        _btn = produceButton != null ? produceButton
             : GetComponent<Button>() ?? GetComponentInChildren<Button>();
        if (_btn != null && _btn.GetComponent<UIButtonScale>() == null)
            _btn.gameObject.AddComponent<UIButtonScale>();
    }

    private void Start()
    {
        if (_btn == null)
            _btn = produceButton != null ? produceButton
                 : GetComponent<Button>() ?? GetComponentInChildren<Button>();
        if (_btn != null)
        {
            _btn.onClick.RemoveAllListeners();
            _btn.onClick.AddListener(OnClick);
        }

        if (GameHub.Instance != null) Initialize();
        else                          GameHub.OnGameReady += Initialize;
    }

    private void OnDestroy()
    {
        GameHub.OnGameReady -= Initialize;
    }

    private void Initialize()
    {
        GameHub.OnGameReady -= Initialize;
        _studio     = GameHub.Instance?.studio;
        _studioLevel = GameHub.Instance?.studioLevel;
        _depts      = GameHub.Instance?.departments;
        _initialized = true;
        RefreshUI();
    }

    // Legacy compatibility
    public void Setup(MovieConfig cfg, StudioManager st)
    {
        movieConfig  = cfg;
        _studio      = st;
        _studioLevel = GameHub.Instance?.studioLevel;
        _depts       = GameHub.Instance?.departments;
        _initialized = true;
        if (_btn == null) _btn = GetComponent<Button>() ?? GetComponentInChildren<Button>();
        if (_btn != null) { _btn.onClick.RemoveAllListeners(); _btn.onClick.AddListener(OnClick); }
        RefreshUI();
    }

    private void OnClick()
    {
        if (!_initialized || _studio == null || movieConfig == null) return;
        if (IsLocked()) return;
        _studio.StartMovie(movieConfig);
    }

    private void RefreshUI()
    {
        if (movieConfig == null) return;

        bool locked = IsLocked();

        if (lockedOverlay != null) lockedOverlay.SetActive(locked);
        if (_btn          != null) _btn.interactable = !locked;
        if (titleText     != null) titleText.text    = movieConfig.movieName;
        if (taglineText   != null) taglineText.text  = movieConfig.tagline;
        if (genreText     != null) genreText.text    = GenreLabel(movieConfig.genre);

        if (genreBadge != null)
        {
            ColorUtility.TryParseHtmlString(movieConfig.posterColorHex, out Color c);
            genreBadge.color = c;
        }

        if (locked)
        {
            string reason = GetLockReason();
            if (unlockText != null) unlockText.text = reason;
            if (costText   != null) costText.text   = "";
            if (rewardText != null) rewardText.text = "";
            if (durationText != null) durationText.text = "";
            if (repText    != null) repText.text    = "";
            return;
        }

        if (unlockText != null) unlockText.text = "";

        if (_depts == null) return;

        float quality   = _depts.CalculateQuality();
        float speed     = _depts.CalculateSpeed();
        float costRed   = _depts.CalculateCostReduction();
        float realCost  = movieConfig.cost     * (1f - costRed);
        float reward    = movieConfig.baseReward * movieConfig.quality * quality;
        float duration  = movieConfig.duration  / speed;
        float rep       = movieConfig.baseRep   * quality;

        if (costText     != null) costText.text     = "Coste " + AnimatedMoneyText.FormatMoney((long)realCost);
        if (rewardText   != null) rewardText.text   = AnimatedMoneyText.FormatMoney((long)reward);
        if (durationText != null) durationText.text = duration.ToString("0.0") + "s";
        if (repText      != null) repText.text      = "+" + rep.ToString("0.0") + " REP";
    }

    private bool IsLocked()
    {
        if (movieConfig == null) return false;
        int level = _studioLevel?.Level ?? 1;
        if (movieConfig.unlockStudioLevel > 0 && level < movieConfig.unlockStudioLevel) return true;
        if (GameHub.Instance?.city != null && !GameHub.Instance.city.IsMovieUnlocked(movieConfig)) return true;
        if (_studio != null && _studio.IsMovieCompleted(movieConfig)) return true;
        if (movieConfig.unlockReputation  > 0 && (_studio?.reputation ?? 0) < movieConfig.unlockReputation) return true;
        return false;
    }

    string GetLockReason()
    {
        if (GameHub.Instance?.city != null && !GameHub.Instance.city.IsMovieUnlocked(movieConfig))
            return CityProgressionRules.GetMovieLockLabel(movieConfig);
        if (movieConfig.unlockStudioLevel > 0 && (_studioLevel?.Level ?? 1) < movieConfig.unlockStudioLevel)
            return $"Requiere Nv.{movieConfig.unlockStudioLevel}";
        if (movieConfig.unlockReputation > 0 && (_studio?.reputation ?? 0) < movieConfig.unlockReputation)
            return $"Requiere {movieConfig.unlockReputation:N0} REP";
        if (_studio != null && _studio.IsMovieCompleted(movieConfig))
            return "Completada";
        return "Bloqueado";
    }

    private void Update()
    {
        if (_initialized) RefreshUI();
    }

    private static string GenreLabel(MovieGenre g) => g switch
    {
        MovieGenre.Action  => "ACCIÓN",
        MovieGenre.Drama   => "DRAMA",
        MovieGenre.Horror  => "TERROR",
        MovieGenre.Comedy  => "COMEDIA",
        MovieGenre.Romance => "ROMANCE",
        MovieGenre.SciFi   => "SCI-FI",
        _                  => g.ToString().ToUpper()
    };
}
