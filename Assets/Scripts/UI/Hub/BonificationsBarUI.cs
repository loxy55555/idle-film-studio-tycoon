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

    bool  _subscribed;
    float _nextPoll;

    static readonly Color C_POSITIVE  = new Color(0.18f, 0.80f, 0.44f);
    static readonly Color C_NEUTRAL   = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color C_GOLD      = new Color(0.95f, 0.77f, 0.06f);
    static readonly Color BG_BAR      = new Color(0.08f, 0.09f, 0.16f);
    static readonly Color BG_CELL     = new Color(0.11f, 0.13f, 0.22f);
    static readonly Color BG_BADGE    = new Color(0.05f, 0.06f, 0.12f);
    static readonly Color BG_INFO     = new Color(0.07f, 0.08f, 0.14f);
    static readonly Color TEXT_DIM    = new Color(0.45f, 0.46f, 0.58f);
    static readonly Color ACCENT_LINE = new Color(0.18f, 0.80f, 0.44f, 0.90f);
    static readonly Color ACCENT_DIM  = new Color(0.18f, 0.80f, 0.44f, 0.25f);
    static readonly Color BAR_BG      = new Color(0.10f, 0.11f, 0.20f);
    static readonly Color BAR_FILL    = new Color(0.18f, 0.80f, 0.44f);

    void Awake()
    {
        GameHub.OnGameReady += OnGameReady;
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
    }

    void OnLevelUp(int _) => Refresh();

    // ─────────────────────────────────────────────────────────────
    //  Construction
    // ─────────────────────────────────────────────────────────────

    void EnsureBuilt()
    {
        if (transform.Find(CompactMarker) != null && FindTmp("BonusSummary") != null) return;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var ch = transform.GetChild(i);
            if (Application.isPlaying) Destroy(ch.gameObject);
            else DestroyImmediate(ch.gameObject);
        }

        var bg = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        bg.color = BG_BAR;
        bg.raycastTarget = false;

        var le = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = HudLayoutConstants.StudioSummaryHeight;
        le.minHeight       = 100f;
        le.flexibleHeight  = 0f;

        var marker = new GameObject(CompactMarker, typeof(RectTransform));
        marker.transform.SetParent(transform, false);

        var row = new GameObject("SummaryRow", typeof(RectTransform), typeof(Image));
        row.transform.SetParent(transform, false);
        row.GetComponent<Image>().color = BG_INFO;
        row.GetComponent<Image>().raycastTarget = false;
        row.AddComponent<LayoutElement>().preferredHeight = HudLayoutConstants.StudioSummaryHeight - 6f;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(12, 12, 8, 8);
        hlg.spacing = 10;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // Level badge
        var badge = new GameObject("LevelBadge", typeof(RectTransform), typeof(Image));
        badge.transform.SetParent(row.transform, false);
        badge.GetComponent<Image>().color = BG_BADGE;
        badge.GetComponent<Image>().raycastTarget = false;
        badge.AddComponent<LayoutElement>().preferredWidth = 56f;
        _levelNumText = RuntimeTmpText.Create(badge.transform, "1",
            22f, ACCENT_LINE, FontStyles.Bold, TextAlignmentOptions.Center, "LevelNum");
        _levelNumText.raycastTarget = false;

        // Info column
        var info = new GameObject("InfoCol", typeof(RectTransform));
        info.transform.SetParent(row.transform, false);
        info.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var vlg = info.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var meta = new GameObject("MetaRow", typeof(RectTransform));
        meta.transform.SetParent(info.transform, false);
        meta.AddComponent<LayoutElement>().preferredHeight = 18f;
        var metaHLG = meta.AddComponent<HorizontalLayoutGroup>();
        metaHLG.spacing = 8;
        metaHLG.childControlWidth = metaHLG.childControlHeight = true;
        metaHLG.childForceExpandWidth = false;
        metaHLG.childForceExpandHeight = true;

        _repLineText = RuntimeTmpText.Create(meta.transform, "★ 0 REP",
            13f, C_GOLD, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "RepLine");
        _repLineText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        _xpLineText = RuntimeTmpText.Create(meta.transform, "0 / 120 XP",
            11f, TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.MidlineRight, "XpLine");
        _xpLineText.gameObject.AddComponent<LayoutElement>().preferredWidth = 110f;

        var xpBarGo = new GameObject("XpBar", typeof(RectTransform), typeof(Image), typeof(Slider));
        xpBarGo.transform.SetParent(info.transform, false);
        xpBarGo.GetComponent<Image>().color = BAR_BG;
        xpBarGo.AddComponent<LayoutElement>().preferredHeight = 8f;
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

        _bonusSummaryText = RuntimeTmpText.Create(info.transform, "Ingresos +0% · REP +0% · Vel +0%",
            10f, C_POSITIVE, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "BonusSummary");
        _bonusSummaryText.gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;

        _objectiveText = RuntimeTmpText.Create(info.transform, "Próximo nivel",
            9f, TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, "Objective");
        _objectiveText.gameObject.AddComponent<LayoutElement>().preferredHeight = 12f;
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
        if (_levelNumText != null) _levelNumText.text = stuLevel.ToString();

        float rep = ST != null ? ST.reputation : 0f;
        if (_repLineText != null) _repLineText.text = $"★ {rep:0.#} REP";

        if (SL != null)
        {
            if (_xpLineText != null)
                _xpLineText.text = $"{SL.XP:0} / {SL.XPToNext:0} XP";
            if (_objectiveText != null)
                _objectiveText.text = $"Próximo nivel · {Mathf.Max(0f, SL.XPToNext - SL.XP):0} XP restantes";
            if (_levelXpBar != null)
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

        var levelLine = estudio.transform.Find("StudioHeader/XpBlock/LevelLineText")?.GetComponent<TextMeshProUGUI>();
        var xpText    = estudio.transform.Find("StudioHeader/XpBlock/XPText")?.GetComponent<TextMeshProUGUI>();
        var repText   = estudio.transform.Find("StudioHeader/RepBlock/RepText")?.GetComponent<TextMeshProUGUI>();

        var SL     = hub.studioLevel;
        var studio = hub.studio;
        if (SL != null && levelLine != null) levelLine.text = $"Nv. {SL.Level}";
        if (SL != null && xpText    != null) xpText.text    = $"{SL.XP:0}/{SL.XPToNext:0} XP";
        if (studio != null && repText != null) repText.text = $"{studio.reputation:0} REP";
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
