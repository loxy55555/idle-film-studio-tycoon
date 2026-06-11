using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopBarUI : MonoBehaviour
{
    [Header("Money Row")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI incomeText;
    [SerializeField] private TextMeshProUGUI diamondsText;

    [Header("Stats Row")]
    [SerializeField] private TextMeshProUGUI reputationText;
    [SerializeField] private TextMeshProUGUI oscarsText;
    [SerializeField] private TextMeshProUGUI cityText;
    [SerializeField] private TextMeshProUGUI levelText;

    [SerializeField] private TextMeshProUGUI qualityText;
    [SerializeField] private TextMeshProUGUI speedText;

    [SerializeField] private StudioManager studio;

    static readonly Color DIAMOND_COLOR = new Color(0.45f, 0.82f, 1f);

    private AnimatedMoneyText _animatedMoney;
    private DiamondWallet     _diamonds;
    private CitySystem        _city;
    private DepartmentSystem  D  => GameHub.Instance?.departments;
    private PrestigeSystem    P  => GameHub.Instance?.prestige;
    private StudioLevelSystem SL => GameHub.Instance?.studioLevel;
    private RectTransform     _oscarRT;
    private RectTransform     _diamondRT;

    private bool _subscribed;
    private bool _diamondsSubscribed;
    private bool _citySubscribed;

    private void Awake()
    {
        AutoWireMoneyTexts();
        AutoWireStatsTexts();
        AutoWireDiamondsText();
    }

    private void Start()
    {
        AutoWireMoneyTexts();
        AutoWireStatsTexts();
        AutoWireDiamondsText();

        if (moneyText != null)
            _animatedMoney = moneyText.GetComponent<AnimatedMoneyText>()
                             ?? moneyText.gameObject.AddComponent<AnimatedMoneyText>();

        if (oscarsText != null)
            _oscarRT = oscarsText.GetComponent<RectTransform>();

        if (diamondsText != null)
            _diamondRT = diamondsText.GetComponent<RectTransform>();

        GameHub.OnGameReady += BindDiamonds;
        GameHub.OnGameReady += BindCity;
        if (GameHub.Instance != null) BindDiamonds();
        if (GameHub.Instance != null) BindCity();
        TrySubscribe();
    }

    private void OnDestroy()
    {
        GameHub.OnGameReady -= BindDiamonds;
        GameHub.OnGameReady -= BindCity;
        UnbindDiamonds();
        UnbindCity();
        if (studio == null) return;
        studio.OnMoneyChanged      -= UpdateMoney;
        studio.OnIncomeRateChanged -= UpdateIncome;
        studio.OnReputationChanged -= UpdateReputation;
    }

    void AutoWireMoneyTexts()
    {
        if (moneyText == null)
            moneyText = FindChildText("MoneyText");
        if (incomeText == null)
            incomeText = FindChildText("IncomeText");
    }

    void AutoWireStatsTexts()
    {
        var statsBlock = transform.Find("StatsBlock");
        if (statsBlock == null) return;

        if (reputationText == null)
            reputationText = FindStatText(statsBlock, "RepText");
        if (oscarsText == null)
            oscarsText = FindStatText(statsBlock, "OscarsText");
        if (cityText == null)
            cityText = FindStatText(statsBlock, "CityText");
        if (levelText == null)
            levelText = FindStatText(statsBlock, "LevelText");
        EnsureCityText(statsBlock as RectTransform);
    }

    void AutoWireDiamondsText()
    {
        if (diamondsText == null)
            diamondsText = FindChildText("DiamondsText");

        if (diamondsText != null)
        {
            RuntimeTmpText.ApplyDefaultFont(diamondsText);
            return;
        }

        var statsBlock = transform.Find("StatsBlock") as RectTransform;
        if (statsBlock == null)
        {
            diamondsText = RuntimeTmpText.Create(transform, "DIA 0", 18, DIAMOND_COLOR, FontStyles.Bold);
            return;
        }

        var block = new GameObject("DiamondsBlock", typeof(RectTransform));
        block.transform.SetParent(statsBlock, false);
        var blockLE = block.AddComponent<LayoutElement>();
        blockLE.flexibleWidth = 1f;
        blockLE.preferredWidth = 88f;

        var vl = block.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment = TextAnchor.MiddleCenter;
        vl.childControlWidth = vl.childControlHeight = true;
        vl.childForceExpandWidth = vl.childForceExpandHeight = true;
        vl.spacing = 0;

        var label = RuntimeTmpText.Create(block.transform, "DIAM", 11, new Color(0.54f, 0.54f, 0.67f),
            FontStyles.Normal, TextAlignmentOptions.Center, "DiamondsLabel");
        label.gameObject.AddComponent<LayoutElement>().preferredHeight = 14;

        diamondsText = RuntimeTmpText.Create(block.transform, "0", 20, DIAMOND_COLOR, FontStyles.Bold,
            TextAlignmentOptions.Center, "DiamondsText");
        diamondsText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24;

        block.transform.SetSiblingIndex(statsBlock.childCount - 1);
        UpdateDiamonds(0);
    }

    TextMeshProUGUI FindChildText(string name)
    {
        foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp.name == name) return tmp;
        }
        return null;
    }

    static TextMeshProUGUI FindStatText(Transform statsBlock, string textName)
    {
        var direct = statsBlock.Find(textName);
        if (direct != null)
        {
            var tmp = direct.GetComponent<TextMeshProUGUI>();
            if (tmp != null) return tmp;
        }

        foreach (Transform block in statsBlock)
        {
            var text = block.Find(textName);
            if (text == null) continue;
            var tmp = text.GetComponent<TextMeshProUGUI>();
            if (tmp != null) return tmp;
        }

        return null;
    }

    void BindCity()
    {
        UnbindCity();
        _city = GameHub.Instance?.city;
        if (_city == null) return;
        _city.OnCityLevelChanged += UpdateCity;
        _citySubscribed = true;
        UpdateCity(_city.Level);
    }

    void UnbindCity()
    {
        if (_city == null || !_citySubscribed) return;
        _city.OnCityLevelChanged -= UpdateCity;
        _citySubscribed = false;
    }

    void EnsureCityText(RectTransform statsBlock)
    {
        if (cityText != null || statsBlock == null) return;

        cityText = RuntimeTmpText.Create(statsBlock, "C1", 18, new Color(0.95f, 0.77f, 0.06f), FontStyles.Bold,
            TextAlignmentOptions.Center, "CityText");
        cityText.gameObject.AddComponent<LayoutElement>().preferredWidth = 96f;
    }

    void UpdateCity(int level)
    {
        if (cityText == null) return;
        cityText.text = "C" + level;
    }

    void BindDiamonds()
    {
        UnbindDiamonds();
        _diamonds = GameHub.Instance?.diamonds;
        if (_diamonds == null) return;
        _diamonds.OnDiamondsChanged += UpdateDiamonds;
        _diamondsSubscribed = true;
        UpdateDiamonds(_diamonds.Balance);
    }

    void UnbindDiamonds()
    {
        if (_diamonds == null || !_diamondsSubscribed) return;
        _diamonds.OnDiamondsChanged -= UpdateDiamonds;
        _diamondsSubscribed = false;
    }

    private void TrySubscribe()
    {
        if (_subscribed) return;
        if (studio == null && GameHub.Instance != null) studio = GameHub.Instance.studio;
        if (studio == null) return;

        studio.OnMoneyChanged      += UpdateMoney;
        studio.OnIncomeRateChanged += UpdateIncome;
        studio.OnReputationChanged += UpdateReputation;
        _subscribed = true;

        UpdateMoney(studio.Money);
        UpdateIncome(studio.CurrentIncome);
        UpdateReputation(studio.reputation);
        UpdateStats();
    }

    private void Update()
    {
        if (!_subscribed) TrySubscribe();
        if (!_diamondsSubscribed && GameHub.Instance?.diamonds != null) BindDiamonds();
        if (!_citySubscribed && GameHub.Instance?.city != null) BindCity();
        UpdateStats();
    }

    private void UpdateMoney(long amount)
    {
        if (_animatedMoney != null)
            _animatedMoney.SetValue(amount);
        else if (moneyText != null)
            moneyText.text = AnimatedMoneyText.FormatMoney(amount);
    }

    private void UpdateIncome(long income)
    {
        if (incomeText != null)
            incomeText.text = "+" + AnimatedMoneyText.FormatMoney(income) + "/s";
    }

    private void UpdateReputation(float rep)
    {
        if (reputationText != null)
            reputationText.text = "REP " + rep.ToString("N0");
    }

    void UpdateDiamonds(int amount)
    {
        if (diamondsText == null) return;
        diamondsText.text = amount.ToString("N0");

        if (_diamondRT == null) _diamondRT = diamondsText.GetComponent<RectTransform>();
        if (_diamondRT == null) return;
        _diamondRT.DOKill();
        _diamondRT.localScale = Vector3.one;
        _diamondRT.DOPunchScale(Vector3.one * 0.12f, 0.3f, 6, 0.5f).SetUpdate(true);
    }

    private void UpdateStats()
    {
        if (D != null)
        {
            if (qualityText != null) qualityText.text = "Cal " + D.CalculateQuality().ToString("0.0");
            if (speedText   != null) speedText.text   = "Vel " + D.CalculateSpeed().ToString("0.0");
        }

        if (P != null && oscarsText != null)
            oscarsText.text = "★ " + P.oscars;

        if (_city != null && cityText != null)
            UpdateCity(_city.Level);

        if (SL != null && levelText != null && levelText.gameObject.activeInHierarchy)
            levelText.text = "Nv." + SL.Level;
    }

    public RectTransform GetOscarRect() => _oscarRT;
}
