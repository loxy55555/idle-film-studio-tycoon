using System.Collections.Generic;
using UnityEngine;

/// <summary>Runtime lookup for saga metadata (variable sizes: 1/1, 2/2, 4/4, 6/6).</summary>
public static class SagaDatabase
{
    static Dictionary<string, SagaDefinition> _byId;
    static SagaDefinition[] _all;

    public static SagaDefinition[] All
    {
        get
        {
            EnsureLoaded();
            return _all;
        }
    }

    public static SagaDefinition Get(string sagaId)
    {
        if (string.IsNullOrEmpty(sagaId)) return null;
        EnsureLoaded();
        _byId.TryGetValue(sagaId, out var def);
        return def;
    }

    public static int GetExpectedSize(string sagaId, int fallbackFromCatalog = 0)
    {
        var def = Get(sagaId);
        if (def != null) return def.ExpectedSize;
        return fallbackFromCatalog > 0 ? fallbackFromCatalog : 1;
    }

    public static string GetDisplayName(string sagaId, MovieConfig sample = null)
    {
        var def = Get(sagaId);
        if (def != null && !string.IsNullOrEmpty(def.displayName))
            return def.displayName;
        if (sample != null && !string.IsNullOrEmpty(sample.sagaDisplayName))
            return sample.sagaDisplayName;
        return FormatId(sagaId);
    }

    static void EnsureLoaded()
    {
        if (_byId != null) return;

        _all = Resources.LoadAll<SagaDefinition>("Sagas");
        _byId = new Dictionary<string, SagaDefinition>();

        if (_all == null || _all.Length == 0)
            _all = BuildBuiltInFallback();

        foreach (var def in _all)
        {
            if (def == null || string.IsNullOrEmpty(def.sagaId)) continue;
            _byId[def.sagaId] = def;
        }
    }

    static SagaDefinition[] BuildBuiltInFallback()
    {
        return new[]
        {
            MakeBuiltin("project_avalanche", "Project Avalanche", SagaSizeClass.Large, 6, 2),
            MakeBuiltin("dominion", "Dominion", SagaSizeClass.Large, 6, 3),
            MakeBuiltin("atlas_signal", "Atlas Signal", SagaSizeClass.Medium, 4, 3),
            MakeBuiltin("the_long_road", "The Long Road", SagaSizeClass.Medium, 4, 2),
            MakeBuiltin("hollow_creek", "Hollow Creek", SagaSizeClass.Medium, 4, 3),
            MakeBuiltin("moonkeeper", "Moonkeeper", SagaSizeClass.Medium, 4, 4),
            MakeBuiltin("the_last_colony", "The Last Colony", SagaSizeClass.Large, 6, 5),
            MakeBuiltin("winter_letters", "Winter Letters", SagaSizeClass.Mini, 2, 2),
            MakeBuiltin("blackwater", "Blackwater", SagaSizeClass.Medium, 4, 4),
            MakeBuiltin("uncle_gary", "Uncle Gary", SagaSizeClass.Mini, 2, 3),
            MakeBuiltin("the_hidden_crown", "The Hidden Crown", SagaSizeClass.Medium, 4, 5),
            MakeBuiltin("the_glass_forest", "The Glass Forest", SagaSizeClass.Mini, 2, 4),
            MakeBuiltin("beneath_the_summer_sky", "Beneath the Summer Sky", SagaSizeClass.Mini, 2, 3),
            MakeBuiltin("the_black_ledger", "The Black Ledger", SagaSizeClass.Medium, 4, 5),
            MakeBuiltin("shadow_trigger", "Shadow Trigger", SagaSizeClass.Medium, 4, 2),
        };
    }

    static SagaDefinition MakeBuiltin(string id, string name, SagaSizeClass size, int count, int firstCity)
    {
        var def = ScriptableObject.CreateInstance<SagaDefinition>();
        def.sagaId = id;
        def.displayName = name;
        def.sizeClass = size;
        def.expectedEntryCount = count;
        def.firstEntryCity = firstCity;
        return def;
    }

    static string FormatId(string sagaId)
    {
        if (string.IsNullOrEmpty(sagaId)) return "Saga";
        string cleaned = sagaId.Replace('_', ' ').Replace('-', ' ');
        return char.ToUpper(cleaned[0]) + cleaned.Substring(1);
    }
}
