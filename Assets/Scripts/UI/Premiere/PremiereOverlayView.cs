using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Premiere overlay — fixed layout, player-triggered discovery (Phase 10.3).</summary>
public class PremiereOverlayView : MonoBehaviour
{
    const string LayoutMarker = "PremiereLayout_v10_3";

    static readonly Color OverlayDim    = new Color(0f, 0f, 0f, 0.82f);
    static readonly Color CardBg        = CinematicTheme.CardBg;
    static readonly Color TextPrimary   = CinematicTheme.TextPrimary;
    static readonly Color TextSecondary = CinematicTheme.TextSecondary;
    static readonly Color MoneyGold     = CinematicTheme.GoldBase;
    static readonly Color RepGold       = CinematicTheme.GoldBright;
    static readonly Color PosterBg      = CinematicTheme.PanelBg;
    // Red Carpet accent — premiere headlines use cinematic red, not gold
    static readonly Color HeadlineColor = CinematicTheme.RedCarpetBright;

    CanvasGroup _overlayGroup;
    CanvasGroup _cardGroup;
    CanvasGroup _posterGroup;
    CanvasGroup _headlineGroup;
    CanvasGroup _titleGroup;
    CanvasGroup _rewardsGroup;
    CanvasGroup _extrasGroup;
    CanvasGroup _continueGroup;

    RectTransform _card;
    RectTransform _posterWrap;
    Image         _posterImage;
    Image         _rarityFrame;
    TextMeshProUGUI _headlineText;
    TextMeshProUGUI _titleText;
    TextMeshProUGUI _genreText;
    TextMeshProUGUI _rarityText;
    TextMeshProUGUI _moneyText;
    TextMeshProUGUI _repText;
    TextMeshProUGUI _xpText;
    TextMeshProUGUI _extrasText;
    Button        _continueButton;
    Button        _backdropButton;

    Sequence _activeSequence;
    Action   _onClosed;
    bool     _canDismiss;

    public static PremiereOverlayView Create(Transform parent)
    {
        ProductionPremiereHooks.EnsureOnCanvas();
        var hooks = ProductionPremiereHooks.Instance;
        if (hooks == null) return null;

        var existing = hooks.transform.Find("PremiereOverlay");
        if (existing != null)
        {
            if (existing.Find(LayoutMarker) != null)
                return existing.GetComponent<PremiereOverlayView>();
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }

        var go = new GameObject("PremiereOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(PremiereOverlayView));
        go.transform.SetParent(hooks.transform, false);
        Stretch(go.GetComponent<RectTransform>());

        var canvas = go.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 9500;
        go.AddComponent<GraphicRaycaster>();

        var view = go.GetComponent<PremiereOverlayView>();
        view.Build();
        go.SetActive(false);
        return view;
    }

    void Build()
    {
        _overlayGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        var marker = new GameObject(LayoutMarker, typeof(RectTransform));
        marker.transform.SetParent(transform, false);

        var dim = CreatePanel(transform, "Backdrop", OverlayDim);
        Stretch(dim);
        _backdropButton = dim.gameObject.AddComponent<Button>();
        _backdropButton.transition = Selectable.Transition.None;
        _backdropButton.onClick.AddListener(TryDismiss);

        _card = CreatePanel(transform, "Card", CardBg);
        var cardRT = _card;
        cardRT.anchorMin = cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(400f, 620f);
        cardRT.anchoredPosition = Vector2.zero;
        HudSkinProvider.ApplyPanel(_card.GetComponent<Image>(), HudPanelVariant.Card);
        CinematicTheme.ApplyElevationPopup(_card);
        _cardGroup = _card.gameObject.AddComponent<CanvasGroup>();

        _headlineText = CreateLabel(_card, "Headline", Loc.Get(LocKeys.PremiereHeadline), 22f, HeadlineColor,
            FontStyles.Bold, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(340f, 28f));
        _headlineGroup = _headlineText.gameObject.AddComponent<CanvasGroup>();

        _posterWrap = CreatePanel(_card, "PosterWrap", PosterBg);
        var posterRT = _posterWrap;
        posterRT.anchorMin = posterRT.anchorMax = new Vector2(0.5f, 1f);
        posterRT.pivot = new Vector2(0.5f, 1f);
        posterRT.anchoredPosition = new Vector2(0f, -68f);
        posterRT.sizeDelta = new Vector2(320f, 240f);
        _posterGroup = _posterWrap.gameObject.AddComponent<CanvasGroup>();
        _rarityFrame = CreatePanel(_posterWrap, "RarityFrame", Color.clear).GetComponent<Image>();
        Stretch(_rarityFrame.rectTransform);
        _posterImage = CreatePanel(_posterWrap, "Poster", Color.clear).GetComponent<Image>();
        Inset(_posterImage.rectTransform, 0.05f);

        _titleText = CreateLabel(_card, "Title", string.Empty, 20f, TextPrimary,
            FontStyles.Bold, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -322f), new Vector2(340f, 30f));
        _titleText.enableAutoSizing = true;
        _titleText.fontSizeMin = 16f;
        _titleText.fontSizeMax = 22f;
        _titleGroup = _titleText.gameObject.AddComponent<CanvasGroup>();

        _genreText = CreateLabel(_card, "Genre", string.Empty, 12f, TextSecondary,
            FontStyles.Normal, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(24f, -358f), new Vector2(160f, 18f));
        _genreText.alignment = TextAlignmentOptions.MidlineLeft;

        _rarityText = CreateLabel(_card, "Rarity", string.Empty, 12f, TextSecondary,
            FontStyles.Bold, new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -358f), new Vector2(160f, 18f));
        _rarityText.alignment = TextAlignmentOptions.MidlineRight;

