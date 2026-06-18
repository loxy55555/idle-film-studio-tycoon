using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Compact studio summary — Phase 10.0.
/// 90 px strip below hero: Level · REP · XP · progress · next objective.
/// </summary>
public class BonificationsBarUI : MonoBehaviour
{
    const string CompactMarker = "SummaryCompact_v10_2";

    TextMeshProUGUI _levelNumText;
    TextMeshProUGUI _repLineText;
    TextMeshProUGUI _xpLineText;
    TextMeshProUGUI _bonusSummaryText;
    TextMeshProUGUI _objectiveText;
    Slider          _levelXpBar;

    AnimatedValueText _repAnimator;
    AnimatedValueText _xpAnimator;
    SmoothProgressBar _xpBarAnimator;
    int _lastLevel = -1;

    bool  _subscribed;
    bool  _citySubscribed;
    float _nextPoll;

    static readonly Color C_POSITIVE  = CinematicTheme.GoldBase;
    static readonly Color C_NEUTRAL   = CinematicTheme.TextSecondary;
    static readonly Color C_GOLD      = CinematicTheme.GoldBright;
    static readonly Color BG_BAR      = CinematicTheme.DeepBg;
    static readonly Color BG_CELL     = CinematicTheme.CardBg;
    static readonly Color BG_BADGE    = CinematicTheme.DeepBg;
    static readonly Color BG_INFO     = CinematicTheme.PanelBg;
    static readonly Color TEXT_DIM    = CinematicTheme.TextDim;
    static readonly Color ACCENT_LINE = new Color(CinematicTheme.GoldBase.r, CinematicTheme.GoldBase.g, CinematicTheme.GoldBase.b, 0.90f);
    static readonly Color ACCENT_DIM  = new Color(CinematicTheme.GoldBase.r, CinematicTheme.GoldBase.g, CinematicTheme.GoldBase.b, 0.22f);
    static readonly Color BAR_BG      = CinematicTheme.DeepBg;
    static readonly Color BAR_FILL    = CinematicTheme.GoldBase;

    void Awake()
    {
        GameHub.OnGameReady += OnGameReady;
    }

    void Start()
    {
        // Start() fires after all AfterSceneLoad bootstrap patches and other Start() calls.
        // For a component that starts inactive (EstudioPanel is inactive at scene load),
        // this is the earliest reliable moment all systems are ready and children stable.
        AutoWire();
        EnsureBuilt();
        Refresh();
    }

    void OnEnable()
    {
        AutoWire();
        EnsureBuilt();
        OnGameReady();
    }

    void Update()
    {
        if (Time.unscaledTime >= _nextPoll)
        {
            _nextPoll = Time.unscaledTime + 1f;
            Refresh();
        }
    }

    void OnDestroy()
    {
        GameHub.OnGameReady -= OnGameReady;
        UnbindCity();
        if (_subscribed && GameHub.Instance != null)
        {
            if (GameHub.Instance.studio      != null) GameHub.Instance.studio.OnProductionsChanged -= Refresh;
            if (GameHub.Instance.upgrades    != null) GameHub.Instance.upgrades.OnUpgradePurchased -= Refresh;
            if (GameHub.Instance.studioLevel != null) GameHub.Instance.studioLevel.OnLevelUp       -= OnLevelUp;
        }
    }

    void OnGameReady()
    {
        var hub = GameHub.Instance;
        if (!_subscribed && hub != null)
        {
            if (hub.studio      != null) hub.studio.OnProductionsChanged += Refresh;
            if (hub.upgrades    != null) hub.upgrades.OnUpgradePurchased += Refresh;
            if (hub.studioLevel != null) hub.studioLevel.OnLevelUp       += OnLevelUp;
            _subscribed = hub.studio != null;
        }
        Refresh();
        RefreshStudioHeader();
        BindCity();
    }

    void BindCity()
    {
        UnbindCity();
        var city = GameHub.Instance?.city;
        if (city == null) return;
        city.OnCityLevelChanged += OnCityChanged;
        _citySubscribed = true;
    }

    void UnbindCity()
    {
        var city = GameHub.Instance?.city;
        if (city != null && _citySubscribed)
            city.OnCityLevelChanged -= OnCityChanged;
        _citySubscribed = false;
    }

