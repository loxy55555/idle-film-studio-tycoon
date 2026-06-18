/// <summary>Primary bottom navigation tabs for the definitive HUD shell.</summary>
public enum MainHudTab
{
    Studio     = 0,
    Production = 1,
    Awards     = 2,
    Collection = 3,
    Shop       = 4,
}

public static class MainHudTabLabels
{
    public static string[] BottomNav => new[]
    {
        Loc.Get(LocKeys.NavStudio),
        Loc.Get(LocKeys.NavProduction),
        Loc.Get(LocKeys.NavAwards),
        Loc.Get(LocKeys.NavCollection),
        Loc.Get(LocKeys.NavShop),
    };

    public static readonly string[] BottomIcons =
    {
        "🎬",
        "🎥",
        "⭐",
        "📚",
        "🛒",
    };
}
