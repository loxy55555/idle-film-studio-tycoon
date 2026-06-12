using UnityEngine;

/// <summary>Maps departments to existing personnel upgrade assets (read-only bridge).</summary>
public static class DepartmentPersonnelBridge
{
    public static string GetUpgradeId(DepartmentType type) => type switch
    {
        DepartmentType.Editor         => "personal_editor",
        DepartmentType.Director       => "personal_director",
        DepartmentType.Actors         => "personal_actors",
        DepartmentType.Sound          => "personal_sound",
        DepartmentType.Cinematography => "personal_cinematography",
        DepartmentType.Makeup         => "personal_makeup",
        DepartmentType.Costume        => "personal_costume",
        DepartmentType.Art            => "personal_art",
        DepartmentType.Lighting       => "personal_lighting",
        DepartmentType.Grip           => "personal_grip",
        DepartmentType.Producer       => "personal_producer",
        _                             => null,
    };

    public static UpgradeConfig FindUpgrade(UpgradeSystem upgrades, DepartmentType type)
    {
        var id = GetUpgradeId(type);
        if (upgrades?.allUpgrades == null || string.IsNullOrEmpty(id)) return null;

        foreach (var cfg in upgrades.allUpgrades)
        {
            if (cfg != null && cfg.id == id)
                return cfg;
        }

        return null;
    }
}
