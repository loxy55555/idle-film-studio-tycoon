using System;
using UnityEngine;

/// <summary>
/// Tracks accumulated Oscars. No resets — permanent progression only.
/// </summary>
public class PrestigeSystem : MonoBehaviour
{
    public int oscars;

    public event Action OnOscarGained;

    const float BASE_THRESHOLD   = 300f;
    const float THRESHOLD_GROWTH = 1.7f;

    public float NextOscarThreshold => BASE_THRESHOLD * Mathf.Pow(THRESHOLD_GROWTH, oscars);

    /// <summary>Legacy alias.</summary>
    public float NextPrestigeThreshold => NextOscarThreshold;

    public bool CanClaimOscar(float reputation) => reputation >= NextOscarThreshold;

    /// <summary>Legacy alias.</summary>
    public bool CanPrestige(float reputation) => CanClaimOscar(reputation);

    /// <summary>
    /// Claim one Oscar when reputation threshold is met.
    /// Does NOT reset money, upgrades, departments, movies, contracts, or studio level.
    /// </summary>
    public bool TryClaimOscar(float reputation)
    {
        if (!CanClaimOscar(reputation))
        {
            Debug.LogWarning("[OscarSystem] Claim attempted below threshold.");
            return false;
        }

        GainOscar();
        return true;
    }

    /// <summary>Legacy entry point — now claim-only, no reset.</summary>
    public void Prestige(StudioManager studio, DepartmentSystem departments)
    {
        if (studio == null) return;
        TryClaimOscar(studio.reputation);
    }

    public void GainOscar()
    {
        oscars++;
        OnOscarGained?.Invoke();
    }

    public void Init()
    {
        oscars = 0;
    }
}
