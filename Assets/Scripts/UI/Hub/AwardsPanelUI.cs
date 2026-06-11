using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Oscars panel — claim Oscars permanently (no reset).</summary>
public class AwardsPanelUI : MonoBehaviour
{
    [Header("Oscar Display")]
    public TextMeshProUGUI oscarCountText;
    public TextMeshProUGUI multiplierText;

    [Header("Progress to next Oscar")]
    public TextMeshProUGUI thresholdLabel;
    public Slider prestigeProgressBar;
    public TextMeshProUGUI progressText;

    [Header("Oscar Button")]
    public Button prestigeButton;
    public TextMeshProUGUI prestigeButtonLabel;

    [Header("Colors")]
    public Color readyColor    = new Color(0.18f, 0.80f, 0.44f);
    public Color notReadyColor = new Color(0.40f, 0.40f, 0.55f);

    static readonly Color BG_SECTION = new Color(0.09f, 0.09f, 0.18f);
    static readonly Color FILL_GREEN = new Color(0.15f, 0.68f, 0.38f);

    private PrestigeSystem prestige;
    private StudioManager  studio;
    private bool           _buttonsWired;
    private bool           _runtimeUiBuilt;

    private void Awake()
    {
        AutoWireReferences();
        EnsureRuntimeUI();
        GameHub.OnGameReady += Bind;
    }

    private void OnEnable()
    {
        if (GameHub.Instance != null)
            Bind();
    }

    private void Start()
    {
        AutoWireReferences();
        EnsureRuntimeUI();
        WireButtons();
        if (GameHub.Instance != null)
            Bind();
    }

    private void OnDestroy()
    {
        GameHub.OnGameReady -= Bind;
        if (prestigeButton != null)
            prestigeButton.onClick.RemoveListener(OnPrestigeClick);
    }

    void AutoWireReferences()
    {
        if (oscarCountText == null)
            oscarCountText = FindTmpDeep(transform, "OscarsCount");
        if (multiplierText == null)
            multiplierText = FindTmpDeep(transform, "MultText");
        if (thresholdLabel == null)
            thresholdLabel = FindTmpDeep(transform, "ThrLabel");
        if (progressText == null)
            progressText = FindTmpDeep(transform, "ThrValue");
    }

    static TextMeshProUGUI FindTmpDeep(Transform root, string name)
    {
        foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp.name == name)
                return tmp;
        }
        return null;
    }

    void EnsureRuntimeUI()
    {
        if (_runtimeUiBuilt) return;
        _runtimeUiBuilt = true;

        if (prestigeProgressBar == null)
            prestigeProgressBar = CreateProgressBar();

        if (prestigeButton == null)
            CreatePrestigeButton();
    }

    Slider CreateProgressBar()
    {
        var parent = transform.Find("ThresholdCard");
        if (parent == null) parent = transform;

        var barGo = new GameObject("PrestigeProgressBar", typeof(RectTransform), typeof(Image), typeof(Slider));
        barGo.transform.SetParent(parent, false);
        barGo.GetComponent<Image>().color = BG_SECTION;

        var le = barGo.AddComponent<LayoutElement>();
        le.preferredHeight = 14;
        le.flexibleWidth = 1;

        var slider = barGo.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.interactable = false;

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(barGo.transform, false);
        var faRT = fillArea.GetComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero;
        faRT.anchorMax = Vector2.one;
        faRT.offsetMin = faRT.offsetMax = Vector2.zero;

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        fill.GetComponent<Image>().color = FILL_GREEN;
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
        slider.fillRect = fillRT;
        ReadOnlySlider.Configure(slider);

        return slider;
    }

    void CreatePrestigeButton()
    {
        var btnGo = new GameObject("PrestigeBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(transform, false);
        btnGo.GetComponent<Image>().color = notReadyColor;

        var le = btnGo.AddComponent<LayoutElement>();
        le.preferredHeight = 44;
        le.flexibleWidth = 1;

        btnGo.AddComponent<UIButtonScale>();

        var lblGo = new GameObject("Label", typeof(RectTransform));
        lblGo.transform.SetParent(btnGo.transform, false);
        prestigeButtonLabel = lblGo.AddComponent<TextMeshProUGUI>();
        prestigeButtonLabel.text = "Sigue produciendo...";
        prestigeButtonLabel.fontSize = 18;
        prestigeButtonLabel.fontStyle = FontStyles.Bold;
        prestigeButtonLabel.color = Color.white;
        prestigeButtonLabel.alignment = TextAlignmentOptions.Center;

        var lblRT = lblGo.GetComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero;
        lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;

        prestigeButton = btnGo.GetComponent<Button>();
    }

    void WireButtons()
    {
        if (_buttonsWired || prestigeButton == null) return;
        prestigeButton.onClick.RemoveListener(OnPrestigeClick);
        prestigeButton.onClick.AddListener(OnPrestigeClick);
        _buttonsWired = true;
    }

    void Bind()
    {
        prestige = GameHub.Instance?.prestige;
        studio   = GameHub.Instance?.studio;
        WireButtons();
    }

    private void Update()
    {
        if (prestige == null || studio == null)
        {
            if (GameHub.Instance != null)
                Bind();
            if (prestige == null || studio == null) return;
        }

        float rep       = studio.reputation;
        float threshold = prestige.NextOscarThreshold;
        bool  canClaim  = prestige.CanClaimOscar(rep);

        if (oscarCountText != null)
            oscarCountText.text = prestige.oscars == 1 ? "1 Oscar" : $"{prestige.oscars} Oscars";

        if (multiplierText != null)
        {
            var city = GameHub.Instance?.city;
            if (city != null)
                multiplierText.text = $"{city.CurrentDefinition.displayName} · ×{city.GlobalMultiplier:0.00}";
            else
                multiplierText.text = "Multiplicador: ×1.00";
        }

        if (thresholdLabel != null)
            thresholdLabel.text = "Próximo Oscar: " + threshold.ToString("N0") + " REP";

        float fill = threshold > 0f ? Mathf.Clamp01(rep / threshold) : 0f;
        if (prestigeProgressBar != null)
            prestigeProgressBar.value = fill;

        if (progressText != null)
            progressText.text = rep.ToString("N0") + " / " + threshold.ToString("N0") + " REP";

        if (prestigeButton != null)
        {
            prestigeButton.interactable = canClaim;
            var img = prestigeButton.GetComponent<Image>();
            if (img != null) img.color = canClaim ? readyColor : notReadyColor;
        }

        if (prestigeButtonLabel != null)
            prestigeButtonLabel.text = canClaim ? "CONSEGUIR OSCAR" : "Sigue produciendo...";
    }

    private void OnPrestigeClick()
    {
        if (prestige == null || studio == null) return;
        if (!prestige.CanClaimOscar(studio.reputation)) return;
        GameHub.Instance.ClaimOscar();
    }
}
