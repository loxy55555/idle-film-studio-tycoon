#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Menu: IdleFilm / Generate Movie Database
/// Creates 180 MovieConfig ScriptableObject assets (30 per genre).
/// Original titles only — no registered trademarks.
/// </summary>
public static class MovieDatabaseBuilder
{
    private const string OUTPUT_PATH = "Assets/Data/Movies";

    [MenuItem("IdleFilm/Generate Movie Database")]
    public static void Generate()
    {
        Directory.CreateDirectory(OUTPUT_PATH);

        int created = 0;

        // ── ACTION ──────────────────────────────────────────────────────────
        var action = new MovieDef[]
        {
            // Level 1-3 (starter)
            new("Persecución en la Ciudad",    MovieGenre.Action, 1, 50,   80,   1f,  1.0f, 6f,  "#E74C3C", "Cuando el crimen toma las calles, un detective solitario actúa."),
            new("Rescate al Límite",           MovieGenre.Action, 1, 60,   95,   1.2f,1.0f, 7f,  "#E74C3C", "Una misión de rescate que pondrá a prueba todo."),
            new("Fuga Nocturna",               MovieGenre.Action, 1, 45,   72,   0.9f,1.0f, 5f,  "#E74C3C", "Dos horas para escapar de la ciudad."),
            // Level 4-5
            new("Operación Tormenta",          MovieGenre.Action, 4, 200,  340,  3f,  1.5f, 18f, "#E74C3C", "Un equipo de élite contra una amenaza global."),
            new("El Último Bastión",           MovieGenre.Action, 4, 180,  300,  2.8f,1.5f, 16f, "#C0392B", "Defender lo que queda del mundo civilizado."),
            new("Código Rojo",                 MovieGenre.Action, 4, 220,  370,  3.2f,1.5f, 20f, "#E74C3C", "La ciudad entera como campo de batalla."),
            // Level 6-7
            new("Cazadores de Sombras",        MovieGenre.Action, 6, 600,  1050, 7f,  2.0f, 35f, "#C0392B", "Élite vs. red criminal invisible."),
            new("Zona de Exclusión",           MovieGenre.Action, 6, 550,  960,  6.5f,2.0f, 32f, "#E74C3C", "Penetrar en territorio prohibido."),
            new("Retribución",                 MovieGenre.Action, 7, 700,  1200, 8f,  2.0f, 38f, "#E74C3C", "Justicia fuera del sistema."),
            // Level 8-10
            new("Impacto Directo",             MovieGenre.Action, 8, 1800, 3200, 18f, 2.5f, 60f, "#C0392B", "Nada puede detener la misión."),
            new("La Línea Roja",               MovieGenre.Action, 8, 1600, 2800, 16f, 2.5f, 55f, "#E74C3C", "Cuando cruzar la línea es lo correcto."),
            new("Tormenta de Acero",           MovieGenre.Action, 9, 2000, 3600, 20f, 2.5f, 65f, "#C0392B", "Una guerra que nadie ganará."),
            // Level 11-14
            new("Punto de Ruptura",            MovieGenre.Action, 11, 5000,9000, 45f, 3.0f,120f, "#E74C3C", "Todo o nada."),
            new("La Gran Ofensiva",            MovieGenre.Action, 11, 4800,8600, 43f, 3.0f,115f, "#C0392B", "La operación más ambiciosa jamás planeada."),
            new("Sin Regreso",                 MovieGenre.Action, 12, 5500,9900, 50f, 3.0f,130f, "#E74C3C", "Una misión sin vuelta atrás."),
            new("Fuego Cruzado",               MovieGenre.Action, 13, 7000,12600,60f, 3.0f,150f, "#C0392B", "Los aliados de hoy son los enemigos de mañana."),
            // Level 15-18
            new("Legado Bélico",               MovieGenre.Action, 15,14000,25200,110f,3.5f,240f, "#E74C3C", "La guerra que definió generaciones."),
            new("Destino Marcado",             MovieGenre.Action, 15,13000,23400,100f,3.5f,220f, "#C0392B", "No hay escapatoria del destino."),
            new("El Precio del Honor",         MovieGenre.Action, 16,16000,28800,125f,3.5f,260f, "#E74C3C", "Honor es lo único que queda."),
            new("Alianza Imposible",           MovieGenre.Action, 17,19000,34200,145f,3.5f,300f, "#C0392B", "Enemigos que deben unirse para sobrevivir."),
            // Level 19-22
            new("Amanecer de Hierro",          MovieGenre.Action, 19,40000,72000,280f,4.0f,480f, "#E74C3C", "El mundo al borde del abismo."),
            new("La Última Frontera",          MovieGenre.Action, 20,50000,90000,340f,4.0f,560f, "#C0392B", "Más allá de toda frontera conocida."),
            new("Colisión de Mundos",          MovieGenre.Action, 21,65000,117000,420f,4.0f,660f,"#E74C3C", "Cuando dos fuerzas igualadas choquan."),
            // Level 23-25 (endgame)
            new("Catástrofe Total",            MovieGenre.Action, 23,140000,252000,840f,4.5f,1080f,"#C0392B","El fin de la era."),
            new("Élite Absoluta",              MovieGenre.Action, 24,180000,324000,1050f,4.5f,1320f,"#E74C3C","Solo los mejores sobreviven."),
            new("Imperativo de Guerra",        MovieGenre.Action, 25,250000,450000,1400f,5.0f,1680f,"#C0392B","La batalla definitiva de la humanidad."),
            // filler
            new("Golpe Final",                 MovieGenre.Action, 3, 120,  200,  2f,  1.2f, 10f, "#E74C3C", "Un atraco que cambiará todo."),
            new("Velocidad Extrema",           MovieGenre.Action, 5, 350,  620,  4f,  1.8f, 25f, "#C0392B", "La velocidad como arma."),
            new("El Infiltrado",               MovieGenre.Action, 10,3000, 5400, 28f, 2.5f, 80f, "#E74C3C", "Vivir bajo identidad falsa."),
            new("Cazador Implacable",          MovieGenre.Action, 18,22000,39600,165f,3.8f,330f, "#C0392B", "Un cazador que nunca abandona la presa."),
        };

        // ── DRAMA ──────────────────────────────────────────────────────────
        var drama = new MovieDef[]
        {
            new("Cartas sin Respuesta",        MovieGenre.Drama, 1, 55,   88,  0.9f,1.0f,  7f, "#3498DB", "Una historia de amor que no puede ser."),
            new("El Peso del Silencio",        MovieGenre.Drama, 1, 50,   80,  0.8f,1.0f,  6f, "#3498DB", "Lo que no se dice destruye más que las palabras."),
            new("Raíces Rotas",                MovieGenre.Drama, 1, 60,   95,  1.0f,1.0f,  7f, "#2980B9", "Volver al pueblo que nos abandonó."),
            new("La Herencia",                 MovieGenre.Drama, 4, 190, 320,  2.5f,1.5f, 17f, "#3498DB", "Una familia divida por el dinero."),
            new("Cenizas de Verano",           MovieGenre.Drama, 4, 200, 340,  2.8f,1.5f, 18f, "#2980B9", "Un verano que lo cambió todo."),
            new("La Última Cena",              MovieGenre.Drama, 5, 280, 480,  3.5f,1.5f, 22f, "#3498DB", "Una familia que se despide."),
            new("El Fotógrafo",                MovieGenre.Drama, 6, 580, 990,  6.0f,2.0f, 33f, "#2980B9", "Capturar la belleza del dolor."),
            new("Marea Alta",                  MovieGenre.Drama, 6, 600,1020,  6.5f,2.0f, 35f, "#3498DB", "Una ciudad inundada, un amor que resiste."),
            new("El Poeta Olvidado",           MovieGenre.Drama, 7, 750,1280,  8.0f,2.0f, 40f, "#2980B9", "Rescatar la obra de un genio incomprendido."),
            new("Sombras de Familia",          MovieGenre.Drama, 8,1700,2900,  15f, 2.5f, 58f, "#3498DB", "Los secretos que se heredan."),
            new("Años de Plomo",               MovieGenre.Drama, 9,2100,3780,  22f, 2.5f, 68f, "#2980B9", "Crecer en tiempos difíciles."),
            new("La Promesa",                  MovieGenre.Drama,10,3100,5580,  30f, 2.5f, 82f, "#3498DB", "Una promesa que atraviesa décadas."),
            new("Tiempo Prestado",             MovieGenre.Drama,11,4900,8820,  44f, 3.0f,118f, "#2980B9", "Vivir sabiendo que el tiempo se acaba."),
            new("Espejo Roto",                 MovieGenre.Drama,12,5600,10080, 52f, 3.0f,132f, "#3498DB", "Reconstruirse después de la traición."),
            new("El Nombre del Padre",         MovieGenre.Drama,13,7200,12960, 62f, 3.0f,152f, "#2980B9", "Buscar al padre que nunca estuvo."),
            new("Volver a Empezar",            MovieGenre.Drama,14,9000,16200, 75f, 3.0f,180f, "#3498DB", "Nada es imposible si empiezas desde cero."),
            new("La Mujer del Puerto",         MovieGenre.Drama,15,13500,24300,105f,3.5f,235f, "#2980B9", "Esperar al amor durante treinta años."),
            new("Tierra de Nadie",             MovieGenre.Drama,16,16500,29700,128f,3.5f,265f, "#3498DB", "Entre dos mundos, sin pertenecer a ninguno."),
            new("El Último Cuadro",            MovieGenre.Drama,17,20000,36000,150f,3.5f,305f, "#2980B9", "Pintar la obra maestra antes de morir."),
            new("La Gran Ilusión",             MovieGenre.Drama,18,23000,41400,170f,3.8f,340f, "#3498DB", "Un sueño demasiado grande para ser real."),
            new("Puentes de Papel",            MovieGenre.Drama,19,42000,75600,290f,4.0f,495f, "#2980B9", "Las palabras que construyen y destruyen."),
            new("Mar de Fondo",                MovieGenre.Drama,20,52000,93600,355f,4.0f,580f, "#3498DB", "Lo que viene de lo profundo no se puede ignorar."),
            new("La Dignidad",                 MovieGenre.Drama,21,68000,122400,435f,4.0f,675f,"#2980B9","Mantener la dignidad cuando todo se derrumba."),
            new("Generaciones",                MovieGenre.Drama,23,145000,261000,870f,4.5f,1100f,"#3498DB","Cien años de historia familiar."),
            new("El Círculo",                  MovieGenre.Drama,24,185000,333000,1080f,4.5f,1350f,"#2980B9","La historia se repite, generación tras generación."),
            new("Vida y Obra",                 MovieGenre.Drama,25,260000,468000,1450f,5.0f,1720f,"#3498DB","La biograf\u00eda de un siglo."),
            new("Invierno Eterno",             MovieGenre.Drama, 3, 130, 210,  2.0f,1.2f, 11f, "#2980B9", "Un frío que dura más que el invierno."),
            new("Lo Que Quedó",                MovieGenre.Drama, 5, 330, 580,  3.8f,1.8f, 24f, "#3498DB", "Los objetos guardan la memoria de sus dueños."),
            new("Confesiones",                 MovieGenre.Drama,10,2900,5200,  27f, 2.5f, 78f, "#2980B9", "Decir la verdad siempre tiene consecuencias."),
            new("Deuda de Sangre",             MovieGenre.Drama,22,85000,153000,530f,4.2f,780f,"#3498DB","Una deuda que se paga con el tiempo."),
        };

        // ── HORROR ─────────────────────────────────────────────────────────
        var horror = new MovieDef[]
        {
            new("La Casa del Fin",             MovieGenre.Horror, 1, 55,  90,   0.9f,1.0f,  6f, "#8E44AD", "Una casa que no deja salir a sus huéspedes."),
            new("Cosas que Acechan",           MovieGenre.Horror, 1, 48,  78,   0.8f,1.0f,  5f, "#7D3C98", "No mires debajo de la cama."),
            new("El Último Grito",             MovieGenre.Horror, 1, 60,  96,   1.0f,1.0f,  7f, "#8E44AD", "Cinco amigos, una cabaña, ningún retorno."),
            new("Sombras del Pasado",          MovieGenre.Horror, 4, 195, 330,  2.6f,1.5f, 17f, "#7D3C98", "El pasado siempre regresa."),
            new("La Criatura del Pantano",     MovieGenre.Horror, 4, 185, 315,  2.4f,1.5f, 16f, "#8E44AD", "Algo antiguo despertó en las aguas."),
            new("Pesadilla Recurrente",        MovieGenre.Horror, 5, 270, 460,  3.3f,1.5f, 21f, "#7D3C98", "El sueño no es el refugio que creías."),
            new("El Ritual",                   MovieGenre.Horror, 6, 590,1005,  6.2f,2.0f, 34f, "#8E44AD", "Ceremonias que no deben realizarse."),
            new("Carne y Hueso",               MovieGenre.Horror, 7, 720,1224,  7.5f,2.0f, 38f, "#7D3C98", "El monstruo más aterrador es el humano."),
            new("Oscuridad Total",             MovieGenre.Horror, 7, 780,1326,  8.5f,2.0f, 42f, "#8E44AD", "Sin luz, el miedo se amplifica."),
            new("La Infección",                MovieGenre.Horror, 8,1650,2805,  14f, 2.5f, 55f, "#7D3C98", "Un virus que convierte lo humano en monstruo."),
            new("Gritos en el Bosque",         MovieGenre.Horror, 9,2050,3485,  21f, 2.5f, 66f, "#8E44AD", "El bosque recuerda lo que enterramos."),
            new("La Máscara",                  MovieGenre.Horror,10,3050,5185,  29f, 2.5f, 80f, "#7D3C98", "Detrás de cada máscara hay una historia sangrienta."),
            new("Punto Sin Retorno",           MovieGenre.Horror,11,4850,8245,  43f, 3.0f,116f, "#8E44AD", "Ya no hay camino de vuelta."),
            new("El Exorcismo de Villa Roja",  MovieGenre.Horror,12,5500,9350,  51f, 3.0f,130f, "#7D3C98", "Una posesión que desafía toda lógica."),
            new("Aberración",                  MovieGenre.Horror,13,7100,12070, 61f, 3.0f,148f, "#8E44AD", "Algo salió mal en el laboratorio."),
            new("La Guarida",                  MovieGenre.Horror,14,8800,14960, 73f, 3.0f,177f, "#7D3C98", "Su hogar ya no les pertenece."),
            new("Eco del Infierno",            MovieGenre.Horror,15,13200,22440,103f,3.5f,230f, "#8E44AD", "Voces que vienen del más allá."),
            new("Caza Nocturna",               MovieGenre.Horror,16,16200,27540,126f,3.5f,262f, "#7D3C98", "La noche pertenece a los que cazan."),
            new("Sangre en las Paredes",       MovieGenre.Horror,17,19500,33150,148f,3.5f,298f, "#8E44AD", "Una casa construida sobre secretos."),
            new("La Plaga",                    MovieGenre.Horror,18,22500,38250,168f,3.8f,335f, "#7D3C98", "Cuando la enfermedad toma control."),
            new("Dimensión Oscura",            MovieGenre.Horror,19,41000,69700,283f,4.0f,485f, "#8E44AD", "Una grieta en la realidad deja pasar algo."),
            new("El Culto",                    MovieGenre.Horror,20,51000,86700,348f,4.0f,570f, "#7D3C98", "Una secta que nunca deja ir a sus miembros."),
            new("Bestia",                      MovieGenre.Horror,21,66000,112200,428f,4.0f,662f,"#8E44AD","Una criatura que desafía toda clasificación científica."),
            new("Maldición Ancestral",         MovieGenre.Horror,23,138000,234600,828f,4.5f,1065f,"#7D3C98","Una maldición que dura siglos."),
            new("El Portal",                   MovieGenre.Horror,24,178000,302600,1030f,4.5f,1300f,"#8E44AD","Abrir la puerta fue el peor error."),
            new("Terror Absoluto",             MovieGenre.Horror,25,245000,416500,1380f,5.0f,1650f,"#7D3C98","El miedo máximo que el cine puede ofrecer."),
            new("Noche de los Muertos",        MovieGenre.Horror, 3, 125, 205,  1.9f,1.2f, 10f, "#8E44AD", "Los muertos vuelven con hambre."),
            new("El Psicópata",                MovieGenre.Horror, 5, 340, 595,  3.9f,1.8f, 25f, "#7D3C98", "Una mente retorcida, una ciudad aterrorizada."),
            new("Caja Negra",                  MovieGenre.Horror,10,2950,5015,  28f, 2.5f, 79f, "#8E44AD", "Encontrar la caja fue el principio del fin."),
            new("Engendro",                    MovieGenre.Horror,22,83000,141100,518f,4.2f,770f,"#7D3C98","Una creación que se volvió contra su creador."),
        };

        // ── COMEDY ─────────────────────────────────────────────────────────
        var comedy = new MovieDef[]
        {
            new("Mi Cuñado Es un Desastre",    MovieGenre.Comedy, 1, 52,  84,   0.85f,1.0f, 6f, "#F39C12", "La familia política más caótica del barrio."),
            new("El Chef Accidental",          MovieGenre.Comedy, 1, 48,  77,   0.78f,1.0f, 5f, "#D35400", "Un fontanero que acaba dirigiendo un restaurante."),
            new("Vacaciones en Crisis",        MovieGenre.Comedy, 1, 58,  92,   0.95f,1.0f, 7f, "#F39C12", "El peor viaje familiar de la historia."),
            new("El Gran Malentendido",        MovieGenre.Comedy, 4, 188, 318,  2.4f, 1.5f,16f, "#D35400", "Una cadena de errores que no para de crecer."),
            new("Operación Suegra",            MovieGenre.Comedy, 4, 198, 336,  2.7f, 1.5f,17f, "#F39C12", "Impresionar a la suegra es cuestión de vida o muerte."),
            new("El Detective Torpe",          MovieGenre.Comedy, 5, 265, 451,  3.2f, 1.5f,21f, "#D35400", "Resolver crímenes por accidente."),
            new("Cuerpos Cambiados",           MovieGenre.Comedy, 6, 575, 978,  5.8f, 2.0f,32f, "#F39C12", "Un niño en el cuerpo de un CEO."),
            new("El Doble Perfecto",           MovieGenre.Comedy, 7, 710,1207, 7.2f,  2.0f,37f, "#D35400", "Hacerse pasar por famoso tiene sus riesgos."),
            new("El Político Honesto",         MovieGenre.Comedy, 7, 760,1292, 8.0f,  2.0f,41f, "#F39C12", "El único político que no miente sobrevive... por poco."),
            new("Boda de Película",            MovieGenre.Comedy, 8,1620,2754, 13f,   2.5f,53f, "#D35400", "Organizar una boda en 48 horas."),
            new("El Millonario Accidental",    MovieGenre.Comedy, 9,2030,3451, 20f,   2.5f,64f, "#F39C12", "Ganar la lotería es el inicio de los problemas."),
            new("Caos Total",                  MovieGenre.Comedy,10,3000,5100, 28f,   2.5f,79f, "#D35400", "Un día donde todo sale mal a la vez."),
            new("El Jefe Imposible",           MovieGenre.Comedy,11,4800,8160, 42f,   3.0f,114f,"#F39C12", "Trabajar para el peor jefe del mundo."),
            new("Familia en Fuga",             MovieGenre.Comedy,12,5400,9180, 50f,   3.0f,128f,"#D35400", "Escapar de la familia en unas vacaciones."),
            new("El Novio Fantasma",           MovieGenre.Comedy,13,7000,11900, 60f,  3.0f,146f,"#F39C12", "Cuando tu prometido resulta ser un espíritu."),
            new("Los Vecinos del Caos",        MovieGenre.Comedy,14,8700,14790, 72f,  3.0f,175f,"#D35400", "Vivir junto a la familia más ruidosa del barrio."),
            new("El Agente Secreto Torpe",     MovieGenre.Comedy,15,13000,22100,101f, 3.5f,228f,"#F39C12", "La CIA no entrenó para esto."),
            new("Intercambio de Vidas",        MovieGenre.Comedy,16,16000,27200,124f, 3.5f,260f,"#D35400", "Vivir la vida del otro es más difícil de lo que parece."),
            new("El Papá Modernísimo",         MovieGenre.Comedy,17,19000,32300,146f, 3.5f,295f,"#F39C12", "Criar hijos en la era digital."),
            new("Superstar Accidental",        MovieGenre.Comedy,18,22000,37400,165f, 3.8f,330f,"#D35400", "Viral por accidente, famoso por necesidad."),
            new("La Reunión de Excompañeros",  MovieGenre.Comedy,19,40000,68000,278f, 4.0f,479f,"#F39C12", "Reunirse veinte años después."),
            new("El Falso Médico",             MovieGenre.Comedy,20,50000,85000,342f, 4.0f,562f,"#D35400", "Pasarse por doctor sin estudios fue mala idea."),
            new("Doble Identidad",             MovieGenre.Comedy,21,64000,108800,421f,4.0f,655f,"#F39C12","Ser dos personas a la vez agota."),
            new("La Gran Comedia Nacional",    MovieGenre.Comedy,23,136000,231200,818f,4.5f,1048f,"#D35400","Una sátira del país que todos reconocerán."),
            new("El Show Debe Continuar",      MovieGenre.Comedy,24,176000,299200,1022f,4.5f,1285f,"#F39C12","En el escenario del mundo, todos actuamos."),
            new("El Último Cómico",            MovieGenre.Comedy,25,240000,408000,1360f,5.0f,1625f,"#D35400","El último grande del humor."),
            new("Novatos",                     MovieGenre.Comedy, 3, 122, 198,  1.85f,1.2f,10f, "#F39C12", "Su primer día de trabajo."),
            new("El Vecino Espía",             MovieGenre.Comedy, 5, 320, 554,  3.7f, 1.8f,24f, "#D35400", "El vecino más curioso del edificio."),
            new("Amor en Cuarentena",          MovieGenre.Comedy,10,2850,4845, 26f,   2.5f,76f, "#F39C12", "Enamorarse en el peor momento posible."),
            new("El Heredero Rebelde",         MovieGenre.Comedy,22,81000,137700,507f, 4.2f,756f,"#D35400","Heredar un imperio que no quieres."),
        };

        // ── ROMANCE ────────────────────────────────────────────────────────
        var romance = new MovieDef[]
        {
            new("Primer Café",                 MovieGenre.Romance, 1, 50,  80,   0.8f, 1.0f, 6f, "#E91E8C", "Dos desconocidos en la misma cafetería."),
            new("El Billete de Tren",          MovieGenre.Romance, 1, 54,  87,   0.88f,1.0f, 6f, "#C2185B", "Un viaje en tren cambia su vida para siempre."),
            new("Cartas de Abril",             MovieGenre.Romance, 1, 60,  96,   1.0f, 1.0f, 7f, "#E91E8C", "Un amor epistolar que desafía el tiempo."),
            new("Dos Mundos Distintos",        MovieGenre.Romance, 4, 190, 323,  2.55f,1.5f,17f, "#C2185B", "Él de clase alta, ella de barrio. El amor no entiende de clases."),
            new("El Restaurante Secreto",      MovieGenre.Romance, 4, 200, 340,  2.8f, 1.5f,18f, "#E91E8C", "Enamorarse del chef que nunca muestra su cara."),
            new("Verano en la Costa",          MovieGenre.Romance, 5, 275, 468,  3.4f, 1.5f,22f, "#C2185B", "Un amor de verano que podría durar toda la vida."),
            new("La Librería de los Sueños",   MovieGenre.Romance, 6, 585, 995,  6.1f, 2.0f,34f, "#E91E8C", "Encontrarse entre las páginas."),
            new("Promesas Bajo la Lluvia",     MovieGenre.Romance, 7, 725,1233,  7.6f, 2.0f,39f, "#C2185B", "Una promesa hecha en la tormenta."),
            new("El Destino Tenía un Plan",    MovieGenre.Romance, 7, 775,1318,  8.2f, 2.0f,42f, "#E91E8C", "Dos personas que el universo siempre une."),
            new("Amor en Tokyo",               MovieGenre.Romance, 8,1640,2788, 13.5f,2.5f,55f, "#C2185B", "Un amor que nació entre tradiciones distintas."),
            new("La Segunda Oportunidad",      MovieGenre.Romance, 9,2040,3468, 20.5f,2.5f,65f, "#E91E8C", "Reconectar con el amor perdido años atrás."),
            new("El Contrato Nupcial",         MovieGenre.Romance,10,3020,5134, 28.5f,2.5f,80f, "#C2185B", "Casarse por acuerdo y enamorarse por accidente."),
            new("Flores de Invierno",          MovieGenre.Romance,11,4820,8194, 42.5f,3.0f,115f,"#E91E8C","Amor que florece en el frío."),
            new("Siete Días",                  MovieGenre.Romance,12,5450,9265, 50.5f,3.0f,129f,"#C2185B","Solo tienen una semana para decidir."),
            new("La Fotógrafa de Guerra",      MovieGenre.Romance,13,7050,11985, 60.5f,3.0f,147f,"#E91E8C","Un amor nació donde menos se esperaba."),
            new("El Hombre del Sombrero",      MovieGenre.Romance,14,8750,14875, 72.5f,3.0f,176f,"#C2185B","Un misterioso desconocido en el mercado."),
            new("Danzando en la Oscuridad",    MovieGenre.Romance,15,13100,22270,102f, 3.5f,229f,"#E91E8C","Bailar sin poder verse."),
            new("Más Allá del Océano",         MovieGenre.Romance,16,16100,27370,125f, 3.5f,261f,"#C2185B","Un amor que sobrevive la distancia."),
            new("El Arquitecto de Sueños",     MovieGenre.Romance,17,19200,32640,147f, 3.5f,296f,"#E91E8C","Construir juntos algo más que una casa."),
            new("Medianoche en Roma",          MovieGenre.Romance,18,22200,37740,166f, 3.8f,331f,"#C2185B","Una noche en la ciudad eterna."),
            new("La Pianista",                 MovieGenre.Romance,19,40500,68850,280f, 4.0f,482f,"#E91E8C","La música como lenguaje del amor."),
            new("Eterno Retorno",              MovieGenre.Romance,20,50500,85850,344f, 4.0f,566f,"#C2185B","El amor que se repite a través del tiempo."),
            new("El Amor Prohibido",           MovieGenre.Romance,21,65000,110500,423f,4.0f,658f,"#E91E8C","Amar cuando el mundo lo prohíbe."),
            new("La Historia de Amor del Siglo",MovieGenre.Romance,23,137000,232900,823f,4.5f,1052f,"#C2185B","Dos vidas entrelazadas por el destino."),
            new("Eternamente",                 MovieGenre.Romance,24,177000,300900,1028f,4.5f,1308f,"#E91E8C","El amor más grande jamás filmado."),
            new("Amor Sin Fin",                MovieGenre.Romance,25,242000,411400,1368f,5.0f,1635f,"#C2185B","Una historia de amor que dura para siempre."),
            new("La Nota de la Canción",       MovieGenre.Romance, 3, 123, 200,  1.88f,1.2f,10f, "#E91E8C", "Una canción que los unió."),
            new("El Barista",                  MovieGenre.Romance, 5, 325, 559,  3.75f,1.8f,24f, "#C2185B", "Enamorarse de la persona que prepara tu café."),
            new("Corazones Cruzados",          MovieGenre.Romance,10,2870,4879, 26.5f,2.5f,77f, "#E91E8C","Dos almas que siempre se encuentran."),
            new("La Propuesta",                MovieGenre.Romance,22,82000,139400,513f, 4.2f,762f,"#C2185B","La propuesta más inesperada del año."),
        };

        // ── SCI-FI ──────────────────────────────────────────────────────────
        var scifi = new MovieDef[]
        {
            new("La Señal",                    MovieGenre.SciFi, 1, 58,  93,   0.95f,1.0f, 7f, "#27AE60", "Una señal del espacio cambia todo."),
            new("Protocolo Omega",             MovieGenre.SciFi, 1, 53,  85,   0.87f,1.0f, 6f, "#1ABC9C", "Un protocolo que no debía activarse."),
            new("Año 2199",                    MovieGenre.SciFi, 1, 62,  99,   1.02f,1.0f, 7f, "#27AE60", "El futuro tiene sus propios problemas."),
            new("La Colonia",                  MovieGenre.SciFi, 4, 195, 332,  2.6f, 1.5f,17f, "#1ABC9C", "Sobrevivir en un planeta hostil."),
            new("Transferencia Mental",        MovieGenre.SciFi, 4, 205, 348,  2.85f,1.5f,18f, "#27AE60", "¿Qué eres sin tu memoria?"),
            new("El Último Android",           MovieGenre.SciFi, 5, 280, 476,  3.45f,1.5f,22f, "#1ABC9C", "Una máquina que aprendió a sentir."),
            new("Génesis II",                  MovieGenre.SciFi, 6, 595,1012,  6.3f, 2.0f,35f, "#27AE60", "Recrear la vida desde cero."),
            new("El Tiempo No Perdona",        MovieGenre.SciFi, 7, 730,1241, 7.7f,  2.0f,39f, "#1ABC9C", "Un viajero del tiempo que no puede parar."),
            new("Nexo",                        MovieGenre.SciFi, 7, 770,1309, 8.1f,  2.0f,41f, "#27AE60", "La conexión entre dimensiones paralelas."),
            new("Invasión Silenciosa",         MovieGenre.SciFi, 8,1660,2822, 13.8f,2.5f,55f, "#1ABC9C", "No sabes que están aquí... hasta que es tarde."),
            new("El Proyecto Arca",            MovieGenre.SciFi, 9,2060,3502, 20.8f,2.5f,66f, "#27AE60", "Construir la última nave antes del fin."),
            new("Frontera Cuántica",           MovieGenre.SciFi,10,3050,5185, 29f,   2.5f,81f, "#1ABC9C", "La física es solo la primera barrera."),
            new("Algoritmo",                   MovieGenre.SciFi,11,4870,8279, 43.5f,3.0f,117f,"#27AE60","La IA que se volvió más inteligente que sus creadores."),
            new("Los Inmortales",              MovieGenre.SciFi,12,5480,9316, 51f,   3.0f,131f,"#1ABC9C","¿Qué pasa cuando no puedes morir?"),
            new("Singularidad",                MovieGenre.SciFi,13,7080,12036, 61f,  3.0f,149f,"#27AE60","El momento en que la máquina supera al humano."),
            new("Mundo Espejo",                MovieGenre.SciFi,14,8820,14994, 73f,  3.0f,178f,"#1ABC9C","Descubrir que tu mundo es una simulación."),
            new("La Gran Migración",           MovieGenre.SciFi,15,13200,22440,103f, 3.5f,232f,"#27AE60","Abandonar la Tierra para siempre."),
            new("Paradoja",                    MovieGenre.SciFi,16,16200,27540,126f, 3.5f,263f,"#1ABC9C","Cambiar el pasado rompe el futuro."),
            new("Código de Vida",              MovieGenre.SciFi,17,19500,33150,148f, 3.5f,299f,"#27AE60","Reescribir el ADN humano."),
            new("La Última Galaxia",           MovieGenre.SciFi,18,22500,38250,168f, 3.8f,336f,"#1ABC9C","Explorar los confines del universo conocido."),
            new("Mente Colmena",               MovieGenre.SciFi,19,41500,70550,285f, 4.0f,488f,"#27AE60","Una conciencia compartida por millones."),
            new("El Código Estelar",           MovieGenre.SciFi,20,51500,87550,350f, 4.0f,574f,"#1ABC9C","Un mensaje oculto en las estrellas."),
            new("Transición",                  MovieGenre.SciFi,21,66500,113050,430f,4.0f,665f,"#27AE60","La humanidad ante su próxima evolución."),
            new("Dominio Cósmico",             MovieGenre.SciFi,23,139000,236300,832f,4.5f,1070f,"#1ABC9C","Controlar el cosmos entero."),
            new("La Ecuación Final",           MovieGenre.SciFi,24,179000,304300,1035f,4.5f,1310f,"#27AE60","La teoría que lo explica todo."),
            new("Génesis Universal",           MovieGenre.SciFi,25,247000,419900,1390f,5.0f,1660f,"#1ABC9C","El origen del universo, revelado."),
            new("Planeta Virgen",              MovieGenre.SciFi, 3, 127, 207,  1.92f,1.2f,10f, "#27AE60", "El primer planeta habitable encontrado."),
            new("Rebelde Digital",             MovieGenre.SciFi, 5, 330, 574,  3.8f, 1.8f,24f, "#1ABC9C", "Un hacker que lucha contra el sistema global."),
            new("La Anomalía",                 MovieGenre.SciFi,10,2920,4964, 27.5f,2.5f,78f, "#27AE60","Una anomalía que rompe las leyes de la física."),
            new("Dominación",                  MovieGenre.SciFi,22,84000,142800,522f, 4.2f,774f,"#1ABC9C","La conquista del espacio como destino humano."),
        };

        foreach (var defs in new[] { action, drama, horror, comedy, romance, scifi })
        {
            foreach (var def in defs)
            {
                string slug   = def.name.Replace(" ","_").Replace("ó","o").Replace("é","e")
                                        .Replace("á","a").Replace("í","i").Replace("ú","u")
                                        .Replace("ñ","n").Replace("ü","u").Replace(",","")
                                        .Replace(".","").Replace("(","").Replace(")","")
                                        .Replace("'","").Replace("¿","").Replace("?","")
                                        .Replace("¡","").Replace("!","");
                string assetPath = $"{OUTPUT_PATH}/{def.genre}_{slug}.asset";

                // Skip if already exists
                var existing = AssetDatabase.LoadAssetAtPath<MovieConfig>(assetPath);
                if (existing != null) { created++; continue; }

                var cfg = UnityEngine.ScriptableObject.CreateInstance<MovieConfig>();
                cfg.movieName          = def.name;
                cfg.genre              = def.genre;
                cfg.tagline            = def.tagline;
                cfg.unlockStudioLevel  = def.unlockLevel;
                cfg.cost               = def.cost;
                cfg.baseReward         = def.reward;
                cfg.baseRep            = def.rep;
                cfg.duration           = def.duration;
                cfg.quality            = def.quality;
                cfg.posterColorHex     = def.color;
                cfg.unlockReputation   = 0;

                AssetDatabase.CreateAsset(cfg, assetPath);
                created++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[MovieDB] Generated/verified {created} movie assets in {OUTPUT_PATH}");
    }

    // ─── Data definition helper ───────────────────────────────────────────────

    private struct MovieDef
    {
        public string     name;
        public MovieGenre genre;
        public int        unlockLevel;
        public long       cost;
        public long       reward;
        public float      rep;
        public float      quality;
        public float      duration;
        public string     color;
        public string     tagline;

        public MovieDef(string n, MovieGenre g, int ul, long c, long r,
                        float rep, float q, float dur, string col, string tag)
        { name=n; genre=g; unlockLevel=ul; cost=c; reward=r;
          this.rep=rep; quality=q; duration=dur; color=col; tagline=tag; }
    }
}
#endif
