using System;
using UnityEngine;

/// <summary>Paid refresh flows for production offers and contract candidates (Phase 8.0).</summary>
public static class GameplayRefreshService
{
    public const int DiamondRefreshCost = 5;
    public const string ProductionOffersPlacement = "production_offers_refresh";
    public const string ContractCandidatesPlacement = "contract_candidates_refresh";

    static IAdsRewardProvider _ads;

    public static void RegisterAdsProvider(IAdsRewardProvider provider) => _ads = provider;

    public static void RequestProductionOfferRefresh(Action onSuccess)
    {
        RefreshChoiceDialogUI.Show(
            Loc.Get(LocKeys.ProdNewProduction),
            DiamondRefreshCost,
            placementId: ProductionOffersPlacement,
            onAd: ok =>
            {
                if (!ok) return;
                Debug.Log("[Offers] RefreshConsumed");
                CompleteProductionRefresh(onSuccess);
            },
            onDiamonds: () => { Debug.Log("[Offers] RefreshConsumed"); CompleteProductionRefresh(onSuccess); });
    }

    public static void RequestContractCandidateRefresh(Action onSuccess)
    {
        var contracts = GameHub.Instance?.contracts;
        if (contracts == null || contracts.HasActiveContract)
            return;

        RefreshChoiceDialogUI.Show(
            Loc.Get(LocKeys.ContractRefresh),
            DiamondRefreshCost,
            placementId: ContractCandidatesPlacement,
            onAd: ok =>
            {
                if (!ok) return;
                CompleteContractRefresh(onSuccess);
            },
            onDiamonds: () => CompleteContractRefresh(onSuccess));
    }

    static void CompleteProductionRefresh(Action onSuccess)
    {
        Debug.Log("[Offers] RefreshConfirmed");
        var hub = GameHub.Instance;
        if (hub?.studio == null) return;

        MovieOfferState.ForceRegenerateAll(
            MovieCatalogRuntime.AllMovies,
            hub.studioLevel?.Level ?? 1,
            hub.studio.reputation,
            hub.studio.CompletedMovieKeys,
            hub.studio.GetActiveProductionConfigs());

        onSuccess?.Invoke();
        hub.save?.Save("RefreshOffers");
    }

    static void CompleteContractRefresh(Action onSuccess)
    {
        var hub = GameHub.Instance;
        if (hub?.contracts == null) return;

        hub.contracts.ForceRefreshCandidates(hub.studioLevel?.Level ?? 1);
        onSuccess?.Invoke();
        hub.save?.Save("RefreshContracts");
    }

    public static bool TrySpendDiamonds(int amount)
    {
        var hub = GameHub.Instance;
        var wallet = hub?.diamonds;
        if (wallet == null || !wallet.TrySpend(amount)) return false;
        hub.save?.Save("DiamondSpend");
        return true;
    }

    public static void TryShowAd(string placementId, Action<bool> onComplete)
    {
        if (_ads != null)
        {
            _ads.ShowRewardedAd(placementId, onComplete);
            return;
        }

        Debug.LogWarning($"[GameplayRefresh] No ads provider — cannot show ad for '{placementId}'.");
        AdRewardUI.ShowMessage(Loc.Get(LocKeys.AdUnavailable));
        onComplete?.Invoke(false);
    }
}
