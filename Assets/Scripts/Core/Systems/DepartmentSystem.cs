using UnityEngine;

public class DepartmentSystem : MonoBehaviour
{
    [Header("Creative Departments")]
    public int editor;
    public int director;
    public int actors;
    public int sound;
    public int cinematography;
    public int makeup;
    public int costume;
    public int art;

    [Header("Technical Departments")]
    public int lighting;
    public int grip;

    [Header("Management")]
    public int producer;

    private UpgradeSystem U => GameHub.Instance?.upgrades;

    // ─── Base formulas (personnel levels) ─────────────────────────────────────

    float BaseQuality() =>
        1f +
        (editor * 0.15f) +
        (director * 0.20f) +
        (actors * 0.20f) +
        (sound * 0.10f) +
        (cinematography * 0.15f) +
        (makeup * 0.10f) +
        (costume * 0.10f) +
        (art * 0.10f);

    float BaseSpeed() =>
        1f + ((lighting + grip) * 0.10f);

    float BaseCostReduction() =>
        producer * 0.03f;

    // ─── Public stats (base + non-personnel upgrade bonuses) ────────────────

    /// <summary>Final studio quality = department base + equipment/installation/marketing bonuses.</summary>
    public float CalculateQuality()
    {
        float baseQ   = BaseQuality();
        float upgrade = U != null ? U.TotalEffectNonPersonnel(UpgradeEffectType.Quality) : 0f;
        float total   = baseQ + upgrade;
        if (UpgradeSystem.LOG_EFFECTS && upgrade > 0f)
            Debug.Log($"[UpgradeFX:Quality] base={baseQ:0.###} + upgrade={upgrade:0.###} → {total:0.###}");
        return total;
    }

    /// <summary>Final production speed multiplier.</summary>
    public float CalculateSpeed()
    {
        float baseS   = BaseSpeed();
        float upgrade = U != null ? U.TotalEffectNonPersonnel(UpgradeEffectType.Speed) : 0f;
        float total   = baseS + upgrade;
        if (UpgradeSystem.LOG_EFFECTS && upgrade > 0f)
            Debug.Log($"[UpgradeFX:Speed] base={baseS:0.###} + upgrade={upgrade:0.###} → {total:0.###}");
        return Mathf.Max(0.1f, total);
    }

    /// <summary>Final cost reduction [0..0.5].</summary>
    public float CalculateCostReduction()
    {
        float baseR   = BaseCostReduction();
        float upgrade = U != null ? U.TotalEffectNonPersonnel(UpgradeEffectType.CostReduction) : 0f;
        float total   = Mathf.Clamp(baseR + upgrade, 0f, 0.5f);
        if (UpgradeSystem.LOG_EFFECTS && upgrade > 0f)
            Debug.Log($"[UpgradeFX:CostReduction] base={baseR * 100f:0.#}% + upgrade={upgrade * 100f:0.#}% → {total * 100f:0.#}%");
        return total;
    }

    public void Init()
    {
        editor = director = actors = sound = cinematography = 0;
        makeup = costume = art = 0;
        lighting = grip = producer = 0;
    }
}