    void OnCityChanged(int _) => RefreshStudioHeader();

    void OnLevelUp(int _) => Refresh();

    // ─────────────────────────────────────────────────────────────
    //  Construction
    // ─────────────────────────────────────────────────────────────

    void EnsureBuilt()
    {
        // Always remove any baked HLG and ensure VLG is correctly configured — even when
        // returning early below. The scene may have stale baked components from a previous
        // edit-mode call that left childControlWidth=false.
        var barHLG = GetComponent<HorizontalLayoutGroup>();
        if (barHLG != null)
        {
            barHLG.enabled = false;
            if (Application.isPlaying) Destroy(barHLG);
            else DestroyImmediate(barHLG);
        }

        var barVLG = GetComponent<VerticalLayoutGroup>() ?? gameObject.AddComponent<VerticalLayoutGroup>();
        barVLG.childAlignment        = TextAnchor.UpperLeft;
        barVLG.childControlWidth     = true;   // must be true: VLG sets sizeDelta directly, not via anchors
        barVLG.childControlHeight    = true;
        barVLG.childForceExpandWidth = true;
        barVLG.childForceExpandHeight = true;
        barVLG.spacing  = 0;
        barVLG.padding  = new RectOffset(0, 0, 0, 0);

        if (transform.Find(CompactMarker) != null && FindTmp("BonusSummary") != null) return;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var ch = transform.GetChild(i);
            ch.SetParent(null, false);   // remove from hierarchy immediately so layout sees 0 children
            if (Application.isPlaying) Destroy(ch.gameObject);
            else DestroyImmediate(ch.gameObject);
        }

        var bg = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        bg.color = BG_BAR;
        bg.raycastTarget = false;

        var le = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 118f;
        le.minHeight       = 118f;
        le.flexibleHeight  = 0f;

        var marker = new GameObject(CompactMarker, typeof(RectTransform));
        marker.transform.SetParent(transform, false);

        var row = new GameObject("SummaryRow", typeof(RectTransform), typeof(Image));
        row.transform.SetParent(transform, false);
        row.GetComponent<Image>().color = BG_INFO;
        row.GetComponent<Image>().raycastTarget = false;
        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 112f;
        rowLE.flexibleWidth = 1f;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(12, 12, 8, 8);
        hlg.spacing = 10;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        // Level badge — wider for mobile readability
        var badge = new GameObject("LevelBadge", typeof(RectTransform), typeof(Image));
        badge.transform.SetParent(row.transform, false);
        badge.GetComponent<Image>().color = BG_BADGE;
        badge.GetComponent<Image>().raycastTarget = false;
        badge.AddComponent<LayoutElement>().preferredWidth = 80f;
        _levelNumText = RuntimeTmpText.Create(badge.transform, "1",
            38f, ACCENT_LINE, FontStyles.Bold, TextAlignmentOptions.Center, "LevelNum");
        _levelNumText.raycastTarget = false;

        // Info column
        var info = new GameObject("InfoCol", typeof(RectTransform));
        info.transform.SetParent(row.transform, false);
        info.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var vlg = info.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var meta = new GameObject("MetaRow", typeof(RectTransform));
        meta.transform.SetParent(info.transform, false);
        meta.AddComponent<LayoutElement>().preferredHeight = 28f;
        var metaHLG = meta.AddComponent<HorizontalLayoutGroup>();
        metaHLG.spacing = 8;
        metaHLG.childAlignment = TextAnchor.MiddleLeft;
        metaHLG.childControlWidth = metaHLG.childControlHeight = true;
        metaHLG.childForceExpandWidth = false;
        metaHLG.childForceExpandHeight = true;

        _repLineText = RuntimeTmpText.Create(meta.transform, Loc.Format(LocKeys.StudioRepBarFmt, 0f),
            16f, C_GOLD, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "RepLine");
        _repLineText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        _xpLineText = RuntimeTmpText.Create(meta.transform, Loc.Format(LocKeys.StudioXPFormat, 0, 0),
            18f, TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, "XpLine");
        _xpLineText.gameObject.AddComponent<LayoutElement>().preferredWidth = 140f;

