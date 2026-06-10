using UnityEngine;
using UnityEngine.UI;

public class BottomBarUI : MonoBehaviour
{
    [SerializeField] private Button productionTabButton;
    [SerializeField] private Button currentProductionTabButton;
    [SerializeField] private Button departmentTabButton;

    private UIManager uiManager;

    public void Initialize(UIManager manager)
    {
        uiManager = manager;

        if (productionTabButton != null)
            productionTabButton.onClick.AddListener(() => uiManager.ShowPanel(uiManager.productionPanel));

        if (currentProductionTabButton != null)
            currentProductionTabButton.onClick.AddListener(() => uiManager.ShowPanel(uiManager.currentProductionPanel));

        if (departmentTabButton != null)
            departmentTabButton.onClick.AddListener(() => uiManager.ShowPanel(uiManager.departmentPanel));

        if (uiManager.currentProductionPanel != null)
            uiManager.ShowPanel(uiManager.currentProductionPanel);
    }

    private void OnDestroy()
    {
        if (productionTabButton != null)
            productionTabButton.onClick.RemoveAllListeners();

        if (currentProductionTabButton != null)
            currentProductionTabButton.onClick.RemoveAllListeners();

        if (departmentTabButton != null)
            departmentTabButton.onClick.RemoveAllListeners();
    }
}
