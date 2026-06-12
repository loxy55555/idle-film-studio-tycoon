#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Phase 8.2 — save stability and UI event stabilization audit.</summary>
public static class Phase82StabilizationAuditTools
{
    const string SaveStabilityReport            = "Assets/Data/Reports/SaveStabilityReport.txt";
    const string OfferRefreshFixReport          = "Assets/Data/Reports/OfferRefreshFixReport.txt";
    const string ContractLayoutFixReport        = "Assets/Data/Reports/ContractCandidateLayoutFixReport.txt";
    const string UiEventStabilizationReport     = "Assets/Data/Reports/UiEventStabilizationReport.txt";
    const string FinalReport                    = "Assets/Data/Reports/Phase8_2FinalReport.txt";

    [MenuItem("IdleFilm/Reports/Generate Phase 8.2 Stabilization Reports")]
    public static void GenerateAll()
    {
        Directory.CreateDirectory("Assets/Data/Reports");

        bool saveStability   = VerifySaveStability();
        bool offerRefresh    = VerifyOfferRefreshFix();
        bool contractLayout  = VerifyContractLayoutFix();
        bool uiEvents        = VerifyUiEventStabilization();
        bool overall         = saveStability && offerRefresh && contractLayout && uiEvents;

        File.WriteAllText(SaveStabilityReport,        BuildSaveStabilityReport(saveStability), Encoding.UTF8);
        File.WriteAllText(OfferRefreshFixReport,      BuildOfferRefreshFixReport(offerRefresh), Encoding.UTF8);
        File.WriteAllText(ContractLayoutFixReport,    BuildContractLayoutFixReport(contractLayout), Encoding.UTF8);
        File.WriteAllText(UiEventStabilizationReport, BuildUiEventStabilizationReport(uiEvents), Encoding.UTF8);
        File.WriteAllText(FinalReport,                BuildFinalReport(overall, saveStability, offerRefresh, contractLayout, uiEvents), Encoding.UTF8);

        AssetDatabase.Refresh();
        Debug.Log("[Phase8.2] Reports written to Assets/Data/Reports/");
    }

    // ─── P0-1 / P0-2 Save stability ─────────────────────────────────────────

    static bool VerifySaveStability()
    {
        if (!File.Exists("Assets/Scripts/SaveSystem.cs")) return false;
        if (!File.Exists("Assets/Scripts/Core/MovieOfferState.cs")) return false;

        string save  = File.ReadAllText("Assets/Scripts/SaveSystem.cs");
        string state = File.ReadAllText("Assets/Scripts/Core/MovieOfferState.cs");

        bool reasonParam       = save.Contains("Save(string reason)");
        bool reasonLog         = save.Contains("[Save] Reason=");
        bool tmpWrite          = save.Contains(".tmp") && save.Contains("TempSavePath");
        bool validate          = save.Contains("ValidateSaveJson");
        bool preserveCorrupt   = save.Contains("PreservingCorruptFile");
        bool loadFailedLog     = save.Contains("[Save] LoadFailed");
        bool loadGameEnum      = save.Contains("SaveLoadResult");
        bool noAutoSaveUpdate  = !save.Contains("AUTO_SAVE_INTERVAL") || !save.Contains("autoSaveTimer += Time.deltaTime");
        bool noPersistSave     = !state.Contains("PersistSave");

        return reasonParam && reasonLog && tmpWrite && validate && preserveCorrupt
            && loadFailedLog && loadGameEnum && noAutoSaveUpdate && noPersistSave;
    }

