using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Central reusable UI animation API — Phase 11 Premium Feel Foundation.</summary>
public static class UIAnimationService
{
    public const float ButtonPressScale   = 0.92f;
    public const float ButtonPressDuration = 0.08f;
    public const float TabSelectDuration  = 0.18f;
    public const float PanelFadeDuration  = 0.22f;
    public const float PopupScaleDuration = 0.28f;
    public const float BarDuration        = 0.28f;
    public const float CurrencyDuration   = 0.35f;

    static Tween T(Tween t) => t.SetUpdate(true);
    static Sequence S(Sequence s) => s.SetUpdate(true);

    // ── Level 1 — Micro-interactions ─────────────────────────────────────────

    public static Tween PlayButtonPress(RectTransform rt, Vector3 baseScale)
    {
        if (rt == null) return null;
        rt.DOKill(false);
        return T(rt.DOScale(baseScale * ButtonPressScale, ButtonPressDuration).SetEase(Ease.OutQuad));
    }

    public static Tween PlayButtonRelease(RectTransform rt, Vector3 baseScale)
    {
        if (rt == null) return null;
        rt.DOKill(false);
        return T(rt.DOScale(baseScale, ButtonPressDuration).SetEase(Ease.OutQuad));
    }

    public static Sequence PlayTabSelect(RectTransform tabRt, TextMeshProUGUI label, bool selected)
    {
        if (tabRt == null) return null;
        tabRt.DOKill(false);
        var seq = S(DOTween.Sequence());
        float target = selected ? 1.04f : 1f;
        seq.Append(tabRt.DOScale(target, TabSelectDuration).SetEase(selected ? Ease.OutBack : Ease.OutQuad));
        if (label != null)
        {
            Color targetColor = selected ? CinematicTheme.GoldBright : CinematicTheme.TextDim;
            seq.Join(T(label.DOColor(targetColor, TabSelectDuration)));
        }
        return seq;
    }

    public static Sequence PlayPopupOpen(RectTransform rt, CanvasGroup group, float enterScale = 1f)
    {
        if (rt == null) return null;
        rt.DOKill(false);
        group?.DOKill(false);
        rt.localScale = Vector3.one * 0.85f;
        if (group != null) group.alpha = 0f;

        var seq = S(DOTween.Sequence());
        if (group != null) seq.Append(group.DOFade(1f, PanelFadeDuration));
        seq.Join(rt.DOScale(enterScale, PopupScaleDuration).SetEase(Ease.OutBack));
        return seq;
    }

    public static Sequence PlayPopupClose(RectTransform rt, CanvasGroup group, Action onComplete = null)
    {
        if (rt == null)
        {
            onComplete?.Invoke();
            return null;
        }

        var seq = S(DOTween.Sequence());
        if (group != null) seq.Append(group.DOFade(0f, PanelFadeDuration));
        seq.Join(rt.DOScale(0.92f, PanelFadeDuration).SetEase(Ease.InQuad));
        if (onComplete != null) seq.OnComplete(() => onComplete());
        return seq;
    }

    public static Sequence PlayPanelOpen(RectTransform panel, CanvasGroup group, float slideY = 24f)
    {
        if (panel == null) return null;
        panel.DOKill(false);
        group?.DOKill(false);

        // Always start from absolute offset below zero and animate to zero — prevents
        // cumulative drift caused by PlayPanelClose leaving the panel below its rest position.
        panel.anchoredPosition = new Vector2(0f, -slideY);
        if (group != null) group.alpha = 0f;

        var seq = S(DOTween.Sequence());
        if (group != null) seq.Append(group.DOFade(1f, PanelFadeDuration));
        seq.Join(panel.DOAnchorPos(Vector2.zero, PanelFadeDuration).SetEase(Ease.OutCubic));
        return seq;
    }

    public static Sequence PlayPanelClose(RectTransform panel, CanvasGroup group, float slideY = 16f)
    {
        if (panel == null) return null;
        var seq = S(DOTween.Sequence());
        if (group != null) seq.Append(group.DOFade(0f, PanelFadeDuration * 0.85f));
        seq.Join(panel.DOAnchorPos(panel.anchoredPosition + new Vector2(0f, -slideY), PanelFadeDuration * 0.85f)
            .SetEase(Ease.InQuad));
        return seq;
    }

    // ── Level 2 — Progression feedback ───────────────────────────────────────

    public static Tween AnimateCurrency(TextMeshProUGUI text, double from, double to,
        float duration = CurrencyDuration, Func<long, string> formatter = null, Action<double> onChanged = null)
    {
        if (text == null) return null;
        formatter ??= AnimatedMoneyText.FormatMoney;
        text.DOKill(false);

        double current = from;
        return T(DOTween.To(() => current, v =>
        {
            current = v;
            onChanged?.Invoke(v);
            text.text = formatter((long)v);
        }, to, duration).SetEase(Ease.OutCubic));
    }

