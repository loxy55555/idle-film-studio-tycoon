using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FASE 15.2A / 15.9D / 17B — Store UI wired to PurchaseManager (Google Play Billing).
///   • Diamond packs / premium packs / NoAds — real IAP
///   • Rewarded ads unchanged
///   • Offline Premium (Productor Remoto) — hidden from V1.0 store UI (FASE 17B.0)
/// </summary>
public class StorePanelUI : MonoBehaviour
{
    static readonly Color BG_DEEP     = new Color(0.05f, 0.06f, 0.11f);
    static readonly Color BG_CARD     = new Color(0.10f, 0.11f, 0.19f);
    static readonly Color BG_DARK     = new Color(0.04f, 0.04f, 0.08f);
    static readonly Color TEXT_PRI    = CinematicTheme.TextPrimary;
    static readonly Color TEXT_SEC    = CinematicTheme.SilverBase;
    static readonly Color ACCENT_GOLD = CinematicTheme.GoldBright;
    static readonly Color ACCENT_BLUE = new Color(0.25f, 0.55f, 0.90f);
    static readonly Color ACCENT_PURP = new Color(0.61f, 0.35f, 0.71f);
    static readonly Color ACCENT_GREEN = CinematicTheme.BronzeBase;
    static readonly Color ACCENT_RED    = new Color(0.85f, 0.25f, 0.25f);
    static readonly Color ACCENT_ORANGE = new Color(0.88f, 0.50f, 0.15f);
    static readonly Color BTN_BUY     = CinematicTheme.ButtonSuccess;
    static readonly Color BTN_AD      = CinematicTheme.GoldDim;   // warm amber — replaces generic green
    static readonly Color BTN_OWNED   = new Color(0.3f, 0.3f, 0.3f);

    // Shared action-button geometry — Inversor hero + Featured Supporter banner
    const float StorePromoButtonWidth = 92f;

    bool _built;
    bool _iapSubscribed;

    // Toast label for feedback messages
    TextMeshProUGUI _toastLabel;
    float           _toastTimer;

    // BUG-05: Live timer labels for active boosts — updated every frame without rebuilding
    readonly System.Collections.Generic.Dictionary<BoostSystem.BoostType, TextMeshProUGUI>
        _boostTimerLabels = new System.Collections.Generic.Dictionary<BoostSystem.BoostType, TextMeshProUGUI>();

    // H1 (FASE 16.1): Investor cooldown label — updated every frame so the countdown is live
    readonly System.Collections.Generic.List<TextMeshProUGUI> _investorCooldownLabels = new();

    void Awake()
    {
        EnsureBuilt();
        UserPrefs.OnLanguageChanged += RefreshLocalization;
        AdRewardUI.Register(ShowToast);
        SubscribeBoostEvents();
        GameHub.OnGameReady += OnGameHubReady;
    }

    void OnGameHubReady()
    {
        SubscribeIapEvents();
        RefreshLocalization();
    }

    void SubscribeIapEvents()
    {
        if (_iapSubscribed) return;
        var pm = PurchaseManager.Instance;
        if (pm == null) return;
        pm.OnCatalogReady += RefreshLocalization;
        pm.OnEntitlementsChanged += RefreshLocalization;
        pm.OnIapError += OnIapError;
        _iapSubscribed = true;
    }

    void OnIapError(IapErrorKind kind) => RefreshLocalization();

    void OnDestroy()
    {
        GameHub.OnGameReady -= OnGameHubReady;
        if (PurchaseManager.Instance != null && _iapSubscribed)
        {
            PurchaseManager.Instance.OnCatalogReady -= RefreshLocalization;
            PurchaseManager.Instance.OnEntitlementsChanged -= RefreshLocalization;
            PurchaseManager.Instance.OnIapError -= OnIapError;
        }
        UserPrefs.OnLanguageChanged -= RefreshLocalization;
        AdRewardUI.Register(null);
        UnsubscribeBoostEvents();
    }

    void SubscribeBoostEvents()
    {
        if (BoostSystem.Instance != null)
            BoostSystem.Instance.OnBoostsChanged += RefreshLocalization;
        else
            GameHub.OnGameReady += OnGameReady;
    }

    void UnsubscribeBoostEvents()
    {
        if (BoostSystem.Instance != null)
            BoostSystem.Instance.OnBoostsChanged -= RefreshLocalization;
        GameHub.OnGameReady -= OnGameReady;
    }

    void OnGameReady()
    {
        GameHub.OnGameReady -= OnGameReady;
        if (BoostSystem.Instance != null)
            BoostSystem.Instance.OnBoostsChanged += RefreshLocalization;
    }

    void Start()    => EnsureBuilt();
    void OnEnable() => EnsureBuilt();

    void Update()
    {
        if (_toastTimer > 0f)
        {
            _toastTimer -= Time.deltaTime;
            if (_toastLabel != null)
                _toastLabel.alpha = Mathf.Clamp01(_toastTimer);
            if (_toastTimer <= 0f && _toastLabel != null)
                _toastLabel.gameObject.SetActive(false);
        }

        // BUG-05: Tick active boost timers every frame without rebuilding the store
        if (_boostTimerLabels.Count > 0)
        {
            var boosts = BoostSystem.Instance;
            if (boosts != null)
            {
                foreach (var kv in _boostTimerLabels)
                {
                    var label = kv.Value;
                    if (label == null) continue; // safely skip destroyed labels after a rebuild
                    if (boosts.IsActive(kv.Key))
                    {
                        float rem = boosts.GetTimeRemaining(kv.Key);
                        int mins = (int)(rem / 60f);
                        int secs = (int)(rem % 60f);
                        label.text = $"{Loc.Get(LocKeys.StoreBoostActive)} {mins:0}m {secs:00}s";
                    }
                }
            }
        }

        // H1 (FASE 16.1): Tick investor cooldown in real time without rebuilding the store
        if (_investorCooldownLabels.Count > 0)
        {
            float cd = AdRewardSystem.GetInvestorCooldownRemaining();
            if (cd > 0f)
            {
                int mins = (int)(cd / 60f);
                int secs = (int)(cd % 60f);
                string cdText = string.Format(Loc.Get(LocKeys.InvestorCooldownFmt), mins, secs);
                foreach (var lbl in _investorCooldownLabels)
                    if (lbl != null) lbl.text = cdText;
            }
            else
            {
                // Cooldown just expired — rebuild so the "claim" button appears
                _investorCooldownLabels.Clear();
                RefreshLocalization();
            }
        }
    }

