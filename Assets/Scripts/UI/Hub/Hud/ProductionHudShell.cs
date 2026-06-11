using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Production tab shell — active slots, offers, new production action.</summary>
public class ProductionHudShell : MonoBehaviour
{
    public RectTransform activeProductionRoot;
    public MovieTabUI movieTab;
    public Button newProductionButton;

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
    }

    void OnNewProductionClicked()
    {
        movieTab?.RebuildAll();
    }
}
