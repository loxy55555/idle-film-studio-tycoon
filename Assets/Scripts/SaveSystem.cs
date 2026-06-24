using System;
using System.IO;
using UnityEngine;

// ─── Save Data ────────────────────────────────────────────────────────────────

[Serializable]
public class GameSaveData
{
    public int version = 5;

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

    // v5 — persistent production offers
    public string[] currentOffers;

    // v6 — premium systems
    public BoostSaveEntry[]       activeBoosts;
    public PremiumFeaturesSaveData premiumFeatures;

    // v7 — IAP ownership + fulfillment ledger
    public IapSaveData iap;
}

[Serializable]
public class ContractSaveEntry
{
    public string id;
    public float  progress;
    public bool   readyToClaim;
    public int    genreMask;
    /// <summary>True = this entry is the single active contract; false = candidate slot.</summary>
    public bool   isActiveContract;
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
    public bool   awaitingDiscovery;
}

[Serializable]
public class MovieHistorySaveEntry
{
    public string movieName;
    public long   moneyReward;
    public float  repGain;
    public float  xpGain;
}

public enum SaveLoadResult
{
    Success,
    NotFound,
    Corrupt,
}

// ─── Save System ─────────────────────────────────────────────────────────────

public class SaveSystem : MonoBehaviour
{
    const string SaveFile        = "save_v2.json";
    const string LegacySaveFile  = "save.json";
    const string TempSaveSuffix  = ".tmp";
    const string CorruptSuffix   = ".corrupt";

    static string SavePath       => Path.Combine(Application.persistentDataPath, SaveFile);
    static string TempSavePath   => SavePath + TempSaveSuffix;
    static string LegacySavePath => Path.Combine(Application.persistentDataPath, LegacySaveFile);

    /// <summary>True when v6→v7 migration ran and needs persisting.</summary>
    public static bool PendingMigrationSave { get; private set; }

    const string LegacyPackPurchasedKeyPrefix = "pack.purchased.";

    public static float GetOfflineSeconds(long saveTimestampUnix)
    {
        if (saveTimestampUnix <= 0) return 0f;
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return Math.Max(0f, now - saveTimestampUnix);
    }

    void OnApplicationPause(bool pause)
    {
        if (pause) Save("ApplicationPause");
    }

