using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Películas tab: 3 production choices on top + completed movies history below.
/// </summary>
public class MovieTabUI : MonoBehaviour
{
    [Header("Data")]
    public MovieConfig[] allMovies;

    [Header("UI")]
    public RectTransform slotsRow;
    public RectTransform historyContent;
    public TextMeshProUGUI historyEmptyLabel;

    static readonly Color BG_CARD    = new Color(0.10f, 0.10f, 0.19f);
    static readonly Color BG_SECTION = new Color(0.08f, 0.08f, 0.16f);
    static readonly Color TEXT_PRI   = Color.white;
    static readonly Color TEXT_SEC   = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color ACCENT_GREEN = new Color(0.18f, 0.80f, 0.44f);
    static readonly Color ACCENT_GOLD  = new Color(0.95f, 0.77f, 0.06f);

    static readonly string[] SlotLabels = { "RÁPIDA", "ESTÁNDAR", "ÉPICA" };

    readonly List<MovieButtonUI> _slotButtons = new();
    Coroutine _layoutRoutine;

    StudioManager     _studio;
    StudioLevelSystem _level;

    void Awake() => GameHub.OnGameReady += Bind;

    void OnEnable()
    {
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
        Unbind();
        if (_layoutRoutine != null)
            StopCoroutine(_layoutRoutine);
    }

    void Bind()
    {
        Unbind();
        _studio = GameHub.Instance?.studio;
        _level  = GameHub.Instance?.studioLevel;
        if (_studio != null)
        {
            _studio.OnMovieCompleted += OnMovieCompleted;
            _studio.OnMovieHistoryChanged += RebuildHistory;
        }

        if (isActiveAndEnabled)
            RebuildAll();
    }

    void Unbind()
    {
        if (_studio != null)
        {
            _studio.OnMovieCompleted -= OnMovieCompleted;
            _studio.OnMovieHistoryChanged -= RebuildHistory;
        }
    }

    void OnMovieCompleted(MovieCompletePayload _) { RebuildHistory(); RebuildSlots(); }

    public void RebuildAll()
    {
        RebuildSlots();
        RebuildHistory();
    }

    void RebuildSlots()
    {
        if (slotsRow == null) return;

        for (int i = slotsRow.childCount - 1; i >= 0; i--)
            Destroy(slotsRow.GetChild(i).gameObject);
        _slotButtons.Clear();

        var picks = MovieOfferPicker.PickThree(
            allMovies,
            _level?.Level ?? 1,
            _studio?.reputation ?? 0f,
            _studio?.CompletedMovieKeys);

        var catalog = _studio != null
            ? _studio.GetMovieCatalogSnapshot(allMovies)
            : default;
        bool catalogEmpty = _studio != null && catalog.eligibleRemaining == 0 && catalog.eligibleTotal > 0;
        bool catalogLow = _studio != null && catalog.NeedsCatalogExpansion && !catalogEmpty;

        for (int i = 0; i < MovieOfferPicker.OfferSlotCount; i++)
        {
            MovieConfig cfg = i < picks.Count ? picks[i] : null;
            string emptyLabel = catalogEmpty
                ? "Catálogo completado"
                : catalogLow && cfg == null
                    ? "Sin oferta disponible"
                    : "—";
            var slot = CreateSlotCard(cfg, SlotLabels[i], emptyLabel);
            if (slot != null) _slotButtons.Add(slot);
        }

        slotsRow.GetComponent<SquareTileRowLayout>()?.RequestDeferredApply();
        RequestSlotLayout();
    }

    void RequestSlotLayout()
    {
        if (!isActiveAndEnabled || slotsRow == null) return;
        if (_layoutRoutine != null)
            StopCoroutine(_layoutRoutine);
        _layoutRoutine = StartCoroutine(ApplySlotLayoutDeferred());
    }

    IEnumerator ApplySlotLayoutDeferred()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        var layout = slotsRow.GetComponent<SquareTileRowLayout>();
        if (layout != null)
        {
            layout.RequestDeferredApply();
            layout.Apply();
        }

