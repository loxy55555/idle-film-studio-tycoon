using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StudioLevelBarUI : MonoBehaviour
{
    public TextMeshProUGUI studioNameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI xpText;
    public Slider          xpBar;

    private StudioLevelSystem _sl;
    private SmoothProgressBar _smoothBar;

    private void Awake()
    {
        GameHub.OnGameReady += OnGameReady;
        if (xpBar != null)
            _smoothBar = xpBar.gameObject.AddComponent<SmoothProgressBar>();
        ReadOnlySlider.Configure(xpBar);
    }

    private void OnDestroy()
    {
        GameHub.OnGameReady -= OnGameReady;
        if (_sl != null) _sl.OnLevelUp -= OnLevelUp;
    }

    private void OnGameReady()
    {
        _sl = GameHub.Instance?.studioLevel;
        if (_sl != null) _sl.OnLevelUp += OnLevelUp;
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (_sl == null) return;

        if (studioNameText != null) studioNameText.text = "IDLE FILM STUDIO";
        if (levelText != null) levelText.text = $"Nivel {_sl.Level}";
        if (xpText    != null) xpText.text    = $"{_sl.XP:0}/{_sl.XPToNext:0} XP";

        if (_smoothBar != null)
            _smoothBar.SetTarget(_sl.XP, _sl.XPToNext);
        else if (xpBar != null)
        {
            xpBar.maxValue = _sl.XPToNext;
            xpBar.value    = _sl.XP;
        }
    }

    private void OnLevelUp(int newLevel) => RefreshUI();

    private void Update()
    {
        if (_sl == null) return;
        if (xpText != null)
            xpText.text = $"{_sl.XP:0}/{_sl.XPToNext:0} XP";
        if (_smoothBar != null)
            _smoothBar.SetTarget(_sl.XP, _sl.XPToNext);
    }
}
