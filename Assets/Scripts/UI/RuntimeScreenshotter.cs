using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Captures the UI by temporarily switching Canvases to Screen Space Camera,
/// rendering to a RenderTexture, and saving as PNG. Works regardless of Game View size.
/// </summary>
public class RuntimeScreenshotter : MonoBehaviour
{
    public string filePath;
    public int width  = 1080;
    public int height = 1920;

    void Start() => StartCoroutine(Capture());

    IEnumerator Capture()
    {
        yield return new WaitForEndOfFrame();

        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 1;

        var cam = new GameObject("_CaptureCamera").AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.cullingMask     = 0;          // don't render 3D scene
        cam.orthographic    = true;
        cam.targetTexture   = rt;
        cam.depth           = 99;

        // Find all canvases and switch to Screen Space Camera
        var canvases  = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var oldModes  = new RenderMode[canvases.Length];
        var oldCams   = new Camera[canvases.Length];

        for (int i = 0; i < canvases.Length; i++)
        {
            if (!canvases[i].isRootCanvas) continue;
            oldModes[i] = canvases[i].renderMode;
            oldCams[i]  = canvases[i].worldCamera;
            canvases[i].renderMode  = RenderMode.ScreenSpaceCamera;
            canvases[i].worldCamera = cam;
        }

        Canvas.ForceUpdateCanvases();
        cam.Render();

        // Read pixels
        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        // Save
        string dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Debug.Log($"[Screenshot] Saved {filePath}");

        // Restore canvases
        for (int i = 0; i < canvases.Length; i++)
        {
            if (!canvases[i].isRootCanvas) continue;
            canvases[i].renderMode  = oldModes[i];
            canvases[i].worldCamera = oldCams[i];
        }

        Destroy(cam.gameObject);
        Destroy(rt);
        Destroy(tex);
        Destroy(gameObject);
    }
}
