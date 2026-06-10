using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Spawns a DepartmentMiniCard for each of the 11 departments
/// inside a horizontal scroll view.
/// </summary>
public class DepartmentBarUI : MonoBehaviour
{
    [Header("References")]
    public GameObject miniCardPrefab;
    public Transform  content;

    // Per-category badge colors
    private static readonly Color[] s_CategoryColors =
    {
        new Color(0.55f, 0.27f, 0.90f), // Editor        – Creative (purple)
        new Color(0.60f, 0.20f, 0.85f), // Director
        new Color(0.65f, 0.23f, 0.80f), // Actors
        new Color(0.50f, 0.25f, 0.88f), // Sound
        new Color(0.58f, 0.28f, 0.82f), // Cinematography
        new Color(0.52f, 0.22f, 0.86f), // Makeup
        new Color(0.56f, 0.24f, 0.84f), // Costume
        new Color(0.53f, 0.26f, 0.89f), // Art
        new Color(0.20f, 0.55f, 0.90f), // Lighting  – Technical (blue)
        new Color(0.18f, 0.50f, 0.88f), // Grip
        new Color(0.90f, 0.50f, 0.13f), // Producer  – Management (orange)
    };

    private void Start()
    {
        if (miniCardPrefab == null || content == null) return;

        foreach (DepartmentType type in Enum.GetValues(typeof(DepartmentType)))
        {
            int idx = (int)type;
            GameObject card = Instantiate(miniCardPrefab, content);
            card.name = "DeptCard_" + type;

            var ui = card.GetComponent<DepartmentMiniCardUI>();
            if (ui == null) continue;

            ui.deptType = type;

            if (ui.categoryBadge != null && idx < s_CategoryColors.Length)
                ui.categoryBadge.color = s_CategoryColors[idx];
        }
    }
}
