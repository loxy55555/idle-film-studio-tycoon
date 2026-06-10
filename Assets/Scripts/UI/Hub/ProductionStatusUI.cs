using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Production widget with DOTween-smoothed progress bar.
/// </summary>
public class ProductionStatusUI : MonoBehaviour
{
    [Header("Idle State")]
    public GameObject idleGroup;
    public TextMeshProUGUI idleLabel;

    [Header("Active State")]
    public GameObject activeGroup;
    public TextMeshProUGUI movieNameText;
    public TextMeshProUGUI timeLeftText;
    public TextMeshProUGUI rewardText;
    public TextMeshProUGUI repText;
    public Slider progressBar;

    private StudioManager     _studio;
    private SmoothProgressBar _smoothBar;
    private float             _lastProgress = -1f;

    private void Awake()
    {
        if (GetComponent<MultiProductionPanelUI>() == null)
            gameObject.AddComponent<MultiProductionPanelUI>();

        if (progressBar != null)
            _smoothBar = progressBar.gameObject.AddComponent<SmoothProgressBar>();
        ReadOnlySlider.Configure(progressBar);
    }

    private void Start()
    {
        _studio = GameHub.Instance?.studio;
    }

    private void Update()
    {
        if (MultiProductionPanelUI.Instance != null) return;

        if (_studio == null)
        {
            _studio = GameHub.Instance?.studio;
            return;
        }

        bool producing = _studio.IsProducing;

        if (idleGroup   != null) idleGroup.SetActive(!producing);
        if (activeGroup != null) activeGroup.SetActive(producing);

        if (!producing)
        {
            _lastProgress = -1f;
            return;
        }

        if (movieNameText != null)
            movieNameText.text = _studio.CurrentMovieName;

        if (_smoothBar != null && _studio.ProductionProgress != _lastProgress)
        {
            _smoothBar.SetNormalized(_studio.ProductionProgress);
            _lastProgress = _studio.ProductionProgress;
        }
        else if (progressBar != null)
            progressBar.value = _studio.ProductionProgress;

        if (timeLeftText != null)
        {
            int secs = Mathf.CeilToInt(_studio.ProductionTimeLeft);
            timeLeftText.text = string.Format("{0:00}:{1:00}", secs / 60, secs % 60);
        }

        if (rewardText != null)
            rewardText.text = AnimatedMoneyText.FormatMoney((long)_studio.CurrentMovieReward);

        if (repText != null)
            repText.text = "+" + _studio.CurrentMovieRep.ToString("0.0") + " REP";
    }
}
