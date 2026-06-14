using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>FASE 13.4C — takes 5 play-mode screenshots. Attach to any GameObject in Play Mode.</summary>
public class Fase134cScreenshots : MonoBehaviour
{
    void Start() => StartCoroutine(Run());

    IEnumerator Run()
    {
        string dir = Application.dataPath + "/Screenshots/";
        Application.runInBackground = true;

        // Wait for game to settle
        yield return new WaitForSeconds(1.5f);

        // Force top-bar patch
        var bar = FindObjectOfType<TopBarUI>();
        if (bar != null) { bar.PatchTopBarVisuals(); bar.PatchSettingsIcon(); }

        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // 1. ESTUDIO (main hub, sub-tab DEPARTAMENTOS)
        ClickTab("Tab_ESTUDIO");
        yield return new WaitForSeconds(0.5f);
        ClickTab("Tab_DEPARTAMENTOS");
        yield return new WaitForSeconds(0.5f);
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(dir + "fase134c_estudio.png");
        Debug.Log("[FASE134C] shot: estudio");

        yield return new WaitForSeconds(2f);

        // 2. MEJORAS (sub-tab within Studio)
        ClickTab("Tab_MEJORAS");
        yield return new WaitForSeconds(0.5f);
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(dir + "fase134c_mejoras.png");
        Debug.Log("[FASE134C] shot: mejoras");

        yield return new WaitForSeconds(2f);

        // 3. PRODUCCION
        ClickTab("Tab_PRODUCCI\u00d3N");
        yield return new WaitForSeconds(1f);
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(dir + "fase134c_produccion.png");
        Debug.Log("[FASE134C] shot: produccion");

        yield return new WaitForSeconds(2f);

        // 4. PREMIOS first open
        ClickTab("Tab_PREMIOS");
        yield return new WaitForSeconds(1.5f);
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(dir + "fase134c_premios1.png");
        Debug.Log("[FASE134C] shot: premios first");

        yield return new WaitForSeconds(2f);

        // 5. PREMIOS after scroll + re-enter
        var sr = FindAwardsScrollRect();
        if (sr != null) { sr.verticalNormalizedPosition = 0f; yield return new WaitForSeconds(0.3f); }
        ClickTab("Tab_ESTUDIO");
        yield return new WaitForSeconds(0.5f);
        ClickTab("Tab_PREMIOS");
        yield return new WaitForSeconds(1.5f);
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(dir + "fase134c_premios2.png");
        Debug.Log("[FASE134C] shot: premios reopen");

        yield return new WaitForSeconds(2f);
        Debug.Log("[FASE134C] ALL DONE");
        Destroy(gameObject);
    }

    static void ClickTab(string name)
    {
        var btns = FindObjectsOfType<Button>(true);
        foreach (var btn in btns)
        {
            if (btn.name == name && btn.gameObject.activeInHierarchy)
            {
                btn.onClick.Invoke();
                return;
            }
        }
        Debug.LogWarning("[FASE134C] tab not found: " + name);
    }

    static ScrollRect FindAwardsScrollRect()
    {
        var awards = FindObjectOfType<AwardsPanelUI>();
        return awards?.GetComponentInChildren<ScrollRect>(true);
    }
}
