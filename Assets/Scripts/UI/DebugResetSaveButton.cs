#if UNITY_EDITOR
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Editor-only debug control: deletes save_v2.json and reloads the active scene.
/// Not included in player builds.
/// </summary>
public class DebugResetSaveButton : MonoBehaviour
{
    static readonly Color BTN_COLOR = new Color(0.75f, 0.22f, 0.22f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (!Application.isPlaying) return;
        if (FindAnyObjectByType<DebugResetSaveButton>() != null) return;

        var hub = FindAnyObjectByType<GameHub>();
        if (hub == null) return;
        hub.gameObject.AddComponent<DebugResetSaveButton>();
    }

    private Button _button;

    private void Start()
    {
        if (!Application.isPlaying) return;
        BuildButton();
    }

    void BuildButton()
    {
        var canvas = GetComponent<Canvas>() ?? FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("DebugResetSave", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(canvas.transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(12f, -12f);
        rt.sizeDelta = new Vector2(120f, 32f);

        go.GetComponent<Image>().color = BTN_COLOR;

        var lblGo = new GameObject("Label", typeof(RectTransform));
        lblGo.transform.SetParent(go.transform, false);
        var lbl = lblGo.AddComponent<TextMeshProUGUI>();
        lbl.text = "Reset Save";
        lbl.fontSize = 14;
        lbl.fontStyle = FontStyles.Bold;
        lbl.color = Color.white;
        lbl.alignment = TextAlignmentOptions.Center;
        var lblRT = lblGo.GetComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero;
        lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;

        _button = go.GetComponent<Button>();
        _button.onClick.AddListener(OnResetClicked);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnResetClicked);
    }

    void OnResetClicked()
    {
        var save = GameHub.Instance?.save ?? FindAnyObjectByType<SaveSystem>();
        save?.DeleteSave();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
#endif