    static string BuildSaveStabilityReport(bool pass)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SAVE STABILITY REPORT (Phase 8.2)");
        sb.AppendLine("Generated: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        sb.AppendLine($"OVERALL: {(pass ? "PASS" : "FAIL")}");
        sb.AppendLine();
        sb.AppendLine("--- Event-driven saves ---");
        sb.AppendLine("Save(string reason) with [Save] Reason= logs");
        sb.AppendLine("Removed: PersistSave() from MovieOfferState");
        sb.AppendLine("Removed: 30s auto-save timer");
        sb.AppendLine("Allowed reasons: StartMovie, MovieCompleted, ContractSelected,");
        sb.AppendLine("  ContractClaimed, RefreshOffers, RefreshContracts, PurchaseUpgrade,");
        sb.AppendLine("  FtueStep, FtueCompletion, ApplicationQuit, ApplicationPause");
        sb.AppendLine();
        sb.AppendLine("--- Atomic write ---");
        sb.AppendLine("Write save_v2.tmp → validate → replace save_v2.json");
        sb.AppendLine();
        sb.AppendLine("--- Corrupt load handling ---");
        sb.AppendLine("[Save] LoadFailed + [Save] PreservingCorruptFile");
        sb.AppendLine("Corrupt file renamed to .corrupt — not overwritten on init");
        sb.AppendLine("GenerateCandidates no longer auto-saves on new game init");
        return sb.ToString();
    }

    // ─── P0-3 Offer refresh button ────────────────────────────────────────────

    static bool VerifyOfferRefreshFix()
    {
        if (!File.Exists("Assets/Scripts/UI/Hub/Hud/ProductionHudShell.cs")) return false;
        if (!File.Exists("Assets/Scripts/Core/GameplayRefreshService.cs")) return false;

        string shell    = File.ReadAllText("Assets/Scripts/UI/Hub/Hud/ProductionHudShell.cs");
        string refresh  = File.ReadAllText("Assets/Scripts/Core/GameplayRefreshService.cs");

        bool awakeWire       = shell.Contains("void Awake()") && shell.Contains("WireRefreshButton");
        bool onEnableWire    = shell.Contains("void OnEnable()") && shell.Contains("WireRefreshButton");
        bool refreshClicked  = shell.Contains("[Offers] RefreshClicked");
        bool refreshLogs     = refresh.Contains("[Offers] RefreshConfirmed");
        bool regenerateLog   = refresh.Contains("[Offers] Regenerated") || refresh.Contains("ForceRegenerateAll");
        bool saveReason      = refresh.Contains("Save(\"RefreshOffers\")");

        return awakeWire && onEnableWire && refreshClicked && refreshLogs && regenerateLog && saveReason;
    }

