#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// FASE 21 — Editor-only marketing save for Play Store captures and promo material.
/// Does not ship to production builds; never auto-overwrites the player save.
/// </summary>
public static class MarketingSaveGenerator
{
    const string MenuRoot = "Tools/Idle Film/Generate Marketing Save";
    const string MarketingFolder = "Assets/Data/Marketing";
    const string MarketingFileName = "marketing_save_v7.json";

    const string ActiveSaveFile = "save_v2.json";
    const string UserBackupFile = "save_v2.user_backup.json";

    const int MarketingCityLevel = 5;
    const int MarketingOscars = 12;
    const long MarketingMoney = 100_000_000L;
    const int MarketingDiamonds = 5000;
    const float MarketingReputation = 35_000f;
    const int MarketingStudioLevel = 18;
    const float MarketingStudioXp = 420f;

    static string MarketingAssetPath => $"{MarketingFolder}/{MarketingFileName}";
    static string ActiveSavePath => Path.Combine(Application.persistentDataPath, ActiveSaveFile);
    static string UserBackupPath => Path.Combine(Application.persistentDataPath, UserBackupFile);

    [MenuItem(MenuRoot)]
    public static void GenerateMarketingSave()
    {
        Directory.CreateDirectory(MarketingFolder);

        var data = BuildMarketingSave();
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(MarketingAssetPath, json);
        AssetDatabase.Refresh();

        Debug.Log($"[MarketingSave] Generated → {MarketingAssetPath}\n" + DescribeContents(data));
        EditorUtility.DisplayDialog(
            "Marketing Save Generated",
            $"Saved to:\n{MarketingAssetPath}\n\n" +
            "To use in Play Mode:\n" +
            "Tools → Idle Film → Marketing Save → Install Marketing Save\n\n" +
            "Your current save is backed up automatically before install.",
            "OK");
    }

