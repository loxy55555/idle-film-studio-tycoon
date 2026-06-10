using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the 2-5 minute session loop through short-term contracts.
/// </summary>
public class ContractSystem : MonoBehaviour
{
    const string AllGenresContractId = "c_all_genres";

    [Header("Contract Database")]
    public ContractConfig[] allContracts;

    [Tooltip("Maximum active contracts at once")]
    public int maxActiveContracts = 3;

    public event Action OnContractUpdated;
    public event Action<ContractConfig> OnContractCompleted;
    public event Action<ContractConfig> OnContractClaimed;

    private readonly List<ContractConfig>      _active          = new();
    private readonly Dictionary<string, float> _progress        = new();
    private readonly Dictionary<string, int>   _genreMask       = new();
    private readonly HashSet<string>           _readyToClaim    = new();
    private readonly HashSet<string>           _permanentlyDone = new();
    private readonly List<ContractConfig>      _history         = new();

    public IReadOnlyList<ContractConfig> ActiveContracts  => _active;
    public IReadOnlyList<ContractConfig> HistoryContracts => _history;

    public void Init(int studioLevel)
    {
        _active.Clear();
        _progress.Clear();
        _genreMask.Clear();
        _readyToClaim.Clear();
        _history.Clear();
        RefreshContracts(studioLevel);
    }

    public void RefreshContracts(int studioLevel)
    {
        if (allContracts == null) return;

        foreach (var c in allContracts)
        {
            if (_active.Count >= maxActiveContracts) break;
            if (_active.Contains(c)) continue;
            if (!c.repeatable && _permanentlyDone.Contains(c.id)) continue;
            if (_readyToClaim.Contains(c.id)) continue;
            if (studioLevel < c.unlockStudioLevel) continue;
            if (GameHub.Instance?.city != null && !GameHub.Instance.city.IsContractUnlocked(c)) continue;

            _active.Add(c);
            if (!_progress.ContainsKey(c.id))
                _progress[c.id] = 0f;
        }

        OnContractUpdated?.Invoke();
    }

    public float GetProgress(ContractConfig c) =>
        _progress.TryGetValue(c.id, out var p) ? p : 0f;

    public bool IsReadyToClaim(ContractConfig c) =>
        c != null && _readyToClaim.Contains(c.id);

    public bool IsActive(ContractConfig c) =>
        c != null && _active.Contains(c);

    public void OnMovieCompleted(MovieConfig movie, float quality, float duration, long movieReward)
    {
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

        OnContractClaimed?.Invoke(contract);
        OnContractUpdated?.Invoke();
        RefreshContracts(studioLevel?.Level ?? 1);
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
        foreach (var c in _active)
        {
            list.Add(new ContractSaveEntry
            {
                id           = c.id,
                progress     = GetProgress(c),
                readyToClaim = _readyToClaim.Contains(c.id),
                genreMask    = _genreMask.TryGetValue(c.id, out var mask) ? mask : 0,
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
        _progress.Clear();
        _genreMask.Clear();
        _readyToClaim.Clear();
        _history.Clear();

        if (states != null)
        {
            foreach (var s in states)
            {
                if (string.IsNullOrEmpty(s.id)) continue;
                var c = FindContract(s.id);
                if (c == null) continue;

                _active.Add(c);
                _progress[s.id] = s.progress;
                if (s.genreMask != 0)
                    _genreMask[s.id] = s.genreMask;
                if (s.readyToClaim)
                    _readyToClaim.Add(s.id);
            }
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

    ContractConfig FindContract(string id)
    {
        if (allContracts == null || string.IsNullOrEmpty(id)) return null;
        foreach (var c in allContracts)
            if (c != null && c.id == id) return c;
        return null;
    }
}
