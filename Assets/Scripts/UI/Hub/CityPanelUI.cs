using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-155)]
public class CityPanelUI : MonoBehaviour
{
    static readonly Color BG_DEEP   = new Color(0.04f, 0.04f, 0.10f);
    static readonly Color BG_CARD  = new Color(0.10f, 0.10f, 0.19f);
    static readonly Color TEXT_PRI = Color.white;
    static readonly Color TEXT_DIM = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color ACCENT_GOLD = new Color(0.95f, 0.77f, 0.06f);
    static readonly Color ACCENT_GREEN = new Color(0.18f, 0.80f, 0.44f);
    static readonly Color BTN_GREEN = new Color(0.15f, 0.68f, 0.38f);

    Image               _background;
    TextMeshProUGUI     _cityNameText;
    TextMeshProUGUI     _oscarText;
    TextMeshProUGUI     _bonusText;
    TextMeshProUGUI     _unlockSummaryText;
    TextMeshProUGUI     _nextPreviewText;
    Slider              _progressSlider;
    SmoothProgressBar   _smoothBar;
    Button              _upgradeButton;
    RectTransform       _heroPanel;

    CitySystem     _city;
    PrestigeSystem _prestige;
    bool           _celebratePending;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (!Application.isPlaying) return;

        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name != "PremiosCiudadSection") continue;
            if (t.GetComponent<CityPanelUI>() != null) return;
            t.gameObject.AddComponent<CityPanelUI>();
            return;
        }

        if (Object.FindAnyObjectByType<DefinitiveHudShell>() != null) return;

        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name != "CiudadPanel") continue;
            if (t.GetComponent<CityPanelUI>() != null) return;
            t.gameObject.AddComponent<CityPanelUI>();
            return;
        }
    }

    void Awake()
    {
        HidePlaceholderContent();
        BuildLayout();
        GameHub.OnGameReady += Bind;
    }

    void OnDestroy()
    {
        GameHub.OnGameReady -= Bind;
        Unbind();
    }

    void OnEnable() => Refresh();

    void Bind()
    {
        Unbind();
        _city     = GameHub.Instance?.city;
        _prestige = GameHub.Instance?.prestige;
        if (_prestige != null) _prestige.OnOscarGained += Refresh;
        if (_city != null)     _city.OnCityLevelChanged += OnCityLevelChanged;
        Refresh();
    }

    void Unbind()
    {
        if (_prestige != null) _prestige.OnOscarGained -= Refresh;
        if (_city != null)     _city.OnCityLevelChanged -= OnCityLevelChanged;
    }

    void OnCityLevelChanged(int level)
    {
        Refresh();
        if (!_celebratePending) return;
        _celebratePending = false;
        var def = _city?.CurrentDefinition;
        if (def == null) return;
        GameFeelUI.Instance?.ShowCityUpgrade(def.displayName, def.globalMultiplier);
        if (_heroPanel != null)
        {
            _heroPanel.DOKill();
            _heroPanel.localScale = Vector3.one;
            _heroPanel.DOPunchScale(Vector3.one * 0.08f, 0.45f, 8, 0.5f).SetUpdate(true);
        }
    }

    void HidePlaceholderContent()
    {
        foreach (Transform child in transform)
            child.gameObject.SetActive(false);
    }

    void BuildLayout()
    {
        var root = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        Stretch(root);

        var bg = new GameObject("CityBg", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(transform, false);
        Stretch(bg.GetComponent<RectTransform>());
        _background = bg.GetComponent<Image>();
        _background.color = BG_DEEP;

        var scrollGo = new GameObject("CityScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollGo.transform.SetParent(transform, false);
        Stretch(scrollGo.GetComponent<RectTransform>());
        scrollGo.GetComponent<Image>().color = Color.clear;

        var scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scrollGo.transform, false);
        Stretch(viewport.GetComponent<RectTransform>());
        scroll.viewport = viewport.GetComponent<RectTransform>();

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.offsetMin = contentRT.offsetMax = Vector2.zero;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(16, 16, 16, 16);
        vlg.spacing = 12;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRT;

        _heroPanel = MakeCard(content.transform, "HeroPanel", 160);
        _heroPanel.GetComponent<Image>().color = BG_CARD;

        _cityNameText = RuntimeTmpText.Create(_heroPanel, "GARAJE", 28, TEXT_PRI, FontStyles.Bold,
            TextAlignmentOptions.Center, "CityName");
        Stretch(_cityNameText.rectTransform);

        var oscCard = MakeCard(content.transform, "OscarCard", 0);
        AddCardHeader(oscCard, "PROGRESO DE CIUDAD");
        _oscarText = AddCardBody(oscCard, "0 / 1 OSC", 18, ACCENT_GOLD);

        var barGo = new GameObject("ProgressBar", typeof(RectTransform), typeof(Image), typeof(Slider));
        barGo.transform.SetParent(oscCard, false);
        barGo.GetComponent<Image>().color = new Color(0.09f, 0.09f, 0.18f);
        barGo.AddComponent<LayoutElement>().preferredHeight = 14;
        _progressSlider = barGo.GetComponent<Slider>();
        SetupSlider(_progressSlider, ACCENT_GOLD);
        ReadOnlySlider.Configure(_progressSlider);
        _smoothBar = barGo.AddComponent<SmoothProgressBar>();

        var bonusCard = MakeCard(content.transform, "BonusCard", 0);
        AddCardHeader(bonusCard, "BONUS GLOBAL");
        _bonusText = AddCardBody(bonusCard, "×1.00 ingreso", 22, ACCENT_GREEN, FontStyles.Bold);

        var unlockCard = MakeCard(content.transform, "UnlockCard", 0);
        AddCardHeader(unlockCard, "DESBLOQUEOS ACTUALES");
        _unlockSummaryText = AddCardBody(unlockCard, "—", 16, TEXT_PRI);
        _unlockSummaryText.textWrappingMode = TextWrappingModes.Normal;
        _unlockSummaryText.gameObject.AddComponent<LayoutElement>().preferredHeight = 72;

        var nextCard = MakeCard(content.transform, "NextCard", 0);
        AddCardHeader(nextCard, "PRÓXIMO NIVEL");
        _nextPreviewText = AddCardBody(nextCard, "—", 16, TEXT_DIM);
        _nextPreviewText.textWrappingMode = TextWrappingModes.Normal;
        _nextPreviewText.gameObject.AddComponent<LayoutElement>().preferredHeight = 72;

        var btnGo = new GameObject("UpgradeBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(content.transform, false);
        HudSkinProvider.ApplyButton(btnGo.GetComponent<Image>(), HudButtonVariant.Success);
        btnGo.AddComponent<LayoutElement>().preferredHeight = 48;
        btnGo.AddComponent<UIButtonScale>();
        _upgradeButton = btnGo.GetComponent<Button>();
        _upgradeButton.onClick.AddListener(OnUpgradeClicked);

        var btnLabel = RuntimeTmpText.Create(btnGo.transform, "MEJORAR ESTUDIO", 18, TEXT_PRI, FontStyles.Bold,
            TextAlignmentOptions.Center);
        Stretch(btnLabel.rectTransform);
    }

    void Refresh()
    {
        if (_city == null && GameHub.Instance != null)
            Bind();
        if (_city == null) return;

        var current = _city.CurrentDefinition;
        var next    = _city.GetNextDefinition();

        if (_background != null && ColorUtility.TryParseHtmlString(current.backgroundColorHex, out Color bg))
            _background.color = bg;

        if (_heroPanel != null && ColorUtility.TryParseHtmlString(current.backgroundColorHex, out Color hero))
            _heroPanel.GetComponent<Image>().color = Color.Lerp(hero, Color.white, 0.08f);

        if (_cityNameText != null) _cityNameText.text = current.displayName.ToUpper();
        if (_oscarText != null)    _oscarText.text = _city.GetProgressLabel();
        if (_bonusText != null)    _bonusText.text = $"×{current.globalMultiplier:0.00} ingreso global";

        if (_unlockSummaryText != null)
            _unlockSummaryText.text = string.Join("\n", _city.GetUnlockSummaryLines());

        if (_nextPreviewText != null)
        {
            _nextPreviewText.text = _city.IsMaxLevel
                ? "Has alcanzado el Imperio Cinematográfico."
                : next != null
                    ? $"{next.displayName}\n{string.Join("\n", _city.GetNextUnlockPreviewLines())}"
                    : "—";
        }

        if (_smoothBar != null)
            _smoothBar.SetNormalized(_city.GetProgressToNextLevel());
        else if (_progressSlider != null)
            _progressSlider.value = _city.GetProgressToNextLevel();

        if (_upgradeButton != null)
        {
            bool can = _city.CanUpgrade();
            _upgradeButton.gameObject.SetActive(!_city.IsMaxLevel);
            _upgradeButton.interactable = can;
            var img = _upgradeButton.GetComponent<Image>();
            if (img != null)
                HudSkinProvider.ApplyButtonState(img, can ? HudButtonState.Ready : HudButtonState.Disabled);
        }
    }

    void OnUpgradeClicked()
    {
        if (_city == null || !_city.CanUpgrade()) return;
        _celebratePending = true;
        _city.TryUpgrade();
    }

    static RectTransform MakeCard(Transform parent, string name, float height)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        HudSkinProvider.ApplyCard(go.GetComponent<Image>(), HudCardVariant.Primary);
        var le = go.AddComponent<LayoutElement>();
        if (height > 0) le.preferredHeight = height;
        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 12, 12);
        vlg.spacing = 6;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        return go.GetComponent<RectTransform>();
    }

    static TextMeshProUGUI AddCardHeader(RectTransform card, string text)
    {
        var tmp = RuntimeTmpText.Create(card, text, 13, TEXT_DIM, FontStyles.Bold);
        tmp.gameObject.AddComponent<LayoutElement>().preferredHeight = 18;
        return tmp;
    }

    static TextMeshProUGUI AddCardBody(RectTransform card, string text, float size, Color color,
        FontStyles style = FontStyles.Normal)
    {
        var tmp = RuntimeTmpText.Create(card, text, size, color, style);
        tmp.gameObject.AddComponent<LayoutElement>().preferredHeight = size + 8;
        return tmp;
    }

    static void SetupSlider(Slider slider, Color fillColor)
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(slider.transform, false);
        Stretch(fillArea.GetComponent<RectTransform>());
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        fill.GetComponent<Image>().color = fillColor;
        Stretch(fill.GetComponent<RectTransform>());
        slider.fillRect = fill.GetComponent<RectTransform>();
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
