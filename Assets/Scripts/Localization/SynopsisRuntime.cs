using System;

/// <summary>
/// Runtime Spanish synopsis fallback when asset data is mislabeled (FASE 16.4).
/// Uses the same deterministic template pools as the editor generator.
/// </summary>
public static class SynopsisRuntime
{
    public static string GenerateSpanish(MovieConfig cfg)
    {
        if (cfg == null) return null;
        bool highRarity = cfg.rarity == MovieRarity.Legendary || cfg.rarity == MovieRarity.Epic;
        var pool = GetPoolES(cfg.genre, highRarity);
        if (pool == null || pool.Length == 0) return null;
        int idx = Math.Abs((cfg.movieName ?? cfg.name).GetHashCode()) % pool.Length;
        return pool[idx].Replace("{name}", cfg.movieName ?? cfg.name);
    }

    static string[] GetPoolES(MovieGenre genre, bool highRarity)
    {
        string[] standard;
        string[] epic;

        switch (genre)
        {
            case MovieGenre.Action:
                standard = new[]
                {
                    "{name} sigue a una unidad de élite que debe actuar en la sombra para evitar una catástrofe.",
                    "Cuando el tiempo se agota, solo la determinación marca la diferencia en una misión imposible.",
                    "Una operación al límite donde cada decisión tiene un coste y ningún error es perdonable.",
                };
                epic = new[]
                {
                    "{name} eleva la acción a cotas épicas con una confrontación que redefine el sacrificio.",
                };
                break;
            case MovieGenre.Drama:
                standard = new[]
                {
                    "Una historia de pérdida, redención y esperanza que retrata la complejidad humana.",
                    "{name} explora los vínculos que nos definen y las decisiones que cambian una vida entera.",
                    "Entre el amor y el dolor, los protagonistas descubren quiénes son cuando más importa.",
                };
                epic = new[]
                {
                    "{name} es un drama monumental que deja huella mucho después del último plano.",
                };
                break;
            case MovieGenre.Horror:
                standard = new[]
                {
                    "Algo acecha en la oscuridad y nadie puede confiar en lo que ve.",
                    "{name} convierte el miedo en una experiencia visceral que no suelta al espectador.",
                    "Una presencia invisible desgarra la calma de quienes creen estar a salvo.",
                };
                epic = new[]
                {
                    "{name} redefine el terror con una pesadilla que trasciende la pantalla.",
                };
                break;
            case MovieGenre.Comedy:
                standard = new[]
                {
                    "Una cadena de malentendidos desata el caos más divertido del año.",
                    "{name} demuestra que reírse de uno mismo es el mejor remedio.",
                    "Cuando todo sale mal, el humor se convierte en la única salida.",
                };
                epic = new[]
                {
                    "{name} es una comedia memorable que mezcla ingenio y corazón.",
                };
                break;
            case MovieGenre.Romance:
                standard = new[]
                {
                    "Dos almas se cruzan en el momento menos esperado y nada volverá a ser igual.",
                    "{name} celebra el amor con ternura, conflicto y esperanza.",
                    "Entre miradas y silencios, nace una historia que desafía el destino.",
                };
                epic = new[]
                {
                    "{name} es un romance épico que permanece en la memoria.",
                };
                break;
            case MovieGenre.SciFi:
                standard = new[]
                {
                    "La ciencia y la moral chocan cuando el futuro llega antes de tiempo.",
                    "{name} plantea un dilema tecnológico con consecuencias imprevisibles.",
                    "En un universo expandido, la humanidad debe elegir entre progreso y supervivencia.",
                };
                epic = new[]
                {
                    "{name} redefine el género con una visión audaz del mañana.",
                };
                break;
            case MovieGenre.Fantasy:
                standard = new[]
                {
                    "Un reino olvidado despierta y convoca a héroes improbables.",
                    "{name} mezcla magia, peligro y destino en una aventura absorbente.",
                    "Lo imposible se vuelve real cuando la profecía exige un sacrificio.",
                };
                epic = new[]
                {
                    "{name} es una epopeya fantástica de gran escala y emoción.",
                };
                break;
            case MovieGenre.Thriller:
                standard = new[]
                {
                    "Cada pista esconde una trampa y la verdad siempre llega tarde.",
                    "{name} mantiene la tensión hasta el último segundo.",
                    "Un secreto enterrado amenaza con destruir a quienes intentan descubrirlo.",
                };
                epic = new[]
                {
                    "{name} es un thriller magistral que no da tregua.",
                };
                break;
            case MovieGenre.Animation:
                standard = new[]
                {
                    "Colores, ritmo y corazón se unen en una aventura para todas las edades.",
                    "{name} demuestra que la animación puede contar historias profundas.",
                    "Un mundo dibujado cobra vida con humor y emoción.",
                };
                epic = new[]
                {
                    "{name} es una obra animada que trasciende el formato.",
                };
                break;
            case MovieGenre.Documentary:
                standard = new[]
                {
                    "Hechos reales narrados con claridad y respeto por quienes vivieron la historia.",
                    "{name} revela un mundo desconocido con rigor y sensibilidad.",
                    "La cámara observa sin juzgar y deja que la verdad hable.",
                };
                epic = new[]
                {
                    "{name} es un documental impactante que invita a reflexionar.",
                };
                break;
            default:
                standard = new[]
                {
                    "{name} ofrece una experiencia cinematográfica memorable.",
                };
                epic = new[]
                {
                    "{name} destaca por su ambición y su fuerza narrativa.",
                };
                break;
        }

        return highRarity ? epic : standard;
    }
}
