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
            parts.Add("[NUEVA]");

        if (!string.IsNullOrEmpty(cfg.sagaId))
            parts.Add("[SAGA]");

        if (contracts != null && contracts.DoesMovieHelpActiveContract(cfg, depts))
            parts.Add("[CONTRATO]");

        return parts.Count == 0 ? string.Empty : string.Join(" ", parts);
    }
}
