using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays a single upgrade card (equipment / personnel / installation / marketing).
/// Binds when GameHub is ready, including on inactive tabs activated after startup.
/// </summary>
public class UpgradeCardUI : MonoBehaviour
{
    [Header("Config")]
    public UpgradeConfig upgradeConfig;

    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI effectText;
    public TextMeshProUGUI costText;
    public Button          buyButton;
    public Image           badgeImage;
    public Slider          levelBar;

    private UpgradeSystem     _upgrades;
    private StudioManager     _studio;
    private DepartmentSystem  _depts;
    private StudioLevelSystem _studioLevel;
    private CitySystem        _city;
    private bool              _upgradeEventsBound;
    private bool              _cityEventsBound;

    private void Awake()
    {
        AutoWireReferences();
        GameHub.OnGameReady += Bind;
        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyClicked);
        ReadOnlySlider.Configure(levelBar);
    }

    private void OnEnable()
    {
        if (GameHub.Instance != null)
            Bind();
    }

    private void Start()
    {
        if (GameHub.Instance != null)
            Bind();
    }

    private void OnDestroy()
    {
        GameHub.OnGameReady -= Bind;
        UnbindUpgradeEvents();
        UnbindCityEvents();
    }

    public void Bind()
    {
        var hub = GameHub.Instance;
        if (hub == null) return;

        _upgrades    = hub.upgrades;
        _studio      = hub.studio;
        _depts       = hub.departments;
        _studioLevel = hub.studioLevel;
        _city        = hub.city;

        UnbindUpgradeEvents();
        if (_upgrades != null)
        {
            _upgrades.OnUpgradePurchased += RefreshUI;
            _upgradeEventsBound = true;
        }

        UnbindCityEvents();
        if (_city != null)
        {
            _city.OnCityLevelChanged += OnCityChanged;
            _cityEventsBound = true;
        }

        RefreshUI();
    }

    void OnCityChanged(int _) => RefreshUI();

    void UnbindCityEvents()
    {
        if (_city == null || !_cityEventsBound) return;
        _city.OnCityLevelChanged -= OnCityChanged;
        _cityEventsBound = false;
    }

    void UnbindUpgradeEvents()
    {
        if (_upgrades == null || !_upgradeEventsBound) return;
        _upgrades.OnUpgradePurchased -= RefreshUI;
        _upgradeEventsBound = false;
    }

    public void RefreshUI()
    {
        if (upgradeConfig == null) return;

        if (_upgrades == null || _studio == null)
        {
            if (GameHub.Instance != null)
                Bind();
            if (_upgrades == null || _studio == null) return;
        }

        AutoWireReferences();
        EnsureEffectLayout();

        int  level   = _upgrades.GetLevel(upgradeConfig);
        bool maxed   = _upgrades.IsMaxLevel(upgradeConfig);
        long cost    = _upgrades.GetNextCost(upgradeConfig);
        bool canBuy  = _upgrades.CanPurchase(upgradeConfig, _studio.Money, _studioLevel?.Level ?? 1);
        bool studioLocked = (_studioLevel?.Level ?? 1) < upgradeConfig.unlockStudioLevel;
        bool cityLocked   = GameHub.Instance?.city != null && !GameHub.Instance.city.IsUpgradeUnlocked(upgradeConfig);
        bool locked  = studioLocked || cityLocked;

        if (nameText  != null) nameText.text  = upgradeConfig.displayName;
        if (descText  != null) descText.text  = upgradeConfig.description;
        if (levelText != null) levelText.text = maxed ? "MAX" : $"Nv.{level}";

        if (effectText != null)
        {
            string current = level > 0 ? BuildEffectString(level) : "Sin efecto";
            string next    = maxed ? "—" : BuildEffectString(Mathf.Max(1, level + 1));
            effectText.text = maxed
                ? $"Ahora:\n{current}"
                : $"Ahora:\n{current}\nSiguiente:\n{next}";
        }
        if (costText   != null) costText.text   = maxed  ? "MAX" :
                                                  locked  ? LockLabel(studioLocked, cityLocked, upgradeConfig) :
                                                  AnimatedMoneyText.FormatMoney(cost);

        if (badgeImage != null)
        {
            ColorUtility.TryParseHtmlString(upgradeConfig.badgeColorHex, out Color c);
            badgeImage.color = c;
        }

        if (buyButton != null)
        {
            buyButton.interactable = canBuy;
            if (buyButton.GetComponent<UIButtonScale>() == null)
                buyButton.gameObject.AddComponent<UIButtonScale>();
            var btnImg = buyButton.GetComponent<Image>();
            if (btnImg != null)
                btnImg.color = canBuy ? new Color(0.18f, 0.80f, 0.44f) :
                               locked  ? new Color(0.50f, 0.50f, 0.50f) :
                                         new Color(0.30f, 0.30f, 0.35f);
        }

        if (levelBar != null)
        {
            var smooth = levelBar.GetComponent<SmoothProgressBar>()
                       ?? levelBar.gameObject.AddComponent<SmoothProgressBar>();
            smooth.SetTarget(level, upgradeConfig.maxLevel);
        }
    }

    void AutoWireReferences()
    {
        if (nameText == null)   nameText   = FindChildText("NameText");
        if (descText == null)   descText   = FindChildText("Desc");
        if (levelText == null)  levelText  = FindChildText("LevelText");
        if (effectText == null) effectText = FindChildText("EffectText");
        if (costText == null)   costText   = FindChildText("CostText");
        if (buyButton == null)
        {
            var btn = transform.Find("BotRow/BuyBtn") ?? transform.Find("BuyBtn");
            if (btn != null) buyButton = btn.GetComponent<Button>();
        }
        if (badgeImage == null)
        {
            var badge = transform.Find("TopRow/Badge") ?? transform.Find("Badge");
            if (badge != null) badgeImage = badge.GetComponent<Image>();
        }
        if (levelBar == null)
        {
            var bar = transform.Find("LevelBar");
            if (bar != null) levelBar = bar.GetComponent<Slider>();
        }

        if (effectText != null)
            RuntimeTmpText.ApplyDefaultFont(effectText);
    }

    void EnsureEffectLayout()
    {
        if (effectText == null) return;

        effectText.textWrappingMode = TextWrappingModes.Normal;
        effectText.overflowMode = TextOverflowModes.Overflow;
        effectText.fontSize = Mathf.Max(effectText.fontSize, 15f);
        effectText.lineSpacing = -4f;

        var effectLE = effectText.GetComponent<LayoutElement>()
                       ?? effectText.gameObject.AddComponent<LayoutElement>();
        effectLE.preferredHeight = 56f;
        effectLE.minHeight = 48f;

        var cardLE = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        cardLE.preferredHeight = Mathf.Max(cardLE.preferredHeight, 132f);
        cardLE.minHeight = 124f;
    }

    TextMeshProUGUI FindChildText(string childName)
    {
        foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp.name == childName) return tmp;
        }
        return null;
    }

    string LockLabel(bool studioLocked, bool cityLocked, UpgradeConfig cfg)
    {
        if (cityLocked)
            return CityProgressionRules.GetUpgradeLockLabel(cfg);
        if (studioLocked)
            return $"Nv.{cfg.unlockStudioLevel} estudio";
        return "Bloqueado";
    }

    private string BuildEffectString(int level)
    {
        if (level <= 0 || upgradeConfig.effects == null || upgradeConfig.effects.Length == 0)
            return "";

        var sb = new System.Text.StringBuilder();
        foreach (var e in upgradeConfig.effects)
        {
            float val = e.valuePerLevel * level;
            string part = UpgradeEffectFormatter.FormatEffect(e.type, val);
            if (!string.IsNullOrEmpty(part))
                sb.Append(part).Append("  ");
        }
        return sb.ToString().TrimEnd();
    }

    private void OnBuyClicked()
    {
        if (upgradeConfig == null) return;
        if (_upgrades == null || _studio == null)
        {
            Bind();
            if (_upgrades == null || _studio == null) return;
        }

        if (!_upgrades.Purchase(upgradeConfig, _studio, _depts, _studioLevel?.Level ?? 1))
            return;

        int newLevel = _upgrades.GetLevel(upgradeConfig);
        GameFeelUI.Instance?.ShowUpgradePurchase(transform as RectTransform, upgradeConfig, newLevel);
        RefreshUI();
    }
}

