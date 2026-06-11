using UnityEngine;

/// <summary>
/// Planned future content-feature upgrades (architecture only — effects TBD in balance phase).
/// </summary>
public static class FutureContentRegistry
{
    public struct PlannedFeature
    {
        public int cityLevel;
        public string upgradeId;
        public string displayName;
        public string description;
        public UpgradeCategory category;
    }

    public static readonly PlannedFeature[] PlannedUpgrades =
    {
        // Ciudad 3
        Plan(3, "feat_casting_pro",       "Casting Profesional",       "Desbloquea casting avanzado para reparto.", UpgradeCategory.ContentFeature),
        Plan(3, "feat_script_dept",       "Departamento de Guion",   "Equipo dedicado a guiones y rewrites.", UpgradeCategory.ContentFeature),
        Plan(3, "feat_exterior_locs",     "Localizaciones Exteriores","Rodajes fuera del plató principal.", UpgradeCategory.ContentFeature),

        // Ciudad 4
        Plan(4, "feat_franchise_dept",    "Departamento de Franquicias","Gestión de sagas y spin-offs.", UpgradeCategory.ContentFeature),
        Plan(4, "feat_remaster_studio",   "Remaster Studio",           "Relanzamientos y remasters del catálogo.", UpgradeCategory.ContentFeature),

        // Ciudad 5
        Plan(5, "feat_national_campaign", "Campaña Nacional",          "Marketing a escala nacional.", UpgradeCategory.ContentFeature),
        Plan(5, "feat_physical_dist",     "Distribución Física",       "Estrenos en salas y físico.", UpgradeCategory.ContentFeature),

        // Ciudad 6
        Plan(6, "feat_vfx_studio",        "VFX Studio",                "Departamento visual completo.", UpgradeCategory.ContentFeature),
        Plan(6, "feat_intl_awards",       "Premios Internacionales",   "Circuito de festivales globales.", UpgradeCategory.ContentFeature),

        // Ciudad 7
        Plan(7, "feat_global_streaming",  "Streaming Global",          "Plataforma de streaming propia.", UpgradeCategory.ContentFeature),
        Plan(7, "feat_shared_universe",   "Universo Compartido",       "Crossovers entre sagas del estudio.", UpgradeCategory.ContentFeature),

        // Ciudad 8
        Plan(8, "feat_theme_park",        "Parque Temático",           "Parque basado en tus franquicias.", UpgradeCategory.ContentFeature),
        Plan(8, "feat_official_merch",    "Merchandising Oficial",     "Línea global de productos.", UpgradeCategory.ContentFeature),
    };

    static PlannedFeature Plan(int city, string id, string name, string desc, UpgradeCategory cat) => new()
    {
        cityLevel   = city,
        upgradeId   = id,
        displayName = name,
        description = desc,
        category    = cat,
    };

    public static PlannedFeature[] GetForCity(int cityLevel)
    {
        var list = new System.Collections.Generic.List<PlannedFeature>();
        foreach (var p in PlannedUpgrades)
        {
            if (p.cityLevel == cityLevel)
                list.Add(p);
        }
        return list.ToArray();
    }
}
