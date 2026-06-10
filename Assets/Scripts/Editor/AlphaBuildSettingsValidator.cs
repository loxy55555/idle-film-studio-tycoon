#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
static class AlphaBuildSettingsValidator
{
    static AlphaBuildSettingsValidator()
    {
        EditorApplication.delayCall += ValidateOnce;
    }

    static void ValidateOnce()
    {
        var scenes = EditorBuildSettings.scenes;
        if (scenes == null || scenes.Length == 0)
        {
            Debug.LogWarning("[Build] No scenes in Build Settings. Add Assets/Scenes/MainGame.unity.");
            return;
        }

        bool mainEnabled = false;
        foreach (var scene in scenes)
        {
            if (scene.path != null && scene.path.EndsWith("MainGame.unity") && scene.enabled)
                mainEnabled = true;
        }

        if (!mainEnabled)
            Debug.LogWarning("[Build] MainGame.unity is not the enabled build scene.");
    }
}
#endif
