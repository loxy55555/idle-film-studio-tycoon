#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Keeps Resources/Audio/AudioClipIndex.asset in sync with Assets/Audio/music and sfx.</summary>
[InitializeOnLoad]
static class AudioClipIndexBuilder
{
    const string IndexPath = "Assets/Resources/Audio/AudioClipIndex.asset";
    static readonly string[] ScanFolders = { "Assets/Audio/music", "Assets/Audio/sfx" };

    static AudioClipIndexBuilder() => Refresh();

    [MenuItem("IdleFilm/Audio/Refresh Clip Index")]
    public static void Refresh()
    {
        var clips = new List<AudioClip>();
        var seen = new HashSet<string>();

        foreach (var folder in ScanFolders)
        {
            if (!AssetDatabase.IsValidFolder(folder)) continue;

            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null || !seen.Add(clip.name)) continue;
                clips.Add(clip);
            }
        }

        clips.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

        var index = AssetDatabase.LoadAssetAtPath<AudioClipIndex>(IndexPath);
        if (index == null)
        {
            EnsureFolder("Assets/Resources/Audio");
            index = ScriptableObject.CreateInstance<AudioClipIndex>();
            AssetDatabase.CreateAsset(index, IndexPath);
        }

        index.clips = clips.ToArray();
        EditorUtility.SetDirty(index);
        AssetDatabase.SaveAssets();
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        var leaf = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
#endif
