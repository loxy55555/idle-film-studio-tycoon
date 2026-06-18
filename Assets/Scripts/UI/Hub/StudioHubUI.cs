using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Generic tab controller with DOTween panel fade.
/// </summary>
public class StudioHubUI : MonoBehaviour
{
    [Header("Tab Panels (array — index matches button index)")]
    public GameObject[] tabPanels;

    [Header("Tab Buttons (array)")]
    public Button[] tabButtons;

    [Header("Legacy — three-tab wiring (kept for compatibility)")]
    public GameObject mejorasPanel;
    public GameObject peliculasPanel;
    public GameObject premiosPanel;

    public Button mejorasBtn;
    public Button peliculasBtn;
    public Button premiosBtn;

    [Header("Style")]
    public Color activeTabColor   = CinematicTheme.GoldBright;
    public Color inactiveTabColor = CinematicTheme.TextDim;

    [Header("Animation")]
    public float fadeDuration = 0.2f;

    private int _currentTab = 0;
    private CanvasGroup[] _panelGroups;

    public int CurrentTab => _currentTab;

    public bool IsMainBottomNavigation
    {
        get
        {
            var shell = FindAnyObjectByType<DefinitiveHudShell>();
            return shell != null && shell.mainNavigation == this;
        }
    }

    private void Start()
    {
        if ((tabPanels == null || tabPanels.Length == 0) &&
            (mejorasPanel != null || peliculasPanel != null || premiosPanel != null))
        {
            tabPanels  = new[] { mejorasPanel,  peliculasPanel,  premiosPanel };
            tabButtons = new[] { mejorasBtn,    peliculasBtn,    premiosBtn    };
        }

        CachePanelGroups();

        RewireTabListeners();

        ShowTab(0, instant: true);
    }

    public void RewireTabListeners()
    {
        if (tabButtons == null) return;
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int idx = i;
            if (tabButtons[i] == null) continue;
            if (tabButtons[i].GetComponent<UIButtonScale>() == null)
                tabButtons[i].gameObject.AddComponent<UIButtonScale>();
            tabButtons[i].onClick.RemoveAllListeners();
            tabButtons[i].onClick.AddListener(() => ShowTab(idx));
        }
    }

    void CachePanelGroups()
    {
        if (tabPanels == null) return;
        _panelGroups = new CanvasGroup[tabPanels.Length];
        for (int i = 0; i < tabPanels.Length; i++)
        {
            if (tabPanels[i] == null) continue;
            _panelGroups[i] = tabPanels[i].GetComponent<CanvasGroup>();
            if (_panelGroups[i] == null)
                _panelGroups[i] = tabPanels[i].AddComponent<CanvasGroup>();
        }
    }

    public void ShowTab(int idx) => ShowTab(idx, instant: false);

    public void ShowTab(int idx, bool instant)
    {
        if (!instant && idx != _currentTab)
            AudioManager.Instance?.PlaySfx("tabswitch");

        _currentTab = idx;

        if (tabPanels != null)
        {
            // Pre-pass: kill ALL pending tweens (RT + CanvasGroup) and immediately deactivate every
            // non-target panel.  The old pattern used OnComplete(() => SetActive(false)) which never
            // fired when rapid switches called DOKill(canvasGroup) mid-animation, leaving multiple
            // panels active simultaneously with drifted anchoredPositions.
            for (int i = 0; i < tabPanels.Length; i++)
            {
                if (tabPanels[i] == null || i == idx) continue;
                tabPanels[i].GetComponent<RectTransform>()?.DOKill(false);
                bool hg = _panelGroups != null && i < _panelGroups.Length && _panelGroups[i] != null;
                if (hg)
                {
                    _panelGroups[i].DOKill(false);
                    _panelGroups[i].blocksRaycasts = false;
                    _panelGroups[i].interactable   = false;
                    _panelGroups[i].alpha           = 0f;
                }
                var rt = tabPanels[i].GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = Vector2.zero;
                tabPanels[i].SetActive(false);
            }

            // Main pass: activate and animate only the target panel.
            for (int i = 0; i < tabPanels.Length; i++)
            {
                if (tabPanels[i] == null || i != idx) continue;
                bool hasGroup = _panelGroups != null && i < _panelGroups.Length && _panelGroups[i] != null;

                tabPanels[i].SetActive(true);
                tabPanels[i].GetComponent<MovieCollectionUI>()?.Refresh();
                if (hasGroup)
                {
                    _panelGroups[i].blocksRaycasts = true;
                    _panelGroups[i].interactable   = true;
                    if (instant)
                        _panelGroups[i].alpha = 1f;
                    else
                    {
                        _panelGroups[i].alpha = 0f;
                        UIAnimationService.PlayPanelOpen(
                            tabPanels[i].GetComponent<RectTransform>(), _panelGroups[i]);
                    }
                }
            }
        }

        UpdateTabButtonStyles(idx, instant);
        RestoreMainNavHighlightIfSubHub();
    }

    void RestoreMainNavHighlightIfSubHub()
    {
        if (IsMainBottomNavigation) return;
        var shell = FindAnyObjectByType<DefinitiveHudShell>();
        var main = shell?.mainNavigation;
        if (main == null || main == this) return;
        main.SyncMainNavHighlight(main.CurrentTab, instant: true);
    }

    void UpdateTabButtonStyles(int idx, bool instant)
    {
        if (tabButtons == null) return;

        if (IsMainBottomNavigation)
        {
            for (int i = 0; i < tabButtons.Length; i++)
                ApplyTabButtonStyle(tabButtons[i], i == idx, instant);
            return;
        }

        for (int i = 0; i < tabButtons.Length; i++)
            ApplyTabButtonStyle(tabButtons[i], i == idx, instant);
    }

    void ApplyTabButtonStyle(Button btn, bool active, bool instant)
    {
        if (btn == null) return;
        UpdateTabStyle(btn, active);
        if (instant) return;
        var label = btn.GetComponentInChildren<TextMeshProUGUI>();
        UIAnimationService.PlayTabSelect(btn.transform as RectTransform, label, active);
    }

    public void SyncMainNavHighlight(int tabIndex, bool instant = true)
    {
        if (!IsMainBottomNavigation) return;
        _currentTab = tabIndex;
        UpdateTabButtonStyles(tabIndex, instant);
    }

    private void UpdateTabStyle(Button btn, bool active)
    {
        if (btn == null) return;

        var label = btn.transform.Find("Label")?.GetComponent<TextMeshProUGUI>()
                 ?? btn.GetComponentInChildren<TextMeshProUGUI>();
        var img = btn.GetComponent<Image>();
        var variant = IsMainBottomNavigation ? HudTabVariant.MainNav : HudTabVariant.SubTab;
        HudSkinProvider.ApplyTab(img, label, active, variant);

        var indicator = btn.transform.Find("ActiveLine")?.GetComponent<Image>();
        HudSkinProvider.ApplyTabIndicator(indicator, active);
    }
}
