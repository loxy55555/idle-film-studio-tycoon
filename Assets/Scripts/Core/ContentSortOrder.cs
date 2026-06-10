using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Runtime content ordering for hub panels (visual layout unchanged).</summary>
public static class ContentSortOrder
{
    public enum MovieBucket
    {
        Available = 0,
        LockedByCity = 1,
        LockedByOther = 2,
    }

    public enum UpgradeBucket
    {
        PurchasableNow = 0,
        UnlockedNoMoney = 1,
        LockedByCity = 2,
    }

    public enum ContractBucket
    {
        Claimable = 0,
        InProgress = 1,
        Other = 2,
    }

    public static MovieBucket GetMovieBucket(MovieConfig cfg, StudioManager studio, StudioLevelSystem level, CitySystem city)
    {
        if (cfg == null) return MovieBucket.LockedByOther;

        int studioLvl = level?.Level ?? 1;
        float rep = studio?.reputation ?? 0f;

        if (studio != null && studio.IsMovieCompleted(cfg))
            return MovieBucket.LockedByOther;

        if (city != null && !city.IsMovieUnlocked(cfg))
            return MovieBucket.LockedByCity;

        if (cfg.unlockStudioLevel > 0 && studioLvl < cfg.unlockStudioLevel)
            return MovieBucket.LockedByOther;
        if (cfg.unlockReputation > 0 && rep < cfg.unlockReputation)
            return MovieBucket.LockedByOther;

        return MovieBucket.Available;
    }

    public static int CompareMovies(MovieConfig a, MovieConfig b, StudioManager studio, StudioLevelSystem level, CitySystem city)
    {
        int bucket = GetMovieBucket(a, studio, level, city).CompareTo(GetMovieBucket(b, studio, level, city));
        if (bucket != 0) return bucket;
        return a.cost.CompareTo(b.cost);
    }

    public static void SortMovies(List<MovieConfig> list, StudioManager studio, StudioLevelSystem level, CitySystem city)
    {
        if (list == null || list.Count < 2) return;
        list.Sort((a, b) => CompareMovies(a, b, studio, level, city));
    }

    public static UpgradeBucket GetUpgradeBucket(UpgradeConfig cfg, UpgradeSystem upgrades, StudioManager studio, StudioLevelSystem level, CitySystem city)
    {
        if (cfg == null || upgrades == null) return UpgradeBucket.LockedByCity;

        int studioLvl = level?.Level ?? 1;
        long money = studio?.Money ?? 0;

        if (city != null && !city.IsUpgradeUnlocked(cfg))
            return UpgradeBucket.LockedByCity;
        if (studioLvl < cfg.unlockStudioLevel)
            return UpgradeBucket.LockedByCity;
        if (upgrades.IsMaxLevel(cfg))
            return UpgradeBucket.LockedByCity;
        if (upgrades.CanPurchase(cfg, money, studioLvl))
            return UpgradeBucket.PurchasableNow;
        return UpgradeBucket.UnlockedNoMoney;
    }

    public static int CompareUpgrades(UpgradeConfig a, UpgradeConfig b, UpgradeSystem upgrades, StudioManager studio, StudioLevelSystem level, CitySystem city)
    {
        int bucket = GetUpgradeBucket(a, upgrades, studio, level, city)
            .CompareTo(GetUpgradeBucket(b, upgrades, studio, level, city));
        if (bucket != 0) return bucket;

        long costA = upgrades?.GetNextCost(a) ?? a.baseCost;
        long costB = upgrades?.GetNextCost(b) ?? b.baseCost;
        return costA.CompareTo(costB);
    }

    public static ContractBucket GetContractBucket(ContractConfig cfg, ContractSystem contracts)
    {
        if (cfg == null || contracts == null) return ContractBucket.Other;
        if (contracts.IsReadyToClaim(cfg)) return ContractBucket.Claimable;
        if (contracts.GetProgress(cfg) > 0f) return ContractBucket.InProgress;
        return ContractBucket.InProgress;
    }

    public static int CompareContracts(ContractConfig a, ContractConfig b, ContractSystem contracts)
    {
        int bucket = GetContractBucket(a, contracts).CompareTo(GetContractBucket(b, contracts));
        if (bucket != 0) return bucket;
        return string.Compare(a?.contractTitle, b?.contractTitle, StringComparison.Ordinal);
    }

    public static void SortContracts(List<ContractConfig> list, ContractSystem contracts)
    {
        if (list == null || list.Count < 2) return;
        list.Sort((a, b) => CompareContracts(a, b, contracts));
    }

    public static int CompareDepartments(DepartmentMiniCardUI a, DepartmentMiniCardUI b, CitySystem city)
    {
        bool lockedA = city != null && !city.IsDepartmentUnlocked(a.deptType);
        bool lockedB = city != null && !city.IsDepartmentUnlocked(b.deptType);
        if (lockedA != lockedB) return lockedA ? 1 : -1;
        return ((int)a.deptType).CompareTo((int)b.deptType);
    }
}