    void OnApplicationQuit()
    {
        Save("ApplicationQuit");
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    public void Save(string reason)
    {
        var hub = GameHub.Instance;
        if (hub == null) return;

        var data = BuildSaveData(hub);
        try
        {
            string json = JsonUtility.ToJson(data, true);
            if (!ValidateSaveJson(json, out string validationError))
            {
                Debug.LogError("[Save] ValidationFailed Reason=" + reason + " — " + validationError);
                return;
            }

            File.WriteAllText(TempSavePath, json);

            if (!ValidateSaveFile(TempSavePath, out validationError))
            {
                Debug.LogError("[Save] TempValidationFailed Reason=" + reason + " — " + validationError);
                TryDeleteFile(TempSavePath);
                return;
            }

            if (File.Exists(SavePath))
                File.Delete(SavePath);

            File.Move(TempSavePath, SavePath);
            Debug.Log("[Save] Reason=" + reason + " → " + SavePath);
        }
        catch (Exception e)
        {
            Debug.LogError("[Save] SaveFailed Reason=" + reason + " — " + e.Message);
            TryDeleteFile(TempSavePath);
        }
    }

    /// <summary>Legacy bool API — true only on successful load.</summary>
    public bool Load() => LoadGame() == SaveLoadResult.Success;

    public SaveLoadResult LoadGame()
    {
        if (File.Exists(SavePath))
            return TryLoad(SavePath);

        if (File.Exists(LegacySavePath))
            return TryLoad(LegacySavePath);

        return SaveLoadResult.NotFound;
    }

    SaveLoadResult TryLoad(string path)
    {
        try
        {
            string raw = File.ReadAllText(path);
            if (!ValidateSaveJson(raw, out string validationError))
                throw new InvalidDataException(validationError);

            var data = JsonUtility.FromJson<GameSaveData>(raw);
            if (data == null)
                throw new InvalidDataException("JsonUtility returned null.");

            // A real save always has saveTimestampUnix set (BuildSaveData writes it since v3).
            // Only apply the blank-parse heuristic when no timestamp is present (ancient pre-v3 saves).
            // This prevents falsely rejecting legitimate saves where the player has 0 money and 0 rep.
            if (data.saveTimestampUnix <= 0 && data.money == 0 && data.reputation == 0f)
                return SaveLoadResult.NotFound;

            Debug.Log($"[SaveSystem] Cargando save v{data.version} — oscars={data.oscars}  rep={data.reputation:F0}  money={data.money}  city={data.cityLevel}  ftue={data.ftueCompleted}");
            ApplyLoadedData(data);
            Debug.Log($"[SaveSystem] Loaded v{data.version} from {Path.GetFileName(path)} | ftueCompleted={FtueState.Completed} ftueStep={FtueState.Step} offers={MovieOfferState.HasActiveOffers}");
            return SaveLoadResult.Success;
        }
        catch (Exception e)
        {
            Debug.LogError("[Save] LoadFailed — " + e.Message);
            PreserveCorruptFile(path);
            return SaveLoadResult.Corrupt;
        }
    }

    static GameSaveData BuildSaveData(GameHub hub) => new GameSaveData
    {
        version = 7,
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

        saveTimestampUnix    = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        contractStates       = hub.contracts?.GetSaveData() ?? Array.Empty<ContractSaveEntry>(),
        contractHistoryIds   = hub.contracts?.GetHistorySaveIds(),
        activeProductions    = hub.studio?.GetProductionSaveData(),
        movieHistory         = hub.studio?.GetMovieHistorySaveData(),
        recentProductionKeys = hub.studio?.GetRecentProductionSaveData(),
        completedMovieKeys   = hub.studio?.GetCompletedMovieSaveData(),
        ftueCompleted        = FtueState.Completed,
        ftueStep             = (int)FtueState.Step,
        currentOffers        = MovieOfferState.GetSaveData(),
        activeBoosts         = hub.boosts?.GetSaveData(),
        premiumFeatures      = hub.premium?.GetSaveData(),
        iap                  = EntitlementService.GetSaveData(),
    };

    void ApplyLoadedData(GameSaveData data)
    {
        var hub = GameHub.Instance;

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

        hub.prestige.oscars = Mathf.Clamp(data.oscars, 0, PrestigeSystem.TotalOscars);
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

        if (data.version >= 6 && data.premiumFeatures != null)
            hub.premium?.LoadFromSave(data.premiumFeatures);

        ApplyIapData(data);

        if (data.version >= 6 && data.activeBoosts != null)
            hub.boosts?.LoadFromSave(data.activeBoosts);

        float offlineSeconds = GetOfflineSeconds(data.saveTimestampUnix);
        float offlineCap = hub.premium?.EffectiveOfflineSeconds ?? StudioManager.MaxOfflineSeconds;
        offlineSeconds = Mathf.Min(offlineSeconds, offlineCap);
        hub.studio.ApplyOfflinePassiveIncome(offlineSeconds);
        hub.studio.ResumeProductions(data.activeProductions, offlineSeconds);

        if (data.version >= 4)
            FtueState.ApplySave(data.ftueCompleted, data.ftueStep);
        else if (data.completedMovieKeys != null && data.completedMovieKeys.Length > 0)
            FtueState.ApplySave(true, (int)FtueStep.Done);
        else
            FtueState.ApplySave(false, (int)FtueStep.Welcome);

        if (data.version >= 5 && data.currentOffers != null)
            MovieOfferState.ApplySave(data.currentOffers);
        else
            MovieOfferState.Clear();
    }

    static bool ValidateSaveJson(string json, out string error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            error = "Empty JSON payload.";
            return false;
        }

        try
        {
            var probe = JsonUtility.FromJson<GameSaveData>(json);
            if (probe == null)
            {
                error = "Deserialized save data is null.";
                return false;
            }

            if (probe.version < 1)
            {
                error = "Invalid save version.";
                return false;
            }
        }
        catch (Exception e)
        {
            error = e.Message;
            return false;
        }

        return true;
    }

    static bool ValidateSaveFile(string path, out string error)
    {
        error = null;
        if (!File.Exists(path))
        {
            error = "Temp save file missing.";
            return false;
        }

        try
        {
            return ValidateSaveJson(File.ReadAllText(path), out error);
        }
        catch (Exception e)
        {
            error = e.Message;
            return false;
        }
    }

    static void PreserveCorruptFile(string path)
    {
        if (!File.Exists(path)) return;

        string backup = path + CorruptSuffix;
        int attempt = 0;
        while (File.Exists(backup))
        {
            attempt++;
            backup = path + CorruptSuffix + "." + attempt;
            if (attempt > 20) break;
        }

        try
        {
            File.Move(path, backup);
            Debug.LogWarning("[Save] PreservingCorruptFile → " + backup);
        }
        catch (Exception e)
        {
            Debug.LogError("[Save] PreservingCorruptFile failed — " + e.Message);
        }
    }

