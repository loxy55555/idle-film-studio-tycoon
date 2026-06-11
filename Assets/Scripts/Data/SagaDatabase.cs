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
            MakeBuiltin("shadow_trigger", "Shadow Trigger", SagaSizeClass.Medium, 4, 1),
            MakeBuiltin("neon_drift", "Neon Drift", SagaSizeClass.Mini, 2, 2),
            MakeBuiltin("last_signal", "La Última Señal", SagaSizeClass.Single, 1, 3),
            MakeBuiltin("iron_circuit", "Circuito de Hierro", SagaSizeClass.Large, 6, 4),
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
