using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player-chosen contracts: 3 candidates OR 1 active contract (Phase 8.0).
/// </summary>
public class ContractSystem : MonoBehaviour
{
    const string AllGenresContractId = "c_all_genres";
    public const int CandidateSlotCount = 3;
    public const int MaxActiveContracts = 1;

    [Header("Contract Database")]
    public ContractConfig[] allContracts;

    [Tooltip("Legacy inspector field — always 1 active contract in Phase 8.0")]
    public int maxActiveContracts = MaxActiveContracts;

    public event Action OnContractUpdated;
    public event Action<ContractConfig> OnContractCompleted;
    public event Action<ContractConfig> OnContractClaimed;
    public event Action<ContractConfig> OnContractSelected;

    readonly List<ContractConfig>      _active          = new();
    readonly List<ContractConfig>      _candidates      = new();
    readonly Dictionary<string, float> _progress        = new();
    readonly Dictionary<string, int>   _genreMask       = new();
    readonly HashSet<string>           _readyToClaim    = new();
    readonly HashSet<string>           _permanentlyDone = new();
    readonly List<ContractConfig>      _history         = new();

    public IReadOnlyList<ContractConfig> ActiveContracts    => _active;
    public IReadOnlyList<ContractConfig> CandidateContracts => _candidates;
    public IReadOnlyList<ContractConfig> HistoryContracts  => _history;

    public bool HasActiveContract => _active.Count > 0;
    public bool IsSelectionMode   => !HasActiveContract && _candidates.Count > 0;

    public struct RecoverySnapshot
    {
        public bool triggered;
        public string trigger;
        public int activeBefore;
        public int activeAfter;
        public int maxActive;
    }

    public static RecoverySnapshot LastRecovery { get; private set; }

    public int ActiveCount => _active.Count;

    public void Init(int studioLevel)
    {
        _active.Clear();
        _candidates.Clear();
        _progress.Clear();
        _genreMask.Clear();
        _readyToClaim.Clear();
        _history.Clear();
        GenerateCandidates(studioLevel);
    }

    public RecoverySnapshot EnsureActiveContracts(int studioLevel)
    {
        int before = HasActiveContract ? 1 : _candidates.Count;
        bool triggered = false;
        string trigger = "None";

        if (HasActiveContract)
        {
            trigger = "ActiveLocked";
        }
        else if (_candidates.Count == 0)
        {
            GenerateCandidates(studioLevel);
            triggered = true;
            trigger = "EmptyCandidates";
        }
        else if (_candidates.Count < CandidateSlotCount)
        {
            TopUpCandidates(studioLevel);
            triggered = true;
            trigger = "TopUpCandidates";
        }

        int after = HasActiveContract ? 1 : _candidates.Count;
        var snapshot = new RecoverySnapshot
        {
            activeBefore = before,
            activeAfter = after,
            maxActive = MaxActiveContracts,
            triggered = triggered,
            trigger = trigger,
        };
        LastRecovery = snapshot;

        if (triggered && after > before)
        {
            Debug.Log(
                $"[ContractSystem] Contract recovery — {before} → {after} " +
                $"(mode={(HasActiveContract ? "active" : "selection")}, trigger={trigger}).");
        }
        else if (triggered && after == 0 && !HasActiveContract)
        {
            Debug.LogWarning(
                $"[ContractSystem] Contract recovery found 0 eligible candidates at studio level {studioLevel}.");
        }

        return snapshot;
    }

    /// <summary>Legacy entry point — tops up candidate pool when no active contract.</summary>
    public void RefreshContracts(int studioLevel)
    {
        if (HasActiveContract) return;

        if (_candidates.Count == 0)
            GenerateCandidates(studioLevel);
        else
            TopUpCandidates(studioLevel);

        OnContractUpdated?.Invoke();
    }

    public void ForceRefreshCandidates(int studioLevel)
    {
        if (HasActiveContract) return;

        _candidates.Clear();
        GenerateCandidates(studioLevel);
        OnContractUpdated?.Invoke();
    }

