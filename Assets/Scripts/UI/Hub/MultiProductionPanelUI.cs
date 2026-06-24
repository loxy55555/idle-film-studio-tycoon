using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Premium active production list inside ProductionWidget (Phase 8.2).</summary>
public class MultiProductionPanelUI : MonoBehaviour
{
    public static MultiProductionPanelUI Instance { get; private set; }

    RectTransform _slotsRoot;
    ScrollRect    _scroll;
    LayoutElement _widgetLE;
    StudioManager _studio;
    UpgradeSystem _upgrades;
    MovieConfig[] _catalog;
    readonly List<SlotView> _slots = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        PrepareWidgetRoot();
        HideLegacyLayout();
        BuildSlotsRoot();
        GameHub.OnGameReady += Bind;
        if (GameHub.Instance != null) Bind();
        ProductionPremiereHooks.EnsureOnCanvas();
    }

    void OnDestroy()
    {
        GameHub.OnGameReady -= Bind;
        Unbind();
        if (Instance == this) Instance = null;
    }

    void PrepareWidgetRoot()
    {
        _widgetLE = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();

        var hlg = GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) hlg.enabled = false;

        var rt = transform as RectTransform;
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }

    void Bind()
    {
        Unbind();
        _studio = GameHub.Instance?.studio;
        _upgrades = GameHub.Instance?.upgrades;
        _catalog = MoviePosterVisual.ResolveCatalog();
        if (_studio != null)
            _studio.OnProductionsChanged += Refresh;
        if (_upgrades != null)
            _upgrades.OnUpgradePurchased += Refresh;
        Refresh();
    }

    void Unbind()
    {
        if (_studio != null)
            _studio.OnProductionsChanged -= Refresh;
        if (_upgrades != null)
            _upgrades.OnUpgradePurchased -= Refresh;
    }

    void Update()
    {
        if (_studio == null && GameHub.Instance?.studio != null)
            Bind();
        if (_studio == null) return;
        RefreshProgressOnly();
    }

    void HideLegacyLayout()
    {
        foreach (Transform child in transform)
        {
            if (child.name == "MultiProdScroll") continue;
            child.gameObject.SetActive(false);
        }
    }

    void BuildSlotsRoot()
    {
        var existing = transform.Find("MultiProdScroll");
        if (existing != null) Destroy(existing.gameObject);

        var scrollGo = new GameObject("MultiProdScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.transform.SetParent(transform, false);

        var scrollLE = scrollGo.AddComponent<LayoutElement>();
        scrollLE.flexibleWidth = 1f;
        scrollLE.flexibleHeight = 1f;
        scrollLE.minHeight = ActiveProductionSlotLayout.SlotHeight;

        HudSkinProvider.ApplyPanel(scrollGo.GetComponent<Image>(), HudPanelVariant.Card);

        var scrollRT = scrollGo.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(6f, 6f);
        scrollRT.offsetMax = new Vector2(-6f, -6f);

        _scroll = scrollGo.GetComponent<ScrollRect>();
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scrollGo.transform, false);
        var vpRT = viewport.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
        _scroll.viewport = vpRT;

        var content = new GameObject("SlotsContent", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        _slotsRoot = content.GetComponent<RectTransform>();
        _slotsRoot.anchorMin = new Vector2(0f, 1f);
        _slotsRoot.anchorMax = new Vector2(1f, 1f);
        _slotsRoot.pivot = new Vector2(0.5f, 1f);
        _slotsRoot.offsetMin = _slotsRoot.offsetMax = Vector2.zero;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _scroll.content = _slotsRoot;
    }

    public void RefreshLocalization() => Refresh();

    void OnEnable() => Refresh();

    void Refresh()
    {
        if (_studio == null || _slotsRoot == null) return;
        if (_catalog == null) _catalog = MoviePosterVisual.ResolveCatalog();

        var active = _studio.GetProductionSnapshots();
        int maxSlots = Mathf.Max(1, _studio.maxMovieSlots);
        int displayCount = Mathf.Max(active.Count, maxSlots, 2);

        EnsureSlotCount(displayCount);
        for (int i = 0; i < displayCount; i++)
        {
            if (i < active.Count)
                _slots[i].Apply(active[i], _catalog);
            else if (i >= maxSlots)
                _slots[i].ApplyLocked();
            else
                _slots[i].ApplyIdle();
        }

        float slotH = ActiveProductionSlotLayout.SlotHeight;
        float contentHeight = displayCount * slotH + (displayCount - 1) * 6f + 12f;
        _widgetLE.preferredHeight = contentHeight;
        _widgetLE.minHeight = contentHeight;
        _widgetLE.flexibleHeight = 0f;

        _scroll.enabled = false;
        LayoutRebuilder.ForceRebuildLayoutImmediate(_slotsRoot);
    }

    void RefreshProgressOnly()
    {
        if (_studio == null) return;
        var active = _studio.GetProductionSnapshots();
        for (int i = 0; i < _slots.Count && i < active.Count; i++)
            _slots[i].UpdateProgress(active[i]);
    }

    void EnsureSlotCount(int count)
    {
        while (_slots.Count < count)
        {
            var view = SlotView.Create(_slotsRoot);
            _slots.Add(view);
            view.root.localScale = Vector3.one * 0.94f;
            view.root.DOScale(1f, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);
        }
        while (_slots.Count > count)
        {
            var last = _slots[_slots.Count - 1];
            _slots.RemoveAt(_slots.Count - 1);
            if (last.root != null) Destroy(last.root.gameObject);
        }
    }

    class SlotView
    {
        public RectTransform root;
        ActiveProductionSlotLayout.SlotRefs _refs;
        string _movieKey;
        float _timeLeftSeconds;

        public static SlotView Create(Transform parent)
        {
            var view = new SlotView();
            view._refs = ActiveProductionSlotLayout.Create(parent);
            view.root = view._refs.root;
            if (view._refs.discoverButton != null)
            {
                view._refs.discoverButton.onClick.RemoveAllListeners();
                view._refs.discoverButton.onClick.AddListener(view.OnDiscoverClicked);
            }
            if (view._refs.speedUpButton != null)
            {
                view._refs.speedUpButton.onClick.RemoveAllListeners();
                view._refs.speedUpButton.onClick.AddListener(view.OnSpeedUpClicked);
            }
            return view;
        }

        void OnSpeedUpClicked()
        {
            if (string.IsNullOrEmpty(_movieKey)) return;
            // Guard kept as safety net — button is already non-interactable when no options.
            if (!ProductionSpeedUpService.HasAnyOption(_timeLeftSeconds)) return;

            ProductionSpeedUpDialogUI.Show(_movieKey, _timeLeftSeconds, () =>
            {
                MultiProductionPanelUI.Instance?.Refresh();
            });
        }

        void OnDiscoverClicked()
        {
            var studio = GameHub.Instance?.studio;
            if (studio == null || string.IsNullOrEmpty(_movieKey)) return;
            if (!studio.TryPrepareDiscovery(_movieKey, out var payload)) return;

            _refs.discoverButton.interactable = false;
            ActiveProductionSlotLayout.SetDiscoverPulse(_refs, false);

            PremiereSequenceController.TryPresent(payload, () =>
            {
                studio.FinalizeDiscovery(_movieKey);
                if (GameFeelUI.Instance != null && GameFeelUI.Instance.productionWidget != null)
                {
                    var w = GameFeelUI.Instance.productionWidget;
                    w.DOKill();
                    w.localScale = Vector3.one;
                    w.DOPunchScale(Vector3.one * 0.06f, 0.3f, 8, 0.5f).SetUpdate(true);
                }
            });
        }

        public void Apply(ProductionSlotSnapshot snap, MovieConfig[] catalog)
        {
            _movieKey = snap.movieKey;
            _timeLeftSeconds = snap.timeLeftSeconds;
            ResetTitleTypography();
            _refs.titleText.text = snap.movieName;

            if (snap.awaitingDiscovery)
            {
                SetSpeedUpVisible(false);
                _refs.statusText.text = Loc.Get(LocKeys.ProdStatusComplete);
                _refs.statusText.color = CinematicTheme.GoldBright;
                if (_refs.progressRow != null) _refs.progressRow.gameObject.SetActive(false);
                if (_refs.metaRow != null) _refs.metaRow.gameObject.SetActive(false);
                if (_refs.discoverLabel != null)
                    _refs.discoverLabel.text = Loc.Get(LocKeys.ProdDiscoverButton);
                if (_refs.discoverButton != null)
                {
                    _refs.discoverButton.gameObject.SetActive(true);
                    _refs.discoverButton.interactable = true;
                }
                ActiveProductionSlotLayout.SetDiscoverPulse(_refs, true);
                if (_refs.smoothBar != null) _refs.smoothBar.SetNormalized(1f);
                else if (_refs.progressBar != null) _refs.progressBar.value = 1f;
            }
            else
            {
                // Show the row for any active production; interactable only when options exist.
                bool hasOption = ProductionSpeedUpService.HasAnyOption(snap.timeLeftSeconds);
                SetSpeedUpVisible(snap.timeLeftSeconds > 0f);
                SetSpeedUpInteractable(hasOption);
                if (_refs.speedUpLabel != null)
                    _refs.speedUpLabel.text = Loc.Get(LocKeys.ProdSpeedUpTitle);
                _refs.statusText.text = Loc.Get(LocKeys.ProdStatusProducing);
                _refs.statusText.color = CinematicTheme.GoldBase;
                if (_refs.progressRow != null) _refs.progressRow.gameObject.SetActive(true);
                if (_refs.metaRow != null) _refs.metaRow.gameObject.SetActive(true);
                if (_refs.discoverButton != null) _refs.discoverButton.gameObject.SetActive(false);
                ActiveProductionSlotLayout.SetDiscoverPulse(_refs, false);
                _refs.timeText.text = Loc.Format(LocKeys.ProdTimeFmt, ProductionLoc.FormatTimeRemaining(snap.timeLeftSeconds));
                if (_refs.rewardText != null)
                    _refs.rewardText.text = Loc.Format(LocKeys.ProdSlotRewardFmt, AnimatedMoneyText.FormatMoney(snap.rewardMoney), snap.rewardRep);
                UpdateProgress(snap);
            }

            var cfg = MoviePosterVisual.FindByName(catalog, snap.movieName);
            if (cfg != null)
                ShowSlotIcon(cfg);
            else
            {
                HideSlotIcon();
                if (_refs.rarityFrame != null) _refs.rarityFrame.color = Color.clear;
                MovieRarityVisual.ApplyCardBorder(_refs.root, MovieRarity.Common);
            }
        }

        void ShowSlotIcon(MovieConfig cfg)
        {
            var wrap = _refs.bodyRow != null ? _refs.bodyRow.Find("RarityIconWrap") : null;
            if (wrap == null) return;

            var sprite = UIIconCatalog.GetProductionIcon(cfg.rarity, cfg.genre);
            if (sprite == null)
            {
                HideSlotIcon();
                MovieRarityVisual.ApplyCardBorder(_refs.root, cfg.rarity);
                return;
            }

            wrap.gameObject.SetActive(true);
            if (_refs.rarityIconText != null)
            {
                _refs.rarityIconText.gameObject.SetActive(false);
            }
            UIIconGraphic.ApplyGenreRarityIcon(wrap, sprite, _refs.rarityIconText);
            MovieRarityVisual.ApplyCardBorder(_refs.root, cfg.rarity);
        }

        void HideSlotIcon()
        {
            var wrap = _refs.bodyRow != null ? _refs.bodyRow.Find("RarityIconWrap") : null;
            if (wrap != null) wrap.gameObject.SetActive(false);
            if (_refs.rarityIconText != null) _refs.rarityIconText.gameObject.SetActive(false);
            var iconSprite = wrap != null ? wrap.Find("RarityIconSprite") : null;
            if (iconSprite != null) iconSprite.gameObject.SetActive(false);
        }

        public void ApplyIdle()
        {
            _movieKey = null;
            _timeLeftSeconds = 0f;
            SetSpeedUpVisible(false);
            ActiveProductionSlotLayout.SetDiscoverPulse(_refs, false);
            ResetTitleTypography();
            _refs.titleText.text = Loc.Get(LocKeys.ProdNoActive);
            _refs.statusText.text = Loc.Get(LocKeys.ProdSlotFree);
            _refs.statusText.fontSize = 14f;
            _refs.statusText.color = CinematicTheme.TextDim;
            _refs.statusText.alignment = TextAlignmentOptions.MidlineRight;
            if (_refs.bodyRow != null) _refs.bodyRow.gameObject.SetActive(true);
            if (_refs.progressRow != null) _refs.progressRow.gameObject.SetActive(true);
            if (_refs.metaRow != null) _refs.metaRow.gameObject.SetActive(true);
            if (_refs.discoverButton != null) _refs.discoverButton.gameObject.SetActive(false);
            _refs.timeText.text = Loc.Get(LocKeys.ProdStartMovie);
            if (_refs.rewardText != null) _refs.rewardText.text = string.Empty;
            if (_refs.rarityIconText != null)
            {
                _refs.rarityIconText.text = string.Empty;
                _refs.rarityIconText.gameObject.SetActive(false);
            }
            HideSlotIcon();
            MovieRarityVisual.ApplyCardBorder(_refs.root, MovieRarity.Common);
            if (_refs.rarityFrame != null) _refs.rarityFrame.color = Color.clear;
            if (_refs.smoothBar != null) _refs.smoothBar.SetNormalized(0f);
            else if (_refs.progressBar != null) _refs.progressBar.value = 0f;
        }

        public void ApplyLocked()
        {
            _movieKey = null;
            _timeLeftSeconds = 0f;
            SetSpeedUpVisible(false);
            ActiveProductionSlotLayout.SetDiscoverPulse(_refs, false);
            ResetTitleTypography();
            _refs.titleText.text = Loc.Get(LocKeys.ProdSlotLocked);
            _refs.titleText.fontSize = 22f;
            _refs.titleText.fontSizeMin = 18f;
            _refs.titleText.fontSizeMax = 24f;
            _refs.titleText.alignment = TextAlignmentOptions.MidlineLeft;
            _refs.titleText.color = CinematicTheme.TextSecondary;
            _refs.statusText.text = Loc.Get(LocKeys.ProdSlotUnlock);
            _refs.statusText.fontSize = 14f;
            _refs.statusText.color = CinematicTheme.TextDim;
            _refs.statusText.alignment = TextAlignmentOptions.MidlineRight;
            if (_refs.bodyRow != null) _refs.bodyRow.gameObject.SetActive(true);
            if (_refs.progressRow != null) _refs.progressRow.gameObject.SetActive(false);
            if (_refs.metaRow != null) _refs.metaRow.gameObject.SetActive(false);
            if (_refs.discoverButton != null) _refs.discoverButton.gameObject.SetActive(false);
            if (_refs.rarityIconText != null)
            {
                _refs.rarityIconText.text = string.Empty;
                _refs.rarityIconText.gameObject.SetActive(false);
            }
            HideSlotIcon();
            if (_refs.rarityFrame != null) _refs.rarityFrame.color = Color.clear;
            MovieRarityVisual.ApplyCardBorder(_refs.root, MovieRarity.Common);
        }

        void ResetTitleTypography()
        {
            if (_refs.titleText == null) return;
            MovieOfferCardLayoutBuilder.ApplyTitleTypography(_refs.titleText, TextAlignmentOptions.MidlineLeft);
            _refs.titleText.enableAutoSizing = true;
            _refs.titleText.fontSizeMin = 14f;
            _refs.titleText.fontSizeMax = 18f;
            _refs.titleText.fontSize = 18f;
            _refs.titleText.color = CinematicTheme.TextPrimary;
        }

        public void UpdateProgress(ProductionSlotSnapshot snap)
        {
            if (snap.awaitingDiscovery) return;
            _timeLeftSeconds = snap.timeLeftSeconds;
            bool hasOption = ProductionSpeedUpService.HasAnyOption(snap.timeLeftSeconds);
            SetSpeedUpVisible(snap.timeLeftSeconds > 0f);
            SetSpeedUpInteractable(hasOption);
            if (_refs.smoothBar != null) _refs.smoothBar.SetNormalized(snap.progress01);
            else if (_refs.progressBar != null) _refs.progressBar.value = snap.progress01;
            _refs.timeText.text = Loc.Format(LocKeys.ProdTimeFmt, ProductionLoc.FormatTimeRemaining(snap.timeLeftSeconds));
            if (_refs.rewardText != null)
                _refs.rewardText.text = Loc.Format(LocKeys.ProdSlotRewardFmt, AnimatedMoneyText.FormatMoney(snap.rewardMoney), snap.rewardRep);
        }

        void SetSpeedUpVisible(bool visible)
        {
            if (_refs.speedUpRow != null)
                _refs.speedUpRow.gameObject.SetActive(visible);
        }

        void SetSpeedUpInteractable(bool interactable)
        {
            if (_refs.speedUpButton != null)
                _refs.speedUpButton.interactable = interactable;
        }
    }
}
