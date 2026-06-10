using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime cleanup for dead UI space and placeholder labels left by earlier layouts.
/// </summary>
public static class UiSafeCleanup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Run()
    {
        if (!Application.isPlaying) return;
        CollapseNamedLayout("TopBarSpacer");
        HidePlaceholderTexts();
    }

    static void CollapseNamedLayout(string objectName)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name != objectName) continue;
            var le = t.GetComponent<LayoutElement>() ?? t.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 0f;
            le.minHeight = 0f;
            le.flexibleHeight = 0f;
            t.gameObject.SetActive(false);
        }
    }

    static void HidePlaceholderTexts()
    {
        foreach (var tmp in Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (tmp.name == "NowLabel" && tmp.text == "PRODUCCIÓN ACTUAL")
                tmp.gameObject.SetActive(false);
        }
    }
}