    /// <summary>
    /// Phase 13.4D — Reroll a single candidate contract without touching the others.
    /// Replaces <paramref name="old"/> with a new random eligible contract drawn from
    /// a pool that excludes every currently visible candidate and the old entry itself.
    /// Returns false if there are no alternatives available.
    /// </summary>
    public bool RerollCandidate(ContractConfig old, int studioLevel)
    {
        if (old == null || HasActiveContract) return false;
        int idx = _candidates.IndexOf(old);
        if (idx < 0) return false;

        // Build exclusion set: all current candidates + old itself
        var exclude = new HashSet<string>();
        foreach (var c in _candidates) exclude.Add(c.id);
        foreach (var c in _active) exclude.Add(c.id);
        foreach (var c in _history) exclude.Add(c.id);
        foreach (var id in _permanentlyDone) exclude.Add(id);
        foreach (var id in _readyToClaim) exclude.Add(id);

        // Old candidate is already in exclude; try to find a replacement
        var eligible = new List<ContractConfig>();
        foreach (var c in allContracts)
        {
            if (c == null || exclude.Contains(c.id)) continue;
            if (!c.repeatable && _permanentlyDone.Contains(c.id)) continue;
            if (studioLevel < c.unlockStudioLevel) continue;
            if (GameHub.Instance?.city != null && !GameHub.Instance.city.IsContractUnlocked(c)) continue;
            eligible.Add(c);
        }

        if (eligible.Count == 0)
        {
            Debug.LogWarning("[ContractSystem] Reroll: no alternative contracts available.");
            return false;
        }

        // Genre-balanced reroll: prefer a different genre from existing candidates
        var existingGenres = new HashSet<MovieGenre>();
        foreach (var c in _candidates)
            if (c.goalType == ContractGoalType.ProduceMoviesByGenre)
                existingGenres.Add(c.targetGenre);

        var differentGenre = eligible.FindAll(c =>
            c.goalType != ContractGoalType.ProduceMoviesByGenre ||
            !existingGenres.Contains(c.targetGenre));

        var pool = differentGenre.Count > 0 ? differentGenre : eligible;
        var replacement = pool[UnityEngine.Random.Range(0, pool.Count)];
        _candidates[idx] = replacement;

        Debug.Log($"[ContractSystem] Rerolled [{old.contractTitle}] → [{replacement.contractTitle}]");
        GameHub.Instance?.save?.Save("ContractReroll");
        try { OnContractUpdated?.Invoke(); }
        catch (System.Exception ex) { Debug.LogError($"[ContractSystem] Reroll UI event error: {ex}"); }
        return true;
    }

