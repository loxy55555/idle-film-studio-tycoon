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
    public Color activeTabColor   = new Color(0.18f, 0.80f, 0.44f);
    public Color inactiveTabColor = new Color(0.55f, 0.55f, 0.70f);

    [Header("Animation")]
    public float fadeDuration = 0.2f;

    private int _currentTab = 0;
    private CanvasGroup[] _panelGroups;

    private void Start()
    {
        if ((tabPanels == null || tabPanels.Length == 0) &&
            (mejorasPanel != null || peliculasPanel != null || premiosPanel != null))
        {
            tabPanels  = new[] { mejorasPanel,  peliculasPanel,  premiosPanel };
            tabButtons = new[] { mejorasBtn,    peliculasBtn,    premiosBtn    };
        }

        CachePanelGroups();

        if (tabButtons != null)
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int idx = i;
                if (tabButtons[i] != null)
                {
                    if (tabButtons[i].GetComponent<UIButtonScale>() == null)
                        tabButtons[i].gameObject.AddComponent<UIButtonScale>();
                    tabButtons[i].onClick.AddListener(() => ShowTab(idx));
                }
            }
        }

        ShowTab(0, instant: true);
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
        _currentTab = idx;

        if (tabPanels != null)
        {
            for (int i = 0; i < tabPanels.Length; i++)
            {
                if (tabPanels[i] == null) continue;
                bool active = i == idx;
                tabPanels[i].SetActive(active);
                if (active)
                    tabPanels[i].GetComponent<MovieCollectionUI>()?.Refresh();
                if (_panelGroups != null && i < _panelGroups.Length && _panelGroups[i] != null)
                {
                    _panelGroups[i].DOKill();
                    _panelGroups[i].blocksRaycasts = active;
                    _panelGroups[i].interactable  = active;
                    if (instant || !active)
                    {
                        _panelGroups[i].alpha = active ? 1f : 0f;
                    }
                    else
                    {
                        _panelGroups[i].alpha = 0f;
                        _panelGroups[i].DOFade(1f, fadeDuration).SetUpdate(true);
                    }
                }
            }
        }

        if (tabButtons != null)
            for (int i = 0; i < tabButtons.Length; i++)
                UpdateTabStyle(tabButtons[i], i == idx);
    }

    private void UpdateTabStyle(Button btn, bool active)
    {
        if (btn == null) return;
        var label = btn.GetComponentInChildren<TextMeshProUGUI>();
        var img = btn.GetComponent<Image>();
        HudSkinProvider.ApplyTab(img, label, active, HudTabVariant.MainNav);
    }
}
