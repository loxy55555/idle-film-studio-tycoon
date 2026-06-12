using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Horizontal bonifications strip shown at the top of the Studio screen (Phase 8.6A).
/// Shows totals for: Velocidad / Calidad / Taquilla / REP / XP / Costes.
/// Self-building: when the labels are missing it constructs its own UI so the
/// panel works in baked scenes without the editor builder.
/// Reads live values from DepartmentSystem / UpgradeSystem / StudioLevelSystem.
/// No gameplay logic — display only.
/// </summary>
public class BonificationsBarUI : MonoBehaviour
{
    TextMeshProUGUI _speedVal;
    TextMeshProUGUI _qualityVal;
    TextMeshProUGUI _boxOfficeVal;
    TextMeshProUGUI _repVal;
    TextMeshProUGUI _xpVal;
    TextMeshProUGUI _costsVal;

    bool  _subscribed;
    float _nextPoll;

    static readonly Color C_POSITIVE = new Color(0.18f, 0.80f, 0.44f);
    static readonly Color C_NEUTRAL  = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color BG_BAR     = new Color(0.07f, 0.08f, 0.14f);
    static readonly Color BG_CELL    = new Color(0.11f, 0.12f, 0.20f);
    static readonly Color TEXT_DIM   = new Color(0.45f, 0.46f, 0.58f);

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
        // DepartmentSystem has no change event — light poll keeps values live.
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
    }

    void OnLevelUp(int _) => Refresh();

    // ── Self-build (Phase 8.6A) ───────────────────────────────────────────────

    void EnsureBuilt()
    {
        if (_speedVal != null) return; // wired from a baked/builder hierarchy

        // Background
        var bg = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        bg.color = BG_BAR;
        bg.raycastTarget = false;

        var le = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 78f;
        le.minHeight = 70f;
        le.flexibleHeight = 0f;

        var hlg = GetComponent<HorizontalLayoutGroup>() ?? gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(8, 8, 6, 6);
        hlg.spacing = 6;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        _qualityVal   = BuildCell("⭐", "CALIDAD",   "QualityVal");
        _speedVal     = BuildCell("⚡", "VELOCIDAD", "SpeedVal");
        _boxOfficeVal = BuildCell("🎟", "TAQUILLA",  "BoxOfficeVal");
        _repVal       = BuildCell("🏆", "REP",       "RepVal");
        _xpVal        = BuildCell("📈", "XP",        "XpVal");
        _costsVal     = BuildCell("💰", "COSTES",    "CostsVal");
    }

    TextMeshProUGUI BuildCell(string icon, string label, string valueName)
    {
        var cell = new GameObject("Cell_" + valueName, typeof(RectTransform), typeof(Image));
        cell.transform.SetParent(transform, false);
        cell.GetComponent<Image>().color = BG_CELL;
        cell.GetComponent<Image>().raycastTarget = false;

        var vlg = cell.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(2, 2, 4, 4);
        vlg.spacing = 0;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var iconTmp = RuntimeTmpText.Create(cell.transform, icon,
            16f, Color.white, FontStyles.Normal, TextAlignmentOptions.Center, "Icon");
        iconTmp.raycastTarget = false;
        iconTmp.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

        var valTmp = RuntimeTmpText.Create(cell.transform, "+0%",
            13f, C_NEUTRAL, FontStyles.Bold, TextAlignmentOptions.Center, valueName);
        valTmp.raycastTarget = false;
        valTmp.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        var lblTmp = RuntimeTmpText.Create(cell.transform, label,
            8f, TEXT_DIM, FontStyles.Bold, TextAlignmentOptions.Center, "Lbl");
        lblTmp.raycastTarget = false;
        lblTmp.gameObject.AddComponent<LayoutElement>().preferredHeight = 12f;

        return valTmp;
    }

    public void AutoWire()
    {
        _speedVal     = FindValueTmp("SpeedVal");
        _qualityVal   = FindValueTmp("QualityVal");
        _boxOfficeVal = FindValueTmp("BoxOfficeVal");
        _repVal       = FindValueTmp("RepVal");
        _xpVal        = FindValueTmp("XpVal");
        _costsVal     = FindValueTmp("CostsVal");
    }

    TextMeshProUGUI FindValueTmp(string name)
    {
        foreach (var t in GetComponentsInChildren<TextMeshProUGUI>(true))
            if (t.name == name) return t;
        return null;
    }

    public void Refresh()
    {
        if (_speedVal == null) AutoWire();

        var hub = GameHub.Instance;
        if (hub == null) return;

        var D = hub.departments;
        var SL = hub.studioLevel;

        // Velocidad: base speed - 1.0 expressed as % bonus
        float rawSpeed = D != null ? D.CalculateSpeed() : 1f;
        float speedBonus = rawSpeed - 1f;
        SetStat(_speedVal, FormatBonus(speedBonus));

        // Calidad: base quality - 1.0 as % bonus
        float rawQuality = D != null ? D.CalculateQuality() : 1f;
        float qualBonus = rawQuality - 1f;
        SetStat(_qualityVal, FormatBonus(qualBonus));

        // Taquilla: quality × studio-level multiplier (proxy for box office)
        float boxBonus = qualBonus * 0.6f;
        SetStat(_boxOfficeVal, FormatBonus(boxBonus));

        // REP: 5% per studio level above 1
        int stuLevel = SL != null ? SL.Level : 1;
        float repBonus = (stuLevel - 1) * 0.05f;
        SetStat(_repVal, FormatBonus(repBonus));

        // XP: 3% per studio level above 1
        float xpBonus = (stuLevel - 1) * 0.03f;
        SetStat(_xpVal, FormatBonus(xpBonus));

        // Costes: cost reduction (negative means cheaper)
        float costRed = D != null ? D.CalculateCostReduction() : 0f;
        if (_costsVal != null)
        {
            _costsVal.text  = costRed > 0f ? $"-{costRed * 100f:0}%" : "0%";
            _costsVal.color = costRed > 0f ? C_POSITIVE : C_NEUTRAL;
        }
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
