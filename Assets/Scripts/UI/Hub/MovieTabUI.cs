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
    bool _bound;

    public int LastPickCount { get; private set; }
    public int LastSlotUiCount => slotsRow != null ? slotsRow.childCount : 0;

    StudioManager     _studio;
    StudioLevelSystem _level;

    void Awake()
    {
        GameHub.OnGameReady += Bind;
        MovieOfferState.OnOffersChanged += RefreshOfferDisplay;
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
        MovieOfferState.OnOffersChanged -= RefreshOfferDisplay;
        Unbind();
        if (_layoutRoutine != null)
            StopCoroutine(_layoutRoutine);
    }

    void Bind()
    {
        if (!_bound)
        {
            _studio = GameHub.Instance?.studio;
            _level  = GameHub.Instance?.studioLevel;
            if (_studio != null)
            {
                _studio.OnMovieCompleted += OnMovieCompleted;
                _studio.OnMovieHistoryChanged += RebuildHistory;
                _bound = true;
            }
        }

        if (_bound && isActiveAndEnabled)
            RefreshOfferDisplay();
    }

    void Unbind()
    {
        if (_studio != null)
        {
            _studio.OnMovieCompleted -= OnMovieCompleted;
            _studio.OnMovieHistoryChanged -= RebuildHistory;
        }
        _studio = null;
        _bound = false;
    }

    void OnMovieCompleted(MovieCompletePayload payload)
    {
        Debug.Log($"[Production] MovieCompleted={payload.movieName}");
        RebuildHistory();

        var catalog = MovieCatalogRuntime.AllMovies;
        MovieOfferState.OnMovieCompleted(
            catalog,
            _level?.Level ?? 1,
            _studio?.reputation ?? 0f,
            _studio?.CompletedMovieKeys,
            _studio?.GetActiveProductionConfigs());

        Debug.Log($"[Production] OffersRebuilt={LastPickCount} slots={LastSlotUiCount}");
    }

    public void RebuildAll()
    {
        RefreshOfferDisplay();
        RebuildHistory();
    }

    public void RefreshLocalization()
    {
        RefreshOfferDisplay();
        RebuildHistory();
        foreach (var btn in _slotButtons)
            if (btn != null) btn.RefreshUI();
    }

    public void RefreshOfferDisplay()
    {
        if (slotsRow == null) return;

        var catalogMovies = MovieCatalogRuntime.AllMovies;
        MovieOfferState.SyncOffers(
            catalogMovies,
            _level?.Level ?? 1,
            _studio?.reputation ?? 0f,
            _studio?.CompletedMovieKeys,
            _studio?.GetActiveProductionConfigs());

        RenderOfferSlots(MovieOfferState.ResolveOffers(catalogMovies));
    }


    void RenderOfferSlots(List<MovieConfig> picks)
    {
        if (slotsRow == null) return;

        if (_layoutRoutine != null)
        {
            StopCoroutine(_layoutRoutine);
            _layoutRoutine = null;
        }

        ClearOfferSlots();

        LastPickCount = 0;
        for (int i = 0; i < picks.Count; i++)
        {
            if (picks[i] != null) LastPickCount++;
        }

        MovieOfferAuditState.rebuildCount++;
        MovieOfferAuditState.lastPickCount = LastPickCount;

        var catalog = _studio != null
            ? _studio.GetMovieCatalogSnapshot(MovieCatalogRuntime.AllMovies)
            : default;
        bool catalogEmpty = _studio != null && catalog.eligibleRemaining == 0 && catalog.eligibleTotal > 0;
        bool catalogLow = _studio != null && catalog.NeedsCatalogExpansion && !catalogEmpty;

        int emptySlots = 0;
        for (int i = 0; i < MovieOfferPicker.OfferSlotCount; i++)
        {
            MovieConfig cfg = i < picks.Count ? picks[i] : null;
            if (cfg == null) emptySlots++;
            string emptyLabel = catalogEmpty
                ? Loc.Get(LocKeys.ProdCatalogComplete)
                : catalogLow && cfg == null
                    ? Loc.Get(LocKeys.ProdNoOffer)
                    : Loc.Get(LocKeys.ProdEmptySlot);
            var slot = CreateSlotCard(cfg, emptyLabel);
            if (slot != null) _slotButtons.Add(slot);
        }

        MovieOfferAuditState.lastSlotUiCount = slotsRow.childCount;
        MovieOfferAuditState.lastEmptySlotCount = emptySlots;
        MovieOfferAuditState.lastDuplicateUiDetected = CountOfferUiIssues();

        slotsRow.GetComponent<SquareTileRowLayout>()?.RequestDeferredApply();
        RequestSlotLayout();
    }

    void ClearOfferSlots()
    {
        for (int i = slotsRow.childCount - 1; i >= 0; i--)
        {
            var go = slotsRow.GetChild(i).gameObject;
            go.transform.SetParent(null, false);
            Destroy(go);
        }
        _slotButtons.Clear();
    }

    static int CountOfferUiIssues()
    {
        var tab = Object.FindAnyObjectByType<MovieTabUI>(FindObjectsInactive.Include);
        if (tab == null || tab.slotsRow == null) return 0;

        int issues = Mathf.Max(0, tab.slotsRow.childCount - MovieOfferPicker.OfferSlotCount);

        var names = new HashSet<string>();
        for (int i = 0; i < tab.slotsRow.childCount; i++)
        {
            string n = tab.slotsRow.GetChild(i).name;
            if (!names.Add(n)) issues++;
        }
        return issues;
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
        ExpandOfferCardsToFill();
        _layoutRoutine = null;
    }

    void ExpandOfferCardsToFill()
    {
        if (slotsRow == null) return;

        int slotCount = 0;
        for (int i = 0; i < slotsRow.childCount; i++)
        {
            if (slotsRow.GetChild(i).gameObject.activeSelf)
                slotCount++;
        }
        if (slotCount <= 0) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(slotsRow);

        float rowHeight = slotsRow.rect.height;
        if (rowHeight < 120f) return;

        var vlg = slotsRow.GetComponent<VerticalLayoutGroup>();
        float spacing = vlg != null ? vlg.spacing : HudLayoutConstants.SectionSpacing;
        float padding = vlg != null ? vlg.padding.top + vlg.padding.bottom : 28f;

        float perCard = HudLayoutConstants.OfferCardBaseHeight;

        for (int i = 0; i < slotsRow.childCount; i++)
        {
            var card = slotsRow.GetChild(i) as RectTransform;
            if (card == null) continue;

            var le = card.GetComponent<LayoutElement>() ?? card.gameObject.AddComponent<LayoutElement>();
            le.flexibleHeight = 0f;
            le.preferredHeight = perCard;
            le.minHeight = perCard;

            if (!card.name.Contains("Empty"))
            {
                MovieOfferCardLayoutBuilder.ApplyCompactOfferLayout(card);
                MovieOfferCardLayoutBuilder.ApplyExpandedLayout(card, perCard);
                var ui = card.GetComponent<MovieButtonUI>();
                MovieOfferCardLayoutBuilder.ApplyRarityIconLayout(card, ui?.movieConfig?.rarity ?? MovieRarity.Common,
                    ui?.movieConfig?.genre ?? MovieGenre.Drama);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(slotsRow);
    }

    MovieButtonUI CreateSlotCard(MovieConfig cfg, string emptyLabel)
    {
        var cardGo = new GameObject(cfg != null ? "Offer_" + cfg.movieName : "Offer_Empty",
            typeof(RectTransform), typeof(Image));
        cardGo.transform.SetParent(slotsRow, false);
        var le = cardGo.AddComponent<LayoutElement>();
        le.flexibleWidth   = 1f;
        le.flexibleHeight  = 0f;
        le.preferredHeight = MovieOfferCardLayoutBuilder.CardPreferredHeight;
        le.minHeight       = HudLayoutConstants.OfferCardBaseHeight;

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
        ui.rewardText = wire.rewardText;
        ui.xpText = wire.xpText;
        ui.repText = wire.repText;
        ui.badgesText = wire.badgesText;
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

/// <summary>Uniform-random offers from the eligible pool. Rejected offers are not tracked.</summary>
public static class MovieOfferPicker
{
    public const int OfferSlotCount = 3;

    public static List<MovieConfig> PickThree(
        MovieConfig[] allMovies,
        int studioLevel,
        float reputation,
        IReadOnlyCollection<string> completedMovieKeys) =>
        PickMany(allMovies, studioLevel, reputation, completedMovieKeys, OfferSlotCount, null);

    public static List<MovieConfig> PickMany(
        MovieConfig[] allMovies,
        int studioLevel,
        float reputation,
        IReadOnlyCollection<string> completedMovieKeys,
        int count,
        HashSet<string> excludeKeys)
    {
        var result = new List<MovieConfig>(count);
        var exclude = excludeKeys ?? new HashSet<string>();

        for (int i = 0; i < count; i++)
        {
            var pick = PickOne(allMovies, studioLevel, reputation, completedMovieKeys, exclude);
            if (pick == null) break;
            result.Add(pick);
            exclude.Add(pick.name);
        }

        return result;
    }

    public static MovieConfig PickOne(
        MovieConfig[] allMovies,
        int studioLevel,
        float reputation,
        IReadOnlyCollection<string> completedMovieKeys,
        HashSet<string> excludeKeys)
    {
        var eligible = MovieOfferPoolRules.BuildEligiblePool(
            allMovies, studioLevel, reputation, completedMovieKeys);
        if (eligible.Count == 0) return null;

        var candidates = new List<MovieConfig>();
        foreach (var cfg in eligible)
        {
            if (cfg == null) continue;
            if (excludeKeys != null && excludeKeys.Contains(cfg.name)) continue;
            candidates.Add(cfg);
        }

        if (candidates.Count == 0) return null;
        return candidates[Random.Range(0, candidates.Count)];
    }
}
