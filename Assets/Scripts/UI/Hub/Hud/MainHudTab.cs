/// <summary>Primary bottom navigation tabs for the definitive HUD shell.</summary>
public enum MainHudTab
{
    Production = 0,
    Studio     = 1,
    Awards     = 2,
    Collection = 3,
    Menu       = 4,
}

public static class MainHudTabLabels
{
    public static readonly string[] BottomNav =
    {
        "PRODUCCIÓN",
        "ESTUDIO",
        "PREMIOS",
        "COLECCIÓN",
        "MENÚ",
    };

    public static readonly string[] BottomIcons =
    {
        "PRO",
        "EST",
        "PRE",
        "COL",
        "MEN",
    };
}
