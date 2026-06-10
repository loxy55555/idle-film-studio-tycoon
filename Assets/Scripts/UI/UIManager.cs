using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    public GameObject currentProductionPanel;
    public GameObject productionPanel;
    public GameObject departmentPanel;

    [Header("Managers")]
    public MovieUIManager movieUIManager;
    public TopBarUI topBarUI;
    public CurrentProductionPanelUI currentProductionPanelUI;
    public BottomBarUI bottomBarUI;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (bottomBarUI != null)
            bottomBarUI.Initialize(this);
    }

    public void ShowPanel(GameObject panel)
    {
        if (currentProductionPanel != null)
            currentProductionPanel.SetActive(panel == currentProductionPanel);

        if (productionPanel != null)
            productionPanel.SetActive(panel == productionPanel);

        if (departmentPanel != null)
            departmentPanel.SetActive(panel == departmentPanel);
    }
}