/// <summary>Shared upgrade effect labels for cards and game-feel popups.</summary>
public static class UpgradeEffectFormatter
{
    public static string FormatLevelEffects(UpgradeConfig cfg, int level)
    {
        if (cfg == null || level <= 0 || cfg.effects == null || cfg.effects.Length == 0)
            return "";

        var sb = new System.Text.StringBuilder();
        foreach (var e in cfg.effects)
        {
            float val = e.valuePerLevel * level;
            string part = FormatEffect(e.type, val);
            if (!string.IsNullOrEmpty(part))
                sb.Append(part).Append("  ");
        }
        return sb.ToString().TrimEnd();
    }

    public static string FormatEffect(UpgradeEffectType type, float val)
    {
        switch (type)
        {
            case UpgradeEffectType.Quality:
                return $"+{val * 100f:0.#}% Calidad";
            case UpgradeEffectType.Speed:
                return $"Velocidad +{val * 100f:0.#}%";
            case UpgradeEffectType.CostReduction:
                return $"-{val * 100f:0.#}% Coste";
            case UpgradeEffectType.ReputationBonus:
                return $"+{val * 100f:0.#}% Reputación";
            case UpgradeEffectType.PassiveIncomeBonus:
                return $"+{AnimatedMoneyText.FormatMoney((long)val)}/s";
            case UpgradeEffectType.MaxMovieSlots:
                return val == 1f ? "+1 Slot de Producción" : $"+{(int)val} Slots de Producción";
            case UpgradeEffectType.XPBonus:
                return $"+{val * 100f:0.#}% XP";
            default:
                return "";
        }
    }
}
