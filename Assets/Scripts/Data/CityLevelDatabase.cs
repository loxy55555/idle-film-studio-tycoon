using System;
using UnityEngine;

[Serializable]
public class CityUnlockSet
{
    public string[] movieIds;
    public string[] upgradeIds;
    public string[] contractIds;
    public string[] departmentKeys;
}

[Serializable]
public class CityLevelDefinition
{
    public int   level;
    public string displayName;
    public int   requiredOscars;
    public float globalMultiplier;
    public string backgroundColorHex = "#1A1A2E";
    public string backgroundArtKey = "";
    public CityUnlockSet unlocks = new();
}

public static class CityLevelDatabase
{
    public const int MaxLevel = 8;

    static CityLevelDefinition[] _levels;

    public static CityLevelDefinition[] AllLevels
    {
        get
        {
            if (_levels == null) _levels = BuildLevels();
            return _levels;
        }
    }

    public static CityLevelDefinition GetLevel(int level)
    {
        level = Mathf.Clamp(level, 1, MaxLevel);
        return AllLevels[level - 1];
    }

    static CityLevelDefinition[] BuildLevels()
    {
        return new[]
        {
            Def(1, "Garaje",                     0,  1.00f, "#1A1A2E"),
            Def(2, "Estudio Independiente",      1,  1.10f, "#1E2438"),
            Def(3, "Estudio Local",              3,  1.25f, "#222848"),
            Def(4, "Estudio Regional",           6,  1.50f, "#263058"),
            Def(5, "Gran Estudio",              10,  2.00f, "#2A3868"),
            Def(6, "Hollywood Boulevard",       15,  3.00f, "#324078"),
            Def(7, "Major Studio",              25,  5.00f, "#3A4888"),
            Def(8, "Imperio Cinematográfico",   40,  8.00f, "#425098"),
        };
    }

    static CityLevelDefinition Def(int level, string name, int oscars, float mult, string bgHex)
    {
        return new CityLevelDefinition
        {
            level = level,
            displayName = name,
            requiredOscars = oscars,
            globalMultiplier = mult,
            backgroundColorHex = bgHex,
            unlocks = new CityUnlockSet(),
        };
    }
}
