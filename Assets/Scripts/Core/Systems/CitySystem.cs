using System;
using System.Collections.Generic;
using UnityEngine;

public class CitySystem : MonoBehaviour
{
    public int Level { get; private set; } = 1;

    public event Action<int> OnCityLevelChanged;

    PrestigeSystem _prestige;
    StudioManager  _studio;

    public int MaxLevel => CityLevelDatabase.MaxLevel;
    public CityLevelDefinition CurrentDefinition => CityLevelDatabase.GetLevel(Level);
    public float GlobalMultiplier => CurrentDefinition.globalMultiplier;
    public bool IsMaxLevel => Level >= MaxLevel;

    public void Init()
    {
        Level = 1;
        OnCityLevelChanged?.Invoke(Level);
    }

    public void LoadFromSave(int savedLevel)
    {
        Level = Mathf.Clamp(savedLevel <= 0 ? 1 : savedLevel, 1, MaxLevel);
        OnCityLevelChanged?.Invoke(Level);
    }

    public int GetSaveData() => Level;

    public void BindRuntime(PrestigeSystem prestige, StudioManager studio)
    {
        _prestige = prestige;
        _studio   = studio;
    }

    public CityLevelDefinition GetNextDefinition()
    {
        if (IsMaxLevel) return null;
        return CityLevelDatabase.GetLevel(Level + 1);
    }

    public int CurrentOscars => _prestige != null ? _prestige.oscars : 0;

    public bool CanUpgrade()
    {
        if (IsMaxLevel) return false;
        var next = GetNextDefinition();
        return next != null && CurrentOscars >= next.requiredOscars;
    }

    public bool TryUpgrade()
    {
        if (!CanUpgrade()) return false;

        int oldLevel = Level;
        Level++;
        OnCityLevelChanged?.Invoke(Level);
        CityProgressionNotifier.NotifyLevelUp(oldLevel, Level);
        _studio?.RecalculateIncome();
        GameHub.Instance?.contracts?.RefreshContracts(GameHub.Instance?.studioLevel?.Level ?? 1);
        GameHub.Instance?.save?.Save("CityUpgrade");
        return true;
    }

    public float GetProgressToNextLevel()
    {
        if (IsMaxLevel) return 1f;

        var current = CurrentDefinition;
        var next    = GetNextDefinition();
        int span = next.requiredOscars - current.requiredOscars;
        if (span <= 0) return 1f;

        float value = CurrentOscars - current.requiredOscars;
        return Mathf.Clamp01(value / span);
    }

    public string GetProgressLabel()
    {
        if (IsMaxLevel) return Loc.Format(LocKeys.InstOscarMax, CurrentOscars);

        var next = GetNextDefinition();
        return Loc.Format(LocKeys.InstOscarProgress, CurrentOscars, next.requiredOscars);
    }

    public bool IsMovieUnlocked(MovieConfig cfg) =>
        CityProgressionRules.IsMovieUnlocked(cfg, Level);

    public bool IsUpgradeUnlocked(UpgradeConfig cfg) =>
        CityProgressionRules.IsUpgradeUnlocked(cfg, Level);

    public bool IsContractUnlocked(ContractConfig cfg) =>
        CityProgressionRules.IsContractUnlocked(cfg, Level);

    public bool IsDepartmentUnlocked(DepartmentType type) =>
        CityProgressionRules.IsDepartmentUnlocked(type, Level);

    public bool IsDepartmentUnlocked(string departmentKey)
    {
        if (string.IsNullOrEmpty(departmentKey)) return true;
        if (System.Enum.TryParse<DepartmentType>(departmentKey, true, out var type))
            return IsDepartmentUnlocked(type);
        return true;
    }

    public List<string> GetUnlockSummaryLines()
    {
        var lines = new List<string>();
        lines.Add(Loc.Format(LocKeys.InstUnlockGlobal, CurrentDefinition.globalMultiplier));
        AppendDepartmentLines(lines, Level);
        AppendContentBandLines(lines, Level);
        return lines;
    }

    public List<string> GetNextUnlockPreviewLines()
    {
        if (IsMaxLevel) return new List<string> { Loc.Get(LocKeys.InstMaxCity) };

        int next = Level + 1;
        var lines = new List<string>();
        var def = CityLevelDatabase.GetLevel(next);
        lines.Add(Loc.Format(LocKeys.InstUnlockGlobal, def.globalMultiplier));
        AppendDepartmentLines(lines, next);
        AppendContentBandLines(lines, next);
        return lines;
    }

    static void AppendDepartmentLines(List<string> lines, int cityLevel)
    {
        var depts = CityProgressionRules.GetDepartmentsUnlockedAtCity(cityLevel);
        foreach (var d in depts)
            lines.Add(Loc.Format(LocKeys.InstUnlockDept, DepartmentLoc.GetName(d)));
    }

    static void AppendContentBandLines(List<string> lines, int cityLevel)
    {
        var tierKey = LocKeys.InstTierPfx + cityLevel;
        var tierDesc = Loc.Get(tierKey);
        if (tierDesc != tierKey)
            lines.Add(tierDesc);
    }
}
