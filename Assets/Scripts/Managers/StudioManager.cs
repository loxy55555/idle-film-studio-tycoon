using System;

using System.Collections;

using System.Collections.Generic;

using UnityEngine;

using UnityEngine.UI;

using TMPro;



public struct MovieCompletePayload
{
    public string movieName;
    public long   moneyReward;
    public float  repGain;
    public float  xpGain;
    public float  varietyBonusPercent;
    public MovieConfig config;
    public bool isFirstDiscovery;
}

public struct ProductionSlotSnapshot
{
    public bool      isActive;
    public string    movieKey;
    public string    movieName;
    public MovieGenre genre;
    public float     progress01;
    public float     timeLeftSeconds;
    public long      rewardMoney;
    public float     rewardRep;
    public bool      awaitingDiscovery;
}



[System.Serializable]

public struct MovieHistoryEntry

{

    public string movieName;

    public long   moneyReward;

    public float  repGain;

    public float  xpGain;

}



class ActiveProduction

{

    public MovieConfig config;

    public ProductionBudget budget = ProductionBudget.Standard;

    public float elapsed;

    public float totalDuration;

    public long  pendingReward;

    public float pendingRep;

    public float pendingXp;

    public float varietyBonusPercent;

    public bool awaitingDiscovery;

}



public class StudioManager : MonoBehaviour

{

    [Header("Economy")]

    public long Money;



    /// <summary>Money including fractional passive-income accumulation (for smooth UI).</summary>

    public double MoneyExact { get; private set; }



    public event Action<long> OnMoneyChanged;

    public event Action<double> OnMoneyDisplayChanged;

    public event Action<long> OnIncomeRateChanged;

    public event Action<float> OnReputationChanged;

    public event Action<MovieCompletePayload> OnMovieCompleted;

    public event Action OnMovieHistoryChanged;

    public event Action OnProductionsChanged;



    public IReadOnlyList<MovieHistoryEntry> MovieHistory => _movieHistory;



    private readonly List<MovieHistoryEntry> _movieHistory = new();
    private readonly List<ActiveProduction>  _productions = new();
    private readonly Dictionary<string, MovieConfig> _movieCatalog = new();
    private readonly List<string> _recentProductionKeys = new();
    private readonly HashSet<string> _completedMovieKeys = new();

    public const int RecentProductionMemory = 5;
    public const float MaxOfflineSeconds = 8f * 3600f;
    const int VarietyLookback = 3;
    const float VarietyBonusPerGenre = 0.05f;

    public IReadOnlyList<string> RecentProductionKeys => _recentProductionKeys;
    public IReadOnlyCollection<string> CompletedMovieKeys => _completedMovieKeys;



    [Header("UI — Production")]

    public TextMeshProUGUI movieStatusText;

    public Slider movieProgressBar;



    [Header("Production")]

    private int currentMovies = 0;

    public int MaxMovieSlots => maxMovieSlots;

    public int ActiveProductionCount => _productions.Count;

    public IEnumerable<MovieConfig> GetActiveProductionConfigs() => GetActiveMovieConfigs();

    public int UsedProductionSlotUnits => LegendaryProductionRules.GetUsedSlotUnits(GetActiveMovieConfigs());

    public int AvailableProductionSlotUnits => Mathf.Max(0, maxMovieSlots - UsedProductionSlotUnits);

    public int maxMovieSlots = 1;



    [Header("Progression")]

    public float reputation = 0f;



    public bool    IsProducing        { get; private set; }

    public float   ProductionProgress { get; private set; }

    public float   ProductionTimeLeft { get; private set; }

    public string  CurrentMovieName   { get; private set; } = "";

    public long    CurrentMovieReward { get; private set; }

    public float   CurrentMovieRep    { get; private set; }



    private float cachedPassiveIncome;



    private DepartmentSystem  D  => GameHub.Instance?.departments;

    private PrestigeSystem    P  => GameHub.Instance?.prestige;

    private UpgradeSystem     U  => GameHub.Instance?.upgrades;

    private StudioLevelSystem SL => GameHub.Instance?.studioLevel;

    private ContractSystem    CS => GameHub.Instance?.contracts;



