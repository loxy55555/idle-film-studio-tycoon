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
    public Color moneyColor    = new Color(0.18f, 0.80f, 0.44f);
    public Color repColor        = new Color(0.95f, 0.77f, 0.06f);
    public Color xpColor         = new Color(0.65f, 0.85f, 1f);
    public Color diamondColor    = new Color(0.55f, 0.85f, 1f);
    public Color titleColor      = Color.white;
    public Color oscarTitleColor = new Color(0.95f, 0.77f, 0.06f);
    public Color panelBgColor    = new Color(0.08f, 0.08f, 0.16f, 0.94f);

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
        if (_studio != null)    _studio.OnMovieCompleted += OnMovieCompleted;
        if (_prestige != null)  _prestige.OnOscarGained  += OnOscarGained;
        if (_contracts != null) _contracts.OnContractClaimed += OnContractClaimed;
    }

    void Unbind()
    {
        if (_studio != null)    _studio.OnMovieCompleted -= OnMovieCompleted;
        if (_prestige != null)  _prestige.OnOscarGained  -= OnOscarGained;
        if (_contracts != null) _contracts.OnContractClaimed -= OnContractClaimed;
    }

    // ─── Event handlers ───────────────────────────────────────────────────────

    void OnMovieCompleted(MovieCompletePayload p)
    {
        PremiereSequenceController.TryPresent(p);

        if (productionWidget != null)
        {
            productionWidget.DOKill();
            productionWidget.localScale = Vector3.one;
            productionWidget.DOPunchScale(Vector3.one * 0.08f, 0.35f, 8, 0.5f).SetUpdate(true);
        }
    }

    void OnContractClaimed(ContractConfig contract)
    {
        if (contract == null) return;

        var lines = new List<string>();
        var colors = new List<Color>();

        if (contract.rewardMoney > 0)
        {
            lines.Add("+$" + contract.rewardMoney.ToString("N0"));
            colors.Add(moneyColor);
        }
        if (contract.rewardReputation > 0)
        {
            lines.Add("+" + contract.rewardReputation.ToString("0") + " REP");
            colors.Add(repColor);
        }
        if (contract.rewardDiamonds > 0)
        {
            lines.Add("+" + contract.rewardDiamonds + " 💎");
            colors.Add(diamondColor);
        }
        if (contract.rewardStudioXP > 0)
        {
            lines.Add("+" + contract.rewardStudioXP + " XP");
            colors.Add(xpColor);
        }

        if (lines.Count == 0) return;

        EnqueuePanel(new PanelRequest
        {
            title      = "CONTRATO COMPLETADO",
            subtitle   = contract.contractTitle,
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
        {
            target.DOKill();
            target.DOShakeAnchorPos(0.65f, 18f, 24, 90f, false, true).SetUpdate(true);
        }

        EnqueuePanel(new PanelRequest
        {
            title      = "OSCAR CONSEGUIDO",
            subtitle   = "",
            lines      = new[] { oscars + " Oscar(s) · Mejora tu Ciudad" },
            lineColors = new[] { oscarTitleColor },
            prominent  = true,
        });
    }

    public void ShowCityUpgrade(string cityName, float globalMultiplier)
    {
        EnqueuePanel(new PanelRequest
        {
            title      = "CIUDAD MEJORADA",
            subtitle   = cityName,
            lines      = new[]
            {
                "×" + globalMultiplier.ToString("0.00") + " ingreso global",
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
            cardRoot.DOKill();
            cardRoot.localScale = Vector3.one;
            cardRoot.DOPunchScale(Vector3.one * 0.12f, 0.35f, 8, 0.5f).SetUpdate(true);

            var bg = cardRoot.GetComponent<Image>();
            if (bg != null)
            {
                Color baseColor = bg.color;
                bg.DOKill();
                bg.color = baseColor;
                bg.DOColor(Color.Lerp(baseColor, UpgradeFlash, 0.85f), 0.08f)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetUpdate(true)
                    .OnComplete(() => bg.color = baseColor);
            }
        }

        string effect = UpgradeEffectFormatter.FormatLevelEffects(cfg, newLevel);
        if (string.IsNullOrEmpty(effect))
            effect = cfg != null ? cfg.displayName : "Mejora adquirida";

        EnqueuePanel(new PanelRequest
        {
            title      = "MEJORA ADQUIRIDA",
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

        for (int i = 0; i < req.lines.Length; i++)
        {
            Color c = req.lineColors != null && i < req.lineColors.Length ? req.lineColors[i] : titleColor;
            AddPanelText(go.transform, req.lines[i], 20, c, FontStyles.Bold);
        }

        var seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(cg.DOFade(1f, 0.18f));
        seq.Join(rt.DOScale(enterScale, 0.28f).SetEase(Ease.OutBack));
        if (req.prominent)
            seq.Join(rt.DOPunchScale(Vector3.one * 0.06f, 0.45f, 6, 0.4f));

        if (req.requireDismiss)
        {
            var dismissBtn = CreateDismissButton(go.transform, () =>
            {
                cg.DOFade(0f, 0.22f).SetUpdate(true).OnComplete(() =>
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
            seq.Append(cg.DOFade(0f, 0.28f));
            seq.Join(rt.DOScale(enterScale * 0.92f, 0.28f).SetEase(Ease.InQuad));
            seq.OnComplete(() =>
            {
                Destroy(go);
                onComplete?.Invoke();
            });
        }
    }

    static Button CreateDismissButton(Transform parent, UnityEngine.Events.UnityAction onClick)
    {
        var btnGo = new GameObject("ContinueBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(parent, false);
        btnGo.GetComponent<Image>().color = new Color(0.15f, 0.68f, 0.38f);
        btnGo.AddComponent<LayoutElement>().preferredHeight = 36f;
        btnGo.AddComponent<UIButtonScale>();

        var lbl = RuntimeTmpText.Create(btnGo.transform, "CONTINUAR", 16, Color.white, FontStyles.Bold,
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
