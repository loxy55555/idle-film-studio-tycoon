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
        // If GameHub already fired OnGameReady before this panel was activated, bind now.
        if (GameHub.Instance != null) OnGameReady();
    }

    private void OnEnable()
    {
        // Re-check binding every time the panel becomes visible (tab switches, etc.)
        if (_sl == null && GameHub.Instance != null)
            OnGameReady();
        else
            RefreshUI();

        // Force Slider layout so the fill rect renders correctly on the first visible frame.
        if (xpBar != null)
        {
            var parent = xpBar.transform.parent as RectTransform ?? xpBar.transform as RectTransform;
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        }
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

        if (studioNameText != null) studioNameText.text = Loc.Get(LocKeys.StudioName);
        if (levelText != null) levelText.text = string.Format(Loc.Get(LocKeys.StudioLevelFormat), _sl.Level);
        if (xpText    != null) xpText.text    = Loc.Format(LocKeys.StudioXPFormat, _sl.XP, _sl.XPToNext);

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
            xpText.text = Loc.Format(LocKeys.StudioXPFormat, _sl.XP, _sl.XPToNext);
        if (_smoothBar != null)
            _smoothBar.SetTarget(_sl.XP, _sl.XPToNext);
    }
}
