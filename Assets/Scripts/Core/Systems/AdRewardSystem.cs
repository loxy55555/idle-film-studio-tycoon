using System;
using UnityEngine;

/// <summary>
/// FASE 15.2A — BLOQUE D + E + F + G + H
/// Centralises all rewarded-ad flows:
///   D — infrastructure / placement IDs
///   E — Free diamonds (+3, max 1/day)
///   F — Free boosts (Income / XP / REP, max 2/day each)
///   G — Investor (5 min income, no daily limit)
///   H — NoAds bypass: skips the ad display but still checks all limits / durations
///
/// Daily counters are stored in PlayerPrefs (separate from main save — lighter, date-keyed).
/// </summary>
public static class AdRewardSystem
{
    // ── Placement IDs ──────────────────────────────────────────────────────────
    public const string PlacementFreeDiamonds   = "free_diamonds";
    public const string PlacementBoostIncome    = "boost_income";
    public const string PlacementBoostXP        = "boost_xp";
    public const string PlacementBoostRep       = "boost_rep";
    public const string PlacementInvestor       = "investor";
    public const string PlacementCancelContract = "cancel_contract"; // FASE 16.1 — D3

    // ── Daily limits ───────────────────────────────────────────────────────────
    public const int LimitFreeDiamonds  = 1;
    public const int LimitBoostIncome   = 2;
    public const int LimitBoostXP       = 2;
    public const int LimitBoostRep      = 2;
    // Investor: no daily limit — only 10-minute cooldown.

    // ── Investor cooldown (FASE 15.2B — BLOQUE F) ─────────────────────────────
    public const float InvestorCooldownSeconds = 10f * 60f; // 10 minutes
    static readonly string InvestorLastUseKey  = "investor.last_use_unix";

    // ── Rewards ───────────────────────────────────────────────────────────────
    public const int FreeDiamondsReward     = 3;
    public const float InvestorMinutes      = 5f;

    // ── PlayerPrefs keys ──────────────────────────────────────────────────────
    static string DateKey(string placement)    => $"adlimit.date.{placement}";
    static string CounterKey(string placement) => $"adlimit.count.{placement}";

    static string TodayString => DateTime.UtcNow.ToString("yyyyMMdd");

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>How many uses remain today for this placement. -1 = unlimited.</summary>
    public static int UsesRemaining(string placement)
    {
        int limit = GetLimit(placement);
        if (limit < 0) return -1;
        return Mathf.Max(0, limit - GetUsesToday(placement));
    }

    /// <summary>True if the player can use this placement (checks daily limit and investor cooldown).</summary>
    public static bool CanUse(string placement)
    {
        if (placement == PlacementInvestor)
            return GetInvestorCooldownRemaining() <= 0f;

        int limit = GetLimit(placement);
        if (limit < 0) return true;
        return GetUsesToday(placement) < limit;
    }

    /// <summary>Returns remaining investor cooldown in seconds (0 if ready).</summary>
    public static float GetInvestorCooldownRemaining()
    {
        long lastUse = long.TryParse(PlayerPrefs.GetString(InvestorLastUseKey, "0"), out long v) ? v : 0L;
        if (lastUse <= 0) return 0f;
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        float elapsed = now - lastUse;
        return Mathf.Max(0f, InvestorCooldownSeconds - elapsed);
    }

    /// <summary>
    /// Main entry point for all rewarded-ad buttons.
    /// Shows a real ad OR skips it for NoAds players.
    /// On success: delivers reward, increments counter, saves.
    /// On failure: nothing.
    /// </summary>
    public static void RequestReward(string placement)
    {
        if (!CanUse(placement))
        {
            Debug.Log($"[AdReward] Daily limit reached for '{placement}'.");
            AdRewardUI.ShowMessage(Loc.Get(LocKeys.AdLimitReached));
            return;
        }

        bool noAds = PremiumFeatures.Instance?.NoAdsPurchased ?? false;

        if (noAds)
        {
            DeliverReward(placement);
        }
        else
        {
            GameplayRefreshService.TryShowAd(placement, ok =>
            {
                if (!ok) return;
                DeliverReward(placement);
            });
        }
    }

    // ── Reward delivery ────────────────────────────────────────────────────────

