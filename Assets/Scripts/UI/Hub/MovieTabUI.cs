using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Production tab — premium offer cards + history (Phase 8.2).</summary>
public class MovieTabUI : MonoBehaviour
{
    [Header("Data")]
    public MovieConfig[] allMovies;

    [Header("UI")]
    public RectTransform slotsRow;
    public RectTransform historyContent;
    public TextMeshProUGUI historyEmptyLabel;

    static readonly Color TEXT_PRI   = Color.white;
    static readonly Color TEXT_SEC   = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color ACCENT_GOLD = new Color(0.95f, 0.77f, 0.06f);

    readonly List<MovieButtonUI> _slotButtons = new();
    Coroutine _layoutRoutine;

    StudioManager     _studio;
    StudioLevelSystem _level;

    void Awake()
    {
        GameHub.OnGameReady += Bind;
        ProductionPremiereHooks.EnsureOnCanvas();
    }

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
                ? Loc.Get(LocKeys.ProdCatalogComplete)
                : catalogLow && cfg == null
                    ? Loc.Get(LocKeys.ProdNoOffer)
                    : Loc.Get(LocKeys.ProdEmptySlot);
            var slot = CreateSlotCard(cfg, emptyLabel);
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

    MovieButtonUI CreateSlotCard(MovieConfig cfg, string emptyLabel)
    {
        var cardGo = new GameObject(cfg != null ? "Offer_" + cfg.movieName : "Offer_Empty",
            typeof(RectTransform), typeof(Image));
        cardGo.transform.SetParent(slotsRow, false);
        var le = cardGo.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;
        le.flexibleHeight = 0f;

        if (cfg == null)
        {
            MovieOfferCardLayoutBuilder.Build(cardGo.transform, null, true);
            var empty = cardGo.GetComponentInChildren<TextMeshProUGUI>();
            if (empty != null) empty.text = emptyLabel;
            return null;
        }

        var wire = MovieOfferCardLayoutBuilder.Build(cardGo.transform, cfg, false);

        var ui = cardGo.AddComponent<MovieButtonUI>();
        ui.movieConfig = cfg;
        ui.posterImage = wire.posterImage;
        ui.rarityFrame = wire.rarityFrame;
        ui.titleText = wire.titleText;
        ui.genreText = wire.genreText;
        ui.rarityText = wire.rarityText;
        ui.durationText = wire.durationText;
        ui.unlockText = wire.unlockText;
        ui.lockedOverlay = wire.lockedOverlay;
        ui.produceButton = wire.selectButton;
        ui.selectLabelText = wire.selectLabelText;
        ui.Setup(cfg, _studio);
        return ui;
    }

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
        HudSkinProvider.ApplyCard(row.GetComponent<Image>(), HudCardVariant.Primary);
        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 72;
        le.flexibleWidth = 1;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(12, 12, 8, 8);
        hlg.spacing = 10;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;

        var title = RuntimeTmpText.Create(row.transform, e.movieName, 16, TEXT_PRI, FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft, "Title");
        title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        var rewards = RuntimeTmpText.Create(row.transform,
            ProductionLoc.FormatHistoryRewards(e.moneyReward, e.repGain, e.xpGain),
            14, ACCENT_GOLD, FontStyles.Normal, TextAlignmentOptions.MidlineRight, "Rewards");
        rewards.gameObject.AddComponent<LayoutElement>().preferredWidth = 260;
    }
}

/// <summary>Three uniform-random offers from the eligible pool. Rejected offers are not tracked.</summary>
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
        var eligible = MovieOfferPoolRules.BuildEligiblePool(
            allMovies, studioLevel, reputation, completedMovieKeys);
        if (eligible.Count == 0) return result;

        var shuffled = new List<MovieConfig>(eligible);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        int pickCount = Mathf.Min(OfferSlotCount, shuffled.Count);
        for (int i = 0; i < pickCount; i++)
            result.Add(shuffled[i]);

        return result;
    }
}