    public void Initialize()

    {

        StopAllCoroutines();

        _productions.Clear();

        _movieHistory.Clear();
        _recentProductionKeys.Clear();
        currentMovies = 0;



        Money = 1500;

        MoneyExact = Money;

        reputation = 5f;

        cachedPassiveIncome = 0f;



        ClearProductionUI();

        if (U != null) maxMovieSlots = U.GetMaxMovieSlots();

        NotifyAll();

    }



    public void SyncMoneyExact() => MoneyExact = Money;



    public void LoadMoneyExact(double savedExact, long fallbackMoney)

    {

        if (savedExact > 0d)

            MoneyExact = savedExact;

        else

            MoneyExact = fallbackMoney;



        Money = (long)Math.Floor(MoneyExact);

    }



    public void NotifyAll()

    {

        OnMoneyChanged?.Invoke(Money);

        OnMoneyDisplayChanged?.Invoke(MoneyExact);

        OnReputationChanged?.Invoke(reputation);

        RecalculateIncome();

        RefreshProductionUI();

    }



    private void Update()

    {

        if (cachedPassiveIncome <= 0f) return;



        MoneyExact += cachedPassiveIncome * Time.deltaTime;

        long floored = (long)Math.Floor(MoneyExact);

        if (floored != Money)

        {

            Money = floored;

            OnMoneyChanged?.Invoke(Money);

        }

        OnMoneyDisplayChanged?.Invoke(MoneyExact);

    }



    /// <summary>

    /// income/s = reputation^1.25 × quality × cityMult + PassiveIncomeBonus (flat $/s from upgrades)

    /// </summary>

    public void RecalculateIncome()

    {

        float quality   = D != null ? D.CalculateQuality() : 1f;
        float cityMult  = GameHub.Instance?.city != null ? GameHub.Instance.city.GlobalMultiplier : 1f;

        float repPower     = Mathf.Pow(Mathf.Max(reputation, 0f), 1.25f);

        float flatBonus    = U != null ? U.TotalEffect(UpgradeEffectType.PassiveIncomeBonus) : 0f;



        float repIncome = repPower * quality * cityMult;

        cachedPassiveIncome = repIncome + flatBonus;



        U?.LogEffects("RecalculateIncome");

        OnIncomeRateChanged?.Invoke((long)cachedPassiveIncome);

    }



    public void ApplyOfflinePassiveIncome(float offlineSeconds)
    {
        if (offlineSeconds <= 0f) return;

        offlineSeconds = Mathf.Min(offlineSeconds, MaxOfflineSeconds);
        RecalculateIncome();

        if (cachedPassiveIncome <= 0f) return;

        double gain = cachedPassiveIncome * offlineSeconds;
        MoneyExact += gain;
        Money = (long)Math.Floor(MoneyExact);
        OnMoneyChanged?.Invoke(Money);
        OnMoneyDisplayChanged?.Invoke(MoneyExact);

        Debug.Log($"[StudioManager] Offline passive +${gain:N0} ({offlineSeconds / 3600f:0.##}h capped)");
    }

    public string[] GetRecentProductionSaveData()
    {
        if (_recentProductionKeys.Count == 0) return null;
        return _recentProductionKeys.ToArray();
    }

    public void LoadRecentProductionKeys(string[] keys)
    {
        _recentProductionKeys.Clear();
        if (keys == null) return;
        foreach (var key in keys)
        {
            if (string.IsNullOrEmpty(key)) continue;
            _recentProductionKeys.Add(key);
            if (_recentProductionKeys.Count >= RecentProductionMemory) break;
        }
    }

    float CalculateVarietyMultiplier(MovieConfig config, out float bonusPercent)
    {
        bonusPercent = 0f;
        if (config == null) return 1f;

        var genres = new HashSet<MovieGenre> { config.genre };
        int added = 0;
        foreach (var key in _recentProductionKeys)
        {
            if (added >= VarietyLookback - 1) break;
            if (!_movieCatalog.TryGetValue(key, out var prev) || prev == null) continue;
            genres.Add(prev.genre);
            added++;
        }

        int distinct = genres.Count;
        int bonusSteps = Mathf.Max(0, distinct - 1);
        bonusPercent = bonusSteps * VarietyBonusPerGenre * 100f;
        return 1f + bonusSteps * VarietyBonusPerGenre;
    }

