using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum DepartmentType
{
    Editor, Director, Actors, Sound, Cinematography,
    Makeup, Costume, Art, Lighting, Grip, Producer
}

/// <summary>
/// Generic hire card that works for any department.
/// Assign the DepartmentType in the Inspector and wire up the UI fields.
/// Shows the exact stat change before confirming the hire.
/// </summary>
public class DepartmentCardUI : MonoBehaviour
{
    [Header("Department")]
    public DepartmentType departmentType;

    [Header("UI")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI effectText;
    public Button hireButton;

    // ─── Static department data ───────────────────────────────────────────────
    // (name, baseCost, category label, effect per level)
    public static readonly (string name, int baseCost, string category, float effectPerLevel)[] DeptData =
    {
        ("Editor",       75,  "Calidad",    0.15f),   // Editor
        ("Director",    100,  "Calidad",    0.20f),   // Director
        ("Actores",     100,  "Calidad",    0.20f),   // Actors
        ("Sonido",       60,  "Calidad",    0.10f),   // Sound
        ("Fotografía",   80,  "Calidad",    0.15f),   // Cinematography
        ("Maquillaje",   50,  "Calidad",    0.10f),   // Makeup
        ("Vestuario",    50,  "Calidad",    0.10f),   // Costume
        ("Arte",         50,  "Calidad",    0.10f),   // Art
        ("Iluminación",  60,  "Velocidad",  0.10f),   // Lighting
        ("Grip",         60,  "Velocidad",  0.10f),   // Grip
        ("Productor",   120,  "Reducción",  0.03f),   // Producer
    };

    private const float COST_GROWTH = 1.5f;
    private int currentCost;
    private bool initialized;

    private DepartmentSystem D => GameHub.Instance?.departments;
    private StudioManager Studio => GameHub.Instance?.studio;

    // ─── Init ─────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (hireButton != null)
            hireButton.onClick.AddListener(OnHire);

        // If GameHub is already ready (it has DefaultExecutionOrder(-100)), init now.
        // Otherwise wait for the OnGameReady event.
        if (GameHub.Instance != null)
            Initialize();
        else
            GameHub.OnGameReady += Initialize;
    }

    private void OnDestroy()
    {
        GameHub.OnGameReady -= Initialize;
    }

    private void Initialize()
    {
        GameHub.OnGameReady -= Initialize;

        int idx = (int)departmentType;
        currentCost = DeptData[idx].baseCost;

        // Recompute cost from saved level so numbers are correct after load
        int level = GetLevel();
        for (int i = 0; i < level; i++)
            currentCost = Mathf.RoundToInt(currentCost * COST_GROWTH);

        initialized = true;
        RefreshUI();
    }

    // ─── Per-frame affordability check ────────────────────────────────────────

    private void Update()
    {
        if (!initialized || hireButton == null || Studio == null) return;
        hireButton.interactable = Studio.Money >= currentCost;
    }

    // ─── Hire ─────────────────────────────────────────────────────────────────

    public void OnHire()
    {
        if (!initialized) return;
        if (!Studio.TrySpendMoney(currentCost)) return;

        SetLevel(GetLevel() + 1);
        currentCost = Mathf.RoundToInt(currentCost * COST_GROWTH);

        // Quality or speed changed → income must be recalculated
        Studio.RecalculateIncome();

        RefreshUI();
    }

    // ─── UI ───────────────────────────────────────────────────────────────────

    private void RefreshUI()
    {
        if (D == null) return;

        int idx   = (int)departmentType;
        int level = GetLevel();
        var (deptName, _, category, effectPerLevel) = DeptData[idx];

        if (nameText  != null) nameText.text  = deptName;
        if (levelText != null) levelText.text = "Nivel " + level;
        if (costText  != null) costText.text  = "$" + currentCost;

        if (effectText != null)
        {
            float before = GetStat(category);
            float after  = before + effectPerLevel;

            effectText.text = category == "Reducción"
                ? string.Format("Coste: -{0:0}% → -{1:0}%", before * 100f, after * 100f)
                : string.Format("{0}: {1:0.00} → {2:0.00}", category, before, after);
        }
    }

    private float GetStat(string category)
    {
        if (D == null) return 0f;
        return category switch
        {
            "Calidad"   => D.CalculateQuality(),
            "Velocidad" => D.CalculateSpeed(),
            _           => D.CalculateCostReduction(),
        };
    }

    // ─── Department level access ──────────────────────────────────────────────

    private int GetLevel() => departmentType switch
    {
        DepartmentType.Editor         => D.editor,
        DepartmentType.Director       => D.director,
        DepartmentType.Actors         => D.actors,
        DepartmentType.Sound          => D.sound,
        DepartmentType.Cinematography => D.cinematography,
        DepartmentType.Makeup         => D.makeup,
        DepartmentType.Costume        => D.costume,
        DepartmentType.Art            => D.art,
        DepartmentType.Lighting       => D.lighting,
        DepartmentType.Grip           => D.grip,
        DepartmentType.Producer       => D.producer,
        _                             => 0
    };

    private void SetLevel(int value)
    {
        switch (departmentType)
        {
            case DepartmentType.Editor:         D.editor         = value; break;
            case DepartmentType.Director:       D.director       = value; break;
            case DepartmentType.Actors:         D.actors         = value; break;
            case DepartmentType.Sound:          D.sound          = value; break;
            case DepartmentType.Cinematography: D.cinematography = value; break;
            case DepartmentType.Makeup:         D.makeup         = value; break;
            case DepartmentType.Costume:        D.costume        = value; break;
            case DepartmentType.Art:            D.art            = value; break;
            case DepartmentType.Lighting:       D.lighting       = value; break;
            case DepartmentType.Grip:           D.grip           = value; break;
            case DepartmentType.Producer:       D.producer       = value; break;
        }
    }
}
