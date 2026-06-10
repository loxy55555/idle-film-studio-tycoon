using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrentProductionPanelUI : MonoBehaviour
{
    [SerializeField] private StudioManager studio;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Slider progressBar;

    private void Awake()
    {
        if (studio == null && GameHub.Instance != null)
            studio = GameHub.Instance.studio;

        if (studio == null)
            return;

        if (statusText == null)
            statusText = studio.movieStatusText;

        if (progressBar == null)
            progressBar = studio.movieProgressBar;

        ReadOnlySlider.Configure(progressBar);
    }
}
