using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// FASE 13.4D — captures screenshots for Mejoras (all sub-tabs), Producción, Contratos, and Premios.
/// Also injects rarity diversity into production offers for the legibility audit.
/// Attach to any GameObject in the scene, then enter Play Mode.
/// </summary>
public class Fase134dScreenshots : MonoBehaviour
{
    void Start() => StartCoroutine(Run());

    IEnumerator Run()
    {
        string dir = Application.dataPath + "/Screenshots/";
        Application.runInBackground = true;

        yield return new WaitForSeconds(2f);

        var bar = FindObjectOfType<TopBarUI>();
        if (bar != null) { bar.PatchTopBarVisuals(); bar.PatchSettingsIcon(); }

        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();

        // ── 1. MEJORAS — capture all 4 sub-tabs ──────────────────────────────
        ClickTab("Tab_ESTUDIO");
        yield return new WaitForSeconds(0.6f);
        ClickTab("Tab_MEJORAS");
        yield return new WaitForSeconds(0.8f);

        // Equipo
        ClickTab("Tab_EQUIPO");
        yield return new WaitForSeconds(0.6f);
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(dir + "fase134d_mejoras_equipo.png");
        Debug.Log("[FASE134D] shot: mejoras_equipo");
        yield return new WaitForSeconds(1.5f);

        // Personal
        ClickTab("Tab_PERSONAL");
        yield return new WaitForSeconds(0.6f);
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(dir + "fase134d_mejoras_personal.png");
        Debug.Log("[FASE134D] shot: mejoras_personal");
        yield return new WaitForSeconds(1.5f);

        // Investigación
        ClickTab("Tab_INVESTIGACI\u00d3N");
        yield return new WaitForSeconds(0.6f);
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(dir + "fase134d_mejoras_investigacion.png");
        Debug.Log("[FASE134D] shot: mejoras_investigacion");
        yield return new WaitForSeconds(1.5f);

        // Marketing
        ClickTab("Tab_MARKETING");
        yield return new WaitForSeconds(0.6f);
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(dir + "fase134d_mejoras_marketing.png");
        Debug.Log("[FASE134D] shot: mejoras_marketing");
        yield return new WaitForSeconds(1.5f);

        // ── 2. PRODUCCIÓN — inject varied rarities then screenshot ────────────
        ClickTab("Tab_PRODUCCI\u00d3N");
        yield return new WaitForSeconds(1.2f);

        // Inject Common/Rare/Epic/Legendary tints into the visible offer cards
        var offerCards = FindObjectsOfType<MovieButtonUI>(true);
        int injectedIdx = 0;
        MovieRarity[] rarities = { MovieRarity.Common, MovieRarity.Rare, MovieRarity.Epic, MovieRarity.Legendary };
        foreach (var card in offerCards)
        {
            if (card == null || !card.gameObject.activeInHierarchy) continue;
            var rt = card.transform as RectTransform;
            if (rt == null) continue;
            var rarity = rarities[injectedIdx % rarities.Length];
            MovieRarityVisual.ApplyCardBorder(rt, rarity);
            injectedIdx++;
            if (injectedIdx >= rarities.Length) break;
        }

        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(dir + "fase134d_produccion_raridades.png");
        Debug.Log("[FASE134D] shot: produccion_raridades");
        yield return new WaitForSeconds(1.5f);

        // Default production view (real save data)
        ScreenCapture.CaptureScreenshot(dir + "fase134d_produccion.png");
        Debug.Log("[FASE134D] shot: produccion");
        yield return new WaitForSeconds(1.5f);

        // ── 3. CONTRATOS ──────────────────────────────────────────────────────
        ClickTab("Tab_CONTRATOS");
        yield return new WaitForSeconds(1f);
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(dir + "fase134d_contratos.png");
        Debug.Log("[FASE134D] shot: contratos");
        yield return new WaitForSeconds(1.5f);

        // ── 4. PREMIOS — first open ───────────────────────────────────────────
        ClickTab("Tab_PREMIOS");
        yield return new WaitForSeconds(1f);
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(dir + "fase134d_premios_open1.png");
        Debug.Log("[FASE134D] shot: premios_open1");
        yield return new WaitForSeconds(1.5f);

        // Scroll down in premios then exit and re-enter multiple times
        for (int pass = 1; pass <= 3; pass++)
        {
            var scrollRect = FindScrollRect("Premios");
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f; // scroll to bottom
            yield return new WaitForSeconds(0.4f);

            // Exit Premios
            ClickTab("Tab_ESTUDIO");
            yield return new WaitForSeconds(0.4f);

            // Re-enter Premios
            ClickTab("Tab_PREMIOS");
            yield return new WaitForSeconds(1f);
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(dir + $"fase134d_premios_reentry{pass}.png");
            Debug.Log($"[FASE134D] shot: premios_reentry{pass}");
            yield return new WaitForSeconds(1.5f);
        }

        Debug.Log("[FASE134D] ALL SCREENSHOTS COMPLETE");
    }

    static void ClickTab(string tabName)
    {
        var buttons = FindObjectsOfType<UnityEngine.UI.Button>(true);
        foreach (var btn in buttons)
        {
            if (btn.name == tabName && btn.gameObject.activeInHierarchy)
            {
                btn.onClick.Invoke();
                return;
            }
        }
        // Fallback: search by child text
        foreach (var btn in buttons)
        {
            if (!btn.gameObject.activeInHierarchy) continue;
            var lbl = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl != null && lbl.text.Contains(tabName.Replace("Tab_", "")))
            {
                btn.onClick.Invoke();
                return;
            }
        }
    }

    static ScrollRect FindScrollRect(string panelName)
    {
        var scrollRects = FindObjectsOfType<ScrollRect>(true);
        foreach (var sr in scrollRects)
        {
            if (!sr.gameObject.activeInHierarchy) continue;
            Transform t = sr.transform;
            while (t != null)
            {
                if (t.name.Contains(panelName)) return sr;
                t = t.parent;
            }
        }
        return scrollRects.Length > 0 ? scrollRects[0] : null;
    }
}
