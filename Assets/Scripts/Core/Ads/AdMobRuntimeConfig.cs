using System;
using UnityEngine;

/// <summary>FASE 18 — AdMob App ID and Rewarded Ad Unit ID (Resources/AdMobConfig.json).</summary>
[Serializable]
public class AdMobConfigData
{
    public string androidAppId;
    public string androidRewardedAdUnitId;
    public bool useTestAdsInEditor = true;
}

public static class AdMobRuntimeConfig
{
    const string ResourcePath = "AdMobConfig";

    const string TestAndroidAppId = "ca-app-pub-3940256099942544~3347511713";
    const string TestAndroidRewardedId = "ca-app-pub-3940256099942544/5224354917";

    static AdMobConfigData _cached;

    public static AdMobConfigData Load()
    {
        if (_cached != null) return _cached;

        var asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset != null && !string.IsNullOrWhiteSpace(asset.text))
        {
            try
            {
                _cached = JsonUtility.FromJson<AdMobConfigData>(asset.text);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AdMob] Failed to parse AdMobConfig.json — using test IDs. " + ex.Message);
            }
        }

        _cached ??= new AdMobConfigData();
        if (string.IsNullOrWhiteSpace(_cached.androidAppId))
            _cached.androidAppId = TestAndroidAppId;
        if (string.IsNullOrWhiteSpace(_cached.androidRewardedAdUnitId))
            _cached.androidRewardedAdUnitId = TestAndroidRewardedId;

        return _cached;
    }

    public static string RewardedAdUnitId
    {
        get
        {
            var cfg = Load();
#if UNITY_EDITOR
            if (cfg.useTestAdsInEditor)
                return TestAndroidRewardedId;
#endif
            return cfg.androidRewardedAdUnitId;
        }
    }

    public static string AndroidAppId => Load().androidAppId;
}
