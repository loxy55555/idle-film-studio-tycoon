using System;

/// <summary>Hook for rewarded ads — safe no-op when no implementation is registered.</summary>
public interface IAdsRewardProvider
{
    void ShowRewardedAd(string placementId, Action<bool> onComplete);
}
