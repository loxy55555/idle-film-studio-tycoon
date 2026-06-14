using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Premium department card — display + personnel upgrade CTA (Phase 8.1).</summary>
public class DepartmentMiniCardUI : MonoBehaviour
{
    [HideInInspector] public DepartmentType deptType;

    [Header("UI Refs")]
    public Image           categoryBadge;
    public TextMeshProUGUI deptNameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI effectText;
    public Slider          levelProgressBar;
    public Button          upgradeButton;
    public TextMeshProUGUI upgradeLabelText;
    public TextMeshProUGUI upgradeCostText;
    public Image           themeBackdrop;
    public DepartmentThemeVisual themeVisual;

    DepartmentSystem  _depts;
    UpgradeSystem     _upgrades;
    StudioManager     _studio;
    StudioLevelSystem _studioLevel;
    CitySystem        _city;
    CanvasGroup       _canvasGroup;
    UpgradeConfig     _personnelUpgrade;
    int               _cachedLevel = -1;
    bool              _cachedLocked;
    bool              _purchaseInFlight;

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        EnsurePremiumMaterial();
        EnsurePremiumLayoutIfNeeded();
        WireUpgradeButton();
        GameHub.OnGameReady += OnGameReady;
    }

    void OnDestroy()
    {
        GameHub.OnGameReady -= OnGameReady;
        Unsubscribe();
        if (upgradeButton != null)
            upgradeButton.onClick.RemoveListener(OnUpgradeClicked);
    }

    void Unsubscribe()
    {
        if (_upgrades != null)
            _upgrades.OnUpgradePurchased -= Refresh;
        if (_city != null)
            _city.OnCityLevelChanged -= OnCityChanged;
    }

    void OnGameReady()
    {
        Unsubscribe();
        var hub = GameHub.Instance;
        _depts        = hub?.departments;
        _upgrades     = hub?.upgrades;
        _studio       = hub?.studio;
        _studioLevel  = hub?.studioLevel;
        _city         = hub?.city;
        _personnelUpgrade = DepartmentPersonnelBridge.FindUpgrade(_upgrades, deptType);

        if (_upgrades != null)
            _upgrades.OnUpgradePurchased += Refresh;
        if (_city != null)
            _city.OnCityLevelChanged += OnCityChanged;

        EnsurePremiumLayoutIfNeeded();
        WireUpgradeButton();
        Refresh();
    }

    void OnCityChanged(int _) => Refresh();

    void Start()
    {
        if (GameHub.Instance != null)
            OnGameReady();
    }

    void OnEnable()
    {
        EnsurePremiumMaterial();
        WireUpgradeButton();
        Refresh();
    }

    public void ForcePremiumLayoutRebuild() => EnsurePremiumLayoutIfNeeded();

    void EnsurePremiumLayoutIfNeeded()
    {
        if (!DepartmentMiniCardLayoutBuilder.HasPremiumLayout(transform))
        {
            var wire = DepartmentMiniCardLayoutBuilder.EnsurePremiumLayout(this);
            ApplyWire(wire);
            return;
        }

        ApplyWire(DepartmentMiniCardLayoutBuilder.WireExisting(transform));
    }

    void EnsurePremiumMaterial()
    {
        var rt = transform as RectTransform;
        if (rt != null)
            CinematicTheme.ApplyPremiumMaterial(rt);
    }

    void ApplyWire(DepartmentMiniCardLayoutBuilder.WireResult wire)
    {
        categoryBadge    = wire.categoryBadge;
        deptNameText     = wire.deptNameText;
        levelText        = wire.levelText;
        effectText       = wire.effectText;
        levelProgressBar = wire.levelProgressBar;
        upgradeButton    = wire.upgradeButton;
        upgradeLabelText = wire.upgradeLabelText;
        upgradeCostText  = wire.upgradeCostText;
        themeBackdrop    = wire.themeBackdrop;
        themeVisual      = wire.themeVisual ?? GetComponent<DepartmentThemeVisual>();

        EnsureProgressBarWired();
        themeVisual?.Apply(deptType);
    }

    void EnsureProgressBarWired()
    {
        if (levelProgressBar != null) return;
        levelProgressBar = transform.Find("Content/LevelProgressBar")?.GetComponent<Slider>();
        if (levelProgressBar == null) return;
        ReadOnlySlider.Configure(levelProgressBar);
        EnsureLevelBarLayout();
    }

    void EnsureLevelBarLayout()
    {
        if (levelProgressBar == null) return;

        var barRt = levelProgressBar.transform as RectTransform;
        if (barRt == null) return;

        var barLE = barRt.GetComponent<LayoutElement>() ?? barRt.gameObject.AddComponent<LayoutElement>();
        barLE.enabled = true;
        barLE.ignoreLayout = false;
        barLE.flexibleWidth = 1f;
        barLE.minWidth = 0f;
        if (barLE.preferredHeight < 8f) barLE.preferredHeight = 8f;
        if (barLE.minHeight < 8f) barLE.minHeight = 8f;

        var fillArea = barRt.Find("Fill Area") as RectTransform;
        if (fillArea != null)
        {
            fillArea.anchorMin = Vector2.zero;
            fillArea.anchorMax = Vector2.one;
            fillArea.offsetMin = fillArea.offsetMax = Vector2.zero;
        }
    }

    void WireUpgradeButton()
    {
        if (upgradeButton == null) return;
        upgradeButton.onClick.RemoveListener(OnUpgradeClicked);
        upgradeButton.onClick.AddListener(OnUpgradeClicked);

        var guard = upgradeButton.GetComponent<UpgradeBuyButtonGuard>()
                   ?? upgradeButton.gameObject.AddComponent<UpgradeBuyButtonGuard>();
        guard.Bind(() => _personnelUpgrade != null ? _personnelUpgrade.id : string.Empty);

        UpgradeUiRaycastPolicy.ApplyDepartmentCard(transform, upgradeButton);
    }

    void OnUpgradeClicked()
    {
        if (_purchaseInFlight || _personnelUpgrade == null || _upgrades == null || _studio == null) return;

        Debug.Log($"[Upgrade] OnClick recibido id={_personnelUpgrade.id} dept={deptType}");
        Debug.Log($"[Upgrade] OnBuyClicked ejecutado id={_personnelUpgrade.id}");

        _purchaseInFlight = true;
        if (upgradeButton != null) upgradeButton.interactable = false;

        try
        {
            long cost = _upgrades.GetNextCost(_personnelUpgrade);
            bool canBuy = _upgrades.CanPurchase(_personnelUpgrade, _studio.Money, _studioLevel?.Level ?? 1);
            Debug.Log($"[Upgrade] pre-Purchase id={_personnelUpgrade.id} money={_studio.Money} cost={cost} CanPurchase={canBuy} block={_upgrades.GetPurchaseBlockReason(_personnelUpgrade, _studio.Money, _studioLevel?.Level ?? 1)}");

            bool ok = _upgrades.Purchase(_personnelUpgrade, _studio, _depts, _studioLevel?.Level ?? 1);
            Debug.Log($"[Upgrade] Purchase()={ok} id={_personnelUpgrade.id}");
            if (ok)
                UIAnimationService.PlayUpgradeFeedback(transform as RectTransform, GetComponent<Image>(), levelText);
        }
        finally
        {
            _purchaseInFlight = false;
            Refresh();
        }
    }

    void Refresh()
    {
        if (_depts == null) return;

        themeVisual?.Apply(deptType);

        int  idx    = (int)deptType;
        var  data   = DepartmentCardUI.DeptData[idx];
        bool locked = _city != null && !_city.IsDepartmentUnlocked(deptType);
        int  level  = locked ? 0 : GetLevel();
        int  maxLevel = _personnelUpgrade != null ? _personnelUpgrade.maxLevel : 25;

        if (deptNameText != null)
        {
            deptNameText.text = DepartmentLoc.GetName(deptType);
            RuntimeTmpText.ApplyDefaultFont(deptNameText);
        }

        if (locked)
        {
            if (levelText != null)
            {
                levelText.text = Loc.Format(LocKeys.DeptAvailableCity, CityProgressionRules.GetDepartmentRequiredCity(deptType));
                RuntimeTmpText.ApplyDefaultFont(levelText);
            }
            if (effectText != null)
            {
                effectText.text = Loc.Get(LocKeys.DeptLocked);
                RuntimeTmpText.ApplyDefaultFont(effectText);
            }
        }
        else
        {
            if (levelText != null)
            {
                levelText.text = Loc.Format(LocKeys.DeptLevelFormat, level);
                RuntimeTmpText.ApplyDefaultFont(levelText);
            }

            if (effectText != null)
            {
                effectText.text = FormatBonusLine(data.category, data.effectPerLevel);
                RuntimeTmpText.ApplyDefaultFont(effectText);
            }
        }

        UpdateProgressBar(level, maxLevel, locked);
        UpdateUpgradeButton(locked, level, maxLevel);
        UpgradeUiRaycastPolicy.ApplyDepartmentCard(transform, upgradeButton);

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = locked ? 0.72f : 1f;
            _canvasGroup.interactable = true;
        }

        DepartmentMiniCardLayoutBuilder.LogLayoutAudit(this);

        _cachedLevel = level;
        _cachedLocked = locked;
    }

    void UpdateProgressBar(int level, int maxLevel, bool locked)
    {
        EnsureProgressBarWired();
        if (levelProgressBar == null) return;

        levelProgressBar.maxValue = locked || maxLevel <= 0 ? 1f : maxLevel;
        levelProgressBar.value  = locked ? 0f : level;
    }

    void UpdateUpgradeButton(bool locked, int level, int maxLevel)
    {
        if (upgradeButton == null) return;

        bool hasUpgradeUi = upgradeLabelText != null && upgradeCostText != null;
        if (!hasUpgradeUi) return;

        upgradeButton.gameObject.SetActive(true);
        DepartmentMiniCardLayoutBuilder.EnsureUpgradeButtonLayout(upgradeButton.transform as RectTransform);

        if (locked)
        {
            upgradeLabelText.text = Loc.Get(LocKeys.DeptUpgrade);
            upgradeCostText.text = string.Empty;
            upgradeButton.interactable = false;
            var lockedImg = upgradeButton.GetComponent<Image>();
            if (lockedImg != null) HudSkinProvider.ApplyButtonState(lockedImg, HudButtonState.Locked);
            return;
        }

        bool maxed = _personnelUpgrade != null && _upgrades != null && _upgrades.IsMaxLevel(_personnelUpgrade);
        if (maxed)
        {
            upgradeLabelText.text = Loc.Get(LocKeys.DeptMax);
            upgradeCostText.text = string.Empty;
            upgradeButton.interactable = false;
            var img = upgradeButton.GetComponent<Image>();
            if (img != null) HudSkinProvider.ApplyButtonState(img, HudButtonState.Disabled);
            return;
        }

        upgradeLabelText.text = Loc.Get(LocKeys.DeptUpgrade);

        if (_personnelUpgrade == null || _upgrades == null || _studio == null)
        {
            upgradeCostText.text = "—";
            upgradeButton.interactable = false;
            return;
        }

        long cost = _upgrades.GetNextCost(_personnelUpgrade);
        bool studioLocked = (_studioLevel?.Level ?? 1) < _personnelUpgrade.unlockStudioLevel;
        bool cityLocked = _city != null && !_city.IsUpgradeUnlocked(_personnelUpgrade);
        bool canBuy = _upgrades.CanPurchase(_personnelUpgrade, _studio.Money, _studioLevel?.Level ?? 1);

        upgradeCostText.text = AnimatedMoneyText.FormatMoney(cost);
        upgradeButton.interactable = canBuy && !_purchaseInFlight;

        var btnImg = upgradeButton.GetComponent<Image>();
        if (btnImg != null)
            HudSkinProvider.ApplyPurchaseButton(btnImg, canBuy, studioLocked || cityLocked);
    }

    void Update()
    {
        if (_depts == null)
        {
            _depts = GameHub.Instance?.departments;
            if (_depts != null) Refresh();
            return;
        }
        if (GetLevel() != _cachedLevel || IsLocked() != _cachedLocked) Refresh();
    }

    bool IsLocked() => _city != null && !_city.IsDepartmentUnlocked(deptType);

    static string FormatBonusLine(string category, float effectPerLevel)
    {
        float pct = effectPerLevel * 100f;
        return category switch
        {
            "Reducción" => $"-{pct:0.#}% costes",
            "Velocidad" => $"+{pct:0.#}% velocidad",
            _           => $"+{pct:0.#}% ingresos",
        };
    }

    int GetLevel() => deptType switch
    {
        DepartmentType.Editor         => _depts.editor,
        DepartmentType.Director       => _depts.director,
        DepartmentType.Actors         => _depts.actors,
        DepartmentType.Sound          => _depts.sound,
        DepartmentType.Cinematography => _depts.cinematography,
        DepartmentType.Makeup         => _depts.makeup,
        DepartmentType.Costume        => _depts.costume,
        DepartmentType.Art            => _depts.art,
        DepartmentType.Lighting       => _depts.lighting,
        DepartmentType.Grip           => _depts.grip,
        DepartmentType.Producer       => _depts.producer,
        _                             => 0,
    };
}
