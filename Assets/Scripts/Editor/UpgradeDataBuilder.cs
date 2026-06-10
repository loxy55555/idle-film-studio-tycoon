#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Menu: IdleFilm / Generate Upgrade Database
/// Creates UpgradeConfig assets for Equipment, Personnel, Installations.
/// Menu: IdleFilm / Generate Contract Database
/// Creates ContractConfig assets.
/// </summary>
public static class UpgradeDataBuilder
{
    private const string UPGRADE_PATH  = "Assets/Data/Upgrades";
    private const string CONTRACT_PATH = "Assets/Data/Contracts";

    // ─── EQUIPMENT ───────────────────────────────────────────────────────────

    [MenuItem("IdleFilm/Generate Upgrade Database")]
    public static void GenerateUpgrades()
    {
        Directory.CreateDirectory(UPGRADE_PATH + "/Equipment");
        Directory.CreateDirectory(UPGRADE_PATH + "/Personnel");
        Directory.CreateDirectory(UPGRADE_PATH + "/Installations");
        Directory.CreateDirectory(UPGRADE_PATH + "/Marketing");

        CreateEquipment();
        CreatePersonnel();
        CreateInstallations();
        CreateMarketing();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[UpgradeDB] Upgrade database generated.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    static void CreateEquipment()
    {
        var defs = new[]
        {
            new EquipDef("equip_camera_basic",     "Cámara Básica",
                "Una cámara de entrada nivel. Mejora la calidad visual de tus producciones.",
                0, 150, 1.40f, 10,
                new[]{E(UpgradeEffectType.Quality, 0.08f)},
                "#3498DB", "cam_basic"),

            new EquipDef("equip_camera_pro",       "Cámara Profesional",
                "Óptica superior para detalles cinematográficos.",
                4, 800, 1.42f, 10,
                new[]{E(UpgradeEffectType.Quality, 0.15f), E(UpgradeEffectType.ReputationBonus, 0.05f)},
                "#2980B9", "cam_pro"),

            new EquipDef("equip_camera_digital",   "Cámara Digital Avanzada",
                "Sensor de última generación para resolución 8K.",
                9, 6000, 1.45f, 15,
                new[]{E(UpgradeEffectType.Quality, 0.22f), E(UpgradeEffectType.ReputationBonus, 0.10f)},
                "#1A5276", "cam_digital"),

            new EquipDef("equip_lighting_kit",     "Kit de Iluminación",
                "Iluminación profesional para sets más rápidos y dinámicos.",
                2, 300, 1.40f, 10,
                new[]{E(UpgradeEffectType.Speed, 0.06f), E(UpgradeEffectType.Quality, 0.04f)},
                "#F39C12", "light_kit"),

            new EquipDef("equip_lighting_led",     "Sistema LED Cinematográfico",
                "Control total del color y temperatura. Reduce tiempos de rodaje.",
                7, 4000, 1.43f, 10,
                new[]{E(UpgradeEffectType.Speed, 0.12f), E(UpgradeEffectType.Quality, 0.08f)},
                "#D68910", "light_led"),

            new EquipDef("equip_mic_pro",          "Micrófono Profesional",
                "Captura el audio con la máxima fidelidad.",
                1, 200, 1.38f, 10,
                new[]{E(UpgradeEffectType.Quality, 0.07f)},
                "#8E44AD", "mic_pro"),

            new EquipDef("equip_sound_surround",   "Sistema de Sonido Surround",
                "Mezcla de audio inmersiva que eleva la calidad de cada escena.",
                5, 1800, 1.42f, 10,
                new[]{E(UpgradeEffectType.Quality, 0.14f), E(UpgradeEffectType.ReputationBonus, 0.06f)},
                "#7D3C98", "sound_surround"),

            new EquipDef("equip_dolby",            "Procesador Dolby Atmos",
                "Experiencia sonora de referencia en cine.",
                12, 20000, 1.45f, 10,
                new[]{E(UpgradeEffectType.Quality, 0.20f), E(UpgradeEffectType.ReputationBonus, 0.15f)},
                "#6C3483", "dolby"),

            new EquipDef("equip_vfx_basic",        "Estación VFX Básica",
                "Efectos visuales que dan vida a mundos imposibles.",
                8, 5000, 1.44f, 15,
                new[]{E(UpgradeEffectType.Quality, 0.18f), E(UpgradeEffectType.ReputationBonus, 0.12f)},
                "#27AE60", "vfx_basic"),

            new EquipDef("equip_vfx_advanced",     "Suite VFX Avanzada",
                "Efectos visuales de nivel Hollywood.",
                15, 60000, 1.46f, 15,
                new[]{E(UpgradeEffectType.Quality, 0.30f), E(UpgradeEffectType.ReputationBonus, 0.20f)},
                "#1E8449", "vfx_advanced"),

            new EquipDef("equip_drone",            "Sistema de Drones",
                "Planos aéreos espectaculares que impresionan al público.",
                6, 3000, 1.42f, 10,
                new[]{E(UpgradeEffectType.Quality, 0.12f), E(UpgradeEffectType.ReputationBonus, 0.08f)},
                "#2ECC71", "drone"),

            new EquipDef("equip_steadicam",        "Steadicam Pro",
                "Movimientos de cámara fluidos y cinematográficos.",
                3, 600, 1.40f, 10,
                new[]{E(UpgradeEffectType.Quality, 0.09f), E(UpgradeEffectType.Speed, 0.04f)},
                "#58D68D", "steadicam"),

            new EquipDef("equip_editing_soft",     "Software de Edición Pro",
                "Montaje ágil y postproducción de primera.",
                3, 500, 1.40f, 10,
                new[]{E(UpgradeEffectType.Speed, 0.10f), E(UpgradeEffectType.Quality, 0.05f)},
                "#E67E22", "edit_soft"),

            new EquipDef("equip_coloring",         "Suite de Corrección de Color",
                "El look visual definitivo para cada película.",
                10, 12000, 1.45f, 10,
                new[]{E(UpgradeEffectType.Quality, 0.17f), E(UpgradeEffectType.ReputationBonus, 0.10f)},
                "#CA6F1E", "coloring"),

            new EquipDef("equip_render_farm",      "Render Farm",
                "Procesa renders complejos a velocidad imposible.",
                18, 150000, 1.46f, 10,
                new[]{E(UpgradeEffectType.Speed, 0.25f), E(UpgradeEffectType.Quality, 0.15f)},
                "#A04000", "render_farm"),
        };

        foreach (var d in defs) CreateUpgradeAsset(d, UPGRADE_PATH + "/Equipment");
    }

    // ─────────────────────────────────────────────────────────────────────────
    static void CreatePersonnel()
    {
        var defs = new[]
        {
            new EquipDef("personal_editor",         "Editor",
                "El montador da vida a la historia.",
                0, 75, 1.50f, 25,
                new[]{E(UpgradeEffectType.Quality, 0.15f)},
                "#9B59B6", "", UpgradeCategory.Personnel),

            new EquipDef("personal_director",       "Director",
                "La visión del director lo define todo.",
                0, 100, 1.50f, 25,
                new[]{E(UpgradeEffectType.Quality, 0.20f), E(UpgradeEffectType.ReputationBonus, 0.02f)},
                "#8E44AD", "", UpgradeCategory.Personnel),

            new EquipDef("personal_actors",         "Actores",
                "Un elenco estrella multiplica el impacto.",
                0, 100, 1.50f, 25,
                new[]{E(UpgradeEffectType.Quality, 0.20f), E(UpgradeEffectType.ReputationBonus, 0.03f)},
                "#E74C3C", "", UpgradeCategory.Personnel),

            new EquipDef("personal_sound",          "Técnico de Sonido",
                "El sonido es el 50% de la experiencia.",
                1, 60, 1.48f, 25,
                new[]{E(UpgradeEffectType.Quality, 0.10f)},
                "#3498DB", "", UpgradeCategory.Personnel),

            new EquipDef("personal_cinematography", "Directora de Fotografía",
                "Cada plano, una obra de arte.",
                1, 80, 1.49f, 25,
                new[]{E(UpgradeEffectType.Quality, 0.15f)},
                "#2980B9", "", UpgradeCategory.Personnel),

            new EquipDef("personal_makeup",         "Maquillaje y Caracterización",
                "Transforma a los actores en sus personajes.",
                2, 50, 1.46f, 25,
                new[]{E(UpgradeEffectType.Quality, 0.10f)},
                "#E91E8C", "", UpgradeCategory.Personnel),

            new EquipDef("personal_costume",        "Vestuario",
                "El vestido del personaje completa su identidad.",
                2, 50, 1.46f, 25,
                new[]{E(UpgradeEffectType.Quality, 0.10f)},
                "#C2185B", "", UpgradeCategory.Personnel),

            new EquipDef("personal_art",            "Departamento de Arte",
                "El diseño de producción crea los mundos.",
                3, 50, 1.46f, 25,
                new[]{E(UpgradeEffectType.Quality, 0.10f), E(UpgradeEffectType.ReputationBonus, 0.01f)},
                "#F39C12", "", UpgradeCategory.Personnel),

            new EquipDef("personal_lighting",       "Gaffer (Iluminación Técnica)",
                "La iluminación acelera y mejora el rodaje.",
                2, 60, 1.48f, 25,
                new[]{E(UpgradeEffectType.Speed, 0.10f)},
                "#E67E22", "", UpgradeCategory.Personnel),

            new EquipDef("personal_grip",           "Grip (Maquinaria de Cámara)",
                "Movimientos imposibles al servicio de la historia.",
                3, 60, 1.48f, 25,
                new[]{E(UpgradeEffectType.Speed, 0.10f)},
                "#D35400", "", UpgradeCategory.Personnel),

            new EquipDef("personal_producer",       "Productor Ejecutivo",
                "Reduce costes y maximiza la eficiencia del estudio.",
                2, 120, 1.52f, 25,
                new[]{E(UpgradeEffectType.CostReduction, 0.03f)},
                "#27AE60", "", UpgradeCategory.Personnel),

            new EquipDef("personal_composer",       "Compositor de Banda Sonora",
                "La música eleva cada escena a otra dimensión.",
                5, 200, 1.48f, 20,
                new[]{E(UpgradeEffectType.Quality, 0.12f), E(UpgradeEffectType.ReputationBonus, 0.04f)},
                "#1ABC9C", "", UpgradeCategory.Personnel),

            new EquipDef("personal_publicist",      "Publicista",
                "Un buen PR multiplica la reputación obtenida.",
                8, 500, 1.50f, 20,
                new[]{E(UpgradeEffectType.ReputationBonus, 0.15f), E(UpgradeEffectType.XPBonus, 0.10f)},
                "#16A085", "", UpgradeCategory.Personnel),
        };

        // Force Personnel category
        foreach (var d in defs)
        {
            var copy = d; copy.category = UpgradeCategory.Personnel;
            CreateUpgradeAsset(copy, UPGRADE_PATH + "/Personnel");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    static void CreateInstallations()
    {
        var defs = new[]
        {
            new EquipDef("install_plato_small",    "Plató Pequeño",
                "Tu primer plató profesional. Permite producción simultánea básica.",
                0, 500, 1.50f, 1,
                new[]{E(UpgradeEffectType.MaxMovieSlots, 1f)},
                "#7F8C8D", "plato_small"),

            new EquipDef("install_plato_medium",   "Plató Mediano",
                "Más espacio para producciones más ambiciosas.",
                5, 5000, 1.55f, 3,
                new[]{E(UpgradeEffectType.MaxMovieSlots, 1f), E(UpgradeEffectType.Quality, 0.05f)},
                "#95A5A6", "plato_medium"),

            new EquipDef("install_plato_large",    "Plató Grande",
                "Capaz de albergar superproducciones.",
                12, 40000, 1.55f, 3,
                new[]{E(UpgradeEffectType.MaxMovieSlots, 1f), E(UpgradeEffectType.Quality, 0.10f)},
                "#ABB2B9", "plato_large"),

            new EquipDef("install_offices",        "Oficinas de Producción",
                "Mejor organización = mejores contratos y reducción de costes.",
                3, 2000, 1.48f, 5,
                new[]{E(UpgradeEffectType.CostReduction, 0.02f), E(UpgradeEffectType.XPBonus, 0.05f)},
                "#F1C40F", "offices"),

            new EquipDef("install_exterior",       "Estudios Exteriores",
                "Localizaciones al aire libre para géneros como acción y aventura.",
                8, 15000, 1.50f, 5,
                new[]{E(UpgradeEffectType.Quality, 0.08f), E(UpgradeEffectType.ReputationBonus, 0.10f)},
                "#27AE60", "exterior"),

            new EquipDef("install_editing_room",   "Sala de Montaje",
                "Postproducción dedicada. Reduce tiempos de entrega.",
                6, 8000, 1.50f, 5,
                new[]{E(UpgradeEffectType.Speed, 0.15f), E(UpgradeEffectType.Quality, 0.04f)},
                "#2ECC71", "edit_room"),

            new EquipDef("install_vfx_dept",       "Departamento de Efectos Visuales",
                "Un departamento VFX propio desbloquea géneros de ciencia ficción y fantástico.",
                14, 100000, 1.50f, 5,
                new[]{E(UpgradeEffectType.Quality, 0.20f), E(UpgradeEffectType.MaxMovieSlots, 1f)},
                "#8E44AD", "vfx_dept"),

            new EquipDef("install_screening",      "Sala de Proyección Privada",
                "Proyecciones privadas que atraen a críticos y distribuidores.",
                10, 25000, 1.48f, 5,
                new[]{E(UpgradeEffectType.ReputationBonus, 0.20f), E(UpgradeEffectType.PassiveIncomeBonus, 500f)},
                "#E74C3C", "screening"),

            new EquipDef("install_studio_lot",     "Studio Lot Completo",
                "Un backlot completo para producciones de gran escala.",
                20, 500000, 1.50f, 3,
                new[]{E(UpgradeEffectType.MaxMovieSlots, 2f), E(UpgradeEffectType.Quality, 0.25f)},
                "#C0392B", "studio_lot"),
        };

        foreach (var d in defs)
        {
            var copy = d; copy.category = UpgradeCategory.Installation;
            CreateUpgradeAsset(copy, UPGRADE_PATH + "/Installations");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    static void CreateMarketing()
    {
        var defs = new[]
        {
            new EquipDef("mkt_social_media",  "Social Media Manager",
                "Presencia en redes sociales que amplifica el alcance.",
                5, 1500, 1.45f, 10,
                new[]{E(UpgradeEffectType.ReputationBonus, 0.08f), E(UpgradeEffectType.XPBonus, 0.08f)},
                "#3498DB", "social"),

            new EquipDef("mkt_press_kit",     "Campaña de Prensa",
                "Críticas y reseñas que multiplican la visibilidad.",
                8, 5000, 1.45f, 10,
                new[]{E(UpgradeEffectType.ReputationBonus, 0.15f), E(UpgradeEffectType.PassiveIncomeBonus, 200f)},
                "#2980B9", "press"),

            new EquipDef("mkt_trailer",       "Departamento de Trailers",
                "Trailers virales que generan hype antes del estreno.",
                12, 20000, 1.46f, 8,
                new[]{E(UpgradeEffectType.ReputationBonus, 0.25f), E(UpgradeEffectType.XPBonus, 0.15f)},
                "#1ABC9C", "trailer"),

            new EquipDef("mkt_global",        "Distribución Global",
                "Tu película llega a todos los mercados del mundo.",
                18, 100000, 1.48f, 5,
                new[]{E(UpgradeEffectType.PassiveIncomeBonus, 2000f), E(UpgradeEffectType.ReputationBonus, 0.40f)},
                "#16A085", "global"),
        };

        foreach (var d in defs)
        {
            var copy = d; copy.category = UpgradeCategory.Marketing;
            CreateUpgradeAsset(copy, UPGRADE_PATH + "/Marketing");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem("IdleFilm/Generate Contract Database")]
    public static void GenerateContracts()
    {
        Directory.CreateDirectory(CONTRACT_PATH);

        var defs = new[]
        {
            // Level 1 — Tutorial contracts
            C("c_first_movie",        "Primer Rodaje",         "Produce tu primera película.",
              ContractGoalType.ProduceMovies,    MovieGenre.Action, 1, 0,
              500, 0, 2f, 10, false),
            C("c_three_movies",       "Festival de Cortometrajes","Produce 3 películas.",
              ContractGoalType.ProduceMovies,    MovieGenre.Action, 3, 0,
              1200, 0, 4f, 20, false),
            C("c_first_reputation",   "Primeros Fans",         "Alcanza 20 puntos de reputación.",
              ContractGoalType.ReachReputation,  MovieGenre.Action, 20, 0,
              800, 0, 3f, 15, false),

            // Level 2-4
            C("c_action_fan",         "Fan del Cine de Acción","Produce 2 películas de acción.",
              ContractGoalType.ProduceMoviesByGenre, MovieGenre.Action, 2, 2,
              1500, 0, 5f, 25, true),
            C("c_drama_fan",          "Fan del Drama",         "Produce 2 películas de drama.",
              ContractGoalType.ProduceMoviesByGenre, MovieGenre.Drama, 2, 2,
              1500, 0, 5f, 25, true),
            C("c_first_upgrade",      "Invertir en el Estudio","Gasta $500 en mejoras.",
              ContractGoalType.SpendOnUpgrades,  MovieGenre.Action, 500, 2,
              1000, 0, 3f, 20, false),
            C("c_rep_50",             "Estrella en Ascenso",   "Alcanza 50 de reputación.",
              ContractGoalType.ReachReputation,  MovieGenre.Action, 50, 3,
              2000, 0, 5f, 30, false),

            // Level 5-8
            C("c_studio_level5",      "Estudio Consolidado",   "Llega al nivel 5 del estudio.",
              ContractGoalType.ReachStudioLevel, MovieGenre.Action, 5, 5,
              3000, 2, 10f, 40, false),
            C("c_horror_night",       "Noche de Terror",       "Produce 3 películas de terror.",
              ContractGoalType.ProduceMoviesByGenre, MovieGenre.Horror, 3, 5,
              2500, 0, 8f, 35, true),
            C("c_speed_run",          "Producción Express",    "Produce una película en menos de 10 segundos.",
              ContractGoalType.ProduceMoviesUnderTime, MovieGenre.Action, 1, 5,
              3500, 2, 10f, 50, true),
            C("c_rep_200",            "Referente del Sector",  "Alcanza 200 de reputación.",
              ContractGoalType.ReachReputation,  MovieGenre.Action, 200, 6,
              5000, 3, 12f, 60, false),

            // Level 9-12
            C("c_studio_level10",     "Productora Establecida","Llega al nivel 10 del estudio.",
              ContractGoalType.ReachStudioLevel, MovieGenre.Action, 10, 9,
              10000, 5, 15f, 80, false),
            C("c_ten_movies",         "Maratón de Cine",       "Produce 10 películas.",
              ContractGoalType.ProduceMovies,    MovieGenre.Action, 10, 9,
              8000, 3, 18f, 70, false),
            C("c_comedy_king",        "Rey de la Comedia",     "Produce 5 comedias.",
              ContractGoalType.ProduceMoviesByGenre, MovieGenre.Comedy, 5, 9,
              6000, 3, 15f, 65, true),
            C("c_scifi_pioneer",      "Pionero de la Ciencia Ficción","Produce 3 películas de Sci-Fi.",
              ContractGoalType.ProduceMoviesByGenre, MovieGenre.SciFi, 3, 10,
              7000, 4, 18f, 75, true),

            // Level 13-18
            C("c_studio_level15",     "Gran Productora",       "Llega al nivel 15 del estudio.",
              ContractGoalType.ReachStudioLevel, MovieGenre.Action, 15, 13,
              25000, 8, 20f, 100, false),
            C("c_rep_1000",           "Leyenda del Cine",      "Alcanza 1000 de reputación.",
              ContractGoalType.ReachReputation,  MovieGenre.Action, 1000, 14,
              20000, 6, 25f, 90, false),
            C("c_all_genres",         "Cine Sin Fronteras",    "Produce una película de cada género.",
              ContractGoalType.ProduceMovies,    MovieGenre.Action, 6, 15,
              30000, 10, 30f, 120, false),
            C("c_big_spender",        "Gran Inversión",        "Gasta $50,000 en mejoras.",
              ContractGoalType.SpendOnUpgrades,  MovieGenre.Action, 50000, 16,
              40000, 8, 35f, 130, false),

            // Level 19-25
            C("c_studio_level20",     "Estudio Internacional", "Llega al nivel 20.",
              ContractGoalType.ReachStudioLevel, MovieGenre.Action, 20, 19,
              80000, 15, 30f, 180, false),
            C("c_rep_5000",           "Icono Global",          "Alcanza 5000 de reputación.",
              ContractGoalType.ReachReputation,  MovieGenre.Action, 5000, 20,
              100000, 20, 40f, 200, false),
            C("c_blockbuster",        "El Gran Blockbuster",   "Produce 5 películas de acción de alta calidad.",
              ContractGoalType.ProduceMoviesByGenre, MovieGenre.Action, 5, 22,
              150000, 25, 50f, 250, true),
        };

        foreach (var d in defs)
        {
            string assetPath = $"{CONTRACT_PATH}/{d.id}.asset";
            if (AssetDatabase.LoadAssetAtPath<ContractConfig>(assetPath) != null) continue;

            var cfg = ScriptableObject.CreateInstance<ContractConfig>();
            cfg.id                 = d.id;
            cfg.contractTitle      = d.title;
            cfg.description        = d.description;
            cfg.goalType           = d.goalType;
            cfg.targetGenre        = d.genre;
            cfg.goalAmount         = d.goal;
            cfg.unlockStudioLevel  = d.unlockLevel;
            cfg.rewardMoney        = d.money;
            cfg.rewardDiamonds     = d.diamonds;
            cfg.rewardReputation   = d.rep;
            cfg.rewardStudioXP     = d.xp;
            cfg.repeatable         = d.repeatable;

            AssetDatabase.CreateAsset(cfg, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ContractDB] Contract database generated.");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static UpgradeEffect E(UpgradeEffectType t, float v)
        => new UpgradeEffect { type = t, valuePerLevel = v };

    private static void CreateUpgradeAsset(EquipDef d, string folder)
    {
        string path = $"{folder}/{d.id}.asset";
        if (AssetDatabase.LoadAssetAtPath<UpgradeConfig>(path) != null) return;

        var cfg = ScriptableObject.CreateInstance<UpgradeConfig>();
        cfg.id                  = d.id;
        cfg.displayName         = d.name;
        cfg.description         = d.desc;
        cfg.category            = d.category;
        cfg.unlockStudioLevel   = d.unlock;
        cfg.baseCost            = d.baseCost;
        cfg.costGrowthRate      = d.growth;
        cfg.maxLevel            = d.maxLvl;
        cfg.effects             = d.effects;
        cfg.badgeColorHex       = d.color;
        cfg.iconKey             = d.iconKey;

        AssetDatabase.CreateAsset(cfg, path);
    }

    private struct EquipDef
    {
        public string          id, name, desc, color, iconKey;
        public UpgradeCategory category;
        public int             unlock, maxLvl;
        public long            baseCost;
        public float           growth;
        public UpgradeEffect[] effects;

        public EquipDef(string id, string name, string desc, int unlock, long cost, float growth,
                        int maxLvl, UpgradeEffect[] fx, string color, string icon,
                        UpgradeCategory cat = UpgradeCategory.Equipment)
        { this.id=id; this.name=name; this.desc=desc; this.unlock=unlock; baseCost=cost;
          this.growth=growth; this.maxLvl=maxLvl; effects=fx; this.color=color;
          iconKey=icon; category=cat; }
    }

    private struct ContractDef
    {
        public string id, title, description;
        public ContractGoalType goalType;
        public MovieGenre genre;
        public float goal;
        public int unlockLevel;
        public long money;
        public int diamonds, xp;
        public float rep;
        public bool repeatable;
    }

    private static ContractDef C(string id, string title, string desc,
        ContractGoalType gt, MovieGenre genre, float goal, int unlock,
        long money, int diamonds, float rep, int xp, bool repeat)
        => new ContractDef { id=id, title=title, description=desc, goalType=gt, genre=genre,
            goal=goal, unlockLevel=unlock, money=money, diamonds=diamonds, rep=rep,
            xp=xp, repeatable=repeat };
}

// ─── Personnel upgrade factory needs category override ───────────────────────
// (defined at top of UpgradeDataBuilder – local extension)
public static class UpgradeDataBuilderExt
{
    public static UpgradeConfig SetPersonnel(this UpgradeConfig cfg)
    { cfg.category = UpgradeCategory.Personnel; return cfg; }
}
#endif