    static string BuildOfferRefreshFixReport(bool pass)
    {
        var sb = new StringBuilder();
        sb.AppendLine("OFFER REFRESH FIX REPORT (Phase 8.2)");
        sb.AppendLine("Generated: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        sb.AppendLine($"OVERALL: {(pass ? "PASS" : "FAIL")}");
        sb.AppendLine();
        sb.AppendLine("--- Root cause (Phase 8.1B) ---");
        sb.AppendLine("ProductionHudShell.Configure() only wired onClick in editor build.");
        sb.AppendLine();
        sb.AppendLine("--- Fix ---");
        sb.AppendLine("WireRefreshButton() in Awake() + OnEnable()");
        sb.AppendLine("Flow: RefreshClicked → RefreshChoiceDialog → RefreshConfirmed");
        sb.AppendLine("  → ForceRegenerateAll(activeProductions) → RefreshOfferDisplay()");
        sb.AppendLine("  → Save(RefreshOffers)");
        return sb.ToString();
    }

    // ─── P1-1 Contract candidate layout ─────────────────────────────────────

    static bool VerifyContractLayoutFix()
    {
        if (!File.Exists("Assets/Scripts/UI/Hub/ContractsPanelUI.cs")) return false;
        string panel = File.ReadAllText("Assets/Scripts/UI/Hub/ContractsPanelUI.cs");
        return panel.Contains("FixedSlotHeightPublic = 148f");
    }

    static string BuildContractLayoutFixReport(bool pass)
    {
        var sb = new StringBuilder();
        sb.AppendLine("CONTRACT CANDIDATE LAYOUT FIX REPORT (Phase 8.2)");
        sb.AppendLine("Generated: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        sb.AppendLine($"OVERALL: {(pass ? "PASS" : "FAIL")}");
        sb.AppendLine();
        sb.AppendLine("--- Root cause (Phase 8.1B) ---");
        sb.AppendLine("FixedSlotHeightPublic = 110px; content ~148px → select button clipped.");
        sb.AppendLine();
        sb.AppendLine("--- Fix ---");
        sb.AppendLine("FixedSlotHeightPublic = 148f");
        sb.AppendLine("Visible: title, description, objective, reward, select button");
        return sb.ToString();
    }

    // ─── P1-2 / P1-3 UI event stabilization ──────────────────────────────────

    static bool VerifyUiEventStabilization()
    {
        if (!File.Exists("Assets/Scripts/UI/Hub/MovieTabUI.cs")) return false;
        if (!File.Exists("Assets/Scripts/Core/MovieOfferState.cs")) return false;
        if (!File.Exists("Assets/Scripts/Managers/StudioManager.cs")) return false;

        string tab     = File.ReadAllText("Assets/Scripts/UI/Hub/MovieTabUI.cs");
        string state   = File.ReadAllText("Assets/Scripts/Core/MovieOfferState.cs");
        string studio  = File.ReadAllText("Assets/Scripts/Managers/StudioManager.cs");

        bool offersEvent       = state.Contains("OnOffersChanged");
        bool tabSubscribes     = tab.Contains("MovieOfferState.OnOffersChanged += RefreshOfferDisplay");
        bool noProdChanged     = !tab.Contains("OnProductionsChanged += OnProductionsChanged");
        bool noProdHandler       = !tab.Contains("void OnProductionsChanged()");
        bool startMovieSave      = studio.Contains("Save(\"StartMovie\")");
        bool movieCompletedSave  = studio.Contains("Save(\"MovieCompleted\")");
        bool syncOffers          = tab.Contains("MovieOfferState.SyncOffers");

        return offersEvent && tabSubscribes && noProdChanged && noProdHandler
            && startMovieSave && movieCompletedSave && syncOffers;
    }

    static string BuildUiEventStabilizationReport(bool pass)
    {
        var sb = new StringBuilder();
        sb.AppendLine("UI EVENT STABILIZATION REPORT (Phase 8.2)");
        sb.AppendLine("Generated: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        sb.AppendLine($"OVERALL: {(pass ? "PASS" : "FAIL")}");
        sb.AppendLine();
        sb.AppendLine("--- Offer refresh triggers (event-driven) ---");
        sb.AppendLine("MovieOfferState.OnOffersChanged fires on:");
        sb.AppendLine("  • NotifyProductionStarted (consume + refill → D B C)");
        sb.AppendLine("  • OnMovieCompleted (prune + refill)");
        sb.AppendLine("  • ForceRegenerateAll (reroll)");
        sb.AppendLine();
        sb.AppendLine("--- Removed per-frame path ---");
        sb.AppendLine("MovieTabUI no longer subscribes to OnProductionsChanged");
        sb.AppendLine("RefreshProductionUI still updates production widget only");
        sb.AppendLine();
        sb.AppendLine("--- Production visual (A B C → D B C) ---");
        sb.AppendLine("StartMovie → NotifyProductionStarted → OnOffersChanged → single UI rebuild");
        return sb.ToString();
    }

    static string BuildFinalReport(bool overall, bool save, bool offer, bool contract, bool ui)
    {
        var sb = new StringBuilder();
        sb.AppendLine("PHASE 8.2 FINAL REPORT");
        sb.AppendLine("Generated: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        sb.AppendLine($"OVERALL: {(overall ? "PASS" : "FAIL")}");
        sb.AppendLine();
        sb.AppendLine($"  Save stability              : {(save ? "PASS" : "FAIL")}");
        sb.AppendLine($"  Offer refresh button        : {(offer ? "PASS" : "FAIL")}");
        sb.AppendLine($"  Contract candidate layout   : {(contract ? "PASS" : "FAIL")}");
        sb.AppendLine($"  UI event stabilization      : {(ui ? "PASS" : "FAIL")}");
        sb.AppendLine();
        sb.AppendLine("Expected outcomes:");
        sb.AppendLine("  0 saves per frame");
        sb.AppendLine("  0 reinicios de partida (corrupt save preserved)");
        sb.AppendLine("  0 botones desconectados");
        sb.AppendLine("  0 clipping de contratos");
        sb.AppendLine("  0 reconstrucciones continuas de ofertas");
        return sb.ToString();
    }
}
#endif
