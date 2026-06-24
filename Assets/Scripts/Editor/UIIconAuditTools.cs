#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>FASE 16.5F — exhaustive icon/sprite audit (editor truth, no assumptions).</summary>
public static class UIIconAuditTools
{
    const string ReportPath = "Assets/Data/Reports/FASE165F_IconAudit.txt";

    [MenuItem("IdleFilm/Reports/Audit UI Icons (FASE 16.5F)")]
    public static void RunAuditMenu()
    {
        var issues = RunAudit();
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "Assets/Data/Reports");
        File.WriteAllText(ReportPath, BuildReport(issues), Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log($"[FASE16.5F] Icon audit complete — {issues.Count} issue(s). Report: {ReportPath}");
    }

    public static List<IconIssue> RunAudit()
    {
        var issues = new List<IconIssue>();
        var registry = AssetDatabase.LoadAssetAtPath<UIIconRegistry>("Assets/Resources/UIIconRegistry.asset");
        if (registry == null)
        {
            issues.Add(new IconIssue("registry_missing", "UIIconRegistry", "Resources/UIIconRegistry.asset not found"));
            return issues;
        }

        foreach (var field in typeof(UIIconRegistry).GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            if (field.FieldType != typeof(Sprite)) continue;
            var sprite = field.GetValue(registry) as Sprite;
            if (sprite == null)
                issues.Add(new IconIssue("registry_null", field.Name, "UIIconRegistry field has no sprite assigned"));
        }

        foreach (DepartmentType dept in Enum.GetValues(typeof(DepartmentType)))
        {
            if (UIIconCatalog.GetDepartment(dept) == null)
                issues.Add(new IconIssue("department_runtime", dept.ToString(),
                    $"UIIconCatalog.GetDepartment({dept}) returns null"));
        }

        foreach (MovieGenre genre in Enum.GetValues(typeof(MovieGenre)))
        {
            if (UIIconCatalog.GetGenre(genre) == null)
                issues.Add(new IconIssue("genre_runtime", genre.ToString(),
                    $"UIIconCatalog.GetGenre({genre}) returns null"));
        }

        foreach (MainHudTab tab in Enum.GetValues(typeof(MainHudTab)))
        {
            if (UIIconCatalog.GetNavigation(tab) == null)
                issues.Add(new IconIssue("navigation_runtime", tab.ToString(),
                    $"UIIconCatalog.GetNavigation({tab}) returns null"));
        }

        AuditResourceGetter(issues, "GetResourceMoney", UIIconCatalog.GetResourceMoney);
        AuditResourceGetter(issues, "GetResourceReputation", UIIconCatalog.GetResourceReputation);
        AuditResourceGetter(issues, "GetResourceGoldStar", UIIconCatalog.GetResourceGoldStar);
        AuditResourceGetter(issues, "GetResourceDiamonds", UIIconCatalog.GetResourceDiamonds);
        AuditResourceGetter(issues, "GetResourceExperience", UIIconCatalog.GetResourceExperience);
        AuditResourceGetter(issues, "GetResourceTicket", UIIconCatalog.GetResourceTicket);
        AuditResourceGetter(issues, "GetResourceFilmReel", UIIconCatalog.GetResourceFilmReel);
        AuditResourceGetter(issues, "GetResourceDecoCamera", UIIconCatalog.GetResourceDecoCamera);
        AuditResourceGetter(issues, "GetResourceSettings", UIIconCatalog.GetResourceSettings);
        AuditResourceGetter(issues, "GetAwardStar", UIIconCatalog.GetAwardStar);
        AuditResourceGetter(issues, "GetAwardStarLocked", UIIconCatalog.GetAwardStarLocked);
        AuditResourceGetter(issues, "GetUtilityBoost", UIIconCatalog.GetUtilityBoost);
        AuditResourceGetter(issues, "GetUtilitySpeedProduction", UIIconCatalog.GetUtilitySpeedProduction);
        AuditResourceGetter(issues, "GetMarketing", UIIconCatalog.GetMarketing);
        AuditResourceGetter(issues, "GetMissions", UIIconCatalog.GetMissions);

        foreach (var guid in AssetDatabase.FindAssets("t:UpgradeConfig", new[] { "Assets/Data/Upgrades" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var cfg = AssetDatabase.LoadAssetAtPath<UpgradeConfig>(path);
            if (cfg == null || string.IsNullOrEmpty(cfg.id)) continue;
            if (UIIconCatalog.GetUpgradeIcon(cfg.id) == null)
                issues.Add(new IconIssue("upgrade_runtime", cfg.id,
                    $"UIIconCatalog.GetUpgradeIcon('{cfg.id}') returns null — emoji placeholder at runtime"));
        }

        ScanPrefabIconImages(issues);
        return issues;
    }

    static void AuditResourceGetter(List<IconIssue> issues, string name, Func<Sprite> getter)
    {
        if (getter() == null)
            issues.Add(new IconIssue("resource_runtime", name, $"{name}() returns null"));
    }

    static void ScanPrefabIconImages(List<IconIssue> issues)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null) continue;

            foreach (var img in root.GetComponentsInChildren<Image>(true))
            {
                if (img.sprite != null) continue;
                var n = img.gameObject.name;
                if (!IsIconObjectName(n)) continue;

                // Container badges (background Image) are intentionally sprite-less.
                if (n is "CategoryBadge" or "Badge") continue;

                // Child IconImage is filled by DepartmentThemeVisual at runtime.
                if (n == "IconImage" && img.GetComponentInParent<DepartmentThemeVisual>(true) != null)
                    continue;
                if (root.GetComponentInChildren<DepartmentThemeVisual>(true) != null &&
                    (n == "IconImage" || n == "BadgeIcon"))
                    continue;

                issues.Add(new IconIssue("prefab_null_sprite", $"{path} :: {GetTransformPath(img.transform)}",
                    "Image on icon-named object has null sprite and no known runtime filler"));
            }
        }
    }

    static bool IsIconObjectName(string name) =>
        name.Contains("Icon", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Badge", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Sprite", StringComparison.OrdinalIgnoreCase);

    static string GetTransformPath(Transform t)
    {
        var parts = new List<string>();
        while (t != null)
        {
            parts.Add(t.name);
            t = t.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }

    static string BuildReport(List<IconIssue> issues)
    {
        var sb = new StringBuilder();
        sb.AppendLine("FASE 16.5F — UI ICON AUDIT");
        sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        sb.AppendLine($"TOTAL ISSUES: {issues.Count}");
        sb.AppendLine();
        foreach (var issue in issues)
        {
            sb.AppendLine($"[{issue.category}] {issue.id}");
            sb.AppendLine($"  {issue.detail}");
        }
        return sb.ToString();
    }

    public readonly struct IconIssue
    {
        public readonly string category;
        public readonly string id;
        public readonly string detail;

        public IconIssue(string category, string id, string detail)
        {
            this.category = category;
            this.id = id;
            this.detail = detail;
        }
    }
}
#endif
