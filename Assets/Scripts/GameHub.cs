using System;

using UnityEngine;

using UnityEngine.EventSystems;



/// <summary>

/// Central service locator. DefaultExecutionOrder(-100) guarantees all systems

/// are ready before any UI MonoBehaviour's Start() runs.

/// </summary>

[DefaultExecutionOrder(-100)]

public class GameHub : MonoBehaviour

{

    public static GameHub Instance { get; private set; }



    [Header("Core Systems")]

    public StudioManager      studio;

    public DepartmentSystem   departments;

    public PrestigeSystem     prestige;

    public SaveSystem         save;



    [Header("New Systems (v2)")]

    public UpgradeSystem      upgrades;

    public StudioLevelSystem  studioLevel;

    public ContractSystem     contracts;

    public DiamondWallet      diamonds;

    public CitySystem         city;



    /// <summary>Set during load when v2 save has no contract snapshot.</summary>

    [NonSerialized] public bool pendingLegacyContractRefresh;



    /// <summary>Fired after all systems are initialized and any save has been applied.</summary>

    public static event Action OnGameReady;



    private void Awake()

    {

        if (Instance != null && Instance != this) { Destroy(gameObject); return; }

        Instance = this;



        EnsureEventSystem();

        FixCanvasScale();

        EnsureDiamondWallet();



        if (studio      == null) studio      = FindAnyObjectByType<StudioManager>();

        if (departments == null) departments = FindAnyObjectByType<DepartmentSystem>();

        if (prestige    == null) prestige    = FindAnyObjectByType<PrestigeSystem>();

        if (save        == null) save        = GetComponentInChildren<SaveSystem>();

        if (upgrades    == null) upgrades    = FindAnyObjectByType<UpgradeSystem>();

        if (studioLevel == null) studioLevel = FindAnyObjectByType<StudioLevelSystem>();

        if (contracts   == null) contracts   = FindAnyObjectByType<ContractSystem>();

        if (city        == null) city        = FindAnyObjectByType<CitySystem>();
        if (city        == null) city        = gameObject.AddComponent<CitySystem>();

        if (GetComponent<FtueController>() == null)
            gameObject.AddComponent<FtueController>();

        if (GetComponent<PremiereSequenceController>() == null)
            gameObject.AddComponent<PremiereSequenceController>();

        if (studio      == null) Debug.LogError("GameHub: StudioManager not found!");

        if (departments == null) Debug.LogError("GameHub: DepartmentSystem not found!");

        if (prestige    == null) Debug.LogError("GameHub: PrestigeSystem not found!");

    }



    void EnsureDiamondWallet()

    {

        if (diamonds != null) return;

        diamonds = GetComponentInChildren<DiamondWallet>();

        if (diamonds != null) return;



        diamonds = gameObject.AddComponent<DiamondWallet>();

    }



    private void Start()

    {

        pendingLegacyContractRefresh = false;

        SaveLoadResult loadResult = save != null ? save.LoadGame() : SaveLoadResult.NotFound;

        if (loadResult == SaveLoadResult.Success)
        {
            upgrades?.ResyncAllPersonnel(departments);

            contracts?.RecoverAfterLoad(studioLevel?.Level ?? 1);

            studio.NotifyAll();

            city?.BindRuntime(prestige, studio);
            studio.RecalculateIncome();
        }
        else
        {
            if (loadResult == SaveLoadResult.Corrupt)
                Debug.LogWarning("[Save] Corrupt save preserved — session starts without overwriting backup file.");

            studio.Initialize();

            departments.Init();

            prestige.Init();

            city?.Init();
            city?.BindRuntime(prestige, studio);

            upgrades?.Init();

            studioLevel?.Init();

            diamonds?.Init();

            contracts?.Init(1);
            contracts?.EnsureActiveContracts(1);

            MovieOfferState.Clear();
            FtueState.Reset();
        }



        pendingLegacyContractRefresh = false;

        OnGameReady?.Invoke();

    }



    static void FixCanvasScale()

    {

        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))

        {

            if (canvas.transform.localScale.sqrMagnitude < 0.001f)

            {

                canvas.transform.localScale = Vector3.one;

                Debug.LogWarning($"[GameHub] Fixed Canvas '{canvas.name}' scale (was 0).");

            }

        }

    }



    static void EnsureEventSystem()

    {

        var existing = FindAnyObjectByType<EventSystem>();

        if (existing != null)

        {

            EnsureInputModule(existing.gameObject);

            return;

        }



        var go = new GameObject("EventSystem");

        go.AddComponent<EventSystem>();

        EnsureInputModule(go);

        Debug.Log("[GameHub] EventSystem created at runtime.");

    }



    static void EnsureInputModule(GameObject eventSystemGo)

    {

        var inputSystemModule = System.Type.GetType(

            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

        if (inputSystemModule != null)

        {

            if (eventSystemGo.GetComponent(inputSystemModule) == null)

                eventSystemGo.AddComponent(inputSystemModule);



            var legacy = eventSystemGo.GetComponent<StandaloneInputModule>();

            if (legacy != null) Destroy(legacy);

            return;

        }



        if (eventSystemGo.GetComponent<StandaloneInputModule>() == null)

            eventSystemGo.AddComponent<StandaloneInputModule>();

    }



    public void ClaimOscar()
    {
        if (prestige == null || studio == null) return;
        if (!prestige.TryClaimOscar(studio.reputation)) return;
        save?.Save("ClaimOscar");
    }

    /// <summary>Legacy alias — permanent Oscar claim, no reset.</summary>
    public void Prestige() => ClaimOscar();

}