    public void RefreshLocalization()
    {
        _built = false;
        EnsureBuilt();
    }

    public void EnsureBuilt()
    {
        if (_built && transform.Find("StoreRoot") != null) return;
        _built = true;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name == "StoreRoot") continue;
            child.gameObject.SetActive(false);
        }

        var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        BuildStore();
    }

    void BuildStore()
    {
        // BUG-01: Destroy ALL existing StoreRoot instances immediately so that a new
        // StoreRoot is always built on a clean slate — Destroy() is deferred to end-of-frame
        // which let multiple roots accumulate during the same frame.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name == "StoreRoot")
                DestroyImmediate(child.gameObject);
        }
        _boostTimerLabels.Clear();     // BUG-05: stale label refs invalidated on rebuild
        _investorCooldownLabels.Clear(); // H1: stale cooldown label refs invalidated on rebuild
        _toastTimer = 0f;          // prevent ghost toast after rebuild

        var root = new GameObject("StoreRoot", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(transform, false);
        root.GetComponent<Image>().color = BG_DEEP;
        Stretch(root.GetComponent<RectTransform>());
        var rootLE = root.AddComponent<LayoutElement>();
        rootLE.flexibleWidth = 1f; rootLE.flexibleHeight = 1f;

        // Scroll container
        var scroll = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scroll.transform.SetParent(root.transform, false);
        Stretch(scroll.GetComponent<RectTransform>());
        scroll.GetComponent<Image>().color = Color.clear;
        var sr = scroll.GetComponent<ScrollRect>();
        sr.horizontal = false; sr.vertical = true;
        sr.scrollSensitivity = 36f; sr.inertia = true; sr.decelerationRate = 0.135f;

        var vp = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        vp.transform.SetParent(scroll.transform, false);
        Stretch(vp.GetComponent<RectTransform>());
        sr.viewport = vp.GetComponent<RectTransform>();

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(vp.transform, false);
        var cRT = content.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0f, 1f); cRT.anchorMax = new Vector2(1f, 1f);
        cRT.pivot = new Vector2(0.5f, 1f); cRT.offsetMin = cRT.offsetMax = Vector2.zero;
        sr.content = cRT;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.padding = HudLayoutConstants.SectionPadding;
        vlg.spacing = HudLayoutConstants.SectionSpacing;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Toast overlay ─────────────────────────────────────────────────────
        BuildToast(root.transform);

        // ── Header ───────────────────────────────────────────────────────────
        var hdr = RuntimeTmpText.Create(content.transform, Loc.Get(LocKeys.TiendaTitle),
            36f, TEXT_PRI, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "Header");
        hdr.raycastTarget = false;
        hdr.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;

        // ── BLOQUE A: Inversor — hero card, elemento principal ──────────────
        BuildInvestorHero(content.transform);

        // ── Oferta Destacada — segunda en importancia, sin separador dorado ──
        BuildFeaturedBanner(content.transform);

        // ── Diamond Packs — sección secundaria, sin ruido dorado ─────────────
        BuildSubtleHeader(content.transform, Loc.Get(LocKeys.StoreSectionDiamonds));
        var dRow1 = BuildRow(content.transform, 200f);
        BuildDiamondCard(dRow1, "100",  IapProductCatalog.Diamonds100);
        BuildDiamondCard(dRow1, "550",  IapProductCatalog.Diamonds550);
        var dRow2 = BuildRow(content.transform, 200f);
        BuildDiamondCard(dRow2, "1.500", IapProductCatalog.Diamonds1500);
        BuildDiamondCard(dRow2, "5.000", IapProductCatalog.Diamonds5000);

        // ── Premium Packs — 2 columns for mobile legibility ─────────────────
        BuildSectionHeader(content.transform, Loc.Get(LocKeys.StoreSectionPacks));
        var pRow1 = BuildRow(content.transform, 310f);
        BuildPackCard(pRow1, PremiumPackCatalog.PackId.Supporter,         null, Loc.Get(LocKeys.StorePackSupporter), ACCENT_PURP, false, false);
        BuildPackCard(pRow1, PremiumPackCatalog.PackId.Producer,          null, Loc.Get(LocKeys.StorePackProducer),  ACCENT_PURP, true,  false);
        var pRow2 = BuildRow(content.transform, 310f);
        BuildPackCard(pRow2, PremiumPackCatalog.PackId.ExecutiveProducer, null, Loc.Get(LocKeys.StorePackExecutive), ACCENT_PURP, false, true);
        var packPad = new GameObject("PackPad", typeof(RectTransform));
        packPad.transform.SetParent(pRow2, false);
        packPad.AddComponent<LayoutElement>().flexibleWidth = 1f;

        // ── Boost Ads — 2 columns ────────────────────────────────────────────
        BuildSubtleHeader(content.transform, Loc.Get(LocKeys.StoreSectionBoosts));
        var bRow1 = BuildRow(content.transform, 200f);
        BuildAdBoostCard(bRow1, null, Loc.Get(LocKeys.StoreProdBoostInc),  BoostSystem.BoostType.Income, AdRewardSystem.PlacementBoostIncome, ACCENT_ORANGE);
        BuildAdBoostCard(bRow1, null, Loc.Get(LocKeys.StoreProdBoostXP),   BoostSystem.BoostType.XP,     AdRewardSystem.PlacementBoostXP,     ACCENT_ORANGE);
        var bRow2 = BuildRow(content.transform, 200f);
        BuildAdBoostCard(bRow2, null, Loc.Get(LocKeys.StoreProdBoostRep),  BoostSystem.BoostType.Rep,    AdRewardSystem.PlacementBoostRep,    ACCENT_ORANGE);
        var boostPad = new GameObject("BoostPad", typeof(RectTransform));
        boostPad.transform.SetParent(bRow2, false);
        boostPad.AddComponent<LayoutElement>().flexibleWidth = 1f;

        // ── Diamantes Gratis — sección ligera ─────────────────────────────────
        BuildSubtleHeader(content.transform, Loc.Get(LocKeys.StoreSectionFreeRewards));
        var fRow = BuildRow(content.transform, 200f);
        BuildAdSimpleCard(fRow, null, Loc.Get(LocKeys.StoreProdFreeDiam),
            $"+{AdRewardSystem.FreeDiamondsReward}  (1/{AdRewardSystem.LimitFreeDiamonds}{Loc.Get(LocKeys.StorePerDay)})",
            ACCENT_GREEN, AdRewardSystem.PlacementFreeDiamonds);
        var freePadding = new GameObject("FreePad", typeof(RectTransform));
        freePadding.transform.SetParent(fRow, false);
        freePadding.AddComponent<LayoutElement>().flexibleWidth = 1f;

        // ── BLOQUE B: Premium permanente — Sin Anuncios (V1.0) ─────────────────
        BuildSectionHeader(content.transform, Loc.Get(LocKeys.StoreSectionPremium));
        var premiumGroup = BuildPremiumGroupContainer(content.transform);
        BuildNoAdsBanner(premiumGroup);
    }

    // ── Investor hero card — full-width, máxima prominencia ─────────────────────

    void BuildInvestorHero(Transform parent)
    {
        var hero = new GameObject("InvestorHero", typeof(RectTransform), typeof(Image));
        hero.transform.SetParent(parent, false);
        hero.GetComponent<Image>().color = BG_CARD;
        hero.AddComponent<LayoutElement>().preferredHeight = 260f;
        CinematicTheme.ApplyElevationCard(hero.GetComponent<RectTransform>());
        hero.GetComponent<Image>().color = BG_CARD;

        var vlg = hero.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(16, 16, 14, 14);
        vlg.spacing = 8;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        var sectionLbl = RuntimeTmpText.Create(hero.transform, Loc.Get(LocKeys.StoreSectionInvestor),
            14f, ACCENT_GOLD, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "SectionLbl");
        sectionLbl.raycastTarget = false;
        sectionLbl.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

        var contentRow = new GameObject("ContentRow", typeof(RectTransform));
        contentRow.transform.SetParent(hero.transform, false);
        contentRow.AddComponent<LayoutElement>().flexibleHeight = 1f;
        var hlg = contentRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.UpperLeft;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        var iconCol = new GameObject("IconCol", typeof(RectTransform));
        iconCol.transform.SetParent(contentRow.transform, false);
        iconCol.AddComponent<LayoutElement>().preferredWidth = 48f;
        var iconVLG = iconCol.AddComponent<VerticalLayoutGroup>();
        iconVLG.childAlignment = TextAnchor.MiddleCenter;
        iconVLG.childControlWidth = iconVLG.childControlHeight = true;
        iconVLG.childForceExpandWidth = true; iconVLG.childForceExpandHeight = false;

        var iconTmp = RuntimeTmpText.Create(iconCol.transform, "$",
            42f, ACCENT_GREEN, FontStyles.Bold, TextAlignmentOptions.Center, "Icon");
        iconTmp.raycastTarget = false;
        iconTmp.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;

        var txtCol = new GameObject("TxtCol", typeof(RectTransform));
        txtCol.transform.SetParent(contentRow.transform, false);
        txtCol.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var txtVLG = txtCol.AddComponent<VerticalLayoutGroup>();
        txtVLG.spacing = 4;
        txtVLG.childAlignment = TextAnchor.UpperLeft;
        txtVLG.childControlWidth = txtVLG.childControlHeight = true;
        txtVLG.childForceExpandWidth = true; txtVLG.childForceExpandHeight = false;

        var nameTmp = RuntimeTmpText.Create(txtCol.transform, Loc.Get(LocKeys.StoreProdInvestor),
            30f, TEXT_PRI, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "Name");
        nameTmp.raycastTarget = false;
        nameTmp.textWrappingMode = TextWrappingModes.Normal;
        nameTmp.overflowMode = TextOverflowModes.Ellipsis;
        nameTmp.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;

        float cooldown = AdRewardSystem.GetInvestorCooldownRemaining();
        bool onCooldown = cooldown > 0f;
        string detailText;
        if (onCooldown)
        {
            int mins = (int)(cooldown / 60f);
            int secs = (int)(cooldown % 60f);
            detailText = string.Format(Loc.Get(LocKeys.InvestorCooldownFmt), mins, secs);
        }
        else
        {
            long preview = AdRewardSystem.GetInvestorPreviewReward();
            detailText = preview > 0
                ? $"+${preview:N0}  ·  {AdRewardSystem.InvestorMinutes:0} min"
                : Loc.Get(LocKeys.InvestorReady);
        }

        var detailTmp = RuntimeTmpText.Create(txtCol.transform, detailText,
            22f, onCooldown ? TEXT_SEC : ACCENT_GREEN, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, "Detail");
        detailTmp.raycastTarget = false;
        detailTmp.textWrappingMode = TextWrappingModes.Normal;
        detailTmp.overflowMode = TextOverflowModes.Ellipsis;
        detailTmp.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;

        if (onCooldown)
            _investorCooldownLabels.Add(detailTmp);

        var btnRow = new GameObject("BtnRow", typeof(RectTransform));
        btnRow.transform.SetParent(hero.transform, false);
        btnRow.AddComponent<LayoutElement>().preferredHeight = 48f;
        var btnHLG = btnRow.AddComponent<HorizontalLayoutGroup>();
        btnHLG.childControlWidth = btnHLG.childControlHeight = true;
        btnHLG.childForceExpandWidth = true;
        btnHLG.childForceExpandHeight = true;

        if (onCooldown)
        {
            var coolBtn = MakeButton(btnRow.transform, detailText, BTN_OWNED, -1f);
            coolBtn.interactable = false;
            var coolBtnLbl = coolBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (coolBtnLbl != null) _investorCooldownLabels.Add(coolBtnLbl);
        }
        else
        {
            var btn = MakeButton(btnRow.transform, Loc.Get(LocKeys.StoreWatchAd), ACCENT_GREEN, -1f);
            btn.onClick.AddListener(() =>
            {
                AdRewardSystem.RequestReward(AdRewardSystem.PlacementInvestor);
                RefreshLocalization();
            });
        }
    }

    // ── Subtle section header — sin línea dorada para secciones secundarias ────

    void BuildSubtleHeader(Transform parent, string title)
    {
        var container = new GameObject("SubtleHdr", typeof(RectTransform));
        container.transform.SetParent(parent, false);
        container.AddComponent<LayoutElement>().preferredHeight = 30f;

        var lbl = RuntimeTmpText.Create(container.transform, title,
            20f, TEXT_SEC, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "Label");
        lbl.raycastTarget = false;
        Stretch(lbl.GetComponent<RectTransform>());
    }

    // ── Featured Supporter banner ──────────────────────────────────────────────

    void BuildFeaturedBanner(Transform parent)
    {
        bool owned = PremiumPackCatalog.PackAlreadyPurchased(PremiumPackCatalog.PackId.Supporter);

        var banner = new GameObject("Featured", typeof(RectTransform), typeof(Image));
        banner.transform.SetParent(parent, false);
        banner.GetComponent<Image>().color = BG_CARD;
        banner.AddComponent<LayoutElement>().preferredHeight = 188f;
        CinematicTheme.ApplyElevationCard(banner.GetComponent<RectTransform>());
        banner.GetComponent<Image>().color = BG_CARD;

        var vlg = banner.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(16, 16, 12, 12);
        vlg.spacing = 8;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        var contentRow = new GameObject("ContentRow", typeof(RectTransform));
        contentRow.transform.SetParent(banner.transform, false);
        contentRow.AddComponent<LayoutElement>().flexibleHeight = 1f;
        var hlg = contentRow.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.UpperLeft;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        var icon = RuntimeTmpText.Create(contentRow.transform, "♦",
            44f, Color.white, FontStyles.Normal, TextAlignmentOptions.Center, "Icon");
        icon.raycastTarget = false;
        icon.gameObject.AddComponent<LayoutElement>().preferredWidth = 48f;

        var txtCol = new GameObject("Texts", typeof(RectTransform));
        txtCol.transform.SetParent(contentRow.transform, false);
        txtCol.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var txtVLG = txtCol.AddComponent<VerticalLayoutGroup>();
        txtVLG.spacing = 4;
        txtVLG.childAlignment = TextAnchor.UpperLeft;
        txtVLG.childControlWidth = txtVLG.childControlHeight = true;
        txtVLG.childForceExpandWidth = true; txtVLG.childForceExpandHeight = false;

        var t1 = RuntimeTmpText.Create(txtCol.transform, Loc.Get(LocKeys.StorePackSupporter),
            28f, TEXT_PRI, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "T1");
        t1.raycastTarget = false;
        t1.textWrappingMode = TextWrappingModes.Normal;
        t1.overflowMode = TextOverflowModes.Ellipsis;
        t1.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        var t2 = RuntimeTmpText.Create(txtCol.transform, Loc.Get(LocKeys.PackSupporterDesc),
            18f, TEXT_PRI, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, "T2");
        t2.raycastTarget = false;
        t2.textWrappingMode = TextWrappingModes.Normal;
        t2.overflowMode = TextOverflowModes.Ellipsis;
        t2.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;

        var t3 = RuntimeTmpText.Create(txtCol.transform,
            BuildPackRewardLine(PremiumPackCatalog.Get(PremiumPackCatalog.PackId.Supporter)),
            14f, ACCENT_GOLD, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "T3");
        t3.raycastTarget = false;
        t3.textWrappingMode = TextWrappingModes.Normal;
        t3.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        var btnRow = new GameObject("BtnRow", typeof(RectTransform));
        btnRow.transform.SetParent(banner.transform, false);
        btnRow.AddComponent<LayoutElement>().preferredHeight = 48f;
        var btnHLG = btnRow.AddComponent<HorizontalLayoutGroup>();
        btnHLG.childControlWidth = btnHLG.childControlHeight = true;
        btnHLG.childForceExpandWidth = true;
        btnHLG.childForceExpandHeight = true;

        string bannerPrice = GetIapPrice(IapProductCatalog.PackSupporter,
            PremiumPackCatalog.Get(PremiumPackCatalog.PackId.Supporter)?.priceDisplay ?? "4,99 €");
        BuildPackButton(btnRow.transform, owned ? Loc.Get(LocKeys.StorePackAlreadyOwned) : bannerPrice,
            owned,
            () => { PurchaseManager.Instance?.Purchase(IapProductCatalog.PackSupporter); },
            -1f, BTN_AD);
    }

    // ── Diamond pack card ─────────────────────────────────────────────────────

    void BuildDiamondCard(Transform parent, string amount, string productId)
    {
        var card = MakeCard(parent, ACCENT_BLUE, out _);

        AddStoreSpriteIcon(card.transform, UIIconCatalog.GetResourceDiamonds(), 40f);

        RuntimeTmpText.Create(card.transform, Loc.Get(LocKeys.StoreSectionDiamonds), 16f, TEXT_PRI, FontStyles.Bold, TextAlignmentOptions.Center, "Title")
            .gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        RuntimeTmpText.Create(card.transform, amount, 22f, ACCENT_BLUE, FontStyles.Bold, TextAlignmentOptions.Center, "Detail")
            .gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

        var spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(card.transform, false);
        spacer.AddComponent<LayoutElement>().flexibleHeight = 1f;

        string price = GetIapPrice(productId);
        bool blocked = IsIapPurchaseBlocked();
        BuildIAPButton(card.transform, price, -1f, () =>
        {
            PurchaseManager.Instance?.Purchase(productId);
        }, ACCENT_BLUE, blocked);
    }

    // ── Premium Pack card ─────────────────────────────────────────────────────

    void BuildPackCard(Transform parent, PremiumPackCatalog.PackId packId, string icon, string name, Color accent, bool popular, bool premium)
    {
        var pack = PremiumPackCatalog.Get(packId);
        bool owned = PremiumPackCatalog.PackAlreadyPurchased(packId);

        var card = MakeCard(parent, accent, out _);

        // BLOQUE C: tier-based strip height — Supporter 4px · Producer 6px · Executive 8px
        float stripH = premium ? 8f : popular ? 6f : 4f;
        var stripLE = card.transform.Find("Strip")?.GetComponent<LayoutElement>();
        if (stripLE != null) stripLE.preferredHeight = stripH;

        // "POPULAR" badge on Producer pack
        if (popular)
        {
            var badge = RuntimeTmpText.Create(card.transform, "· POPULAR ·",
                14f, accent, FontStyles.Bold, TextAlignmentOptions.Center, "Badge");
            badge.raycastTarget = false;
            badge.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        }

        // "PREMIUM" badge on Executive Producer pack — gold to match permanent-upgrade aesthetic
        if (premium)
        {
            var badge = RuntimeTmpText.Create(card.transform, "· PREMIUM ·",
                14f, ACCENT_GOLD, FontStyles.Bold, TextAlignmentOptions.Center, "Badge");
            badge.raycastTarget = false;
            badge.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        }

        AddStoreSpriteIcon(card.transform, UIIconCatalog.GetResourceDiamonds(), 44f);

        RuntimeTmpText.Create(card.transform, name, 16f, TEXT_PRI, FontStyles.Bold, TextAlignmentOptions.Center, "Title")
            .gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        string rewardLine = BuildPackRewardLine(pack);
        var detailTmp = RuntimeTmpText.Create(card.transform, rewardLine, 14f, accent, FontStyles.Bold, TextAlignmentOptions.Center, "Detail");
        detailTmp.raycastTarget = false;
        detailTmp.textWrappingMode = TextWrappingModes.Normal;
        detailTmp.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;

        string descKey = packId switch
        {
            PremiumPackCatalog.PackId.Supporter       => LocKeys.PackSupporterDesc,
            PremiumPackCatalog.PackId.Producer        => LocKeys.PackProducerDesc,
            PremiumPackCatalog.PackId.ExecutiveProducer => LocKeys.PackExecutiveDesc,
            _ => LocKeys.PackSupporterDesc,
        };
        var descTmp = RuntimeTmpText.Create(card.transform, Loc.Get(descKey), 14f, TEXT_SEC, FontStyles.Normal,
            packId == PremiumPackCatalog.PackId.Producer
                ? TextAlignmentOptions.MidlineLeft
                : TextAlignmentOptions.Center, "Desc");
        descTmp.raycastTarget = false;
        descTmp.textWrappingMode = TextWrappingModes.Normal;
        descTmp.overflowMode = TextOverflowModes.Ellipsis;
        descTmp.gameObject.AddComponent<LayoutElement>().preferredHeight =
            packId == PremiumPackCatalog.PackId.Producer ? 56f : 44f;

        var spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(card.transform, false);
        spacer.AddComponent<LayoutElement>().flexibleHeight = 1f;

        string price = GetIapPrice(pack?.storeProductId, pack?.priceDisplay ?? "—");
        if (pack == null || string.IsNullOrEmpty(pack.storeProductId)) return;
        BuildPackButton(card.transform, owned ? Loc.Get(LocKeys.StorePackAlreadyOwned) : price, owned,
            () => { PurchaseManager.Instance?.Purchase(pack.storeProductId); }, -1f, accent);
    }

    string BuildPackRewardLine(PremiumPackDef pack)
    {
        if (pack == null) return "";
        var sb = new System.Text.StringBuilder();
        if (pack.diamonds > 0) sb.Append($"{pack.diamonds:N0} {Loc.Get(LocKeys.StoreSectionDiamonds)}");
        if (pack.noAdsIncluded) { if (sb.Length > 0) sb.Append(" + "); sb.Append(Loc.Get(LocKeys.StoreNoAds)); }
        return sb.ToString().Trim();
    }

    // ── Ad Boost card ─────────────────────────────────────────────────────────

    void BuildAdBoostCard(Transform parent, string icon, string name, BoostSystem.BoostType boostType, string placement, Color accent)
    {
        var card = MakeCard(parent, accent, out _);

        var boostSprite = boostType switch
        {
            BoostSystem.BoostType.Income => UIIconCatalog.GetResourceMoney(),
            BoostSystem.BoostType.XP     => UIIconCatalog.GetResourceExperience(),
            BoostSystem.BoostType.Rep    => UIIconCatalog.GetResourceReputation(),
            _                            => UIIconCatalog.GetUtilityBoost(),
        };
        if (boostSprite != null)
            AddStoreSpriteIcon(card.transform, boostSprite, 36f);
        else if (!string.IsNullOrEmpty(icon))
            RuntimeTmpText.Create(card.transform, icon, 24f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center, "Icon")
                .gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;

        RuntimeTmpText.Create(card.transform, name, 16f, TEXT_PRI, FontStyles.Bold, TextAlignmentOptions.Center, "Title")
            .gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        var detail = BuildBoostDetailLabel(card.transform, boostType, placement, accent);
        detail.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;
        _boostTimerLabels[boostType] = detail; // BUG-05: track label for real-time updates

        var spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(card.transform, false);
        spacer.AddComponent<LayoutElement>().flexibleHeight = 1f;

        BuildAdButton(card.transform, () =>
        {
            AdRewardSystem.RequestReward(placement);
            // BUG-02: RefreshLocalization() removed — OnBoostsChanged fires from BoostSystem
            // and triggers the rebuild via the event subscription in SubscribeBoostEvents().
        }, accent);
    }

    TextMeshProUGUI BuildBoostDetailLabel(Transform parent, BoostSystem.BoostType type, string placement, Color accent)
    {
        var boosts = BoostSystem.Instance;
        string text;

        if (boosts != null && boosts.IsActive(type))
        {
            float rem = boosts.GetTimeRemaining(type);
            int mins = (int)(rem / 60f);
            int secs = (int)(rem % 60f);
            text = $"{Loc.Get(LocKeys.StoreBoostActive)} {mins:0}m {secs:00}s";
        }
        else
        {
            int uses = AdRewardSystem.UsesRemaining(placement);
            text = uses > 0 ? $"×2  ·  {uses}/{GetLimit(placement)}{Loc.Get(LocKeys.StorePerDay)}" : Loc.Get(LocKeys.AdLimitReached);
        }

        var tmp = RuntimeTmpText.Create(parent, text, 14f, accent, FontStyles.Bold, TextAlignmentOptions.Center, "Detail");
        tmp.raycastTarget = false;
        return tmp;
    }

    // ── Simple ad reward card (free diamonds) ─────────────────────────────────

    void BuildAdSimpleCard(Transform parent, string icon, string name, string detail, Color accent, string placement)
    {
        var card = MakeCard(parent, accent, out _);

        AddStoreSpriteIcon(card.transform, UIIconCatalog.GetResourceDiamonds(), 36f);

        RuntimeTmpText.Create(card.transform, name, 16f, TEXT_PRI, FontStyles.Bold, TextAlignmentOptions.Center, "Title")
            .gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        int uses = AdRewardSystem.UsesRemaining(placement);
        string detailText = uses > 0 ? detail : Loc.Get(LocKeys.AdLimitReached);
        var detailTmp = RuntimeTmpText.Create(card.transform, detailText, 18f, accent, FontStyles.Bold, TextAlignmentOptions.Center, "Detail");
        detailTmp.raycastTarget = false;
        detailTmp.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

        var spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(card.transform, false);
        spacer.AddComponent<LayoutElement>().flexibleHeight = 1f;

        BuildAdButton(card.transform, () =>
        {
            AdRewardSystem.RequestReward(placement);
            RefreshLocalization();
        }, accent);
    }

    // ── Investor card (BLOQUE F — 10-min cooldown) ───────────────────────────

    void BuildInvestorCard(Transform parent, Color accent)
    {
        var card = MakeCard(parent, accent, out _);

        RuntimeTmpText.Create(card.transform, "$", 26f, Color.white, FontStyles.Normal, TextAlignmentOptions.Center, "Icon")
            .gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        RuntimeTmpText.Create(card.transform, Loc.Get(LocKeys.StoreProdInvestor), 14f, TEXT_PRI, FontStyles.Bold, TextAlignmentOptions.Center, "Title")
            .gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        float cooldown = AdRewardSystem.GetInvestorCooldownRemaining();
        bool onCooldown = cooldown > 0f;

        string detailText;
        if (onCooldown)
        {
            int mins = (int)(cooldown / 60f);
            int secs = (int)(cooldown % 60f);
            detailText = string.Format(Loc.Get(LocKeys.InvestorCooldownFmt), mins, secs);
        }
        else
        {
            long preview = AdRewardSystem.GetInvestorPreviewReward();
            detailText = preview > 0
                ? $"+${preview:N0}\n({AdRewardSystem.InvestorMinutes:0} min)"
                : Loc.Get(LocKeys.InvestorReady);
        }

        var detail = RuntimeTmpText.Create(card.transform, detailText,
            14f, onCooldown ? TEXT_SEC : accent, FontStyles.Normal, TextAlignmentOptions.Center, "Detail");
        detail.raycastTarget = false;
        detail.textWrappingMode = TextWrappingModes.Normal;
        detail.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

        var spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(card.transform, false);
        spacer.AddComponent<LayoutElement>().flexibleHeight = 1f;

        if (onCooldown)
        {
            // Show grayed-out disabled button during cooldown
            MakeButton(card.transform, detailText, BTN_OWNED, -1f).interactable = false;
        }
        else
        {
            BuildAdButton(card.transform, () =>
            {
                AdRewardSystem.RequestReward(AdRewardSystem.PlacementInvestor);
                RefreshLocalization();
            });
        }
    }

    // ── Offline Premium banner (V1.0 hidden — internal system retained) ────────
