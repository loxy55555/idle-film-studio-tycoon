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

            { LocKeys.PremiereCompleted, "Estreno completado" },
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
            { LocKeys.SettingsClose,      "CERRAR" },

            // Tienda
            { LocKeys.TiendaTitle,      "TIENDA" },
            { LocKeys.TiendaComingSoon, "Próximamente" },
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
            d[LocKeys.SettingsClose]      = "CLOSE";
            d[LocKeys.TiendaTitle]      = "SHOP";
            d[LocKeys.TiendaComingSoon] = "Coming soon";
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
            d[LocKeys.ProdSelect] = "SÉLECTIONNER";
            d[LocKeys.ProdCatalogComplete] = "Catalogue terminé";
            d[LocKeys.ProdNoOffer] = "Aucune offre disponible";
            d[LocKeys.ProdBudgetTitle] = "BUDGET";
            d[LocKeys.ProdBudgetSubtitle] = "Choisissez comment produire ce film";
            d[LocKeys.ProdBudgetCheap] = "BAS";
            d[LocKeys.ProdBudgetStandard] = "NORMAL";
            d[LocKeys.ProdBudgetPremium] = "PREMIUM";
            d[LocKeys.ProdBudgetCancel] = "ANNULER";
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
            d[LocKeys.ProdSelect] = "AUSWÄHLEN";
            d[LocKeys.ProdCatalogComplete] = "Katalog abgeschlossen";
            d[LocKeys.ProdNoOffer] = "Kein Angebot verfügbar";
            d[LocKeys.ProdBudgetTitle] = "BUDGET";
            d[LocKeys.ProdBudgetSubtitle] = "Wähle die Produktionsart";
            d[LocKeys.ProdBudgetCheap] = "NIEDRIG";
            d[LocKeys.ProdBudgetStandard] = "NORMAL";
            d[LocKeys.ProdBudgetPremium] = "PREMIUM";
            d[LocKeys.ProdBudgetCancel] = "ABBRECHEN";
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
            d[LocKeys.ProdSelect] = "選択";
            d[LocKeys.ProdCatalogComplete] = "カタログ完了";
            d[LocKeys.ProdNoOffer] = "オファーなし";
            d[LocKeys.ProdBudgetTitle] = "予算";
            d[LocKeys.ProdBudgetSubtitle] = "制作方法を選んでください";
            d[LocKeys.ProdBudgetCheap] = "低";
            d[LocKeys.ProdBudgetStandard] = "標準";
            d[LocKeys.ProdBudgetPremium] = "プレミアム";
            d[LocKeys.ProdBudgetCancel] = "キャンセル";
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