        yield return null;
        Canvas.ForceUpdateCanvases();
        layout?.Apply();
        _layoutRoutine = null;
    }

    MovieButtonUI CreateSlotCard(MovieConfig cfg, string slotLabel, string emptyLabel = "—")
    {
        var cardGo = new GameObject("Slot_" + slotLabel, typeof(RectTransform), typeof(Image));
        cardGo.transform.SetParent(slotsRow, false);
        cardGo.GetComponent<Image>().color = cfg != null ? BG_CARD : BG_SECTION;
        var le = cardGo.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;
        le.flexibleHeight = 0f;

        var vlg = cardGo.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.spacing = 1;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;

        var lbl = MakeTmp(cardGo.transform, slotLabel, 11, ACCENT_GOLD, TextAlignmentOptions.Center);
        lbl.fontStyle = FontStyles.Bold;
        lbl.GetComponent<RectTransform>().gameObject.AddComponent<LayoutElement>().preferredHeight = 14;

        if (cfg == null)
        {
            MakeTmp(cardGo.transform, emptyLabel, 12, TEXT_SEC, TextAlignmentOptions.Center);
            return null;
        }

        var title = MakeTmp(cardGo.transform, cfg.movieName, 13, TEXT_PRI, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        title.textWrappingMode = TextWrappingModes.Normal;
        title.overflowMode = TextOverflowModes.Ellipsis;
        title.GetComponent<RectTransform>().gameObject.AddComponent<LayoutElement>().preferredHeight = 28;

        var stats = MakeTmp(cardGo.transform, BuildPreview(cfg), 10, TEXT_SEC, TextAlignmentOptions.Center);
        stats.textWrappingMode = TextWrappingModes.Normal;
        stats.GetComponent<RectTransform>().gameObject.AddComponent<LayoutElement>().preferredHeight = 56;

        var btnGo = new GameObject("ProduceBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(cardGo.transform, false);
        btnGo.GetComponent<Image>().color = ACCENT_GREEN;
        btnGo.AddComponent<UIButtonScale>();
        var btnLE = btnGo.AddComponent<LayoutElement>();
        btnLE.preferredHeight = 32;
        btnLE.flexibleHeight = 0f;
        MakeTmp(btnGo.transform, "▶", 16, TEXT_PRI, TextAlignmentOptions.Center);

        var ui = cardGo.AddComponent<MovieButtonUI>();
        ui.movieConfig = cfg;
        ui.titleText = title;
        ui.produceButton = btnGo.GetComponent<Button>();
        ui.Setup(cfg, _studio);
        return ui;
    }

    string BuildPreview(MovieConfig cfg)
    {
        var d = GameHub.Instance?.departments;
        float q = d?.CalculateQuality() ?? 1f;
        float s = d?.CalculateSpeed() ?? 1f;
        float cr = d?.CalculateCostReduction() ?? 0f;
        long cost = (long)(cfg.cost * (1f - cr));
        long reward = (long)(cfg.baseReward * cfg.quality * q);
        float secs = cfg.duration / s;

        var lines = new List<string>
        {
            GenreLabel(cfg.genre),
            "Coste: " + AnimatedMoneyText.FormatMoney(cost),
            "Duración: " + secs.ToString("0.0") + "s",
            "Recompensa: +" + AnimatedMoneyText.FormatMoney(reward),
        };

        if (_studio != null)
        {
            float variety = _studio.GetPreviewVarietyBonusPercent(cfg);
            if (variety > 0f)
                lines.Add("+" + variety.ToString("0") + "% variedad");
        }

        return string.Join("\n", lines);
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

    void RebuildHistory()
    {
        if (historyContent == null) return;

        for (int i = historyContent.childCount - 1; i >= 0; i--)
            Destroy(historyContent.GetChild(i).gameObject);

        var history = _studio?.MovieHistory;
        int count = history?.Count ?? 0;

        if (historyEmptyLabel != null)
            historyEmptyLabel.gameObject.SetActive(count == 0);

        if (count == 0) return;

        for (int i = 0; i < count; i++)
            CreateHistoryRow(history[i]);
    }

    void CreateHistoryRow(MovieHistoryEntry e)
    {
        var row = new GameObject("Hist_" + e.movieName, typeof(RectTransform), typeof(Image));
        row.transform.SetParent(historyContent, false);
        row.GetComponent<Image>().color = BG_CARD;
        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 72;
        le.flexibleWidth = 1;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(12, 12, 8, 8);
        hlg.spacing = 10;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;

        var title = MakeTmp(row.transform, e.movieName, 16, TEXT_PRI, TextAlignmentOptions.MidlineLeft);
        title.fontStyle = FontStyles.Bold;
        title.GetComponent<RectTransform>().gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        var rewards = MakeTmp(row.transform,
            $"+{AnimatedMoneyText.FormatMoney(e.moneyReward)}  +{e.repGain:0.0} REP  +{e.xpGain:0} XP",
            14, ACCENT_GOLD, TextAlignmentOptions.MidlineRight);
        rewards.GetComponent<RectTransform>().gameObject.AddComponent<LayoutElement>().preferredWidth = 260;
    }

    static TextMeshProUGUI MakeTmp(Transform parent, string text, float size, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject("Txt", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }
}

/// <summary>Picks Rápida / Estándar / Épica offers; excludes permanently completed movies.</summary>
public static class MovieOfferPicker
{
    public const int OfferSlotCount = 3;

    public static List<MovieConfig> PickThree(
        MovieConfig[] allMovies,
        int studioLevel,
        float reputation,
        IReadOnlyCollection<string> completedMovieKeys)
    {
        var result = new List<MovieConfig>(OfferSlotCount);
        if (allMovies == null || allMovies.Length == 0) return result;

        var completed = completedMovieKeys != null
            ? new HashSet<string>(completedMovieKeys)
            : new HashSet<string>();

        var unlocked = new List<MovieConfig>();
        foreach (var m in allMovies)
        {
            if (m == null) continue;
            if (completed.Contains(m.name)) continue;
            if (m.unlockStudioLevel > studioLevel) continue;
            if (GameHub.Instance?.city != null && !GameHub.Instance.city.IsMovieUnlocked(m)) continue;
            if (m.unlockReputation > 0 && reputation < m.unlockReputation) continue;
            unlocked.Add(m);
        }

        if (unlocked.Count == 0) return result;
        unlocked.Sort((a, b) => a.cost.CompareTo(b.cost));

        int count = unlocked.Count;
        int tierSize = Mathf.Max(1, count / 3);
        int fastEnd  = Mathf.Min(tierSize, count);
        int stdStart = fastEnd;
        int stdEnd   = Mathf.Min(stdStart + tierSize, count);

        var picked = new HashSet<MovieConfig>();
        TryAdd(result, picked, PickRandom(Slice(unlocked, 0, fastEnd)));
        TryAdd(result, picked, PickRandom(Slice(unlocked, stdStart, stdEnd)));
        TryAdd(result, picked, PickRandom(Slice(unlocked, stdEnd, count)));

        while (result.Count < OfferSlotCount && result.Count < unlocked.Count)
        {
            var fallback = PickRandom(unlocked, picked);
            if (fallback == null) break;
            TryAdd(result, picked, fallback);
        }

        return result;
    }

    static List<MovieConfig> Slice(List<MovieConfig> list, int start, int end)
    {
        var slice = new List<MovieConfig>();
        for (int i = start; i < end && i < list.Count; i++)
            slice.Add(list[i]);
        return slice;
    }

    static MovieConfig PickRandom(List<MovieConfig> pool, HashSet<MovieConfig> alreadyPicked = null)
    {
        if (pool == null || pool.Count == 0) return null;
        var candidates = new List<MovieConfig>();
        foreach (var m in pool)
        {
            if (alreadyPicked != null && alreadyPicked.Contains(m)) continue;
            candidates.Add(m);
        }
        if (candidates.Count == 0) return null;
        return candidates[Random.Range(0, candidates.Count)];
    }

    static void TryAdd(List<MovieConfig> result, HashSet<MovieConfig> picked, MovieConfig movie)
    {
        if (movie == null || picked.Contains(movie)) return;
        result.Add(movie);
        picked.Add(movie);
    }
}