    static void DeliverReward(string placement)
    {
        // Pre-check: if this is a boost placement and boost is already active, reject early.
        if (IsBoostAlreadyActive(placement))
        {
            AdRewardUI.ShowMessage(Loc.Get(LocKeys.BoostAlreadyActive));
            return;
        }

        IncrementUses(placement);

        switch (placement)
        {
            case PlacementFreeDiamonds:
                DeliverFreeDiamonds();
                break;
            case PlacementBoostIncome:
                DeliverBoost(BoostSystem.BoostType.Income);
                break;
            case PlacementBoostXP:
                DeliverBoost(BoostSystem.BoostType.XP);
                break;
            case PlacementBoostRep:
                DeliverBoost(BoostSystem.BoostType.Rep);
                break;
            case PlacementInvestor:
                PlayerPrefs.SetString(InvestorLastUseKey, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
                PlayerPrefs.Save();
                DeliverInvestorReward();
                break;
        }

        GameHub.Instance?.save?.Save("AdReward_" + placement);
    }

    static bool IsBoostAlreadyActive(string placement)
    {
        var boosts = BoostSystem.Instance;
        if (boosts == null) return false;
        return placement switch
        {
            PlacementBoostIncome => boosts.IsActive(BoostSystem.BoostType.Income),
            PlacementBoostXP     => boosts.IsActive(BoostSystem.BoostType.XP),
            PlacementBoostRep    => boosts.IsActive(BoostSystem.BoostType.Rep),
            _ => false,
        };
    }

    static void DeliverFreeDiamonds()
    {
        var wallet = GameHub.Instance?.diamonds;
        if (wallet == null) return;
        wallet.Add(FreeDiamondsReward);
        AdRewardUI.ShowMessage(string.Format(Loc.Get(LocKeys.FreeDiamondsAwarded), FreeDiamondsReward));
        Debug.Log($"[AdReward] +{FreeDiamondsReward} diamonds awarded.");
    }

    static void DeliverBoost(BoostSystem.BoostType type)
    {
        var boosts = BoostSystem.Instance;
        if (boosts == null) return;

        bool ok = boosts.TryActivate(type);
        if (ok)
            AdRewardUI.ShowMessage(Loc.Get(LocKeys.BoostActivated));
        else
            AdRewardUI.ShowMessage(Loc.Get(LocKeys.BoostAlreadyActive));

        Debug.Log($"[AdReward] Boost {type} — activated={ok}.");
    }

    static void DeliverInvestorReward()
    {
        var studio = GameHub.Instance?.studio;
        if (studio == null) return;

        long income = studio.CurrentIncome;
        long reward = income * (long)(InvestorMinutes * 60f);

        if (reward <= 0)
        {
            AdRewardUI.ShowMessage(Loc.Get(LocKeys.InvestorNoIncome));
            return;
        }

        studio.AddMoney(reward);
        AdRewardUI.ShowMessage(string.Format(Loc.Get(LocKeys.InvestorAwarded), reward));
        Debug.Log($"[AdReward] Investor: +${reward:N0} ({InvestorMinutes} min income).");
    }

    // ── Preview (BLOQUE G — show before claiming) ──────────────────────────────

    /// <summary>Returns the investor reward value based on current income. Call before showing the button label.</summary>
    public static long GetInvestorPreviewReward()
    {
        var studio = GameHub.Instance?.studio;
        if (studio == null) return 0;
        return studio.CurrentIncome * (long)(InvestorMinutes * 60f);
    }

    // ── Daily counter helpers ──────────────────────────────────────────────────

    static int GetUsesToday(string placement)
    {
        string storedDate = PlayerPrefs.GetString(DateKey(placement), "");
        if (storedDate != TodayString) return 0;
        return PlayerPrefs.GetInt(CounterKey(placement), 0);
    }

    static void IncrementUses(string placement)
    {
        string today = TodayString;
        string storedDate = PlayerPrefs.GetString(DateKey(placement), "");

        int count = storedDate == today ? PlayerPrefs.GetInt(CounterKey(placement), 0) : 0;
        count++;

        PlayerPrefs.SetString(DateKey(placement), today);
        PlayerPrefs.SetInt(CounterKey(placement), count);
        PlayerPrefs.Save();
    }

    static int GetLimit(string placement) => placement switch
    {
        PlacementFreeDiamonds   => LimitFreeDiamonds,
        PlacementBoostIncome    => LimitBoostIncome,
        PlacementBoostXP        => LimitBoostXP,
        PlacementBoostRep       => LimitBoostRep,
        PlacementInvestor       => -1, // no limit
        PlacementCancelContract => -1, // no limit — contract guards its own state
        _ => 1,
    };
}

/// <summary>
/// Thin static helper to show one-line feedback messages in-game.
/// Used by AdRewardSystem and store buttons.
/// Falls back to Debug.Log if no UI is connected.
/// </summary>
public static class AdRewardUI
{
    static System.Collections.Generic.Queue<string> _pending = new();
    static Action<string> _handler;

    /// <summary>Register a UI callback (e.g., toast popup). Called from StorePanelUI or HUD.</summary>
    public static void Register(Action<string> handler) => _handler = handler;

    public static void ShowMessage(string msg)
    {
        if (_handler != null)
            _handler(msg);
        else
            Debug.Log("[AdReward] " + msg);
    }
}
