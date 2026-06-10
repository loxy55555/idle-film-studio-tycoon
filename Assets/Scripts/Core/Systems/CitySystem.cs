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
        GameHub.Instance?.save?.Save();
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
        if (IsMaxLevel) return $"{CurrentOscars} OSC · MÁXIMO";

        var next = GetNextDefinition();
        return $"{CurrentOscars} / {next.requiredOscars} OSC";
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
        lines.Add($"×{CurrentDefinition.globalMultiplier:0.00} ingreso global");
        AppendDepartmentLines(lines, Level);
        AppendContentBandLines(lines, Level);
        return lines;
    }

    public List<string> GetNextUnlockPreviewLines()
    {
        if (IsMaxLevel) return new List<string> { "Nivel máximo alcanzado" };

        int next = Level + 1;
        var lines = new List<string>();
        var def = CityLevelDatabase.GetLevel(next);
        lines.Add($"×{def.globalMultiplier:0.00} ingreso global");
        AppendDepartmentLines(lines, next);
        AppendContentBandLines(lines, next);
        return lines;
    }

    static void AppendDepartmentLines(List<string> lines, int cityLevel)
    {
        var depts = CityProgressionRules.GetDepartmentsUnlockedAtCity(cityLevel);
        foreach (var d in depts)
            lines.Add("Dept: " + DepartmentCardUI.DeptData[(int)d].name);
    }

    static void AppendContentBandLines(List<string> lines, int cityLevel)
    {
        lines.Add(cityLevel switch
        {
            1 => "Películas Tier 1-2",
            2 => "Secuelas y remakes",
            3 => "Películas épicas",
            4 => "Películas avanzadas",
            5 => "Mejoras de arte",
            6 => "Mejoras épicas",
            7 => "Mejoras legendarias",
            8 => "Catálogo completo",
            _ => "",
        });
    }
}
