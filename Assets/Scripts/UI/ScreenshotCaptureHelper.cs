using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Auto-screenshot helper for FASE 13.4B delivery.
/// Runs synchronously in Start(): navigates to each panel, forces layout, captures.
/// Uses camera-based rendering so GameView focus is not required.
/// </summary>
public class ScreenshotCaptureHelper : MonoBehaviour
{
    static readonly string ScreenshotDir = Application.dataPath + "/Screenshots/";

    void Awake()
    {
        Time.timeScale = 1f;
        Debug.Log("[SSH] Awake");
        File.WriteAllText(Application.dataPath + "/SSH_awake.txt",
            "Awake " + System.DateTime.Now.ToString("HH:mm:ss"));
    }

    void Start()
    {
        Time.timeScale = 1f;
        Debug.Log("[SSH] Start — running sync captures");
        File.WriteAllText(Application.dataPath + "/SSH_start.txt",
            "Start " + System.DateTime.Now.ToString("HH:mm:ss") + " fc=" + Time.frameCount);

        // Allow all Awake/Start methods to settle
        Canvas.ForceUpdateCanvases();
        FixCanvasGroups();

        // --- ESTUDIO (default) ---
        CaptureSync(ScreenshotDir + "fase134b_ESTUDIO_s.png", "ESTUDIO");

        // --- MEJORAS tab ---
        ClickTab("MEJORAS");
        Canvas.ForceUpdateCanvases();
        FixCanvasGroups();
        CaptureSync(ScreenshotDir + "fase134b_MEJORAS_s.png", "MEJORAS");

        // --- PRODUCCION (bottom nav 1) ---
        ClickBottomNav(1);
        Canvas.ForceUpdateCanvases();
        FixCanvasGroups();
        CaptureSync(ScreenshotDir + "fase134b_PRODUCCION_s.png", "PRODUCCION");

        // --- PREMIOS (bottom nav 2) ---
        ClickBottomNav(2);
        Canvas.ForceUpdateCanvases();
        // Manually trigger grid resize (mimics LateUpdate from AwardsPanelUI)
        ForceAwardsPanelGridResize();
        Canvas.ForceUpdateCanvases();
        FixCanvasGroups();
        CaptureSync(ScreenshotDir + "fase134b_PREMIOS_s.png", "PREMIOS");

        File.WriteAllText(Application.dataPath + "/SSH_done.txt",
            "Done " + System.DateTime.Now.ToString("HH:mm:ss"));
        Debug.Log("[SSH] ALL DONE");
    }

    static void CaptureSync(string path, string label)
    {
        File.WriteAllText(Application.dataPath + "/SSH_coroutine.txt",
            "capturing " + label + " " + System.DateTime.Now.ToString("HH:mm:ss"));
        Debug.Log("[SSH] Capturing: " + label);

        try
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));

            const int W = 1080, H = 1920;
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var camGO = new GameObject("_CapCam");
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.09f, 0.13f, 0.19f, 1f);
            cam.cullingMask = ~0;
            cam.orthographic = true;
            cam.targetTexture = rt;
            cam.depth = 100;

            // Switch canvases to ScreenSpaceCamera so our capture cam renders them
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var savedModes = new RenderMode[canvases.Length];
            var savedCams = new Camera[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] == null || !canvases[i].isRootCanvas) continue;
                savedModes[i] = canvases[i].renderMode;
                savedCams[i] = canvases[i].worldCamera;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = cam;
            }
            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log("[SSH] Saved: " + path);

            // Restore canvases
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] == null) continue;
                try
                {
                    if (!canvases[i].isRootCanvas) continue;
                    canvases[i].renderMode = savedModes[i];
                    canvases[i].worldCamera = savedCams[i];
                }
                catch { /* destroyed canvas — skip */ }
            }
            Destroy(camGO);
            Destroy(rt);
            Destroy(tex);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SSH] Capture error " + label + ": " + e);
            File.WriteAllText(Application.dataPath + "/SSH_error_" + label + ".txt", e.ToString());
        }
    }

    static void ForceAwardsPanelGridResize()
    {
        var awards = FindObjectsByType<AwardsPanelUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var a in awards)
        {
            // Call SendMessage to trigger any refresh if available
            // Directly resize by finding the grid
            var grids = a.GetComponentsInChildren<GridLayoutGroup>(true);
            foreach (var grid in grids)
            {
                var parentRt = grid.transform.parent as RectTransform;
                if (parentRt == null) continue;
                float availW = parentRt.rect.width;
                if (availW < 10f)
                {
                    // Try grandparent
                    var grandParentRt = parentRt.parent as RectTransform;
                    if (grandParentRt != null) availW = grandParentRt.rect.width;
                }
                if (availW < 10f) continue;
                int cols = grid.constraintCount > 0 ? grid.constraintCount : 4;
                float spacing = grid.spacing.x;
                float cellW = (availW - spacing * (cols - 1)) / cols;
                if (cellW > 10f)
                {
                    grid.cellSize = new Vector2(cellW, cellW);
                    Debug.Log($"[SSH] AwardsGrid resize: availW={availW:F0} cols={cols} cellW={cellW:F0}");
                }
            }
        }
    }

    static void FixCanvasGroups()
    {
        var cgs = FindObjectsByType<CanvasGroup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var cg in cgs)
        {
            if (cg == null || cg.gameObject == null) continue;
            if (cg.alpha < 0.05f) cg.alpha = 1f;
        }
    }

    static void ClickTab(string tabText)
    {
        foreach (var btn in FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (btn == null || !btn.gameObject.activeInHierarchy) continue;
            var tmp = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmp != null && tmp.text != null && tmp.text.ToUpper().Contains(tabText.ToUpper()))
            {
                btn.onClick.Invoke();
                Debug.Log("[SSH] Clicked tab: " + tmp.text);
                return;
            }
        }
        Debug.LogWarning("[SSH] Tab not found: " + tabText);
    }

    static void ClickBottomNav(int index)
    {
        var nav = GameObject.Find("BottomNav");
        if (nav == null) { Debug.LogWarning("[SSH] BottomNav not found"); return; }
        var btns = nav.GetComponentsInChildren<Button>(true);
        if (index < btns.Length)
        {
            btns[index].onClick.Invoke();
            Debug.Log("[SSH] BottomNav[" + index + "] clicked");
        }
        else Debug.LogWarning("[SSH] BottomNav " + index + " OOB (" + btns.Length + ")");
    }
}
