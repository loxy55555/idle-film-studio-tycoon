/// <summary>Production UI localization helpers (Phase 8.2).</summary>
public static class ProductionLoc
{
    public static string GetRarityLabel(MovieRarity rarity) =>
        Loc.Get(MovieRarityVisual.GetLocKey(rarity));

    public static string FormatDuration(float seconds) =>
        Loc.Format(LocKeys.ProdDurationFormat, seconds.ToString("0.0"));

    public static string FormatTimeRemaining(float seconds)
    {
        int s = UnityEngine.Mathf.CeilToInt(seconds);
        if (s >= 60)
            return Loc.Format(LocKeys.ProdTimeRemainingMinutes, s / 60, s % 60);
        return Loc.Format(LocKeys.ProdTimeRemainingSeconds, s);
    }

    public static string FormatHistoryRewards(long money, float rep, float xp) =>
        Loc.Format(LocKeys.ProdHistoryRewards,
            AnimatedMoneyText.FormatMoney(money),
            rep.ToString("0.0"),
            xp.ToString("0"));
}

public static class GenreLoc
{
    public static string GetLabel(MovieGenre genre) =>
        Loc.Get(GenreLocKeys.Key(genre));
}

public static class GenreLocKeys
{
    public static string Key(MovieGenre genre) => genre switch
    {
        MovieGenre.Action  => LocKeys.GenreAction,
        MovieGenre.Drama   => LocKeys.GenreDrama,
        MovieGenre.Horror  => LocKeys.GenreHorror,
        MovieGenre.Comedy  => LocKeys.GenreComedy,
        MovieGenre.Romance => LocKeys.GenreRomance,
        MovieGenre.SciFi       => LocKeys.GenreSciFi,
        MovieGenre.Fantasy     => LocKeys.GenreFantasy,
        MovieGenre.Thriller    => LocKeys.GenreThriller,
        MovieGenre.Animation   => LocKeys.GenreAnimation,
        MovieGenre.Documentary => LocKeys.GenreDocumentary,
        _                      => LocKeys.GenreAction,
    };
}