#if false // FASE 17B.0 — Productor Remoto not in V1.0
    void BuildOfflinePremiumBanner(Transform parent)
    {
        bool owned = PremiumFeatures.Instance?.OfflinePremiumUnlocked ?? false;

        var banner = new GameObject("OfflinePremium", typeof(RectTransform), typeof(Image));
        banner.transform.SetParent(parent, false);
        banner.GetComponent<Image>().color = BG_CARD;
        banner.AddComponent<LayoutElement>().preferredHeight = 168f;
        CinematicTheme.ApplyElevationCard(banner.GetComponent<RectTransform>());
        banner.GetComponent<Image>().color = BG_CARD;

        var vlg = banner.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(16, 16, 10, 10);
        vlg.spacing = 8;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        var contentRow = new GameObject("ContentRow", typeof(RectTransform));
        contentRow.transform.SetParent(banner.transform, false);
        contentRow.AddComponent<LayoutElement>().flexibleHeight = 1f;
        var hlg = contentRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.UpperLeft;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        var iconCol = new GameObject("IconCol", typeof(RectTransform));
        iconCol.transform.SetParent(contentRow.transform, false);
        iconCol.AddComponent<LayoutElement>().preferredWidth = 40f;
        var iconVlg = iconCol.AddComponent<VerticalLayoutGroup>();
        iconVlg.childAlignment = TextAnchor.MiddleCenter;
        iconVlg.childControlWidth = iconVlg.childControlHeight = true;
        iconVlg.childForceExpandWidth = true; iconVlg.childForceExpandHeight = false;

        var icon = RuntimeTmpText.Create(iconCol.transform, "»",
            28f, Color.white, FontStyles.Normal, TextAlignmentOptions.Center, "Icon");
        icon.raycastTarget = false;
        icon.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;

        var badge = RuntimeTmpText.Create(iconCol.transform, "PERM.",
            14f, ACCENT_GOLD, FontStyles.Bold, TextAlignmentOptions.Center, "Badge");
        badge.raycastTarget = false;
        badge.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

        var txtCol = new GameObject("Texts", typeof(RectTransform));
        txtCol.transform.SetParent(contentRow.transform, false);
        txtCol.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var txtVLG = txtCol.AddComponent<VerticalLayoutGroup>();
        txtVLG.spacing = 4;
        txtVLG.childAlignment = TextAnchor.UpperLeft;
        txtVLG.childControlWidth = txtVLG.childControlHeight = true;
        txtVLG.childForceExpandWidth = true; txtVLG.childForceExpandHeight = false;

        RuntimeTmpText.Create(txtCol.transform, Loc.Get(LocKeys.OfflinePremiumName),
            24f, TEXT_PRI, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "T1")
            .gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

        string descText = owned
            ? $"✓ {PremiumFeatures.PremiumOfflineHours:0}h — {Loc.Get(LocKeys.OfflinePremiumOwned)}"
            : Loc.Get(LocKeys.OfflinePremiumDesc);
        var descTmp = RuntimeTmpText.Create(txtCol.transform, descText,
            18f, TEXT_SEC, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, "T2");
        descTmp.raycastTarget = false;
        descTmp.textWrappingMode = TextWrappingModes.Normal;
        descTmp.overflowMode = TextOverflowModes.Ellipsis;
        descTmp.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;

        var btnRow = new GameObject("BtnRow", typeof(RectTransform));
        btnRow.transform.SetParent(banner.transform, false);
        btnRow.AddComponent<LayoutElement>().preferredHeight = 48f;
        var btnHLG = btnRow.AddComponent<HorizontalLayoutGroup>();
        btnHLG.childControlWidth = btnHLG.childControlHeight = true;
        btnHLG.childForceExpandWidth = true;
        btnHLG.childForceExpandHeight = true;

        BuildPackButton(btnRow.transform,
            owned ? Loc.Get(LocKeys.OfflinePremiumOwned) : Loc.Get(LocKeys.OfflinePremiumCostPending),
            owned,
            () =>
            {
                PremiumFeatures.Instance?.PurchaseOfflinePremium();
                ShowToast(Loc.Get(LocKeys.OfflinePremiumName) + " ✓");
                RefreshLocalization();
            },
            -1f, BTN_AD);
    }
