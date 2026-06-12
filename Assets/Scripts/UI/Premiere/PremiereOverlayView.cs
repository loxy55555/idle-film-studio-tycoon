using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Premiere Night overlay — poster, rewards, discovery (Phase 8.5).</summary>
public class PremiereOverlayView : MonoBehaviour
{
    static readonly Color OverlayDim    = new Color(0f, 0f, 0f, 0.78f);
    static readonly Color CardBg        = new Color(0.08f, 0.08f, 0.16f, 0.98f);
    static readonly Color TextPrimary   = Color.white;
    static readonly Color TextSecondary = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color MoneyGreen    = new Color(0.18f, 0.80f, 0.44f);
    static readonly Color RepGold       = new Color(0.95f, 0.77f, 0.06f);
    static readonly Color BarBg         = new Color(0.09f, 0.09f, 0.18f);

    CanvasGroup _overlayGroup;
    CanvasGroup _cardGroup;
    CanvasGroup _posterGroup;
    CanvasGroup _headlineGroup;
    CanvasGroup _moneyGroup;
    CanvasGroup _repGroup;
    CanvasGroup _discoveryGroup;
    CanvasGroup _continueGroup;

    RectTransform _card;
    RectTransform _posterWrap;
    RectTransform _moneyLine;
    RectTransform _repLine;
    RectTransform _discoverySection;
    Image         _posterImage;
    Image         _discoveryPoster;
    Image         _rarityFrame;
    TextMeshProUGUI _titleText;
    TextMeshProUGUI _genreText;
    TextMeshProUGUI _rarityText;
    TextMeshProUGUI _headlineText;
    TextMeshProUGUI _moneyText;
    TextMeshProUGUI _repText;
    TextMeshProUGUI _discoveryTitleText;
    TextMeshProUGUI _discoveryNameText;
    Button        _continueButton;
    Button        _backdropButton;

    RectTransform _premiereSection;
    RectTransform _rewardsSection;
    RectTransform _futureRewardsHook;

    Sequence _activeSequence;
    Action   _onClosed;
    bool     _canDismiss;

