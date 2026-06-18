using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > Idle Film > Generate Missing Synopses
/// Fills synopsis fields (ES + EN + FR + DE + JA) on every MovieConfig that currently lacks them.
/// Does NOT overwrite existing synopses, does NOT touch any gameplay data.
///
/// Memory cost: ~300–600 bytes per movie per language (plain UTF-8 strings stored in ScriptableObject).
/// For a catalog of 80 movies × 5 languages = ~200 KB of text data — negligible at runtime.
/// All synopsis data is resident in memory only while the Collection screen holds the MovieConfig
/// references; they are not streamed or lazy-loaded but the total footprint is tiny.
/// </summary>
public static class GenerateMissingSynopsesEditor
{
    /// <summary>Silent version — no dialog, no undo. Safe to call from editor scripts or MCP.</summary>
    [MenuItem("Tools/Idle Film/Generate Missing Synopses (Silent)")]
    public static void ExecuteSilent()
    {
        var guids = AssetDatabase.FindAssets("t:MovieConfig", new[] { "Assets/Data/Movies" });
        int updated = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var cfg  = AssetDatabase.LoadAssetAtPath<MovieConfig>(path);
            if (cfg == null) continue;
            bool dirty = false;
            if (string.IsNullOrWhiteSpace(cfg.synopsis))   { cfg.synopsis   = BuildSynopsis(cfg, Language.ES); dirty = true; }
            if (string.IsNullOrWhiteSpace(cfg.synopsisEn)) { cfg.synopsisEn = BuildSynopsis(cfg, Language.EN); dirty = true; }
            if (string.IsNullOrWhiteSpace(cfg.synopsisFr)) { cfg.synopsisFr = BuildSynopsis(cfg, Language.FR); dirty = true; }
            if (string.IsNullOrWhiteSpace(cfg.synopsisDe)) { cfg.synopsisDe = BuildSynopsis(cfg, Language.DE); dirty = true; }
            if (string.IsNullOrWhiteSpace(cfg.synopsisJa)) { cfg.synopsisJa = BuildSynopsis(cfg, Language.JA); dirty = true; }
            if (dirty) { EditorUtility.SetDirty(cfg); updated++; }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GenerateMissingSynopses] Done. Updated {updated}/{guids.Length} MovieConfig assets.");
    }

    /// <summary>FASE 16.4 — overwrite Spanish synopsis fields that contain English/tagline copy.</summary>
    [MenuItem("Tools/Idle Film/Fix Mislabeled Spanish Synopses (Silent)")]
    public static void FixMislabeledSpanishSilent()
    {
        var guids = AssetDatabase.FindAssets("t:MovieConfig", new[] { "Assets/Data/Movies" });
        int fixedCount = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var cfg  = AssetDatabase.LoadAssetAtPath<MovieConfig>(path);
            if (cfg == null) continue;
            if (!SynopsisDataQuality.IsMislabeledSpanish(cfg.synopsis, cfg)) continue;
            cfg.synopsis = GenerateFromTemplate(cfg, Language.ES);
            EditorUtility.SetDirty(cfg);
            fixedCount++;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[FixMislabeledSpanishSynopses] Fixed {fixedCount}/{guids.Length} MovieConfig assets.");
    }

    [MenuItem("Tools/Idle Film/Generate Missing Synopses")]
    public static void Execute()
    {
        // ── 1. Collect all MovieConfig assets ───────────────────────────────────
        var guids = AssetDatabase.FindAssets("t:MovieConfig", new[] { "Assets/Data/Movies" });

        var needsAny = new List<(MovieConfig cfg, string path)>();
        int esCount = 0, enCount = 0, frCount = 0, deCount = 0, jaCount = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var cfg  = AssetDatabase.LoadAssetAtPath<MovieConfig>(path);
            if (cfg == null) continue;

            bool missing = string.IsNullOrWhiteSpace(cfg.synopsis)   ||
                           string.IsNullOrWhiteSpace(cfg.synopsisEn) ||
                           string.IsNullOrWhiteSpace(cfg.synopsisFr) ||
                           string.IsNullOrWhiteSpace(cfg.synopsisDe) ||
                           string.IsNullOrWhiteSpace(cfg.synopsisJa);
            if (missing)
            {
                needsAny.Add((cfg, path));
                if (string.IsNullOrWhiteSpace(cfg.synopsis))   esCount++;
                if (string.IsNullOrWhiteSpace(cfg.synopsisEn)) enCount++;
                if (string.IsNullOrWhiteSpace(cfg.synopsisFr)) frCount++;
                if (string.IsNullOrWhiteSpace(cfg.synopsisDe)) deCount++;
                if (string.IsNullOrWhiteSpace(cfg.synopsisJa)) jaCount++;
            }
        }

        int total   = guids.Length;
        int toWrite = needsAny.Count;
        int toSkip  = total - toWrite;

        // ── 2. Confirm before applying ──────────────────────────────────────────
        bool proceed = EditorUtility.DisplayDialog(
            "Generate Missing Synopses",
            $"MovieConfig assets found:     {total}\n" +
            $"Assets needing any synopsis:  {toWrite}\n" +
            $"Already complete:             {toSkip}\n\n" +
            $"Missing by language:\n" +
            $"  ES: {esCount}  EN: {enCount}  FR: {frCount}  DE: {deCount}  JA: {jaCount}\n\n" +
            "Proceed? (Existing synopses will NOT be overwritten.)",
            "Generate", "Cancel");

        if (!proceed) return;

        // ── 3. Register Undo for all modified objects ───────────────────────────
        var objects = new UnityEngine.Object[toWrite];
        for (int i = 0; i < toWrite; i++)
            objects[i] = needsAny[i].cfg;

        Undo.RecordObjects(objects, "Generate Missing Synopses");

        // ── 4. Generate & apply ─────────────────────────────────────────────────
        var modifiedPaths = new List<string>(toWrite);

        foreach (var (cfg, path) in needsAny)
        {
            if (string.IsNullOrWhiteSpace(cfg.synopsis))   cfg.synopsis   = BuildSynopsis(cfg, Language.ES);
            if (string.IsNullOrWhiteSpace(cfg.synopsisEn)) cfg.synopsisEn = BuildSynopsis(cfg, Language.EN);
            if (string.IsNullOrWhiteSpace(cfg.synopsisFr)) cfg.synopsisFr = BuildSynopsis(cfg, Language.FR);
            if (string.IsNullOrWhiteSpace(cfg.synopsisDe)) cfg.synopsisDe = BuildSynopsis(cfg, Language.DE);
            if (string.IsNullOrWhiteSpace(cfg.synopsisJa)) cfg.synopsisJa = BuildSynopsis(cfg, Language.JA);
            EditorUtility.SetDirty(cfg);
            modifiedPaths.Add(path);
        }

        // ── 5. Save ──────────────────────────────────────────────────────────────
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ── 6. Report ────────────────────────────────────────────────────────────
        var sb = new StringBuilder();
        sb.AppendLine("=== Generate Missing Synopses — Report ===");
        sb.AppendLine($"Total MovieConfig assets:  {total}");
        sb.AppendLine($"Assets updated:            {toWrite}");
        sb.AppendLine($"Skipped (already complete):{toSkip}");
        sb.AppendLine($"\nModified assets ({modifiedPaths.Count}):");
        foreach (var p in modifiedPaths)
            sb.AppendLine($"  · {System.IO.Path.GetFileName(p)}");

        Debug.Log(sb.ToString());

        EditorUtility.DisplayDialog(
            "Synopses Generated",
            $"Complete.\n\nUpdated:  {toWrite}\nSkipped:  {toSkip}",
            "OK");
    }

    // ── Language enum ───────────────────────────────────────────────────────────
    enum Language { ES, EN, FR, DE, JA }

    // ── Generation logic ────────────────────────────────────────────────────────

    static string BuildSynopsis(MovieConfig cfg, Language lang)
    {
        if (lang == Language.ES)
            return GenerateFromTemplate(cfg, lang);

        if (lang == Language.EN && !string.IsNullOrWhiteSpace(cfg.tagline) && cfg.tagline.Trim().Length > 30)
        {
            var t = cfg.tagline.Trim();
            if (!t.EndsWith(".") && !t.EndsWith("!") && !t.EndsWith("?")) t += ".";
            return t;
        }
        return GenerateFromTemplate(cfg, lang);
    }

    /// <summary>Picks a template deterministically based on movie name hash.</summary>
    static string GenerateFromTemplate(MovieConfig cfg, Language lang)
    {
        bool highRarity = cfg.rarity == MovieRarity.Legendary || cfg.rarity == MovieRarity.Epic;
        string[] pool = GetPool(cfg.genre, highRarity, lang);

        int idx = Math.Abs(cfg.movieName.GetHashCode()) % pool.Length;
        return pool[idx].Replace("{name}", cfg.movieName);
    }

    // ── Template pools (8 standard + 3 high-rarity per genre, per language) ────

    static string[] GetPool(MovieGenre genre, bool highRarity, Language lang)
    {
        if (lang == Language.EN) return GetPoolEN(genre, highRarity);
        if (lang == Language.FR) return GetPoolFR(genre, highRarity);
        if (lang == Language.DE) return GetPoolDE(genre, highRarity);
        if (lang == Language.JA) return GetPoolJA(genre, highRarity);
        return GetPoolES(genre, highRarity);
    }

    // ── Spanish (original) ──────────────────────────────────────────────────────
    static string[] GetPoolES(MovieGenre genre, bool highRarity)
    {
        string[] standard;
        string[] epic;

        switch (genre)
        {
            case MovieGenre.Action:
                standard = new[]
                {
                    "{name} sigue a una unidad de élite que debe actuar en la sombra para evitar una catástrofe de consecuencias imprevisibles.",
                    "Cuando el tiempo se agota y el enemigo está más cerca de lo previsto, solo la determinación marca la diferencia.",
                    "Una operación encubierta al límite de lo posible: cada decisión tiene un coste y ningún error es perdonable.",
                    "En un escenario de alta tensión, un grupo de combatientes libra la batalla más importante de sus vidas.",
                    "{name} pone a prueba los límites humanos en una misión donde el fracaso no es una opción.",
                    "La adrenalina y la estrategia se combinan en una carrera contrarreloj que no deja espacio para el respiro.",
                    "Cuando el deber y la supervivencia entran en conflicto, solo los más valientes pueden mantener el rumbo.",
                    "Una persecución implacable a través de entornos extremos donde el límite entre la victoria y el desastre es una línea muy fina.",
                };
                epic = new[]
                {
                    "{name} eleva el género de la acción a cotas épicas con una confrontación que redefine el significado del sacrificio.",
                    "Una misión de alcance global cuya resolución determinará el equilibrio de poder durante décadas.",
                    "La operación más ambiciosa de la historia reciente enfrenta a sus protagonistas con lo peor de la condición humana.",
                };
                break;

            case MovieGenre.Drama:
                standard = new[]
                {
                    "Una historia de pérdida, redención y esperanza que retrata con honestidad la complejidad del ser humano.",
                    "Cuando las certezas se desmoronan, los personajes deben encontrar la fuerza para seguir adelante sin perder lo esencial.",
                    "{name} explora los vínculos que nos definen y las decisiones imposibles que determinan el curso de una vida entera.",
                    "El pasado regresa de formas inesperadas, exigiendo respuestas que no todos están preparados para dar.",
                    "Entre el amor, el dolor y la esperanza, los protagonistas descubren quiénes son realmente cuando más importa.",
                    "Una travesía emocional que desafía las convenciones y celebra la resiliencia del espíritu humano.",
                    "{name} retrata con sensibilidad las cicatrices invisibles de quienes eligen vivir con integridad frente a la adversidad.",
                    "Decisiones irrevocables, consecuencias profundas y la búsqueda incesante de un sentido en el caos cotidiano.",
                };
                epic = new[]
                {
                    "{name} es una obra de una envergadura emocional extraordinaria que trasciende la pantalla para interpelar al espectador.",
                    "Una historia generacional que abarca décadas, vidas y legados en un fresco humano de proporciones monumentales.",
                    "El drama más poderoso es el que refleja con exactitud lo que todos sentimos pero pocos somos capaces de expresar.",
                };
                break;

            case MovieGenre.Horror:
                standard = new[]
                {
                    "Una presencia desconocida comienza a manifestarse en las sombras, y quienes la descubren no vuelven a ser los mismos.",
                    "Cuando el terror más profundo cobra forma, huir deja de ser suficiente para estar a salvo.",
                    "{name} conduce al espectador hacia un lugar donde la razón no alcanza y el miedo gobierna cada instante.",
                    "Algo antiguo y olvidado despierta, y con ello resurgen secretos que nunca debieron salir a la luz.",
                    "En un entorno aislado del mundo, un grupo descubre que el verdadero peligro siempre estuvo entre ellos.",
                    "Las reglas del mundo conocido dejan de aplicarse cuando lo imposible se convierte en lo cotidiano.",
                    "{name} construye una atmósfera de tensión creciente que no abandona al espectador hasta el último instante.",
                    "Bajo la superficie de lo ordinario se oculta un horror que cambiará para siempre la vida de sus protagonistas.",
                };
                epic = new[]
                {
                    "{name} lleva el horror a territorios inexplorados con una perturbación que cuestiona los fundamentos de la percepción.",
                    "Un terror primigenio y sin nombre emerge para recordar que no todo lo que existe en el mundo puede ser comprendido.",
                    "La oscuridad tiene memoria, y cuando decide reclamar lo que considera suyo, nada puede interponerse en su camino.",
                };
                break;

            case MovieGenre.Comedy:
                standard = new[]
                {
                    "Lo que empieza como un plan perfecto se convierte rápidamente en la cadena de desastres más hilarante imaginable.",
                    "Cuando el azar une a personajes incompatibles, el resultado solo puede ser el caos más divertido del año.",
                    "{name} lleva al límite la capacidad de improvisación de sus protagonistas en situaciones cada vez más absurdas.",
                    "Malentendidos, coincidencias imposibles y decisiones cuestionables: los ingredientes de una comedia que no da tregua.",
                    "Un viaje repleto de sorpresas que demuestra que a veces el mejor plan es no tener ninguno.",
                    "La vida tiene una forma peculiar de complicarse justo cuando más necesitas que todo salga bien.",
                    "{name} celebra el humor más inesperado con una historia que provoca la carcajada en el momento menos previsto.",
                    "Personajes entrañables y situaciones imposibles se combinan en una aventura tan caótica como inolvidable.",
                };
                epic = new[]
                {
                    "{name} eleva la comedia a una dimensión de absurdo tan preciso y tan bien ejecutado que resulta casi virtuoso.",
                    "Una farsa de proporciones colosales que ríe de todo y de todos con una inteligencia fuera de lo común.",
                    "El humor como forma de resistencia: una comedia que esconde bajo la carcajada una mirada profunda sobre el mundo.",
                };
                break;

            case MovieGenre.Romance:
                standard = new[]
                {
                    "Dos caminos que nunca debieron cruzarse se encuentran en el momento más inesperado de sus vidas.",
                    "Entre el amor y las circunstancias adversas, algunos lazos resultan imposibles de romper con el paso del tiempo.",
                    "{name} es una historia de encuentros y despedidas que demuestra que algunas conexiones trascienden el tiempo.",
                    "Cuando el corazón decide, ni la distancia ni el pasado pueden impedir lo que estaba destinado a ocurrir.",
                    "Una historia que explora la vulnerabilidad y la valentía de quienes se atreven a amar sin garantías.",
                    "El amor, cuando es auténtico, encuentra la manera de florecer incluso en las condiciones más adversas.",
                    "{name} narra con delicadeza y emoción la historia de dos personas que aprenden a verse realmente.",
                    "Entre malentendidos y revelaciones, dos almas descubren que lo que buscaban siempre estuvo más cerca de lo imaginado.",
                };
                epic = new[]
                {
                    "{name} es un romance de una intensidad y una belleza que raramente se alcanzan en el género.",
                    "Una historia de amor que abarca continentes, décadas y vidas enteras sin perder un ápice de su emoción.",
                    "El amor como fuerza que transforma el mundo: una historia épica disfrazada de historia íntima.",
                };
                break;

            case MovieGenre.SciFi:
                standard = new[]
                {
                    "En un futuro marcado por la incertidumbre, una expedición arriesgada revela verdades que cambian el curso de la historia.",
                    "Cuando la ciencia alcanza lo impensable, las preguntas más importantes siguen sin una respuesta satisfactoria.",
                    "{name} plantea un escenario donde los límites de lo posible se desvanecen y la realidad misma se vuelve cuestionable.",
                    "Una civilización al borde de su propio abismo descubre que el mayor peligro siempre provino de dentro.",
                    "El espacio, el tiempo y la conciencia se convierten en el campo de batalla de una historia sin precedentes.",
                    "Más allá de los confines del mundo conocido aguardan respuestas que la humanidad nunca estuvo preparada para escuchar.",
                    "{name} construye un universo fascinante donde cada avance tiene un precio que no todos están dispuestos a pagar.",
                    "Una mente brillante y un descubrimiento inesperado ponen en marcha una cadena de eventos que nadie puede detener.",
                };
                epic = new[]
                {
                    "{name} redefine el género con una visión de la ciencia ficción que trasciende la especulación para tocar lo filosófico.",
                    "Una odisea interestelar de proporciones míticas que sitúa a la humanidad frente a las preguntas definitivas de su existencia.",
                    "La ambición de {name} no tiene límites: un universo construido con una precisión y una imaginación que deja sin palabras.",
                };
                break;

            case MovieGenre.Fantasy:
                standard = new[]
                {
                    "En tierras donde la magia y el destino se entrelazan, un héroe improbable emprende el viaje de su vida.",
                    "Una profecía ancestral se cumple entre reinos en guerra y poderes que el mundo creía extintos para siempre.",
                    "{name} despliega un mundo de maravillas y peligros donde solo el coraje puede inclinar la balanza.",
                    "Cuando las fuerzas del bien y el mal colisionan, el resultado determinará el futuro de todo un mundo.",
                    "Un poder olvidado resurge en el momento más crítico, poniendo a prueba la fe y la valentía de quienes lo portan.",
                    "En un universo donde los imposibles son cotidianos, la mayor aventura comienza con un simple paso en la oscuridad.",
                    "{name} es un relato épico de sacrificio, lealtad y la búsqueda de un destino que nadie puede eludir.",
                    "Las leyendas cobran vida cuando quienes las protagonizan descubren que el mundo necesita exactamente su historia.",
                };
                epic = new[]
                {
                    "{name} es una epopeya fantástica de una escala y una ambición que muy pocas obras del género pueden igualar.",
                    "Un universo construido con la riqueza de un mito y la profundidad de una saga que trasciende generaciones.",
                    "La fantasía como espejo del alma humana: una historia que habla de quiénes somos a través de lo que imaginamos.",
                };
                break;

            case MovieGenre.Thriller:
                standard = new[]
                {
                    "La verdad está más cerca de lo que parece, pero descubrirla podría resultar más peligroso que la propia mentira.",
                    "Cada respuesta abre una nueva pregunta en una espiral de secretos que amenaza con destruirlo todo.",
                    "{name} mantiene al espectador en tensión constante con giros imprevisibles que redefinen lo que se cree saber.",
                    "Cuando las apariencias engañan y la confianza es un lujo que nadie puede permitirse, solo queda el instinto.",
                    "Una conspiración cuidadosamente construida comienza a desmoronarse cuando la persona equivocada descubre demasiado.",
                    "El pasado, los secretos y las lealtades mal entendidas convergen en un clímax que nadie anticipará.",
                    "{name} es un relato de intrigas y traiciones donde la línea entre el bien y el mal se difumina sin remedio.",
                    "En un juego de sombras donde todos tienen algo que ocultar, sobrevivir depende de descubrir la verdad el primero.",
                };
                epic = new[]
                {
                    "{name} lleva el thriller a un nivel de complejidad narrativa y tensión sostenida difícilmente igualable.",
                    "Una conspiración de dimensiones globales cuya resolución reescribirá la historia tal y como la conocemos.",
                    "La paranoia como motor dramático: un thriller que convierte al espectador en cómplice y en sospechoso al mismo tiempo.",
                };
                break;

            case MovieGenre.Animation:
                standard = new[]
                {
                    "Un grupo de amigos extraordinarios vive una aventura que pondrá a prueba su valentía y su amistad de formas inesperadas.",
                    "En un mundo repleto de maravillas, la imaginación es la única herramienta que nunca falla cuando más se necesita.",
                    "{name} lleva al espectador a un universo colorido y emocionante donde los sueños y los desafíos conviven.",
                    "Una historia que celebra la amistad, la creatividad y la capacidad de encontrar magia en lo más cotidiano.",
                    "Lo que comienza como una pequeña aventura pronto se convierte en el viaje más importante de sus protagonistas.",
                    "Con corazón y determinación, incluso los más pequeños pueden cambiar el rumbo de las cosas cuando se lo proponen.",
                    "{name} combina emoción, humor y ternura en una historia que conecta con el público de todas las edades.",
                    "Cuando la valentía y la amistad se unen, no existe obstáculo demasiado grande ni sueño demasiado lejano.",
                };
                epic = new[]
                {
                    "{name} redefine lo que la animación puede contar con una historia de una profundidad emocional fuera de lo común.",
                    "Una obra de animación que trasciende su formato para convertirse en una experiencia cinematográfica de primer nivel.",
                    "La animación como lenguaje universal: una historia que habla sin barreras de lo más íntimo y lo más universal.",
                };
                break;

            case MovieGenre.Documentary:
            default:
                standard = new[]
                {
                    "Una mirada fascinante y rigurosa a los protagonistas y momentos que transformaron para siempre una época.",
                    "Detrás de cada gran acontecimiento hay historias humanas extraordinarias que merecen ser contadas y recordadas.",
                    "{name} ofrece una perspectiva única y cercana sobre un fenómeno que sigue moldeando el mundo que habitamos.",
                    "A través de testimonios, imágenes y documentos únicos se reconstruye una historia que no debe olvidarse.",
                    "Un viaje visual y emocional que ilumina los hechos y los rostros que definieron una generación entera.",
                    "La realidad, cuando se observa de cerca y sin filtros, resulta más sorprendente que cualquier ficción.",
                    "{name} examina con profundidad y honestidad un tema que interpela a la sociedad contemporánea.",
                    "Voces auténticas y momentos irrepetibles construyen un relato que trasciende el simple registro de los hechos.",
                };
                epic = new[]
                {
                    "{name} es un documento histórico de primer orden que captura una realidad que difícilmente volverá a repetirse.",
                    "Un documental de una ambición y una ejecución excepcionales que redefine lo que el género es capaz de ofrecer.",
                    "La historia real supera cualquier ficción: un testimonio definitivo de la era que nos tocó vivir.",
                };
                break;
        }

        return highRarity
            ? ConcatArrays(epic, standard)   // epic templates first for high-rarity movies
            : ConcatArrays(standard, epic);
    }

    // ── English ─────────────────────────────────────────────────────────────────
    static string[] GetPoolEN(MovieGenre genre, bool highRarity)
    {
        string[] standard; string[] epic;
        switch (genre)
        {
            case MovieGenre.Action:
                standard = new[] {
                    "{name} follows an elite unit pushed to its limits in a race against time where failure is not an option.",
                    "When the clock runs out and the enemy is closer than expected, only sheer determination makes the difference.",
                    "A covert operation at the edge of possibility — every decision carries a cost and no mistake is forgiven.",
                    "In a high-stakes scenario, a team of fighters wages the most important battle of their lives.",
                    "{name} tests the limits of human endurance in a mission where every second counts.",
                    "Adrenaline and strategy collide in a relentless race that leaves no room to breathe.",
                };
                epic = new[] {
                    "{name} elevates the action genre to epic heights with a confrontation that redefines the meaning of sacrifice.",
                    "A mission of global scope whose outcome will determine the balance of power for decades to come.",
                };
                break;
            case MovieGenre.Drama:
                standard = new[] {
                    "A story of loss, redemption, and hope that portrays the complexity of human nature with honesty.",
                    "When certainties crumble, the characters must find the strength to move forward without losing what matters most.",
                    "{name} explores the bonds that define us and the impossible choices that shape the course of a life.",
                    "The past returns in unexpected ways, demanding answers that not everyone is prepared to give.",
                    "Between love, pain, and hope, the characters discover who they truly are when it matters most.",
                    "{name} portrays with sensitivity the invisible scars of those who choose to live with integrity against all odds.",
                };
                epic = new[] {
                    "{name} is a work of extraordinary emotional depth that transcends the screen to speak directly to the viewer.",
                    "A generational story spanning decades, lives, and legacies in a human portrait of monumental proportions.",
                };
                break;
            case MovieGenre.Horror:
                standard = new[] {
                    "An unknown presence begins to manifest in the shadows, and those who discover it are never the same.",
                    "When the deepest terror takes shape, running is no longer enough to stay safe.",
                    "{name} leads the audience somewhere reason cannot reach, where fear governs every moment.",
                    "Something ancient and forgotten awakens, and with it resurface secrets that should never have seen the light.",
                    "In an isolated setting, a group discovers that the real danger was always among them.",
                    "{name} builds an atmosphere of escalating tension that never lets go until the very last frame.",
                };
                epic = new[] {
                    "{name} takes horror into unexplored territory with a disturbance that questions the foundations of perception itself.",
                    "A primordial, nameless terror emerges to remind us that not everything in this world can be understood.",
                };
                break;
            case MovieGenre.Comedy:
                standard = new[] {
                    "What starts as a perfect plan quickly becomes the most hilarious chain of disasters imaginable.",
                    "When chance throws together incompatible characters, the result can only be the funniest chaos of the year.",
                    "{name} pushes its characters' ability to improvise to the limit in increasingly absurd situations.",
                    "Misunderstandings, impossible coincidences, and questionable decisions — the ingredients of a comedy with no mercy.",
                    "A journey full of surprises proving that sometimes the best plan is to have no plan at all.",
                    "{name} celebrates the most unexpected humor with a story that delivers laughs at the least expected moment.",
                };
                epic = new[] {
                    "{name} elevates comedy to a dimension of absurdity so precise and so well executed it borders on virtuoso.",
                    "A farce of colossal proportions that laughs at everything and everyone with uncommon intelligence.",
                };
                break;
            case MovieGenre.Romance:
                standard = new[] {
                    "Two paths that should never have crossed meet at the most unexpected moment of their lives.",
                    "Between love and adverse circumstances, some bonds prove impossible to break with the passing of time.",
                    "{name} is a story of encounters and farewells that proves some connections transcend time itself.",
                    "When the heart decides, neither distance nor the past can prevent what was always meant to happen.",
                    "A story exploring the vulnerability and courage of those who dare to love without guarantees.",
                    "{name} recounts with delicacy and emotion the story of two people who learn to truly see each other.",
                };
                epic = new[] {
                    "{name} is a romance of an intensity and beauty rarely achieved in the genre.",
                    "A love story spanning continents, decades, and entire lives without losing an ounce of its emotion.",
                };
                break;
            case MovieGenre.SciFi:
                standard = new[] {
                    "In a future marked by uncertainty, a daring expedition reveals truths that change the course of history.",
                    "When science reaches the unthinkable, the most important questions remain without a satisfying answer.",
                    "{name} presents a scenario where the limits of the possible dissolve and reality itself becomes questionable.",
                    "A civilization on the brink of its own abyss discovers that the greatest danger always came from within.",
                    "Space, time, and consciousness become the battlefield of a story with no precedent.",
                    "{name} builds a fascinating universe where every breakthrough carries a price not everyone is willing to pay.",
                };
                epic = new[] {
                    "{name} redefines the genre with a vision of science fiction that transcends speculation and touches the philosophical.",
                    "An interstellar odyssey of mythic proportions placing humanity before the ultimate questions of its existence.",
                };
                break;
            case MovieGenre.Fantasy:
                standard = new[] {
                    "In lands where magic and destiny intertwine, an unlikely hero embarks on the journey of a lifetime.",
                    "An ancient prophecy unfolds amid warring kingdoms and powers the world believed long extinct.",
                    "{name} unfolds a world of wonders and dangers where only courage can tip the balance.",
                    "When the forces of good and evil collide, the outcome will determine the future of an entire world.",
                    "A forgotten power resurfaces at the most critical moment, testing the faith and valor of those who wield it.",
                    "{name} is an epic tale of sacrifice, loyalty, and the search for a destiny no one can escape.",
                };
                epic = new[] {
                    "{name} is a fantasy epic of a scale and ambition that very few works in the genre can match.",
                    "A universe built with the richness of myth and the depth of a saga that transcends generations.",
                };
                break;
            case MovieGenre.Thriller:
                standard = new[] {
                    "The truth is closer than it seems, but uncovering it may prove more dangerous than the lie itself.",
                    "Every answer opens a new question in a spiral of secrets threatening to destroy everything.",
                    "{name} keeps the audience in constant tension with unpredictable twists that redefine what they think they know.",
                    "When appearances deceive and trust is a luxury no one can afford, only instinct remains.",
                    "A carefully constructed conspiracy begins to unravel when the wrong person discovers too much.",
                    "{name} is a tale of intrigue and betrayal where the line between right and wrong blurs irreversibly.",
                };
                epic = new[] {
                    "{name} takes the thriller to a level of narrative complexity and sustained tension that is hard to match.",
                    "A conspiracy of global dimensions whose resolution will rewrite history as we know it.",
                };
                break;
            case MovieGenre.Animation:
                standard = new[] {
                    "An extraordinary group of friends lives an adventure that will test their courage and friendship in unexpected ways.",
                    "In a world full of wonders, imagination is the one tool that never fails when you need it most.",
                    "{name} takes the audience to a colorful and exciting universe where dreams and challenges coexist.",
                    "A story celebrating friendship, creativity, and the ability to find magic in the most everyday things.",
                    "What starts as a small adventure soon becomes the most important journey of its characters' lives.",
                    "{name} blends emotion, humor, and warmth in a story that resonates with audiences of all ages.",
                };
                epic = new[] {
                    "{name} redefines what animation can tell with a story of uncommon emotional depth.",
                    "An animated work that transcends its format to become a cinematic experience of the highest order.",
                };
                break;
            case MovieGenre.Documentary:
            default:
                standard = new[] {
                    "A fascinating and rigorous look at the people and moments that transformed an era forever.",
                    "Behind every great event are extraordinary human stories that deserve to be told and remembered.",
                    "{name} offers a unique and intimate perspective on a phenomenon that continues to shape the world we inhabit.",
                    "Through testimonies, images, and unique documents, a story is reconstructed that must not be forgotten.",
                    "A visual and emotional journey illuminating the events and faces that defined an entire generation.",
                    "{name} examines with depth and honesty a topic that challenges contemporary society.",
                };
                epic = new[] {
                    "{name} is a primary historical document that captures a reality unlikely ever to be repeated.",
                    "A documentary of exceptional ambition and execution that redefines what the genre can offer.",
                };
                break;
        }
        return highRarity ? ConcatArrays(epic, standard) : ConcatArrays(standard, epic);
    }

    // ── French ──────────────────────────────────────────────────────────────────
    static string[] GetPoolFR(MovieGenre genre, bool highRarity)
    {
        string[] standard; string[] epic;
        switch (genre)
        {
            case MovieGenre.Action:
                standard = new[] {
                    "{name} suit une unité d'élite poussée à ses limites dans une course contre la montre où l'échec n'est pas une option.",
                    "Quand le temps manque et l'ennemi est plus proche que prévu, seule la détermination fait la différence.",
                    "Une opération secrète à la limite du possible — chaque décision a un coût et aucune erreur n'est pardonnée.",
                    "Dans un scénario à haute tension, un groupe de combattants livre la bataille la plus importante de leur vie.",
                    "{name} met à l'épreuve les limites humaines dans une mission où chaque seconde compte.",
                    "L'adrénaline et la stratégie s'affrontent dans une course effrénée qui ne laisse aucun répit.",
                };
                epic = new[] {
                    "{name} élève le film d'action à des sommets épiques avec une confrontation qui redéfinit le sens du sacrifice.",
                    "Une mission d'envergure mondiale dont l'issue déterminera l'équilibre des pouvoirs pour les décennies à venir.",
                };
                break;
            case MovieGenre.Drama:
                standard = new[] {
                    "Une histoire de perte, de rédemption et d'espoir qui décrit avec honnêteté la complexité de la nature humaine.",
                    "Quand les certitudes s'effondrent, les personnages doivent trouver la force d'avancer sans perdre l'essentiel.",
                    "{name} explore les liens qui nous définissent et les choix impossibles qui déterminent le cours d'une vie.",
                    "Le passé revient de manière inattendue, exigeant des réponses que tout le monde n'est pas prêt à donner.",
                    "Entre amour, douleur et espoir, les protagonistes découvrent qui ils sont vraiment quand cela compte le plus.",
                    "{name} décrit avec sensibilité les cicatrices invisibles de ceux qui choisissent de vivre avec intégrité.",
                };
                epic = new[] {
                    "{name} est une œuvre d'une profondeur émotionnelle extraordinaire qui transcende l'écran pour interpeller le spectateur.",
                    "Une histoire générationnelle couvrant des décennies, des vies et des legs dans une fresque humaine monumentale.",
                };
                break;
            case MovieGenre.Horror:
                standard = new[] {
                    "Une présence inconnue commence à se manifester dans les ombres, et ceux qui la découvrent ne sont plus jamais les mêmes.",
                    "Quand la terreur la plus profonde prend forme, fuir ne suffit plus à rester en sécurité.",
                    "{name} mène le spectateur là où la raison ne peut atteindre, là où la peur gouverne chaque instant.",
                    "Quelque chose d'ancien et d'oublié se réveille, et avec lui ressurgissent des secrets qui n'auraient jamais dû voir le jour.",
                    "Dans un cadre isolé, un groupe découvre que le vrai danger était depuis toujours parmi eux.",
                    "{name} construit une atmosphère de tension croissante qui ne lâche pas jusqu'au dernier instant.",
                };
                epic = new[] {
                    "{name} emmène l'horreur en territoire inexploré avec une perturbation qui remet en question les fondements de la perception.",
                    "Une terreur primordiale et sans nom émerge pour rappeler que tout ce qui existe dans ce monde ne peut être compris.",
                };
                break;
            case MovieGenre.Comedy:
                standard = new[] {
                    "Ce qui commence comme un plan parfait devient rapidement la chaîne de catastrophes la plus hilarante imaginable.",
                    "Quand le hasard réunit des personnages incompatibles, le résultat ne peut être que le chaos le plus drôle de l'année.",
                    "{name} pousse la capacité d'improvisation de ses personnages à la limite dans des situations de plus en plus absurdes.",
                    "Malentendus, coïncidences impossibles et décisions douteuses — les ingrédients d'une comédie sans pitié.",
                    "Un voyage plein de surprises prouvant que parfois le meilleur plan est de n'en avoir aucun.",
                    "{name} célèbre l'humour le plus inattendu avec une histoire qui fait rire au moment le moins prévu.",
                };
                epic = new[] {
                    "{name} élève la comédie à une dimension d'absurde si précis et si bien exécuté qu'il frôle le virtuose.",
                    "Une farce aux proportions colossales qui rit de tout et de tous avec une intelligence hors du commun.",
                };
                break;
            case MovieGenre.Romance:
                standard = new[] {
                    "Deux chemins qui n'auraient jamais dû se croiser se rencontrent au moment le plus inattendu de leur vie.",
                    "Entre l'amour et les circonstances adverses, certains liens s'avèrent impossibles à briser avec le temps.",
                    "{name} est une histoire de rencontres et d'adieux qui prouve que certaines connexions transcendent le temps.",
                    "Quand le cœur décide, ni la distance ni le passé ne peuvent empêcher ce qui était destiné à arriver.",
                    "Une histoire explorant la vulnérabilité et le courage de ceux qui osent aimer sans garanties.",
                    "{name} raconte avec délicatesse et émotion l'histoire de deux personnes qui apprennent à vraiment se voir.",
                };
                epic = new[] {
                    "{name} est un roman d'une intensité et d'une beauté rarement atteintes dans le genre.",
                    "Une histoire d'amour couvrant continents, décennies et vies entières sans perdre une once de son émotion.",
                };
                break;
            case MovieGenre.SciFi:
                standard = new[] {
                    "Dans un futur marqué par l'incertitude, une expédition audacieuse révèle des vérités qui changent le cours de l'histoire.",
                    "Quand la science atteint l'impensable, les questions les plus importantes restent sans réponse satisfaisante.",
                    "{name} présente un scénario où les limites du possible s'effacent et la réalité elle-même devient discutable.",
                    "Une civilisation au bord de son propre gouffre découvre que le plus grand danger venait de l'intérieur.",
                    "L'espace, le temps et la conscience deviennent le champ de bataille d'une histoire sans précédent.",
                    "{name} construit un univers fascinant où chaque avancée a un prix que tous ne sont pas prêts à payer.",
                };
                epic = new[] {
                    "{name} redéfinit le genre avec une vision de la science-fiction qui transcende la spéculation pour toucher le philosophique.",
                    "Une odyssée interstellaire aux proportions mythiques plaçant l'humanité devant les questions ultimes de son existence.",
                };
                break;
            case MovieGenre.Fantasy:
                standard = new[] {
                    "Dans des terres où magie et destin s'entrelacent, un héros improbable se lance dans le voyage de sa vie.",
                    "Une prophétie ancestrale se réalise au milieu de royaumes en guerre et de pouvoirs que le monde croyait disparus.",
                    "{name} déploie un monde de merveilles et de dangers où seul le courage peut faire pencher la balance.",
                    "Quand les forces du bien et du mal s'affrontent, le résultat déterminera l'avenir d'un monde entier.",
                    "Un pouvoir oublié ressurgit au moment le plus critique, mettant à l'épreuve la foi et la valeur de ceux qui le portent.",
                    "{name} est un récit épique de sacrifice, de loyauté et de la quête d'un destin que personne ne peut fuir.",
                };
                epic = new[] {
                    "{name} est une épopée fantastique d'une échelle et d'une ambition que peu d'œuvres du genre peuvent égaler.",
                    "Un univers construit avec la richesse d'un mythe et la profondeur d'une saga qui transcende les générations.",
                };
                break;
            case MovieGenre.Thriller:
                standard = new[] {
                    "La vérité est plus proche qu'il n'y paraît, mais la découvrir pourrait s'avérer plus dangereux que le mensonge lui-même.",
                    "Chaque réponse ouvre une nouvelle question dans une spirale de secrets qui menace de tout détruire.",
                    "{name} maintient le spectateur en tension constante avec des rebondissements imprévisibles.",
                    "Quand les apparences trompent et que la confiance est un luxe que personne ne peut se permettre, seul l'instinct reste.",
                    "Une conspiration soigneusement construite commence à s'effondrer quand la mauvaise personne en découvre trop.",
                    "{name} est un récit d'intrigues et de trahisons où la frontière entre le bien et le mal s'estompe irrémédiablement.",
                };
                epic = new[] {
                    "{name} élève le thriller à un niveau de complexité narrative et de tension soutenue difficilement égalable.",
                    "Une conspiration aux dimensions mondiales dont la résolution réécrira l'histoire telle que nous la connaissons.",
                };
                break;
            case MovieGenre.Animation:
                standard = new[] {
                    "Un groupe d'amis extraordinaires vit une aventure qui mettra leur courage et leur amitié à l'épreuve.",
                    "Dans un monde rempli de merveilles, l'imagination est le seul outil qui ne faillit jamais quand on en a le plus besoin.",
                    "{name} emmène le spectateur dans un univers coloré et passionnant où rêves et défis coexistent.",
                    "Une histoire célébrant l'amitié, la créativité et la capacité à trouver de la magie dans le quotidien.",
                    "Ce qui commence comme une petite aventure devient vite le voyage le plus important de leurs vies.",
                    "{name} mêle émotion, humour et tendresse dans une histoire qui touche un public de tous âges.",
                };
                epic = new[] {
                    "{name} redéfinit ce que l'animation peut raconter avec une histoire d'une profondeur émotionnelle hors du commun.",
                    "Une œuvre animée qui transcende son format pour devenir une expérience cinématographique de premier plan.",
                };
                break;
            case MovieGenre.Documentary:
            default:
                standard = new[] {
                    "Un regard fascinant et rigoureux sur les protagonistes et les moments qui ont transformé une époque à jamais.",
                    "Derrière chaque grand événement se cachent des histoires humaines extraordinaires qui méritent d'être racontées.",
                    "{name} offre une perspective unique et intime sur un phénomène qui continue de façonner le monde que nous habitons.",
                    "À travers témoignages, images et documents uniques, une histoire est reconstruite qui ne doit pas être oubliée.",
                    "Un voyage visuel et émotionnel qui éclaire les événements et les visages qui ont défini toute une génération.",
                    "{name} examine avec profondeur et honnêteté un sujet qui interpelle la société contemporaine.",
                };
                epic = new[] {
                    "{name} est un document historique de premier ordre qui capture une réalité qui ne se reproduira guère.",
                    "Un documentaire d'une ambition et d'une exécution exceptionnelles qui redéfinit ce que le genre peut offrir.",
                };
                break;
        }
        return highRarity ? ConcatArrays(epic, standard) : ConcatArrays(standard, epic);
    }

    // ── German ──────────────────────────────────────────────────────────────────
    static string[] GetPoolDE(MovieGenre genre, bool highRarity)
    {
        string[] standard; string[] epic;
        switch (genre)
        {
            case MovieGenre.Action:
                standard = new[] {
                    "{name} folgt einer Eliteeinheit an den Grenzen des Möglichen — in einem Wettlauf gegen die Zeit, in dem Versagen keine Option ist.",
                    "Als die Zeit abläuft und der Feind näher ist als erwartet, entscheidet allein die Entschlossenheit.",
                    "Eine verdeckte Operation jenseits aller Grenzen — jede Entscheidung hat ihren Preis und kein Fehler wird verziehen.",
                    "In einem Hochrisikoszenario liefern Kämpfer die wichtigste Schlacht ihres Lebens.",
                    "{name} stellt die Grenzen menschlicher Ausdauer auf die Probe in einem Einsatz, bei dem jede Sekunde zählt.",
                    "Adrenalin und Strategie stoßen in einem gnadenlosen Wettlauf aufeinander, der keine Verschnaufpause lässt.",
                };
                epic = new[] {
                    "{name} hebt das Actiongenre auf epische Höhen mit einer Konfrontation, die die Bedeutung von Opfer neu definiert.",
                    "Ein Einsatz von globalem Ausmaß, dessen Ausgang das Machtgefüge für Jahrzehnte bestimmen wird.",
                };
                break;
            case MovieGenre.Drama:
                standard = new[] {
                    "Eine Geschichte von Verlust, Erlösung und Hoffnung, die die Komplexität menschlicher Natur ehrlich porträtiert.",
                    "Als die Gewissheiten zerbröckeln, müssen die Charaktere die Kraft finden, voranzugehen, ohne das Wesentliche zu verlieren.",
                    "{name} erkundet die Bindungen, die uns definieren, und die unmöglichen Entscheidungen, die den Lauf eines Lebens bestimmen.",
                    "Die Vergangenheit kehrt auf unerwartete Weise zurück und fordert Antworten, auf die nicht jeder vorbereitet ist.",
                    "Zwischen Liebe, Schmerz und Hoffnung entdecken die Protagonisten, wer sie wirklich sind, wenn es darauf ankommt.",
                    "{name} portraitiert mit Feingefühl die unsichtbaren Narben derer, die wählen, mit Integrität gegen alle Widerstände zu leben.",
                };
                epic = new[] {
                    "{name} ist ein Werk von außergewöhnlicher emotionaler Tiefe, das über die Leinwand hinaus den Zuschauer direkt anspricht.",
                    "Eine Generationengeschichte über Jahrzehnte, Leben und Vermächtnisse — ein menschliches Fresko von monumentalem Ausmaß.",
                };
                break;
            case MovieGenre.Horror:
                standard = new[] {
                    "Eine unbekannte Präsenz beginnt sich in den Schatten zu manifestieren, und wer sie entdeckt, ist nie mehr dieselbe Person.",
                    "Wenn die tiefste Angst Gestalt annimmt, reicht Flucht nicht mehr aus, um in Sicherheit zu bleiben.",
                    "{name} führt das Publikum an einen Ort, den die Vernunft nicht erreicht, wo Angst jeden Moment regiert.",
                    "Etwas Altes und Vergessenes erwacht, und mit ihm tauchen Geheimnisse auf, die nie ans Licht hätten kommen dürfen.",
                    "In einer abgelegenen Umgebung entdeckt eine Gruppe, dass die wahre Gefahr immer unter ihnen war.",
                    "{name} baut eine Atmosphäre wachsender Spannung auf, die bis zum letzten Moment nicht loslässt.",
                };
                epic = new[] {
                    "{name} treibt den Horror in unerforschtes Terrain mit einer Erschütterung, die die Grundlagen der Wahrnehmung in Frage stellt.",
                    "Ein uralter, namenloser Schrecken taucht auf, um uns daran zu erinnern, dass nicht alles in dieser Welt zu begreifen ist.",
                };
                break;
            case MovieGenre.Comedy:
                standard = new[] {
                    "Was als perfekter Plan beginnt, wird schnell zur wohl absurdesten Katastrophenserie, die man sich vorstellen kann.",
                    "Wenn der Zufall unvereinbare Charaktere zusammenbringt, kann das Ergebnis nur das lustigste Chaos des Jahres sein.",
                    "{name} treibt das Improvisationstalent seiner Figuren in immer absurderen Situationen an die Grenzen.",
                    "Missverständnisse, unmögliche Zufälle und fragwürdige Entscheidungen — die Zutaten einer Komödie ohne Gnade.",
                    "Eine Reise voller Überraschungen, die beweist, dass der beste Plan manchmal gar kein Plan ist.",
                    "{name} zelebriert den unerwartetsten Humor mit einer Geschichte, die im unpassendsten Moment zum Lachen bringt.",
                };
                epic = new[] {
                    "{name} hebt die Komödie auf eine Dimension des Absurden, die so präzise und so gut ausgeführt ist, dass sie fast virtuos wirkt.",
                    "Eine Farce von kolossalem Ausmaß, die über alles und jeden mit ungewöhnlicher Intelligenz lacht.",
                };
                break;
            case MovieGenre.Romance:
                standard = new[] {
                    "Zwei Wege, die sich nie hätten kreuzen sollen, treffen im unerwartetsten Moment ihres Lebens aufeinander.",
                    "Zwischen Liebe und widrigen Umständen erweisen sich manche Bande als unmöglich zu brechen.",
                    "{name} ist eine Geschichte von Begegnungen und Abschieben, die beweist, dass manche Verbindungen die Zeit überdauern.",
                    "Wenn das Herz entscheidet, können weder Distanz noch Vergangenheit verhindern, was immer geschehen sollte.",
                    "Eine Geschichte, die die Verletzlichkeit und den Mut derer erkundet, die es wagen, ohne Garantien zu lieben.",
                    "{name} erzählt mit Zartheit und Emotion die Geschichte zweier Menschen, die lernen, einander wirklich zu sehen.",
                };
                epic = new[] {
                    "{name} ist eine Romanze von einer Intensität und Schönheit, die im Genre selten erreicht wird.",
                    "Eine Liebesgeschichte über Kontinente, Jahrzehnte und ganze Leben — ohne auch nur einen Funken ihrer Emotion zu verlieren.",
                };
                break;
            case MovieGenre.SciFi:
                standard = new[] {
                    "In einer von Unsicherheit geprägten Zukunft enthüllt eine waghalsige Expedition Wahrheiten, die den Lauf der Geschichte verändern.",
                    "Als die Wissenschaft das Undenkbare erreicht, bleiben die wichtigsten Fragen ohne befriedigende Antwort.",
                    "{name} entwirft ein Szenario, in dem die Grenzen des Möglichen verschwimmen und die Realität selbst in Frage gestellt wird.",
                    "Eine Zivilisation am Rand ihres eigenen Abgrunds entdeckt, dass die größte Gefahr immer von innen kam.",
                    "Raum, Zeit und Bewusstsein werden zum Schlachtfeld einer Geschichte ohne Präzedenzfall.",
                    "{name} baut ein faszinierendes Universum, in dem jeder Fortschritt einen Preis hat, den nicht jeder zu zahlen bereit ist.",
                };
                epic = new[] {
                    "{name} definiert das Genre neu mit einer Science-Fiction-Vision, die über Spekulation hinaus das Philosophische berührt.",
                    "Eine interstellare Odyssee mythischen Ausmaßes, die die Menschheit vor die letzten Fragen ihrer Existenz stellt.",
                };
                break;
            case MovieGenre.Fantasy:
                standard = new[] {
                    "In Ländern, wo Magie und Schicksal sich verweben, bricht ein unwahrscheinlicher Held zur Reise seines Lebens auf.",
                    "Eine uralte Prophezeiung erfüllt sich inmitten kriegführender Königreiche und Mächten, die die Welt für ausgestorben hielt.",
                    "{name} entfaltet eine Welt voller Wunder und Gefahren, wo nur Mut die Waage neigen kann.",
                    "Als Gut und Böse aufeinanderprallen, bestimmt das Ergebnis die Zukunft einer ganzen Welt.",
                    "Eine vergessene Macht taucht im kritischsten Moment wieder auf und stellt Glauben und Tapferkeit auf die Probe.",
                    "{name} ist ein episches Epos aus Opfer, Loyalität und der Suche nach einem Schicksal, dem niemand entfliehen kann.",
                };
                epic = new[] {
                    "{name} ist ein Fantasie-Epos von einer Größe und einem Ehrgeiz, die nur wenige Werke des Genres erreichen.",
                    "Ein Universum, erbaut mit dem Reichtum eines Mythos und der Tiefe einer Saga, die Generationen überdauert.",
                };
                break;
            case MovieGenre.Thriller:
                standard = new[] {
                    "Die Wahrheit ist näher, als es scheint — doch sie aufzudecken könnte gefährlicher sein als die Lüge selbst.",
                    "Jede Antwort öffnet eine neue Frage in einer Spirale aus Geheimnissen, die alles zu zerstören droht.",
                    "{name} hält das Publikum in ständiger Spannung mit unvorhersehbaren Wendungen, die alles in Frage stellen.",
                    "Wenn Schein trügt und Vertrauen ein Luxus ist, den sich niemand leisten kann, bleibt nur noch der Instinkt.",
                    "Eine sorgfältig konstruierte Verschwörung beginnt zu bröckeln, als die falsche Person zu viel herausfindet.",
                    "{name} ist ein Intrigen- und Verrats-Thriller, bei dem die Grenze zwischen Gut und Böse unwiderruflich verschwimmt.",
                };
                epic = new[] {
                    "{name} hebt den Thriller auf ein Niveau narrativer Komplexität und anhaltender Spannung, das schwer zu übertreffen ist.",
                    "Eine Verschwörung globalen Ausmaßes, deren Auflösung die Geschichte so neu schreiben wird, wie wir sie kennen.",
                };
                break;
            case MovieGenre.Animation:
                standard = new[] {
                    "Eine außergewöhnliche Gruppe von Freunden erlebt ein Abenteuer, das ihren Mut und ihre Freundschaft auf unerwartete Weise auf die Probe stellt.",
                    "In einer Welt voller Wunder ist die Vorstellungskraft das einzige Werkzeug, das nie versagt, wenn man es am meisten braucht.",
                    "{name} entführt das Publikum in ein farbenfrohes und aufregendes Universum, in dem Träume und Herausforderungen koexistieren.",
                    "Eine Geschichte, die Freundschaft, Kreativität und die Fähigkeit feiert, Magie im Alltäglichen zu finden.",
                    "Was als kleines Abenteuer beginnt, wird bald zur wichtigsten Reise ihrer Leben.",
                    "{name} verbindet Emotion, Humor und Wärme in einer Geschichte, die ein Publikum jeden Alters anspricht.",
                };
                epic = new[] {
                    "{name} definiert neu, was Animation erzählen kann — mit einer Geschichte von ungewöhnlicher emotionaler Tiefe.",
                    "Ein Animationswerk, das sein Format übersteigt und zu einem Kinoerlebnis erster Güte wird.",
                };
                break;
            case MovieGenre.Documentary:
            default:
                standard = new[] {
                    "Ein faszinierender und rigoroser Blick auf die Menschen und Momente, die eine Ära für immer verändert haben.",
                    "Hinter jedem großen Ereignis verbergen sich außergewöhnliche menschliche Geschichten, die es wert sind, erzählt zu werden.",
                    "{name} bietet eine einzigartige und intime Perspektive auf ein Phänomen, das die Welt, in der wir leben, weiterhin prägt.",
                    "Durch Aussagen, Bilder und einzigartige Dokumente wird eine Geschichte rekonstruiert, die nicht vergessen werden darf.",
                    "Eine visuelle und emotionale Reise, die die Ereignisse und Gesichter beleuchtet, die eine ganze Generation prägten.",
                    "{name} untersucht mit Tiefe und Ehrlichkeit ein Thema, das die heutige Gesellschaft herausfordert.",
                };
                epic = new[] {
                    "{name} ist ein Primärdokument der Geschichte, das eine Realität festhält, die sich kaum wiederholen wird.",
                    "Eine Dokumentation von außergewöhnlichem Ehrgeiz und außergewöhnlicher Ausführung, die neu definiert, was das Genre leisten kann.",
                };
                break;
        }
        return highRarity ? ConcatArrays(epic, standard) : ConcatArrays(standard, epic);
    }

    // ── Japanese ────────────────────────────────────────────────────────────────
    static string[] GetPoolJA(MovieGenre genre, bool highRarity)
    {
        string[] standard; string[] epic;
        switch (genre)
        {
            case MovieGenre.Action:
                standard = new[] {
                    "{name}はエリート部隊が時間との戦いを繰り広げる、失敗が許されない極限のミッションを描く。",
                    "時間が尽きかけ、敵が予想以上に近づいたとき、勝敗を分けるのは揺るぎない意志だけだ。",
                    "可能性の限界に挑む極秘作戦。あらゆる判断に代償があり、どんな失敗も許されない。",
                    "緊迫したシナリオの中で、戦士たちは人生で最も重要な戦いに臨む。",
                    "{name}は、一秒一秒が命取りとなるミッションで人間の限界を試す。",
                    "アドレナリンと戦略が激突する息も吐けない追跡劇が、今始まる。",
                };
                epic = new[] {
                    "{name}はアクション映画を叙事詩的な高みへと引き上げ、犠牲の意味を問い直す対決を描く。",
                    "結末が数十年にわたる勢力均衡を左右する、世界規模のミッション。",
                };
                break;
            case MovieGenre.Drama:
                standard = new[] {
                    "喪失、贖罪、そして希望——人間の複雑さを誠実に描いた物語。",
                    "確かなものが崩れ落ちるとき、登場人物たちは大切なものを失わずに前へ進む力を見つけなければならない。",
                    "{name}は私たちを定義する絆と、人生の行方を決める不可能な選択を探求する。",
                    "過去は予期せぬかたちで戻ってくる——誰もが答える準備ができていない問いを携えて。",
                    "愛と痛みと希望の狭間で、主人公たちは本当に大切なときに自分が何者かを発見する。",
                    "{name}は、逆境の中で誠実に生きる者たちの見えない傷跡を繊細に描き出す。",
                };
                epic = new[] {
                    "{name}はスクリーンを超えて観客に語りかける、圧倒的な感情的深みを持つ作品だ。",
                    "数十年、人生、そして遺産にわたる世代の物語——壮大なスケールの人間叙事詩。",
                };
                break;
            case MovieGenre.Horror:
                standard = new[] {
                    "未知の存在が影の中で動き始め、それに気づいた者はもとの自分に戻れない。",
                    "深淵の恐怖が形を成したとき、逃げるだけでは安全は保証されない。",
                    "{name}は理性の届かない場所へ観客を誘い、恐怖がすべての瞬間を支配する世界を描く。",
                    "古く忘れられた何かが目覚め、決して日の目を見てはならなかった秘密を蘇らせる。",
                    "孤立した場所で、グループは本当の危険が常に自分たちの中にいたことを知る。",
                    "{name}は最後の一瞬まで手を放さない、高まる緊張の雰囲気を作り上げる。",
                };
                epic = new[] {
                    "{name}はホラーを未踏の領域へ誘い、知覚の根本を揺るがす怪異を描く。",
                    "この世のすべてが理解できるわけではないと知らしめる、原初の名もなき恐怖が目覚める。",
                };
                break;
            case MovieGenre.Comedy:
                standard = new[] {
                    "完璧なはずの計画が、想像を絶する惨事の連鎖へと変わっていく。",
                    "相性最悪のキャラクターが運命に引き合わされると、結果は今年最高に笑えるカオスしかない。",
                    "{name}はますます突拍子もない状況で、登場人物たちの即興力を限界まで試す。",
                    "勘違い、あり得ない偶然、疑わしい判断——容赦なく笑いを誘うコメディの必要条件。",
                    "驚きに満ちた旅が証明するのは、ときには無計画がベストプランだということ。",
                    "{name}は最も予想外の場面で笑いを届ける、思いがけないユーモアを讃える物語。",
                };
                epic = new[] {
                    "{name}はコメディを、これほど精巧で見事に演じられたらほとんど芸術的とも言えるほどの不条理の次元に引き上げる。",
                    "並外れた知性で万物を笑い飛ばす、桁外れな規模の爆笑喜劇。",
                };
                break;
            case MovieGenre.Romance:
                standard = new[] {
                    "決して交わるはずのなかった二つの道が、人生で最も思いがけない瞬間に出会う。",
                    "愛と逆境の間で、時の流れをもってしても断ち切れない絆がある。",
                    "{name}は、時を超えてつながる出会いと別れの物語を描く。",
                    "心が決めたとき、距離も過去も、起こるべきことを止められない。",
                    "保証なしに愛することを選んだ者たちの脆さと勇気を探る物語。",
                    "{name}は互いを本当に見ることを学ぶ二人の物語を、繊細さと感情豊かに紡ぐ。",
                };
                epic = new[] {
                    "{name}は、このジャンルで滅多に到達できない強度と美しさを持つロマンスだ。",
                    "大陸、数十年、人生全体にわたる愛の物語——その感動は一瞬たりとも薄れない。",
                };
                break;
            case MovieGenre.SciFi:
                standard = new[] {
                    "不確実な未来を舞台に、大胆な探検が歴史の流れを変える真実を明らかにする。",
                    "科学が想像を絶することに到達したとき、最も重要な問いに満足な答えはない。",
                    "{name}は可能性の限界が消え、現実そのものが問われるシナリオを提示する。",
                    "深淵に立つ文明が、最大の危機は常に内側から来ていたことを発見する。",
                    "空間、時間、意識が前例のない物語の舞台となる。",
                    "{name}はすべての進歩に代償があり、誰もがそれを払う覚悟を持つわけではない世界を築く。",
                };
                epic = new[] {
                    "{name}は思索を超えて哲学の領域に触れるSFのビジョンでジャンルを再定義する。",
                    "人類を存在の究極の問いへと向き合わせる、神話的規模の恒星間叙事詩。",
                };
                break;
            case MovieGenre.Fantasy:
                standard = new[] {
                    "魔法と運命が絡み合う土地で、思わぬ英雄が生涯の旅へと踏み出す。",
                    "戦火の王国と世界が絶えたと信じていた力の中で、古代の予言が成就する。",
                    "{name}は、勇気だけが均衡を傾けられる驚異と危険の世界を展開する。",
                    "善と悪がぶつかったとき、その結末が世界全体の未来を決定する。",
                    "最も重大な瞬間に忘れられた力が蘇り、それを宿す者たちの信仰と勇気が試される。",
                    "{name}は誰も逃れられない運命の探求を描く、犠牲と忠義の叙事詩。",
                };
                epic = new[] {
                    "{name}は、このジャンルでほとんど比肩できない規模と野心を持つファンタジー叙事詩だ。",
                    "神話の豊かさと世代を超える大河の深みで構築された世界。",
                };
                break;
            case MovieGenre.Thriller:
                standard = new[] {
                    "真実は思ったより近くにある——だが暴くことは、嘘よりも危険かもしれない。",
                    "すべてを破壊しようとする秘密の連鎖の中で、答えごとに新たな問いが生まれる。",
                    "{name}は常に観客を緊張の中に置き、予測不能な展開ですべての思い込みを覆す。",
                    "見た目が信用できず、信頼が誰も払えない贅沢なとき、残るのは本能だけだ。",
                    "精緻に構築された陰謀は、間違った人物が知りすぎた瞬間から崩れ始める。",
                    "{name}は善と悪の境界が取り返しのつかないほど曖昧になる、謀略と裏切りの物語。",
                };
                epic = new[] {
                    "{name}はスリラーを、他では到達しにくいほどの語りの複雑さと持続する緊張の次元へと引き上げる。",
                    "世界規模の陰謀——その解決は、私たちが知る歴史を書き換えることになる。",
                };
                break;
            case MovieGenre.Animation:
                standard = new[] {
                    "特別な仲間たちが、勇気と友情を予想外のかたちで試される冒険に挑む。",
                    "驚異に満ちた世界で、想像力だけが最も必要なときに絶対に裏切らない道具だ。",
                    "{name}は夢と挑戦が共存する、色鮮やかで興奮に満ちた世界へ観客を連れていく。",
                    "友情、創造性、そして日常の中に魔法を見つける力を讃える物語。",
                    "ちょっとした冒険として始まったものが、やがて彼らの人生で最も大切な旅へと変わる。",
                    "{name}は感動、ユーモア、温かさを織り交ぜ、あらゆる世代に届く物語を紡ぐ。",
                };
                epic = new[] {
                    "{name}はアニメーションが何を語れるかを再定義する、並外れた感情的深みを持つ物語。",
                    "そのフォーマットを超えて最高水準の映画体験となるアニメーション作品。",
                };
                break;
            case MovieGenre.Documentary:
            default:
                standard = new[] {
                    "ある時代を永遠に変えた人々と瞬間に対する、魅力的で厳密な視線。",
                    "すべての大きな出来事の背後には、語られ記憶されるべき非凡な人間の物語がある。",
                    "{name}は私たちが生きる世界を今も形作り続ける現象への、独自で親密な視点を提供する。",
                    "証言、映像、唯一無二の資料を通じて、忘れてはならない歴史が再構築される。",
                    "一世代を定義した出来事と顔を照らし出す、映像と感情の旅。",
                    "{name}は現代社会に問いかけるテーマを、深みと誠実さをもって考察する。",
                };
                epic = new[] {
                    "{name}は再現されることのない現実を捉えた、一級の歴史的文書だ。",
                    "このジャンルが何を提供できるかを再定義する、卓越した野心と完成度を持つドキュメンタリー。",
                };
                break;
        }
        return highRarity ? ConcatArrays(epic, standard) : ConcatArrays(standard, epic);
    }

    static T[] ConcatArrays<T>(T[] a, T[] b)
    {
        var result = new T[a.Length + b.Length];
        Array.Copy(a, 0, result, 0, a.Length);
        Array.Copy(b, 0, result, a.Length, b.Length);
        return result;
    }
}
