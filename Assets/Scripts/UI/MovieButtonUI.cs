using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MovieButtonUI : MonoBehaviour
{
    [Header("Config")]
    public MovieConfig movieConfig;

    [Header("UI")]
    public Image           posterImage;
    public Image           rarityFrame;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI genreText;
    public TextMeshProUGUI rarityText;
    public TextMeshProUGUI durationText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI rewardText;
    public TextMeshProUGUI repText;
    public TextMeshProUGUI badgesText;
    public TextMeshProUGUI unlockText;
    public TextMeshProUGUI taglineText;
    public GameObject      lockedOverlay;
    public Image           genreBadge;
    public Button          produceButton;
    public TextMeshProUGUI selectLabelText;

    StudioManager     _studio;
    StudioLevelSystem _studioLevel;
    DepartmentSystem  _depts;
    ContractSystem    _contracts;
    Button            _btn;
    bool              _initialized;

    void Awake()
    {
        _btn = produceButton != null ? produceButton
             : GetComponent<Button>() ?? GetComponentInChildren<Button>();
        if (_btn != null && _btn.GetComponent<UIButtonScale>() == null)
            _btn.gameObject.AddComponent<UIButtonScale>();
        ProductionBudgetPickerUI.OnVisibilityChanged += OnPickerVisibilityChanged;
    }

    void Start()
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

        ProductionPremiereHooks.EnsureOnCanvas();
    }

    void OnDestroy()
    {
        GameHub.OnGameReady -= Initialize;
        ProductionBudgetPickerUI.OnVisibilityChanged -= OnPickerVisibilityChanged;
    }

    void OnPickerVisibilityChanged(bool visible)
    {
        if (!_initialized || _btn == null) return;
        _btn.interactable = !visible && !IsLocked();
    }

    void Initialize()
    {
        GameHub.OnGameReady -= Initialize;
        _studio      = GameHub.Instance?.studio;
        _studioLevel = GameHub.Instance?.studioLevel;
        _depts       = GameHub.Instance?.departments;
        _contracts   = GameHub.Instance?.contracts;
        _initialized = true;
        RefreshUI();
    }

    public void Setup(MovieConfig cfg, StudioManager st)
    {
        movieConfig  = cfg;
        _studio      = st;
        _studioLevel = GameHub.Instance?.studioLevel;
        _depts       = GameHub.Instance?.departments;
        _contracts   = GameHub.Instance?.contracts;
        _initialized = true;
        if (_btn == null) _btn = produceButton ?? GetComponent<Button>() ?? GetComponentInChildren<Button>();
        if (_btn != null) { _btn.onClick.RemoveAllListeners(); _btn.onClick.AddListener(OnClick); }
        RefreshUI();
    }

    void OnClick()
    {
        if (!_initialized || _studio == null || movieConfig == null) return;
        if (IsLocked()) return;

        ProductionBudgetPickerUI.Show(movieConfig, budget =>
            _studio.StartMovie(movieConfig, budget));
    }

    void RefreshUI()
    {
        if (movieConfig == null) return;

        bool locked = IsLocked();

        if (lockedOverlay != null) lockedOverlay.SetActive(locked);
        if (_btn != null)
            _btn.interactable = !locked && !ProductionBudgetPickerUI.IsOpen;
        if (selectLabelText != null)
            selectLabelText.text = Loc.Get(LocKeys.ProdSelect);

        if (titleText != null) titleText.text = movieConfig.movieName;
        if (taglineText != null) taglineText.text = movieConfig.tagline;
        if (genreText != null) genreText.text = GenreLoc.GetLabel(movieConfig.genre);

        if (rarityText != null)
        {
            rarityText.text = MovieOfferCardLayoutBuilder.GetRarityIcon(movieConfig.rarity, movieConfig.genre);
            rarityText.color = MovieOfferCardLayoutBuilder.GetRarityIconColor(movieConfig.rarity, movieConfig.genre);
        }

        if (rarityFrame != null) MovieRarityVisual.ApplyFrame(rarityFrame, movieConfig.rarity);

        if (locked)
        {
            if (unlockText != null)
            {
                unlockText.text = GetLockReason();
                unlockText.gameObject.SetActive(true);
            }
            if (costText != null) costText.text = string.Empty;
            if (rewardText != null) rewardText.text = string.Empty;
            if (durationText != null) durationText.text = string.Empty;
            if (repText != null) repText.text = string.Empty;
            if (badgesText != null) badgesText.text = string.Empty;
            return;
        }

        if (unlockText != null) unlockText.gameObject.SetActive(false);
        if (_depts == null) return;

        float speed    = _depts.CalculateSpeed();
        float costRed  = _depts.CalculateCostReduction();
        float quality  = _depts.CalculateQuality();
        float realCost = movieConfig.cost * (1f - costRed);
        float reward   = movieConfig.baseReward * movieConfig.quality * quality;
        float duration = movieConfig.duration / speed;
        float rep      = movieConfig.baseRep * quality;

        if (durationText != null) durationText.text = "⏱ " + ProductionLoc.FormatDuration(duration);
        if (costText != null) costText.text = AnimatedMoneyText.FormatMoney((long)realCost);
        if (rewardText != null) rewardText.text = "💵 " + Loc.Format(LocKeys.ProdOfferMoney, AnimatedMoneyText.FormatMoney((long)reward));
        if (repText != null) repText.text = "🏆 " + Loc.Format(LocKeys.ProdOfferRep, rep.ToString("0.0"));

        if (badgesText != null)
        {
            string badges = MovieOfferBadgeHelper.BuildBadgeLine(
                movieConfig,
                _studio?.CompletedMovieKeys,
                _contracts,
                _depts);
            badgesText.text = badges;
            badgesText.gameObject.SetActive(!string.IsNullOrEmpty(badges));
        }
    }

    bool IsLocked()
    {
        if (movieConfig == null) return false;
        int level = _studioLevel?.Level ?? 1;
        if (movieConfig.unlockStudioLevel > 0 && level < movieConfig.unlockStudioLevel) return true;
        if (GameHub.Instance?.city != null && !GameHub.Instance.city.IsMovieUnlocked(movieConfig)) return true;
        if (_studio != null && _studio.IsMovieCompleted(movieConfig)) return true;
        if (movieConfig.unlockReputation > 0 && (_studio?.reputation ?? 0) < movieConfig.unlockReputation) return true;
        if (_studio != null && !SagaProgressionRules.ArePreviousSagaEntriesDiscovered(
                movieConfig, _studio.CompletedMovieKeys, GetAllMovies())) return true;
        return false;
    }

    MovieConfig[] GetAllMovies() => MovieCatalogRuntime.AllMovies;

    string GetLockReason()
    {
        if (GameHub.Instance?.city != null && !GameHub.Instance.city.IsMovieUnlocked(movieConfig))
            return CityProgressionRules.GetMovieLockLabel(movieConfig);
        if (movieConfig.unlockStudioLevel > 0 && (_studioLevel?.Level ?? 1) < movieConfig.unlockStudioLevel)
            return Loc.Format(LocKeys.DeptLevelFormat, movieConfig.unlockStudioLevel);
        if (movieConfig.unlockReputation > 0 && (_studio?.reputation ?? 0) < movieConfig.unlockReputation)
            return movieConfig.unlockReputation.ToString("N0") + " REP";
        if (_studio != null && !SagaProgressionRules.ArePreviousSagaEntriesDiscovered(
                movieConfig, _studio.CompletedMovieKeys, GetAllMovies()))
            return SagaProgressionRules.GetSagaBlockReason(movieConfig, GetAllMovies()) ?? Loc.Get(LocKeys.DeptLocked);
        if (_studio != null && _studio.IsMovieCompleted(movieConfig))
            return Loc.Get(LocKeys.DeptLocked);
        return Loc.Get(LocKeys.DeptLocked);
    }

    void Update()
    {
        if (_initialized) RefreshUI();
    }
}