    public static PremiereOverlayView Create(Transform parent)
    {
        ProductionPremiereHooks.EnsureOnCanvas();
        var hooks = ProductionPremiereHooks.Instance;
        if (hooks == null) return null;

        var existing = hooks.transform.Find("PremiereOverlay");
        if (existing != null) return existing.GetComponent<PremiereOverlayView>();

        var go = new GameObject("PremiereOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(PremiereOverlayView));
        go.transform.SetParent(hooks.transform, false);
        Stretch(go.GetComponent<RectTransform>());

        var canvas = go.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 9500;
        go.AddComponent<GraphicRaycaster>();

        var view = go.GetComponent<PremiereOverlayView>();
        view.Build(hooks);
        go.SetActive(false);
        return view;
    }

    void Build(ProductionPremiereHooks hooks)
    {
        _overlayGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        var dim = CreatePanel(transform, "Backdrop", OverlayDim);
        Stretch(dim);
        _backdropButton = dim.gameObject.AddComponent<Button>();
        _backdropButton.transition = Selectable.Transition.None;
        _backdropButton.onClick.AddListener(TryDismiss);

        _card = CreatePanel(transform, "Card", CardBg);
        var cardRT = _card;
        cardRT.anchorMin = cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(340f, 0f);
        cardRT.anchoredPosition = Vector2.zero;
        HudSkinProvider.ApplyPanel(_card.GetComponent<Image>(), HudPanelVariant.Card);
        _cardGroup = _card.gameObject.AddComponent<CanvasGroup>();
        _card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var vlg = _card.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.spacing = 10;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        _premiereSection = CreateSection(_card, "PremiereSection", hooks.premiereRoot);
        _posterWrap = CreatePanel(_premiereSection, "PosterWrap", BarBg);
        LE(_posterWrap, 140f);
        _posterGroup = _posterWrap.gameObject.AddComponent<CanvasGroup>();
        _rarityFrame = CreatePanel(_posterWrap, "RarityFrame", Color.clear).GetComponent<Image>();
        Stretch(_rarityFrame.rectTransform);
        _posterImage = CreatePanel(_posterWrap, "Poster", Color.clear).GetComponent<Image>();
        var posterRT = _posterImage.rectTransform;
        posterRT.anchorMin = new Vector2(0.05f, 0.05f);
        posterRT.anchorMax = new Vector2(0.95f, 0.95f);
        posterRT.offsetMin = posterRT.offsetMax = Vector2.zero;

        _titleText = CreateLabel(_premiereSection, "Title", string.Empty, 18f, TextPrimary, FontStyles.Bold, 24f);
        _titleText.enableAutoSizing = true;
        _titleText.fontSizeMin = 14f;
        _titleText.fontSizeMax = 20f;

        var metaRow = CreatePanel(_premiereSection, "MetaRow", Color.clear);
        LE(metaRow, 18f);
        var metaHLG = metaRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        metaHLG.spacing = 8;
        metaHLG.childAlignment = TextAnchor.MiddleCenter;
        metaHLG.childControlWidth = metaHLG.childControlHeight = true;
        metaHLG.childForceExpandWidth = metaHLG.childForceExpandHeight = true;
        _genreText = CreateLabel(metaRow, "Genre", string.Empty, 10f, TextSecondary, FontStyles.Normal, 16f);
        _genreText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        _rarityText = CreateLabel(metaRow, "Rarity", string.Empty, 10f, TextSecondary, FontStyles.Bold, 16f);
        _rarityText.gameObject.AddComponent<LayoutElement>().preferredWidth = 72f;

        _headlineText = CreateLabel(_premiereSection, "Headline", Loc.Get(LocKeys.PremiereCompleted), 15f, RepGold, FontStyles.Bold, 22f);
        _headlineGroup = _headlineText.gameObject.AddComponent<CanvasGroup>();

        _rewardsSection = CreateSection(_card, "RewardsSection", hooks.rewardsRoot);
        var rewardsTitle = CreateLabel(_rewardsSection, "RewardsTitle", Loc.Get(LocKeys.PremiereRewardsTitle), 12f, TextSecondary, FontStyles.Bold, 18f);

        _moneyLine = CreatePanel(_rewardsSection, "MoneyLine", Color.clear);
        LE(_moneyLine, 28f);
        _moneyGroup = _moneyLine.gameObject.AddComponent<CanvasGroup>();
        _moneyText = CreateLabel(_moneyLine, "Money", string.Empty, 16f, MoneyGreen, FontStyles.Bold, 24f);

        _repLine = CreatePanel(_rewardsSection, "RepLine", Color.clear);
        LE(_repLine, 28f);
        _repGroup = _repLine.gameObject.AddComponent<CanvasGroup>();
        _repText = CreateLabel(_repLine, "Rep", string.Empty, 16f, RepGold, FontStyles.Bold, 24f);

        _futureRewardsHook = CreatePanel(_rewardsSection, "FutureRewardsHook", Color.clear);
        LE(_futureRewardsHook, 0f);

        _discoverySection = CreateSection(_card, "DiscoverySection", hooks.discoveryRoot);
        _discoveryGroup = _discoverySection.gameObject.AddComponent<CanvasGroup>();
        _discoveryTitleText = CreateLabel(_discoverySection, "DiscoveryTitle", Loc.Get(LocKeys.PremiereDiscoveryTitle), 13f, TextPrimary, FontStyles.Bold, 20f);
        var discRow = CreatePanel(_discoverySection, "DiscRow", Color.clear);
        LE(discRow, 56f);
        var discHLG = discRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        discHLG.spacing = 10;
        discHLG.childAlignment = TextAnchor.MiddleLeft;
        discHLG.childControlWidth = discHLG.childControlHeight = true;
        discHLG.childForceExpandWidth = discHLG.childForceExpandHeight = true;
        var discPosterWrap = CreatePanel(discRow, "DiscPoster", BarBg);
        LE(discPosterWrap, 48f, 36f);
        _discoveryPoster = CreatePanel(discPosterWrap, "Poster", Color.clear).GetComponent<Image>();
        Stretch(_discoveryPoster.rectTransform);
        _discoveryNameText = CreateLabel(discRow, "DiscName", string.Empty, 14f, TextPrimary, FontStyles.Bold, 48f);
        _discoveryNameText.alignment = TextAlignmentOptions.MidlineLeft;
        _discoveryNameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var btnGo = new GameObject("ContinueBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
        btnGo.transform.SetParent(_card, false);
        HudSkinProvider.ApplyButton(btnGo.GetComponent<Image>(), HudButtonVariant.Success);
        btnGo.AddComponent<UIButtonScale>();
        LE(btnGo.GetComponent<RectTransform>(), 40f);
        _continueGroup = btnGo.GetComponent<CanvasGroup>();
        _continueButton = btnGo.GetComponent<Button>();
        _continueButton.onClick.AddListener(TryDismiss);
        var btnLbl = CreateLabel(btnGo.transform, "Label", Loc.Get(LocKeys.PremiereContinue), 13f, TextPrimary, FontStyles.Bold, 32f);
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
        _activeSequence.Append(PremiereAnimations.PosterReveal(_posterWrap, _posterGroup, 0.05f));
        _activeSequence.Append(PremiereAnimations.RevealGroup(_headlineGroup, _headlineText.rectTransform, 0.05f));
        _activeSequence.Append(PremiereAnimations.RewardPop(_moneyLine, _moneyGroup, 0.05f));
        _activeSequence.Append(PremiereAnimations.RewardPop(_repLine, _repGroup, 0.08f));

        if (data.isFirstDiscovery)
        {
            _discoverySection.gameObject.SetActive(true);
            _activeSequence.Append(PremiereAnimations.RevealGroup(_discoveryGroup, _discoverySection, 0.1f));
        }

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
        MoviePosterVisual.Apply(_discoveryPoster, data.posterSprite, data.posterColorHex);
        _headlineText.text = Loc.Get(LocKeys.PremiereCompleted);
        _moneyText.text = PremiereLoc.FormatMoneyReward(data.moneyReward);
        _repText.text = PremiereLoc.FormatRepReward(data.repGain);
        _discoveryTitleText.text = Loc.Get(LocKeys.PremiereDiscoveryTitle);
        _discoveryNameText.text = data.movieName;
    }

    void ResetVisualState(PremierePresentationData data)
    {
        _overlayGroup.alpha = 0f;
        _cardGroup.alpha = 0f;
        _posterGroup.alpha = 0f;
        _headlineGroup.alpha = 0f;
        _moneyGroup.alpha = 0f;
        _repGroup.alpha = 0f;
        _continueGroup.alpha = 0f;
        _discoverySection.gameObject.SetActive(data.isFirstDiscovery);
        if (data.isFirstDiscovery)
            _discoveryGroup.alpha = 0f;
    }

    void TryDismiss()
    {
        if (!_canDismiss) return;
        _canDismiss = false;
        _continueButton.interactable = false;
        _backdropButton.interactable = false;

        _activeSequence?.Kill();
        var seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(PremiereAnimations.OverlayFadeOut(_overlayGroup));
        if (_cardGroup != null)
            seq.Join(_cardGroup.DOFade(0f, 0.18f));
        seq.OnComplete(() =>
        {
            gameObject.SetActive(false);
            var cb = _onClosed;
            _onClosed = null;
            cb?.Invoke();
        });
    }

    static RectTransform CreateSection(Transform parent, string name, RectTransform hook)
    {
        if (hook != null)
            hook.gameObject.SetActive(false);
        return CreatePanel(parent, name, Color.clear);
    }

    static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, float size, Color color,
        FontStyles style, float height)
    {
        var tmp = RuntimeTmpText.Create(parent, text, size, color, style, TextAlignmentOptions.Center, name);
        tmp.raycastTarget = false;
        LE(tmp.rectTransform, height);
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

    static void LE(RectTransform rt, float height, float width = -1f)
    {
        var le = rt.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        if (width > 0f)
        {
            le.preferredWidth = width;
            le.minWidth = width;
        }
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