    [MenuItem("Tools/Idle Film/Marketing Save/Install Marketing Save")]
    public static void InstallMarketingSave()
    {
        if (!File.Exists(MarketingAssetPath))
        {
            EditorUtility.DisplayDialog("Marketing Save", "Generate the marketing save first.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Install Marketing Save",
                "This replaces the active Play Mode save (save_v2.json) with the marketing snapshot.\n\n" +
                "Your current save will be copied to save_v2.user_backup.json first.\n\n" +
                "Continue?",
                "Install", "Cancel"))
            return;

        try
        {
            BackupActiveSaveIfPresent();
            File.Copy(MarketingAssetPath, ActiveSavePath, overwrite: true);
            Debug.Log($"[MarketingSave] Installed → {ActiveSavePath}");
            EditorUtility.DisplayDialog(
                "Marketing Save Installed",
                "Enter Play Mode (or restart Play Mode) to load the marketing snapshot.\n\n" +
                $"Active save: {ActiveSavePath}",
                "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError("[MarketingSave] Install failed — " + ex.Message);
            EditorUtility.DisplayDialog("Install Failed", ex.Message, "OK");
        }
    }

    [MenuItem("Tools/Idle Film/Marketing Save/Restore User Save from Backup")]
    public static void RestoreUserSave()
    {
        if (!File.Exists(UserBackupPath))
        {
            EditorUtility.DisplayDialog(
                "Restore User Save",
                "No backup found (save_v2.user_backup.json).\n\n" +
                "A backup is created when you install the marketing save.",
                "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Restore User Save",
                "Restore your pre-marketing save from backup?",
                "Restore", "Cancel"))
            return;

        try
        {
            File.Copy(UserBackupPath, ActiveSavePath, overwrite: true);
            Debug.Log($"[MarketingSave] Restored user save → {ActiveSavePath}");
            EditorUtility.DisplayDialog(
                "User Save Restored",
                "Restart Play Mode to load your normal save.",
                "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError("[MarketingSave] Restore failed — " + ex.Message);
            EditorUtility.DisplayDialog("Restore Failed", ex.Message, "OK");
        }
    }

    [MenuItem(MenuRoot, true)]
    [MenuItem("Tools/Idle Film/Marketing Save/Install Marketing Save", true)]
    [MenuItem("Tools/Idle Film/Marketing Save/Restore User Save from Backup", true)]
    static bool MenuEnabled() => !EditorApplication.isCompiling;

    static void BackupActiveSaveIfPresent()
    {
        if (!File.Exists(ActiveSavePath)) return;
        File.Copy(ActiveSavePath, UserBackupPath, overwrite: true);
        Debug.Log($"[MarketingSave] Backed up user save → {UserBackupPath}");
    }

    static GameSaveData BuildMarketingSave()
    {
        var movies = LoadAllMovies();
        var completed = CollectCompletedMovieKeys(movies, MarketingCityLevel);
        var productionMovies = PickProductionMovies(movies, completed, 2);
        var offerMovies = PickOfferMovies(movies, completed, productionMovies, 3);

        var data = new GameSaveData
        {
            version = 7,
            money = MarketingMoney,
            moneyExact = MarketingMoney,
            reputation = MarketingReputation,
            diamonds = MarketingDiamonds,

            editor = 5,
            director = 6,
            actors = 5,
            sound = 4,
            cinematography = 4,
            makeup = 3,
            costume = 3,
            art = 2,
            lighting = 4,
            grip = 3,
            producer = 2,

            oscars = MarketingOscars,
            cityLevel = MarketingCityLevel,

            studioLevel = MarketingStudioLevel,
            studioXP = MarketingStudioXp,

            upgradeLevels = BuildUpgradeLevels(),
            completedContractIds = new[]
            {
                "c_first_movie", "c_three_movies", "c_first_upgrade",
                "c_rep_50", "c_drama_fan", "c_rep_200", "c_studio_level5",
            },

            saveTimestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            contractStates = BuildContractCandidates(),
            contractHistoryIds = new[] { "c_first_movie", "c_rep_50", "c_horror_night" },

            activeProductions = BuildActiveProductions(productionMovies),
            movieHistory = BuildMovieHistory(completed),
            recentProductionKeys = BuildRecentKeys(completed),
            completedMovieKeys = completed,

            ftueCompleted = true,
            ftueStep = (int)FtueStep.Done,

            currentOffers = offerMovies,

            activeBoosts = Array.Empty<BoostSaveEntry>(),
            premiumFeatures = new PremiumFeaturesSaveData
            {
                noAdsPurchased = false,
                offlinePremiumUnlocked = false,
            },
            iap = new IapSaveData
            {
                fulfilledTransactions = Array.Empty<IapTransactionEntry>(),
            },
        };

        return data;
    }

    static UpgradeSaveEntry[] BuildUpgradeLevels() => new[]
    {
        Entry("install_plato_small", 1),
        Entry("equip_camera_basic", 3),
        Entry("equip_camera_pro", 4),
        Entry("equip_lighting_kit", 3),
        Entry("equip_mic_pro", 2),
        Entry("equip_editing_soft", 3),
        Entry("personal_director", 5),
        Entry("personal_actors", 4),
        Entry("personal_editor", 4),
        Entry("personal_sound", 3),
        Entry("install_offices", 2),
        Entry("install_editing_room", 2),
        Entry("install_exterior", 1),
        Entry("mkt_social_media", 2),
        Entry("mkt_trailer", 1),
    };

    static UpgradeSaveEntry Entry(string id, int level) =>
        new UpgradeSaveEntry { id = id, level = level };

    static ContractSaveEntry[] BuildContractCandidates() => new[]
    {
        Candidate("c_blockbuster"),
        Candidate("c_comedy_king"),
        Candidate("c_big_spender"),
    };

    static ContractSaveEntry Candidate(string id) => new ContractSaveEntry
    {
        id = id,
        progress = 0f,
        readyToClaim = false,
        genreMask = 0,
        isActiveContract = false,
    };

    static ProductionSaveEntry[] BuildActiveProductions(MovieConfig[] picks)
    {
        if (picks == null || picks.Length == 0)
            return Array.Empty<ProductionSaveEntry>();

        var list = new List<ProductionSaveEntry>(picks.Length);
        float[] progressFractions = { 0.42f, 0.58f };

        for (int i = 0; i < picks.Length; i++)
        {
            var cfg = picks[i];
            if (cfg == null) continue;

            float duration = Mathf.Max(cfg.duration, 120f);
            float elapsed = duration * progressFractions[i % progressFractions.Length];
            long reward = (long)(cfg.baseReward * cfg.quality * 2.5f);
            float rep = cfg.baseRep * 2.2f;
            float xp = StudioLevelSystem.BaseMovieXP(cfg);

            list.Add(new ProductionSaveEntry
            {
                movieKey = cfg.name,
                elapsedSeconds = elapsed,
                totalDuration = duration,
                pendingReward = reward,
                pendingRep = rep,
                pendingXp = xp,
                awaitingDiscovery = false,
            });
        }

        return list.ToArray();
    }

    static MovieHistorySaveEntry[] BuildMovieHistory(string[] completed)
    {
        int count = Mathf.Min(12, completed?.Length ?? 0);
        if (count == 0) return Array.Empty<MovieHistorySaveEntry>();

        var list = new List<MovieHistorySaveEntry>(count);
        for (int i = 0; i < count; i++)
        {
            list.Add(new MovieHistorySaveEntry
            {
                movieName = completed[i],
                moneyReward = 25_000 + i * 4_500,
                repGain = 8f + i * 0.6f,
                xpGain = 40f + i * 3f,
            });
        }

        return list.ToArray();
    }

    static string[] BuildRecentKeys(string[] completed)
    {
        if (completed == null || completed.Length == 0)
            return Array.Empty<string>();

        int start = Mathf.Max(0, completed.Length - 5);
        int count = completed.Length - start;
        var slice = new string[count];
        Array.Copy(completed, start, slice, 0, count);
        return slice;
    }

    static string[] CollectCompletedMovieKeys(MovieConfig[] movies, int cityLevel)
    {
        var keys = new List<string>();
        foreach (var cfg in movies)
        {
            if (cfg == null || string.IsNullOrEmpty(cfg.name)) continue;
            if (CityProgressionRules.GetMovieRequiredCity(cfg) > cityLevel) continue;
            if (cfg.unlockStudioLevel > MarketingStudioLevel) continue;
            keys.Add(cfg.name);
        }

        keys.Sort(StringComparer.Ordinal);
        return keys.ToArray();
    }

    static MovieConfig[] PickProductionMovies(MovieConfig[] movies, string[] completed, int count)
    {
        var picks = new List<MovieConfig>(count);
        string[] preferred =
        {
            "Action_Iron_Descent",
            "Drama_Northbound",
            "Thriller_Shadow_Trigger",
            "SciFi_Redshift",
        };

        foreach (var id in preferred)
        {
            if (picks.Count >= count) break;
            var cfg = FindMovie(movies, id);
            if (cfg != null && cfg.rarity != MovieRarity.Legendary)
                picks.Add(cfg);
        }

        foreach (var cfg in movies)
        {
            if (picks.Count >= count) break;
            if (cfg == null || cfg.rarity == MovieRarity.Legendary) continue;
            if (CityProgressionRules.GetMovieRequiredCity(cfg) > MarketingCityLevel) continue;
            if (Array.IndexOf(completed, cfg.name) < 0) continue;
            if (picks.Exists(p => p.name == cfg.name)) continue;
            picks.Add(cfg);
        }

        return picks.ToArray();
    }

    static string[] PickOfferMovies(MovieConfig[] movies, string[] completed, MovieConfig[] inProduction, int count)
    {
        var exclude = new HashSet<string>(StringComparer.Ordinal);
        if (inProduction != null)
        {
            foreach (var cfg in inProduction)
            {
                if (cfg != null) exclude.Add(cfg.name);
            }
        }

        var offers = new List<string>(count);
        foreach (var cfg in movies)
        {
            if (offers.Count >= count) break;
            if (cfg == null || string.IsNullOrEmpty(cfg.name)) continue;
            if (exclude.Contains(cfg.name)) continue;
            if (cfg.rarity == MovieRarity.Legendary) continue;
            if (CityProgressionRules.GetMovieRequiredCity(cfg) > MarketingCityLevel) continue;
            if (cfg.unlockStudioLevel > MarketingStudioLevel) continue;
            if (Array.IndexOf(completed, cfg.name) >= 0) continue;
            offers.Add(cfg.name);
        }

        while (offers.Count < count && completed.Length > offers.Count)
        {
            string key = completed[offers.Count];
            if (!exclude.Contains(key) && !offers.Contains(key))
                offers.Add(key);
            else
                break;
        }

        return offers.ToArray();
    }

    static MovieConfig FindMovie(MovieConfig[] movies, string name)
    {
        foreach (var cfg in movies)
        {
            if (cfg != null && cfg.name == name)
                return cfg;
        }

        return null;
    }

    static MovieConfig[] LoadAllMovies()
    {
        var guids = AssetDatabase.FindAssets("t:MovieConfig", new[] { MovieCatalogDatabase.MoviesFolder });
        var list = new List<MovieConfig>(guids.Length);
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var cfg = AssetDatabase.LoadAssetAtPath<MovieConfig>(path);
            if (cfg != null)
                list.Add(cfg);
        }

        list.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
        return list.ToArray();
    }

    static string DescribeContents(GameSaveData data)
    {
        int movies = data.completedMovieKeys?.Length ?? 0;
        int productions = data.activeProductions?.Length ?? 0;
        int contracts = data.contractStates?.Length ?? 0;
        int upgrades = data.upgradeLevels?.Length ?? 0;
        return
            $"Money=${data.money:N0} | Diamonds={data.diamonds} | Rep={data.reputation:N0} | " +
            $"Oscars={data.oscars} | City={data.cityLevel} | Studio Lv={data.studioLevel}\n" +
            $"Discovered movies={movies} | Active productions={productions} | " +
            $"Contract candidates={contracts} | Upgrades purchased={upgrades}";
    }
}
#endif
