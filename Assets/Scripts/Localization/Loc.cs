using System.Collections.Generic;

/// <summary>Simple key-based localization table (Phase 8.1).</summary>
public static class Loc
{
    public const string DefaultLanguage = "es";

    static string _language = DefaultLanguage;

    public static string LanguageCode
    {
        get => _language;
        set => _language = string.IsNullOrEmpty(value) ? DefaultLanguage : value;
    }

    public static string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        if (Table.TryGet(_language, key, out var value)) return value;
        if (_language != DefaultLanguage && Table.TryGet(DefaultLanguage, key, out value)) return value;
        return key;
    }

    public static string Format(string key, params object[] args)
    {
        var template = Get(key);
        return args == null || args.Length == 0 ? template : string.Format(template, args);
    }

    static class Table
    {
        static readonly Dictionary<string, Dictionary<string, string>> Languages = Build();

        public static bool TryGet(string lang, string key, out string value)
        {
            value = null;
            return Languages.TryGetValue(lang, out var dict) && dict.TryGetValue(key, out value);
        }

        static Dictionary<string, Dictionary<string, string>> Build()
        {
            var es = BaseEs();
            return new Dictionary<string, Dictionary<string, string>>
            {
                { "es", es },
                { "en", En(es) },
                { "fr", Fr(es) },
                { "de", De(es) },
                { "ja", Ja(es) },
            };
        }

        static Dictionary<string, string> BaseEs() => new()
        {
            { LocKeys.DeptLevelFormat, "NIVEL {0}" },
            { LocKeys.DeptUpgrade, "MEJORAR" },
            { LocKeys.DeptMax, "MÁXIMO" },
            { LocKeys.DeptLocked, "Bloqueado" },
            { LocKeys.DeptAvailableCity, "Disponible en Instalación {0}" },
            { LocKeys.DeptBonusSummary, "{0}\n+{1}/nv  Total +{2}" },
            { LocKeys.DeptCategoryQuality, "Calidad" },
            { LocKeys.DeptCategorySpeed, "Velocidad" },
            { LocKeys.DeptCategoryReduction, "Reducción" },
            { LocKeys.DeptNameEditor, "Editor" },
            { LocKeys.DeptNameDirector, "Director" },
            { LocKeys.DeptNameActors, "Actores" },
            { LocKeys.DeptNameSound, "Sonido" },
            { LocKeys.DeptNameCinematography, "Fotografía" },
            { LocKeys.DeptNameMakeup, "Maquillaje" },
            { LocKeys.DeptNameCostume, "Vestuario" },
            { LocKeys.DeptNameArt, "Arte" },
            { LocKeys.DeptNameLighting, "Iluminación" },
            { LocKeys.DeptNameGrip, "Grip" },
            { LocKeys.DeptNameProducer, "Productor" },

            { LocKeys.ProdTabTitle, "PRODUCCIÓN" },
            { LocKeys.ProdNewProduction, "ACTUALIZAR OFERTAS" },
            { LocKeys.ProdActiveSection, "EN RODAJE" },
            { LocKeys.ProdNoActive, "Sin producción activa" },
            { LocKeys.ProdStartMovie, "Inicia una película" },
            { LocKeys.ProdStatusProducing, "Produciendo" },
            { LocKeys.ProdStatusComplete, "Producción completada" },
            { LocKeys.ProdDiscoverButton, "ESTRENO" },
            { LocKeys.ProdSelect, "SELECCIONAR" },
            { LocKeys.ProdCatalogComplete, "Catálogo completado" },
            { LocKeys.ProdNoOffer, "Sin oferta disponible" },
            { LocKeys.ProdEmptySlot, "—" },
            { LocKeys.ProdDurationFormat, "{0}s" },
            { LocKeys.ProdTimeRemainingSeconds, "{0}s rest." },
            { LocKeys.ProdTimeRemainingMinutes, "{0}:{1:00} rest." },
            { LocKeys.ProdHistoryRewards, "+{0}  +{1} REP  +{2} XP" },
            { LocKeys.ProdBudgetTitle, "PRESUPUESTO" },
            { LocKeys.ProdBudgetSubtitle, "Elige cómo producir esta película" },
            { LocKeys.ProdBudgetCheap, "BAJO" },
            { LocKeys.ProdBudgetStandard, "NORMAL" },
            { LocKeys.ProdBudgetPremium, "PREMIUM" },
            { LocKeys.ProdBudgetCancel, "CANCELAR" },
            { LocKeys.ProdBudgetStatBlock, "Tiempo: {0}\nDinero: {1}\nReputación: {2}" },
            { LocKeys.ProdOfferMoney, "Dinero: {0}" },
            { LocKeys.ProdOfferRep, "Rep: +{0}" },
            { LocKeys.RefreshChoosePayment, "Elige cómo pagar" },
            { LocKeys.RefreshWatchAd, "VER ANUNCIO" },
            { LocKeys.RefreshSpendDiamonds, "GASTAR DIAMANTES" },
            { LocKeys.ContractRefresh, "ACTUALIZAR CONTRATOS" },
            { LocKeys.ContractSelect, "ELEGIR" },
            { LocKeys.ContractChoosePrompt, "Elige un contrato" },
            { LocKeys.ContractActiveObjective, "Objetivo: {0}" },
            { LocKeys.ContractObjProduceMovies, "Producir {0} películas" },
            { LocKeys.ContractObjProduceGenre, "Producir {0} películas de {1}" },
            { LocKeys.ContractObjReachRep, "Alcanzar {0} REP" },
            { LocKeys.ContractObjReachQuality, "Calidad ≥ {0}" },
            { LocKeys.ContractObjEarnMoney, "Ganar ${0}" },
            { LocKeys.ContractObjSpendUpgrades, "Gastar ${0} en mejoras" },
            { LocKeys.ContractObjReachLevel, "Alcanzar nivel {0}" },
            { LocKeys.ContractObjUnderTime, "Producir en ≤ {0}s" },
            { LocKeys.ProdRarityCommon, "Común" },
            { LocKeys.ProdRarityRare, "Rara" },
            { LocKeys.ProdRarityEpic, "Épica" },
            { LocKeys.ProdRarityLegendary, "Legendaria" },
            { LocKeys.GenreAction, "ACCIÓN" },
            { LocKeys.GenreDrama, "DRAMA" },
            { LocKeys.GenreHorror, "TERROR" },
            { LocKeys.GenreComedy, "COMEDIA" },
            { LocKeys.GenreRomance, "ROMANCE" },
            { LocKeys.GenreSciFi, "SCI-FI" },
            { LocKeys.GenreFantasy, "FANTASÍA" },
            { LocKeys.GenreThriller, "THRILLER" },
            { LocKeys.GenreAnimation, "ANIMACIÓN" },
            { LocKeys.GenreDocumentary, "DOCUMENTAL" },
            { LocKeys.ProdPremiereHook, "Estreno" },
            { LocKeys.ProdRewardsHook, "Recompensas" },
            { LocKeys.ProdDiscoveryHook, "Nueva película" },

            { LocKeys.FtueWelcomeBody, "Todo gran estudio comenzó con una sola película." },
            { LocKeys.FtueProductionTitle, "Primera producción" },
            { LocKeys.FtueProductionBody, "Necesitamos nuestra primera producción." },
            { LocKeys.FtueStudioTitle, "Mejora el estudio" },
            { LocKeys.FtueStudioBody, "Mejora tus departamentos para producir mejores películas." },
            { LocKeys.FtueFirstMovieTitle, "Tu primera película está lista." },
            { LocKeys.FtueCompleteBody, "Mejora el estudio y confirma para continuar." },
            { LocKeys.FtueMoneyEarned, "Dinero ganado: {0}" },
            { LocKeys.FtueRepEarned, "Reputación: +{0}" },
            { LocKeys.FtueMovieDiscovered, "Película descubierta: {0}" },
            { LocKeys.FtueContinue, "Continuar" },
            { LocKeys.FtueSkip, "Saltar" },

            { LocKeys.PremiereCompleted, "PELÍCULA COMPLETADA" },
            { LocKeys.PremiereHeadline, "ESTRENO" },
            { LocKeys.PremiereRewardsTitle, "Recompensas" },
            { LocKeys.PremiereMoneyReward, "+{0}" },
            { LocKeys.PremiereRepReward, "+{0} REP" },
            { LocKeys.PremiereDiscoveryTitle, "Nueva película descubierta" },
            { LocKeys.PremiereContinue, "Continuar" },

            { LocKeys.CollectionTitle, "COLECCIÓN" },
            { LocKeys.CollectionGlobalProgress, "{0} / {1}" },
            { LocKeys.CollectionPercent, "{0}% completado" },
            { LocKeys.CollectionGenreProgress, "{0} / {1} {2}" },
            { LocKeys.CollectionUnknownTitle, "???" },
            { LocKeys.CollectionLocked, "BLOQUEADA" },
            { LocKeys.CollectionDiscovered, "DESCUBIERTA" },
            { LocKeys.CollectionLegendaryLock, "Completa todas las películas de este género para desbloquear esta película legendaria." },
            { LocKeys.CollectionSelectGenre,  "Elige un género" },
            { LocKeys.CollectionAllMovies,    "Todas las películas" },
            { LocKeys.CollectionBack,         "← VOLVER" },

            // Instalaciones
            { LocKeys.InstLevel,          "Instalación {0}" },
            { LocKeys.InstAvailable,      "Disponible en Instalación {0}" },
            { LocKeys.InstProgressScreen, "INSTALACIONES" },
            { LocKeys.InstUpgrade,        "EXPANDIR" },
            { LocKeys.InstNextLevel,      "Próxima expansión:" },
            { LocKeys.InstCurrentLevel,   "Instalación {0}" },
            { LocKeys.InstMultiplier,     "Instalación {0} · ×{1:0.00}" },
            { LocKeys.InstLocked,         "BLOQUEADO" },

            // Bonificaciones
            { LocKeys.BonifTitle,     "BONIFICACIONES" },
            { LocKeys.BonifSpeed,     "Velocidad" },
            { LocKeys.BonifQuality,   "Calidad" },
            { LocKeys.BonifBoxOffice, "Taquilla" },
            { LocKeys.BonifRep,       "REP" },
            { LocKeys.BonifXP,        "XP" },
            { LocKeys.BonifCosts,     "Costes" },

            // Settings
            { LocKeys.SettingsTitle,      "AJUSTES" },
            { LocKeys.SettingsLanguage,   "Idioma" },
            { LocKeys.SettingsGooglePlay, "Google Play Games" },
            { LocKeys.SettingsAccount,    "Cuenta" },
            { LocKeys.SettingsSupport,    "Soporte" },
            { LocKeys.SettingsCredits,    "Créditos" },
            { LocKeys.SettingsPrivacy,    "Política de privacidad" },
            { LocKeys.SettingsRestore,    "Restaurar compras" },
            { LocKeys.IapRestoreSuccess,  "Compras restauradas" },
            { LocKeys.IapRestoreFailed,   "No se pudieron restaurar las compras" },
            { LocKeys.IapPurchaseFailed,  "Compra cancelada o fallida" },
            { LocKeys.IapUnavailable,     "Tienda no disponible" },
            { LocKeys.IapPackActivatedFmt,"Pack activado: {0}" },
            { LocKeys.SettingsClose,      "CERRAR" },
            { LocKeys.SettingsMusic,      "Música" },
            { LocKeys.SettingsSfx,        "Efectos de sonido" },
            { LocKeys.SettingsGroupAudio, "AUDIO" },
            { LocKeys.SettingsGroupLang,  "IDIOMA" },
            { LocKeys.SettingsGroupInfo,  "INFORMACIÓN" },

            // Tienda
            { LocKeys.TiendaTitle,      "TIENDA" },
            { LocKeys.TiendaComingSoon, "Próximamente" },

            // Nav tabs
            { LocKeys.NavStudio,     "ESTUDIO" },
            { LocKeys.NavProduction, "PRODUCCIÓN" },
            { LocKeys.NavAwards,     "PREMIOS" },
            { LocKeys.NavCollection, "COLECCIÓN" },
            { LocKeys.NavShop,       "TIENDA" },

            // Mejoras sub-tabs
            { LocKeys.MejorasProduction, "PRODUCCIÓN" },
            { LocKeys.MejorasStaff,      "PERSONAL" },
            { LocKeys.MejorasResearch,   "INVESTIGACIÓN" },
            { LocKeys.MejorasMarketing,  "MARKETING" },

            // Awards
            { LocKeys.AwardsTitle,         "ESTRELLAS DE ORO" },
            { LocKeys.AwardsNextStar,      "SIGUIENTE ESTRELLA" },
            { LocKeys.AwardsProgressSect,  "PROGRESO DEL ESTUDIO" },
            { LocKeys.AwardsKeepProducing, "Sigue produciendo..." },
            { LocKeys.AwardsAllEarned,     "Todas las Estrellas de Oro conseguidas" },
            { LocKeys.AwardsComplete,      "¡Colección completa!" },
            { LocKeys.AwardsEmpireMax,     "¡Imperio Cinematográfico!" },
            { LocKeys.AwardsReadyClaim,    "¡LISTA PARA RECLAMAR!" },
            { LocKeys.AwardsClaimBtn,      "✦ CONSEGUIR ESTRELLA DE ORO" },
            { LocKeys.AwardsRepNeeded,     "REP necesaria: {0:N0} / {1:N0}" },
            { LocKeys.AwardsHint,          "Las Estrellas de Oro impulsan el crecimiento del estudio." },
            { LocKeys.AwardsStudioLevel,   "Estudio Nv. {0}" },

            // Production slots
            { LocKeys.ProdSlotFree,   "SLOT LIBRE" },
            { LocKeys.ProdSlotLocked, "SLOT BLOQUEADO" },
            { LocKeys.ProdSlotUnlock, "Desbloquea en Mejoras → Instalaciones" },

            // Upgrade cards
            { LocKeys.UpgradeNoEffect,     "Sin efecto" },
            { LocKeys.UpgradeLockedStudio, "Nv.{0} estudio" },

            // Contracts panel
            { LocKeys.ContractActive,        "CONTRATO ACTIVO" },
            { LocKeys.ContractNoneAvailable, "Sin contrato disponible" },
            { LocKeys.ContractClaim,         "RECLAMAR" },
            { LocKeys.ContractCompletedPfx,  "Completado — " },
            { LocKeys.ContractNoActive,      "Sin contrato activo" },
            { LocKeys.ContractReadyClaim,    "¡Listo para reclamar!" },
            { LocKeys.ContractObjective,     "OBJETIVO" },

            // Collection screen
            { LocKeys.CollectionScreenTitle,   "FILMOTECA" },
            { LocKeys.CollectionGenreLabel,    "GÉNERO" },
            { LocKeys.CollectionSynopsisHdr,   "SINOPSIS" },
            { LocKeys.CollectionClose,         "CERRAR" },
            { LocKeys.CollectionSynopsisNone,  "Sinopsis no disponible." },
            { LocKeys.CollectionFilmCompleted, "✔ COMPLETADA" },
            { LocKeys.CollectionFilmPending,   "○ PENDIENTE" },

            // GameFeel popups
            { LocKeys.GameFeelContractComplete, "CONTRATO COMPLETADO" },
            { LocKeys.GameFeelOscarEarned,      "OSCAR CONSEGUIDO" },
            { LocKeys.GameFeelCityImproved,     "CIUDAD MEJORADA" },
            { LocKeys.GameFeelUpgradeBought,    "MEJORA ADQUIRIDA" },
            { LocKeys.GameFeelContinue,         "CONTINUAR" },
            { LocKeys.GameFeelOscarsSuffix,     " Oscar(s) · Mejora tu Ciudad" },
            { LocKeys.GameFeelIncomeGlobal,     " ingreso global" },

            // Studio chrome
            { LocKeys.StudioName,            "IDLE FILM STUDIO" },
            { LocKeys.StudioLevelFormat,     "Nivel {0}" },
            { LocKeys.StudioXPFormat,        "{0:0}/{1:0} XP" },
            { LocKeys.StudioNextLevel,       "Próximo nivel · {0:0} XP restantes" },
            { LocKeys.StudioBonusSummaryFmt, "Ingresos +{0:0}% · REP +{1:0}% · Vel +{2:0}%" },
            { LocKeys.StudioRepBarFmt,       "{0:0.#} REP" },

            // TopBar abbreviations
            { LocKeys.TopBarDiam,       "DIAM" },
            { LocKeys.TopBarQualAbbr,   "Cal" },
            { LocKeys.TopBarSpeedAbbr,  "Vel" },
            { LocKeys.TopBarLevelAbbr,  "Nv." },
            { LocKeys.TopBarIncomeFmt,  "+{0}/s" },

            // Upgrade card effects
            { LocKeys.UpgradeMax,           "MAX" },
            { LocKeys.UpgradeCurrent,       "Actual" },
            { LocKeys.UpgradeNext,          "Siguiente" },
            { LocKeys.UpgradeEffectQuality, "+{0:0.#}% Calidad" },
            { LocKeys.UpgradeEffectSpeed,   "+{0:0.#}% Velocidad" },
            { LocKeys.UpgradeEffectCost,    "-{0:0.#}% Coste" },
            { LocKeys.UpgradeEffectRep,     "+{0:0.#}% Reputación" },
            { LocKeys.UpgradeEffectIncome,  "+{0}/s Ingresos" },
            { LocKeys.UpgradeEffectSlot,    "+{0} Slot(s) Producción" },
            { LocKeys.UpgradeEffectXP,      "+{0:0.#}% XP" },
            { LocKeys.UpgradeEffectCatalog, "Catálogo Nv.{0}" },

            // Awards remaining format strings
            { LocKeys.AwardsStarTitleFmt,  "Estrella #{0}" },
            { LocKeys.AwardsRepReadyFmt,   "Reputación: {0:N0} / {1:N0} REP ✓" },
            { LocKeys.AwardsUnlockHintFmt, "{0}  ·  {1} para desbloq. siguiente" },

            // City / Installation panel
            { LocKeys.InstGlobalBonus,     "BONUS GLOBAL" },
            { LocKeys.InstGlobalBonusFmt,  "×{0:0.00} ingreso global" },
            { LocKeys.InstCurrentUnlocks,  "DESBLOQUEOS ACTUALES" },
            { LocKeys.InstMaxCity,         "Has alcanzado el Imperio Cinematográfico." },

            // Production
            { LocKeys.ProdProduceBtn,  "PRODUCIR" },
            { LocKeys.ProdEmptyState,  "No hay películas disponibles." },

            // Collection stats
            { LocKeys.CollStatDurFmt,   "{0}min" },
            { LocKeys.CollStatLvlFmt,   "Nv.{0}" },
            { LocKeys.CollStatMoneyFmt, "{0}" },

            // Contract rewards
            { LocKeys.ContractRewardMoney, "+{0}" },
            { LocKeys.ContractRewardDiam,  "+{0}" },
            { LocKeys.ContractRewardRep,   "+{0:0.#} REP" },
            { LocKeys.ContractRewardXP,    "+{0} XP" },

            // Premiere overlay
            { LocKeys.PremiereXPFmt,        "+{0:0} XP" },
            { LocKeys.PremiereVarietyFmt,   "Variedad +{0:0}%" },
            { LocKeys.PremiereFilmFound,    "Película descubierta" },
            { LocKeys.PremiereContractDone, "Contrato completado" },
            { LocKeys.PremiereContractProg, "Contrato · {0:0}/{1:0}" },

            // GameFeel remaining
            { LocKeys.GameFeelUpgradeFallback, "Mejora adquirida" },

            // Store sections
            { LocKeys.StoreSectionDiamonds, "DIAMANTES" },
            { LocKeys.StoreSectionPacks,    "› PACKS" },
            { LocKeys.StoreSectionBoosts,   "› BOOSTS" },
            { LocKeys.StoreSectionPremium,  "› PREMIUM" },
            { LocKeys.StoreWatchAd,         "Ver anuncio" },
            { LocKeys.StoreStarterPack,     "STARTER PACK" },
            { LocKeys.StoreStarterDesc,     "Diamantes + Boost + Sin anuncios 24h" },
            { LocKeys.StoreUniqueOffer,     "OFERTA ÚNICA" },
            { LocKeys.StoreNoAds,           "SIN ANUNCIOS" },
            { LocKeys.StoreNoAdsDesc,       "Elimina los anuncios para siempre" },

            // Store products
            { LocKeys.StoreProdDiamSmall,    "PUÑADO" },
            { LocKeys.StoreProdDiamMed,      "BOLSA" },
            { LocKeys.StoreProdDiamLarge,    "COFRE" },
            { LocKeys.StoreProdPackDir,      "PACK DIRECTOR" },
            { LocKeys.StoreProdPackDirDesc,  "Diamantes + Boost\n+ Película rara" },
            { LocKeys.StoreProdPackStar,     "PACK ESTRELLA" },
            { LocKeys.StoreProdPackStarDesc, "Diamantes + REP\n+ Película épica" },
            { LocKeys.StoreProdBoostProd,    "PRODUCCIÓN x2" },
            { LocKeys.StoreProdBoostInc,     "INGRESOS x2" },

            // Production slot formats
            { LocKeys.ProdTimeFmt,       "{0}" },
            { LocKeys.ProdSlotRewardFmt, "{0} · +{1:0.0} REP" },

            // Contract reroll
            { LocKeys.ContractRerollBtn, "5" },

            // Settings coming-soon badge
            { LocKeys.SettingsComing, "PRONTO" },

            // Studio header REP format (no ★)
            { LocKeys.StudioRepHeaderFmt, "{0:0} REP" },

            // Studio main subtabs
            { LocKeys.StudTabDepts,     "DEPARTAMENTOS" },
            { LocKeys.StudTabMejoras,   "MEJORAS" },
            { LocKeys.StudTabContratos, "CONTRATOS" },

            // Credits panel
            { LocKeys.CreditsCreatedBy,  "Creado por" },
            { LocKeys.CreditsVersion,    "Versión {0}" },
            { LocKeys.CreditsPoweredBy,  "Desarrollado con Unity" },

            // Stat abbreviations (same across languages, overridden per-locale as needed)
            { LocKeys.RepSuffix,         "REP" },
            { LocKeys.XPSuffix,          "XP" },

            // Income display
            { LocKeys.IncomePerSecFmt,   "+${0:N0}/seg" },

            // ── City / Facility names ─────────────────────────────────────────
            { LocKeys.CityNamePfx + "1", "Garaje" },
            { LocKeys.CityNamePfx + "2", "Estudio Independiente" },
            { LocKeys.CityNamePfx + "3", "Estudio Local" },
            { LocKeys.CityNamePfx + "4", "Estudio Regional" },
            { LocKeys.CityNamePfx + "5", "Gran Estudio" },
            { LocKeys.CityNamePfx + "6", "Hollywood Boulevard" },
            { LocKeys.CityNamePfx + "7", "Major Studio" },
            { LocKeys.CityNamePfx + "8", "Imperio Cinematográfico" },

            // ── Upgrade names ──────────────────────────────────────────────────
            { LocKeys.UpgradeNamePfx + "equip_camera_basic",    "Cámara Básica" },
            { LocKeys.UpgradeNamePfx + "equip_camera_digital",  "Cámara Digital Avanzada" },
            { LocKeys.UpgradeNamePfx + "equip_camera_pro",      "Cámara Profesional" },
            { LocKeys.UpgradeNamePfx + "equip_coloring",        "Suite de Corrección de Color" },
            { LocKeys.UpgradeNamePfx + "equip_dolby",           "Procesador Dolby Atmos" },
            { LocKeys.UpgradeNamePfx + "equip_drone",           "Sistema de Drones" },
            { LocKeys.UpgradeNamePfx + "equip_editing_soft",    "Software de Edición Pro" },
            { LocKeys.UpgradeNamePfx + "equip_lighting_kit",    "Kit de Iluminación" },
            { LocKeys.UpgradeNamePfx + "equip_lighting_led",    "Sistema LED Cinematográfico" },
            { LocKeys.UpgradeNamePfx + "equip_mic_pro",         "Micrófono Profesional" },
            { LocKeys.UpgradeNamePfx + "equip_render_farm",     "Render Farm" },
            { LocKeys.UpgradeNamePfx + "equip_sound_surround",  "Sistema de Sonido Surround" },
            { LocKeys.UpgradeNamePfx + "equip_steadicam",       "Steadicam Pro" },
            { LocKeys.UpgradeNamePfx + "equip_vfx_advanced",    "Suite VFX Avanzada" },
            { LocKeys.UpgradeNamePfx + "equip_vfx_basic",       "Estación VFX Básica" },
            { LocKeys.UpgradeNamePfx + "install_editing_room",  "Sala de Montaje" },
            { LocKeys.UpgradeNamePfx + "install_exterior",      "Estudios Exteriores" },
            { LocKeys.UpgradeNamePfx + "install_offices",       "Oficinas de Producción" },
            { LocKeys.UpgradeNamePfx + "install_plato_large",   "Plató Grande" },
            { LocKeys.UpgradeNamePfx + "install_plato_medium",  "Plató Mediano" },
            { LocKeys.UpgradeNamePfx + "install_plato_small",   "Plató Pequeño" },
            { LocKeys.UpgradeNamePfx + "install_screening",     "Sala de Proyección Privada" },
            { LocKeys.UpgradeNamePfx + "install_studio_lot",    "Studio Lot Completo" },
            { LocKeys.UpgradeNamePfx + "install_vfx_dept",      "Departamento de Efectos Visuales" },
            { LocKeys.UpgradeNamePfx + "mkt_global",            "Distribución Global" },
            { LocKeys.UpgradeNamePfx + "mkt_press_kit",         "Campaña de Prensa" },
            { LocKeys.UpgradeNamePfx + "mkt_social_media",      "Social Media Manager" },
            { LocKeys.UpgradeNamePfx + "mkt_trailer",           "Departamento de Trailers" },
            { LocKeys.UpgradeNamePfx + "personal_actors",       "Actores" },
            { LocKeys.UpgradeNamePfx + "personal_art",          "Departamento de Arte" },
            { LocKeys.UpgradeNamePfx + "personal_cinematography","Directora de Fotografía" },
            { LocKeys.UpgradeNamePfx + "personal_composer",     "Compositor de Banda Sonora" },
            { LocKeys.UpgradeNamePfx + "personal_costume",      "Vestuario" },
            { LocKeys.UpgradeNamePfx + "personal_director",     "Director" },
            { LocKeys.UpgradeNamePfx + "personal_editor",       "Editor" },
            { LocKeys.UpgradeNamePfx + "personal_grip",         "Grip (Maquinaria de Cámara)" },
            { LocKeys.UpgradeNamePfx + "personal_lighting",     "Gaffer (Iluminación Técnica)" },
            { LocKeys.UpgradeNamePfx + "personal_makeup",       "Maquillaje y Caracterización" },
            { LocKeys.UpgradeNamePfx + "personal_producer",     "Productor Ejecutivo" },
            { LocKeys.UpgradeNamePfx + "personal_publicist",    "Publicista" },
            { LocKeys.UpgradeNamePfx + "personal_sound",        "Técnico de Sonido" },

            // ── Contract titles ───────────────────────────────────────────────
            { LocKeys.ContractTitlePfx + "c_three_movies",      "Festival de Cortometrajes" },
            { LocKeys.ContractTitlePfx + "c_first_reputation",  "Primeros Fans" },
            { LocKeys.ContractTitlePfx + "c_first_movie",       "Primer Rodaje" },
            { LocKeys.ContractTitlePfx + "c_action_fan",        "Fan del Cine de Acción" },
            { LocKeys.ContractTitlePfx + "c_romance_story",     "Historia de Amor" },
            { LocKeys.ContractTitlePfx + "c_first_upgrade",     "Invertir en el Estudio" },
            { LocKeys.ContractTitlePfx + "c_fantasy_realm",     "Reinos de Fantasía" },
            { LocKeys.ContractTitlePfx + "c_thriller_night",    "Noche de Suspenso" },
            { LocKeys.ContractTitlePfx + "c_documentary_truth", "La Verdad del Cine" },
            { LocKeys.ContractTitlePfx + "c_drama_fan",         "Fan del Drama" },
            { LocKeys.ContractTitlePfx + "c_animation_studio",  "Estudio de Animación" },
            { LocKeys.ContractTitlePfx + "c_rep_50",            "Estrella en Ascenso" },
            { LocKeys.ContractTitlePfx + "c_horror_night",      "Noche de Terror" },
            { LocKeys.ContractTitlePfx + "c_speed_run",         "Producción Express" },
            { LocKeys.ContractTitlePfx + "c_studio_level5",     "Estudio Consolidado" },
            { LocKeys.ContractTitlePfx + "c_rep_200",           "Referente del Sector" },
            { LocKeys.ContractTitlePfx + "c_comedy_king",       "Rey de la Comedia" },
            { LocKeys.ContractTitlePfx + "c_studio_level10",    "Productora Establecida" },
            { LocKeys.ContractTitlePfx + "c_ten_movies",        "Maratón de Cine" },
            { LocKeys.ContractTitlePfx + "c_scifi_pioneer",     "Pionero de la Ciencia Ficción" },
            { LocKeys.ContractTitlePfx + "c_studio_level15",    "Gran Productora" },
            { LocKeys.ContractTitlePfx + "c_rep_1000",          "Leyenda del Cine" },
            { LocKeys.ContractTitlePfx + "c_all_genres",        "Cine Sin Fronteras" },
            { LocKeys.ContractTitlePfx + "c_big_spender",       "Gran Inversión" },
            { LocKeys.ContractTitlePfx + "c_studio_level20",    "Estudio Internacional" },
            { LocKeys.ContractTitlePfx + "c_rep_5000",          "Icono Global" },
            { LocKeys.ContractTitlePfx + "c_blockbuster",       "El Gran Blockbuster" },

            // ── Oscar / installation progress ────────────────────────────────
            { LocKeys.InstOscarProgress,  "{0} / {1} OSC" },
            { LocKeys.InstOscarMax,       "{0} OSC · MÁXIMO" },
            { LocKeys.InstUnlockGlobal,   "×{0:0.00} ingreso global" },
            { LocKeys.InstUnlockDept,     "Dept: {0}" },
            { LocKeys.InstTierPfx + "1",  "Tier 1–2 · películas básicas" },
            { LocKeys.InstTierPfx + "2",  "Secuelas y remakes" },
            { LocKeys.InstTierPfx + "3",  "Producción profesional" },
            { LocKeys.InstTierPfx + "4",  "Películas avanzadas" },
            { LocKeys.InstTierPfx + "5",  "Campaña y distribución" },
            { LocKeys.InstTierPfx + "6",  "VFX y premios" },
            { LocKeys.InstTierPfx + "7",  "Streaming y universos" },
            { LocKeys.InstTierPfx + "8",  "Imperio completo" },

            // ── City lock labels ─────────────────────────────────────────────
            { LocKeys.CityLockLabel,      "Requiere Ciudad {0}" },

            // ── Offer card badges ────────────────────────────────────────────
            { LocKeys.OfferBadgeNew,      "[NUEVA]" },
            { LocKeys.OfferBadgeSaga,     "[SAGA]" },
            { LocKeys.OfferBadgeContract, "[CONTRATO]" },

            // ── Saga labels ──────────────────────────────────────────────────
            { LocKeys.SagaCompleted,      "{0}/{1} COMPLETADA" },
            { LocKeys.SagaBlockReason,    "Completa la entrada {0} de la saga primero" },

            // ── Ad reward feedback (FASE 15.2A) ──────────────────────────────
            { LocKeys.AdLimitReached,        "Límite diario alcanzado" },
            { LocKeys.FreeDiamondsAwarded,   "+{0} diamantes" },
            { LocKeys.BoostActivated,        "¡Boost activado! ×2 durante 10 min" },
            { LocKeys.BoostAlreadyActive,    "Ya tienes este boost activo" },
            { LocKeys.InvestorAwarded,       "+${0:N0} del inversor" },
            { LocKeys.InvestorNoIncome,      "Income actual es 0. Produce más películas primero." },

            // ── Store new sections ────────────────────────────────────────────
            { LocKeys.StoreSectionInvestor,    "Inversor" },
            { LocKeys.StoreSectionFreeRewards, "Recompensas Diarias" },
            { LocKeys.StoreProdBoostRep,       "Boost REP" },
            { LocKeys.StoreProdBoostXP,        "Boost XP" },
            { LocKeys.StoreProdFreeDiam,       "Gratis" },
            { LocKeys.StoreProdInvestor,       "Inversor" },
            { LocKeys.StoreProdOfflinePremium, "Productor Remoto" },
            { LocKeys.StoreBoostTimerFmt,      "{0}m {1}s" },
            { LocKeys.StoreBoostActive,        "ACTIVO ·" },
            { LocKeys.StorePackStarter,        "Starter Pack" },
            { LocKeys.StorePackSupporter,      "Supporter" },
            { LocKeys.StorePackProducer,       "Producer" },
            { LocKeys.StorePackExecutive,      "Executive Producer" },
            { LocKeys.StorePackAlreadyOwned,   "Ya adquirido" },
            { LocKeys.StoreSimBuy,             "[SIMULAR COMPRA]" },

            // ── Offline premium ───────────────────────────────────────────────
            { LocKeys.OfflinePremiumName,        "Productor Remoto" },
            { LocKeys.OfflinePremiumDesc,        "Amplía el límite offline a 2 horas" },
            { LocKeys.OfflinePremiumOwned,       "Desbloqueado" },
            { LocKeys.OfflinePremiumCostPending, "Precio pendiente" },

            // ── Pack descriptions (FASE 15.2B) ────────────────────────────────
            { LocKeys.PackSupporterDesc, "Gracias por apoyar Film Producer Tycoon." },
            { LocKeys.PackProducerDesc,  "Impulsa tu carrera como productor y obtén una importante reserva de diamantes." },
            { LocKeys.PackExecutiveDesc, "La máxima edición para auténticos magnates del cine." },

            // ── Investor cooldown ─────────────────────────────────────────────
            { LocKeys.InvestorCooldownFmt, "CD · {0}m {1}s" },
            { LocKeys.InvestorReady,       "5 min income" },
            { LocKeys.InvestorReadyLabel,  "5 min. de ingresos" },

            // ── UX feedback dialogs (FASE 16.1 — D1, D2) ─────────────────────
            { LocKeys.UxNotEnoughDiamonds, "No tienes suficientes diamantes." },
            { LocKeys.UxGoToStore,         "Ir a tienda" },
            { LocKeys.UxCancelAction,      "Cancelar" },
            { LocKeys.UxNoSlotsAvailable,  "No hay slots disponibles." },

            // ── Contract cancel via ad (FASE 16.1 — D3) ──────────────────────
            { LocKeys.ContractCancelWithAd,  "Cancelar contrato" },
            { LocKeys.ContractCancelConfirm, "¿Cancelar el contrato activo?" },
            { LocKeys.ContractCancelDone,    "Contrato cancelado. Nuevas opciones generadas." },
        };

        static Dictionary<string, string> En(Dictionary<string, string> es)
        {
            var d = new Dictionary<string, string>(es);
            d[LocKeys.DeptLevelFormat] = "LEVEL {0}";
            d[LocKeys.DeptUpgrade] = "UPGRADE";
            d[LocKeys.DeptMax] = "MAX";
            d[LocKeys.DeptLocked] = "Locked";
            d[LocKeys.DeptAvailableCity] = "Available in Facility {0}";
            d[LocKeys.DeptBonusSummary] = "{0}\n+{1}/lvl  Total +{2}";
            d[LocKeys.DeptCategoryQuality] = "Quality";
            d[LocKeys.DeptCategorySpeed] = "Speed";
            d[LocKeys.DeptCategoryReduction] = "Cost Reduction";
            d[LocKeys.DeptNameActors] = "Actors";
            d[LocKeys.DeptNameSound] = "Sound";
            d[LocKeys.DeptNameCinematography] = "Cinematography";
            d[LocKeys.DeptNameMakeup] = "Makeup";
            d[LocKeys.DeptNameCostume] = "Costume";
            d[LocKeys.DeptNameArt] = "Art";
            d[LocKeys.DeptNameLighting] = "Lighting";
            d[LocKeys.DeptNameProducer] = "Producer";
            d[LocKeys.ProdTabTitle] = "PRODUCTION";
            d[LocKeys.ProdNewProduction] = "REFRESH OFFERS";
            d[LocKeys.ProdActiveSection] = "IN PRODUCTION";
            d[LocKeys.ProdNoActive] = "No active production";
            d[LocKeys.ProdStartMovie] = "Start a movie";
            d[LocKeys.ProdStatusProducing] = "Producing";
            d[LocKeys.ProdStatusComplete] = "Production complete";
            d[LocKeys.ProdDiscoverButton] = "PREMIERE";
            d[LocKeys.ProdSelect] = "SELECT";
            d[LocKeys.ProdCatalogComplete] = "Catalog complete";
            d[LocKeys.ProdNoOffer] = "No offer available";
            d[LocKeys.ProdBudgetTitle] = "BUDGET";
            d[LocKeys.ProdBudgetSubtitle] = "Choose how to produce this film";
            d[LocKeys.ProdBudgetCheap] = "LOW";
            d[LocKeys.ProdBudgetStandard] = "NORMAL";
            d[LocKeys.ProdBudgetPremium] = "PREMIUM";
            d[LocKeys.ProdBudgetCancel] = "CANCEL";
            d[LocKeys.ProdBudgetStatBlock] = "Time: {0}\nMoney: {1}\nReputation: {2}";
            d[LocKeys.ProdOfferMoney] = "Money: {0}";
            d[LocKeys.ProdOfferRep] = "Rep: +{0}";
            d[LocKeys.RefreshChoosePayment] = "Choose payment";
            d[LocKeys.RefreshWatchAd] = "WATCH AD";
            d[LocKeys.RefreshSpendDiamonds] = "SPEND DIAMONDS";
            d[LocKeys.ContractRefresh] = "REFRESH CONTRACTS";
            d[LocKeys.ContractSelect] = "CHOOSE";
            d[LocKeys.ContractChoosePrompt] = "Choose a contract";
            d[LocKeys.ContractActiveObjective] = "Objective: {0}";
            d[LocKeys.ContractObjProduceMovies] = "Produce {0} movies";
            d[LocKeys.ContractObjProduceGenre] = "Produce {0} {1} movies";
            d[LocKeys.ContractObjReachRep] = "Reach {0} REP";
            d[LocKeys.ContractObjReachQuality] = "Quality ≥ {0}";
            d[LocKeys.ContractObjEarnMoney] = "Earn ${0}";
            d[LocKeys.ContractObjSpendUpgrades] = "Spend ${0} on upgrades";
            d[LocKeys.ContractObjReachLevel] = "Reach level {0}";
            d[LocKeys.ContractObjUnderTime] = "Produce in ≤ {0}s";
            d[LocKeys.ProdRarityCommon] = "Common";
            d[LocKeys.ProdRarityRare] = "Rare";
            d[LocKeys.ProdRarityEpic] = "Epic";
            d[LocKeys.ProdRarityLegendary] = "Legendary";
            d[LocKeys.GenreAction] = "ACTION";
            d[LocKeys.GenreDrama] = "DRAMA";
            d[LocKeys.GenreHorror] = "HORROR";
            d[LocKeys.GenreComedy] = "COMEDY";
            d[LocKeys.GenreRomance] = "ROMANCE";
            d[LocKeys.GenreSciFi] = "SCI-FI";
            d[LocKeys.GenreFantasy] = "FANTASY";
            d[LocKeys.GenreThriller] = "THRILLER";
            d[LocKeys.GenreAnimation] = "ANIMATION";
            d[LocKeys.GenreDocumentary] = "DOCUMENTARY";
            d[LocKeys.FtueWelcomeBody] = "Every great studio started with a single film.";
            d[LocKeys.FtueProductionTitle] = "First production";
            d[LocKeys.FtueProductionBody] = "We need our first production.";
            d[LocKeys.FtueStudioTitle] = "Upgrade the studio";
            d[LocKeys.FtueStudioBody] = "Upgrade your departments to make better films.";
            d[LocKeys.FtueFirstMovieTitle] = "Your first film is ready.";
            d[LocKeys.FtueCompleteBody] = "Upgrade the studio and confirm to continue.";
            d[LocKeys.FtueMoneyEarned] = "Money earned: {0}";
            d[LocKeys.FtueRepEarned] = "Reputation: +{0}";
            d[LocKeys.FtueMovieDiscovered] = "Film discovered: {0}";
            d[LocKeys.FtueContinue] = "Continue";
            d[LocKeys.FtueSkip] = "Skip";
            d[LocKeys.PremiereCompleted] = "Premiere complete";
            d[LocKeys.PremiereRewardsTitle] = "Rewards";
            d[LocKeys.PremiereMoneyReward] = "+{0}";
            d[LocKeys.PremiereRepReward] = "+{0} REP";
            d[LocKeys.PremiereDiscoveryTitle] = "New film discovered";
            d[LocKeys.PremiereContinue] = "Continue";
            d[LocKeys.CollectionTitle] = "COLLECTION";
            d[LocKeys.CollectionGlobalProgress] = "{0} / {1}";
            d[LocKeys.CollectionPercent] = "{0}% complete";
            d[LocKeys.CollectionGenreProgress] = "{0} / {1} {2}";
            d[LocKeys.CollectionUnknownTitle] = "???";
            d[LocKeys.CollectionLocked] = "LOCKED";
            d[LocKeys.CollectionDiscovered] = "DISCOVERED";
            d[LocKeys.CollectionLegendaryLock] = "Complete every film in this genre to unlock this legendary film";
            d[LocKeys.CollectionSelectGenre]  = "Choose a genre";
            d[LocKeys.CollectionAllMovies]    = "All films";
            d[LocKeys.CollectionBack]         = "← BACK";
            d[LocKeys.InstLevel]          = "Facility {0}";
            d[LocKeys.InstAvailable]      = "Available at Facility {0}";
            d[LocKeys.InstProgressScreen] = "FACILITIES";
            d[LocKeys.InstUpgrade]        = "EXPAND";
            d[LocKeys.InstNextLevel]      = "Next expansion:";
            d[LocKeys.InstCurrentLevel]   = "Facility {0}";
            d[LocKeys.InstMultiplier]     = "Facility {0} · ×{1:0.00}";
            d[LocKeys.InstLocked]         = "LOCKED";
            d[LocKeys.BonifTitle]     = "BONUSES";
            d[LocKeys.BonifSpeed]     = "Speed";
            d[LocKeys.BonifQuality]   = "Quality";
            d[LocKeys.BonifBoxOffice] = "Box Office";
            d[LocKeys.BonifRep]       = "REP";
            d[LocKeys.BonifXP]        = "XP";
            d[LocKeys.BonifCosts]     = "Costs";
            d[LocKeys.SettingsTitle]      = "SETTINGS";
            d[LocKeys.SettingsLanguage]   = "Language";
            d[LocKeys.SettingsGooglePlay] = "Google Play Games";
            d[LocKeys.SettingsAccount]    = "Account";
            d[LocKeys.SettingsSupport]    = "Support";
            d[LocKeys.SettingsCredits]    = "Credits";
            d[LocKeys.SettingsPrivacy]    = "Privacy Policy";
            d[LocKeys.SettingsRestore]    = "Restore Purchases";
            d[LocKeys.IapRestoreSuccess]  = "Purchases restored";
            d[LocKeys.IapRestoreFailed]   = "Could not restore purchases";
            d[LocKeys.IapPurchaseFailed]  = "Purchase cancelled or failed";
            d[LocKeys.IapUnavailable]     = "Store unavailable";
            d[LocKeys.IapPackActivatedFmt]= "Pack activated: {0}";
            d[LocKeys.SettingsClose]      = "CLOSE";
            d[LocKeys.SettingsMusic]      = "Music";
            d[LocKeys.SettingsSfx]        = "Sound Effects";
            d[LocKeys.SettingsGroupAudio] = "AUDIO";
            d[LocKeys.SettingsGroupLang]  = "LANGUAGE";
            d[LocKeys.SettingsGroupInfo]  = "INFORMATION";
            d[LocKeys.TiendaTitle]      = "SHOP";
            d[LocKeys.TiendaComingSoon] = "Coming soon";

            // Nav tabs
            d[LocKeys.NavStudio]     = "STUDIO";
            d[LocKeys.NavProduction] = "PRODUCTION";
            d[LocKeys.NavAwards]     = "AWARDS";
            d[LocKeys.NavCollection] = "COLLECTION";
            d[LocKeys.NavShop]       = "SHOP";

            // Mejoras sub-tabs
            d[LocKeys.MejorasProduction] = "PRODUCTION";
            d[LocKeys.MejorasStaff]      = "STAFF";
            d[LocKeys.MejorasResearch]   = "RESEARCH";
            d[LocKeys.MejorasMarketing]  = "MARKETING";

            // Awards
            d[LocKeys.AwardsTitle]         = "GOLDEN STARS";
            d[LocKeys.AwardsNextStar]      = "NEXT STAR";
            d[LocKeys.AwardsProgressSect]  = "STUDIO PROGRESS";
            d[LocKeys.AwardsKeepProducing] = "Keep producing...";
            d[LocKeys.AwardsAllEarned]     = "All Golden Stars earned";
            d[LocKeys.AwardsComplete]      = "Collection complete!";
            d[LocKeys.AwardsEmpireMax]     = "Cinematic Empire!";
            d[LocKeys.AwardsReadyClaim]    = "READY TO CLAIM!";
            d[LocKeys.AwardsClaimBtn]      = "✦ CLAIM GOLDEN STAR";
            d[LocKeys.AwardsRepNeeded]     = "REP needed: {0:N0} / {1:N0}";
            d[LocKeys.AwardsHint]          = "Golden Stars boost studio growth.";
            d[LocKeys.AwardsStudioLevel]   = "Studio Lv. {0}";

            // Production slots
            d[LocKeys.ProdSlotFree]   = "FREE SLOT";
            d[LocKeys.ProdSlotLocked] = "SLOT LOCKED";
            d[LocKeys.ProdSlotUnlock] = "Unlock in Upgrades → Facilities";

            // Upgrade cards
            d[LocKeys.UpgradeNoEffect]     = "No effect";
            d[LocKeys.UpgradeLockedStudio] = "Studio Lv.{0}";

            // Contracts panel
            d[LocKeys.ContractActive]        = "ACTIVE CONTRACT";
            d[LocKeys.ContractNoneAvailable] = "No contract available";
            d[LocKeys.ContractClaim]         = "CLAIM";
            d[LocKeys.ContractCompletedPfx]  = "Completed — ";
            d[LocKeys.ContractNoActive]      = "No active contract";
            d[LocKeys.ContractReadyClaim]    = "Ready to claim!";
            d[LocKeys.ContractObjective]     = "OBJECTIVE";

            // Collection screen
            d[LocKeys.CollectionScreenTitle]   = "MY COLLECTION";
            d[LocKeys.CollectionGenreLabel]    = "GENRE";
            d[LocKeys.CollectionSynopsisHdr]   = "SYNOPSIS";
            d[LocKeys.CollectionClose]         = "CLOSE";
            d[LocKeys.CollectionSynopsisNone]  = "No synopsis available.";
            d[LocKeys.CollectionFilmCompleted] = "✔ COMPLETED";
            d[LocKeys.CollectionFilmPending]   = "○ PENDING";

            // GameFeel popups
            d[LocKeys.GameFeelContractComplete] = "CONTRACT COMPLETE";
            d[LocKeys.GameFeelOscarEarned]      = "OSCAR EARNED";
            d[LocKeys.GameFeelCityImproved]     = "FACILITY UPGRADED";
            d[LocKeys.GameFeelUpgradeBought]    = "UPGRADE PURCHASED";
            d[LocKeys.GameFeelContinue]         = "CONTINUE";
            d[LocKeys.GameFeelOscarsSuffix]     = " Oscar(s) · Upgrade your Facility";
            d[LocKeys.GameFeelIncomeGlobal]     = " global income";

            // Studio chrome
            d[LocKeys.StudioName]            = "IDLE FILM STUDIO";
            d[LocKeys.StudioLevelFormat]     = "Level {0}";
            d[LocKeys.StudioXPFormat]        = "{0:0}/{1:0} XP";
            d[LocKeys.StudioNextLevel]       = "Next level · {0:0} XP remaining";
            d[LocKeys.StudioBonusSummaryFmt] = "Income +{0:0}% · REP +{1:0}% · Speed +{2:0}%";

            // TopBar
            d[LocKeys.TopBarQualAbbr]  = "Qual";
            d[LocKeys.TopBarSpeedAbbr] = "Spd";
            d[LocKeys.TopBarLevelAbbr] = "Lv.";

            // Upgrade effects
            d[LocKeys.UpgradeCurrent]       = "Current";
            d[LocKeys.UpgradeNext]          = "Next";
            d[LocKeys.UpgradeEffectQuality] = "+{0:0.#}% Quality";
            d[LocKeys.UpgradeEffectSpeed]   = "+{0:0.#}% Speed";
            d[LocKeys.UpgradeEffectCost]    = "-{0:0.#}% Cost";
            d[LocKeys.UpgradeEffectRep]     = "+{0:0.#}% Reputation";
            d[LocKeys.UpgradeEffectIncome]  = "+{0}/s Income";
            d[LocKeys.UpgradeEffectSlot]    = "+{0} Prod. Slot(s)";
            d[LocKeys.UpgradeEffectCatalog] = "Catalog Lv.{0}";

            // Awards
            d[LocKeys.AwardsStarTitleFmt]  = "Star #{0}";
            d[LocKeys.AwardsRepReadyFmt]   = "Reputation: {0:N0} / {1:N0} REP ✓";
            d[LocKeys.AwardsUnlockHintFmt] = "{0}  ·  {1} to next unlock";

            // City
            d[LocKeys.InstGlobalBonus]    = "GLOBAL BONUS";
            d[LocKeys.InstGlobalBonusFmt] = "×{0:0.00} global income";
            d[LocKeys.InstCurrentUnlocks] = "CURRENT UNLOCKS";
            d[LocKeys.InstMaxCity]        = "You've reached the Cinematic Empire!";

            // Production
            d[LocKeys.ProdProduceBtn] = "PRODUCE";
            d[LocKeys.ProdEmptyState] = "No movies available.";

            // Collection stats
            d[LocKeys.CollStatLvlFmt] = "Lv.{0}";

            // Premiere
            d[LocKeys.PremiereVarietyFmt]   = "Variety +{0:0}%";
            d[LocKeys.PremiereFilmFound]    = "Film discovered";
            d[LocKeys.PremiereContractDone] = "Contract complete";
            d[LocKeys.PremiereContractProg] = "Contract · {0:0}/{1:0}";

            // GameFeel
            d[LocKeys.GameFeelUpgradeFallback] = "Upgrade acquired";

            // Store
            d[LocKeys.StoreSectionDiamonds] = "DIAMONDS";
            d[LocKeys.StoreSectionPacks]    = "› PACKS";
            d[LocKeys.StoreSectionBoosts]   = "› BOOSTS";
            d[LocKeys.StoreSectionPremium]  = "› PREMIUM";
            d[LocKeys.StoreWatchAd]         = "Watch ad";
            d[LocKeys.StoreStarterDesc]     = "Diamonds + Boost + No ads 24h";
            d[LocKeys.StoreUniqueOffer]     = "UNIQUE OFFER";
            d[LocKeys.StoreNoAds]           = "NO ADS";
            d[LocKeys.StoreNoAdsDesc]       = "Remove ads forever";

            // Store products
            d[LocKeys.StoreProdDiamSmall]    = "HANDFUL";
            d[LocKeys.StoreProdDiamMed]      = "BAG";
            d[LocKeys.StoreProdDiamLarge]    = "CHEST";
            d[LocKeys.StoreProdPackDir]      = "DIRECTOR PACK";
            d[LocKeys.StoreProdPackDirDesc]  = "Diamonds + Boost\n+ Rare Movie";
            d[LocKeys.StoreProdPackStar]     = "STAR PACK";
            d[LocKeys.StoreProdPackStarDesc] = "Diamonds + REP\n+ Epic Movie";
            d[LocKeys.StoreProdBoostProd]    = "PRODUCTION x2";
            d[LocKeys.StoreProdBoostInc]     = "INCOME x2";
            d[LocKeys.SettingsComing]        = "SOON";
            // Studio subtabs
            d[LocKeys.StudTabDepts]     = "DEPARTMENTS";
            d[LocKeys.StudTabMejoras]   = "UPGRADES";
            d[LocKeys.StudTabContratos] = "CONTRACTS";

            // Credits
            d[LocKeys.CreditsCreatedBy]  = "Created by";
            d[LocKeys.CreditsVersion]    = "Version {0}";
            d[LocKeys.CreditsPoweredBy]  = "Powered by Unity";
            d[LocKeys.IncomePerSecFmt]   = "+${0:N0}/s";

            // Premiere headline
            d[LocKeys.PremiereHeadline]  = "PREMIERE";

            // City / Facility names
            d[LocKeys.CityNamePfx + "1"] = "Garage";
            d[LocKeys.CityNamePfx + "2"] = "Independent Studio";
            d[LocKeys.CityNamePfx + "3"] = "Local Studio";
            d[LocKeys.CityNamePfx + "4"] = "Regional Studio";
            d[LocKeys.CityNamePfx + "5"] = "Major Studio";
            d[LocKeys.CityNamePfx + "6"] = "Hollywood Boulevard";
            d[LocKeys.CityNamePfx + "7"] = "Grand Studio";
            d[LocKeys.CityNamePfx + "8"] = "Cinematic Empire";

            // Upgrade names
            d[LocKeys.UpgradeNamePfx + "equip_camera_basic"]    = "Basic Camera";
            d[LocKeys.UpgradeNamePfx + "equip_camera_digital"]  = "Advanced Digital Camera";
            d[LocKeys.UpgradeNamePfx + "equip_camera_pro"]      = "Professional Camera";
            d[LocKeys.UpgradeNamePfx + "equip_coloring"]        = "Color Grading Suite";
            d[LocKeys.UpgradeNamePfx + "equip_dolby"]           = "Dolby Atmos Processor";
            d[LocKeys.UpgradeNamePfx + "equip_drone"]           = "Drone System";
            d[LocKeys.UpgradeNamePfx + "equip_editing_soft"]    = "Pro Editing Software";
            d[LocKeys.UpgradeNamePfx + "equip_lighting_kit"]    = "Lighting Kit";
            d[LocKeys.UpgradeNamePfx + "equip_lighting_led"]    = "Cinematic LED System";
            d[LocKeys.UpgradeNamePfx + "equip_mic_pro"]         = "Professional Microphone";
            d[LocKeys.UpgradeNamePfx + "equip_render_farm"]     = "Render Farm";
            d[LocKeys.UpgradeNamePfx + "equip_sound_surround"]  = "Surround Sound System";
            d[LocKeys.UpgradeNamePfx + "equip_steadicam"]       = "Steadicam Pro";
            d[LocKeys.UpgradeNamePfx + "equip_vfx_advanced"]    = "Advanced VFX Suite";
            d[LocKeys.UpgradeNamePfx + "equip_vfx_basic"]       = "Basic VFX Station";
            d[LocKeys.UpgradeNamePfx + "install_editing_room"]  = "Editing Room";
            d[LocKeys.UpgradeNamePfx + "install_exterior"]      = "Exterior Studios";
            d[LocKeys.UpgradeNamePfx + "install_offices"]       = "Production Offices";
            d[LocKeys.UpgradeNamePfx + "install_plato_large"]   = "Large Stage";
            d[LocKeys.UpgradeNamePfx + "install_plato_medium"]  = "Medium Stage";
            d[LocKeys.UpgradeNamePfx + "install_plato_small"]   = "Small Stage";
            d[LocKeys.UpgradeNamePfx + "install_screening"]     = "Private Screening Room";
            d[LocKeys.UpgradeNamePfx + "install_studio_lot"]    = "Complete Studio Lot";
            d[LocKeys.UpgradeNamePfx + "install_vfx_dept"]      = "VFX Department";
            d[LocKeys.UpgradeNamePfx + "mkt_global"]            = "Global Distribution";
            d[LocKeys.UpgradeNamePfx + "mkt_press_kit"]         = "Press Campaign";
            d[LocKeys.UpgradeNamePfx + "mkt_social_media"]      = "Social Media Manager";
            d[LocKeys.UpgradeNamePfx + "mkt_trailer"]           = "Trailer Department";
            d[LocKeys.UpgradeNamePfx + "personal_actors"]       = "Actors";
            d[LocKeys.UpgradeNamePfx + "personal_art"]          = "Art Department";
            d[LocKeys.UpgradeNamePfx + "personal_cinematography"]= "Director of Photography";
            d[LocKeys.UpgradeNamePfx + "personal_composer"]     = "Soundtrack Composer";
            d[LocKeys.UpgradeNamePfx + "personal_costume"]      = "Costume";
            d[LocKeys.UpgradeNamePfx + "personal_director"]     = "Director";
            d[LocKeys.UpgradeNamePfx + "personal_editor"]       = "Editor";
            d[LocKeys.UpgradeNamePfx + "personal_grip"]         = "Grip (Camera Machinery)";
            d[LocKeys.UpgradeNamePfx + "personal_lighting"]     = "Gaffer (Technical Lighting)";
            d[LocKeys.UpgradeNamePfx + "personal_makeup"]       = "Makeup & Characterization";
            d[LocKeys.UpgradeNamePfx + "personal_producer"]     = "Executive Producer";
            d[LocKeys.UpgradeNamePfx + "personal_publicist"]    = "Publicist";
            d[LocKeys.UpgradeNamePfx + "personal_sound"]        = "Sound Technician";

            // Contract titles
            d[LocKeys.ContractTitlePfx + "c_three_movies"]      = "Short Film Festival";
            d[LocKeys.ContractTitlePfx + "c_first_reputation"]  = "First Fans";
            d[LocKeys.ContractTitlePfx + "c_first_movie"]       = "First Shoot";
            d[LocKeys.ContractTitlePfx + "c_action_fan"]        = "Action Movie Fan";
            d[LocKeys.ContractTitlePfx + "c_romance_story"]     = "Love Story";
            d[LocKeys.ContractTitlePfx + "c_first_upgrade"]     = "Invest in the Studio";
            d[LocKeys.ContractTitlePfx + "c_fantasy_realm"]     = "Fantasy Realms";
            d[LocKeys.ContractTitlePfx + "c_thriller_night"]    = "Thriller Night";
            d[LocKeys.ContractTitlePfx + "c_documentary_truth"] = "The Truth of Cinema";
            d[LocKeys.ContractTitlePfx + "c_drama_fan"]         = "Drama Fan";
            d[LocKeys.ContractTitlePfx + "c_animation_studio"]  = "Animation Studio";
            d[LocKeys.ContractTitlePfx + "c_rep_50"]            = "Rising Star";
            d[LocKeys.ContractTitlePfx + "c_horror_night"]      = "Horror Night";
            d[LocKeys.ContractTitlePfx + "c_speed_run"]         = "Express Production";
            d[LocKeys.ContractTitlePfx + "c_studio_level5"]     = "Established Studio";
            d[LocKeys.ContractTitlePfx + "c_rep_200"]           = "Industry Reference";
            d[LocKeys.ContractTitlePfx + "c_comedy_king"]       = "Comedy King";
            d[LocKeys.ContractTitlePfx + "c_studio_level10"]    = "Established Production Co.";
            d[LocKeys.ContractTitlePfx + "c_ten_movies"]        = "Cinema Marathon";
            d[LocKeys.ContractTitlePfx + "c_scifi_pioneer"]     = "Sci-Fi Pioneer";
            d[LocKeys.ContractTitlePfx + "c_studio_level15"]    = "Major Studio";
            d[LocKeys.ContractTitlePfx + "c_rep_1000"]          = "Cinema Legend";
            d[LocKeys.ContractTitlePfx + "c_all_genres"]        = "Cinema Without Borders";
            d[LocKeys.ContractTitlePfx + "c_big_spender"]       = "Big Investment";
            d[LocKeys.ContractTitlePfx + "c_studio_level20"]    = "International Studio";
            d[LocKeys.ContractTitlePfx + "c_rep_5000"]          = "Global Icon";
            d[LocKeys.ContractTitlePfx + "c_blockbuster"]       = "The Big Blockbuster";

            // Oscar / installation progress
            d[LocKeys.InstOscarProgress]  = "{0} / {1} OSC";
            d[LocKeys.InstOscarMax]       = "{0} OSC · MAX";
            d[LocKeys.InstUnlockGlobal]   = "×{0:0.00} global income";
            d[LocKeys.InstUnlockDept]     = "Dept: {0}";
            d[LocKeys.InstTierPfx + "1"]  = "Tier 1–2 · Basic films";
            d[LocKeys.InstTierPfx + "2"]  = "Sequels and remakes";
            d[LocKeys.InstTierPfx + "3"]  = "Professional production";
            d[LocKeys.InstTierPfx + "4"]  = "Advanced films";
            d[LocKeys.InstTierPfx + "5"]  = "Campaign & distribution";
            d[LocKeys.InstTierPfx + "6"]  = "VFX & awards";
            d[LocKeys.InstTierPfx + "7"]  = "Streaming & universes";
            d[LocKeys.InstTierPfx + "8"]  = "Complete empire";

            // City lock labels
            d[LocKeys.CityLockLabel]      = "City {0} required";

            // Offer card badges
            d[LocKeys.OfferBadgeNew]      = "[NEW]";
            d[LocKeys.OfferBadgeSaga]     = "[SAGA]";
            d[LocKeys.OfferBadgeContract] = "[CONTRACT]";

            // Saga labels
            d[LocKeys.SagaCompleted]   = "{0}/{1} COMPLETED";
            d[LocKeys.SagaBlockReason] = "Complete entry {0} of the saga first";

            // Ad reward feedback
            d[LocKeys.AdLimitReached]        = "Daily limit reached";
            d[LocKeys.FreeDiamondsAwarded]   = "+{0} diamonds added";
            d[LocKeys.BoostActivated]        = "Boost activated! ×2 for 10 min";
            d[LocKeys.BoostAlreadyActive]    = "You already have this boost active";
            d[LocKeys.InvestorAwarded]       = "+${0:N0} from investor";
            d[LocKeys.InvestorNoIncome]      = "Current income is 0. Produce more films first.";
            d[LocKeys.StoreSectionInvestor]  = "Investor";
            d[LocKeys.StoreSectionFreeRewards] = "Daily Rewards";
            d[LocKeys.StoreProdBoostRep]     = "REP Boost";
            d[LocKeys.StoreProdBoostXP]      = "XP Boost";
            d[LocKeys.StoreProdFreeDiam]     = "Free";
            d[LocKeys.StoreProdInvestor]     = "Investor";
            d[LocKeys.StoreProdOfflinePremium] = "Remote Producer";
            d[LocKeys.StoreBoostTimerFmt]    = "{0}m {1}s";
            d[LocKeys.StoreBoostActive]      = "ACTIVE ·";
            d[LocKeys.StorePackStarter]      = "Starter Pack";
            d[LocKeys.StorePackSupporter]    = "Supporter";
            d[LocKeys.StorePackProducer]     = "Producer";
            d[LocKeys.StorePackExecutive]    = "Executive Producer";
            d[LocKeys.StorePackAlreadyOwned] = "Already owned";
            d[LocKeys.StoreSimBuy]           = "[SIMULATE PURCHASE]";
            d[LocKeys.OfflinePremiumName]        = "Remote Producer";
            d[LocKeys.OfflinePremiumDesc]        = "Extends offline cap to 2 hours";
            d[LocKeys.OfflinePremiumOwned]       = "Unlocked";
            d[LocKeys.OfflinePremiumCostPending] = "Price pending";
            d[LocKeys.PackSupporterDesc]  = "Thank you for supporting Film Producer Tycoon.";
            d[LocKeys.PackProducerDesc]   = "Boost your career as a producer with a major diamond reserve.";
            d[LocKeys.PackExecutiveDesc]  = "The ultimate edition for true film moguls.";
            d[LocKeys.InvestorCooldownFmt] = "CD · {0}m {1}s";
            d[LocKeys.InvestorReady]       = "5 min income";
            d[LocKeys.InvestorReadyLabel]  = "5 min. income";
            d[LocKeys.UxNotEnoughDiamonds] = "Not enough diamonds.";
            d[LocKeys.UxGoToStore]         = "Go to store";
            d[LocKeys.UxCancelAction]      = "Cancel";
            d[LocKeys.UxNoSlotsAvailable]  = "No production slots available.";
            d[LocKeys.ContractCancelWithAd]  = "Cancel contract";
            d[LocKeys.ContractCancelConfirm] = "Cancel the active contract?";
            d[LocKeys.ContractCancelDone]    = "Contract cancelled. New options generated.";
            return d;
        }

        static Dictionary<string, string> Fr(Dictionary<string, string> es)
        {
            var d = new Dictionary<string, string>(es);
            d[LocKeys.DeptLevelFormat] = "NIVEAU {0}";
            d[LocKeys.DeptUpgrade] = "AMÉLIORER";
            d[LocKeys.DeptMax] = "MAX";
            d[LocKeys.DeptLocked] = "Verrouillé";
            d[LocKeys.DeptAvailableCity] = "Disponible en Ville {0}";
            d[LocKeys.DeptBonusSummary] = "{0}\n+{1}/niv  Total +{2}";
            d[LocKeys.DeptCategoryQuality] = "Qualité";
            d[LocKeys.DeptCategorySpeed] = "Vitesse";
            d[LocKeys.DeptCategoryReduction] = "Réduction";
            d[LocKeys.DeptNameActors] = "Acteurs";
            d[LocKeys.DeptNameSound] = "Son";
            d[LocKeys.DeptNameCinematography] = "Photographie";
            d[LocKeys.DeptNameMakeup] = "Maquillage";
            d[LocKeys.DeptNameCostume] = "Costumes";
            d[LocKeys.DeptNameArt] = "Décors";
            d[LocKeys.DeptNameLighting] = "Éclairage";
            d[LocKeys.DeptNameProducer] = "Producteur";
            d[LocKeys.ProdTabTitle] = "PRODUCTION";
            d[LocKeys.ProdNewProduction] = "ACTUALISER LES OFFRES";
            d[LocKeys.ProdActiveSection] = "EN TOURNAGE";
            d[LocKeys.ProdNoActive] = "Aucune production active";
            d[LocKeys.ProdStartMovie] = "Lancez un film";
            d[LocKeys.ProdStatusProducing] = "En production";
            d[LocKeys.ProdStatusComplete] = "Production terminée";
            d[LocKeys.ProdDiscoverButton] = "AVANT-PREMIÈRE";
            d[LocKeys.ProdSelect] = "SÉLECTIONNER";
            d[LocKeys.ProdCatalogComplete] = "Catalogue terminé";
            d[LocKeys.ProdNoOffer] = "Aucune offre disponible";
            d[LocKeys.ProdBudgetTitle] = "BUDGET";
            d[LocKeys.ProdBudgetSubtitle] = "Choisissez comment produire ce film";
            d[LocKeys.ProdBudgetCheap] = "BAS";
            d[LocKeys.ProdBudgetStandard] = "NORMAL";
            d[LocKeys.ProdBudgetPremium] = "PREMIUM";
            d[LocKeys.ProdBudgetCancel] = "ANNULER";
            d[LocKeys.ProdBudgetStatBlock] = "Durée: {0}\nArgent: {1}\nRéputation: {2}";
            d[LocKeys.ProdOfferMoney] = "Argent: {0}";
            d[LocKeys.ProdOfferRep] = "Rép.: +{0}";
            d[LocKeys.RefreshChoosePayment] = "Choisissez le paiement";
            d[LocKeys.RefreshWatchAd] = "VOIR UNE PUB";
            d[LocKeys.RefreshSpendDiamonds] = "DÉPENSER DES DIAMANTS";
            d[LocKeys.ContractRefresh] = "ACTUALISER LES CONTRATS";
            d[LocKeys.ProdRarityCommon] = "Commune";
            d[LocKeys.ProdRarityRare] = "Rare";
            d[LocKeys.ProdRarityEpic] = "Épique";
            d[LocKeys.ProdRarityLegendary] = "Légendaire";
            d[LocKeys.GenreAction] = "ACTION";
            d[LocKeys.GenreDrama] = "DRAME";
            d[LocKeys.GenreHorror] = "HORREUR";
            d[LocKeys.GenreComedy] = "COMÉDIE";
            d[LocKeys.GenreRomance] = "ROMANCE";
            d[LocKeys.GenreSciFi] = "SCI-FI";
            d[LocKeys.GenreFantasy] = "FANTASY";
            d[LocKeys.GenreThriller] = "THRILLER";
            d[LocKeys.GenreAnimation] = "ANIMATION";
            d[LocKeys.GenreDocumentary] = "DOCUMENTAL";
            d[LocKeys.FtueWelcomeBody] = "Tout grand studio a commencé par un seul film.";
            d[LocKeys.FtueProductionTitle] = "Première production";
            d[LocKeys.FtueProductionBody] = "Nous avons besoin de notre première production.";
            d[LocKeys.FtueStudioTitle] = "Améliorez le studio";
            d[LocKeys.FtueStudioBody] = "Améliorez vos départements pour de meilleurs films.";
            d[LocKeys.FtueFirstMovieTitle] = "Votre premier film est prêt.";
            d[LocKeys.FtueCompleteBody] = "Améliorez le studio et confirmez pour continuer.";
            d[LocKeys.FtueMoneyEarned] = "Argent gagné : {0}";
            d[LocKeys.FtueRepEarned] = "Réputation : +{0}";
            d[LocKeys.FtueMovieDiscovered] = "Film découvert : {0}";
            d[LocKeys.FtueContinue] = "Continuer";
            d[LocKeys.FtueSkip] = "Passer";
            d[LocKeys.PremiereCompleted] = "Avant-première terminée";
            d[LocKeys.PremiereRewardsTitle] = "Récompenses";
            d[LocKeys.PremiereDiscoveryTitle] = "Nouveau film découvert";
            d[LocKeys.PremiereContinue] = "Continuer";
            d[LocKeys.CollectionTitle] = "COLLECTION";
            d[LocKeys.CollectionGlobalProgress] = "{0} / {1}";
            d[LocKeys.CollectionPercent] = "{0} % complété";
            d[LocKeys.CollectionGenreProgress] = "{0} / {1} {2}";
            d[LocKeys.CollectionUnknownTitle] = "???";
            d[LocKeys.CollectionLocked] = "VERROUILLÉE";
            d[LocKeys.CollectionDiscovered] = "DÉCOUVERTE";
            d[LocKeys.CollectionLegendaryLock] = "Terminez tous les films de ce genre pour débloquer ce film légendaire";

            // Settings (fr was missing these)
            d[LocKeys.SettingsTitle]      = "PARAMÈTRES";
            d[LocKeys.SettingsLanguage]   = "Langue";
            d[LocKeys.SettingsGooglePlay] = "Google Play Games";
            d[LocKeys.SettingsAccount]    = "Compte";
            d[LocKeys.SettingsSupport]    = "Assistance";
            d[LocKeys.SettingsCredits]    = "Crédits";
            d[LocKeys.SettingsPrivacy]    = "Politique de confidentialité";
            d[LocKeys.SettingsRestore]    = "Restaurer les achats";
            d[LocKeys.IapRestoreSuccess]  = "Achats restaurés";
            d[LocKeys.IapRestoreFailed]   = "Impossible de restaurer les achats";
            d[LocKeys.IapPurchaseFailed]  = "Achat annulé ou échoué";
            d[LocKeys.IapUnavailable]     = "Boutique indisponible";
            d[LocKeys.IapPackActivatedFmt]= "Pack activé : {0}";
            d[LocKeys.SettingsClose]      = "FERMER";
            d[LocKeys.SettingsMusic]      = "Musique";
            d[LocKeys.SettingsSfx]        = "Effets sonores";
            d[LocKeys.SettingsGroupAudio] = "AUDIO";
            d[LocKeys.SettingsGroupLang]  = "LANGUE";
            d[LocKeys.SettingsGroupInfo]  = "INFORMATIONS";
            d[LocKeys.TiendaTitle]        = "BOUTIQUE";
            d[LocKeys.TiendaComingSoon]   = "Bientôt disponible";

            // Inst / Bonif (fr was missing these)
            d[LocKeys.InstLevel]          = "Installation {0}";
            d[LocKeys.InstAvailable]      = "Disponible à l'Installation {0}";
            d[LocKeys.InstProgressScreen] = "INSTALLATIONS";
            d[LocKeys.InstUpgrade]        = "AGRANDIR";
            d[LocKeys.InstNextLevel]      = "Prochaine extension :";
            d[LocKeys.InstCurrentLevel]   = "Installation {0}";
            d[LocKeys.InstMultiplier]     = "Installation {0} · ×{1:0.00}";
            d[LocKeys.InstLocked]         = "VERROUILLÉ";
            d[LocKeys.BonifTitle]     = "BONUS";
            d[LocKeys.BonifSpeed]     = "Vitesse";
            d[LocKeys.BonifQuality]   = "Qualité";
            d[LocKeys.BonifBoxOffice] = "Box-Office";
            d[LocKeys.BonifRep]       = "RÉP";
            d[LocKeys.BonifXP]        = "XP";
            d[LocKeys.BonifCosts]     = "Coûts";

            // Nav tabs
            d[LocKeys.NavStudio]     = "STUDIO";
            d[LocKeys.NavProduction] = "PRODUCTION";
            d[LocKeys.NavAwards]     = "PRIX";
            d[LocKeys.NavCollection] = "COLLECTION";
            d[LocKeys.NavShop]       = "BOUTIQUE";

            // Mejoras sub-tabs
            d[LocKeys.MejorasProduction] = "PRODUCTION";
            d[LocKeys.MejorasStaff]      = "ÉQUIPE";
            d[LocKeys.MejorasResearch]   = "RECHERCHE";
            d[LocKeys.MejorasMarketing]  = "MARKETING";

            // Awards
            d[LocKeys.AwardsTitle]         = "ÉTOILES D'OR";
            d[LocKeys.AwardsNextStar]      = "ÉTOILE SUIVANTE";
            d[LocKeys.AwardsProgressSect]  = "PROGRÈS DU STUDIO";
            d[LocKeys.AwardsKeepProducing] = "Continuez à produire...";
            d[LocKeys.AwardsAllEarned]     = "Toutes les Étoiles d'Or obtenues";
            d[LocKeys.AwardsComplete]      = "Collection complète !";
            d[LocKeys.AwardsEmpireMax]     = "Empire Cinématographique !";
            d[LocKeys.AwardsReadyClaim]    = "PRÊTE À RÉCLAMER !";
            d[LocKeys.AwardsClaimBtn]      = "✦ OBTENIR ÉTOILE D'OR";
            d[LocKeys.AwardsRepNeeded]     = "RÉP. nécessaire : {0:N0} / {1:N0}";
            d[LocKeys.AwardsHint]          = "Les Étoiles d'Or stimulent la croissance du studio.";
            d[LocKeys.AwardsStudioLevel]   = "Studio Niv. {0}";

            // Production slots
            d[LocKeys.ProdSlotFree]   = "SLOT LIBRE";
            d[LocKeys.ProdSlotLocked] = "SLOT VERROUILLÉ";
            d[LocKeys.ProdSlotUnlock] = "Débloquez dans Améliorations → Installations";

            // Upgrade cards
            d[LocKeys.UpgradeNoEffect]     = "Aucun effet";
            d[LocKeys.UpgradeLockedStudio] = "Studio Niv.{0}";

            // Contracts
            d[LocKeys.ContractActive]        = "CONTRAT ACTIF";
            d[LocKeys.ContractNoneAvailable] = "Aucun contrat disponible";
            d[LocKeys.ContractClaim]         = "RÉCLAMER";
            d[LocKeys.ContractCompletedPfx]  = "Terminé — ";
            d[LocKeys.ContractNoActive]      = "Aucun contrat actif";
            d[LocKeys.ContractReadyClaim]    = "Prêt à réclamer !";
            d[LocKeys.ContractObjective]     = "OBJECTIF";

            // Collection screen
            d[LocKeys.CollectionScreenTitle]   = "MA COLLECTION";
            d[LocKeys.CollectionGenreLabel]    = "GENRE";
            d[LocKeys.CollectionSynopsisHdr]   = "SYNOPSIS";
            d[LocKeys.CollectionClose]         = "FERMER";
            d[LocKeys.CollectionSynopsisNone]  = "Aucun synopsis disponible.";
            d[LocKeys.CollectionFilmCompleted] = "✔ TERMINÉ";
            d[LocKeys.CollectionFilmPending]   = "○ EN COURS";
            d[LocKeys.CollectionSelectGenre]   = "Choisir un genre";
            d[LocKeys.CollectionAllMovies]     = "Tous les films";
            d[LocKeys.CollectionBack]          = "← RETOUR";

            // GameFeel
            d[LocKeys.GameFeelContractComplete] = "CONTRAT TERMINÉ";
            d[LocKeys.GameFeelOscarEarned]      = "OSCAR OBTENU";
            d[LocKeys.GameFeelCityImproved]     = "INSTALLATION AMÉLIORÉE";
            d[LocKeys.GameFeelUpgradeBought]    = "AMÉLIORATION ACHETÉE";
            d[LocKeys.GameFeelContinue]         = "CONTINUER";
            d[LocKeys.GameFeelOscarsSuffix]     = " Oscar(s) · Améliorez votre Installation";
            d[LocKeys.GameFeelIncomeGlobal]     = " revenu global";

            // Studio chrome
            d[LocKeys.StudioName]            = "IDLE FILM STUDIO";
            d[LocKeys.StudioLevelFormat]     = "Niveau {0}";
            d[LocKeys.StudioXPFormat]        = "{0:0}/{1:0} XP";
            d[LocKeys.StudioNextLevel]       = "Prochain niveau · {0:0} XP restants";
            d[LocKeys.StudioBonusSummaryFmt] = "Revenus +{0:0}% · RÉP +{1:0}% · Vit +{2:0}%";

            // TopBar
            d[LocKeys.TopBarQualAbbr]  = "Qual";
            d[LocKeys.TopBarSpeedAbbr] = "Vit";

            // Upgrade effects
            d[LocKeys.UpgradeCurrent]       = "Actuel";
            d[LocKeys.UpgradeNext]          = "Suivant";
            d[LocKeys.UpgradeEffectQuality] = "+{0:0.#}% Qualité";
            d[LocKeys.UpgradeEffectSpeed]   = "+{0:0.#}% Vitesse";
            d[LocKeys.UpgradeEffectCost]    = "-{0:0.#}% Coût";
            d[LocKeys.UpgradeEffectRep]     = "+{0:0.#}% Réputation";
            d[LocKeys.UpgradeEffectIncome]  = "+{0}/s Revenus";
            d[LocKeys.UpgradeEffectSlot]    = "+{0} Slot(s) Prod.";
            d[LocKeys.UpgradeEffectCatalog] = "Catalogue Nv.{0}";

            // Awards
            d[LocKeys.AwardsStarTitleFmt]  = "Étoile #{0}";
            d[LocKeys.AwardsRepReadyFmt]   = "Réputation : {0:N0} / {1:N0} REP ✓";
            d[LocKeys.AwardsUnlockHintFmt] = "{0}  ·  {1} pour débloquer la suivante";

            // City
            d[LocKeys.InstGlobalBonus]    = "BONUS GLOBAL";
            d[LocKeys.InstGlobalBonusFmt] = "×{0:0.00} revenus globaux";
            d[LocKeys.InstCurrentUnlocks] = "DÉBLOQUÉS ACTUELS";
            d[LocKeys.InstMaxCity]        = "Vous avez atteint l'Empire Cinématographique !";

            // Production
            d[LocKeys.ProdProduceBtn] = "PRODUIRE";
            d[LocKeys.ProdEmptyState] = "Aucun film disponible.";

            // Collection stats
            d[LocKeys.CollStatLvlFmt] = "Nv.{0}";

            // Premiere
            d[LocKeys.PremiereVarietyFmt]   = "Variété +{0:0}%";
            d[LocKeys.PremiereFilmFound]    = "Film découvert";
            d[LocKeys.PremiereContractDone] = "Contrat terminé";
            d[LocKeys.PremiereContractProg] = "Contrat · {0:0}/{1:0}";

            // GameFeel
            d[LocKeys.GameFeelUpgradeFallback] = "Amélioration acquise";

            // Store
            d[LocKeys.StoreSectionDiamonds] = "DIAMANTS";
            d[LocKeys.StoreSectionPacks]    = "› PACKS";
            d[LocKeys.StoreSectionBoosts]   = "› BOOSTS";
            d[LocKeys.StoreSectionPremium]  = "› PREMIUM";
            d[LocKeys.StoreWatchAd]         = "Voir la pub";
            d[LocKeys.StoreStarterDesc]     = "Diamants + Boost + Sans pub 24h";
            d[LocKeys.StoreUniqueOffer]     = "OFFRE UNIQUE";
            d[LocKeys.StoreNoAds]           = "SANS PUB";
            d[LocKeys.StoreNoAdsDesc]       = "Supprimez les publicités pour toujours";

            // Store products
            d[LocKeys.StoreProdDiamSmall]    = "POIGNÉE";
            d[LocKeys.StoreProdDiamMed]      = "SAC";
            d[LocKeys.StoreProdDiamLarge]    = "COFFRE";
            d[LocKeys.StoreProdPackDir]      = "PACK DIRECTEUR";
            d[LocKeys.StoreProdPackDirDesc]  = "Diamants + Boost\n+ Film rare";
            d[LocKeys.StoreProdPackStar]     = "PACK ÉTOILE";
            d[LocKeys.StoreProdPackStarDesc] = "Diamants + REP\n+ Film épique";
            d[LocKeys.StoreProdBoostProd]    = "PRODUCTION x2";
            d[LocKeys.StoreProdBoostInc]     = "REVENUS x2";
            d[LocKeys.SettingsComing]        = "BIENTÔT";
            // Studio subtabs
            d[LocKeys.StudTabDepts]     = "DÉPARTEMENTS";
            d[LocKeys.StudTabMejoras]   = "AMÉLIORATIONS";
            d[LocKeys.StudTabContratos] = "CONTRATS";

            // Credits
            d[LocKeys.CreditsCreatedBy]  = "Créé par";
            d[LocKeys.CreditsVersion]    = "Version {0}";
            d[LocKeys.CreditsPoweredBy]  = "Propulsé par Unity";
            d[LocKeys.IncomePerSecFmt]   = "+${0:N0}/s";

            // Premiere headline
            d[LocKeys.PremiereHeadline]  = "AVANT-PREMIÈRE";

            // City / Facility names
            d[LocKeys.CityNamePfx + "1"] = "Garage";
            d[LocKeys.CityNamePfx + "2"] = "Studio Indépendant";
            d[LocKeys.CityNamePfx + "3"] = "Studio Local";
            d[LocKeys.CityNamePfx + "4"] = "Studio Régional";
            d[LocKeys.CityNamePfx + "5"] = "Grand Studio";
            d[LocKeys.CityNamePfx + "6"] = "Hollywood Boulevard";
            d[LocKeys.CityNamePfx + "7"] = "Studio Majeur";
            d[LocKeys.CityNamePfx + "8"] = "Empire Cinématographique";

            // Upgrade names
            d[LocKeys.UpgradeNamePfx + "equip_camera_basic"]    = "Caméra Basique";
            d[LocKeys.UpgradeNamePfx + "equip_camera_digital"]  = "Caméra Numérique Avancée";
            d[LocKeys.UpgradeNamePfx + "equip_camera_pro"]      = "Caméra Professionnelle";
            d[LocKeys.UpgradeNamePfx + "equip_coloring"]        = "Suite d'Étalonnage";
            d[LocKeys.UpgradeNamePfx + "equip_dolby"]           = "Processeur Dolby Atmos";
            d[LocKeys.UpgradeNamePfx + "equip_drone"]           = "Système de Drones";
            d[LocKeys.UpgradeNamePfx + "equip_editing_soft"]    = "Logiciel de Montage Pro";
            d[LocKeys.UpgradeNamePfx + "equip_lighting_kit"]    = "Kit d'Éclairage";
            d[LocKeys.UpgradeNamePfx + "equip_lighting_led"]    = "Système LED Cinématique";
            d[LocKeys.UpgradeNamePfx + "equip_mic_pro"]         = "Microphone Professionnel";
            d[LocKeys.UpgradeNamePfx + "equip_render_farm"]     = "Ferme de Rendu";
            d[LocKeys.UpgradeNamePfx + "equip_sound_surround"]  = "Système Surround";
            d[LocKeys.UpgradeNamePfx + "equip_steadicam"]       = "Steadicam Pro";
            d[LocKeys.UpgradeNamePfx + "equip_vfx_advanced"]    = "Suite VFX Avancée";
            d[LocKeys.UpgradeNamePfx + "equip_vfx_basic"]       = "Station VFX Basique";
            d[LocKeys.UpgradeNamePfx + "install_editing_room"]  = "Salle de Montage";
            d[LocKeys.UpgradeNamePfx + "install_exterior"]      = "Studios Extérieurs";
            d[LocKeys.UpgradeNamePfx + "install_offices"]       = "Bureaux de Production";
            d[LocKeys.UpgradeNamePfx + "install_plato_large"]   = "Grand Plateau";
            d[LocKeys.UpgradeNamePfx + "install_plato_medium"]  = "Plateau Moyen";
            d[LocKeys.UpgradeNamePfx + "install_plato_small"]   = "Petit Plateau";
            d[LocKeys.UpgradeNamePfx + "install_screening"]     = "Salle de Projection Privée";
            d[LocKeys.UpgradeNamePfx + "install_studio_lot"]    = "Studio Lot Complet";
            d[LocKeys.UpgradeNamePfx + "install_vfx_dept"]      = "Département d'Effets Visuels";
            d[LocKeys.UpgradeNamePfx + "mkt_global"]            = "Distribution Mondiale";
            d[LocKeys.UpgradeNamePfx + "mkt_press_kit"]         = "Campagne de Presse";
            d[LocKeys.UpgradeNamePfx + "mkt_social_media"]      = "Responsable Réseaux Sociaux";
            d[LocKeys.UpgradeNamePfx + "mkt_trailer"]           = "Département Bandes-Annonces";
            d[LocKeys.UpgradeNamePfx + "personal_actors"]       = "Acteurs";
            d[LocKeys.UpgradeNamePfx + "personal_art"]          = "Département Artistique";
            d[LocKeys.UpgradeNamePfx + "personal_cinematography"]= "Directeur de la Photographie";
            d[LocKeys.UpgradeNamePfx + "personal_composer"]     = "Compositeur";
            d[LocKeys.UpgradeNamePfx + "personal_costume"]      = "Costumes";
            d[LocKeys.UpgradeNamePfx + "personal_director"]     = "Réalisateur";
            d[LocKeys.UpgradeNamePfx + "personal_editor"]       = "Monteur";
            d[LocKeys.UpgradeNamePfx + "personal_grip"]         = "Machiniste (Grip)";
            d[LocKeys.UpgradeNamePfx + "personal_lighting"]     = "Chef Électricien";
            d[LocKeys.UpgradeNamePfx + "personal_makeup"]       = "Maquillage et Caractérisation";
            d[LocKeys.UpgradeNamePfx + "personal_producer"]     = "Producteur Exécutif";
            d[LocKeys.UpgradeNamePfx + "personal_publicist"]    = "Attaché de Presse";
            d[LocKeys.UpgradeNamePfx + "personal_sound"]        = "Technicien Son";

            // Contract titles
            d[LocKeys.ContractTitlePfx + "c_three_movies"]      = "Festival de Courts-métrages";
            d[LocKeys.ContractTitlePfx + "c_first_reputation"]  = "Premiers Fans";
            d[LocKeys.ContractTitlePfx + "c_first_movie"]       = "Premier Tournage";
            d[LocKeys.ContractTitlePfx + "c_action_fan"]        = "Fan de Films d'Action";
            d[LocKeys.ContractTitlePfx + "c_romance_story"]     = "Histoire d'Amour";
            d[LocKeys.ContractTitlePfx + "c_first_upgrade"]     = "Investir dans le Studio";
            d[LocKeys.ContractTitlePfx + "c_fantasy_realm"]     = "Royaumes de Fantaisie";
            d[LocKeys.ContractTitlePfx + "c_thriller_night"]    = "Nuit de Suspense";
            d[LocKeys.ContractTitlePfx + "c_documentary_truth"] = "La Vérité du Cinéma";
            d[LocKeys.ContractTitlePfx + "c_drama_fan"]         = "Fan de Drame";
            d[LocKeys.ContractTitlePfx + "c_animation_studio"]  = "Studio d'Animation";
            d[LocKeys.ContractTitlePfx + "c_rep_50"]            = "Étoile Montante";
            d[LocKeys.ContractTitlePfx + "c_horror_night"]      = "Nuit d'Horreur";
            d[LocKeys.ContractTitlePfx + "c_speed_run"]         = "Production Express";
            d[LocKeys.ContractTitlePfx + "c_studio_level5"]     = "Studio Établi";
            d[LocKeys.ContractTitlePfx + "c_rep_200"]           = "Référence du Secteur";
            d[LocKeys.ContractTitlePfx + "c_comedy_king"]       = "Roi de la Comédie";
            d[LocKeys.ContractTitlePfx + "c_studio_level10"]    = "Sté de Production Établie";
            d[LocKeys.ContractTitlePfx + "c_ten_movies"]        = "Marathon du Cinéma";
            d[LocKeys.ContractTitlePfx + "c_scifi_pioneer"]     = "Pionnier de la SF";
            d[LocKeys.ContractTitlePfx + "c_studio_level15"]    = "Grand Studio";
            d[LocKeys.ContractTitlePfx + "c_rep_1000"]          = "Légende du Cinéma";
            d[LocKeys.ContractTitlePfx + "c_all_genres"]        = "Cinéma Sans Frontières";
            d[LocKeys.ContractTitlePfx + "c_big_spender"]       = "Grand Investissement";
            d[LocKeys.ContractTitlePfx + "c_studio_level20"]    = "Studio International";
            d[LocKeys.ContractTitlePfx + "c_rep_5000"]          = "Icône Mondiale";
            d[LocKeys.ContractTitlePfx + "c_blockbuster"]       = "Le Grand Blockbuster";

            // Oscar / installation progress
            d[LocKeys.InstOscarProgress]  = "{0} / {1} OSC";
            d[LocKeys.InstOscarMax]       = "{0} OSC · MAX";
            d[LocKeys.InstUnlockGlobal]   = "×{0:0.00} revenu global";
            d[LocKeys.InstUnlockDept]     = "Dépt : {0}";
            d[LocKeys.InstTierPfx + "1"]  = "Tier 1–2 · Films basiques";
            d[LocKeys.InstTierPfx + "2"]  = "Suites et remakes";
            d[LocKeys.InstTierPfx + "3"]  = "Production professionnelle";
            d[LocKeys.InstTierPfx + "4"]  = "Films avancés";
            d[LocKeys.InstTierPfx + "5"]  = "Campagne & distribution";
            d[LocKeys.InstTierPfx + "6"]  = "VFX & récompenses";
            d[LocKeys.InstTierPfx + "7"]  = "Streaming & univers";
            d[LocKeys.InstTierPfx + "8"]  = "Empire complet";

            // City lock labels
            d[LocKeys.CityLockLabel]      = "Ville {0} requise";

            // Offer card badges
            d[LocKeys.OfferBadgeNew]      = "[NOUVEAU]";
            d[LocKeys.OfferBadgeSaga]     = "[SAGA]";
            d[LocKeys.OfferBadgeContract] = "[CONTRAT]";

            // Saga labels
            d[LocKeys.SagaCompleted]   = "{0}/{1} TERMINÉ";
            d[LocKeys.SagaBlockReason] = "Terminez l'entrée {0} de la saga d'abord";

            // Ad reward feedback
            d[LocKeys.AdLimitReached]        = "Limite quotidienne atteinte";
            d[LocKeys.FreeDiamondsAwarded]   = "+{0} diamants ajoutés";
            d[LocKeys.BoostActivated]        = "Boost activé ! ×2 pendant 10 min";
            d[LocKeys.BoostAlreadyActive]    = "Ce boost est déjà actif";
            d[LocKeys.InvestorAwarded]       = "+{0:N0} $ de l'investisseur";
            d[LocKeys.InvestorNoIncome]      = "Revenu actuel nul. Produisez plus de films.";
            d[LocKeys.StoreSectionInvestor]  = "Investisseur";
            d[LocKeys.StoreSectionFreeRewards] = "Récompenses Quotidiennes";
            d[LocKeys.StoreProdBoostRep]     = "Boost REP";
            d[LocKeys.StoreProdBoostXP]      = "Boost XP";
            d[LocKeys.StoreProdFreeDiam]     = "Gratuit";
            d[LocKeys.StoreProdInvestor]     = "Investisseur";
            d[LocKeys.StoreProdOfflinePremium] = "Producteur à distance";
            d[LocKeys.StoreBoostTimerFmt]    = "{0}m {1}s";
            d[LocKeys.StoreBoostActive]      = "ACTIF ·";
            d[LocKeys.StorePackStarter]      = "Pack Débutant";
            d[LocKeys.StorePackSupporter]    = "Supporter";
            d[LocKeys.StorePackProducer]     = "Producteur";
            d[LocKeys.StorePackExecutive]    = "Producteur Exécutif";
            d[LocKeys.StorePackAlreadyOwned] = "Déjà acheté";
            d[LocKeys.StoreSimBuy]           = "[SIMULER ACHAT]";
            d[LocKeys.OfflinePremiumName]        = "Producteur à distance";
            d[LocKeys.OfflinePremiumDesc]        = "Étend le délai hors-ligne à 2 heures";
            d[LocKeys.OfflinePremiumOwned]       = "Débloqué";
            d[LocKeys.OfflinePremiumCostPending] = "Prix en attente";
            d[LocKeys.PackSupporterDesc]  = "Merci de soutenir Film Producer Tycoon.";
            d[LocKeys.PackProducerDesc]   = "Boostez votre carrière de producteur avec une importante réserve de diamants.";
            d[LocKeys.PackExecutiveDesc]  = "L'édition ultime pour les vrais magnats du cinéma.";
            d[LocKeys.InvestorCooldownFmt] = "CD · {0}m {1}s";
            d[LocKeys.InvestorReady]       = "5 min de revenus";
            return d;
        }

        static Dictionary<string, string> De(Dictionary<string, string> es)
        {
            var d = new Dictionary<string, string>(es);
            d[LocKeys.DeptLevelFormat] = "STUFE {0}";
            d[LocKeys.DeptUpgrade] = "VERBESSERN";
            d[LocKeys.DeptMax] = "MAX";
            d[LocKeys.DeptLocked] = "Gesperrt";
            d[LocKeys.DeptAvailableCity] = "Verfügbar in Stadt {0}";
            d[LocKeys.DeptBonusSummary] = "{0}\n+{1}/St.  Gesamt +{2}";
            d[LocKeys.DeptCategoryQuality] = "Qualität";
            d[LocKeys.DeptCategorySpeed] = "Tempo";
            d[LocKeys.DeptCategoryReduction] = "Kostenreduktion";
            d[LocKeys.DeptNameActors] = "Schauspieler";
            d[LocKeys.DeptNameSound] = "Ton";
            d[LocKeys.DeptNameCinematography] = "Kamera";
            d[LocKeys.DeptNameMakeup] = "Maske";
            d[LocKeys.DeptNameCostume] = "Kostüm";
            d[LocKeys.DeptNameArt] = "Art";
            d[LocKeys.DeptNameLighting] = "Beleuchtung";
            d[LocKeys.DeptNameProducer] = "Produzent";
            d[LocKeys.ProdTabTitle] = "PRODUKTION";
            d[LocKeys.ProdNewProduction] = "ANGEBOTE AKTUALISIEREN";
            d[LocKeys.ProdActiveSection] = "IN PRODUKTION";
            d[LocKeys.ProdNoActive] = "Keine aktive Produktion";
            d[LocKeys.ProdStartMovie] = "Starte einen Film";
            d[LocKeys.ProdStatusProducing] = "In Produktion";
            d[LocKeys.ProdStatusComplete] = "Produktion abgeschlossen";
            d[LocKeys.ProdDiscoverButton] = "PREMIERE";
            d[LocKeys.ProdSelect] = "AUSWÄHLEN";
            d[LocKeys.ProdCatalogComplete] = "Katalog abgeschlossen";
            d[LocKeys.ProdNoOffer] = "Kein Angebot verfügbar";
            d[LocKeys.ProdBudgetTitle] = "BUDGET";
            d[LocKeys.ProdBudgetSubtitle] = "Wähle die Produktionsart";
            d[LocKeys.ProdBudgetCheap] = "NIEDRIG";
            d[LocKeys.ProdBudgetStandard] = "NORMAL";
            d[LocKeys.ProdBudgetPremium] = "PREMIUM";
            d[LocKeys.ProdBudgetCancel] = "ABBRECHEN";
            d[LocKeys.ProdBudgetStatBlock] = "Zeit: {0}\nGeld: {1}\nRuf: {2}";
            d[LocKeys.ProdOfferMoney] = "Geld: {0}";
            d[LocKeys.ProdOfferRep] = "Rep.: +{0}";
            d[LocKeys.RefreshChoosePayment] = "Zahlungsart wählen";
            d[LocKeys.RefreshWatchAd] = "WERBUNG ANSEHEN";
            d[LocKeys.RefreshSpendDiamonds] = "DIAMANTEN AUSGEBEN";
            d[LocKeys.ProdRarityCommon] = "Gewöhnlich";
            d[LocKeys.ProdRarityRare] = "Selten";
            d[LocKeys.ProdRarityEpic] = "Episch";
            d[LocKeys.ProdRarityLegendary] = "Legendär";
            d[LocKeys.GenreAction] = "ACTION";
            d[LocKeys.GenreDrama] = "DRAMA";
            d[LocKeys.GenreHorror] = "HORROR";
            d[LocKeys.GenreComedy] = "KOMÖDIE";
            d[LocKeys.GenreRomance] = "ROMANTIK";
            d[LocKeys.GenreSciFi] = "SCI-FI";
            d[LocKeys.GenreFantasy] = "FANTASY";
            d[LocKeys.GenreThriller] = "THRILLER";
            d[LocKeys.GenreAnimation] = "ANIMATION";
            d[LocKeys.GenreDocumentary] = "DOKU";
            d[LocKeys.FtueWelcomeBody] = "Jedes große Studio begann mit einem einzigen Film.";
            d[LocKeys.FtueProductionTitle] = "Erste Produktion";
            d[LocKeys.FtueProductionBody] = "Wir brauchen unsere erste Produktion.";
            d[LocKeys.FtueStudioTitle] = "Studio verbessern";
            d[LocKeys.FtueStudioBody] = "Verbessere deine Abteilungen für bessere Filme.";
            d[LocKeys.FtueFirstMovieTitle] = "Dein erster Film ist fertig.";
            d[LocKeys.FtueCompleteBody] = "Verbessere das Studio und bestätige, um fortzufahren.";
            d[LocKeys.FtueMoneyEarned] = "Verdientes Geld: {0}";
            d[LocKeys.FtueRepEarned] = "Reputation: +{0}";
            d[LocKeys.FtueMovieDiscovered] = "Film entdeckt: {0}";
            d[LocKeys.FtueContinue] = "Weiter";
            d[LocKeys.FtueSkip] = "Überspringen";
            d[LocKeys.PremiereCompleted] = "Premiere abgeschlossen";
            d[LocKeys.PremiereRewardsTitle] = "Belohnungen";
            d[LocKeys.PremiereDiscoveryTitle] = "Neuer Film entdeckt";
            d[LocKeys.PremiereContinue] = "Weiter";
            d[LocKeys.CollectionTitle] = "SAMMLUNG";
            d[LocKeys.CollectionGlobalProgress] = "{0} / {1}";
            d[LocKeys.CollectionPercent] = "{0} % abgeschlossen";
            d[LocKeys.CollectionGenreProgress] = "{0} / {1} {2}";
            d[LocKeys.CollectionUnknownTitle] = "???";
            d[LocKeys.CollectionLocked] = "GESPERRT";
            d[LocKeys.CollectionDiscovered] = "ENTDECKT";
            d[LocKeys.CollectionLegendaryLock] = "Schließe alle Filme dieses Genres ab, um diesen Legendärfilm freizuschalten";

            // Settings (de was missing these)
            d[LocKeys.SettingsTitle]      = "EINSTELLUNGEN";
            d[LocKeys.SettingsLanguage]   = "Sprache";
            d[LocKeys.SettingsGooglePlay] = "Google Play Games";
            d[LocKeys.SettingsAccount]    = "Konto";
            d[LocKeys.SettingsSupport]    = "Support";
            d[LocKeys.SettingsCredits]    = "Impressum";
            d[LocKeys.SettingsPrivacy]    = "Datenschutzrichtlinie";
            d[LocKeys.SettingsRestore]    = "Käufe wiederherstellen";
            d[LocKeys.IapRestoreSuccess]  = "Käufe wiederhergestellt";
            d[LocKeys.IapRestoreFailed]   = "Käufe konnten nicht wiederhergestellt werden";
            d[LocKeys.IapPurchaseFailed]  = "Kauf abgebrochen oder fehlgeschlagen";
            d[LocKeys.IapUnavailable]     = "Shop nicht verfügbar";
            d[LocKeys.IapPackActivatedFmt]= "Pack aktiviert: {0}";
            d[LocKeys.SettingsClose]      = "SCHLIESSEN";
            d[LocKeys.SettingsMusic]      = "Musik";
            d[LocKeys.SettingsSfx]        = "Soundeffekte";
            d[LocKeys.SettingsGroupAudio] = "AUDIO";
            d[LocKeys.SettingsGroupLang]  = "SPRACHE";
            d[LocKeys.SettingsGroupInfo]  = "INFORMATION";
            d[LocKeys.TiendaTitle]        = "SHOP";
            d[LocKeys.TiendaComingSoon]   = "Demnächst";

            // Inst / Bonif (de was missing these)
            d[LocKeys.InstLevel]          = "Anlage {0}";
            d[LocKeys.InstAvailable]      = "Verfügbar in Anlage {0}";
            d[LocKeys.InstProgressScreen] = "ANLAGEN";
            d[LocKeys.InstUpgrade]        = "ERWEITERN";
            d[LocKeys.InstNextLevel]      = "Nächste Erweiterung:";
            d[LocKeys.InstCurrentLevel]   = "Anlage {0}";
            d[LocKeys.InstMultiplier]     = "Anlage {0} · ×{1:0.00}";
            d[LocKeys.InstLocked]         = "GESPERRT";
            d[LocKeys.BonifTitle]     = "BONI";
            d[LocKeys.BonifSpeed]     = "Tempo";
            d[LocKeys.BonifQuality]   = "Qualität";
            d[LocKeys.BonifBoxOffice] = "Kasse";
            d[LocKeys.BonifRep]       = "REP";
            d[LocKeys.BonifXP]        = "XP";
            d[LocKeys.BonifCosts]     = "Kosten";

            // Contracts (de was missing several)
            d[LocKeys.ContractRefresh]        = "VERTRÄGE AKTUALISIEREN";
            d[LocKeys.ContractSelect]         = "WÄHLEN";
            d[LocKeys.ContractChoosePrompt]   = "Wähle einen Vertrag";
            d[LocKeys.ContractActiveObjective] = "Ziel: {0}";
            d[LocKeys.ContractObjProduceMovies] = "Produziere {0} Filme";
            d[LocKeys.ContractObjProduceGenre]  = "Produziere {0} {1}-Filme";
            d[LocKeys.ContractObjReachRep]      = "Erreiche {0} REP";
            d[LocKeys.ContractObjReachQuality]  = "Qualität ≥ {0}";
            d[LocKeys.ContractObjEarnMoney]     = "Verdiene ${0}";
            d[LocKeys.ContractObjSpendUpgrades] = "Gebe ${0} für Verbesserungen aus";
            d[LocKeys.ContractObjReachLevel]    = "Erreiche Stufe {0}";
            d[LocKeys.ContractObjUnderTime]     = "Produziere in ≤ {0}s";

            // Nav tabs
            d[LocKeys.NavStudio]     = "STUDIO";
            d[LocKeys.NavProduction] = "PRODUKTION";
            d[LocKeys.NavAwards]     = "PREISE";
            d[LocKeys.NavCollection] = "SAMMLUNG";
            d[LocKeys.NavShop]       = "SHOP";

            // Mejoras sub-tabs
            d[LocKeys.MejorasProduction] = "PRODUKTION";
            d[LocKeys.MejorasStaff]      = "PERSONAL";
            d[LocKeys.MejorasResearch]   = "FORSCHUNG";
            d[LocKeys.MejorasMarketing]  = "MARKETING";

            // Awards
            d[LocKeys.AwardsTitle]         = "GOLDENE STERNE";
            d[LocKeys.AwardsNextStar]      = "NÄCHSTER STERN";
            d[LocKeys.AwardsProgressSect]  = "STUDIO-FORTSCHRITT";
            d[LocKeys.AwardsKeepProducing] = "Weiter produzieren...";
            d[LocKeys.AwardsAllEarned]     = "Alle Goldenen Sterne erreicht";
            d[LocKeys.AwardsComplete]      = "Sammlung vollständig!";
            d[LocKeys.AwardsEmpireMax]     = "Filmimperium!";
            d[LocKeys.AwardsReadyClaim]    = "BEREIT ZUM EINLÖSEN!";
            d[LocKeys.AwardsClaimBtn]      = "✦ GOLDENEN STERN ERHALTEN";
            d[LocKeys.AwardsRepNeeded]     = "REP benötigt: {0:N0} / {1:N0}";
            d[LocKeys.AwardsHint]          = "Goldene Sterne fördern das Studiowachstum.";
            d[LocKeys.AwardsStudioLevel]   = "Studio Stufe {0}";

            // Production slots
            d[LocKeys.ProdSlotFree]   = "FREIER SLOT";
            d[LocKeys.ProdSlotLocked] = "SLOT GESPERRT";
            d[LocKeys.ProdSlotUnlock] = "Freischalten unter Verbesserungen → Anlagen";

            // Upgrade cards
            d[LocKeys.UpgradeNoEffect]     = "Kein Effekt";
            d[LocKeys.UpgradeLockedStudio] = "Studio Stufe {0}";

            // Contracts
            d[LocKeys.ContractActive]        = "AKTIVER VERTRAG";
            d[LocKeys.ContractNoneAvailable] = "Kein Vertrag verfügbar";
            d[LocKeys.ContractClaim]         = "EINLÖSEN";
            d[LocKeys.ContractCompletedPfx]  = "Abgeschlossen — ";
            d[LocKeys.ContractNoActive]      = "Kein aktiver Vertrag";
            d[LocKeys.ContractReadyClaim]    = "Bereit zum Einlösen!";
            d[LocKeys.ContractObjective]     = "ZIEL";

            // Collection screen
            d[LocKeys.CollectionScreenTitle]   = "MEINE SAMMLUNG";
            d[LocKeys.CollectionGenreLabel]    = "GENRE";
            d[LocKeys.CollectionSynopsisHdr]   = "SYNOPSIS";
            d[LocKeys.CollectionClose]         = "SCHLIESSEN";
            d[LocKeys.CollectionSynopsisNone]  = "Keine Inhaltsangabe verfügbar.";
            d[LocKeys.CollectionFilmCompleted] = "✔ ABGESCHLOSSEN";
            d[LocKeys.CollectionFilmPending]   = "○ AUSSTEHEND";
            d[LocKeys.CollectionSelectGenre]   = "Genre wählen";
            d[LocKeys.CollectionAllMovies]     = "Alle Filme";
            d[LocKeys.CollectionBack]          = "← ZURÜCK";

            // GameFeel
            d[LocKeys.GameFeelContractComplete] = "VERTRAG ABGESCHLOSSEN";
            d[LocKeys.GameFeelOscarEarned]      = "OSCAR GEWONNEN";
            d[LocKeys.GameFeelCityImproved]     = "ANLAGE VERBESSERT";
            d[LocKeys.GameFeelUpgradeBought]    = "VERBESSERUNG GEKAUFT";
            d[LocKeys.GameFeelContinue]         = "WEITER";
            d[LocKeys.GameFeelOscarsSuffix]     = " Oscar(s) · Anlage verbessern";
            d[LocKeys.GameFeelIncomeGlobal]     = " globales Einkommen";

            // Studio chrome
            d[LocKeys.StudioName]            = "IDLE FILM STUDIO";
            d[LocKeys.StudioLevelFormat]     = "Stufe {0}";
            d[LocKeys.StudioXPFormat]        = "{0:0}/{1:0} XP";
            d[LocKeys.StudioNextLevel]       = "Nächste Stufe · {0:0} XP verbleibend";
            d[LocKeys.StudioBonusSummaryFmt] = "Einnahmen +{0:0}% · REP +{1:0}% · Tempo +{2:0}%";

            // TopBar
            d[LocKeys.TopBarQualAbbr]  = "Qual.";
            d[LocKeys.TopBarSpeedAbbr] = "Gsch.";
            d[LocKeys.TopBarLevelAbbr] = "Lv.";

            // Upgrade effects
            d[LocKeys.UpgradeCurrent]       = "Aktuell";
            d[LocKeys.UpgradeNext]          = "Nächste";
            d[LocKeys.UpgradeEffectQuality] = "+{0:0.#}% Qualität";
            d[LocKeys.UpgradeEffectSpeed]   = "+{0:0.#}% Geschw.";
            d[LocKeys.UpgradeEffectCost]    = "-{0:0.#}% Kosten";
            d[LocKeys.UpgradeEffectRep]     = "+{0:0.#}% Ruf";
            d[LocKeys.UpgradeEffectIncome]  = "+{0}/s Einnahmen";
            d[LocKeys.UpgradeEffectSlot]    = "+{0} Prod.-Slot(s)";
            d[LocKeys.UpgradeEffectCatalog] = "Katalog Lv.{0}";

            // Awards
            d[LocKeys.AwardsStarTitleFmt]  = "Stern #{0}";
            d[LocKeys.AwardsRepReadyFmt]   = "Ruf: {0:N0} / {1:N0} REP ✓";
            d[LocKeys.AwardsUnlockHintFmt] = "{0}  ·  {1} bis zur nächsten";

            // City
            d[LocKeys.InstGlobalBonus]    = "GLOBALER BONUS";
            d[LocKeys.InstGlobalBonusFmt] = "×{0:0.00} glob. Einnahmen";
            d[LocKeys.InstCurrentUnlocks] = "AKTUELLE FREISCHALTUNGEN";
            d[LocKeys.InstMaxCity]        = "Sie haben das Kinoimperium erreicht!";

            // Production
            d[LocKeys.ProdProduceBtn] = "PRODUZIEREN";
            d[LocKeys.ProdEmptyState] = "Keine Filme verfügbar.";

            // Collection stats
            d[LocKeys.CollStatLvlFmt] = "Lv.{0}";

            // Premiere
            d[LocKeys.PremiereVarietyFmt]   = "Vielfalt +{0:0}%";
            d[LocKeys.PremiereFilmFound]    = "Film entdeckt";
            d[LocKeys.PremiereContractDone] = "Vertrag abgeschlossen";
            d[LocKeys.PremiereContractProg] = "Vertrag · {0:0}/{1:0}";

            // GameFeel
            d[LocKeys.GameFeelUpgradeFallback] = "Upgrade erworben";

            // Store
            d[LocKeys.StoreSectionDiamonds] = "DIAMANTEN";
            d[LocKeys.StoreSectionPacks]    = "› PACKS";
            d[LocKeys.StoreSectionBoosts]   = "› BOOSTS";
            d[LocKeys.StoreSectionPremium]  = "› PREMIUM";
            d[LocKeys.StoreWatchAd]         = "Werbung ansehen";
            d[LocKeys.StoreStarterDesc]     = "Diamanten + Boost + Keine Werbung 24h";
            d[LocKeys.StoreUniqueOffer]     = "EINMALIGES ANGEBOT";
            d[LocKeys.StoreNoAds]           = "KEINE WERBUNG";
            d[LocKeys.StoreNoAdsDesc]       = "Werbung für immer entfernen";

            // Store products
            d[LocKeys.StoreProdDiamSmall]    = "HANDVOLL";
            d[LocKeys.StoreProdDiamMed]      = "BEUTEL";
            d[LocKeys.StoreProdDiamLarge]    = "TRUHE";
            d[LocKeys.StoreProdPackDir]      = "REGISSEUR-PACK";
            d[LocKeys.StoreProdPackDirDesc]  = "Diamanten + Boost\n+ Seltener Film";
            d[LocKeys.StoreProdPackStar]     = "STERN-PACK";
            d[LocKeys.StoreProdPackStarDesc] = "Diamanten + REP\n+ Epischer Film";
            d[LocKeys.StoreProdBoostProd]    = "PRODUKTION x2";
            d[LocKeys.StoreProdBoostInc]     = "EINNAHMEN x2";
            d[LocKeys.SettingsComing]        = "BALD";
            // Studio subtabs
            d[LocKeys.StudTabDepts]     = "ABTEILUNGEN";
            d[LocKeys.StudTabMejoras]   = "UPGRADES";
            d[LocKeys.StudTabContratos] = "VERTRÄGE";

            // Credits
            d[LocKeys.CreditsCreatedBy]  = "Erstellt von";
            d[LocKeys.CreditsVersion]    = "Version {0}";
            d[LocKeys.CreditsPoweredBy]  = "Erstellt mit Unity";
            d[LocKeys.IncomePerSecFmt]   = "+${0:N0}/s";

            // Premiere headline
            d[LocKeys.PremiereHeadline]  = "PREMIERE";

            // City / Facility names
            d[LocKeys.CityNamePfx + "1"] = "Garage";
            d[LocKeys.CityNamePfx + "2"] = "Unabhängiges Studio";
            d[LocKeys.CityNamePfx + "3"] = "Lokales Studio";
            d[LocKeys.CityNamePfx + "4"] = "Regionales Studio";
            d[LocKeys.CityNamePfx + "5"] = "Großes Studio";
            d[LocKeys.CityNamePfx + "6"] = "Hollywood Boulevard";
            d[LocKeys.CityNamePfx + "7"] = "Major Studio";
            d[LocKeys.CityNamePfx + "8"] = "Filmimperium";

            // Upgrade names
            d[LocKeys.UpgradeNamePfx + "equip_camera_basic"]    = "Basiskamera";
            d[LocKeys.UpgradeNamePfx + "equip_camera_digital"]  = "Digitale Profikamera";
            d[LocKeys.UpgradeNamePfx + "equip_camera_pro"]      = "Professionelle Kamera";
            d[LocKeys.UpgradeNamePfx + "equip_coloring"]        = "Colorgrading-Suite";
            d[LocKeys.UpgradeNamePfx + "equip_dolby"]           = "Dolby Atmos Prozessor";
            d[LocKeys.UpgradeNamePfx + "equip_drone"]           = "Drohnensystem";
            d[LocKeys.UpgradeNamePfx + "equip_editing_soft"]    = "Profi-Schnittsoftware";
            d[LocKeys.UpgradeNamePfx + "equip_lighting_kit"]    = "Beleuchtungskit";
            d[LocKeys.UpgradeNamePfx + "equip_lighting_led"]    = "Kinematografisches LED-System";
            d[LocKeys.UpgradeNamePfx + "equip_mic_pro"]         = "Profimikrofon";
            d[LocKeys.UpgradeNamePfx + "equip_render_farm"]     = "Render-Farm";
            d[LocKeys.UpgradeNamePfx + "equip_sound_surround"]  = "Surround-Sound-System";
            d[LocKeys.UpgradeNamePfx + "equip_steadicam"]       = "Steadicam Pro";
            d[LocKeys.UpgradeNamePfx + "equip_vfx_advanced"]    = "Erweiterte VFX-Suite";
            d[LocKeys.UpgradeNamePfx + "equip_vfx_basic"]       = "Basis-VFX-Station";
            d[LocKeys.UpgradeNamePfx + "install_editing_room"]  = "Schnittroom";
            d[LocKeys.UpgradeNamePfx + "install_exterior"]      = "Außenstudios";
            d[LocKeys.UpgradeNamePfx + "install_offices"]       = "Produktionsbüros";
            d[LocKeys.UpgradeNamePfx + "install_plato_large"]   = "Großes Studio";
            d[LocKeys.UpgradeNamePfx + "install_plato_medium"]  = "Mittleres Studio";
            d[LocKeys.UpgradeNamePfx + "install_plato_small"]   = "Kleines Studio";
            d[LocKeys.UpgradeNamePfx + "install_screening"]     = "Privater Vorführraum";
            d[LocKeys.UpgradeNamePfx + "install_studio_lot"]    = "Komplettes Studio-Gelände";
            d[LocKeys.UpgradeNamePfx + "install_vfx_dept"]      = "VFX-Abteilung";
            d[LocKeys.UpgradeNamePfx + "mkt_global"]            = "Weltweiter Vertrieb";
            d[LocKeys.UpgradeNamePfx + "mkt_press_kit"]         = "Pressekampagne";
            d[LocKeys.UpgradeNamePfx + "mkt_social_media"]      = "Social Media Manager";
            d[LocKeys.UpgradeNamePfx + "mkt_trailer"]           = "Trailer-Abteilung";
            d[LocKeys.UpgradeNamePfx + "personal_actors"]       = "Schauspieler";
            d[LocKeys.UpgradeNamePfx + "personal_art"]          = "Kunstdepartment";
            d[LocKeys.UpgradeNamePfx + "personal_cinematography"]= "Chefkameramann";
            d[LocKeys.UpgradeNamePfx + "personal_composer"]     = "Filmkomponist";
            d[LocKeys.UpgradeNamePfx + "personal_costume"]      = "Kostüm";
            d[LocKeys.UpgradeNamePfx + "personal_director"]     = "Regisseur";
            d[LocKeys.UpgradeNamePfx + "personal_editor"]       = "Cutter";
            d[LocKeys.UpgradeNamePfx + "personal_grip"]         = "Kameramechaniker";
            d[LocKeys.UpgradeNamePfx + "personal_lighting"]     = "Beleuchter";
            d[LocKeys.UpgradeNamePfx + "personal_makeup"]       = "Maske";
            d[LocKeys.UpgradeNamePfx + "personal_producer"]     = "Ausf. Produzent";
            d[LocKeys.UpgradeNamePfx + "personal_publicist"]    = "Pressesprecher";
            d[LocKeys.UpgradeNamePfx + "personal_sound"]        = "Tontechniker";

            // Contract titles
            d[LocKeys.ContractTitlePfx + "c_three_movies"]      = "Kurzfilmfestival";
            d[LocKeys.ContractTitlePfx + "c_first_reputation"]  = "Erste Fans";
            d[LocKeys.ContractTitlePfx + "c_first_movie"]       = "Erste Dreharbeiten";
            d[LocKeys.ContractTitlePfx + "c_action_fan"]        = "Actionfilm-Fan";
            d[LocKeys.ContractTitlePfx + "c_romance_story"]     = "Liebesgeschichte";
            d[LocKeys.ContractTitlePfx + "c_first_upgrade"]     = "In das Studio investieren";
            d[LocKeys.ContractTitlePfx + "c_fantasy_realm"]     = "Fantasie-Reiche";
            d[LocKeys.ContractTitlePfx + "c_thriller_night"]    = "Thriller-Nacht";
            d[LocKeys.ContractTitlePfx + "c_documentary_truth"] = "Die Wahrheit des Kinos";
            d[LocKeys.ContractTitlePfx + "c_drama_fan"]         = "Drama-Fan";
            d[LocKeys.ContractTitlePfx + "c_animation_studio"]  = "Animationsstudio";
            d[LocKeys.ContractTitlePfx + "c_rep_50"]            = "Aufgehender Stern";
            d[LocKeys.ContractTitlePfx + "c_horror_night"]      = "Horrornacht";
            d[LocKeys.ContractTitlePfx + "c_speed_run"]         = "Express-Produktion";
            d[LocKeys.ContractTitlePfx + "c_studio_level5"]     = "Etabliertes Studio";
            d[LocKeys.ContractTitlePfx + "c_rep_200"]           = "Branchenreferenz";
            d[LocKeys.ContractTitlePfx + "c_comedy_king"]       = "Komödienkönig";
            d[LocKeys.ContractTitlePfx + "c_studio_level10"]    = "Etablierte Produktionsfirma";
            d[LocKeys.ContractTitlePfx + "c_ten_movies"]        = "Kino-Marathon";
            d[LocKeys.ContractTitlePfx + "c_scifi_pioneer"]     = "Sci-Fi-Pionier";
            d[LocKeys.ContractTitlePfx + "c_studio_level15"]    = "Großes Studio";
            d[LocKeys.ContractTitlePfx + "c_rep_1000"]          = "Kinolegende";
            d[LocKeys.ContractTitlePfx + "c_all_genres"]        = "Kino ohne Grenzen";
            d[LocKeys.ContractTitlePfx + "c_big_spender"]       = "Große Investition";
            d[LocKeys.ContractTitlePfx + "c_studio_level20"]    = "Internationales Studio";
            d[LocKeys.ContractTitlePfx + "c_rep_5000"]          = "Globale Ikone";
            d[LocKeys.ContractTitlePfx + "c_blockbuster"]       = "Der große Blockbuster";

            // Oscar / installation progress
            d[LocKeys.InstOscarProgress]  = "{0} / {1} OSC";
            d[LocKeys.InstOscarMax]       = "{0} OSC · MAX";
            d[LocKeys.InstUnlockGlobal]   = "×{0:0.00} glob. Einnahmen";
            d[LocKeys.InstUnlockDept]     = "Abt.: {0}";
            d[LocKeys.InstTierPfx + "1"]  = "Tier 1–2 · Basisfilme";
            d[LocKeys.InstTierPfx + "2"]  = "Sequels und Remakes";
            d[LocKeys.InstTierPfx + "3"]  = "Professionelle Produktion";
            d[LocKeys.InstTierPfx + "4"]  = "Fortgeschrittene Filme";
            d[LocKeys.InstTierPfx + "5"]  = "Kampagne & Vertrieb";
            d[LocKeys.InstTierPfx + "6"]  = "VFX & Preise";
            d[LocKeys.InstTierPfx + "7"]  = "Streaming & Universen";
            d[LocKeys.InstTierPfx + "8"]  = "Vollständiges Imperium";

            // City lock labels
            d[LocKeys.CityLockLabel]      = "Stadt {0} erforderlich";

            // Offer card badges
            d[LocKeys.OfferBadgeNew]      = "[NEU]";
            d[LocKeys.OfferBadgeSaga]     = "[SAGA]";
            d[LocKeys.OfferBadgeContract] = "[VERTRAG]";

            // Saga labels
            d[LocKeys.SagaCompleted]   = "{0}/{1} ABGESCHLOSSEN";
            d[LocKeys.SagaBlockReason] = "Schließe Eintrag {0} der Saga zuerst ab";

            // Ad reward feedback
            d[LocKeys.AdLimitReached]        = "Tageslimit erreicht";
            d[LocKeys.FreeDiamondsAwarded]   = "+{0} Diamanten hinzugefügt";
            d[LocKeys.BoostActivated]        = "Boost aktiviert! ×2 für 10 Min";
            d[LocKeys.BoostAlreadyActive]    = "Dieser Boost ist bereits aktiv";
            d[LocKeys.InvestorAwarded]       = "+{0:N0} $ vom Investor";
            d[LocKeys.InvestorNoIncome]      = "Aktuelles Einkommen ist 0. Produziere mehr Filme.";
            d[LocKeys.StoreSectionInvestor]  = "Investor";
            d[LocKeys.StoreSectionFreeRewards] = "Tagesbelohnungen";
            d[LocKeys.StoreProdBoostRep]     = "REP-Boost";
            d[LocKeys.StoreProdBoostXP]      = "XP-Boost";
            d[LocKeys.StoreProdFreeDiam]     = "Gratis";
            d[LocKeys.StoreProdInvestor]     = "Investor";
            d[LocKeys.StoreProdOfflinePremium] = "Fernproduzent";
            d[LocKeys.StoreBoostTimerFmt]    = "{0}m {1}s";
            d[LocKeys.StoreBoostActive]      = "AKTIV ·";
            d[LocKeys.StorePackStarter]      = "Starter-Paket";
            d[LocKeys.StorePackSupporter]    = "Unterstützer";
            d[LocKeys.StorePackProducer]     = "Produzent";
            d[LocKeys.StorePackExecutive]    = "Ausführender Produzent";
            d[LocKeys.StorePackAlreadyOwned] = "Bereits gekauft";
            d[LocKeys.StoreSimBuy]           = "[KAUF SIMULIEREN]";
            d[LocKeys.OfflinePremiumName]        = "Fernproduzent";
            d[LocKeys.OfflinePremiumDesc]        = "Erweitert das Offline-Limit auf 2 Stunden";
            d[LocKeys.OfflinePremiumOwned]       = "Entsperrt";
            d[LocKeys.OfflinePremiumCostPending] = "Preis ausstehend";
            d[LocKeys.PackSupporterDesc]  = "Danke für Ihre Unterstützung von Film Producer Tycoon.";
            d[LocKeys.PackProducerDesc]   = "Treiben Sie Ihre Karriere als Produzent voran mit einer großen Diamantenreserve.";
            d[LocKeys.PackExecutiveDesc]  = "Die ultimative Edition für echte Filmmagnaten.";
            d[LocKeys.InvestorCooldownFmt] = "CD · {0}m {1}s";
            d[LocKeys.InvestorReady]       = "5 Min. Einkommen";
            return d;
        }

        static Dictionary<string, string> Ja(Dictionary<string, string> es)
        {
            var d = new Dictionary<string, string>(es);
            d[LocKeys.DeptLevelFormat] = "レベル {0}";
            d[LocKeys.DeptUpgrade] = "強化";
            d[LocKeys.DeptMax] = "最大";
            d[LocKeys.DeptLocked] = "ロック";
            d[LocKeys.DeptAvailableCity] = "シティ {0} で解放";
            d[LocKeys.DeptBonusSummary] = "{0}\n+{1}/Lv  合計 +{2}";
            d[LocKeys.DeptCategoryQuality] = "品質";
            d[LocKeys.DeptCategorySpeed] = "速度";
            d[LocKeys.DeptCategoryReduction] = "コスト削減";
            d[LocKeys.DeptNameEditor] = "編集";
            d[LocKeys.DeptNameDirector] = "監督";
            d[LocKeys.DeptNameActors] = "俳優";
            d[LocKeys.DeptNameSound] = "音響";
            d[LocKeys.DeptNameCinematography] = "撮影";
            d[LocKeys.DeptNameMakeup] = "メイク";
            d[LocKeys.DeptNameCostume] = "衣装";
            d[LocKeys.DeptNameArt] = "美術";
            d[LocKeys.DeptNameLighting] = "照明";
            d[LocKeys.DeptNameGrip] = "グリップ";
            d[LocKeys.DeptNameProducer] = "プロデューサー";
            d[LocKeys.ProdTabTitle] = "制作";
            d[LocKeys.ProdNewProduction] = "オファーを更新";
            d[LocKeys.ProdActiveSection] = "撮影中";
            d[LocKeys.ProdNoActive] = "進行中の制作なし";
            d[LocKeys.ProdStartMovie] = "映画を開始";
            d[LocKeys.ProdStatusProducing] = "制作中";
            d[LocKeys.ProdStatusComplete] = "制作完了";
            d[LocKeys.ProdDiscoverButton] = "プレミア上映";
            d[LocKeys.ProdSelect] = "選択";
            d[LocKeys.ProdCatalogComplete] = "カタログ完了";
            d[LocKeys.ProdNoOffer] = "オファーなし";
            d[LocKeys.ProdBudgetTitle] = "予算";
            d[LocKeys.ProdBudgetSubtitle] = "制作方法を選んでください";
            d[LocKeys.ProdBudgetCheap] = "低";
            d[LocKeys.ProdBudgetStandard] = "標準";
            d[LocKeys.ProdBudgetPremium] = "プレミアム";
            d[LocKeys.ProdBudgetCancel] = "キャンセル";
            d[LocKeys.ProdBudgetStatBlock] = "時間: {0}\n収入: {1}\n評価: {2}";
            d[LocKeys.ProdOfferMoney] = "金額: {0}";
            d[LocKeys.ProdOfferRep] = "評価: +{0}";
            d[LocKeys.RefreshChoosePayment] = "支払い方法を選択";
            d[LocKeys.RefreshWatchAd] = "広告を見る";
            d[LocKeys.RefreshSpendDiamonds] = "ダイヤモンドを使う";
            d[LocKeys.ProdRarityCommon] = "コモン";
            d[LocKeys.ProdRarityRare] = "レア";
            d[LocKeys.ProdRarityEpic] = "エピック";
            d[LocKeys.ProdRarityLegendary] = "レジェンダリー";
            d[LocKeys.GenreAction] = "アクション";
            d[LocKeys.GenreDrama] = "ドラマ";
            d[LocKeys.GenreHorror] = "ホラー";
            d[LocKeys.GenreComedy] = "コメディ";
            d[LocKeys.GenreRomance] = "ロマンス";
            d[LocKeys.GenreSciFi] = "SF";
            d[LocKeys.GenreFantasy] = "ファンタジー";
            d[LocKeys.GenreThriller] = "スリラー";
            d[LocKeys.GenreAnimation] = "アニメーション";
            d[LocKeys.GenreDocumentary] = "ドキュメンタリー";
            d[LocKeys.FtueWelcomeBody] = "偉大なスタジオも、最初は一本の映画から始まった。";
            d[LocKeys.FtueProductionTitle] = "最初の制作";
            d[LocKeys.FtueProductionBody] = "最初の制作が必要だ。";
            d[LocKeys.FtueStudioTitle] = "スタジオを強化";
            d[LocKeys.FtueStudioBody] = "部署を強化して、より良い映画を作ろう。";
            d[LocKeys.FtueFirstMovieTitle] = "最初の映画が完成した。";
            d[LocKeys.FtueCompleteBody] = "スタジオを強化して、確認して続けよう。";
            d[LocKeys.FtueMoneyEarned] = "獲得金: {0}";
            d[LocKeys.FtueRepEarned] = "評判: +{0}";
            d[LocKeys.FtueMovieDiscovered] = "発見した映画: {0}";
            d[LocKeys.FtueContinue] = "続ける";
            d[LocKeys.FtueSkip] = "スキップ";
            d[LocKeys.PremiereCompleted] = "プレミア完了";
            d[LocKeys.PremiereRewardsTitle] = "報酬";
            d[LocKeys.PremiereDiscoveryTitle] = "新しい映画を発見";
            d[LocKeys.PremiereContinue] = "続ける";
            d[LocKeys.CollectionTitle] = "コレクション";
            d[LocKeys.CollectionGlobalProgress] = "{0} / {1}";
            d[LocKeys.CollectionPercent] = "{0}% 完了";
            d[LocKeys.CollectionGenreProgress] = "{0} / {1} {2}";
            d[LocKeys.CollectionUnknownTitle] = "???";
            d[LocKeys.CollectionLocked] = "ロック";
            d[LocKeys.CollectionDiscovered] = "発見";
            d[LocKeys.CollectionLegendaryLock] = "このジャンルの映画をすべて完成させると、このレジェンダリー映画が解放されます";

            // Settings (ja was missing these)
            d[LocKeys.SettingsTitle]      = "設定";
            d[LocKeys.SettingsLanguage]   = "言語";
            d[LocKeys.SettingsGooglePlay] = "Google Play ゲーム";
            d[LocKeys.SettingsAccount]    = "アカウント";
            d[LocKeys.SettingsSupport]    = "サポート";
            d[LocKeys.SettingsCredits]    = "クレジット";
            d[LocKeys.SettingsPrivacy]    = "プライバシーポリシー";
            d[LocKeys.SettingsRestore]    = "購入を復元";
            d[LocKeys.IapRestoreSuccess]  = "購入を復元しました";
            d[LocKeys.IapRestoreFailed]   = "購入を復元できませんでした";
            d[LocKeys.IapPurchaseFailed]  = "購入がキャンセルまたは失敗しました";
            d[LocKeys.IapUnavailable]     = "ストアを利用できません";
            d[LocKeys.IapPackActivatedFmt]= "パック有効化: {0}";
            d[LocKeys.SettingsClose]      = "閉じる";
            d[LocKeys.SettingsMusic]      = "音楽";
            d[LocKeys.SettingsSfx]        = "効果音";
            d[LocKeys.SettingsGroupAudio] = "オーディオ";
            d[LocKeys.SettingsGroupLang]  = "言語";
            d[LocKeys.SettingsGroupInfo]  = "情報";
            d[LocKeys.TiendaTitle]        = "ショップ";
            d[LocKeys.TiendaComingSoon]   = "近日公開";

            // Inst / Bonif (ja was missing these)
            d[LocKeys.InstLevel]          = "施設 {0}";
            d[LocKeys.InstAvailable]      = "施設 {0} で解放";
            d[LocKeys.InstProgressScreen] = "施設";
            d[LocKeys.InstUpgrade]        = "拡張";
            d[LocKeys.InstNextLevel]      = "次の拡張:";
            d[LocKeys.InstCurrentLevel]   = "施設 {0}";
            d[LocKeys.InstMultiplier]     = "施設 {0} · ×{1:0.00}";
            d[LocKeys.InstLocked]         = "ロック中";
            d[LocKeys.BonifTitle]         = "ボーナス";
            d[LocKeys.BonifSpeed]         = "速度";
            d[LocKeys.BonifQuality]       = "品質";
            d[LocKeys.BonifBoxOffice]     = "興収";
            d[LocKeys.BonifRep]           = "評判";
            d[LocKeys.BonifXP]            = "XP";
            d[LocKeys.BonifCosts]         = "コスト";

            // Contracts (ja was missing several)
            d[LocKeys.ContractRefresh]        = "契約を更新";
            d[LocKeys.ContractSelect]         = "選択";
            d[LocKeys.ContractChoosePrompt]   = "契約を選んでください";
            d[LocKeys.ContractActiveObjective] = "目標: {0}";
            d[LocKeys.ContractObjProduceMovies] = "{0}本の映画を制作";
            d[LocKeys.ContractObjProduceGenre]  = "{1}映画を{0}本制作";
            d[LocKeys.ContractObjReachRep]      = "REP {0} に到達";
            d[LocKeys.ContractObjReachQuality]  = "品質 ≥ {0}";
            d[LocKeys.ContractObjEarnMoney]     = "${0} 獲得";
            d[LocKeys.ContractObjSpendUpgrades] = "アップグレードに${0}使用";
            d[LocKeys.ContractObjReachLevel]    = "レベル{0}に到達";
            d[LocKeys.ContractObjUnderTime]     = "{0}秒以内に制作";

            // Nav tabs
            d[LocKeys.NavStudio]     = "スタジオ";
            d[LocKeys.NavProduction] = "制作";
            d[LocKeys.NavAwards]     = "賞";
            d[LocKeys.NavCollection] = "コレクション";
            d[LocKeys.NavShop]       = "ショップ";

            // Mejoras sub-tabs
            d[LocKeys.MejorasProduction] = "制作";
            d[LocKeys.MejorasStaff]      = "スタッフ";
            d[LocKeys.MejorasResearch]   = "研究";
            d[LocKeys.MejorasMarketing]  = "マーケティング";

            // Awards
            d[LocKeys.AwardsTitle]         = "ゴールデンスター";
            d[LocKeys.AwardsNextStar]      = "次のスター";
            d[LocKeys.AwardsProgressSect]  = "スタジオの進捗";
            d[LocKeys.AwardsKeepProducing] = "制作を続けよう...";
            d[LocKeys.AwardsAllEarned]     = "すべてのゴールデンスターを獲得済み";
            d[LocKeys.AwardsComplete]      = "コレクション完成！";
            d[LocKeys.AwardsEmpireMax]     = "映画帝国！";
            d[LocKeys.AwardsReadyClaim]    = "受け取り準備完了！";
            d[LocKeys.AwardsClaimBtn]      = "✦ ゴールデンスター獲得";
            d[LocKeys.AwardsRepNeeded]     = "必要REP: {0:N0} / {1:N0}";
            d[LocKeys.AwardsHint]          = "ゴールデンスターはスタジオの成長を後押しします。";
            d[LocKeys.AwardsStudioLevel]   = "スタジオ Lv.{0}";

            // Production slots
            d[LocKeys.ProdSlotFree]   = "空きスロット";
            d[LocKeys.ProdSlotLocked] = "ロック中";
            d[LocKeys.ProdSlotUnlock] = "アップグレード→施設で解放";

            // Upgrade cards
            d[LocKeys.UpgradeNoEffect]     = "効果なし";
            d[LocKeys.UpgradeLockedStudio] = "スタジオLv.{0}";

            // Contracts
            d[LocKeys.ContractActive]        = "アクティブ契約";
            d[LocKeys.ContractNoneAvailable] = "契約なし";
            d[LocKeys.ContractClaim]         = "受け取る";
            d[LocKeys.ContractCompletedPfx]  = "完了 — ";
            d[LocKeys.ContractNoActive]      = "アクティブな契約なし";
            d[LocKeys.ContractReadyClaim]    = "受け取り準備完了！";
            d[LocKeys.ContractObjective]     = "目標";

            // Collection screen
            d[LocKeys.CollectionScreenTitle]   = "コレクション";
            d[LocKeys.CollectionGenreLabel]    = "ジャンル";
            d[LocKeys.CollectionSynopsisHdr]   = "あらすじ";
            d[LocKeys.CollectionClose]         = "閉じる";
            d[LocKeys.CollectionSynopsisNone]  = "あらすじがありません。";
            d[LocKeys.CollectionFilmCompleted] = "✔ 完成";
            d[LocKeys.CollectionFilmPending]   = "○ 未完成";
            d[LocKeys.CollectionSelectGenre]   = "ジャンルを選んでください";
            d[LocKeys.CollectionAllMovies]     = "すべての映画";
            d[LocKeys.CollectionBack]          = "← 戻る";

            // GameFeel
            d[LocKeys.GameFeelContractComplete] = "契約完了";
            d[LocKeys.GameFeelOscarEarned]      = "オスカー獲得";
            d[LocKeys.GameFeelCityImproved]     = "施設アップグレード";
            d[LocKeys.GameFeelUpgradeBought]    = "アップグレード購入";
            d[LocKeys.GameFeelContinue]         = "続ける";
            d[LocKeys.GameFeelOscarsSuffix]     = " オスカー · 施設をアップグレード";
            d[LocKeys.GameFeelIncomeGlobal]     = " 全体収入";

            // Studio chrome
            d[LocKeys.StudioName]            = "IDLE FILM STUDIO";
            d[LocKeys.StudioLevelFormat]     = "レベル {0}";
            d[LocKeys.StudioXPFormat]        = "{0:0}/{1:0} XP";
            d[LocKeys.StudioNextLevel]       = "次のレベル · 残り{0:0} XP";
            d[LocKeys.StudioBonusSummaryFmt] = "収入 +{0:0}% · REP +{1:0}% · 速度 +{2:0}%";

            // TopBar
            d[LocKeys.TopBarDiam]      = "ダイヤ";
            d[LocKeys.TopBarQualAbbr]  = "品質";
            d[LocKeys.TopBarSpeedAbbr] = "速度";
            d[LocKeys.TopBarLevelAbbr] = "Lv.";

            // Upgrade effects
            d[LocKeys.UpgradeCurrent]       = "現在";
            d[LocKeys.UpgradeNext]          = "次へ";
            d[LocKeys.UpgradeMax]           = "最大";
            d[LocKeys.UpgradeEffectQuality] = "+{0:0.#}% 品質";
            d[LocKeys.UpgradeEffectSpeed]   = "+{0:0.#}% 速度";
            d[LocKeys.UpgradeEffectCost]    = "-{0:0.#}% コスト";
            d[LocKeys.UpgradeEffectRep]     = "+{0:0.#}% 評判";
            d[LocKeys.UpgradeEffectIncome]  = "+{0}/s 収入";
            d[LocKeys.UpgradeEffectSlot]    = "+{0} 制作スロット";
            d[LocKeys.UpgradeEffectCatalog] = "カタログ Lv.{0}";

            // Awards
            d[LocKeys.AwardsStarTitleFmt]  = "スター #{0}";
            d[LocKeys.AwardsRepReadyFmt]   = "評判: {0:N0} / {1:N0} REP ✓";
            d[LocKeys.AwardsUnlockHintFmt] = "{0}  ·  次まであと {1}";

            // City
            d[LocKeys.InstGlobalBonus]    = "グローバルボーナス";
            d[LocKeys.InstGlobalBonusFmt] = "×{0:0.00} 収入";
            d[LocKeys.InstCurrentUnlocks] = "現在の解放";
            d[LocKeys.InstMaxCity]        = "映画帝国に到達しました！";

            // Production
            d[LocKeys.ProdProduceBtn] = "制作";
            d[LocKeys.ProdEmptyState] = "映画がありません。";

            // Collection stats
            d[LocKeys.CollStatLvlFmt] = "Lv.{0}";

            // Premiere
            d[LocKeys.PremiereVarietyFmt]   = "バラエティ +{0:0}%";
            d[LocKeys.PremiereFilmFound]    = "映画発見";
            d[LocKeys.PremiereContractDone] = "契約完了";
            d[LocKeys.PremiereContractProg] = "契約 · {0:0}/{1:0}";

            // GameFeel
            d[LocKeys.GameFeelUpgradeFallback] = "アップグレード獲得";

            // Store
            d[LocKeys.StoreSectionDiamonds] = "ダイヤモンド";
            d[LocKeys.StoreSectionPacks]    = "› パック";
            d[LocKeys.StoreSectionBoosts]   = "› ブースト";
            d[LocKeys.StoreSectionPremium]  = "› プレミアム";
            d[LocKeys.StoreWatchAd]         = "広告を見る";
            d[LocKeys.StoreStarterDesc]     = "ダイヤ + ブースト + 広告なし 24h";
            d[LocKeys.StoreUniqueOffer]     = "限定オファー";
            d[LocKeys.StoreNoAds]           = "広告なし";
            d[LocKeys.StoreNoAdsDesc]       = "広告を永久に削除";

            // Store products
            d[LocKeys.StoreProdDiamSmall]    = "少量";
            d[LocKeys.StoreProdDiamMed]      = "袋";
            d[LocKeys.StoreProdDiamLarge]    = "宝箱";
            d[LocKeys.StoreProdPackDir]      = "監督パック";
            d[LocKeys.StoreProdPackDirDesc]  = "ダイヤ＋ブースト\n＋レアムービー";
            d[LocKeys.StoreProdPackStar]     = "スターパック";
            d[LocKeys.StoreProdPackStarDesc] = "ダイヤ＋REP\n＋エピックムービー";
            d[LocKeys.StoreProdBoostProd]    = "制作 x2";
            d[LocKeys.StoreProdBoostInc]     = "収入 x2";
            d[LocKeys.SettingsComing]        = "まもなく";
            // Studio subtabs
            d[LocKeys.StudTabDepts]     = "部門";
            d[LocKeys.StudTabMejoras]   = "強化";
            d[LocKeys.StudTabContratos] = "契約";

            // Credits
            d[LocKeys.CreditsCreatedBy]  = "作者";
            d[LocKeys.CreditsVersion]    = "バージョン {0}";
            d[LocKeys.CreditsPoweredBy]  = "Unity 製";
            d[LocKeys.IncomePerSecFmt]   = "+${0:N0}/秒";

            // Premiere headline
            d[LocKeys.PremiereHeadline]  = "プレミア上映";

            // City / Facility names
            d[LocKeys.CityNamePfx + "1"] = "ガレージ";
            d[LocKeys.CityNamePfx + "2"] = "インディー系スタジオ";
            d[LocKeys.CityNamePfx + "3"] = "地元スタジオ";
            d[LocKeys.CityNamePfx + "4"] = "地域スタジオ";
            d[LocKeys.CityNamePfx + "5"] = "大手スタジオ";
            d[LocKeys.CityNamePfx + "6"] = "ハリウッド・ブールバード";
            d[LocKeys.CityNamePfx + "7"] = "メジャースタジオ";
            d[LocKeys.CityNamePfx + "8"] = "映画帝国";

            // Upgrade names
            d[LocKeys.UpgradeNamePfx + "equip_camera_basic"]    = "基本カメラ";
            d[LocKeys.UpgradeNamePfx + "equip_camera_digital"]  = "デジタルカメラ（上位）";
            d[LocKeys.UpgradeNamePfx + "equip_camera_pro"]      = "プロカメラ";
            d[LocKeys.UpgradeNamePfx + "equip_coloring"]        = "カラーグレーディング";
            d[LocKeys.UpgradeNamePfx + "equip_dolby"]           = "Dolby Atmosプロセッサー";
            d[LocKeys.UpgradeNamePfx + "equip_drone"]           = "ドローンシステム";
            d[LocKeys.UpgradeNamePfx + "equip_editing_soft"]    = "プロ編集ソフト";
            d[LocKeys.UpgradeNamePfx + "equip_lighting_kit"]    = "照明キット";
            d[LocKeys.UpgradeNamePfx + "equip_lighting_led"]    = "映像LEDシステム";
            d[LocKeys.UpgradeNamePfx + "equip_mic_pro"]         = "プロマイク";
            d[LocKeys.UpgradeNamePfx + "equip_render_farm"]     = "レンダーファーム";
            d[LocKeys.UpgradeNamePfx + "equip_sound_surround"]  = "サラウンドサウンド";
            d[LocKeys.UpgradeNamePfx + "equip_steadicam"]       = "ステディカムPro";
            d[LocKeys.UpgradeNamePfx + "equip_vfx_advanced"]    = "上位VFXスイート";
            d[LocKeys.UpgradeNamePfx + "equip_vfx_basic"]       = "基本VFXステーション";
            d[LocKeys.UpgradeNamePfx + "install_editing_room"]  = "編集室";
            d[LocKeys.UpgradeNamePfx + "install_exterior"]      = "屋外スタジオ";
            d[LocKeys.UpgradeNamePfx + "install_offices"]       = "プロダクションオフィス";
            d[LocKeys.UpgradeNamePfx + "install_plato_large"]   = "大型撮影セット";
            d[LocKeys.UpgradeNamePfx + "install_plato_medium"]  = "中型撮影セット";
            d[LocKeys.UpgradeNamePfx + "install_plato_small"]   = "小型撮影セット";
            d[LocKeys.UpgradeNamePfx + "install_screening"]     = "試写室";
            d[LocKeys.UpgradeNamePfx + "install_studio_lot"]    = "スタジオ敷地";
            d[LocKeys.UpgradeNamePfx + "install_vfx_dept"]      = "VFX部門";
            d[LocKeys.UpgradeNamePfx + "mkt_global"]            = "グローバル配信";
            d[LocKeys.UpgradeNamePfx + "mkt_press_kit"]         = "プレスキャンペーン";
            d[LocKeys.UpgradeNamePfx + "mkt_social_media"]      = "SNSマネージャー";
            d[LocKeys.UpgradeNamePfx + "mkt_trailer"]           = "予告編部門";
            d[LocKeys.UpgradeNamePfx + "personal_actors"]       = "俳優";
            d[LocKeys.UpgradeNamePfx + "personal_art"]          = "美術部";
            d[LocKeys.UpgradeNamePfx + "personal_cinematography"]= "撮影監督";
            d[LocKeys.UpgradeNamePfx + "personal_composer"]     = "作曲家";
            d[LocKeys.UpgradeNamePfx + "personal_costume"]      = "衣装";
            d[LocKeys.UpgradeNamePfx + "personal_director"]     = "監督";
            d[LocKeys.UpgradeNamePfx + "personal_editor"]       = "編集者";
            d[LocKeys.UpgradeNamePfx + "personal_grip"]         = "グリップ";
            d[LocKeys.UpgradeNamePfx + "personal_lighting"]     = "照明技術者";
            d[LocKeys.UpgradeNamePfx + "personal_makeup"]       = "メイクアップ";
            d[LocKeys.UpgradeNamePfx + "personal_producer"]     = "エグゼクティブPR";
            d[LocKeys.UpgradeNamePfx + "personal_publicist"]    = "広報担当";
            d[LocKeys.UpgradeNamePfx + "personal_sound"]        = "サウンドTECH";

            // Contract titles
            d[LocKeys.ContractTitlePfx + "c_three_movies"]      = "短編映画祭";
            d[LocKeys.ContractTitlePfx + "c_first_reputation"]  = "最初のファン";
            d[LocKeys.ContractTitlePfx + "c_first_movie"]       = "最初の撮影";
            d[LocKeys.ContractTitlePfx + "c_action_fan"]        = "アクション映画ファン";
            d[LocKeys.ContractTitlePfx + "c_romance_story"]     = "ラブストーリー";
            d[LocKeys.ContractTitlePfx + "c_first_upgrade"]     = "スタジオへの投資";
            d[LocKeys.ContractTitlePfx + "c_fantasy_realm"]     = "ファンタジー王国";
            d[LocKeys.ContractTitlePfx + "c_thriller_night"]    = "スリラーの夜";
            d[LocKeys.ContractTitlePfx + "c_documentary_truth"] = "映画の真実";
            d[LocKeys.ContractTitlePfx + "c_drama_fan"]         = "ドラマファン";
            d[LocKeys.ContractTitlePfx + "c_animation_studio"]  = "アニメスタジオ";
            d[LocKeys.ContractTitlePfx + "c_rep_50"]            = "新星";
            d[LocKeys.ContractTitlePfx + "c_horror_night"]      = "ホラーの夜";
            d[LocKeys.ContractTitlePfx + "c_speed_run"]         = "スピード制作";
            d[LocKeys.ContractTitlePfx + "c_studio_level5"]     = "確立したスタジオ";
            d[LocKeys.ContractTitlePfx + "c_rep_200"]           = "業界の基準";
            d[LocKeys.ContractTitlePfx + "c_comedy_king"]       = "コメディの王";
            d[LocKeys.ContractTitlePfx + "c_studio_level10"]    = "確立した制作会社";
            d[LocKeys.ContractTitlePfx + "c_ten_movies"]        = "映画マラソン";
            d[LocKeys.ContractTitlePfx + "c_scifi_pioneer"]     = "SFのパイオニア";
            d[LocKeys.ContractTitlePfx + "c_studio_level15"]    = "大スタジオ";
            d[LocKeys.ContractTitlePfx + "c_rep_1000"]          = "映画の伝説";
            d[LocKeys.ContractTitlePfx + "c_all_genres"]        = "ボーダレス映画";
            d[LocKeys.ContractTitlePfx + "c_big_spender"]       = "大投資";
            d[LocKeys.ContractTitlePfx + "c_studio_level20"]    = "国際スタジオ";
            d[LocKeys.ContractTitlePfx + "c_rep_5000"]          = "グローバルアイコン";
            d[LocKeys.ContractTitlePfx + "c_blockbuster"]       = "ビッグブロックバスター";

            // Oscar / installation progress
            d[LocKeys.InstOscarProgress]  = "{0} / {1} OSC";
            d[LocKeys.InstOscarMax]       = "{0} OSC · 最大";
            d[LocKeys.InstUnlockGlobal]   = "×{0:0.00} 収入";
            d[LocKeys.InstUnlockDept]     = "部門: {0}";
            d[LocKeys.InstTierPfx + "1"]  = "Tier 1–2 · 基本映画";
            d[LocKeys.InstTierPfx + "2"]  = "続編とリメイク";
            d[LocKeys.InstTierPfx + "3"]  = "プロ制作";
            d[LocKeys.InstTierPfx + "4"]  = "上位映画";
            d[LocKeys.InstTierPfx + "5"]  = "キャンペーン＆配信";
            d[LocKeys.InstTierPfx + "6"]  = "VFX＆賞";
            d[LocKeys.InstTierPfx + "7"]  = "配信＆シネマユニバース";
            d[LocKeys.InstTierPfx + "8"]  = "帝国完成";

            // City lock labels
            d[LocKeys.CityLockLabel]      = "施設{0}が必要";

            // Offer card badges
            d[LocKeys.OfferBadgeNew]      = "[新作]";
            d[LocKeys.OfferBadgeSaga]     = "[SAGA]";
            d[LocKeys.OfferBadgeContract] = "[契約]";

            // Saga labels
            d[LocKeys.SagaCompleted]   = "{0}/{1} 完了";
            d[LocKeys.SagaBlockReason] = "最初にサーガの第{0}話を完成させてください";

            // Ad reward feedback
            d[LocKeys.AdLimitReached]        = "本日の上限に達しました";
            d[LocKeys.FreeDiamondsAwarded]   = "+{0} ダイヤ獲得";
            d[LocKeys.BoostActivated]        = "ブースト起動！10分間 ×2";
            d[LocKeys.BoostAlreadyActive]    = "このブーストはすでに有効です";
            d[LocKeys.InvestorAwarded]       = "投資家から +${0:N0}";
            d[LocKeys.InvestorNoIncome]      = "現在の収入は0です。もっと映画を制作してください。";
            d[LocKeys.StoreSectionInvestor]  = "投資家";
            d[LocKeys.StoreSectionFreeRewards] = "デイリー報酬";
            d[LocKeys.StoreProdBoostRep]     = "REPブースト";
            d[LocKeys.StoreProdBoostXP]      = "XPブースト";
            d[LocKeys.StoreProdFreeDiam]     = "無料";
            d[LocKeys.StoreProdInvestor]     = "投資家";
            d[LocKeys.StoreProdOfflinePremium] = "リモートプロデューサー";
            d[LocKeys.StoreBoostTimerFmt]    = "{0}分 {1}秒";
            d[LocKeys.StoreBoostActive]      = "有効 ·";
            d[LocKeys.StorePackStarter]      = "スターターパック";
            d[LocKeys.StorePackSupporter]    = "サポーター";
            d[LocKeys.StorePackProducer]     = "プロデューサー";
            d[LocKeys.StorePackExecutive]    = "エグゼクティブプロデューサー";
            d[LocKeys.StorePackAlreadyOwned] = "購入済み";
            d[LocKeys.StoreSimBuy]           = "[購入シミュレート]";
            d[LocKeys.OfflinePremiumName]        = "リモートプロデューサー";
            d[LocKeys.OfflinePremiumDesc]        = "オフライン上限を2時間に拡張";
            d[LocKeys.OfflinePremiumOwned]       = "解除済み";
            d[LocKeys.OfflinePremiumCostPending] = "価格未定";
            d[LocKeys.PackSupporterDesc]  = "Film Producer Tycoonをご支援ありがとうございます。";
            d[LocKeys.PackProducerDesc]   = "プロデューサーとしてのキャリアを大きなダイヤモンド備蓄で加速しましょう。";
            d[LocKeys.PackExecutiveDesc]  = "真の映画界の大物のための究極のエディション。";
            d[LocKeys.InvestorCooldownFmt] = "CD · {0}分 {1}秒";
            d[LocKeys.InvestorReady]       = "5分の収入";
            return d;
        }
    }
}

public static class DepartmentLoc
{
    public static string GetName(DepartmentType type) =>
        Loc.Get(DepartmentLocKeys.NameKey(type));

    public static string GetCategoryLabel(string legacyCategory) => legacyCategory switch
    {
        "Calidad"    => Loc.Get(LocKeys.DeptCategoryQuality),
        "Velocidad"  => Loc.Get(LocKeys.DeptCategorySpeed),
        "Reducción"  => Loc.Get(LocKeys.DeptCategoryReduction),
        _            => legacyCategory,
    };
}
