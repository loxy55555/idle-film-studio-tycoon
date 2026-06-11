#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Bakes the definitive HUD structure into the open scene (Phase 7A.3).</summary>
public static class HudDefinitiveSceneBaker
{
    [MenuItem("IdleFilm/Bake Definitive HUD (Current Scene)")]
    public static void BakeCurrentScene()
    {
        var switcher = FindContentSwitcher();
        if (switcher == null)
        {
            Debug.LogError("[HudBake] ContentSwitcher not found in open scene.");
            return;
        }

        Undo.SetCurrentGroupName("Bake Definitive HUD");
        var mainHub = switcher.GetComponent<StudioHubUI>();
        if (mainHub == null)
            mainHub = switcher.gameObject.AddComponent<StudioHubUI>();

        WireBottomNav(mainHub);
        Bake(switcher, mainHub);
        EditorSceneManager.MarkSceneDirty(switcher.gameObject.scene);
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        Debug.Log("[HudBake] Definitive HUD baked into current scene.");
    }

    public static void Bake(RectTransform switcher, StudioHubUI mainHub)
    {
        if (switcher == null) return;

        WireBottomNav(mainHub);

        if (!HudDefinitiveStructureBuilder.Apply(switcher, showProductionTab: false))
        {
            Debug.LogError("[HudBake] Structure build failed — missing required panels.");
            return;
        }

        var shell = switcher.GetComponent<DefinitiveHudShell>();
        if (shell != null && mainHub != null)
            shell.mainNavigation = mainHub;

        EditorUtility.SetDirty(switcher.gameObject);
    }

    static void WireBottomNav(StudioHubUI mainHub)
    {
        if (mainHub == null) return;

        var nav = GameObject.Find("BottomNav")?.transform;
        if (nav == null || nav.childCount < 5) return;

        var buttons = new Button[5];
        for (int i = 0; i < 5; i++)
            buttons[i] = nav.GetChild(i).GetComponent<Button>();
        mainHub.tabButtons = buttons;
    }

    static RectTransform FindContentSwitcher()
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == "ContentSwitcher")
                return t as RectTransform;
        }

        return null;
    }
}
#endif
