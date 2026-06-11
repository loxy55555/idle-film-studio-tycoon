using UnityEngine;
using UnityEngine.UI;

/// <summary>Reserved central area for future studio visuals (background, crew, animations).</summary>
public class StudioVisualStage : MonoBehaviour
{
    public static StudioVisualStage Instance { get; private set; }

    public RectTransform StageRoot { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        StageRoot = transform as RectTransform;
        ApplyLayout();
        HideLegacyPlaceholderText();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void ApplyLayout()
    {
        var le = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        le.flexibleHeight   = HudLayoutConstants.StudioVisualShare;
        le.flexibleWidth    = 0f;
        le.minHeight        = HudLayoutConstants.StudioVisualMinHeight;
        le.preferredHeight  = 0f;

        var image = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        image.color = new Color(0.06f, 0.07f, 0.12f, 1f);
        image.raycastTarget = false;
    }

    void HideLegacyPlaceholderText()
    {
        var placeholder = transform.Find("ScenePlaceholder");
        if (placeholder != null)
            placeholder.gameObject.SetActive(false);

        var border = transform.Find("SceneBorder");
        if (border != null)
            border.gameObject.SetActive(false);
    }
}
