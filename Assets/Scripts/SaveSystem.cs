using System;
using System.IO;
using UnityEngine;

// ─── Save Data ────────────────────────────────────────────────────────────────

[Serializable]
public class GameSaveData
{
    public int version = 4;

    // Economy
    public long   money;
    public double moneyExact;
    public float  reputation;
    public int    diamonds;

    // Departments (legacy – kept for v1 migration)
    public int editor, director, actors, sound, cinematography;
    public int makeup, costume, art, lighting, grip, producer;

    // Prestige
    public int oscars;
    public int cityLevel = 1;

    // v2 – new systems
    public int   studioLevel;
    public float studioXP;
    public UpgradeSaveEntry[] upgradeLevels;
    public string[]           completedContractIds;

    // v3 – session persistence
    public long saveTimestampUnix;
    public ContractSaveEntry[]     contractStates;
    public string[]                contractHistoryIds;
    public ProductionSaveEntry[]   activeProductions;
    public MovieHistorySaveEntry[] movieHistory;
    public string[]                recentProductionKeys;
    public string[]                completedMovieKeys;

    // v4 — FTUE
    public bool ftueCompleted;
    public int  ftueStep;
}

[Serializable]
public class ContractSaveEntry
{
    public string id;
    public float  progress;
    public bool   readyToClaim;
    public int    genreMask;
}

[Serializable]
public class ProductionSaveEntry
{
    public string movieKey;
    public float  elapsedSeconds;
    public float  totalDuration;
    public long   pendingReward;
    public float  pendingRep;
    public float  pendingXp;
}

[Serializable]
public class MovieHistorySaveEntry
{
    public string movieName;
    public long   moneyReward;
    public float  repGain;
    public float  xpGain;
}

// ─── Save System ─────────────────────────────────────────────────────────────

public class SaveSystem : MonoBehaviour
{
    private const string SAVE_FILE           = "save_v2.json";
    private const string LEGACY_SAVE_FILE    = "save.json";
    private const float  AUTO_SAVE_INTERVAL  = 30f;

    private static string SavePath       => Path.Combine(Application.persistentDataPath, SAVE_FILE);
    private static string LegacySavePath => Path.Combine(Application.persistentDataPath, LEGACY_SAVE_FILE);

    private float autoSaveTimer;

    public static float GetOfflineSeconds(long saveTimestampUnix)
    {
        if (saveTimestampUnix <= 0) return 0f;
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return Math.Max(0f, now - saveTimestampUnix);
    }

    private void Update()
    {
        autoSaveTimer += Time.deltaTime;
        if (autoSaveTimer >= AUTO_SAVE_INTERVAL) { Save(); autoSaveTimer = 0f; }
    }

    private void OnApplicationPause(bool pause) { if (pause) Save(); }
    private void OnApplicationQuit()            { Save(); }

    // ─── Public API ──────────────────────────────────────────────────────────

