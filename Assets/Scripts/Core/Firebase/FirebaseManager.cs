using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

/// <summary>
/// FASE 16.0B — Firebase bootstrap (Analytics + Crashlytics).
/// Singleton, DontDestroyOnLoad. Initializes after GameHub; never blocks startup.
/// Uses reflection so the project compiles before GooglePackages .tgz are present.
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    public bool IsReady { get; private set; }
    public bool IsAnalyticsReady { get; private set; }

    bool _initRequested;
    bool _sessionActive;
    float _sessionStartTime;

    object _checkTask;
    MethodInfo _continueWithOnMainThread;
    MethodInfo _logEventString;
    MethodInfo _logEventStringLong;
    MethodInfo _logEventStringDouble;
    MethodInfo _logEventStringString;
    MethodInfo _logEventParameters;
    Type _firebaseAnalyticsType;
    Type _crashlyticsType;

    public static void EnsureOn(GameObject host)
    {
        if (Instance != null) return;
        var go = new GameObject("FirebaseManager");
        go.transform.SetParent(host.transform, false);
        go.AddComponent<FirebaseManager>();
    }

    /// <summary>Call from GameHub.Start after load/IAP init.</summary>
    public void BeginAfterGameHub()
    {
        if (_initRequested) return;
        _initRequested = true;
        StartCoroutine(InitializeRoutine());
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            EndSession();
            Instance = null;
        }
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
            EndSession();
        else if (IsAnalyticsReady)
            BeginSession();
    }

    void OnApplicationQuit() => EndSession();

    IEnumerator InitializeRoutine()
    {
        if (!TryLoadFirebaseTypes(out var firebaseAppType, out var dependencyStatusType, out var extensionsType))
        {
            Debug.LogWarning("[Firebase] SDK not installed — run Tools → Firebase → Download Packages (16.0B).");
            yield break;
        }

        var checkMethod = firebaseAppType.GetMethod("CheckAndFixDependenciesAsync", BindingFlags.Public | BindingFlags.Static);
        if (checkMethod == null)
        {
            Debug.LogWarning("[Firebase] CheckAndFixDependenciesAsync not found — continuing without Firebase.");
            yield break;
        }

        _checkTask = checkMethod.Invoke(null, null);
        if (_checkTask == null)
        {
            Debug.LogWarning("[Firebase] Dependency check returned null — continuing without Firebase.");
            yield break;
        }

        var taskType = _checkTask.GetType();
        var isCompleted = taskType.GetProperty("IsCompleted");
        while (isCompleted != null && !(bool)isCompleted.GetValue(_checkTask))
            yield return null;

        if (TryGetTaskException(_checkTask, out var fault))
        {
            Debug.LogWarning("[Firebase] Dependency check failed — continuing without Firebase. " + fault);
            yield break;
        }

        var resultProp = taskType.GetProperty("Result");
        var status = resultProp?.GetValue(_checkTask);
        var available = Enum.Parse(dependencyStatusType, "Available");
        if (status == null || !status.Equals(available))
        {
            Debug.LogWarning($"[Firebase] Dependencies unavailable ({status}) — continuing without Firebase.");
            yield break;
        }

        try
        {
            firebaseAppType.GetProperty("DefaultInstance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);

            _firebaseAnalyticsType = Type.GetType("Firebase.Analytics.FirebaseAnalytics, Firebase.Analytics");
            _crashlyticsType = Type.GetType("Firebase.Crashlytics.Crashlytics, Firebase.Crashlytics");

            if (_crashlyticsType != null)
            {
                _crashlyticsType.GetProperty("ReportUncaughtExceptionsAsFatal")?.SetValue(null, true);
                _crashlyticsType.GetProperty("IsCrashlyticsCollectionEnabled")?.SetValue(null, true);
            }

            if (_firebaseAnalyticsType != null)
            {
                _firebaseAnalyticsType.GetMethod("SetAnalyticsCollectionEnabled", BindingFlags.Public | BindingFlags.Static)
                    ?.Invoke(null, new object[] { true });

                _logEventString = _firebaseAnalyticsType.GetMethod("LogEvent", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                _logEventStringString = _firebaseAnalyticsType.GetMethod("LogEvent", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(string), typeof(string) }, null);
                _logEventStringLong = _firebaseAnalyticsType.GetMethod("LogEvent", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(string), typeof(long) }, null);
                _logEventStringDouble = _firebaseAnalyticsType.GetMethod("LogEvent", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(string), typeof(double) }, null);

                var parameterType = Type.GetType("Firebase.Analytics.Parameter, Firebase.Analytics");
                if (parameterType != null)
                {
                    _logEventParameters = _firebaseAnalyticsType.GetMethod(
                        "LogEvent",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        new[] { typeof(string), parameterType.MakeArrayType() },
                        null);
                }
            }

            IsReady = _firebaseAnalyticsType != null || _crashlyticsType != null;
            IsAnalyticsReady = _firebaseAnalyticsType != null && _logEventString != null;

            if (IsReady)
                Debug.Log("[Firebase] Initialized (Analytics + Crashlytics).");
            else
                Debug.LogWarning("[Firebase] SDK present but Analytics/Crashlytics types missing.");

            if (IsAnalyticsReady)
            {
                BeginSession();
                LogEvent(FirebaseAnalyticsEvents.GameStart);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Firebase] Initialization failed — continuing without Firebase. {ex.Message}");
        }
    }

    static bool TryLoadFirebaseTypes(out Type firebaseAppType, out Type dependencyStatusType, out Type extensionsType)
    {
        firebaseAppType = Type.GetType("Firebase.FirebaseApp, Firebase.App");
        dependencyStatusType = Type.GetType("Firebase.DependencyStatus, Firebase.App");
        extensionsType = Type.GetType("Firebase.Extensions.TaskExtension, Firebase.Extensions");
        return firebaseAppType != null && dependencyStatusType != null;
    }

    static bool TryGetTaskException(object task, out string message)
    {
        message = null;
        if (task == null) return false;
        var isFaulted = task.GetType().GetProperty("IsFaulted");
        if (isFaulted == null || !(bool)isFaulted.GetValue(task)) return false;
        var exProp = task.GetType().GetProperty("Exception");
        message = exProp?.GetValue(task)?.ToString();
        return true;
    }

    public void BeginSession()
    {
        if (!IsAnalyticsReady || _sessionActive) return;
        _sessionActive = true;
        _sessionStartTime = Time.realtimeSinceStartup;
        LogEvent(FirebaseAnalyticsEvents.SessionStart);
    }

    public void EndSession()
    {
        if (!IsAnalyticsReady || !_sessionActive) return;
        _sessionActive = false;
        float duration = Mathf.Max(0f, Time.realtimeSinceStartup - _sessionStartTime);
        LogEvent(FirebaseAnalyticsEvents.SessionEnd, "duration_sec", duration);
    }

    public void LogEvent(string eventName)
    {
        if (!IsAnalyticsReady || string.IsNullOrEmpty(eventName)) return;
        try { _logEventString?.Invoke(null, new object[] { eventName }); }
        catch (Exception ex) { Debug.LogWarning($"[Firebase] LogEvent failed ({eventName}): {ex.Message}"); }
    }

    public void LogEvent(string eventName, string paramName, string paramValue)
    {
        if (!IsAnalyticsReady || string.IsNullOrEmpty(eventName)) return;
        try { _logEventStringString?.Invoke(null, new object[] { eventName, paramName, paramValue ?? "" }); }
        catch (Exception ex) { Debug.LogWarning($"[Firebase] LogEvent failed ({eventName}): {ex.Message}"); }
    }

    public void LogEvent(string eventName, string paramName, long paramValue)
    {
        if (!IsAnalyticsReady || string.IsNullOrEmpty(eventName)) return;
        try { _logEventStringLong?.Invoke(null, new object[] { eventName, paramName, paramValue }); }
        catch (Exception ex) { Debug.LogWarning($"[Firebase] LogEvent failed ({eventName}): {ex.Message}"); }
    }

    public void LogEvent(string eventName, string paramName, double paramValue)
    {
        if (!IsAnalyticsReady || string.IsNullOrEmpty(eventName)) return;
        try { _logEventStringDouble?.Invoke(null, new object[] { eventName, paramName, paramValue }); }
        catch (Exception ex) { Debug.LogWarning($"[Firebase] LogEvent failed ({eventName}): {ex.Message}"); }
    }

    public void LogCityUnlocked(int cityId) =>
        LogEvent(FirebaseAnalyticsEvents.CityUnlocked, FirebaseAnalyticsEvents.ParamCityId, cityId);

    public void LogStarEarned(int starsTotal) =>
        LogEvent(FirebaseAnalyticsEvents.StarEarned, FirebaseAnalyticsEvents.ParamStarsTotal, starsTotal);

    public void LogMovieCompleted(string movieId) =>
        LogEvent(FirebaseAnalyticsEvents.MovieCompleted, FirebaseAnalyticsEvents.ParamMovieId, movieId ?? "");

    public void LogAdRewarded(string placement) =>
        LogEvent(FirebaseAnalyticsEvents.AdRewarded, FirebaseAnalyticsEvents.ParamPlacement, placement ?? "");

    public void LogAdStarted(string placement) =>
        LogEvent(FirebaseAnalyticsEvents.AdStarted, FirebaseAnalyticsEvents.ParamPlacement, placement ?? "");

    public void LogAdCompleted(string placement) =>
        LogEvent(FirebaseAnalyticsEvents.AdCompleted, FirebaseAnalyticsEvents.ParamPlacement, placement ?? "");

    public void LogAdFailed(string placement, string reason)
    {
        if (!IsAnalyticsReady || string.IsNullOrEmpty(FirebaseAnalyticsEvents.AdFailed)) return;
        try
        {
            var parameterType = Type.GetType("Firebase.Analytics.Parameter, Firebase.Analytics");
            if (_logEventParameters != null && parameterType != null)
            {
                var arr = Array.CreateInstance(parameterType, 2);
                arr.SetValue(Activator.CreateInstance(parameterType, FirebaseAnalyticsEvents.ParamPlacement, placement ?? ""), 0);
                arr.SetValue(Activator.CreateInstance(parameterType, FirebaseAnalyticsEvents.ParamAdReason, reason ?? "unknown"), 1);
                _logEventParameters.Invoke(null, new object[] { FirebaseAnalyticsEvents.AdFailed, arr });
                return;
            }

            LogEvent(FirebaseAnalyticsEvents.AdFailed, FirebaseAnalyticsEvents.ParamPlacement, $"{placement}|{reason}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Firebase] LogEvent failed (ad_failed): {ex.Message}");
        }
    }

    public void LogIapPurchase(string productId) =>
        LogEvent(FirebaseAnalyticsEvents.IapPurchase, FirebaseAnalyticsEvents.ParamProductId, productId ?? "");
}
