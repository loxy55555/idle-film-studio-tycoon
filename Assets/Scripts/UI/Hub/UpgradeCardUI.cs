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

    const float CardLayoutHeight = 200f; // Phase 13.4B — increased for mobile readability

    UpgradeSystem     _upgrades;
    StudioManager     _studio;
    DepartmentSystem  _depts;
    StudioLevelSystem _studioLevel;
    CitySystem        _city;
    bool              _upgradeEventsBound;
    bool              _cityEventsBound;
    bool              _purchaseInFlight;

    void Awake()
    {
        AutoWireReferences();
        EnsurePremiumMaterial();
        EnsureEffectLayout();
        GameHub.OnGameReady += Bind;
        WireBuyButton();
        ReadOnlySlider.Configure(levelBar);
    }

    void OnEnable()
    {
        WireBuyButton();
        EnsurePremiumMaterial();
        if (GameHub.Instance != null)
            Bind();
    }

    void Start()
    {
        if (GameHub.Instance != null)
            Bind();
    }

    void OnDestroy()
    {
        GameHub.OnGameReady -= Bind;
        UnbindUpgradeEvents();
        UnbindCityEvents();
        if (buyButton != null)
            buyButton.onClick.RemoveListener(OnBuyClicked);
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

        WireBuyButton();
        RefreshUI();
    }

    void WireBuyButton()
    {
        AutoWireReferences();
        if (buyButton == null) return;

        buyButton.onClick.RemoveListener(OnBuyClicked);
        buyButton.onClick.AddListener(OnBuyClicked);

        var guard = buyButton.GetComponent<UpgradeBuyButtonGuard>()
                   ?? buyButton.gameObject.AddComponent<UpgradeBuyButtonGuard>();
        guard.Bind(() => upgradeConfig != null ? upgradeConfig.id : string.Empty);

        ApplyInteractionPolicy();
    }

    void ApplyInteractionPolicy()
    {
        UpgradeUiRaycastPolicy.ApplyDepartmentCard(transform, buyButton);
        UpgradeUiRaycastPolicy.EnsureCardHeight(
            GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>(),
            CardLayoutHeight);
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
        ApplyInteractionPolicy();
        DisableProgressBarRaycasts();

        int  level   = _upgrades.GetLevel(upgradeConfig);
        bool maxed   = _upgrades.IsMaxLevel(upgradeConfig);
        long cost    = _upgrades.GetNextCost(upgradeConfig);
        bool canBuy  = _upgrades.CanPurchase(upgradeConfig, _studio.Money, _studioLevel?.Level ?? 1);
        bool studioLocked = (_studioLevel?.Level ?? 1) < upgradeConfig.unlockStudioLevel;
        bool cityLocked   = GameHub.Instance?.city != null && !GameHub.Instance.city.IsUpgradeUnlocked(upgradeConfig);
        bool locked  = studioLocked || cityLocked;

        if (nameText  != null) nameText.text  = upgradeConfig.displayName;
        if (descText  != null) descText.text  = upgradeConfig.description;
        if (levelText != null)
        {
            levelText.text  = maxed ? "MAX" : $"Nivel {level}";
            levelText.color = maxed ? CinematicTheme.GoldBright : CinematicTheme.GoldBase;
        }

        if (effectText != null)
        {
            string current = level > 0 ? BuildEffectString(level) : "Sin efecto";
            string next    = maxed ? "—" : BuildEffectString(Mathf.Max(1, level + 1));
            effectText.text = maxed
                ? $"Actual: {current}"
                : $"Nivel {level} → {Mathf.Max(1, level + 1)}\nActual: {current}\nSiguiente: {next}";
            effectText.color = CinematicTheme.TextSecondary;
        }
        if (costText != null)
        {
            costText.text = maxed  ? "MAX" :
                            locked  ? LockLabel(studioLocked, cityLocked, upgradeConfig) :
                            AnimatedMoneyText.FormatMoney(cost);
            costText.color = maxed  ? CinematicTheme.GoldBright :
                             locked  ? CinematicTheme.TextDim :
                                       CinematicTheme.GoldBase;
        }

        if (badgeImage != null)
        {
            badgeImage.color = Color.clear;
            ApplyUpgradeBadgeIcon();
        }

        if (buyButton != null)
        {
            var state = ResolvePurchaseState(maxed, locked, canBuy);
            buyButton.interactable = state == PurchaseUiState.CanBuy && !_purchaseInFlight;

            var btnImg = buyButton.GetComponent<Image>();
            if (btnImg != null)
                ApplyPurchaseButtonVisual(btnImg, state);

            if (buyButton.GetComponent<UIButtonScale>() == null)
                buyButton.gameObject.AddComponent<UIButtonScale>();

            EnsureBuyButtonLabelVisible(state);
            buyButton.GetComponent<UIButtonScale>()?.ResetScale();
        }

        EnsureInfoTextsVisible();
        EnsureLevelBarLayout();

        if (levelBar != null)
        {
            var smooth = levelBar.GetComponent<SmoothProgressBar>()
                       ?? levelBar.gameObject.AddComponent<SmoothProgressBar>();
            smooth.SetTarget(level, upgradeConfig.maxLevel);
        }
    }

    enum PurchaseUiState { CanBuy, NoMoney, Locked, Maxed }

    static PurchaseUiState ResolvePurchaseState(bool maxed, bool locked, bool canBuy)
    {
        if (maxed) return PurchaseUiState.Maxed;
        if (locked) return PurchaseUiState.Locked;
        if (canBuy) return PurchaseUiState.CanBuy;
        return PurchaseUiState.NoMoney;
    }

    static void ApplyPurchaseButtonVisual(Image btnImg, PurchaseUiState state)
    {
        switch (state)
        {
            case PurchaseUiState.CanBuy:
                HudSkinProvider.ApplyPurchaseButton(btnImg, true, false);
                break;
            case PurchaseUiState.Locked:
                HudSkinProvider.ApplyPurchaseButton(btnImg, false, true);
                break;
            default:
                HudSkinProvider.ApplyPurchaseButton(btnImg, false, false);
                break;
        }
    }

    /// <summary>
    /// Baked Mejoras cards: elevation pass leaves BuyBtn/Lbl disabled in scene (12.4D audit).
    /// Re-enable core button components and keep label above bronze layers.
    /// </summary>
    void EnsureBuyButtonLabelVisible(PurchaseUiState state = PurchaseUiState.CanBuy)
    {
        if (buyButton == null) return;

        buyButton.enabled = true;

        var btnImg = buyButton.GetComponent<Image>();
        if (btnImg != null) btnImg.enabled = true;

        var btnLE = buyButton.GetComponent<LayoutElement>();
        if (btnLE != null) btnLE.enabled = true;

        var btnLbl = buyButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (btnLbl == null) return;

        btnLbl.enabled = true;
        btnLbl.raycastTarget = false;
        btnLbl.text = state switch
        {
            PurchaseUiState.Maxed  => Loc.Get(LocKeys.DeptMax),
            PurchaseUiState.Locked => "BLOQUEADO",
            _                      => Loc.Get(LocKeys.DeptUpgrade),
        };
        btnLbl.transform.SetAsLastSibling();
    }

    /// <summary>Baked Mejoras cards: effect/cost TMPs can stay disabled or alpha-0 after elevation pass.</summary>
    void EnsureInfoTextsVisible()
    {
        EnsureTextVisible(nameText);
        EnsureTextVisible(levelText);
        EnsureTextVisible(effectText, minHeight: 60f, preferredHeight: 68f);
        EnsureTextVisible(costText, minHeight: 28f, preferredHeight: 30f);
    }

    static void EnsureTextVisible(TextMeshProUGUI tmp, float minHeight = 0f, float preferredHeight = 0f)
    {
        if (tmp == null) return;

        tmp.enabled = true;
        tmp.raycastTarget = false;
        var c = tmp.color;
        if (c.a < 0.05f) tmp.color = new Color(c.r, c.g, c.b, 1f);

        ResetLayoutChild(tmp.rectTransform);

        if (minHeight <= 0f && preferredHeight <= 0f) return;

        var le = tmp.GetComponent<LayoutElement>() ?? tmp.gameObject.AddComponent<LayoutElement>();
        le.enabled = true;
        le.ignoreLayout = false;
        if (minHeight > 0f) le.minHeight = minHeight;
        if (preferredHeight > 0f) le.preferredHeight = preferredHeight;

        float targetH = preferredHeight > 0f ? preferredHeight : (minHeight > 0f ? minHeight : tmp.rectTransform.sizeDelta.y);
        if (targetH > 0f)
            tmp.rectTransform.sizeDelta = new Vector2(0f, targetH);
    }

    void EnsureLevelBarLayout()
    {
        if (levelBar == null) return;

        var barRt = levelBar.transform as RectTransform;
        if (barRt == null) return;

        var barLE = barRt.GetComponent<LayoutElement>() ?? barRt.gameObject.AddComponent<LayoutElement>();
        barLE.enabled = true;
        barLE.ignoreLayout = false;
        barLE.flexibleWidth = 1f;
        barLE.minWidth = 0f;
        if (barLE.preferredHeight < 8f) barLE.preferredHeight = 8f;
        if (barLE.minHeight < 8f) barLE.minHeight = 8f;

        ApplyStretchWidthHeight(barRt, barLE.preferredHeight);

        var fillArea = barRt.Find("Fill Area") as RectTransform;
        if (fillArea != null)
        {
            fillArea.anchorMin = Vector2.zero;
            fillArea.anchorMax = Vector2.one;
            fillArea.offsetMin = fillArea.offsetMax = Vector2.zero;
        }
    }

    void DisableProgressBarRaycasts()
    {
        if (levelBar == null) return;
        ReadOnlySlider.Configure(levelBar);
        foreach (var graphic in levelBar.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
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
            var btn = transform.Find("CardMainRow/BuyBtn")
                   ?? transform.Find("BotRow/BuyBtn")
                   ?? transform.Find("BuyBtn");
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

    void EnsurePremiumMaterial()
    {
        var rt = transform as RectTransform;
        if (rt != null)
            CinematicTheme.ApplyPremiumMaterial(rt);

        // Override baked green fill color with premium palette
        if (levelBar != null && levelBar.fillRect != null)
        {
            var fillImg = levelBar.fillRect.GetComponent<Image>();
            if (fillImg != null) fillImg.color = CinematicTheme.ProgressFill;
        }
    }

    void EnsureEffectLayout()
    {
        EnsureBadgeReadability();

        // Font size overrides for mobile readability — Phase 13.4B hierarchy
        if (nameText   != null) { nameText.fontSize   = 30f; nameText.fontStyle   = FontStyles.Bold; }
        if (levelText  != null) { levelText.fontSize  = 26f; levelText.fontStyle  = FontStyles.Bold; }
        if (costText   != null) { costText.fontSize   = 26f; costText.fontStyle   = FontStyles.Bold; }

        if (effectText != null)
        {
            effectText.textWrappingMode = TextWrappingModes.Normal;
            effectText.overflowMode = TextOverflowModes.Overflow;
            effectText.fontSize = 20f;
            effectText.lineSpacing = 2f;
            effectText.maxVisibleLines = 4;

            var effectLE = effectText.GetComponent<LayoutElement>()
                           ?? effectText.gameObject.AddComponent<LayoutElement>();
            effectLE.preferredHeight = 88f;
            effectLE.minHeight = 66f;
        }

        EnsureHorizontalBuyLayout();

        // Buy button — fixed CTA on the right
        if (buyButton != null)
        {
            var btnLE = buyButton.GetComponent<LayoutElement>() ?? buyButton.gameObject.AddComponent<LayoutElement>();
            btnLE.preferredWidth = BuyBtnWidth;
            btnLE.minWidth = BuyBtnWidth;
            btnLE.flexibleWidth = 0f;
            btnLE.preferredHeight = BuyBtnHeight;
            btnLE.minHeight = BuyBtnHeight;
            btnLE.flexibleHeight = 0f;
            var btnLbl = buyButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (btnLbl != null && btnLbl.fontSize < 16f) btnLbl.fontSize = 16f;
        }

        var cardLE = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        cardLE.flexibleWidth = 1f;
        cardLE.minWidth = 0f;
        UpgradeUiRaycastPolicy.EnsureCardHeight(cardLE, CardLayoutHeight);
    }

    void ApplyUpgradeBadgeIcon()
    {
        if (badgeImage == null || upgradeConfig == null) return;

        const float iconSize = 56f;
        var badgeRt = badgeImage.rectTransform;
        var badgeLE = badgeRt.GetComponent<LayoutElement>() ?? badgeRt.gameObject.AddComponent<LayoutElement>();
        badgeLE.preferredWidth = badgeLE.preferredHeight = iconSize;
        badgeLE.minWidth = badgeLE.minHeight = iconSize;

        var icon = UIIconGraphic.EnsureChildIcon(badgeRt, "BadgeIcon", iconSize);
        var iconRt = icon.rectTransform;
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.offsetMin = iconRt.offsetMax = Vector2.zero;

        var sprite = UIIconCatalog.GetUpgradeIcon(upgradeConfig.id);
        UIIconGraphic.Apply(icon, sprite);
    }

    void EnsureBadgeReadability()
    {
        if (badgeImage == null) return;

        badgeImage.color = Color.clear;
        badgeImage.raycastTarget = false;

        var badgeRt = badgeImage.rectTransform;
        var badgeLE = badgeRt.GetComponent<LayoutElement>() ?? badgeRt.gameObject.AddComponent<LayoutElement>();
        badgeLE.preferredWidth = badgeLE.preferredHeight = 56f;
        badgeLE.minWidth = badgeLE.minHeight = 56f;
        badgeLE.flexibleWidth = 0f;

        if (upgradeConfig != null)
            ApplyUpgradeBadgeIcon();
    }

    public const float BuyBtnWidth  = 170f;
    public const float BuyBtnHeight = 44f;

    /// <summary>Phase 12.4B — info left, buy button pinned right (no vertical legacy stack).</summary>
    void EnsureHorizontalBuyLayout()
    {
        AutoWireReferences();
        if (buyButton == null) return;

        var cardMain = transform.Find("CardMainRow") as RectTransform;
        if (cardMain == null)
        {
            var mainGo = new GameObject("CardMainRow", typeof(RectTransform));
            mainGo.transform.SetParent(transform, false);
            cardMain = mainGo.GetComponent<RectTransform>();
        }

        ResetLayoutChild(cardMain);

        var mainLE = cardMain.GetComponent<LayoutElement>() ?? cardMain.gameObject.AddComponent<LayoutElement>();
        mainLE.flexibleWidth = 1f;
        mainLE.minWidth = 0f;
        mainLE.flexibleHeight = 0f;
        mainLE.preferredHeight = CardLayoutHeight - 24f;
        mainLE.minHeight = CardLayoutHeight - 28f;
        ApplyStretchWidthHeight(cardMain, mainLE.preferredHeight);

        var mainHLG = cardMain.GetComponent<HorizontalLayoutGroup>() ?? cardMain.gameObject.AddComponent<HorizontalLayoutGroup>();
        mainHLG.padding = new RectOffset(12, 12, 8, 6);
        mainHLG.spacing = 10;
        mainHLG.childAlignment = TextAnchor.MiddleLeft;
        mainHLG.childControlWidth = mainHLG.childControlHeight = true;
        mainHLG.childForceExpandWidth = false;
        mainHLG.childForceExpandHeight = false;

        var infoColT = cardMain.Find("InfoColumn");
        if (infoColT == null)
        {
            var infoGo = new GameObject("InfoColumn", typeof(RectTransform));
            infoGo.transform.SetParent(cardMain, false);
            infoColT = infoGo.transform;
        }

        ResetLayoutChild(infoColT as RectTransform);

        var infoLE = infoColT.GetComponent<LayoutElement>() ?? infoColT.gameObject.AddComponent<LayoutElement>();
        infoLE.flexibleWidth = 1f;
        infoLE.minWidth = 0f;
        infoLE.flexibleHeight = 1f;
        infoLE.preferredWidth = -1f;
        infoLE.preferredHeight = mainLE.preferredHeight - mainHLG.padding.vertical;

        var infoRt = infoColT as RectTransform;
        ResetLayoutChild(infoRt);
        infoRt.sizeDelta = new Vector2(0f, infoLE.preferredHeight);

        var infoVLG = infoColT.GetComponent<VerticalLayoutGroup>() ?? infoColT.gameObject.AddComponent<VerticalLayoutGroup>();
        infoVLG.spacing = 4;
        infoVLG.childAlignment = TextAnchor.UpperLeft;
        infoVLG.childControlWidth = infoVLG.childControlHeight = true;
        infoVLG.childForceExpandWidth = true;
        infoVLG.childForceExpandHeight = false;

        ReparentIfFound(infoColT, "TopRow");
        ReparentIfFound(infoColT, "EffectText");
        ReparentIfFound(infoColT, "Desc");

        for (int i = 0; i < infoColT.childCount; i++)
            ResetLayoutChild(infoColT.GetChild(i) as RectTransform);

        var botRow = transform.Find("BotRow");
        if (botRow != null)
        {
            var cost = botRow.Find("CostText");
            if (cost != null) cost.SetParent(infoColT, false);
            if (Application.isPlaying) Destroy(botRow.gameObject);
            else DestroyImmediate(botRow.gameObject);
        }

        if (costText != null && costText.transform.parent != infoColT)
            costText.transform.SetParent(infoColT, false);

        buyButton.transform.SetParent(cardMain, false);
        buyButton.transform.SetAsLastSibling();

        var btnLE = buyButton.GetComponent<LayoutElement>() ?? buyButton.gameObject.AddComponent<LayoutElement>();
        btnLE.preferredWidth = BuyBtnWidth;
        btnLE.minWidth = BuyBtnWidth;
        btnLE.flexibleWidth = 0f;
        btnLE.preferredHeight = BuyBtnHeight;
        btnLE.minHeight = BuyBtnHeight;
        btnLE.flexibleHeight = 0f;
        btnLE.layoutPriority = 2;
        btnLE.ignoreLayout = false;

        mainHLG.padding = new RectOffset(12, 12, 8, 6);

        var cardVLG = GetComponent<VerticalLayoutGroup>();
        if (cardVLG != null)
        {
            cardVLG.padding = new RectOffset(0, 0, 4, 4);
            cardVLG.spacing = 4;
            cardVLG.childForceExpandWidth = true;
        }

        cardMain.SetSiblingIndex(0);
        var levelBarT = transform.Find("LevelBar");
        if (levelBarT != null) levelBarT.SetAsLastSibling();

        ApplyCardRootHeight();
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);

        // Baked InfoColumn keeps 100×100 sizeDelta; re-apply after parent width is known.
        if (infoColT is RectTransform infoColRt && cardMain.rect.width > 200f)
        {
            float innerW = cardMain.rect.width - mainHLG.padding.horizontal;
            infoColRt.sizeDelta = new Vector2(Mathf.Max(0f, innerW - BuyBtnWidth - 10f), infoLE.preferredHeight);
            LayoutRebuilder.ForceRebuildLayoutImmediate(infoColRt);
        }
    }

    void ApplyCardRootHeight()
    {
        var cardRt = transform as RectTransform;
        var cardLE = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        UpgradeUiRaycastPolicy.EnsureCardHeight(cardLE, CardLayoutHeight);
        ApplyStretchWidthHeight(cardRt, CardLayoutHeight);
    }

    static void ReparentIfFound(Transform infoCol, string childName)
    {
        var card = infoCol.parent?.parent;
        if (card == null) return;
        var child = card.Find(childName);
        if (child == null || child.parent == infoCol) return;
        child.SetParent(infoCol, false);
    }

    /// <summary>Runtime-created rows used center anchors (100×100), breaking parent layout width.</summary>
    static void ResetLayoutChild(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
    }

    /// <summary>VLG row: full width of card, fixed height from LayoutElement.</summary>
    static void ApplyStretchWidthHeight(RectTransform rt, float height)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, height);
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

    string BuildEffectString(int level)
    {
        if (level <= 0 || upgradeConfig.effects == null || upgradeConfig.effects.Length == 0)
            return "Sin efecto";

        var lines = new System.Collections.Generic.List<string>();
        foreach (var e in upgradeConfig.effects)
        {
            float val = e.valuePerLevel * level;
            string part = UpgradeEffectFormatter.FormatEffect(e.type, val);
            if (!string.IsNullOrEmpty(part))
                lines.Add(part);
        }
        return lines.Count > 0 ? string.Join("\n", lines) : "Sin efecto";
    }

    void OnBuyClicked()
    {
        string id = upgradeConfig != null ? upgradeConfig.id : "null";
        Debug.Log($"[Upgrade] OnClick recibido id={id} inFlight={_purchaseInFlight}");

        if (_purchaseInFlight || upgradeConfig == null)
        {
            Debug.Log($"[Upgrade] OnBuyClicked abort motivo={(_purchaseInFlight ? "purchase_in_flight" : "cfg_null")}");
            return;
        }

        // Interaction gate: verify pointer was pressed AND released on THIS specific button.
        // Prevents ghost purchases from layout-reorder events or rapid scroll interactions.
        if (!UpgradeUiInteractionGate.TryConsumeClick(id))
        {
            Debug.Log($"[Upgrade] OnBuyClicked abort motivo=interaction_gate_rejected id={id}");
            return;
        }

        Debug.Log($"[Upgrade] OnBuyClicked ejecutado id={id}");

        if (_upgrades == null || _studio == null)
        {
            Bind();
            if (_upgrades == null || _studio == null)
            {
                Debug.Log($"[Upgrade] OnBuyClicked abort motivo=hub_null upgrades={_upgrades != null} studio={_studio != null}");
                return;
            }
        }

        long cost = _upgrades.GetNextCost(upgradeConfig);
        bool canBuy = _upgrades.CanPurchase(upgradeConfig, _studio.Money, _studioLevel?.Level ?? 1);
        Debug.Log($"[Upgrade] pre-Purchase id={id} money={_studio.Money} cost={cost} CanPurchase={canBuy} block={_upgrades.GetPurchaseBlockReason(upgradeConfig, _studio.Money, _studioLevel?.Level ?? 1)}");

        _purchaseInFlight = true;
        if (buyButton != null)
        {
            buyButton.interactable = false;
            buyButton.GetComponent<UIButtonScale>()?.ResetScale();
        }

        try
        {
            if (!_upgrades.Purchase(upgradeConfig, _studio, _depts, _studioLevel?.Level ?? 1))
            {
                Debug.Log($"[Upgrade] OnBuyClicked Purchase()=false id={id}");
                UpgradeUiInteractionGate.ClearPointerState();
                return;
            }

            Debug.Log($"[Upgrade] OnBuyClicked Purchase()=true id={id} newLevel={_upgrades.GetLevel(upgradeConfig)}");

            int newLevel = _upgrades.GetLevel(upgradeConfig);
            GameFeelUI.Instance?.ShowUpgradePurchase(transform as RectTransform, upgradeConfig, newLevel);
            buyButton?.GetComponent<UIButtonScale>()?.ResetScale();
            RefreshUI();
        }
        finally
        {
            _purchaseInFlight = false;
            buyButton?.GetComponent<UIButtonScale>()?.ResetScale();
            RefreshUI();
        }
    }
}

