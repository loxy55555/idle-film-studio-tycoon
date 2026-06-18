using System.Collections;
using UnityEngine;

/// <summary>
/// Temporary visual audit helper — safe to delete after audit is complete.
/// </summary>
public class CityAuditRunner : MonoBehaviour
{
    public static void Launch()
    {
        var go = new GameObject("__CityAuditRunner");
        DontDestroyOnLoad(go);
        go.AddComponent<CityAuditRunner>();
    }

    void Start()
    {
        StartCoroutine(RunAudit());
    }

    IEnumerator RunAudit()
    {
        var controller = FindObjectOfType<StudioVisualThemeController>();
        var registry   = CityBackgroundRegistry.Load();

        if (controller == null)
        {
            Debug.LogError("[AUDIT] StudioVisualThemeController NOT FOUND");
            Destroy(gameObject);
            yield break;
        }

        string dir = System.IO.Path.Combine(Application.dataPath, "Screenshots");
        System.IO.Directory.CreateDirectory(dir);

        var tiers = new CityTier[]
        {
            CityTier.City1, CityTier.City2, CityTier.City3, CityTier.City4,
            CityTier.City5, CityTier.City6, CityTier.City7, CityTier.City8
        };

        for (int i = 0; i < tiers.Length; i++)
        {
            CityTier tier   = tiers[i];
            Sprite   sprite = registry != null ? registry.GetBackground(tier) : null;
            Debug.Log($"[AUDIT] Applying City{i + 1} ({tier}): sprite={(sprite != null ? sprite.name : "NULL")}");

            controller.ApplyTheme(tier);

            for (int f = 0; f < 5; f++) yield return null;

            string path = System.IO.Path.Combine(dir, $"audit_city{i + 1}_{tier}.png");
            ScreenCapture.CaptureScreenshot(path, 1);
            Debug.Log($"[AUDIT] Screenshot → {path}");

            yield return new WaitForSeconds(2.0f);
        }

        Debug.Log("[AUDIT] === ALL CITIES COMPLETE ===");
        Destroy(gameObject);
    }
}
