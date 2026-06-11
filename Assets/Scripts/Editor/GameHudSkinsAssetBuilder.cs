#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GameHudSkinsAssetBuilder
{
    const string ResourcesDir = "Assets/Resources";
    const string AssetPath = ResourcesDir + "/GameHudSkins.asset";

    [MenuItem("IdleFilm/Create Game HUD Skins Asset")]
    public static void CreateOrRefreshAsset()
    {
        if (!Directory.Exists(ResourcesDir))
            Directory.CreateDirectory(ResourcesDir);

        var bundle = AssetDatabase.LoadAssetAtPath<GameHudSkins>(AssetPath);
        if (bundle == null)
        {
            bundle = ScriptableObject.CreateInstance<GameHudSkins>();
            AssetDatabase.CreateAsset(bundle, AssetPath);
        }

        bundle.panelSkin = GetOrCreateSubAsset<UIPanelSkin>(bundle, "UIPanelSkin");
        bundle.cardSkin = GetOrCreateSubAsset<UICardSkin>(bundle, "UICardSkin");
        bundle.buttonSkin = GetOrCreateSubAsset<UIButtonSkin>(bundle, "UIButtonSkin");
        bundle.tabSkin = GetOrCreateSubAsset<UITabSkin>(bundle, "UITabSkin");

        EditorUtility.SetDirty(bundle);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[HudSkin] GameHudSkins asset ready at {AssetPath}. Assign sprites on sub-assets when art is available.");
    }

    static T GetOrCreateSubAsset<T>(GameHudSkins bundle, string name) where T : ScriptableObject
    {
        var path = AssetPath;
        var existing = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var obj in existing)
        {
            if (obj is T typed && obj.name == name)
                return typed;
        }

        var created = ScriptableObject.CreateInstance<T>();
        created.name = name;
        AssetDatabase.AddObjectToAsset(created, bundle);
        return created;
    }
}
#endif
