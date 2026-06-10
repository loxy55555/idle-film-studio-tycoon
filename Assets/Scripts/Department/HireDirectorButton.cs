using TMPro;
using UnityEngine;

/// <summary>
/// Legacy director-specific hire button. Kept for backward scene compatibility.
/// For new department cards, use DepartmentCardUI instead.
/// </summary>
public class HireDirectorButton : MonoBehaviour
{
    public DepartmentSystem departments;
    public StudioManager studio;

    public TextMeshProUGUI levelText;
    public TextMeshProUGUI costText;

    private const int BASE_COST   = 100;
    private const float COST_GROWTH = 1.5f;
    private int cost;

    private void Start()
    {
        if (departments == null && GameHub.Instance != null)
            departments = GameHub.Instance.departments;
        if (studio == null && GameHub.Instance != null)
            studio = GameHub.Instance.studio;

        // Recompute cost from saved director level so it's correct after a load
        cost = BASE_COST;
        int level = departments != null ? departments.director : 0;
        for (int i = 0; i < level; i++)
            cost = Mathf.RoundToInt(cost * COST_GROWTH);

        RefreshUI();
    }

    public void Hire()
    {
        if (departments == null || studio == null) return;
        if (!studio.TrySpendMoney(cost)) return;

        departments.director++;
        cost = Mathf.RoundToInt(cost * COST_GROWTH);

        studio.RecalculateIncome();
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (levelText != null) levelText.text = "Nivel " + (departments != null ? departments.director : 0);
        if (costText  != null) costText.text  = "$" + cost;
    }
}
