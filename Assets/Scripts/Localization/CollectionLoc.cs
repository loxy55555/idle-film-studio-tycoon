/// <summary>Collection screen localization (Phase 8.6).</summary>
public static class CollectionLoc
{
    public static string Title() => Loc.Get(LocKeys.CollectionTitle);

    public static string GetGenreLabel(MovieGenre genre) => GenreLoc.GetLabel(genre);

    public static string GetRarityLabel(MovieRarity rarity) => ProductionLoc.GetRarityLabel(rarity);

    public static string FormatGlobalProgress(int discovered, int target) =>
        Loc.Format(LocKeys.CollectionGlobalProgress, discovered, target);

    public static string FormatPercent(float percent) =>
        Loc.Format(LocKeys.CollectionPercent, percent.ToString("0"));

    public static string FormatGenreProgress(MovieGenre genre, int discovered, int total) =>
        Loc.Format(LocKeys.CollectionGenreProgress,
            discovered,
            total,
            GetGenreLabel(genre));

    public static string UnknownTitle() => Loc.Get(LocKeys.CollectionUnknownTitle);

    public static string LockedLabel() => Loc.Get(LocKeys.CollectionLocked);

    public static string DiscoveredLabel() => Loc.Get(LocKeys.CollectionDiscovered);

    public static string LegendaryLockCondition() => Loc.Get(LocKeys.CollectionLegendaryLock);
}
