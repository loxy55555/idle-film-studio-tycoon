using System.Collections.Generic;

/// <summary>Production offer badge labels — NUEVA / SAGA / CONTRATO (Phase 8.0).</summary>
public static class MovieOfferBadgeHelper
{
    public static string BuildBadgeLine(
        MovieConfig cfg,
        IReadOnlyCollection<string> completedKeys,
        ContractSystem contracts,
        DepartmentSystem depts)
    {
        if (cfg == null) return string.Empty;

        var parts = new List<string>(3);

        if (completedKeys != null && !MovieCollectionService.IsDiscovered(cfg, completedKeys))
            parts.Add(Loc.Get(LocKeys.OfferBadgeNew));

        if (!string.IsNullOrEmpty(cfg.sagaId))
            parts.Add(Loc.Get(LocKeys.OfferBadgeSaga));

        if (contracts != null && contracts.DoesMovieHelpActiveContract(cfg, depts))
            parts.Add(Loc.Get(LocKeys.OfferBadgeContract));

        return parts.Count == 0 ? string.Empty : string.Join(" ", parts);
    }
}
