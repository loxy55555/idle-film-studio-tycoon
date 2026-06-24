using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reward panels and DOTween feedback for core game actions.
/// </summary>
public class GameFeelUI : MonoBehaviour
{
    public static GameFeelUI Instance { get; private set; }

    [Header("Targets")]
    public RectTransform popupParent;
    public RectTransform productionWidget;
    public RectTransform oscarTarget;

    [Header("Colors")]
    public Color moneyColor    = CinematicTheme.GoldBase;
    public Color repColor      = CinematicTheme.GoldBright;
    public Color xpColor       = new Color(0.65f, 0.85f, 1f);
    public Color diamondColor  = new Color(0.55f, 0.85f, 1f);
    public Color titleColor    = CinematicTheme.TextPrimary;
    public Color oscarTitleColor = CinematicTheme.GoldBright;
    public Color panelBgColor  = CinematicTheme.CardBg;

    static readonly Color UpgradeFlash = new Color(1f, 1f, 1f, 0.35f);

    struct PanelRequest
    {
        public string   title;
        public string   subtitle;
        public string[] lines;
        public Color[]  lineColors;
        public bool     prominent;
        public bool     requireDismiss;
        public Sprite   posterSprite;
        public string   posterColorHex;
    }

    readonly Queue<PanelRequest> _panelQueue = new();
    bool _panelShowing;

