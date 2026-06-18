using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Production tab shell — active slots, offers, new production action. Phase 8.5C: upgrades offer slot layout to tall vertical cards.</summary>
public class ProductionHudShell : MonoBehaviour
{
    public RectTransform activeProductionRoot;
    public MovieTabUI    movieTab;
    public Button        newProductionButton;

    bool _layoutUpgraded;

    void Awake()
    {
        WireRefreshButton();
    }

    void Start()
    {
        UpgradeProductionLayout();
        ApplyLocalizedButtonLabel();
    }

    void OnEnable()
    {
        ApplyLocalizedButtonLabel();
        WireRefreshButton();
        // Re-apply layout upgrade if the panel was rebuilt (e.g. after domain reload)
        if (!_layoutUpgraded)
            UpgradeProductionLayout();
    }

    public void RefreshLocalization() => ApplyLocalizedButtonLabel();

    public void Configure(RectTransform activeProductionRoot, MovieTabUI movieTab, Button newProductionButton)
    {
        this.activeProductionRoot = activeProductionRoot;
        this.movieTab             = movieTab;
        this.newProductionButton  = newProductionButton;
        WireRefreshButton();
        ApplyLocalizedButtonLabel();
    }

    /// <summary>
    /// Phase 8.5C: Replaces SquareTileRowLayout + HorizontalLayoutGroup on the offer slots row
    /// with a VerticalLayoutGroup so each offer card is full-width and tall (mobile-first).
    /// Also shrinks the history section to give offers more visual prominence.
    /// </summary>
    void UpgradeProductionLayout()
    {
        if (movieTab?.slotsRow == null) return;
        if (_layoutUpgraded) return;
        _layoutUpgraded = true;

        var row = movieTab.slotsRow;

        // Remove square-tile constraint (makes cards square = small)
        var strl = row.GetComponent<SquareTileRowLayout>();
        if (strl != null) DestroyImmediate(strl);

        // Replace HorizontalLayoutGroup with VerticalLayoutGroup
        var hlg = row.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) DestroyImmediate(hlg);

        var vlg = row.gameObject.GetComponent<VerticalLayoutGroup>()
                  ?? row.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing              = HudLayoutConstants.SectionSpacing;
        vlg.padding              = HudLayoutConstants.SectionPadding;
        vlg.childControlWidth    = vlg.childControlHeight   = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Offer row grows to fill available space in the production panel
        var rowLE = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
        rowLE.flexibleHeight  = 2f;
        rowLE.preferredHeight = -1f;
        rowLE.minHeight       = -1f;

        // Shrink history section so offers dominate (find it as sibling)
        ConstrainHistorySection(row.transform.parent);

        // Refresh cards so they are rendered with the new layout
        movieTab.RebuildAll();
        PatchOfferRarityIcons(movieTab);
    }

    static void PatchOfferRarityIcons(MovieTabUI movieTab)
    {
        if (movieTab?.slotsRow == null) return;
        for (int i = 0; i < movieTab.slotsRow.childCount; i++)
        {
            var card = movieTab.slotsRow.GetChild(i);
            var ui = card.GetComponent<MovieButtonUI>();
            var rarity = ui?.movieConfig?.rarity ?? MovieRarity.Common;
            var genre  = ui?.movieConfig?.genre ?? MovieGenre.Drama;
            MovieOfferCardLayoutBuilder.ApplyRarityIconLayout(card, rarity, genre);
        }
    }

    static void ConstrainHistorySection(Transform productionPanel)
    {
        if (productionPanel == null) return;

        var historyScroll = productionPanel.Find("MovieHistoryScroll")
                         ?? productionPanel.Find("HistoryScroll");
        if (historyScroll == null)
        {
            // Fallback: find a ScrollRect sibling that isn't the active production widget
            foreach (Transform child in productionPanel)
            {
                if (child.GetComponent<ScrollRect>() != null &&
                    child.name != "MovieSlotsRow" &&
                    child.name != "ProductionWidget" &&
                    child.name != "ActiveProductionRoot")
                {
                    historyScroll = child;
                    break;
                }
            }
        }

        if (historyScroll != null)
        {
            var le = historyScroll.GetComponent<LayoutElement>() ?? historyScroll.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight  = HudLayoutConstants.ProductionHistoryHeight;
            le.minHeight        = 72f;
            le.flexibleHeight   = 0f;
        }
    }

    void WireRefreshButton()
    {
        if (newProductionButton == null) return;
        newProductionButton.onClick.RemoveListener(OnNewProductionClicked);
        newProductionButton.onClick.AddListener(OnNewProductionClicked);
    }

    void ApplyLocalizedButtonLabel()
    {
        if (newProductionButton == null) return;
        var label = newProductionButton.transform.Find("Label")?.GetComponent<TextMeshProUGUI>()
                 ?? newProductionButton.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.text = Loc.Get(LocKeys.ProdNewProduction);
            label.fontSize = 17f;
            label.fontStyle = FontStyles.Bold;
        }

        var le = newProductionButton.GetComponent<LayoutElement>();
        if (le != null) le.preferredHeight = Mathf.Max(le.preferredHeight, HudLayoutConstants.ProductionActionHeight);
    }

    void OnNewProductionClicked()
    {
        Debug.Log("[Offers] RefreshClicked");
        GameplayRefreshService.RequestProductionOfferRefresh(OnRefreshOffersComplete);
    }

    void OnRefreshOffersComplete()
    {
        Debug.Log("[Offers] RefreshGenerated");
        movieTab?.RefreshOfferDisplay();
    }
}

