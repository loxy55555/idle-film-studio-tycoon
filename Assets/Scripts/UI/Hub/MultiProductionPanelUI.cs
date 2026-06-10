using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows all active movie productions inside ProductionWidget (ContratosColumn, hub principal).
/// Attached at runtime by ProductionStatusUI on the same GameObject.
/// </summary>
public class MultiProductionPanelUI : MonoBehaviour
{
    public static MultiProductionPanelUI Instance { get; private set; }

    static readonly Color BG_CARD      = new Color(0.10f, 0.10f, 0.19f);
    static readonly Color BG_SECTION   = new Color(0.09f, 0.09f, 0.18f);
    static readonly Color TEXT_PRI     = Color.white;
    static readonly Color TEXT_SEC     = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color ACCENT_GREEN = new Color(0.18f, 0.80f, 0.44f);
    static readonly Color ACCENT_GOLD  = new Color(0.95f, 0.77f, 0.06f);

    const float SlotHeight = 72f;
    const float MaxWidgetHeight = 300f;

    RectTransform _slotsRoot;
    ScrollRect      _scroll;
    LayoutElement   _widgetLE;
    StudioManager   _studio;
    readonly List<SlotView> _slots = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        PrepareWidgetRoot();
        HideLegacyLayout();
        BuildSlotsRoot();
        GameHub.OnGameReady += Bind;
        if (GameHub.Instance != null) Bind();
    }

    void OnDestroy()
    {
        GameHub.OnGameReady -= Bind;
        Unbind();
        if (Instance == this) Instance = null;
    }

    void PrepareWidgetRoot()
    {
        _widgetLE = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();

        var hlg = GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) hlg.enabled = false;

        var rt = transform as RectTransform;
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }

    void Bind()
    {
        Unbind();
        _studio = GameHub.Instance?.studio;
        if (_studio != null)
            _studio.OnProductionsChanged += Refresh;
        Refresh();
    }

    void Unbind()
    {
        if (_studio != null)
            _studio.OnProductionsChanged -= Refresh;
    }

    void Update()
    {
        if (_studio == null && GameHub.Instance?.studio != null)
            Bind();
        if (_studio == null) return;
        RefreshProgressOnly();
    }

    void HideLegacyLayout()
    {
        foreach (Transform child in transform)
        {
            if (child.name == "MultiProdScroll") continue;
            child.gameObject.SetActive(false);
        }
    }

    void BuildSlotsRoot()
    {
        var existing = transform.Find("MultiProdScroll");
        if (existing != null) Destroy(existing.gameObject);

        var scrollGo = new GameObject("MultiProdScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.transform.SetParent(transform, false);

        var scrollLE = scrollGo.AddComponent<LayoutElement>();
        scrollLE.flexibleWidth = 1f;
        scrollLE.flexibleHeight = 1f;
        scrollLE.minHeight = SlotHeight;

        scrollGo.GetComponent<Image>().color = BG_CARD;

        var scrollRT = scrollGo.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(6f, 6f);
        scrollRT.offsetMax = new Vector2(-6f, -6f);

        _scroll = scrollGo.GetComponent<ScrollRect>();
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scrollGo.transform, false);
        var vpRT = viewport.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
        _scroll.viewport = vpRT;

        var content = new GameObject("SlotsContent", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        _slotsRoot = content.GetComponent<RectTransform>();
        _slotsRoot.anchorMin = new Vector2(0f, 1f);
        _slotsRoot.anchorMax = new Vector2(1f, 1f);
        _slotsRoot.pivot = new Vector2(0.5f, 1f);
        _slotsRoot.offsetMin = _slotsRoot.offsetMax = Vector2.zero;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _scroll.content = _slotsRoot;
    }

    void Refresh()
    {
        if (_studio == null || _slotsRoot == null) return;

        var active = _studio.GetProductionSnapshots();
        int displayCount = active.Count > 0 ? active.Count : 1;

        EnsureSlotCount(displayCount);
        for (int i = 0; i < displayCount; i++)
        {
            if (i < active.Count)
                _slots[i].Apply(active[i]);
            else
                _slots[i].ApplyIdle();
        }

        float contentHeight = displayCount * SlotHeight + (displayCount - 1) * 6f + 12f;
        float widgetHeight  = Mathf.Clamp(contentHeight, SlotHeight + 12f, MaxWidgetHeight);
        _widgetLE.preferredHeight = widgetHeight;
        _widgetLE.minHeight = SlotHeight + 12f;
        _widgetLE.flexibleHeight = 0f;

        _scroll.enabled = contentHeight > widgetHeight;
        LayoutRebuilder.ForceRebuildLayoutImmediate(_slotsRoot);
    }

    void RefreshProgressOnly()
    {
        if (_studio == null) return;
        var active = _studio.GetProductionSnapshots();
        for (int i = 0; i < _slots.Count && i < active.Count; i++)
            _slots[i].UpdateProgress(active[i]);
    }

    void EnsureSlotCount(int count)
    {
        while (_slots.Count < count)
        {
            var view = SlotView.Create(_slotsRoot);
            _slots.Add(view);
            view.root.localScale = Vector3.one * 0.94f;
            view.root.DOScale(1f, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);
        }
        while (_slots.Count > count)
        {
            var last = _slots[_slots.Count - 1];
            _slots.RemoveAt(_slots.Count - 1);
            if (last.root != null) Destroy(last.root.gameObject);
        }
    }

    class SlotView
    {
        public RectTransform root;
        TextMeshProUGUI _title;
        TextMeshProUGUI _genre;
        TextMeshProUGUI _time;
        TextMeshProUGUI _reward;
        Slider          _slider;
        SmoothProgressBar _smooth;

        public static SlotView Create(Transform parent)
        {
            var view = new SlotView();
            var go = new GameObject("ProdSlot", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = BG_CARD;

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = SlotHeight;
            le.minHeight = SlotHeight;

            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 6, 6);
            vlg.spacing = 3;
            vlg.childControlWidth = vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var topRow = new GameObject("TopRow", typeof(RectTransform));
            topRow.transform.SetParent(go.transform, false);
            var hlg = topRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childControlWidth = hlg.childControlHeight = true;
            hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;
            topRow.AddComponent<LayoutElement>().preferredHeight = 20;

            view._title = RuntimeTmpText.Create(topRow.transform, "Película", 14, TEXT_PRI, FontStyles.Bold);
            var titleLE = view._title.gameObject.AddComponent<LayoutElement>();
            titleLE.flexibleWidth = 1;
            view._genre = RuntimeTmpText.Create(topRow.transform, "—", 12, ACCENT_GOLD);
            view._genre.gameObject.AddComponent<LayoutElement>().preferredWidth = 72;

            var barGo = new GameObject("Bar", typeof(RectTransform), typeof(Image), typeof(Slider));
            barGo.transform.SetParent(go.transform, false);
            barGo.GetComponent<Image>().color = BG_SECTION;
            barGo.AddComponent<LayoutElement>().preferredHeight = 12;
            view._slider = barGo.GetComponent<Slider>();
            SetupSlider(view._slider);
            ReadOnlySlider.Configure(view._slider);
            view._smooth = barGo.AddComponent<SmoothProgressBar>();

            var botRow = new GameObject("BotRow", typeof(RectTransform));
            botRow.transform.SetParent(go.transform, false);
            var blg = botRow.AddComponent<HorizontalLayoutGroup>();
            blg.spacing = 8;
            blg.childControlWidth = blg.childControlHeight = true;
            blg.childForceExpandWidth = blg.childForceExpandHeight = true;
            botRow.AddComponent<LayoutElement>().preferredHeight = 18;

            view._time = RuntimeTmpText.Create(botRow.transform, "—", 12, TEXT_SEC);
            view._time.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            view._reward = RuntimeTmpText.Create(botRow.transform, "", 12, ACCENT_GREEN, FontStyles.Bold,
                TextAlignmentOptions.MidlineRight);
            view._reward.gameObject.AddComponent<LayoutElement>().preferredWidth = 130;

            view.root = go.GetComponent<RectTransform>();
            return view;
        }

        public void Apply(ProductionSlotSnapshot snap)
        {
            _title.text = snap.movieName;
            _genre.text = GenreLabel(snap.genre);
            _time.text = FormatTime(snap.timeLeftSeconds);
            _reward.text = "+" + AnimatedMoneyText.FormatMoney(snap.rewardMoney);
            UpdateProgress(snap);
        }

        public void ApplyIdle()
        {
            _title.text = "Sin producción activa";
            _genre.text = "";
            _time.text = "Inicia una película";
            _reward.text = "";
            if (_smooth != null) _smooth.SetNormalized(0f);
            else if (_slider != null) _slider.value = 0f;
        }

        public void UpdateProgress(ProductionSlotSnapshot snap)
        {
            if (_smooth != null) _smooth.SetNormalized(snap.progress01);
            else if (_slider != null) _slider.value = snap.progress01;
            _time.text = FormatTime(snap.timeLeftSeconds);
        }

        static string FormatTime(float seconds)
        {
            int s = Mathf.CeilToInt(seconds);
            return s >= 60 ? $"{s / 60:0}:{s % 60:00} rest." : $"{s:0}s rest.";
        }

        static void SetupSlider(Slider slider)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.direction = Slider.Direction.LeftToRight;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(slider.transform, false);
            var faRT = fillArea.GetComponent<RectTransform>();
            faRT.anchorMin = Vector2.zero;
            faRT.anchorMax = Vector2.one;
            faRT.offsetMin = faRT.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            fill.GetComponent<Image>().color = ACCENT_GREEN;
            var fillRT = fill.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
            slider.fillRect = fillRT;
        }
    }

    static string GenreLabel(MovieGenre g) => g switch
    {
        MovieGenre.Action  => "ACCIÓN",
        MovieGenre.Drama   => "DRAMA",
        MovieGenre.Horror  => "TERROR",
        MovieGenre.Comedy  => "COMEDIA",
        MovieGenre.Romance => "ROMANCE",
        MovieGenre.SciFi   => "SCI-FI",
        _                  => g.ToString().ToUpper(),
    };
}