    StudioManager  _studio;
    PrestigeSystem _prestige;
    ContractSystem _contracts;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        if (popupParent == null)
            popupParent = GetComponent<RectTransform>();
    }

    void Start()
    {
        GameHub.OnGameReady += Bind;
        if (GameHub.Instance != null) Bind();
    }

    void OnDestroy()
    {
        GameHub.OnGameReady -= Bind;
        Unbind();
    }

    void Bind()
    {
        Unbind();
        _studio    = GameHub.Instance?.studio;
        _prestige  = GameHub.Instance?.prestige;
        _contracts = GameHub.Instance?.contracts;
        if (_prestige != null)  _prestige.OnOscarGained  += OnOscarGained;
        if (_contracts != null) _contracts.OnContractClaimed += OnContractClaimed;
    }

    void Unbind()
    {
        if (_prestige != null)  _prestige.OnOscarGained  -= OnOscarGained;
        if (_contracts != null) _contracts.OnContractClaimed -= OnContractClaimed;
    }

    // ─── Event handlers ───────────────────────────────────────────────────────

    void OnContractClaimed(ContractConfig contract)
    {
        if (contract == null) return;

        var lines = new List<string>();
        var colors = new List<Color>();

        if (contract.rewardMoney > 0)
        {
            lines.Add(Loc.Format(LocKeys.ContractRewardMoney, AnimatedMoneyText.FormatMoney(contract.rewardMoney)));
            colors.Add(moneyColor);
        }
        if (contract.rewardReputation > 0)
        {
            lines.Add(Loc.Format(LocKeys.ContractRewardRep, contract.rewardReputation));
            colors.Add(repColor);
        }
        if (contract.rewardDiamonds > 0)
        {
            lines.Add(Loc.Format(LocKeys.ContractRewardDiam, contract.rewardDiamonds));
            colors.Add(diamondColor);
        }
        if (contract.rewardStudioXP > 0)
        {
            lines.Add(Loc.Format(LocKeys.ContractRewardXP, contract.rewardStudioXP));
            colors.Add(xpColor);
        }

        if (lines.Count == 0) return;

        EnqueuePanel(new PanelRequest
        {
            title      = Loc.Get(LocKeys.GameFeelContractComplete),
            subtitle   = ContractSystem.GetLocalizedTitle(contract),
            lines      = lines.ToArray(),
            lineColors = colors.ToArray(),
            prominent  = false,
        });
    }

    void OnOscarGained()
    {
        int oscars = _prestige != null ? _prestige.oscars : 0;
        var target = oscarTarget != null ? oscarTarget : popupParent;
        if (target != null)
            UIAnimationService.PlayStarEarned(target, null);

        EnqueuePanel(new PanelRequest
        {
            title      = Loc.Get(LocKeys.GameFeelOscarEarned),
            subtitle   = "",
            lines      = new[] { oscars + Loc.Get(LocKeys.GameFeelOscarsSuffix) },
            lineColors = new[] { oscarTitleColor },
            prominent  = true,
        });
    }

    public void ShowCityUpgrade(string cityName, float globalMultiplier)
    {
        EnqueuePanel(new PanelRequest
        {
            title      = Loc.Get(LocKeys.GameFeelCityImproved),
            subtitle   = cityName,
            lines      = new[]
            {
                "×" + globalMultiplier.ToString("0.00") + Loc.Get(LocKeys.GameFeelIncomeGlobal),
            },
            lineColors = new[] { oscarTitleColor },
            prominent  = true,
        });
    }

    public void ShowCityContentUnlock(string title, string[] lines)
    {
        if (lines == null || lines.Length == 0) return;

        var colors = new Color[lines.Length];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = repColor;

        EnqueuePanel(new PanelRequest
        {
            title      = title,
            subtitle   = CityProgressionRules.GetCityDisplayName(GameHub.Instance?.city?.Level ?? 1),
            lines      = lines,
            lineColors = colors,
            prominent  = true,
        });
    }

    /// <summary>Called from UpgradeCardUI after a successful purchase.</summary>
    public void ShowUpgradePurchase(RectTransform cardRoot, UpgradeConfig cfg, int newLevel)
    {
        if (cardRoot != null)
        {
            var bg = cardRoot.GetComponent<Image>();
            var levelTxt = cardRoot.GetComponentInChildren<TextMeshProUGUI>();
            UIAnimationService.PlayUpgradeFeedback(cardRoot, bg, levelTxt);
        }

        string effect = UpgradeEffectFormatter.FormatLevelEffects(cfg, newLevel);
        if (string.IsNullOrEmpty(effect))
            effect = cfg != null ? cfg.displayName : Loc.Get(LocKeys.GameFeelUpgradeFallback);

        EnqueuePanel(new PanelRequest
        {
            title      = Loc.Get(LocKeys.GameFeelUpgradeBought),
            subtitle   = cfg != null ? cfg.displayName : "",
            lines      = new[] { effect },
            lineColors = new[] { moneyColor },
            prominent  = false,
        });
    }

    // ─── Panel queue ──────────────────────────────────────────────────────────

    void EnqueuePanel(PanelRequest request)
    {
        _panelQueue.Enqueue(request);
        if (!_panelShowing)
            ShowNextPanel();
    }

    void ShowNextPanel()
    {
        if (_panelQueue.Count == 0)
        {
            _panelShowing = false;
            return;
        }

        if (popupParent == null)
        {
            _panelQueue.Clear();
            _panelShowing = false;
            return;
        }

        _panelShowing = true;
        BuildPanel(_panelQueue.Dequeue(), ShowNextPanel);
    }

    void BuildPanel(PanelRequest req, TweenCallback onComplete)
    {
        float holdDuration = req.requireDismiss ? 0f : (req.prominent ? 2.1f : 1.25f);
        float enterScale   = req.prominent ? 1.08f : 1f;

        var go = new GameObject("RewardPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        go.transform.SetParent(popupParent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.58f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = req.requireDismiss ? new Vector2(540f, 320f) : (req.prominent ? new Vector2(520f, 220f) : new Vector2(460f, 190f));

        var bg = go.GetComponent<Image>();
        bg.color = panelBgColor;
        CinematicTheme.ApplyElevationPopup(rt);

        var cg = go.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        rt.localScale = Vector3.one * 0.85f;

        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 16, 16);
        vlg.spacing = 6;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        AddPanelText(go.transform, req.title, req.prominent ? 26 : 22, req.prominent ? oscarTitleColor : titleColor, FontStyles.Bold);
        if (!string.IsNullOrEmpty(req.subtitle))
            AddPanelText(go.transform, req.subtitle, 18, repColor, FontStyles.Normal);

        if (req.posterSprite != null)
            AddPanelPoster(go.transform, req.posterSprite, null);
        else if (req.requireDismiss && !string.IsNullOrEmpty(req.posterColorHex))
            AddPanelPoster(go.transform, null, req.posterColorHex);

        if (req.lines != null)
        {
            for (int i = 0; i < req.lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(req.lines[i])) continue;
                Color c = req.lineColors != null && i < req.lineColors.Length ? req.lineColors[i] : titleColor;
                AddPanelText(go.transform, req.lines[i], 20, c, FontStyles.Bold);
            }
        }

        var seq = UIAnimationService.PlayRewardPopup(rt, cg, req.prominent);

        if (req.requireDismiss)
        {
            var dismissBtn = CreateDismissButton(go.transform, () =>
            {
                UIAnimationService.PlayPopupClose(rt, cg, () =>
                {
                    Destroy(go);
                    onComplete?.Invoke();
                });
            });
            dismissBtn.interactable = false;
            seq.AppendCallback(() => dismissBtn.interactable = true);
        }
        else
        {
            seq.AppendInterval(holdDuration);
            seq.AppendCallback(() =>
            {
                UIAnimationService.PlayRewardPopupOut(rt, cg, enterScale, () =>
                {
                    Destroy(go);
                    onComplete?.Invoke();
                });
            });
        }
    }

    static Button CreateDismissButton(Transform parent, UnityEngine.Events.UnityAction onClick)
    {
        var btnGo = new GameObject("ContinueBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(parent, false);
        HudSkinProvider.ApplyButton(btnGo.GetComponent<Image>(), HudButtonVariant.Success);
        btnGo.AddComponent<LayoutElement>().preferredHeight = 36f;
        btnGo.AddComponent<UIButtonScale>();

        var lbl = RuntimeTmpText.Create(btnGo.transform, Loc.Get(LocKeys.GameFeelContinue), 16, Color.white, FontStyles.Bold,
            TextAlignmentOptions.Center);
        StretchRect(lbl.rectTransform);

        var btn = btnGo.GetComponent<Button>();
        btn.onClick.AddListener(onClick);
        return btn;
    }

    static void AddPanelPoster(Transform parent, Sprite sprite, string colorHex)
    {
        var row = new GameObject("Poster", typeof(RectTransform), typeof(Image));
        row.transform.SetParent(parent, false);
        row.AddComponent<LayoutElement>().preferredHeight = 96f;
        var img = row.GetComponent<Image>();
        if (sprite != null)
        {
            img.sprite = sprite;
            img.color = Color.white;
            img.preserveAspect = true;
        }
        else if (!string.IsNullOrEmpty(colorHex) && ColorUtility.TryParseHtmlString(colorHex, out Color c))
            img.color = c;
    }

    static void StretchRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void AddPanelText(Transform parent, string text, float size, Color color, FontStyles style)
    {
        var row = new GameObject("Line", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        row.AddComponent<LayoutElement>().preferredHeight = size + 10;
        var tmp = row.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
    }
}