    void TrackRecentProduction(MovieConfig config)
    {
        if (config == null || string.IsNullOrEmpty(config.name)) return;
        _recentProductionKeys.Remove(config.name);
        _recentProductionKeys.Insert(0, config.name);
        while (_recentProductionKeys.Count > RecentProductionMemory)
            _recentProductionKeys.RemoveAt(_recentProductionKeys.Count - 1);
    }

    public bool IsMovieCompleted(MovieConfig config)
    {
        if (config == null || string.IsNullOrEmpty(config.name)) return false;
        return _completedMovieKeys.Contains(config.name);
    }

    bool MarkMovieCompleted(MovieConfig config)
    {
        if (config == null || string.IsNullOrEmpty(config.name)) return false;
        return _completedMovieKeys.Add(config.name);
    }

    public MovieCatalogProgress.Snapshot GetMovieCatalogSnapshot(MovieConfig[] allMovies)
    {
        return MovieCatalogProgress.Evaluate(
            allMovies,
            SL?.Level ?? 1,
            reputation,
            _completedMovieKeys);
    }

    public string[] GetCompletedMovieSaveData()
    {
        if (_completedMovieKeys.Count == 0) return null;
        var arr = new string[_completedMovieKeys.Count];
        _completedMovieKeys.CopyTo(arr);
        return arr;
    }

    public void LoadCompletedMovieKeys(string[] keys)
    {
        _completedMovieKeys.Clear();
        if (keys == null) return;
        foreach (var key in keys)
        {
            if (string.IsNullOrEmpty(key)) continue;
            _completedMovieKeys.Add(key);
        }
    }

    /// <summary>Backfills completed keys from history when loading older saves.</summary>
    public void MigrateCompletedFromMovieHistory(MovieHistorySaveEntry[] history, string[] completedKeys)
    {
        if (completedKeys != null && completedKeys.Length > 0) return;
        if (history == null || history.Length == 0) return;

        EnsureMovieCatalog();
        foreach (var entry in history)
        {
            if (entry == null || string.IsNullOrEmpty(entry.movieName)) continue;
            foreach (var pair in _movieCatalog)
            {
                if (pair.Value != null && pair.Value.movieName == entry.movieName)
                    _completedMovieKeys.Add(pair.Key);
            }
        }
    }

    public float GetPreviewVarietyBonusPercent(MovieConfig config)
    {
        EnsureMovieCatalog();
        CalculateVarietyMultiplier(config, out float bonusPercent);
        return bonusPercent;
    }

    public long CurrentIncome => (long)cachedPassiveIncome;

    public IReadOnlyList<ProductionSlotSnapshot> GetProductionSnapshots()
    {
        var list = new List<ProductionSlotSnapshot>(_productions.Count);
        foreach (var p in _productions)
        {
            if (p.config == null) continue;
            float total = Mathf.Max(p.totalDuration, 0.01f);
            list.Add(new ProductionSlotSnapshot
            {
                isActive         = true,
                movieKey         = p.config.name,
                movieName        = p.config.movieName,
                genre            = p.config.genre,
                progress01       = p.awaitingDiscovery ? 1f : Mathf.Clamp01(p.elapsed / total),
                timeLeftSeconds  = p.awaitingDiscovery ? 0f : Mathf.Max(0f, total - p.elapsed),
                rewardMoney      = p.pendingReward,
                rewardRep        = p.pendingRep,
                awaitingDiscovery = p.awaitingDiscovery,
            });
        }
        return list;
    }

    void NotifyProductionsChanged() => OnProductionsChanged?.Invoke();

    void NotifyProductionStarted(MovieConfig config)
    {
        EnsureMovieCatalog();
        var catalog = MovieCatalogRuntime.AllMovies;
        MovieOfferState.NotifyProductionStarted(
            config,
            catalog,
            SL?.Level ?? 1,
            reputation,
            _completedMovieKeys,
            GetActiveMovieConfigs());
    }



