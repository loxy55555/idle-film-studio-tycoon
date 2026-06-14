#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tools for CityBackgroundRegistry.
/// Menu: IdleFilm / City Backgrounds / ...
/// </summary>
public static class CityBackgroundRegistryTools
{
    [MenuItem("IdleFilm/City Backgrounds/Create Registry Asset")]
    public static void CreateRegistry()
    {
        if (AssetDatabase.LoadAssetAtPath<CityBackgroundRegistry>(CityBackgroundRegistry.AssetPath) != null)
        {
            Debug.Log("[CityBackgrounds] Registry already exists at " + CityBackgroundRegistry.AssetPath);
            OpenRegistry();
            return;
        }

        var asset = ScriptableObject.CreateInstance<CityBackgroundRegistry>();
        AssetDatabase.CreateAsset(asset, CityBackgroundRegistry.AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CityBackgrounds] Created " + CityBackgroundRegistry.AssetPath);
        OpenRegistry();
    }

    [MenuItem("IdleFilm/City Backgrounds/Open Registry")]
    public static void OpenRegistry()
    {
        var asset = AssetDatabase.LoadAssetAtPath<CityBackgroundRegistry>(CityBackgroundRegistry.AssetPath);
        if (asset == null)
        {
            Debug.LogWarning("[CityBackgrounds] Registry not found. Run 'Create Registry Asset' first.");
            return;
        }

        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }

    [MenuItem("IdleFilm/City Backgrounds/Open Registry", validate = true)]
    static bool ValidateOpenRegistry() =>
        AssetDatabase.LoadAssetAtPath<CityBackgroundRegistry>(CityBackgroundRegistry.AssetPath) != null;

    [MenuItem("IdleFilm/City Backgrounds/Force Refresh Active Theme")]
    public static void ForceRefreshTheme()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[CityBackgrounds] Theme refresh only works in Play Mode.");
            return;
        }

        var stage = Object.FindAnyObjectByType<StudioVisualStage>(FindObjectsInactive.Include);
        if (stage == null)
        {
            Debug.LogWarning("[CityBackgrounds] StudioVisualStage not found in scene.");
            return;
        }

        var controller = stage.GetComponent<StudioVisualThemeController>();
        if (controller == null)
        {
            Debug.LogWarning("[CityBackgrounds] StudioVisualThemeController not found.");
            return;
        }

        controller.ApplyThemeForCurrentCity(force: true);
        Debug.Log("[CityBackgrounds] Theme reapplied for current city.");
    }
}
#endif