        var xpBarGo = new GameObject("XpBar", typeof(RectTransform), typeof(Image), typeof(Slider));
        xpBarGo.transform.SetParent(info.transform, false);
        xpBarGo.GetComponent<Image>().color = BAR_BG;
        xpBarGo.AddComponent<LayoutElement>().preferredHeight = 10f;
        _levelXpBar = xpBarGo.GetComponent<Slider>();
        _levelXpBar.minValue = 0f;
        _levelXpBar.maxValue = 1f;
        _levelXpBar.interactable = false;
        var fa = new GameObject("Fill Area", typeof(RectTransform));
        fa.transform.SetParent(xpBarGo.transform, false);
        StretchRT(fa.GetComponent<RectTransform>());
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fa.transform, false);
        fill.GetComponent<Image>().color = BAR_FILL;
        StretchRT(fill.GetComponent<RectTransform>());
        _levelXpBar.fillRect = fill.GetComponent<RectTransform>();
        ReadOnlySlider.Configure(_levelXpBar);

        _bonusSummaryText = RuntimeTmpText.Create(info.transform, Loc.Format(LocKeys.StudioBonusSummaryFmt, 0, 0, 0),
            18f, CinematicTheme.GoldBase, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "BonusSummary");
        _bonusSummaryText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        EnsureAnimatedStats(info.transform);

        _objectiveText = RuntimeTmpText.Create(info.transform, Loc.Get(LocKeys.StudioNextLevel),
            18f, TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, "Objective");
        _objectiveText.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
    }

    void EnsureAnimatedStats(Transform infoRoot)
    {
        if (_repLineText != null && _repAnimator == null)
        {
            _repAnimator = _repLineText.gameObject.GetComponent<AnimatedValueText>()
                        ?? _repLineText.gameObject.AddComponent<AnimatedValueText>();
        }

        if (_xpLineText != null && _xpAnimator == null)
        {
            _xpAnimator = _xpLineText.gameObject.GetComponent<AnimatedValueText>()
                       ?? _xpLineText.gameObject.AddComponent<AnimatedValueText>();
        }

        if (_levelXpBar != null && _xpBarAnimator == null)
            _xpBarAnimator = _levelXpBar.GetComponent<SmoothProgressBar>()
                          ?? _levelXpBar.gameObject.AddComponent<SmoothProgressBar>();
    }

    static void StretchRT(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // ─────────────────────────────────────────────────────────────
    //  Auto-wire (after baked-scene rebuild)
    // ─────────────────────────────────────────────────────────────

    public void AutoWire()
    {
        _levelNumText  = FindTmp("LevelNum")   ?? _levelNumText;
        _repLineText   = FindTmp("RepLine")    ?? _repLineText;
        _xpLineText    = FindTmp("XpLine")     ?? _xpLineText;
        _bonusSummaryText = FindTmp("BonusSummary") ?? _bonusSummaryText;
        _objectiveText = FindTmp("Objective")  ?? _objectiveText;

        if (_levelXpBar == null)
        {
            foreach (var s in GetComponentsInChildren<Slider>(true))
                if (s.name == "XpBar") { _levelXpBar = s; break; }
        }

        EnsureAnimatedStats(transform);
    }

    TextMeshProUGUI FindTmp(string n)
    {
        foreach (var t in GetComponentsInChildren<TextMeshProUGUI>(true))
            if (t.name == n) return t;
        return null;
    }

    // ─────────────────────────────────────────────────────────────
    //  Refresh
    // ─────────────────────────────────────────────────────────────

    public void Refresh()
    {
        if (_levelNumText == null) AutoWire();

        var hub = GameHub.Instance;
        if (hub == null) return;

        var SL = hub.studioLevel;
        var ST = hub.studio;

        int stuLevel = SL != null ? SL.Level : 1;
        if (_levelNumText != null)
        {
            if (_lastLevel >= 0 && stuLevel > _lastLevel)
                UIAnimationService.PlayUpgradeFeedback(_levelNumText.rectTransform, null, _levelNumText);
            _levelNumText.text = stuLevel.ToString();
            _lastLevel = stuLevel;
        }

        float rep = ST != null ? ST.reputation : 0f;
        if (_repLineText != null)
        {
            if (_repAnimator != null)
            {
                _repAnimator.Mode = AnimatedValueText.FormatMode.OneDecimal;
                _repAnimator.Prefix = "";
                _repAnimator.Suffix = " " + Loc.Get(LocKeys.RepSuffix);
                _repAnimator.SetValue(rep);
            }
            else
                _repLineText.text = Loc.Format(LocKeys.StudioRepBarFmt, rep);
        }

        if (SL != null)
        {
            if (_xpLineText != null)
            {
                if (_xpAnimator != null)
                {
                    _xpAnimator.Mode = AnimatedValueText.FormatMode.CustomPrefixSuffix;
                    _xpAnimator.Prefix = "";
                    _xpAnimator.Suffix = " / " + ((int)SL.XPToNext).ToString() + " " + Loc.Get(LocKeys.XPSuffix);
                    _xpAnimator.SetValue(SL.XP);
                }
                else
                    _xpLineText.text = Loc.Format(LocKeys.StudioXPFormat, (int)SL.XP, (int)SL.XPToNext);
            }
            if (_objectiveText != null)
                _objectiveText.text = string.Format(Loc.Get(LocKeys.StudioNextLevel), Mathf.Max(0f, SL.XPToNext - SL.XP));
            if (_xpBarAnimator != null)
                _xpBarAnimator.SetNormalized(SL.XPToNext > 0f ? Mathf.Clamp01(SL.XP / SL.XPToNext) : 0f);
            else if (_levelXpBar != null)
                _levelXpBar.value = SL.XPToNext > 0f ? Mathf.Clamp01(SL.XP / SL.XPToNext) : 0f;
        }

        if (_bonusSummaryText != null)
            _bonusSummaryText.text = StudioBonusSummary.FormatAccumulatedLine();

        RefreshStudioHeader();
    }

    void RefreshStudioHeader()
    {
        var hub = GameHub.Instance;
        if (hub == null) return;

        var estudio = GameObject.Find("EstudioPanel");
        if (estudio == null) return;

        var header = estudio.transform.Find("StudioHeader");
        var iconTxt = header?.Find("Icon/IconTxt")?.GetComponent<TextMeshProUGUI>();
        var levelLine = header?.Find("XpBlock/LevelLineText")?.GetComponent<TextMeshProUGUI>();
        var xpText    = header?.Find("XpBlock/XPText")?.GetComponent<TextMeshProUGUI>();
        var repText   = header?.Find("RepBlock/RepText")?.GetComponent<TextMeshProUGUI>();

        var SL     = hub.studioLevel;
        var studio = hub.studio;
        var city   = hub.city;

        if (iconTxt != null)
        {
            string cityName = city != null
                ? CityLevelDatabase.GetLocalizedName(city.Level)
                : Loc.Get(LocKeys.NavStudio);
            iconTxt.text = cityName.ToUpperInvariant();
            iconTxt.fontStyle = FontStyles.Bold;
            iconTxt.color = CinematicTheme.TextPrimary;
            // Font size is auto-sized between 14-20 by DefinitiveHudBootstrap
        }

        if (SL != null && levelLine != null)
        {
            levelLine.text = string.Format(Loc.Get(LocKeys.StudioLevelFormat), SL.Level);
            levelLine.alignment = TextAlignmentOptions.MidlineLeft;
        }
        if (SL != null && xpText != null)
        {
            xpText.text = Loc.Format(LocKeys.StudioXPFormat, SL.XP, SL.XPToNext);
            xpText.alignment = TextAlignmentOptions.MidlineLeft;
        }
        if (studio != null && repText != null) repText.text = Loc.Format(LocKeys.StudioRepHeaderFmt, studio.reputation);
    }

    void SetStat(TextMeshProUGUI label, string value)
    {
        if (label == null) return;
        label.text  = value;
        label.color = value.StartsWith("+") && value != "+0%" ? C_POSITIVE : C_NEUTRAL;
    }

    static string FormatBonus(float v)
    {
        if (Mathf.Abs(v) < 0.001f) return "+0%";
        return v > 0 ? $"+{v * 100f:0}%" : $"{v * 100f:0}%";
    }
}