    public bool CanStartMovie(MovieConfig config, out string blockReason)
    {
        blockReason = null;
        if (config == null) return false;

        int used = UsedProductionSlotUnits;
        return LegendaryProductionRules.CanStart(config, maxMovieSlots, used, out blockReason);
    }

    public void StartMovie(MovieConfig config, ProductionBudget budget = ProductionBudget.Standard)

    {

        if (config == null) return;



        if (!CanStartMovie(config, out var blockReason))

        {

            SetStatus(blockReason ?? "Slot de producción ocupado");

            return;

        }



        float cost = config.cost * (1f - D.CalculateCostReduction());



        if (!TrySpendMoney((long)cost))

        {

            SetStatus("Dinero insuficiente");

            return;

        }



        var production = CreateProductionState(config, budget);

        _productions.Add(production);

        currentMovies = _productions.Count;

        UpdateProductionFlags();
        NotifyProductionStarted(config);
        NotifyProductionsChanged();
        GameHub.Instance?.save?.Save("StartMovie");

        StartCoroutine(ProductionRoutine(production));

    }



    IEnumerable<MovieConfig> GetActiveMovieConfigs()
    {
        foreach (var production in _productions)
        {
            if (production?.config != null)
                yield return production.config;
        }
    }

    ActiveProduction CreateProductionState(MovieConfig config, ProductionBudget budget = ProductionBudget.Standard)

    {

        float studioQuality = D.CalculateQuality();

        float speed         = D.CalculateSpeed();

        float duration      = config.duration / speed;

        long  reward        = (long)(config.baseReward * config.quality * studioQuality);



        float repMult = 1f + (U?.TotalEffect(UpgradeEffectType.ReputationBonus) ?? 0f);

        float repGain = config.baseRep * studioQuality * repMult;

        float xp      = StudioLevelSystem.CalculateMovieXP(config, U);

        ProductionBudgetRules.Apply(ref duration, ref reward, ref repGain, budget);



        return new ActiveProduction

        {

            config         = config,

            budget         = budget,

            elapsed        = 0f,

            totalDuration  = duration,

            pendingReward  = reward,

            pendingRep     = repGain,

            pendingXp      = xp,

        };

    }



    IEnumerator ProductionRoutine(ActiveProduction production)

    {

        SetStatus("Produciendo: " + production.config.movieName);



        while (production.elapsed < production.totalDuration)

        {

            production.elapsed += Time.deltaTime;

            RefreshProductionUI();

            yield return null;

        }



        FinalizeProduction(production, production.totalDuration);

    }



    void FinalizeProduction(ActiveProduction production, float durationUsed)
    {
        if (production == null || production.config == null) return;
        if (production.awaitingDiscovery) return;

        EnsureMovieCatalog();
        float varietyMult = CalculateVarietyMultiplier(production.config, out float varietyBonusPct);
        production.pendingReward = (long)(production.pendingReward * varietyMult);
        production.pendingRep *= varietyMult;
        production.varietyBonusPercent = varietyBonusPct;
        production.elapsed = production.totalDuration;
        production.awaitingDiscovery = true;

        TrackRecentProduction(production.config);
        UpdateProductionFlags();
        RefreshProductionUI();
        SetStatus(Loc.Get(LocKeys.ProdStatusComplete));
        GameHub.Instance?.save?.Save("ProductionAwaitingDiscovery");
    }

    ActiveProduction FindAwaitingProduction(string movieKey)
    {
        if (string.IsNullOrEmpty(movieKey)) return null;
        foreach (var p in _productions)
        {
            if (p.awaitingDiscovery && p.config != null && p.config.name == movieKey)
                return p;
        }
        return null;
    }

    public bool TryPrepareDiscovery(string movieKey, out MovieCompletePayload payload)
    {
        payload = default;
        var p = FindAwaitingProduction(movieKey);
        if (p == null || p.config == null) return false;

        payload = new MovieCompletePayload
        {
            movieName           = p.config.movieName,
            moneyReward         = p.pendingReward,
            repGain             = p.pendingRep,
            xpGain              = p.pendingXp,
            varietyBonusPercent = p.varietyBonusPercent,
            config              = p.config,
            isFirstDiscovery    = !IsMovieCompleted(p.config),
        };
        return true;
    }

