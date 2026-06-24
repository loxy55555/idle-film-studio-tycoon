#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>FASE 17C — Android AAB build for Google Play Internal Testing (Billing).</summary>
public static class AndroidAabBuildTools
{
    const string OutputDir = "Builds/Android";
    const string AabFileName = "fase17c2-idlefilm-production.aab";
    const string ProductionKeystorePath = "Builds/Android/FilmProducerTycoonUpload.keystore";
    const string ProductionKeyAlias = "filmproducertycoon_upload";

    [MenuItem("Build/Android AAB (FASE 17C Billing)")]
    public static void BuildAabMenu() => BuildAab();

    static void ApplyProductionSigning()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string keystorePath = Path.Combine(projectRoot, ProductionKeystorePath).Replace('\\', '/');
        if (!File.Exists(keystorePath))
            throw new InvalidOperationException($"Production keystore not found: {keystorePath}");

        string storePass = Environment.GetEnvironmentVariable("FPT_KEYSTORE_PASS");
        string keyPass = Environment.GetEnvironmentVariable("FPT_KEY_PASS") ?? storePass;
        if (string.IsNullOrEmpty(storePass))
            storePass = EditorPrefs.GetString("FPT_KEYSTORE_PASS", string.Empty);
        if (string.IsNullOrEmpty(keyPass))
            keyPass = EditorPrefs.GetString("FPT_KEY_PASS", storePass);
        if (string.IsNullOrEmpty(storePass))
            throw new InvalidOperationException("Set FPT_KEYSTORE_PASS/FPT_KEY_PASS environment variables before building.");

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystorePath;
        PlayerSettings.Android.keyaliasName = ProductionKeyAlias;
        PlayerSettings.Android.keystorePass = storePass;
        PlayerSettings.Android.keyaliasPass = keyPass;
    }

    public static string BuildAab()
    {
        ApplyProductionSigning();
        EditorUserBuildSettings.buildAppBundle = true;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.vosnosgames.filmproducertycoon");
        PlayerSettings.Android.bundleVersionCode = Mathf.Max(PlayerSettings.Android.bundleVersionCode, 2);

        Directory.CreateDirectory(OutputDir);
        string outputPath = Path.Combine(OutputDir, AabFileName).Replace('\\', '/');

        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new InvalidOperationException("No enabled scenes in Build Settings.");

        var options = BuildOptions.CompressWithLz4HC;
        BuildReport report = BuildPipeline.BuildPlayer(scenes, outputPath, BuildTarget.Android, options);

        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException($"AAB build failed: {report.summary.result} — errors={report.summary.totalErrors}");

        long bytes = new FileInfo(outputPath).Length;
        Debug.Log($"[FASE17C] AAB succeeded: {outputPath} ({bytes / (1024f * 1024f):0.##} MB)");
        return outputPath;
    }
}
#endif
