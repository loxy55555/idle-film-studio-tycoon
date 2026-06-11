using UnityEngine;

/// <summary>
/// Legacy layout pass for unmigrated scenes. Baked scenes already contain final layout.
/// </summary>
[DefaultExecutionOrder(-158)]
public static class HudLayoutPassBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Run()
    {
        if (!Application.isPlaying) return;

        var switcher = FindContentSwitcher();
        if (switcher == null) return;

        if (HudBakedSceneRules.IsBaked(switcher))
            return;

        var shell = switcher.GetComponent<DefinitiveHudShell>();
        var mainNav = shell?.mainNavigation ?? switcher.GetComponent<StudioHubUI>();
        HudLayoutPass.Apply(switcher, mainNav);
    }

    static RectTransform FindContentSwitcher()
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name != "ContentSwitcher") continue;
            return t as RectTransform;
        }

        return null;
    }
}
