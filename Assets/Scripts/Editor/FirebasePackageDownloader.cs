#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Net;
using UnityEditor;
using UnityEngine;

/// <summary>FASE 16.0B — Downloads Firebase .tgz and registers them in Packages/manifest.json.</summary>
[InitializeOnLoad]
static class FirebasePackageDownloader
{
    const string Version = "13.12.0";
    const string EdmVersion = "1.2.187";

    static readonly (string file, string url)[] RequiredPackages =
    {
        ("com.google.external-dependency-manager-" + EdmVersion + ".tgz",
            "https://dl.google.com/games/registry/unity/com.google.external-dependency-manager/com.google.external-dependency-manager-" + EdmVersion + ".tgz"),
        ("com.google.firebase.app-" + Version + ".tgz",
            "https://dl.google.com/games/registry/unity/com.google.firebase.app/com.google.firebase.app-" + Version + ".tgz"),
        ("com.google.firebase.analytics-" + Version + ".tgz",
            "https://dl.google.com/games/registry/unity/com.google.firebase.analytics/com.google.firebase.analytics-" + Version + ".tgz"),
        ("com.google.firebase.crashlytics-" + Version + ".tgz",
            "https://dl.google.com/games/registry/unity/com.google.firebase.crashlytics/com.google.firebase.crashlytics-" + Version + ".tgz"),
    };

    static readonly (string id, string tgz)[] ManifestEntries =
    {
        ("com.google.external-dependency-manager", "com.google.external-dependency-manager-" + EdmVersion + ".tgz"),
        ("com.google.firebase.app", "com.google.firebase.app-" + Version + ".tgz"),
        ("com.google.firebase.analytics", "com.google.firebase.analytics-" + Version + ".tgz"),
        ("com.google.firebase.crashlytics", "com.google.firebase.crashlytics-" + Version + ".tgz"),
    };

    static FirebasePackageDownloader()
    {
        EditorApplication.delayCall += () => EnsurePackages(force: false);
    }

    [MenuItem("Tools/Firebase/Download Packages (16.0B)")]
    static void MenuDownload() => EnsurePackages(force: true);

    static void EnsurePackages(bool force)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string packagesDir = Path.Combine(projectRoot, "GooglePackages");
        Directory.CreateDirectory(packagesDir);

        bool anyMissing = RequiredPackages.Any(p => !File.Exists(Path.Combine(packagesDir, p.file)));
        if (!anyMissing && !force)
        {
            TryPatchManifest(projectRoot);
            return;
        }

        try
        {
            using var client = new WebClient();
            foreach (var (file, url) in RequiredPackages)
            {
                string dest = Path.Combine(packagesDir, file);
                if (File.Exists(dest) && !force) continue;
                Debug.Log($"[Firebase] Downloading package={file} url={url}");
                try
                {
                    client.DownloadFile(url, dest);
                    Debug.Log($"[Firebase] Downloaded OK: {file}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Firebase] Download failed.\n  package={file}\n  url={url}\n  error={ex}");
                    throw;
                }
            }

            TryPatchManifest(projectRoot);
            Debug.Log("[Firebase] Packages ready in GooglePackages/. Resolving UPM…");
            UnityEditor.PackageManager.Client.Resolve();
        }
        catch (Exception ex)
        {
            Debug.LogError("[Firebase] Package download aborted — see GooglePackages/README.md.\n" + ex);
        }
    }

    static void TryPatchManifest(string projectRoot)
    {
        string manifestPath = Path.Combine(projectRoot, "Packages", "manifest.json");
        if (!File.Exists(manifestPath)) return;

        var lines = File.ReadAllLines(manifestPath).ToList();
        if (lines.Any(l => l.Contains("\"com.google.firebase.app\""))) return;

        int depsIdx = lines.FindIndex(l => l.Contains("\"dependencies\""));
        if (depsIdx < 0) return;

        int insertAt = depsIdx + 1;
        if (insertAt < lines.Count && lines[insertAt].Trim() == "{") insertAt++;

        var toInsert = ManifestEntries.Select(e => $"    \"{e.id}\": \"file:../GooglePackages/{e.tgz}\",");
        lines.InsertRange(insertAt, toInsert);
        File.WriteAllLines(manifestPath, lines);
        Debug.Log("[Firebase] manifest.json updated with Firebase package entries.");
    }
}
#endif