#endif

    // ── No Ads banner ─────────────────────────────────────────────────────────

    void BuildNoAdsBanner(Transform parent)
    {
        bool owned = IsNoAdsOwned();

        var banner = new GameObject("Premium", typeof(RectTransform), typeof(Image));
        banner.transform.SetParent(parent, false);
        banner.GetComponent<Image>().color = BG_CARD;
        banner.AddComponent<LayoutElement>().preferredHeight = 158f;
        CinematicTheme.ApplyElevationCard(banner.GetComponent<RectTransform>());
        banner.GetComponent<Image>().color = BG_CARD;

        var vlg = banner.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(16, 16, 12, 12);
        vlg.spacing = 8;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        var contentRow = new GameObject("ContentRow", typeof(RectTransform));
        contentRow.transform.SetParent(banner.transform, false);
        contentRow.AddComponent<LayoutElement>().flexibleHeight = 1f;
        var hlg = contentRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.UpperLeft;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        var icon = RuntimeTmpText.Create(contentRow.transform, "X",
            36f, Color.white, FontStyles.Normal, TextAlignmentOptions.Center, "Icon");
        icon.raycastTarget = false;
        icon.gameObject.AddComponent<LayoutElement>().preferredWidth = 44f;

        var txtCol = new GameObject("Texts", typeof(RectTransform));
        txtCol.transform.SetParent(contentRow.transform, false);
        txtCol.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var txtVLG = txtCol.AddComponent<VerticalLayoutGroup>();
        txtVLG.spacing = 4;
        txtVLG.childAlignment = TextAnchor.UpperLeft;
        txtVLG.childControlWidth = txtVLG.childControlHeight = true;
        txtVLG.childForceExpandWidth = true; txtVLG.childForceExpandHeight = false;

        RuntimeTmpText.Create(txtCol.transform,
            owned ? $"✓ {Loc.Get(LocKeys.StoreNoAds)}" : Loc.Get(LocKeys.StoreNoAds),
            26f, TEXT_PRI, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "T1")
            .gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        var descTmp = RuntimeTmpText.Create(txtCol.transform, Loc.Get(LocKeys.StoreNoAdsDesc),
            18f, TEXT_SEC, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, "T2");
        descTmp.raycastTarget = false;
        descTmp.textWrappingMode = TextWrappingModes.Normal;
        descTmp.overflowMode = TextOverflowModes.Ellipsis;
        descTmp.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;

        var btnRow = new GameObject("BtnRow", typeof(RectTransform));
        btnRow.transform.SetParent(banner.transform, false);
        btnRow.AddComponent<LayoutElement>().preferredHeight = 48f;
        var btnHLG = btnRow.AddComponent<HorizontalLayoutGroup>();
        btnHLG.childControlWidth = btnHLG.childControlHeight = true;
        btnHLG.childForceExpandWidth = true;
        btnHLG.childForceExpandHeight = true;

        BuildPackButton(btnRow.transform,
            owned ? Loc.Get(LocKeys.StorePackAlreadyOwned) : GetIapPrice(IapProductCatalog.NoAds, "3,99 €"),
            owned,
            () => { PurchaseManager.Instance?.Purchase(IapProductCatalog.NoAds); },
            -1f, BTN_AD);
    }

    string GetIapPrice(string productId, string fallback = null)
    {
        var pm = PurchaseManager.Instance;
        if (pm == null)
            return fallback ?? IapProductCatalog.GetFallbackPrice(productId);

        string status = pm.GetStoreStatusLabel();
        if (status != null)
            return status;

        return pm.GetLocalizedPrice(productId);
    }

    bool IsIapPurchaseBlocked()
    {
        var pm = PurchaseManager.Instance;
        if (pm == null) return true;
        if (!pm.IsReady) return true;
        return pm.LastErrorKind == IapErrorKind.NoProductsAvailable;
    }

    bool IsNoAdsOwned() =>
        PurchaseManager.Instance?.IsOwned(IapProductCatalog.NoAds) == true
        || PremiumFeatures.Instance?.NoAdsPurchased == true;

    // ── BLOQUE B: Premium group container ────────────────────────────────────

    /// <summary>
    /// Wraps Sin Anuncios in a shared premium container (FASE 17B.0: Productor Remoto hidden).
    /// </summary>
    Transform BuildPremiumGroupContainer(Transform parent)
    {
        var container = new GameObject("PremiumGroup", typeof(RectTransform), typeof(Image));
        container.transform.SetParent(parent, false);
        // Very slightly purple-tinted dark — visual frame for premium section
        container.GetComponent<Image>().color = new Color(0.08f, 0.07f, 0.14f, 1f);

        var vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.spacing = 4;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        container.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return container.transform;
    }

    // ── Building blocks ───────────────────────────────────────────────────────

    void BuildSectionHeader(Transform parent, string title)
    {
        var container = new GameObject("SectionHdr", typeof(RectTransform));
        container.transform.SetParent(parent, false);
        container.AddComponent<LayoutElement>().preferredHeight = 40f;
        var vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        var tmp = RuntimeTmpText.Create(container.transform, title,
            24f, ACCENT_GOLD, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "Section");
        tmp.raycastTarget = false;
        tmp.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;

        var line = new GameObject("GoldLine", typeof(RectTransform), typeof(Image));
        line.transform.SetParent(container.transform, false);
        line.GetComponent<Image>().color = CinematicTheme.BorderGold;
        line.GetComponent<Image>().raycastTarget = false;
        line.AddComponent<LayoutElement>().preferredHeight = HudLayoutConstants.AccentLineHeight;
    }

    Transform BuildRow(Transform parent, float height)
    {
        var row = new GameObject("Row", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        row.AddComponent<LayoutElement>().preferredHeight = height;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
        return row.transform;
    }

    static void AddStoreSpriteIcon(Transform card, Sprite sprite, float size)
    {
        var wrap = new GameObject("IconWrap", typeof(RectTransform));
        wrap.transform.SetParent(card, false);
        wrap.AddComponent<LayoutElement>().preferredHeight = size + 4f;
        var img = UIIconGraphic.EnsureChildIcon(wrap.transform, "IconSprite", size);
        UIIconGraphic.Apply(img, sprite);
    }

    /// <summary>Creates a card with a colored accent strip at top, returns card transform.</summary>
    GameObject MakeCard(Transform parent, Color accent, out VerticalLayoutGroup vlg)
    {
        var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(parent, false);
        HudSkinProvider.ApplyCard(card.GetComponent<Image>(), HudCardVariant.Primary);
        CinematicTheme.ApplyPremiumMaterial(card.GetComponent<RectTransform>());

        vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(8, 8, 0, 8);
        vlg.spacing = 4;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        var strip = new GameObject("Strip", typeof(RectTransform), typeof(Image));
        strip.transform.SetParent(card.transform, false);
        strip.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.07f);
        strip.GetComponent<Image>().raycastTarget = false;
        strip.AddComponent<LayoutElement>().preferredHeight = 5f;

        return card;
    }

    // ── Button variants ───────────────────────────────────────────────────────

    void BuildIAPButton(Transform parent, string label, float width, Action onClick, Color btnColor = default, bool disabled = false)
    {
        var btn = MakeButton(parent, label, btnColor.a > 0f ? btnColor : BTN_BUY, width);
        btn.interactable = !disabled && !(PurchaseManager.Instance?.IsPurchaseInFlight ?? false);
        if (!disabled) btn.onClick.AddListener(() => onClick?.Invoke());
    }

    void BuildPackButton(Transform parent, string label, bool owned, Action onClick, float width, Color btnColor = default)
    {
        var activeColor = btnColor.a > 0f ? btnColor : BTN_BUY;
        var btn = MakeButton(parent, label, owned ? BTN_OWNED : activeColor, width);
        btn.interactable = !owned;
        if (!owned) btn.onClick.AddListener(() => onClick?.Invoke());
    }

    void BuildAdButton(Transform parent, Action onClick, Color btnColor = default)
    {
        var btn = MakeButton(parent, Loc.Get(LocKeys.StoreWatchAd), btnColor.a > 0f ? btnColor : BTN_AD, -1f);
        btn.onClick.AddListener(() => onClick?.Invoke());
    }

    Button MakeButton(Transform parent, string label, Color bgColor, float width)
    {
        var go = new GameObject("BuyBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 48f;
        le.flexibleHeight  = 0f;
        if (width > 0f) { le.preferredWidth = width; le.flexibleWidth = 0f; }
        else { le.flexibleWidth = 1f; }

        CinematicTheme.ApplyElevationButton(go.GetComponent<RectTransform>());
        go.GetComponent<Image>().color = bgColor;   // restore desired colour after elevation pass

        var lbl = RuntimeTmpText.Create(go.transform, label,
            20f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center, "Lbl");
        lbl.raycastTarget = false;
        Stretch(lbl.rectTransform);

        return go.GetComponent<Button>();
    }

    // ── Toast overlay ─────────────────────────────────────────────────────────

    void BuildToast(Transform root)
    {
        var go = new GameObject("Toast", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(root, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.85f);
        rt.anchorMax = new Vector2(0.9f, 0.92f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        go.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        _toastLabel = RuntimeTmpText.Create(go.transform, "",
            14f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center, "ToastLbl");
        Stretch(_toastLabel.rectTransform);
        _toastLabel.raycastTarget = false;

        go.SetActive(false);
    }

    void ShowToast(string msg)
    {
        if (_toastLabel == null) return;
        _toastLabel.text = msg;
        _toastLabel.gameObject.SetActive(true);
        _toastLabel.alpha = 1f;
        _toastTimer = 2.5f;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static int GetLimit(string placement) => placement switch
    {
        AdRewardSystem.PlacementBoostIncome => AdRewardSystem.LimitBoostIncome,
        AdRewardSystem.PlacementBoostXP     => AdRewardSystem.LimitBoostXP,
        AdRewardSystem.PlacementBoostRep    => AdRewardSystem.LimitBoostRep,
        _ => 1,
    };
}