/// <summary>Shared upgrade effect labels for cards and game-feel popups.</summary>
public static class UpgradeEffectFormatter
{
    public static string FormatLevelEffects(UpgradeConfig cfg, int level)
    {
        if (cfg == null || level <= 0 || cfg.effects == null || cfg.effects.Length == 0)
            return "";

        var lines = new System.Collections.Generic.List<string>();
        foreach (var e in cfg.effects)
        {
            float val = e.valuePerLevel * level;
            string part = FormatEffect(e.type, val);
            if (!string.IsNullOrEmpty(part))
                lines.Add(part);
        }
        return lines.Count > 0 ? string.Join("\n", lines) : "";
    }

    public static string FormatEffect(UpgradeEffectType type, float val)
    {
        switch (type)
        {
            case UpgradeEffectType.Quality:
                return $"+{val * 100f:0.#}% Calidad";
            case UpgradeEffectType.Speed:
                return $"+{val * 100f:0.#}% Velocidad";
            case UpgradeEffectType.CostReduction:
                return $"-{val * 100f:0.#}% Coste";
            case UpgradeEffectType.ReputationBonus:
                return $"+{val * 100f:0.#}% Reputación";
            case UpgradeEffectType.PassiveIncomeBonus:
                return $"+{AnimatedMoneyText.FormatMoney((long)val)}/s Ingresos";
            case UpgradeEffectType.MaxMovieSlots:
                return val == 1f ? "+1 Slot Producción" : $"+{(int)val} Slots Producción";
            case UpgradeEffectType.XPBonus:
                return $"+{val * 100f:0.#}% XP";
            case UpgradeEffectType.UnlockMovieTier:
                return val >= 1f ? $"Catálogo Nv.{Mathf.RoundToInt(val)}" : "";
            default:
                return "";
        }
    }
}
