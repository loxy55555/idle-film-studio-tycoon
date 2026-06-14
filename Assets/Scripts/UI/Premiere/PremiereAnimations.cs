using DG.Tweening;
using UnityEngine;

/// <summary>Premiere helpers — delegates to UIAnimationService (Phase 11).</summary>
public static class PremiereAnimations
{
    public static Sequence OverlayFadeIn(CanvasGroup group, float duration = 0.22f)
    {
        if (group == null) return null;
        group.alpha = 0f;
        return DOTween.Sequence().SetUpdate(true)
            .Append(group.DOFade(1f, duration).SetEase(Ease.OutQuad));
    }

    public static Sequence OverlayFadeOut(CanvasGroup group, float duration = 0.2f) =>
        UIAnimationService.PlayPremiereOverlayOut(group, null);

    public static Sequence CardScaleIn(RectTransform card, CanvasGroup cardGroup, float duration = 0.32f) =>
        UIAnimationService.PlayPremiereOverlayIn(null, card, cardGroup);

    public static Sequence PosterReveal(RectTransform poster, CanvasGroup posterGroup, float delay = 0f) =>
        UIAnimationService.PlayPremierePosterIn(poster, posterGroup, delay);

    public static Sequence RevealGroup(CanvasGroup group, RectTransform rt, float delay, float duration = 0.22f) =>
        UIAnimationService.PlayPremiereReveal(group, rt, delay);

    public static Sequence RewardPop(RectTransform rewardLine, CanvasGroup group, float delay) =>
        UIAnimationService.PlayPremiereRewardPop(rewardLine, group, delay);
}
