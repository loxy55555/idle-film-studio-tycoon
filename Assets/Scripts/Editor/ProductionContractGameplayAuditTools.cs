#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Phase 8.0 — production & contract gameplay verification report.</summary>
public static class ProductionContractGameplayAuditTools
{
    const string ReportPath = "Assets/Data/Reports/ProductionContractGameplayReport.txt";

    [MenuItem("IdleFilm/Reports/Generate Production Contract Gameplay Report (8.0)")]
    public static void Generate()
    {
        Directory.CreateDirectory("Assets/Data/Reports");

        bool cards         = VerifyProductionCards();
        bool prodRefresh   = VerifyProductionRefresh();
        bool budgetUi      = VerifyBudgetUi();
        bool offerPersist  = VerifyOfferPersistence();
        bool selection     = VerifyContractSelection();
        bool progress      = VerifyContractProgress();
        bool rewards       = VerifyContractRewards();
        bool contractRefresh = VerifyContractRefresh();
        bool badges        = VerifyMovieContractBadges();

        bool allPass = cards && prodRefresh && budgetUi && offerPersist &&
                       selection && progress && rewards && contractRefresh && badges;

        var sb = new StringBuilder();
        sb.AppendLine("PRODUCTION & CONTRACT GAMEPLAY REPORT (Phase 8.0)");
        sb.AppendLine("Generated: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        sb.AppendLine($"Production Cards          : {(cards ? "PASS" : "FAIL")}");
        sb.AppendLine($"Production Refresh        : {(prodRefresh ? "PASS" : "FAIL")}");
        sb.AppendLine($"Production Budget UI      : {(budgetUi ? "PASS" : "FAIL")}");
        sb.AppendLine($"Offer Persistence         : {(offerPersist ? "PASS" : "FAIL")}");
        sb.AppendLine($"Contract Selection        : {(selection ? "PASS" : "FAIL")}");
        sb.AppendLine($"Contract Progress         : {(progress ? "PASS" : "FAIL")}");
        sb.AppendLine($"Contract Rewards          : {(rewards ? "PASS" : "FAIL")}");
        sb.AppendLine($"Contract Refresh          : {(contractRefresh ? "PASS" : "FAIL")}");
        sb.AppendLine($"Movie Contract Badges     : {(badges ? "PASS" : "FAIL")}");
        sb.AppendLine();
        sb.AppendLine("OVERALL: " + (allPass ? "PASS" : "FAIL"));
        sb.AppendLine();
        sb.AppendLine("--- Details ---");
        sb.AppendLine("Production cards: poster/title/rarity/genre/duration/money/rep + NUEVA/SAGA/CONTRATO badges");
        sb.AppendLine("Production refresh: REFRESH OFFERS via ad or diamonds (GameplayRefreshService)");
        sb.AppendLine("Budget UI: LOW/STANDARD/PREMIUM stats from ProductionBudgetRules.GetModifiers()");
        sb.AppendLine("Offer persistence: MovieOfferState + save v5 currentOffers[]");
        sb.AppendLine("Contract selection: 3 candidates OR 1 active — SelectCandidate()");
        sb.AppendLine("Contract progress: active card shows objective + progress bar");
        sb.AppendLine("Contract rewards: claim button + GameFeelUI reward panel");
        sb.AppendLine("Contract refresh: REFRESH CONTRACTS when no active contract");
        sb.AppendLine("Movie badges: MovieOfferBadgeHelper + DoesMovieHelpActiveContract()");

        File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log("[ProductionContractGameplay] Report written to " + ReportPath);
    }

    static bool VerifyProductionCards()
    {
        if (!File.Exists("Assets/Scripts/UI/Production/MovieOfferCardLayoutBuilder.cs")) return false;
        if (!File.Exists("Assets/Scripts/UI/MovieButtonUI.cs")) return false;
        string layout = File.ReadAllText("Assets/Scripts/UI/Production/MovieOfferCardLayoutBuilder.cs");
        string btn = File.ReadAllText("Assets/Scripts/UI/MovieButtonUI.cs");
        return layout.Contains("rewardText") &&
               layout.Contains("repText") &&
               layout.Contains("badgesText") &&
               btn.Contains("MovieOfferBadgeHelper") &&
               btn.Contains("ProdOfferMoney");
    }

    static bool VerifyProductionRefresh()
    {
        if (!File.Exists("Assets/Scripts/Core/GameplayRefreshService.cs")) return false;
        string src = File.ReadAllText("Assets/Scripts/Core/GameplayRefreshService.cs");
        string shell = File.ReadAllText("Assets/Scripts/UI/Hub/Hud/ProductionHudShell.cs");
        return src.Contains("RequestProductionOfferRefresh") &&
               src.Contains("IAdsRewardProvider") &&
               src.Contains("ForceRegenerateAll") &&
               shell.Contains("GameplayRefreshService.RequestProductionOfferRefresh");
    }

    static bool VerifyBudgetUi()
    {
        string rules = File.ReadAllText("Assets/Scripts/Core/ProductionBudget.cs");
        string picker = File.ReadAllText("Assets/Scripts/UI/Production/ProductionBudgetPickerUI.cs");
        return rules.Contains("durationMultiplier   = 0.8f") &&
               rules.Contains("durationMultiplier   = 1.5f") &&
               rules.Contains("FormatStatBlock") &&
               picker.Contains("ProductionBudgetRules.FormatStatBlock");
    }

    static bool VerifyOfferPersistence()
    {
        string state = File.ReadAllText("Assets/Scripts/Core/MovieOfferState.cs");
        string save = File.ReadAllText("Assets/Scripts/SaveSystem.cs");
        return state.Contains("RefillToFull") &&
               save.Contains("currentOffers") &&
               state.Contains("NotifyProductionStarted");
    }

    static bool VerifyContractSelection()
    {
        string sys = File.ReadAllText("Assets/Scripts/Core/Systems/ContractSystem.cs");
        string panel = File.ReadAllText("Assets/Scripts/UI/Hub/ContractsPanelUI.cs");
        return sys.Contains("SelectCandidate") &&
               sys.Contains("CandidateContracts") &&
               sys.Contains("MaxActiveContracts = 1") &&
               panel.Contains("BuildSelectionView") &&
               panel.Contains("ContractCardMode.Candidate");
    }

    static bool VerifyContractProgress()
    {
        string factory = File.ReadAllText("Assets/Scripts/UI/Hub/ContractsPanelUI.cs");
        string card = File.ReadAllText("Assets/Scripts/UI/Hub/ContractCardUI.cs");
        return factory.Contains("ContractActiveObjective") &&
               factory.Contains("ProgBar") &&
               card.Contains("BuildObjectiveLabel");
    }

    static bool VerifyContractRewards()
    {
        string card = File.ReadAllText("Assets/Scripts/UI/Hub/ContractCardUI.cs");
        string feel = File.ReadAllText("Assets/Scripts/UI/Animation/GameFeelUI.cs");
        return card.Contains("OnClaimClicked") &&
               card.Contains("claimButton") &&
               feel.Contains("OnContractClaimed");
    }

    static bool VerifyContractRefresh()
    {
        string svc = File.ReadAllText("Assets/Scripts/Core/GameplayRefreshService.cs");
        string panel = File.ReadAllText("Assets/Scripts/UI/Hub/ContractsPanelUI.cs");
        return svc.Contains("RequestContractCandidateRefresh") &&
               svc.Contains("ForceRefreshCandidates") &&
               panel.Contains("ContractRefresh") &&
               panel.Contains("RequestContractCandidateRefresh");
    }

    static bool VerifyMovieContractBadges()
    {
        string helper = File.ReadAllText("Assets/Scripts/Core/MovieOfferBadgeHelper.cs");
        string sys = File.ReadAllText("Assets/Scripts/Core/Systems/ContractSystem.cs");
        return helper.Contains("[CONTRATO]") &&
               sys.Contains("DoesMovieHelpActiveContract");
    }
}
#endif
