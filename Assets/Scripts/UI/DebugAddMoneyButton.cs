using UnityEngine;
using UnityEngine.UI;

public class DebugAddMoneyButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private StudioManager studioManager;
    [SerializeField] private long amountToAdd = 1000;

    private void Start()
    {
        if (studioManager == null && GameHub.Instance != null)
            studioManager = GameHub.Instance.studio;

        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }

    private void OnClick()
    {
        studioManager?.AddMoney(amountToAdd);
    }
}