    static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch { /* best effort */ }
    }

    static void MigrateV1ToV2(GameSaveData data, GameHub hub)
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

    void ApplyIapData(GameSaveData data)
    {
        PendingMigrationSave = false;

        if (data.version < 7)
        {
            // MigrateV6ToV7 already calls EntitlementService.LoadFromSave internally.
            PendingMigrationSave = MigrateV6ToV7(data);
        }
        else
        {
            // v7+ save: iap may be null if the save was written before the field existed
            // or if JSON deserialization produced a partial object. Build a safe fallback
            // that preserves NoAds ownership inferred from premiumFeatures rather than
            // silently resetting the player's entitlements to an empty state.
            var iapToLoad = data.iap;
            if (iapToLoad == null)
            {
                iapToLoad = new IapSaveData
                {
                    fulfilledTransactions = Array.Empty<IapTransactionEntry>(),
                    noAdsOwned            = data.premiumFeatures?.noAdsPurchased == true,
                };
                Debug.LogWarning("[Save] v7 save has null iap block — reconstructed minimal entitlements from premiumFeatures.");
            }
            EntitlementService.LoadFromSave(iapToLoad);
        }

        EntitlementService.SyncPremiumFeaturesFromIap();
    }

    static bool MigrateV6ToV7(GameSaveData data)
    {
        data.iap ??= new IapSaveData { fulfilledTransactions = Array.Empty<IapTransactionEntry>() };
        if (data.iap.fulfilledTransactions == null)
            data.iap.fulfilledTransactions = Array.Empty<IapTransactionEntry>();

        bool migrated = false;

        if (PlayerPrefs.GetInt(LegacyPackPurchasedKeyPrefix + "1", 0) == 1)
        {
            data.iap.packSupporterOwned = true;
            data.iap.packSupporterRewardsDelivered = true;
            migrated = true;
        }

        if (PlayerPrefs.GetInt(LegacyPackPurchasedKeyPrefix + "2", 0) == 1)
        {
            data.iap.packProducerOwned = true;
            data.iap.packProducerRewardsDelivered = true;
            migrated = true;
        }

        if (PlayerPrefs.GetInt(LegacyPackPurchasedKeyPrefix + "3", 0) == 1)
        {
            data.iap.packExecutiveOwned = true;
            data.iap.packExecutiveRewardsDelivered = true;
            migrated = true;
        }

        data.premiumFeatures ??= new PremiumFeaturesSaveData();
        if (data.premiumFeatures.noAdsPurchased)
            data.iap.noAdsOwned = true;

        if (data.iap.packSupporterOwned || data.iap.packProducerOwned || data.iap.packExecutiveOwned)
        {
            data.iap.noAdsOwned = true;
            data.premiumFeatures.noAdsPurchased = true;
        }

        if (migrated)
        {
            if (data.iap.packSupporterOwned)
                AppendLegacyFulfillmentMarker(data.iap, IapProductCatalog.PackSupporter, "legacy:pack_supporter");
            if (data.iap.packProducerOwned)
                AppendLegacyFulfillmentMarker(data.iap, IapProductCatalog.PackProducer, "legacy:pack_producer");
            if (data.iap.packExecutiveOwned)
                AppendLegacyFulfillmentMarker(data.iap, IapProductCatalog.PackExecutive, "legacy:pack_executive");

            PlayerPrefs.DeleteKey(LegacyPackPurchasedKeyPrefix + "1");
            PlayerPrefs.DeleteKey(LegacyPackPurchasedKeyPrefix + "2");
            PlayerPrefs.DeleteKey(LegacyPackPurchasedKeyPrefix + "3");
            PlayerPrefs.Save();
            Debug.Log("[Save] Migrated pack.purchased.* PlayerPrefs → IapSaveData v7.");
        }

        data.version = 7;
        EntitlementService.LoadFromSave(data.iap);
        return true;
    }

    static void AppendLegacyFulfillmentMarker(IapSaveData iap, string productId, string markerId)
    {
        foreach (var e in iap.fulfilledTransactions ?? Array.Empty<IapTransactionEntry>())
        {
            if (e != null && e.transactionId == markerId)
                return;
        }

        var list = new System.Collections.Generic.List<IapTransactionEntry>(iap.fulfilledTransactions ?? Array.Empty<IapTransactionEntry>());
        list.Add(new IapTransactionEntry
        {
            transactionId = markerId,
            productId       = productId,
            fulfilledUnix   = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        });
        iap.fulfilledTransactions = list.ToArray();
    }

    public void DeleteSave()
    {
        TryDeleteFile(SavePath);
        TryDeleteFile(TempSavePath);
        TryDeleteFile(LegacySavePath);
        Debug.Log("[SaveSystem] Save deleted.");
    }
}
