/// <summary>
/// Endgame gate — contracts stop being relevant once the player has enough Oscars for City 8.
/// Visual/system hooks (hide contracts, legendary hunt, collection) will read this later.
/// </summary>
public static class EndgameProgressionRules
{
    public static int CityEightOscarThreshold =>
        CityLevelDatabase.GetLevel(CityLevelDatabase.MaxLevel).requiredOscars;

    public static bool IsEndgame(int totalOscars) =>
        totalOscars >= CityEightOscarThreshold;

    public static bool IsEndgame(GameHub hub) =>
        hub?.prestige != null && IsEndgame(hub.prestige.oscars);
}