    public bool SelectCandidate(ContractConfig contract)
    {
        if (contract == null || HasActiveContract) return false;
        if (!_candidates.Contains(contract)) return false;

        _candidates.Clear();
        _active.Add(contract);
        if (!_progress.ContainsKey(contract.id))
            _progress[contract.id] = 0f;

        Debug.Log($"[ContractSystem] Selected contract: {contract.contractTitle}");
        GameHub.Instance?.save?.Save("ContractSelected");
        try
        {
            OnContractSelected?.Invoke(contract);
            OnContractUpdated?.Invoke();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ContractSystem] Excepción en evento UI selección contrato (save ya guardado): {ex}");
        }
        return true;
    }

    /// <summary>
    /// FASE 16.1 — D3: Cancels the current active contract without reward.
    /// Generates three fresh candidates. Use only after the player watches an ad.
    /// </summary>
    public bool CancelActiveContract(int studioLevel)
    {
        if (!HasActiveContract) return false;

        var cancelled = _active[0];
        _active.Clear();
        _progress.Remove(cancelled.id);
        _readyToClaim.Remove(cancelled.id);

        _candidates.Clear();
        GenerateCandidates(studioLevel);

        Debug.Log($"[ContractSystem] Active contract '{cancelled.contractTitle}' cancelled via ad.");
        GameHub.Instance?.save?.Save("ContractCancelledViaAd");
        try
        {
            OnContractUpdated?.Invoke();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ContractSystem] Excepción en evento UI cancelación contrato (save ya guardado): {ex}");
        }
        return true;
    }

    public float GetProgress(ContractConfig c) =>
        _progress.TryGetValue(c.id, out var p) ? p : 0f;

    public bool IsReadyToClaim(ContractConfig c) =>
        c != null && _readyToClaim.Contains(c.id);

    public bool IsActive(ContractConfig c) =>
        c != null && _active.Contains(c);

    public bool IsCandidate(ContractConfig c) =>
        c != null && _candidates.Contains(c);

    public bool DoesMovieHelpActiveContract(MovieConfig movie, DepartmentSystem depts = null)
    {
        if (movie == null || !HasActiveContract) return false;

        var c = _active[0];
        if (_readyToClaim.Contains(c.id)) return false;

        switch (c.goalType)
        {
            case ContractGoalType.ProduceMovies:
            case ContractGoalType.EarnMoney:
                return true;
            case ContractGoalType.ProduceMoviesByGenre:
                return movie.genre == c.targetGenre;
            case ContractGoalType.ReachQuality:
                if (depts == null) return true;
                return movie.quality * depts.CalculateQuality() >= c.goalAmount;
            case ContractGoalType.ProduceMoviesUnderTime:
                if (depts == null) return true;
                return (movie.duration / Mathf.Max(0.01f, depts.CalculateSpeed())) <= c.goalAmount;
            default:
                return false;
        }
    }

    /// <summary>
    /// Returns the localized display title for a contract.
    /// Uses the <c>contract.title.{id}</c> key from the localization table,
    /// falling back to the baked asset field when no translation exists.
    /// </summary>
    public static string GetLocalizedTitle(ContractConfig cfg)
    {
        if (cfg == null) return string.Empty;
        var key = LocKeys.ContractTitlePfx + cfg.id;
        var loc = Loc.Get(key);
        return loc == key ? cfg.contractTitle : loc;
    }

    /// <summary>
    /// Returns the localized description for a contract.
    /// Uses the <c>contract.description.{id}</c> key from the localization table,
    /// falling back to the baked asset field when no translation exists.
    /// Once translation entries are added to Loc.cs the description will localize automatically.
    /// </summary>
    public static string GetLocalizedDescription(ContractConfig cfg)
    {
        if (cfg == null) return string.Empty;
        var key = LocKeys.ContractDescriptionPfx + cfg.id;
        var loc = Loc.Get(key);
        return loc == key ? cfg.description : loc;
    }

    public static string BuildObjectiveLabel(ContractConfig c)
    {
        if (c == null) return string.Empty;

        return c.goalType switch
        {
            ContractGoalType.ProduceMovies          => Loc.Format(LocKeys.ContractObjProduceMovies, c.goalAmount),
            ContractGoalType.ProduceMoviesByGenre   => Loc.Format(LocKeys.ContractObjProduceGenre, c.goalAmount, GenreLoc.GetLabel(c.targetGenre)),
            ContractGoalType.ReachReputation        => Loc.Format(LocKeys.ContractObjReachRep, c.goalAmount),
            ContractGoalType.ReachQuality           => Loc.Format(LocKeys.ContractObjReachQuality, c.goalAmount),
            ContractGoalType.EarnMoney              => Loc.Format(LocKeys.ContractObjEarnMoney, c.goalAmount),
            ContractGoalType.SpendOnUpgrades        => Loc.Format(LocKeys.ContractObjSpendUpgrades, c.goalAmount),
            ContractGoalType.ReachStudioLevel       => Loc.Format(LocKeys.ContractObjReachLevel, c.goalAmount),
            ContractGoalType.ProduceMoviesUnderTime => Loc.Format(LocKeys.ContractObjUnderTime, c.goalAmount),
            _                                       => c.description,
        };
    }

    public void OnMovieCompleted(MovieConfig movie, float quality, float duration, long movieReward)
    {
        if (!HasActiveContract) return;

        bool anyUpdated = false;
        foreach (var c in _active)
        {
            if (_readyToClaim.Contains(c.id)) continue;

            if (c.id == AllGenresContractId)
            {
                if (TrackUniqueGenre(c, movie.genre))
                    anyUpdated = true;
                continue;
            }

            switch (c.goalType)
            {
                case ContractGoalType.ProduceMovies:
                    _progress[c.id] = GetProgress(c) + 1;
                    anyUpdated = true;
                    break;
                case ContractGoalType.ProduceMoviesByGenre:
                    if (movie.genre == c.targetGenre)
                    { _progress[c.id] = GetProgress(c) + 1; anyUpdated = true; }
                    break;
                case ContractGoalType.ReachQuality:
                    if (quality >= c.goalAmount)
                    { _progress[c.id] = c.goalAmount; anyUpdated = true; }
                    break;
                case ContractGoalType.ProduceMoviesUnderTime:
                    if (duration <= c.goalAmount)
                    { _progress[c.id] = GetProgress(c) + 1; anyUpdated = true; }
                    break;
                case ContractGoalType.EarnMoney:
                    _progress[c.id] = GetProgress(c) + movieReward;
                    anyUpdated = true;
                    break;
            }
        }
        if (anyUpdated) CheckCompletions();
    }

    bool TrackUniqueGenre(ContractConfig c, MovieGenre genre)
    {
        int bit = 1 << (int)genre;
        if (!_genreMask.TryGetValue(c.id, out int mask))
            mask = 0;

        if ((mask & bit) != 0) return false;

        mask |= bit;
        _genreMask[c.id] = mask;
        _progress[c.id]  = PopCount(mask);
        return true;
    }

    static int PopCount(int value)
    {
        int count = 0;
        while (value != 0)
        {
            count += value & 1;
            value >>= 1;
        }
        return count;
    }

    public void OnReputationChanged(float rep)
    {
        if (!HasActiveContract) return;

        foreach (var c in _active)
        {
            if (c.goalType != ContractGoalType.ReachReputation) continue;
            if (_readyToClaim.Contains(c.id)) continue;
            if (rep >= c.goalAmount)
            {
                _progress[c.id] = c.goalAmount;
                CheckCompletions();
                return;
            }
        }
    }

    public void OnMoneySpentOnUpgrade(long amount)
    {
        if (!HasActiveContract) return;

        foreach (var c in _active)
        {
            if (c.goalType != ContractGoalType.SpendOnUpgrades) continue;
            if (_readyToClaim.Contains(c.id)) continue;
            _progress[c.id] = GetProgress(c) + amount;
            CheckCompletions();
        }
    }

    public void OnStudioLevelUp(int newLevel)
    {
        if (!HasActiveContract) return;

        foreach (var c in _active)
        {
            if (c.goalType != ContractGoalType.ReachStudioLevel) continue;
            if (_readyToClaim.Contains(c.id)) continue;
            if (newLevel >= c.goalAmount)
            {
                _progress[c.id] = newLevel;
                CheckCompletions();
            }
        }
    }

    public bool ClaimReward(ContractConfig contract, StudioManager studio, StudioLevelSystem studioLevel)
    {
        if (contract == null || !_readyToClaim.Contains(contract.id)) return false;
        if (!_active.Contains(contract)) return false;

        // ── DATA CHANGES ────────────────────────────────────────────────────────
        studio.AddMoney(contract.rewardMoney);
        studio.AddReputation(contract.rewardReputation);
        studioLevel?.AddXP(contract.rewardStudioXP);

        if (contract.rewardDiamonds > 0)
            GameHub.Instance?.diamonds?.Add(contract.rewardDiamonds);

        _active.Remove(contract);
        _readyToClaim.Remove(contract.id);
        _progress.Remove(contract.id);
        _genreMask.Remove(contract.id);

        if (!contract.repeatable)
            _permanentlyDone.Add(contract.id);

        _history.Insert(0, contract);
        if (_history.Count > 10) _history.RemoveAt(_history.Count - 1);

        GenerateCandidates(studioLevel?.Level ?? 1);

        // ── SAVE (before UI events) ─────────────────────────────────────────────
        GameHub.Instance?.save?.Save("ContractClaimed");

        // ── UI EVENTS ───────────────────────────────────────────────────────────
        try
        {
            OnContractClaimed?.Invoke(contract);
            OnContractUpdated?.Invoke();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ContractSystem] Excepción en evento UI de contrato (save ya guardado): {ex}");
        }

        return true;
    }

    void CheckCompletions()
    {
        foreach (var c in _active)
        {
            if (_readyToClaim.Contains(c.id)) continue;
            if (GetProgress(c) >= c.goalAmount)
            {
                _readyToClaim.Add(c.id);
                Debug.Log($"[ContractSystem] Ready to claim: {c.contractTitle}");
                OnContractCompleted?.Invoke(c);
            }
        }
        OnContractUpdated?.Invoke();
    }

    public string[] GetCompletedIds() => new List<string>(_permanentlyDone).ToArray();

    public void LoadCompletedIds(string[] ids)
    {
        _permanentlyDone.Clear();
        if (ids != null)
            foreach (var id in ids) _permanentlyDone.Add(id);
    }

    public ContractSaveEntry[] GetSaveData()
    {
        var list = new List<ContractSaveEntry>();

        if (HasActiveContract)
        {
            var c = _active[0];
            list.Add(new ContractSaveEntry
            {
                id               = c.id,
                progress         = GetProgress(c),
                readyToClaim     = _readyToClaim.Contains(c.id),
                genreMask        = _genreMask.TryGetValue(c.id, out var mask) ? mask : 0,
                isActiveContract = true,
            });
            return list.ToArray();
        }

        foreach (var c in _candidates)
        {
            list.Add(new ContractSaveEntry
            {
                id               = c.id,
                progress         = 0f,
                readyToClaim     = false,
                genreMask        = 0,
                isActiveContract = false,
            });
        }

        return list.ToArray();
    }

    public string[] GetHistorySaveIds()
    {
        var ids = new string[_history.Count];
        for (int i = 0; i < _history.Count; i++)
            ids[i] = _history[i].id;
        return ids;
    }

    public void LoadFromSave(ContractSaveEntry[] states, string[] historyIds)
    {
        _active.Clear();
        _candidates.Clear();
        _progress.Clear();
        _genreMask.Clear();
        _readyToClaim.Clear();
        _history.Clear();

        var loadedActive = new List<(ContractConfig cfg, ContractSaveEntry state)>();
        var loadedCandidates = new List<ContractConfig>();

        if (states != null)
        {
            foreach (var s in states)
            {
                if (string.IsNullOrEmpty(s.id)) continue;
                var c = FindContract(s.id);
                if (c == null) continue;

                // isActiveContract is the canonical flag (set in GetSaveData since Phase 8.1).
                // Fall back to progress/readyToClaim for saves written by Phase 8.0 that lack the flag.
                bool isActive = s.isActiveContract || s.progress > 0f || s.readyToClaim;
                if (isActive)
                    loadedActive.Add((c, s));
                else
                    loadedCandidates.Add(c);
            }
        }

        if (loadedActive.Count > 0)
        {
            // Pick the most-progressed entry (or the ready-to-claim one).
            var best = loadedActive[0];
            foreach (var item in loadedActive)
            {
                if (item.state.readyToClaim && !best.state.readyToClaim)
                    best = item;
                else if (item.state.progress > best.state.progress)
                    best = item;
            }

            _active.Add(best.cfg);
            _progress[best.cfg.id] = best.state.progress;
            if (best.state.genreMask != 0)
                _genreMask[best.cfg.id] = best.state.genreMask;
            if (best.state.readyToClaim)
                _readyToClaim.Add(best.cfg.id);
        }
        else
        {
            for (int i = 0; i < loadedCandidates.Count && i < CandidateSlotCount; i++)
                _candidates.Add(loadedCandidates[i]);
        }

        if (historyIds != null)
        {
            foreach (var id in historyIds)
            {
                var c = FindContract(id);
                if (c != null) _history.Add(c);
            }
        }

        OnContractUpdated?.Invoke();
    }

    public void RecoverAfterLoad(int studioLevel) => EnsureActiveContracts(studioLevel);

    void GenerateCandidates(int studioLevel)
    {
        _candidates.Clear();
        FillCandidateSlots(studioLevel, CandidateSlotCount);
    }

    void TopUpCandidates(int studioLevel)
    {
        int missing = CandidateSlotCount - _candidates.Count;
        if (missing <= 0) return;
        FillCandidateSlots(studioLevel, missing);
    }

    void FillCandidateSlots(int studioLevel, int count)
    {
        var exclude = BuildCandidateExcludeSet();
        var eligible = BuildEligiblePool(studioLevel, exclude);

        // Genre-balanced selection: separate genre contracts by genre, shuffle genres,
        // then interleave one contract per genre before falling back to random picks.
        var byGenre = new Dictionary<MovieGenre, List<ContractConfig>>();
        var nonGenre = new List<ContractConfig>();

        foreach (var c in eligible)
        {
            if (c.goalType == ContractGoalType.ProduceMoviesByGenre)
            {
                if (!byGenre.ContainsKey(c.targetGenre))
                    byGenre[c.targetGenre] = new List<ContractConfig>();
                byGenre[c.targetGenre].Add(c);
            }
            else
            {
                nonGenre.Add(c);
            }
        }

        // Shuffle genre keys for variety
        var genreKeys = new List<MovieGenre>(byGenre.Keys);
        for (int i = genreKeys.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (genreKeys[i], genreKeys[j]) = (genreKeys[j], genreKeys[i]);
        }

        // Build a balanced pick order: one per genre (shuffled), then non-genre
        var pickOrder = new List<ContractConfig>();
        foreach (var g in genreKeys)
        {
            var pool = byGenre[g];
            pickOrder.Add(pool[UnityEngine.Random.Range(0, pool.Count)]);
        }

        // Shuffle non-genre pool
        for (int i = nonGenre.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (nonGenre[i], nonGenre[j]) = (nonGenre[j], nonGenre[i]);
        }
        pickOrder.AddRange(nonGenre);

        // Pick `count` candidates from the balanced order (already deduplicated)
        for (int i = 0; i < count && i < pickOrder.Count; i++)
        {
            var pick = pickOrder[i];
            if (exclude.Contains(pick.id)) continue;
            _candidates.Add(pick);
            exclude.Add(pick.id);
        }

        // If we still need more candidates (rare edge case), fall back to remaining eligible
        if (_candidates.Count < count)
        {
            var fallback = new List<ContractConfig>(eligible);
            for (int i = fallback.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (fallback[i], fallback[j]) = (fallback[j], fallback[i]);
            }
            foreach (var c in fallback)
            {
                if (_candidates.Count >= count) break;
                if (exclude.Contains(c.id)) continue;
                _candidates.Add(c);
                exclude.Add(c.id);
            }
        }
    }

    HashSet<string> BuildCandidateExcludeSet()
    {
        var exclude = new HashSet<string>(_permanentlyDone);
        foreach (var c in _active) exclude.Add(c.id);
        foreach (var c in _candidates) exclude.Add(c.id);
        foreach (var id in _readyToClaim) exclude.Add(id);
        return exclude;
    }

    List<ContractConfig> BuildEligiblePool(int studioLevel, HashSet<string> exclude)
    {
        var eligible = new List<ContractConfig>();
        if (allContracts == null) return eligible;

        var city = GameHub.Instance?.city;

        foreach (var c in allContracts)
        {
            if (c == null || exclude.Contains(c.id)) continue;
            if (!c.repeatable && _permanentlyDone.Contains(c.id)) continue;
            if (studioLevel < c.unlockStudioLevel) continue;
            if (city != null && !city.IsContractUnlocked(c)) continue;

            // Phase 13.4D — genre-feasibility guard:
            // For ProduceMoviesByGenre contracts, verify the player can actually produce
            // at least one movie of the target genre at the current city level.
            // Without this check, contracts like "Produce Drama" appear when Drama movies
            // are gated behind a higher city level the player has not yet reached.
            if (c.goalType == ContractGoalType.ProduceMoviesByGenre && city != null)
            {
                bool anyMovieAvailable = false;
                var catalog = GetCatalog();
                if (catalog != null)
                {
                    foreach (var m in catalog)
                    {
                        if (m == null) continue;
                        if (m.genre != c.targetGenre) continue;
                        if (city.IsMovieUnlocked(m)) { anyMovieAvailable = true; break; }
                    }
                }
                if (!anyMovieAvailable)
                {
                    Debug.Log($"[ContractSystem] Skipping '{c.id}' ({c.targetGenre}): no {c.targetGenre} movies available at current city level.");
                    continue;
                }
            }

            eligible.Add(c);
        }

        return eligible;
    }

    static MovieConfig[] _catalogCache;
    static MovieConfig[] GetCatalog()
    {
        if (_catalogCache != null) return _catalogCache;
        var reg = UnityEngine.Resources.Load<MovieCatalogRuntimeRegistry>(MovieCatalogRuntimeRegistry.ResourceName);
        _catalogCache = reg != null ? reg.movies : System.Array.Empty<MovieConfig>();
        return _catalogCache;
    }

    ContractConfig FindContract(string id)
    {
        if (allContracts == null || string.IsNullOrEmpty(id)) return null;
        foreach (var c in allContracts)
            if (c != null && c.id == id) return c;
        return null;
    }
}
