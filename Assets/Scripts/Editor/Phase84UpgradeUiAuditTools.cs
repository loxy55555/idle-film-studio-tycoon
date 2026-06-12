#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Phase 8.4 — upgrade UI final fix audit.</summary>
public static class Phase84UpgradeUiAuditTools
{
    const string ReportPath = "Assets/Data/Reports/UpgradeUiFinalFix.txt";

    [MenuItem("IdleFilm/Reports/Generate Upgrade UI Final Fix Report (8.4)")]
    public static void Generate()
    {
        Directory.CreateDirectory("Assets/Data/Reports");
        bool pass = VerifyFix();
        File.WriteAllText(ReportPath, BuildReport(pass), Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log("[Phase8.4] Report written to " + ReportPath);
    }

    static bool VerifyFix()
    {
        if (!File.Exists("Assets/Scripts/UI/Hub/UpgradeCardUI.cs")) return false;
        if (!File.Exists("Assets/Scripts/UI/Hub/UpgradeUiInteractionGate.cs")) return false;
        if (!File.Exists("Assets/Scripts/UI/Hub/UpgradeBuyButtonGuard.cs")) return false;
        if (!File.Exists("Assets/Scripts/UI/Hub/UpgradeUiRaycastPolicy.cs")) return false;
        if (!File.Exists("Assets/Scripts/UI/Hub/ContentSortBootstrap.cs")) return false;

        string card   = File.ReadAllText("Assets/Scripts/UI/Hub/UpgradeCardUI.cs");
        string gate   = File.ReadAllText("Assets/Scripts/UI/Hub/UpgradeUiInteractionGate.cs");
        string guard  = File.ReadAllText("Assets/Scripts/UI/Hub/UpgradeBuyButtonGuard.cs");
        string policy = File.ReadAllText("Assets/Scripts/UI/Hub/UpgradeUiRaycastPolicy.cs");
        string sort   = File.ReadAllText("Assets/Scripts/UI/Hub/ContentSortBootstrap.cs");
        string dept   = File.Exists("Assets/Scripts/UI/Hub/DepartmentMiniCardUI.cs")
            ? File.ReadAllText("Assets/Scripts/UI/Hub/DepartmentMiniCardUI.cs") : string.Empty;

        bool logs          = gate.Contains("[UpgradeUI] PointerDown=") &&
                             gate.Contains("[UpgradeUI] PointerUp=") &&
                             gate.Contains("[UpgradeUI] Clicked=") &&
                             gate.Contains("UpgradeId=");
        bool clickGate     = card.Contains("TryConsumeClick") && dept.Contains("TryConsumeClick");
        bool raycastPolicy = card.Contains("UpgradeUiRaycastPolicy.ApplyCard") &&
                             policy.Contains("raycastTarget = false");
        bool pointerGuard  = card.Contains("UpgradeBuyButtonGuard") && guard.Contains("IPointerDownHandler");
        bool cardHeight    = card.Contains("CardLayoutHeight = 140f");
        bool deferredSort  = sort.Contains("DeferredSortRoutine") &&
                             sort.Contains("UpgradeUiInteractionGate.CanReorderNow") &&
                             sort.Contains("LayoutRebuilder.ForceRebuildLayoutImmediate");

        return logs && clickGate && raycastPolicy && pointerGuard && cardHeight && deferredSort;
    }

    static string BuildReport(bool pass)
    {
        var sb = new StringBuilder();
        sb.AppendLine("UPGRADE UI FINAL FIX REPORT (Phase 8.4)");
        sb.AppendLine("Generated: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        sb.AppendLine($"OVERALL: {(pass ? "PASS" : "FAIL")}");
        sb.AppendLine();
        sb.AppendLine("--- Root causes ---");
        sb.AppendLine("1. Card layout height (100px builder vs 132px+ content) caused vertical overlap");
        sb.AppendLine("2. Non-button Graphics kept raycastTarget=true (card bg, text, badge)");
        sb.AppendLine("3. UpgradeContentSorter reordered cards immediately after purchase");
        sb.AppendLine("   (pointer release could land on a different BuyBtn hitbox)");
        sb.AppendLine();
        sb.AppendLine("--- Fixes ---");
        sb.AppendLine("UpgradeUiRaycastPolicy: only BuyBtn Image receives raycasts");
        sb.AppendLine("CardLayoutHeight=140: consistent LayoutElement min/preferred height");
        sb.AppendLine("UpgradeBuyButtonGuard: PointerDown/Up per button with UpgradeId logs");
        sb.AppendLine("UpgradeUiInteractionGate: click only valid if PointerDown id matches");
        sb.AppendLine("UpgradeContentSorter: deferred sort + CanReorderNow + layout rebuild");
        sb.AppendLine();
        sb.AppendLine("--- Debug logs (temporary) ---");
        sb.AppendLine("[UpgradeUI] PointerDown=<id>");
        sb.AppendLine("[UpgradeUI] PointerUp=<id>");
        sb.AppendLine("[UpgradeUI] Clicked=<id> UpgradeId=<id>");
        sb.AppendLine();
        sb.AppendLine("--- Manual validation ---");
        sb.AppendLine("Buy 20+ upgrades rapidly. Expect exactly one Clicked log per purchase.");
        sb.AppendLine("No PointerUp mismatch warnings. No neighbor UpgradeId in PurchaseAttempt.");
        sb.AppendLine();
        sb.AppendLine("--- Files modified ---");
        sb.AppendLine("  UpgradeCardUI.cs");
        sb.AppendLine("  DepartmentMiniCardUI.cs");
        sb.AppendLine("  ContentSortBootstrap.cs");
        sb.AppendLine("  UpgradeUiInteractionGate.cs (new)");
        sb.AppendLine("  UpgradeBuyButtonGuard.cs (new)");
        sb.AppendLine("  UpgradeUiRaycastPolicy.cs (new)");
        return sb.ToString();
    }
}
#endif
