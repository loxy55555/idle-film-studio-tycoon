using TMPro;
using UnityEngine;

public class MoneyDisplayUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI incomeText;
    [SerializeField] private StudioManager studioManager;

    private void Start()
    {
        if (studioManager == null && GameHub.Instance != null)
            studioManager = GameHub.Instance.studio;

        if (studioManager == null)
        {
            Debug.LogError("MoneyDisplayUI: no se encontró StudioManager.");
            return;
        }

        studioManager.OnMoneyChanged += UpdateMoneyDisplay;
        studioManager.OnIncomeRateChanged += UpdateIncomeDisplay;

        UpdateMoneyDisplay(studioManager.Money);
        UpdateIncomeDisplay(studioManager.CurrentIncome);
    }

    private void OnDestroy()
    {
        if (studioManager == null)
            return;

        studioManager.OnMoneyChanged -= UpdateMoneyDisplay;
        studioManager.OnIncomeRateChanged -= UpdateIncomeDisplay;
    }

    private void UpdateMoneyDisplay(long amount)
    {
        if (moneyText == null)
        {
            Debug.LogError("MoneyDisplayUI: falta asignar Money Text en el Inspector.");
            return;
        }

        moneyText.text = $"${amount:N0}";
    }

    private void UpdateIncomeDisplay(long incomePerSecond)
    {
        if (incomeText == null)
            return;

        incomeText.text = $"+${incomePerSecond:N0}/seg";
    }
}