    public static Tween AnimateFloatText(TextMeshProUGUI text, float from, float to,
        float duration = CurrencyDuration, Func<float, string> formatter = null, Action<float> onChanged = null)
    {
        if (text == null) return null;
        formatter ??= v => v.ToString("0.#");
        text.DOKill(false);

        float current = from;
        return T(DOTween.To(() => current, v =>
        {
            current = v;
            onChanged?.Invoke(v);
            text.text = formatter(v);
        }, to, duration).SetEase(Ease.OutCubic));
    }

    public static Tween AnimateProgressBar(Slider slider, float value, float max, float duration = BarDuration)
    {
        if (slider == null) return null;
        slider.DOKill(false);
        slider.maxValue = max;
        return T(slider.DOValue(value, duration).SetEase(Ease.OutCubic));
    }

    public static Tween AnimateProgressBarNormalized(Slider slider, float normalized, float duration = BarDuration)
    {
        if (slider == null) return null;
        slider.DOKill(false);
        return T(slider.DOValue(Mathf.Clamp01(normalized), duration).SetEase(Ease.OutCubic));
    }

    // ── Level 3 — Upgrades ───────────────────────────────────────────────────

    public static Sequence PlayUpgradeFeedback(RectTransform cardRoot, Image cardBg, TextMeshProUGUI levelLabel = null)
    {
        if (cardRoot == null) return null;

        foreach (var btnScale in cardRoot.GetComponentsInChildren<UIButtonScale>(true))
            btnScale.ResetScale();

        cardRoot.DOKill(false);
        cardRoot.localScale = Vector3.one;

        var seq = S(DOTween.Sequence());
        seq.Append(cardRoot.DOPunchScale(Vector3.one * 0.12f, 0.35f, 8, 0.5f));
        seq.OnComplete(() =>
        {
            if (cardRoot != null)
            {
                cardRoot.localScale = Vector3.one;
                foreach (var btnScale in cardRoot.GetComponentsInChildren<UIButtonScale>(true))
                    btnScale.ResetScale();
            }
        });
        seq.OnKill(() =>
        {
            if (cardRoot != null)
            {
                cardRoot.localScale = Vector3.one;
                foreach (var btnScale in cardRoot.GetComponentsInChildren<UIButtonScale>(true))
                    btnScale.ResetScale();
            }
        });

        if (cardBg != null)
        {
            Color baseColor = cardBg.color;
            cardBg.DOKill(false);
            seq.Join(cardBg.DOColor(Color.Lerp(baseColor, new Color(1f, 1f, 1f, 0.35f), 0.85f), 0.08f)
                .SetLoops(2, LoopType.Yoyo)
                .OnComplete(() => cardBg.color = baseColor));
        }

        if (levelLabel != null)
        {
            levelLabel.DOKill(false);
            var rt = levelLabel.rectTransform;
            seq.Join(rt.DOPunchScale(Vector3.one * 0.18f, 0.32f, 6, 0.45f));
        }

        return seq;
    }

    // ── Level 4 — Contracts ──────────────────────────────────────────────────

    public static Sequence PlayContractRewardReveal(RectTransform rewardLine, CanvasGroup group = null)
    {
        if (rewardLine == null) return null;
        rewardLine.localScale = Vector3.zero;
        if (group != null) group.alpha = 0f;

        var seq = S(DOTween.Sequence());
        seq.Append(rewardLine.DOScale(1f, 0.26f).SetEase(Ease.OutBack));
        if (group != null) seq.Join(group.DOFade(1f, 0.18f));
        return seq;
    }

    public static Tween PlayContractCompleteHighlight(RectTransform cardRoot)
    {
        if (cardRoot == null) return null;
        cardRoot.DOKill(false);
        return T(cardRoot.DOPunchScale(Vector3.one * 0.06f, 0.45f, 6, 0.35f));
    }

    // ── Level 5 — Discover / Premiere ────────────────────────────────────────

    public static Tween PlayDiscoverPulse(RectTransform button)
    {
        if (button == null) return null;
        button.DOKill(false);
        button.localScale = Vector3.one;
        return T(button.DOScale(1.05f, 0.85f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo));
    }

    public static void StopDiscoverPulse(RectTransform button)
    {
        if (button == null) return;
        button.DOKill(false);
        button.localScale = Vector3.one;
    }

    public static Sequence PlayPremiereOverlayIn(CanvasGroup overlay, RectTransform card, CanvasGroup cardGroup)
    {
        var seq = S(DOTween.Sequence());
        if (overlay != null)
        {
            overlay.alpha = 0f;
            seq.Append(overlay.DOFade(1f, PanelFadeDuration));
        }
        if (card != null)
        {
            card.localScale = Vector3.one * 0.88f;
            if (cardGroup != null) cardGroup.alpha = 0f;
            seq.Join(card.DOScale(1f, PopupScaleDuration).SetEase(Ease.OutBack));
            if (cardGroup != null) seq.Join(cardGroup.DOFade(1f, PopupScaleDuration * 0.85f));
        }
        return seq;
    }