    public void FinalizeDiscovery(string movieKey)
    {
        var p = FindAwaitingProduction(movieKey);
        if (p == null || p.config == null) return;

        long  reward          = p.pendingReward;
        float rep             = p.pendingRep;
        float xp              = p.pendingXp;
        float varietyBonusPct = p.varietyBonusPercent;
        float durationUsed    = p.totalDuration;

        AddMoney(reward);
        reputation += rep;
        OnReputationChanged?.Invoke(reputation);
        RecalculateIncome();
        SL?.AddXP(xp);

        float studioQuality = D != null ? D.CalculateQuality() : 1f;
        CS?.OnMovieCompleted(p.config, studioQuality, durationUsed, reward);
        CS?.OnReputationChanged(reputation);

        _productions.Remove(p);
        currentMovies = _productions.Count;
        if (U != null) maxMovieSlots = U.GetMaxMovieSlots();

        bool isFirstDiscovery = MarkMovieCompleted(p.config);

        OnMovieCompleted?.Invoke(new MovieCompletePayload
        {
            movieName           = p.config.movieName,
            moneyReward         = reward,
            repGain             = rep,
            xpGain              = xp,
            varietyBonusPercent = varietyBonusPct,
            config              = p.config,
            isFirstDiscovery    = isFirstDiscovery,
        });

        RecordHistory(p.config.movieName, reward, rep, xp);
        UpdateProductionFlags();
        RefreshProductionUI();
        SetStatus(string.Format("{0} descubierta! +${1:N0}  +{2:0.0} REP",
            p.config.movieName, reward, rep));
        GameHub.Instance?.save?.Save("MovieDiscovered");
    }



    public void ResumeProductions(ProductionSaveEntry[] entries, float offlineSeconds)

    {

        StopAllCoroutines();

        _productions.Clear();

        currentMovies = 0;

        ClearProductionUI();



        if (entries == null || entries.Length == 0)

        {

            UpdateProductionFlags();

            return;

        }



        EnsureMovieCatalog();



        foreach (var entry in entries)

        {

            if (string.IsNullOrEmpty(entry.movieKey)) continue;



            if (!_movieCatalog.TryGetValue(entry.movieKey, out var config))

            {

                Debug.LogWarning("[StudioManager] Missing movie for save key: " + entry.movieKey);

                continue;

            }



            float elapsed = entry.elapsedSeconds + offlineSeconds;

            float total   = entry.totalDuration > 0f ? entry.totalDuration : entry.elapsedSeconds;



            var production = new ActiveProduction

            {

                config        = config,

                elapsed       = elapsed,

                totalDuration = total,

                pendingReward = entry.pendingReward,

                pendingRep    = entry.pendingRep,

                pendingXp     = entry.pendingXp,

            };



            if (elapsed >= total)

            {

                production.elapsed = total;

                production.totalDuration = total;

                if (entry.awaitingDiscovery)

                {

                    production.awaitingDiscovery = true;

                    production.pendingReward = entry.pendingReward;

                    production.pendingRep = entry.pendingRep;

                    production.pendingXp = entry.pendingXp;

                    _productions.Add(production);

                    currentMovies++;

                    continue;

                }



                FinalizeProduction(production, total);

                _productions.Add(production);

                currentMovies++;

                continue;

            }



            production.elapsed = elapsed;

            _productions.Add(production);

            currentMovies++;

            StartCoroutine(ProductionRoutine(production));

        }



        if (U != null) maxMovieSlots = U.GetMaxMovieSlots();

        UpdateProductionFlags();

        RefreshProductionUI();

    }



    public ProductionSaveEntry[] GetProductionSaveData()

    {

        if (_productions.Count == 0) return null;



        var list = new List<ProductionSaveEntry>();

        foreach (var p in _productions)

        {

            if (p.config == null) continue;

            list.Add(new ProductionSaveEntry

            {

                movieKey        = p.config.name,

                elapsedSeconds  = p.elapsed,

                totalDuration   = p.totalDuration,

                pendingReward   = p.pendingReward,

                pendingRep      = p.pendingRep,

                pendingXp       = p.pendingXp,

                awaitingDiscovery = p.awaitingDiscovery,

            });

        }

        return list.Count > 0 ? list.ToArray() : null;

    }



