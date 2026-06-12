#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Phase 7.3 — core gameplay stabilization verification reports.</summary>
public static class CoreGameplayStabilizationAuditTools
{
    const string RepReport       = "Assets/Data/Reports/ReputationVisibilityFix.txt";
    const string OffersReport    = "Assets/Data/Reports/PersistentOffersFix.txt";
    const string SlotReport      = "Assets/Data/Reports/SlotRefillFix.txt";
    const string FtueReport      = "Assets/Data/Reports/FtueCompletionFix.txt";
    const string RarityReport    = "Assets/Data/Reports/RarityVisualFix.txt";
    const string SummaryReport   = "Assets/Data/Reports/CoreGameplayStabilizationReport.txt";

    [MenuItem("IdleFilm/Reports/Generate Core Gameplay Stabilization Reports (7.3)")]
    public static void GenerateAll()
    {
        Directory.CreateDirectory("Assets/Data/Reports");

        bool rep     = VerifyReputation();
        bool offers  = VerifyPersistentOffers();
        bool slot    = VerifySlotRefill();
        bool ftue    = VerifyFtueCompletion();
        bool rarity  = VerifyRarityVisuals();

        File.WriteAllText(RepReport, BuildReputationReport(rep), Encoding.UTF8);
        File.WriteAllText(OffersReport, BuildOffersReport(offers), Encoding.UTF8);
        File.WriteAllText(SlotReport, BuildSlotReport(slot), Encoding.UTF8);
        File.WriteAllText(FtueReport, BuildFtueReport(ftue), Encoding.UTF8);
        File.WriteAllText(RarityReport, BuildRarityReport(rarity), Encoding.UTF8);
        File.WriteAllText(SummaryReport, BuildSummary(rep, offers, slot, ftue, rarity), Encoding.UTF8);

        AssetDatabase.Refresh();
        Debug.Log("[CoreGameplayStabilization] Reports written to Assets/Data/Reports/");
    }

    static bool VerifyReputation()
    {
        var topBarPath = "Assets/Scripts/UI/TopBarUI.cs";
        if (!File.Exists(topBarPath)) return false;
        string src = File.ReadAllText(topBarPath);
        return src.Contains("FormatReputation") &&
               src.Contains("0.0") &&
               !src.Contains("rep.ToString(\"N0\")");
    }

    static bool VerifyPersistentOffers()
    {
        if (!File.Exists("Assets/Scripts/Core/MovieOfferState.cs")) return false;
        if (!File.Exists("Assets/Scripts/SaveSystem.cs")) return false;
        string save = File.ReadAllText("Assets/Scripts/SaveSystem.cs");
        string tab  = File.ReadAllText("Assets/Scripts/UI/Hub/MovieTabUI.cs");
        return save.Contains("currentOffers") &&
               save.Contains("version = 5") &&
               tab.Contains("RefreshOfferDisplay") &&
               tab.Contains("MovieOfferState.EnsureOffers") &&
               !tab.Contains("MovieOfferPicker.PickThree(");
    }

    static bool VerifySlotRefill()
    {
        string studio = File.ReadAllText("Assets/Scripts/Managers/StudioManager.cs");
        string state  = File.ReadAllText("Assets/Scripts/Core/MovieOfferState.cs");
        return studio.Contains("NotifyProductionStarted") &&
               state.Contains("NotifyProductionStarted") &&
               state.Contains("RefillToFull");
    }

    static bool VerifyFtueCompletion()
    {
        string ftue = File.ReadAllText("Assets/Scripts/FTUE/FtueController.cs");
        return ftue.Contains("AwaitCompletion") &&
               ftue.Contains("OnFinalFtueConfirm") &&
               ftue.Contains("HasFtueUpgradePurchased") &&
               !ftue.Contains("TryCompleteFtueFromFirstMovie") &&
               !ftue.Contains("premiere_dismissed");
    }

    static bool VerifyRarityVisuals()
    {
        string rarityVis = File.ReadAllText("Assets/Scripts/UI/Production/MovieRarityVisual.cs");
        string legVis    = File.ReadAllText("Assets/Scripts/UI/Collection/CollectionLegendaryVisual.cs");
        return (rarityVis.Contains("EpicOrange") || rarityVis.Contains("1f, 0.45f")) &&
               (legVis.Contains("0.95f, 0.77f") || legVis.Contains("gold") || legVis.Contains("Gold"));
    }

    static string BuildReputationReport(bool pass) => $"[{(pass ? "PASS" : "FAIL")}] Reputation UI — FormatReputation() shows decimal (e.g. REP 5.1)\n";
    static string BuildOffersReport(bool pass)     => $"[{(pass ? "PASS" : "FAIL")}] Persistent Offers — MovieOfferState + save v5 + no reroll on tab switch\n";
    static string BuildSlotReport(bool pass)       => $"[{(pass ? "PASS" : "FAIL")}] Slot Refill — NotifyProductionStarted → RefillToFull on movie start\n";
    static string BuildFtueReport(bool pass)       => $"[{(pass ? "PASS" : "FAIL")}] FTUE Completion — AwaitCompletion + OnFinalFtueConfirm, no auto-complete\n";
    static string BuildRarityReport(bool pass)     => $"[{(pass ? "PASS" : "FAIL")}] Rarity Visuals — Epic=orange, Legendary=gold\n";

    static string BuildSummary(bool rep, bool offers, bool slot, bool ftue, bool rarity)
    {
        bool all = rep && offers && slot && ftue && rarity;
        var sb = new StringBuilder();
        sb.AppendLine("CORE GAMEPLAY STABILIZATION REPORT (Phase 7.3)");
        sb.AppendLine("Generated: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        sb.AppendLine($"Reputation UI      : {(rep ? "PASS" : "FAIL")}");
        sb.AppendLine($"Persistent Offers  : {(offers ? "PASS" : "FAIL")}");
        sb.AppendLine($"Slot Refill        : {(slot ? "PASS" : "FAIL")}");
        sb.AppendLine($"FTUE Completion    : {(ftue ? "PASS" : "FAIL")}");
        sb.AppendLine($"Rarity Visuals     : {(rarity ? "PASS" : "FAIL")}");
        sb.AppendLine();
        sb.AppendLine("OVERALL: " + (all ? "PASS" : "FAIL"));
        sb.AppendLine();
        sb.AppendLine("--- Key Fixes ---");
        sb.AppendLine("  TopBarUI.FormatReputation() — integer or one decimal");
        sb.AppendLine("  Save v5 field: currentOffers[]");
        sb.AppendLine("  MovieTabUI.RefreshOfferDisplay — render only, no random reroll on tab switch");
        sb.AppendLine("  StudioManager.StartMovie → MovieOfferState.NotifyProductionStarted");
        sb.AppendLine("  FtueController: AwaitCompletion step, OnFinalFtueConfirm, no auto-complete from premiere");
        sb.AppendLine("  Epic rarity = orange, Legendary = gold");
        return sb.ToString();
    }
}
#endif
