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

    static readonly Color DIAMOND_COLOR = new Color(0.45f, 0.82f, 1f);  // diamond blue — intentional

    private AnimatedMoneyText _animatedMoney;
    private AnimatedValueText _animatedRep;
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

        if (reputationText != null)
            _animatedRep = reputationText.GetComponent<AnimatedValueText>()
                        ?? reputationText.gameObject.AddComponent<AnimatedValueText>();

        if (oscarsText != null)
            _oscarRT = oscarsText.GetComponent<RectTransform>();

        if (diamondsText != null)
            _diamondRT = diamondsText.GetComponent<RectTransform>();

        GameHub.OnGameReady += BindDiamonds;
        GameHub.OnGameReady += BindCity;
        if (GameHub.Instance != null) BindDiamonds();
        if (GameHub.Instance != null) BindCity();
        TrySubscribe();
        PatchSettingsIcon();
        PatchTopBarVisuals();
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
        if (levelText != null)     levelText.color     = CinematicTheme.GoldBright;
        if (oscarsText != null)    oscarsText.color    = CinematicTheme.GoldBase;
        if (reputationText != null) reputationText.color = CinematicTheme.SilverBase;
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

        var label = RuntimeTmpText.Create(block.transform, "DIAM", 11, CinematicTheme.TextDim,
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

        cityText = RuntimeTmpText.Create(statsBlock, "I1", 18, CinematicTheme.GoldBright, FontStyles.Bold,
            TextAlignmentOptions.Center, "CityText");
        cityText.gameObject.AddComponent<LayoutElement>().preferredWidth = 96f;
    }

    void UpdateCity(int level)
    {
        if (cityText == null) return;
        cityText.text = "I" + level;
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
        if (!_topBarPatched) PatchTopBarVisuals();
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
        if (_animatedRep != null)
        {
            _animatedRep.Mode = AnimatedValueText.FormatMode.OneDecimal;
            _animatedRep.Prefix = "";
            _animatedRep.Suffix = "";
            _animatedRep.SetValue(rep);
        }
        else if (reputationText != null)
            reputationText.text = FormatReputation(rep);
    }

    /// <summary>Whole numbers omit decimals; fractional rep shows one decimal (Phase 7.3).</summary>
    public static string FormatReputation(float rep)
    {
        float rounded = Mathf.Round(rep * 10f) / 10f;
        if (Mathf.Approximately(rounded, Mathf.Round(rounded)))
            return ((int)Mathf.Round(rounded)).ToString();
        return rounded.ToString("0.0");
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
            oscarsText.text = P.oscars.ToString("N0");

        if (_city != null && cityText != null)
            UpdateCity(_city.Level);

        if (SL != null && levelText != null && levelText.gameObject.activeInHierarchy)
            levelText.text = "Nv." + SL.Level;
    }

    public RectTransform GetOscarRect() => _oscarRT;

    // ── Phase 12.2 — TopBar visual patch ──────────────────────────────────────

    bool _topBarPatched;

    /// <summary>Phase 13.4C — icon-left / value-right rows, dark panels, balanced width.</summary>
    public void PatchTopBarVisuals()
    {
        if (_topBarPatched) return;
        _topBarPatched = true;

        AutoWireMoneyTexts();
        AutoWireStatsTexts();
        AutoWireDiamondsText();

        // cityText and levelText are rendered inside the stats area — keep hidden per layout
        if (cityText != null)  cityText.gameObject.SetActive(false);
        if (levelText != null) levelText.gameObject.SetActive(false);

        RestructureTopBarLayout();
        AddDarkPanelBg("MoneyBlock");
        AddDarkPanelBg("DiamondsBlock");

        if (moneyText != null)
        {
            moneyText.fontSize = 40f;
            moneyText.fontStyle = FontStyles.Bold;
            moneyText.color = CinematicTheme.TextPrimary;
            moneyText.alignment = TextAlignmentOptions.MidlineLeft;
        }
        if (incomeText != null)
        {
            incomeText.color = CinematicTheme.GoldBase;
            incomeText.fontSize = 21f;
            incomeText.fontStyle = FontStyles.Bold;
            incomeText.alignment = TextAlignmentOptions.MidlineLeft;
            incomeText.gameObject.SetActive(true);
        }
        if (reputationText != null)
        {
            reputationText.color = CinematicTheme.TextPrimary;
            reputationText.fontSize = 26f;
            reputationText.fontStyle = FontStyles.Bold;
            reputationText.alignment = TextAlignmentOptions.MidlineLeft;
        }
        if (oscarsText != null)
        {
            oscarsText.color = CinematicTheme.GoldBright;
            oscarsText.fontSize = 26f;
            oscarsText.fontStyle = FontStyles.Bold;
            oscarsText.alignment = TextAlignmentOptions.MidlineLeft;
        }
        if (diamondsText != null)
        {
            diamondsText.color = DIAMOND_COLOR;
            diamondsText.fontSize = 34f;
            diamondsText.fontStyle = FontStyles.Bold;
            diamondsText.alignment = TextAlignmentOptions.MidlineLeft;
            diamondsText.gameObject.SetActive(true);
        }
    }

    void RestructureTopBarLayout()
    {
        var barHLG = GetComponent<HorizontalLayoutGroup>();
        if (barHLG != null)
        {
            barHLG.childAlignment = TextAnchor.MiddleLeft;
            barHLG.spacing = 10;
            barHLG.padding = new RectOffset(8, 108, 4, 4);
        }

        RestructureMoneyBlock();
        EnsureDiamondsStatBlock();
        RestructureStatsBlock();

        // Collapse the spacer so money block flows directly into stats block
        var spacer = transform.Find("TopBarSpacer");
        if (spacer != null)
        {
            var le = spacer.GetComponent<LayoutElement>() ?? spacer.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 0f;
            le.minWidth = 0f;
            le.flexibleWidth = 0f;
            le.ignoreLayout = true;
        }
    }

    void RestructureMoneyBlock()
    {
        var moneyBlock = transform.Find("MoneyBlock");
        if (moneyBlock == null) return;

        // Fixed preferred width so the block stays compact and leaves room for stats
        var blockLE = moneyBlock.GetComponent<LayoutElement>() ?? moneyBlock.gameObject.AddComponent<LayoutElement>();
        blockLE.preferredWidth = 300f;
        blockLE.minWidth = 220f;
        blockLE.flexibleWidth = 0f;

        // Reconfigure VLG (don't destroy — children would be orphaned)
        var blockVLG = moneyBlock.GetComponent<VerticalLayoutGroup>() ?? moneyBlock.gameObject.AddComponent<VerticalLayoutGroup>();
        blockVLG.childAlignment = TextAnchor.MiddleLeft;
        blockVLG.spacing = 0;
        blockVLG.childControlWidth = blockVLG.childControlHeight = true;
        blockVLG.childForceExpandWidth = true;   // ← makes __IconValueRow fill the full width
        blockVLG.childForceExpandHeight = false;
        blockVLG.padding = new RectOffset(10, 8, 6, 6);

        // Remove any legacy MoneyRow wrapper (children rescued first)
        var oldMoneyRow = moneyBlock.Find("MoneyRow");
        if (oldMoneyRow != null)
        {
            while (oldMoneyRow.childCount > 0)
                oldMoneyRow.GetChild(0).SetParent(moneyBlock, false);
            Object.DestroyImmediate(oldMoneyRow.gameObject);
        }

        // __IconValueRow goes directly under moneyBlock; VLG stretches it to full width
        SetupIconValueRow(moneyBlock, moneyText != null ? moneyText.transform : null,
            "MoneyIcon", UIIconCatalog.GetResourceMoney(), 56f);

        var iconRow = moneyBlock.Find("__IconValueRow");
        if (iconRow != null) iconRow.SetAsFirstSibling();

        // IncomeText directly under moneyBlock, second child (VLG stacks below icon row)
        if (incomeText != null)
        {
            incomeText.transform.SetParent(moneyBlock, false);
            incomeText.transform.SetAsLastSibling();
            var incLE = incomeText.GetComponent<LayoutElement>() ?? incomeText.gameObject.AddComponent<LayoutElement>();
            incLE.preferredHeight = 22f;
            incLE.minHeight = 18f;
            incLE.flexibleWidth = 1f;
            incomeText.textWrappingMode = TextWrappingModes.NoWrap;
            incomeText.overflowMode = TMPro.TextOverflowModes.Overflow;
        }
    }

    void EnsureDiamondsStatBlock()
    {
        var statsBlock = transform.Find("StatsBlock");
        if (statsBlock == null) return;

        // Resolve diamondsBlock: prefer the block holding the already-wired diamondsText
        Transform diamondsBlock = null;
        if (diamondsText != null)
        {
            // Walk up until we find a direct child of statsBlock
            var t = diamondsText.transform;
            while (t != null && t.parent != statsBlock) t = t.parent;
            if (t != null && t.parent == statsBlock) diamondsBlock = t;
        }

        // Fallback: look for a named "DiamondsBlock" child or any child that has DiamondsText anywhere inside
        if (diamondsBlock == null)
        {
            diamondsBlock = statsBlock.Find("DiamondsBlock");
            if (diamondsBlock == null)
            {
                foreach (Transform child in statsBlock)
                {
                    if (child.GetComponentInChildren<TextMeshProUGUI>(true)?.name == "DiamondsText")
                    { diamondsBlock = child; break; }
                }
            }
        }

        if (diamondsBlock == null)
        {
            var block = new GameObject("DiamondsBlock", typeof(RectTransform));
            block.transform.SetParent(statsBlock, false);
            diamondsBlock = block.transform;
        }

        // Remove stale label
        var label = diamondsBlock.Find("DiamondsLabel");
        if (label != null) Object.DestroyImmediate(label.gameObject);

        // Ensure diamondsText is resolved (may already be wired)
        if (diamondsText == null)
        {
            var existing = diamondsBlock.GetComponentInChildren<TextMeshProUGUI>(true);
            diamondsText = (existing != null && existing.name == "DiamondsText")
                ? existing
                : RuntimeTmpText.Create(diamondsBlock, "0", 24f, DIAMOND_COLOR, FontStyles.Bold,
                    TextAlignmentOptions.MidlineLeft, "DiamondsText");
        }

        SetupIconValueRow(diamondsBlock, diamondsText.transform,
            "DiamondIcon", UIIconCatalog.GetResourceDiamonds(), 48f);

        var le = diamondsBlock.GetComponent<LayoutElement>() ?? diamondsBlock.gameObject.AddComponent<LayoutElement>();
        le.minWidth = 130f;
        le.preferredWidth = 150f;
        le.flexibleWidth = 0.8f;

        // Dark panel background matching reference
        var blockImg = diamondsBlock.GetComponent<Image>() ?? diamondsBlock.gameObject.AddComponent<Image>();
        blockImg.color = new Color(0.08f, 0.10f, 0.14f, 0.92f);
        blockImg.raycastTarget = false;

        diamondsBlock.gameObject.SetActive(true);
        UpdateDiamonds(_diamonds != null ? _diamonds.Balance : 0);
    }

    void RestructureStatsBlock()
    {
        var statsBlock = transform.Find("StatsBlock");
        if (statsBlock == null) return;

        var hlg = statsBlock.GetComponent<HorizontalLayoutGroup>() ?? statsBlock.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.spacing = 8;
        hlg.padding = new RectOffset(4, 4, 0, 0);
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;   // distribute space evenly across stat blocks
        hlg.childForceExpandHeight = false;

        var statsLE = statsBlock.GetComponent<LayoutElement>() ?? statsBlock.gameObject.AddComponent<LayoutElement>();
        statsLE.flexibleWidth = 1f;
        statsLE.minWidth = 200f;
        statsLE.preferredWidth = -1f;

        foreach (Transform child in statsBlock)
        {
            if (!child.gameObject.activeInHierarchy) continue;
            if (child.name == "CityTextBlock") continue;

            // Each stat block needs a VLG so childForceExpandWidth from StatsBlock HLG
            // is passed through to __IconValueRow inside each block
            var childVLG = child.GetComponent<VerticalLayoutGroup>() ?? child.gameObject.AddComponent<VerticalLayoutGroup>();
            childVLG.childAlignment = TextAnchor.MiddleLeft;
            childVLG.childControlWidth = childVLG.childControlHeight = true;
            childVLG.childForceExpandWidth = true;
            childVLG.childForceExpandHeight = false;
            childVLG.padding = new RectOffset(8, 8, 2, 2);
            childVLG.spacing = 0;

            var repTxt = child.GetComponentInChildren<TextMeshProUGUI>(true);
            if (repTxt == null) continue;

            if (repTxt.name == "RepText")
                SetupIconValueRow(child, repTxt.transform, "RepIcon", UIIconCatalog.GetResourceReputation(), 48f);
            else if (repTxt.name == "OscarsText")
                SetupIconValueRow(child, repTxt.transform, "StarIcon", UIIconCatalog.GetResourceGoldStar(), 48f);
            else if (repTxt.name == "DiamondsText")
                SetupIconValueRow(child, repTxt.transform, "DiamondIcon", UIIconCatalog.GetResourceDiamonds(), 48f);
        }
    }

    static void SetupIconValueRow(Transform block, Transform valueTransform, string iconName, Sprite sprite, float iconSize)
    {
        if (block == null || block.gameObject == null || valueTransform == null) return;

        var blockLE = block.GetComponent<LayoutElement>() ?? block.gameObject.AddComponent<LayoutElement>();
        blockLE.minWidth = Mathf.Max(blockLE.minWidth, iconSize + 36f);

        var row = block.Find("__IconValueRow");
        if (row == null)
        {
            var rowGo = new GameObject("__IconValueRow", typeof(RectTransform));
            rowGo.transform.SetParent(block, false);
            row = rowGo.transform;
        }

        var rowLE = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
        rowLE.preferredHeight = iconSize;
        rowLE.minHeight = iconSize;
        rowLE.flexibleWidth = 1f;

        var rowHLG = row.GetComponent<HorizontalLayoutGroup>() ?? row.gameObject.AddComponent<HorizontalLayoutGroup>();
        if (rowHLG == null) return;

        rowHLG.childAlignment = TextAnchor.MiddleLeft;
        rowHLG.spacing = 6;
        rowHLG.padding = new RectOffset(0, 0, 0, 0);
        rowHLG.childControlWidth = rowHLG.childControlHeight = true;
        rowHLG.childForceExpandWidth = false;
        rowHLG.childForceExpandHeight = false;

        valueTransform.SetParent(row, false);

        if (sprite != null)
        {
            var icon = row.Find(iconName)?.GetComponent<Image>()
                       ?? UIIconGraphic.EnsureChildIcon(row, iconName, iconSize);
            if (icon != null)
            {
                var iconLE = icon.GetComponent<LayoutElement>() ?? icon.gameObject.AddComponent<LayoutElement>();
                iconLE.preferredWidth = iconLE.preferredHeight = iconSize;
                iconLE.minWidth = iconLE.minHeight = iconSize;
                iconLE.flexibleWidth = 0f;
                icon.transform.SetAsFirstSibling();
                UIIconGraphic.Apply(icon, sprite);
            }
        }

        valueTransform.SetAsLastSibling();
        var tmp = valueTransform.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TMPro.TextOverflowModes.Overflow;
        }
        var valueLE = valueTransform.GetComponent<LayoutElement>() ?? valueTransform.gameObject.AddComponent<LayoutElement>();
        valueLE.flexibleWidth = 1f;
        valueLE.minWidth = 40f;
        valueLE.preferredWidth = -1f;
    }

    /// <summary>Phase 13.4C — add/update a subtle dark rounded panel behind a block to match reference.</summary>
    void AddDarkPanelBg(string blockName)
    {
        var block = transform.Find(blockName);
        if (block == null)
        {
            var statsBlock = transform.Find("StatsBlock");
            if (statsBlock != null) block = statsBlock.Find(blockName);
        }
        if (block == null) return;

        // Remove any existing legacy blue bg
        var legacyBg = block.Find("__ResourceBg");
        if (legacyBg != null)
        {
            var old = legacyBg.GetComponent<Image>();
            if (old != null) old.color = Color.clear;
        }

        var blockImg = block.GetComponent<Image>() ?? block.gameObject.AddComponent<Image>();
        blockImg.color = new Color(0.08f, 0.10f, 0.14f, 0.92f);   // dark navy panel
        blockImg.raycastTarget = false;
    }

    // Phase 13.3D: Remove any previously created __ResourceBg panel that shows as a visible blue box.
    void ClearResourceBlockBg(string blockName)
    {
        var block = transform.Find(blockName);
        if (block == null)
        {
            var statsBlock = transform.Find("StatsBlock");
            if (statsBlock != null) block = statsBlock.Find(blockName);
        }
        if (block == null) return;

        var existingBg = block.Find("__ResourceBg");
        if (existingBg != null)
        {
            var img = existingBg.GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.color = Color.clear;
        }

        // Also clear any baked Image on the block itself that may be a legacy background
        var blockImg = block.GetComponent<UnityEngine.UI.Image>();
        if (blockImg != null && CinematicTheme.IsLegacyGreen(blockImg.color))
            blockImg.color = Color.clear;
    }

    /// <summary>Phase 10.1 — settings button pinned to top-right of the bar.</summary>
    public void PatchSettingsIcon()
    {
        var bar = transform as RectTransform;
        if (bar == null) return;

        Button settingsBtn = null;
        foreach (var btn in GetComponentsInChildren<Button>(true))
        {
            var btnName = btn.gameObject.name.ToUpperInvariant();
            if (btnName.Contains("SETTING") || btnName.Contains("MENU") || btnName.Contains("HAMB") || btnName == "SETTINGSBTN")
            {
                settingsBtn = btn;
                break;
            }
        }

        if (settingsBtn == null) return;

        var btnRT = settingsBtn.transform as RectTransform;
        btnRT.SetAsLastSibling();

        var le = btnRT.GetComponent<LayoutElement>() ?? btnRT.gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = true;

        const float size = 104f;
        const float margin = 12f;
        btnRT.anchorMin = new Vector2(1f, 1f);
        btnRT.anchorMax = new Vector2(1f, 1f);
        btnRT.pivot     = new Vector2(1f, 1f);
        btnRT.sizeDelta = new Vector2(size, size);
        btnRT.anchoredPosition = new Vector2(-margin, -margin);

        var tmp = settingsBtn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.text = "⚙";
            tmp.fontSize = 40f;
            tmp.alignment = TextAlignmentOptions.Center;
        }
    }
}
