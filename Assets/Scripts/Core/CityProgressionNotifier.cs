using System.Collections.Generic;
using UnityEngine;

/// <summary>DOTween popups when a city level unlocks new content.</summary>
public static class CityProgressionNotifier
{
    public static void NotifyLevelUp(int oldLevel, int newLevel)
    {
        if (newLevel <= oldLevel) return;

        var hub = GameHub.Instance;
        if (hub == null) return;

        var feel = GameFeelUI.Instance;
        if (feel == null) return;

        var depts = CityProgressionRules.GetDepartmentsUnlockedAtCity(newLevel);
        if (depts.Count > 0)
        {
            var lines = new List<string>();
            foreach (var d in depts)
                lines.Add(DepartmentCardUI.DeptData[(int)d].name);
            feel.ShowCityContentUnlock("Nuevos departamentos disponibles", lines.ToArray());
        }

        int movieCount = CityProgressionRules.CountMoviesUnlockedAtCity(
            hub.GetComponentInChildren<MovieTabUI>()?.allMovies
            ?? Object.FindAnyObjectByType<MovieTabUI>()?.allMovies, newLevel);
        if (movieCount > 0)
            feel.ShowCityContentUnlock("Nuevas películas desbloqueadas",
                new[] { $"+{movieCount} título(s) en catálogo" });

        int upgradeCount = CityProgressionRules.CountUpgradesUnlockedAtCity(hub.upgrades?.allUpgrades, newLevel);
        if (upgradeCount > 0)
            feel.ShowCityContentUnlock("Nuevas mejoras disponibles",
                new[] { $"+{upgradeCount} mejora(s)" });

        int contractCount = CityProgressionRules.CountContractsUnlockedAtCity(hub.contracts?.allContracts, newLevel);
        if (contractCount > 0)
            feel.ShowCityContentUnlock("Nuevos contratos disponibles",
                new[] { $"+{contractCount} contrato(s)" });
    }
}