    public static Sequence PlayPremierePosterIn(RectTransform poster, CanvasGroup posterGroup, float delay = 0f)
    {
        if (poster == null) return null;
        poster.localScale = Vector3.one * 0.82f;
        if (posterGroup != null) posterGroup.alpha = 0f;

        var seq = S(DOTween.Sequence());
        if (delay > 0f) seq.AppendInterval(delay);
        seq.Append(poster.DOScale(1f, 0.28f).SetEase(Ease.OutBack));
        if (posterGroup != null) seq.Join(posterGroup.DOFade(1f, 0.22f));
        return seq;
    }

    public static Sequence PlayPremiereReveal(CanvasGroup group, RectTransform rt, float delay = 0f)
    {
        if (group != null) group.alpha = 0f;
        if (rt != null) rt.localScale = Vector3.one * 0.94f;

        var seq = S(DOTween.Sequence());
        if (delay > 0f) seq.AppendInterval(delay);
        if (group != null) seq.Append(group.DOFade(1f, 0.22f).SetEase(Ease.OutQuad));
        if (rt != null) seq.Join(rt.DOScale(1f, 0.22f).SetEase(Ease.OutBack));
        return seq;
    }

    public static Sequence PlayPremiereRewardPop(RectTransform line, CanvasGroup group, float delay = 0f)
    {
        if (line == null) return null;
        line.localScale = Vector3.zero;
        if (group != null) group.alpha = 0f;

        var seq = S(DOTween.Sequence());
        if (delay > 0f) seq.AppendInterval(delay);
        seq.Append(line.DOScale(1f, 0.26f).SetEase(Ease.OutBack));
        if (group != null) seq.Join(group.DOFade(1f, 0.18f));
        return seq;
    }

    public static Sequence PlayPremiereOverlayOut(CanvasGroup overlay, CanvasGroup cardGroup, Action onComplete = null)
    {
        var seq = S(DOTween.Sequence());
        seq.Append(overlay.DOFade(0f, 0.2f).SetEase(Ease.InQuad));
        if (cardGroup != null) seq.Join(cardGroup.DOFade(0f, 0.18f));
        if (onComplete != null) seq.OnComplete(() => onComplete());
        return seq;
    }

    // ── Level 6 — Golden stars ─────────────────────────────────────────────────

    public static Sequence PlayStarEarned(RectTransform starRt, Image glow = null, TextMeshProUGUI label = null)
    {
        if (starRt == null) return null;
        starRt.DOKill(false);
        glow?.DOKill(false);
        label?.DOKill(false);

        starRt.localScale = Vector3.zero;
        var seq = S(DOTween.Sequence());
        seq.Append(starRt.DOScale(1.35f, 0.34f).SetEase(Ease.OutBack));
        seq.Append(starRt.DOScale(1f, 0.18f).SetEase(Ease.OutQuad));
        seq.Join(starRt.DOPunchScale(Vector3.one * 0.22f, 0.55f, 10, 0.45f));

        if (glow != null)
        {
            glow.color = new Color(0.97f, 0.82f, 0.18f, 0f);
            seq.Join(glow.DOFade(0.55f, 0.22f));
            seq.Append(glow.DOFade(0.18f, 0.35f));
        }

        if (label != null)
        {
            var c = label.color;
            c.a = 0f;
            label.color = c;
            seq.Join(label.DOFade(1f, 0.25f));
        }

        return seq;
    }

    // ── Reward popup (GameFeelUI) ──────────────────────────────────────────────

    public static Sequence PlayRewardPopup(RectTransform rt, CanvasGroup group, bool prominent, Action onReady = null)
    {
        float enterScale = prominent ? 1.08f : 1f;
        var seq = PlayPopupOpen(rt, group, enterScale);
        if (prominent && rt != null)
            seq.Append(rt.DOPunchScale(Vector3.one * 0.06f, 0.45f, 6, 0.4f));
        if (onReady != null) seq.AppendCallback(() => onReady());
        return seq;
    }

    public static Sequence PlayRewardPopupOut(RectTransform rt, CanvasGroup group, float exitScale, Action onComplete)
    {
        if (rt == null || group == null)
        {
            onComplete?.Invoke();
            return null;
        }

        var seq = S(DOTween.Sequence());
        seq.Append(group.DOFade(0f, 0.28f));
        seq.Join(rt.DOScale(exitScale * 0.92f, 0.28f).SetEase(Ease.InQuad));
        seq.OnComplete(() => onComplete?.Invoke());
        return seq;
    }
}