    public void Save()
    {
        var hub = GameHub.Instance;
        if (hub == null) return;

        var data = new GameSaveData
        {
            version = 4,
            money      = hub.studio.Money,
            moneyExact = hub.studio.MoneyExact,
            reputation = hub.studio.reputation,
            diamonds   = hub.diamonds != null ? hub.diamonds.GetSaveData() : 0,

            editor         = hub.departments.editor,
            director       = hub.departments.director,
            actors         = hub.departments.actors,
            sound          = hub.departments.sound,
            cinematography = hub.departments.cinematography,
            makeup         = hub.departments.makeup,
            costume        = hub.departments.costume,
            art            = hub.departments.art,
            lighting       = hub.departments.lighting,
            grip           = hub.departments.grip,
            producer       = hub.departments.producer,

            oscars = hub.prestige.oscars,
            cityLevel = hub.city != null ? hub.city.GetSaveData() : 1,

            studioLevel = hub.studioLevel?.Level ?? 1,
            studioXP    = hub.studioLevel?.XP    ?? 0f,

            upgradeLevels        = hub.upgrades?.GetSaveData(),
            completedContractIds = hub.contracts?.GetCompletedIds(),

            saveTimestampUnix  = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            contractStates     = hub.contracts?.GetSaveData() ?? System.Array.Empty<ContractSaveEntry>(),
            contractHistoryIds = hub.contracts?.GetHistorySaveIds(),
            activeProductions  = hub.studio?.GetProductionSaveData(),
            movieHistory       = hub.studio?.GetMovieHistorySaveData(),
            recentProductionKeys = hub.studio?.GetRecentProductionSaveData(),
            completedMovieKeys   = hub.studio?.GetCompletedMovieSaveData(),
            ftueCompleted        = FtueState.Completed,
            ftueStep             = (int)FtueState.Step,
        };

        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            Debug.Log("[SaveSystem] Saved v4 → " + SavePath);
        }
        catch (Exception e) { Debug.LogError("[SaveSystem] Save failed: " + e.Message); }
    }

    /// <summary>Returns true if a save was found and loaded.</summary>
    public bool Load()
    {
        if (File.Exists(SavePath))
            return TryLoad(SavePath);

        if (File.Exists(LegacySavePath))
            return TryLoad(LegacySavePath);

        return false;
    }

    private bool TryLoad(string path)
    {
        try
        {
            var data = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(path));
            var hub  = GameHub.Instance;

            if (data.money == 0 && data.reputation == 0f)
                return false;

            hub.studio.Money      = data.money;
            hub.studio.reputation = data.reputation;
            hub.studio.LoadMoneyExact(data.moneyExact, data.money);

            hub.departments.editor         = data.editor;
            hub.departments.director       = data.director;
            hub.departments.actors         = data.actors;
            hub.departments.sound          = data.sound;
            hub.departments.cinematography = data.cinematography;
            hub.departments.makeup         = data.makeup;
            hub.departments.costume        = data.costume;
            hub.departments.art            = data.art;
            hub.departments.lighting       = data.lighting;
            hub.departments.grip           = data.grip;
            hub.departments.producer       = data.producer;

            hub.prestige.oscars = data.oscars;
            hub.city?.LoadFromSave(data.cityLevel);
            hub.city?.BindRuntime(hub.prestige, hub.studio);

            hub.diamonds?.LoadFromSave(data.diamonds);

            if (data.version >= 2)
            {
                hub.studioLevel?.LoadFromSave(data.studioLevel, data.studioXP);
                hub.upgrades?.LoadFromSave(data.upgradeLevels);
                hub.contracts?.LoadCompletedIds(data.completedContractIds);
            }
            else
            {
                MigrateV1ToV2(data, hub);
            }

            hub.studio.LoadMovieHistory(data.movieHistory);
            hub.studio.LoadRecentProductionKeys(data.recentProductionKeys);
            hub.studio.LoadCompletedMovieKeys(data.completedMovieKeys);
            hub.studio.MigrateCompletedFromMovieHistory(data.movieHistory, data.completedMovieKeys);

            if (data.version >= 3)
                hub.contracts?.LoadFromSave(data.contractStates, data.contractHistoryIds);
            else if (data.contractStates != null && data.contractStates.Length > 0)
                hub.contracts?.LoadFromSave(data.contractStates, data.contractHistoryIds);
            else
                hub.pendingLegacyContractRefresh = true;

            float offlineSeconds = GetOfflineSeconds(data.saveTimestampUnix);
            offlineSeconds = Mathf.Min(offlineSeconds, StudioManager.MaxOfflineSeconds);
            hub.studio.ApplyOfflinePassiveIncome(offlineSeconds);
            hub.studio.ResumeProductions(data.activeProductions, offlineSeconds);

            if (data.version >= 4)
                FtueState.ApplySave(data.ftueCompleted, data.ftueStep);
            else if (data.completedMovieKeys != null && data.completedMovieKeys.Length > 0)
                FtueState.ApplySave(true, (int)FtueStep.Done);
            else
                FtueState.ApplySave(false, (int)FtueStep.Welcome);

            Debug.Log($"[SaveSystem] Loaded v{data.version} from {Path.GetFileName(path)} | ftueCompleted={FtueState.Completed} ftueStep={FtueState.Step}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[SaveSystem] Load failed: " + e.Message);
            return false;
        }
    }

    private static void MigrateV1ToV2(GameSaveData data, GameHub hub)
    {
        if (hub.upgrades == null || hub.studioLevel == null) return;

        var mapping = new (string id, int level)[]
        {
            ("personal_editor",         data.editor),
            ("personal_director",       data.director),
            ("personal_actors",         data.actors),
            ("personal_sound",          data.sound),
            ("personal_cinematography", data.cinematography),
            ("personal_makeup",         data.makeup),
            ("personal_costume",        data.costume),
            ("personal_art",            data.art),
            ("personal_lighting",       data.lighting),
            ("personal_grip",           data.grip),
            ("personal_producer",       data.producer),
        };

        var entries = new System.Collections.Generic.List<UpgradeSaveEntry>();
        foreach (var (id, lvl) in mapping)
            if (lvl > 0) entries.Add(new UpgradeSaveEntry { id = id, level = lvl });

        hub.upgrades.LoadFromSave(entries.ToArray());

        int estimatedLevel = Mathf.Max(1, Mathf.FloorToInt(data.reputation / 50f));
        hub.studioLevel.LoadFromSave(estimatedLevel, 0f);
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath))       File.Delete(SavePath);
        if (File.Exists(LegacySavePath)) File.Delete(LegacySavePath);
        Debug.Log("[SaveSystem] Save deleted.");
    }
}
