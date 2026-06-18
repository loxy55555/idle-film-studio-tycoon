using System;
using UnityEngine;

/// <summary>
/// FASE 15.2A — BLOQUE B + C
/// Manages timed x2 boosts for Income, XP, and REP.
/// Rules:
///   • Only one active boost per category at a time (no stacking same type).
///   • Persisted in save file (unix timestamp expiry).
///   • Tick-based expiry check in Update().
/// </summary>
public class BoostSystem : MonoBehaviour
{
    public enum BoostType { Income, XP, Rep }

    public static BoostSystem Instance { get; private set; }

    /// <summary>Duration of any boost in seconds.</summary>
    public const float BoostDurationSeconds = 10f * 60f; // 10 minutes
    public const float BoostMultiplier = 2f;

    // Expiry stored as UTC unix timestamps (0 = not active)
    long _incomeExpiry;
    long _xpExpiry;
    long _repExpiry;

    /// <summary>Fired when any boost state changes (activate, expire).</summary>
    public event Action OnBoostsChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void OnEnable()
    {
        if (Instance == null) Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        bool changed = false;

        if (_incomeExpiry > 0 && now >= _incomeExpiry) { _incomeExpiry = 0; changed = true; }
        if (_xpExpiry     > 0 && now >= _xpExpiry)     { _xpExpiry     = 0; changed = true; }
        if (_repExpiry    > 0 && now >= _repExpiry)     { _repExpiry    = 0; changed = true; }

        if (changed)
        {
            GameHub.Instance?.studio?.RecalculateIncome();
            OnBoostsChanged?.Invoke();
            Debug.Log("[BoostSystem] A boost expired.");
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Returns true if the boost was activated, false if one is already active.</summary>
    public bool TryActivate(BoostType type)
    {
        if (IsActive(type)) return false;

        long expiry = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (long)BoostDurationSeconds;
        SetExpiry(type, expiry);

        if (type == BoostType.Income)
            GameHub.Instance?.studio?.RecalculateIncome();

        OnBoostsChanged?.Invoke();
        GameHub.Instance?.save?.Save("BoostActivated_" + type);
        Debug.Log($"[BoostSystem] Activated {type} boost for {BoostDurationSeconds / 60f:0.0} min.");
        return true;
    }

    public bool IsActive(BoostType type)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return GetExpiry(type) > now;
    }

    /// <summary>Returns remaining seconds, 0 if not active.</summary>
    public float GetTimeRemaining(BoostType type)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long expiry = GetExpiry(type);
        return expiry > now ? Mathf.Max(0f, expiry - now) : 0f;
    }

    /// <summary>Returns 2.0 if boost is active, 1.0 otherwise.</summary>
    public float GetMultiplier(BoostType type) => IsActive(type) ? BoostMultiplier : 1f;

    /// <summary>Static shorthand usable from StudioManager without null-checking everywhere.</summary>
    public static float Multiplier(BoostType type) =>
        Instance != null ? Instance.GetMultiplier(type) : 1f;

    // ── Save / Load ────────────────────────────────────────────────────────────

    public BoostSaveEntry[] GetSaveData() => new[]
    {
        new BoostSaveEntry { type = (int)BoostType.Income, expiryUnix = _incomeExpiry },
        new BoostSaveEntry { type = (int)BoostType.XP,     expiryUnix = _xpExpiry     },
        new BoostSaveEntry { type = (int)BoostType.Rep,    expiryUnix = _repExpiry    },
    };

    public void LoadFromSave(BoostSaveEntry[] entries)
    {
        if (entries == null) return;
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        foreach (var entry in entries)
        {
            if (entry == null || entry.expiryUnix <= now) continue;
            SetExpiry((BoostType)entry.type, entry.expiryUnix);
        }
        OnBoostsChanged?.Invoke();
    }

    public void Init()
    {
        _incomeExpiry = _xpExpiry = _repExpiry = 0;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    long GetExpiry(BoostType type) => type switch
    {
        BoostType.Income => _incomeExpiry,
        BoostType.XP     => _xpExpiry,
        BoostType.Rep    => _repExpiry,
        _ => 0,
    };

    void SetExpiry(BoostType type, long value)
    {
        switch (type)
        {
            case BoostType.Income: _incomeExpiry = value; break;
            case BoostType.XP:     _xpExpiry     = value; break;
            case BoostType.Rep:    _repExpiry     = value; break;
        }
    }
}

[Serializable]
public class BoostSaveEntry
{
    public int  type;       // BoostType enum value
    public long expiryUnix; // UTC unix timestamp
}
