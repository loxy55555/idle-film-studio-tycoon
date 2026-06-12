using DG.Tweening;
using UnityEngine;

/// <summary>DOTween helpers for premiere presentation (Phase 8.5).</summary>
public static class PremiereAnimations
{
    public static Sequence OverlayFadeIn(CanvasGroup group, float duration = 0.22f)
    {
        group.alpha = 0f;
        return DOTween.Sequence()
            .Append(group.DOFade(1f, duration).SetEase(Ease.OutQuad))
            .SetUpdate(true);
    }

    public static Sequence OverlayFadeOut(CanvasGroup group, float duration = 0.2f)
    {
        return DOTween.Sequence()
            .Append(group.DOFade(0f, duration).SetEase(Ease.InQuad))
            .SetUpdate(true);
    }

    public static Sequence CardScaleIn(RectTransform card, CanvasGroup cardGroup, float duration = 0.32f)
    {
        card.localScale = Vector3.one * 0.88f;
        if (cardGroup != null) cardGroup.alpha = 0f;

        var seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(card.DOScale(1f, duration).SetEase(Ease.OutBack));
        if (cardGroup != null)
            seq.Join(cardGroup.DOFade(1f, duration * 0.85f));
        return seq;
    }

    public static Sequence PosterReveal(RectTransform poster, CanvasGroup posterGroup, float delay = 0f)
    {
        poster.localScale = Vector3.one * 0.82f;
        if (posterGroup != null) posterGroup.alpha = 0f;

        var seq = DOTween.Sequence().SetUpdate(true);
        if (delay > 0f) seq.AppendInterval(delay);
        seq.Append(poster.DOScale(1f, 0.28f).SetEase(Ease.OutBack));
        if (posterGroup != null)
            seq.Join(posterGroup.DOFade(1f, 0.22f));
        return seq;
    }

    public static Sequence RevealGroup(CanvasGroup group, RectTransform rt, float delay, float duration = 0.22f)
    {
        if (group != null) group.alpha = 0f;
        if (rt != null) rt.localScale = Vector3.one * 0.94f;

        var seq = DOTween.Sequence().SetUpdate(true);
        if (delay > 0f) seq.AppendInterval(delay);
        if (group != null)
            seq.Append(group.DOFade(1f, duration).SetEase(Ease.OutQuad));
        if (rt != null)
            seq.Join(rt.DOScale(1f, duration).SetEase(Ease.OutBack));
        return seq;
    }

    public static Sequence RewardPop(RectTransform rewardLine, CanvasGroup group, float delay)
    {
        rewardLine.localScale = Vector3.zero;
        if (group != null) group.alpha = 0f;

        var seq = DOTween.Sequence().SetUpdate(true);
        seq.AppendInterval(delay);
        seq.Append(rewardLine.DOScale(1f, 0.26f).SetEase(Ease.OutBack));
        if (group != null)
            seq.Join(group.DOFade(1f, 0.18f));
        return seq;
    }
}
