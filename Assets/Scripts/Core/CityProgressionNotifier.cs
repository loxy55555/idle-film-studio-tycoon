using System.Collections.Generic;

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
            feel.ShowCityContentUnlock(Loc.Get(LocKeys.CityUnlockDepts), lines.ToArray());
        }

        int movieCount = CityProgressionRules.CountMoviesUnlockedAtCity(
            MovieCatalogRuntime.AllMovies, newLevel);
        if (movieCount > 0)
            feel.ShowCityContentUnlock(Loc.Get(LocKeys.CityUnlockMovies),
                new[] { Loc.Format(LocKeys.CityUnlockMoviesFmt, movieCount) });

        int upgradeCount = CityProgressionRules.CountUpgradesUnlockedAtCity(hub.upgrades?.allUpgrades, newLevel);
        if (upgradeCount > 0)
            feel.ShowCityContentUnlock(Loc.Get(LocKeys.CityUnlockUpgrades),
                new[] { Loc.Format(LocKeys.CityUnlockUpgradesFmt, upgradeCount) });

        int contractCount = CityProgressionRules.CountContractsUnlockedAtCity(hub.contracts?.allContracts, newLevel);
        if (contractCount > 0)
            feel.ShowCityContentUnlock(Loc.Get(LocKeys.CityUnlockContracts),
                new[] { Loc.Format(LocKeys.CityUnlockContractsFmt, contractCount) });
    }
}
