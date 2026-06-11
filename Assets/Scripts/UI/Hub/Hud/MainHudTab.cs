/// <summary>Primary bottom navigation tabs for the definitive HUD shell.</summary>
public enum MainHudTab
{
    Studio     = 0,
    Production = 1,
    Awards     = 2,
    Collection = 3,
    Menu       = 4,
}

public static class MainHudTabLabels
{
    public static readonly string[] BottomNav =
    {
        "ESTUDIO",
        "PRODUCCIÓN",
        "PREMIOS",
        "COLECCIÓN",
        "MENÚ",
    };

    public static readonly string[] BottomIcons =
    {
        "EST",
        "PRO",
        "PRE",
        "COL",
        "MEN",
    };
}
