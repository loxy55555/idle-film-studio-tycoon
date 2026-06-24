using System;
using UnityEngine;

/// <summary>
/// Tracks accumulated Oscars (stars). 28 total — no resets, permanent progression only.
/// Thresholds are indexed by cumulative REP: THRESHOLDS[oscars] = REP required for the next Oscar.
/// </summary>
public class PrestigeSystem : MonoBehaviour
{
    public int oscars;

    public event Action OnOscarGained;

    /// <summary>Total Oscars in the main progression (stars 1–28).</summary>
    public const int TotalOscars = 28;

    // ── Approved curve (FASE 15.4D) ──────────────────────────────────────────
    // Each entry is the cumulative lifetime REP required to earn that Oscar.
    // Index = oscars already owned (0-based), so THRESHOLDS[0] = REP for Oscar #1.
    // City gates in CityLevelDatabase use requiredOscars = [1, 3, 6, 10, 15, 21, 28].
    static readonly float[] THRESHOLDS =
    {
              160f,   //  Oscar  #1  →  Ciudad 2
            3_500f,   //  Oscar  #2
            8_200f,   //  Oscar  #3  →  Ciudad 3  (cumRep gate)
           31_000f,   //  Oscar  #4
           63_000f,   //  Oscar  #5
          108_500f,   //  Oscar  #6  →  Ciudad 4
          230_000f,   //  Oscar  #7
          400_000f,   //  Oscar  #8
          640_000f,   //  Oscar  #9
          975_000f,   //  Oscar #10  →  Ciudad 5
        1_680_000f,   //  Oscar #11
        2_660_000f,   //  Oscar #12
        4_030_000f,   //  Oscar #13
        5_950_000f,   //  Oscar #14
        8_630_000f,   //  Oscar #15  →  Ciudad 6
       13_140_000f,   //  Oscar #16
       19_430_000f,   //  Oscar #17
       28_240_000f,   //  Oscar #18
       40_560_000f,   //  Oscar #19
       57_820_000f,   //  Oscar #20
       82_000_000f,   //  Oscar #21  →  Ciudad 7
      150_000_000f,   //  Oscar #22
      245_000_000f,   //  Oscar #23
      378_000_000f,   //  Oscar #24
      565_000_000f,   //  Oscar #25
      826_000_000f,   //  Oscar #26
    1_192_000_000f,   //  Oscar #27
    1_704_000_000f,   //  Oscar #28  →  Ciudad 8  (fin del juego principal)
    };

    /// <summary>
    /// REP required to claim the next Oscar.
    /// Returns float.MaxValue when all 28 Oscars have been earned (no more to claim).
    /// </summary>
    public float NextOscarThreshold =>
        oscars < THRESHOLDS.Length ? THRESHOLDS[oscars] : float.MaxValue;

    /// <summary>Legacy alias.</summary>
    public float NextPrestigeThreshold => NextOscarThreshold;

    public bool CanClaimOscar(float reputation) =>
        oscars < TotalOscars && reputation >= NextOscarThreshold;

    /// <summary>Legacy alias.</summary>
    public bool CanPrestige(float reputation) => CanClaimOscar(reputation);

    /// <summary>
    /// Claim one Oscar when reputation threshold is met.
    /// Does NOT reset money, upgrades, departments, movies, contracts, or studio level.
    /// NOTE: this overload increments and fires the event together (legacy path).
    /// Prefer the split GameHub.ClaimOscar() path that saves before firing the event.
    /// </summary>
    public bool TryClaimOscar(float reputation)
    {
        if (!CanClaimOscar(reputation)) return false;
        GainOscar();
        return true;
    }

    /// <summary>
    /// Increment Oscar count WITHOUT firing OnOscarGained.
    /// Used by GameHub.ClaimOscar() so the save can happen before UI events fire.
    /// Returns true if the count actually changed.
    /// </summary>
    public bool IncrementOscarSilent(float reputation)
    {
        if (!CanClaimOscar(reputation))
        {
            Debug.LogWarning("[OscarSystem] Claim attempted below threshold.");
            return false;
        }

        int prev = oscars;
        oscars = Mathf.Min(oscars + 1, TotalOscars);
        return oscars != prev;
    }

    /// <summary>
    /// Fire OnOscarGained after the save has been committed.
    /// Call only after IncrementOscarSilent returned true.
    /// </summary>
    public void NotifyOscarGained() => OnOscarGained?.Invoke();

    /// <summary>Legacy entry point — now claim-only, no reset.</summary>
    public void Prestige(StudioManager studio, DepartmentSystem departments)
    {
        if (studio == null) return;
        TryClaimOscar(studio.reputation);
    }

    public void GainOscar()
    {
        int prev = oscars;
        oscars = Mathf.Min(oscars + 1, TotalOscars);
        if (oscars != prev)
            OnOscarGained?.Invoke();
    }

    public void Init()
    {
        oscars = 0;
    }
}
