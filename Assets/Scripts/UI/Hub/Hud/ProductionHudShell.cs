using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Production tab shell — active slots, offers, new production action.</summary>
public class ProductionHudShell : MonoBehaviour
{
    public RectTransform activeProductionRoot;
    public MovieTabUI movieTab;
    public Button newProductionButton;

    void OnEnable()
    {
        ApplyLocalizedButtonLabel();
    }

    public void Configure(RectTransform activeProductionRoot, MovieTabUI movieTab, Button newProductionButton)
    {
        this.activeProductionRoot = activeProductionRoot;
        this.movieTab = movieTab;
        this.newProductionButton = newProductionButton;

        if (this.newProductionButton != null)
        {
            this.newProductionButton.onClick.RemoveListener(OnNewProductionClicked);
            this.newProductionButton.onClick.AddListener(OnNewProductionClicked);
        }

        ApplyLocalizedButtonLabel();
    }

    void ApplyLocalizedButtonLabel()
    {
        if (newProductionButton == null) return;

        var label = newProductionButton.transform.Find("Label")?.GetComponent<TextMeshProUGUI>()
                 ?? newProductionButton.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.text = Loc.Get(LocKeys.ProdNewProduction);
    }

    void OnNewProductionClicked()
    {
        movieTab?.RebuildAll();
    }
}
