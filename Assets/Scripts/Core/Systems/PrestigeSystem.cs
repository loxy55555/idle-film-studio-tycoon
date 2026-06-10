using System;
using UnityEngine;

public class PrestigeSystem : MonoBehaviour
{
    public int oscars;

    public event Action OnOscarGained;

    private const float BASE_THRESHOLD   = 300f;
    private const float THRESHOLD_GROWTH = 1.7f;

    public float NextPrestigeThreshold => BASE_THRESHOLD * Mathf.Pow(THRESHOLD_GROWTH, oscars);

    public bool CanPrestige(float reputation) => reputation >= NextPrestigeThreshold;

    public void Prestige(StudioManager studio, DepartmentSystem departments)
    {
        if (!CanPrestige(studio.reputation))
        {
            Debug.LogWarning("[PrestigeSystem] Prestige attempted below threshold.");
            return;
        }

        GainOscar();
        departments.Init();
        studio.Initialize();
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
