#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Bakes StudioVisualStage layer hierarchy into the open scene (Phase 7B.1+).</summary>
public static class StudioVisualStageLayerBaker
{
    [MenuItem("IdleFilm/Bake Studio Visual Stage Layers")]
    public static void BakeCurrentScene()
    {
        var stage = Object.FindAnyObjectByType<StudioVisualStage>(FindObjectsInactive.Include);
        if (stage == null)
        {
            Debug.LogError("[StudioVisualStage] StudioVisualStage not found in open scene.");
            return;
        }

        Undo.SetCurrentGroupName("Bake Studio Visual Stage Layers");
        Undo.RegisterFullObjectHierarchyUndo(stage.gameObject, "Bake Studio Visual Stage Layers");

        stage.ApplyLayout();
        StudioVisualStageLayers.EnsureHierarchy(stage);
        StudioVisualThemeController.ApplyTheme(stage, CityTier.City1);

        EditorUtility.SetDirty(stage);
        EditorSceneManager.MarkSceneDirty(stage.gameObject.scene);
        EditorSceneManager.SaveScene(stage.gameObject.scene);
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        Debug.Log("[StudioVisualStage] Layers + City1 garage theme baked into current scene.");
    }

    [MenuItem("IdleFilm/Bake Garage Studio Visual")]
    public static void BakeGarageVisualCurrentScene() => BakeCurrentScene();

    [MenuItem("IdleFilm/Bake City Visual Theme (City 1)")]
    public static void BakeCity1Theme() => BakeThemeInScene(CityTier.City1);

    [MenuItem("IdleFilm/Bake City Visual Theme (Preview All)")]
    public static void BakeAllThemesPreview()
    {
        Debug.Log("[StudioVisualStage] Registered themes: " +
            string.Join(", ", System.Enum.GetNames(typeof(CityTier))));
    }

    static void BakeThemeInScene(CityTier tier)
    {
        var stage = Object.FindAnyObjectByType<StudioVisualStage>(FindObjectsInactive.Include);
        if (stage == null)
        {
            Debug.LogError("[StudioVisualStage] StudioVisualStage not found in open scene.");
            return;
        }

        Undo.SetCurrentGroupName("Bake City Visual Theme");
        Undo.RegisterFullObjectHierarchyUndo(stage.gameObject, "Bake City Visual Theme");

        stage.ApplyLayout();
        StudioVisualStageLayers.EnsureHierarchy(stage);
        StudioVisualThemeController.ApplyTheme(stage, tier);

        EditorUtility.SetDirty(stage);
        EditorSceneManager.MarkSceneDirty(stage.gameObject.scene);
        EditorSceneManager.SaveScene(stage.gameObject.scene);
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        Debug.Log($"[StudioVisualStage] Theme '{CityVisualThemeRegistry.Get(tier).ThemeId}' baked into current scene.");
    }

    public static void Bake(StudioVisualStage stage)
    {
        if (stage == null) return;

        stage.ApplyLayout();
        StudioVisualStageLayers.EnsureHierarchy(stage);
        StudioVisualThemeController.ApplyTheme(stage, CityTier.City1);
        EditorUtility.SetDirty(stage);
    }
}
#endif