    public MovieHistorySaveEntry[] GetMovieHistorySaveData()

    {

        if (_movieHistory.Count == 0) return null;



        var list = new List<MovieHistorySaveEntry>();

        foreach (var e in _movieHistory)

        {

            list.Add(new MovieHistorySaveEntry

            {

                movieName   = e.movieName,

                moneyReward = e.moneyReward,

                repGain     = e.repGain,

                xpGain      = e.xpGain,

            });

        }

        return list.ToArray();

    }



    public void LoadMovieHistory(MovieHistorySaveEntry[] entries)

    {

        _movieHistory.Clear();

        if (entries == null) return;



        foreach (var e in entries)

        {

            _movieHistory.Add(new MovieHistoryEntry

            {

                movieName   = e.movieName,

                moneyReward = e.moneyReward,

                repGain     = e.repGain,

                xpGain      = e.xpGain,

            });

        }



        while (_movieHistory.Count > 40)

            _movieHistory.RemoveAt(_movieHistory.Count - 1);



        OnMovieHistoryChanged?.Invoke();

    }



    void EnsureMovieCatalog()

    {

        if (_movieCatalog.Count > 0) return;



        foreach (var m in MovieCatalogRuntime.AllMovies)

        {

            if (m != null && !_movieCatalog.ContainsKey(m.name))

                _movieCatalog[m.name] = m;

        }

    }



    void UpdateProductionFlags()

    {

        IsProducing = _productions.Count > 0;

        if (!IsProducing)

        {

            ProductionProgress = 0f;

            ProductionTimeLeft = 0f;

            CurrentMovieName   = "";

            CurrentMovieReward = 0;

            CurrentMovieRep    = 0f;

        }

    }



    void RefreshProductionUI()

    {

        if (_productions.Count == 0)

        {

            ClearProductionUI();

            return;

        }



        var latest = _productions[_productions.Count - 1];

        CurrentMovieName   = latest.config != null ? latest.config.movieName : "";

        CurrentMovieReward = latest.pendingReward;

        CurrentMovieRep    = latest.pendingRep;



        float total = Mathf.Max(latest.totalDuration, 0.01f);

        ProductionProgress = Mathf.Clamp01(latest.elapsed / total);

        ProductionTimeLeft = Mathf.Max(0f, total - latest.elapsed);



        if (movieProgressBar != null)

            movieProgressBar.value = ProductionProgress;

        NotifyProductionsChanged();

    }



    void ClearProductionUI()

    {

        if (movieProgressBar != null)

            movieProgressBar.value = 0f;

        NotifyProductionsChanged();

    }



    void RecordHistory(string name, long money, float rep, float xp)

    {

        _movieHistory.Insert(0, new MovieHistoryEntry

        {

            movieName   = name,

            moneyReward = money,

            repGain     = rep,

            xpGain      = xp,

        });

        if (_movieHistory.Count > 40)

            _movieHistory.RemoveAt(_movieHistory.Count - 1);

        OnMovieHistoryChanged?.Invoke();

    }



    public void AddReputation(float amount)

    {

        reputation += amount;

        OnReputationChanged?.Invoke(reputation);

        RecalculateIncome();

    }



    public void AddMoney(long amount)

    {

        MoneyExact += amount;

        Money = (long)Math.Floor(MoneyExact);

        OnMoneyChanged?.Invoke(Money);

        OnMoneyDisplayChanged?.Invoke(MoneyExact);

    }



    public bool TrySpendMoney(long amount)

    {

        if (MoneyExact < amount) return false;

        MoneyExact -= amount;

        Money = (long)Math.Floor(MoneyExact);

        OnMoneyChanged?.Invoke(Money);

        OnMoneyDisplayChanged?.Invoke(MoneyExact);

        return true;

    }



    private void SetStatus(string msg)

    {

        if (movieStatusText != null)

            movieStatusText.text = msg;

    }

}