        var rewardsRoot = CreatePanel(_card, "RewardsBlock", CinematicTheme.DeepBg);
        var rewardsRT = rewardsRoot;
        rewardsRT.anchorMin = rewardsRT.anchorMax = new Vector2(0.5f, 1f);
        rewardsRT.pivot = new Vector2(0.5f, 1f);
        rewardsRT.anchoredPosition = new Vector2(0f, -388f);
        rewardsRT.sizeDelta = new Vector2(340f, 118f);
        CinematicTheme.ApplyElevationPanel(rewardsRoot);
        _rewardsGroup = rewardsRoot.gameObject.AddComponent<CanvasGroup>();

        var rewardsTitle = CreateLabel(rewardsRoot, "RewardsTitle", Loc.Get(LocKeys.PremiereRewardsTitle), 12f, TextSecondary,
            FontStyles.Bold, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -8f), new Vector2(-24f, 16f));
        rewardsTitle.alignment = TextAlignmentOptions.MidlineLeft;

        _moneyText = CreateLabel(rewardsRoot, "Money", string.Empty, 17f, MoneyGold,
            FontStyles.Bold, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -30f), new Vector2(-24f, 24f));
        _moneyText.alignment = TextAlignmentOptions.MidlineLeft;

        _repText = CreateLabel(rewardsRoot, "Rep", string.Empty, 17f, RepGold,
            FontStyles.Bold, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -56f), new Vector2(-24f, 24f));
        _repText.alignment = TextAlignmentOptions.MidlineLeft;

        _xpText = CreateLabel(rewardsRoot, "Xp", string.Empty, 15f, new Color(0.65f, 0.85f, 1f),
            FontStyles.Bold, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -82f), new Vector2(-24f, 22f));
        _xpText.alignment = TextAlignmentOptions.MidlineLeft;

        _extrasText = CreateLabel(_card, "Extras", string.Empty, 11f, TextSecondary,
            FontStyles.Normal, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -518f), new Vector2(340f, 44f));
        _extrasText.textWrappingMode = TextWrappingModes.Normal;
        _extrasGroup = _extrasText.gameObject.AddComponent<CanvasGroup>();

        var btnGo = new GameObject("ContinueBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
        btnGo.transform.SetParent(_card, false);
        var btnRT = btnGo.GetComponent<RectTransform>();
        btnRT.anchorMin = btnRT.anchorMax = new Vector2(0.5f, 0f);
        btnRT.pivot = new Vector2(0.5f, 0f);
        btnRT.anchoredPosition = new Vector2(0f, 24f);
        btnRT.sizeDelta = new Vector2(300f, 52f);
        HudSkinProvider.ApplyButton(btnGo.GetComponent<Image>(), HudButtonVariant.Success);
        btnGo.AddComponent<UIButtonScale>();
        _continueGroup = btnGo.GetComponent<CanvasGroup>();
        _continueButton = btnGo.GetComponent<Button>();
        _continueButton.onClick.AddListener(TryDismiss);
        var btnLbl = CreateLabel(btnGo.transform, "Label", Loc.Get(LocKeys.PremiereContinue), 16f, TextPrimary,
            FontStyles.Bold, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        btnLbl.enableAutoSizing = true;
    }

    public void Play(PremierePresentationData data, Action onClosed)
    {
        _onClosed = onClosed;
        _canDismiss = false;
        _continueButton.interactable = false;
        _backdropButton.interactable = false;

        BindData(data);
        ResetVisualState(data);
        gameObject.SetActive(true);

        _activeSequence?.Kill();
        _activeSequence = DOTween.Sequence().SetUpdate(true);
        _activeSequence.Append(PremiereAnimations.OverlayFadeIn(_overlayGroup));
        _activeSequence.Join(PremiereAnimations.CardScaleIn(_card, _cardGroup));
        _activeSequence.Append(PremiereAnimations.RevealGroup(_headlineGroup, _headlineText.rectTransform, 0.04f));
        _activeSequence.Append(PremiereAnimations.PosterReveal(_posterWrap, _posterGroup, 0.04f));
        _activeSequence.Append(PremiereAnimations.RevealGroup(_titleGroup, _titleText.rectTransform, 0.03f));
        _activeSequence.Append(PremiereAnimations.RevealGroup(_rewardsGroup, _posterWrap, 0.05f));

        if (!string.IsNullOrEmpty(_extrasText.text))
            _activeSequence.Append(PremiereAnimations.RevealGroup(_extrasGroup, _extrasText.rectTransform, 0.05f));

        _activeSequence.Append(PremiereAnimations.RevealGroup(_continueGroup, _continueButton.transform as RectTransform, 0.05f));
        _activeSequence.AppendCallback(() =>
        {
            _canDismiss = true;
            _continueButton.interactable = true;
            _backdropButton.interactable = true;
        });
    }

    void BindData(PremierePresentationData data)
    {
        _titleText.text = data.movieName;
        _genreText.text = GenreLoc.GetLabel(data.genre);
        MovieRarityVisual.ApplyBadge(_rarityText, data.rarity);
        MovieRarityVisual.ApplyFrame(_rarityFrame, data.rarity);
        MoviePosterVisual.Apply(_posterImage, data.posterSprite, data.posterColorHex);
        _headlineText.text = Loc.Get(LocKeys.PremiereHeadline);
        _moneyText.text = PremiereLoc.FormatMoneyReward(data.moneyReward);
        _repText.text = PremiereLoc.FormatRepReward(data.repGain);
        _xpText.text = data.xpGain > 0.01f ? $"+{data.xpGain:0} XP" : string.Empty;
        _xpText.gameObject.SetActive(data.xpGain > 0.01f);
        _extrasText.text = BuildExtrasLines(data);
        _extrasText.gameObject.SetActive(!string.IsNullOrEmpty(_extrasText.text));
    }

    void ResetVisualState(PremierePresentationData data)
    {
        _overlayGroup.alpha = 0f;
        _cardGroup.alpha = 0f;
        _posterGroup.alpha = 0f;
        _headlineGroup.alpha = 0f;
        _titleGroup.alpha = 0f;
        _rewardsGroup.alpha = 0f;
        _extrasGroup.alpha = 0f;
        _continueGroup.alpha = 0f;
    }

    static string BuildExtrasLines(PremierePresentationData data)
    {
        var lines = new System.Collections.Generic.List<string>();

        if (data.varietyBonusPercent > 0.01f)
            lines.Add($"Variedad +{data.varietyBonusPercent:0}%");
        if (data.isFirstDiscovery)
            lines.Add("Película descubierta");

        var contracts = GameHub.Instance?.contracts;
        if (contracts != null && contracts.HasActiveContract && contracts.ActiveContracts.Count > 0)
        {
            var cfg = contracts.ActiveContracts[0];
            float progress = contracts.GetProgress(cfg);
            if (progress >= cfg.goalAmount - 0.01f)
                lines.Add("Contrato completado");
            else
                lines.Add($"Contrato · {progress:0}/{cfg.goalAmount:0}");
        }

        return lines.Count > 0 ? string.Join("\n", lines) : string.Empty;
    }

    void TryDismiss()
    {
        if (!_canDismiss) return;
        _canDismiss = false;
        _continueButton.interactable = false;
        _backdropButton.interactable = false;

        _activeSequence?.Kill();
        UIAnimationService.PlayPremiereOverlayOut(_overlayGroup, _cardGroup, () =>
        {
            gameObject.SetActive(false);
            var cb = _onClosed;
            _onClosed = null;
            cb?.Invoke();
        });
    }

    static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, float size, Color color,
        FontStyles style, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var tmp = RuntimeTmpText.Create(parent, text, size, color, style, TextAlignmentOptions.Center, name);
        tmp.raycastTarget = false;
        var rt = tmp.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        return tmp;
    }

    static RectTransform CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        go.GetComponent<Image>().raycastTarget = false;
        return go.GetComponent<RectTransform>();
    }

    static void Inset(RectTransform rt, float inset)
    {
        rt.anchorMin = new Vector2(inset, inset);
        rt.anchorMax = new Vector2(1f - inset, 1f - inset);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    void OnDestroy()
    {
        _activeSequence?.Kill();
    }
}
