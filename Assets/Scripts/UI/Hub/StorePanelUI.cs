using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Commercial store screen (Phase 8.6A layout rebuild).
/// Builds a premium-looking storefront at runtime inside the store tab:
/// featured banner, diamond packs, content packs, boosts and premium section.
/// All products are visual placeholders — no purchase logic.
/// </summary>
public class StorePanelUI : MonoBehaviour
{
    static readonly Color BG_DEEP    = new Color(0.05f, 0.06f, 0.11f);
    static readonly Color BG_CARD    = new Color(0.10f, 0.11f, 0.19f);
    static readonly Color BG_DARK    = new Color(0.04f, 0.04f, 0.08f);
    static readonly Color TEXT_PRI   = Color.white;
    static readonly Color TEXT_SEC   = new Color(0.54f, 0.54f, 0.67f);
    static readonly Color ACCENT_GOLD = new Color(0.95f, 0.77f, 0.06f);
    static readonly Color ACCENT_BLUE = new Color(0.25f, 0.55f, 0.90f);
    static readonly Color ACCENT_PURP = new Color(0.61f, 0.35f, 0.71f);
    static readonly Color ACCENT_GREEN = new Color(0.18f, 0.80f, 0.44f);
    static readonly Color BTN_BUY    = new Color(0.13f, 0.55f, 0.30f);

    bool _built;

    void Awake()  => EnsureBuilt();
    void Start()  => EnsureBuilt();
    void OnEnable() => EnsureBuilt();

    public void EnsureBuilt()
    {
        // Also rebuild if StoreRoot was somehow destroyed (e.g., scene reload)
        if (_built && transform.Find("StoreRoot") != null) return;
        _built = true;

        // Hide legacy menu content — settings now live behind the top-bar gear
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name == "StoreRoot") continue; // don't hide our own root if it exists
            child.gameObject.SetActive(false);
        }

        // Ensure this panel fills its parent
        var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        BuildStore();
    }

    void BuildStore()
    {
        // Remove stale root if it exists
        var stale = transform.Find("StoreRoot");
        if (stale != null) Object.Destroy(stale.gameObject);

        var root = new GameObject("StoreRoot", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(transform, false);
        root.GetComponent<Image>().color = BG_DEEP;
        var rootRT = root.GetComponent<RectTransform>();
        Stretch(rootRT);

        // Ensure we fill the panel — also add LayoutElement to prevent being squished
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
        vlg.padding = new RectOffset(14, 14, 14, 20);
        vlg.spacing = 14;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Header ──
        var hdr = RuntimeTmpText.Create(content.transform, "TIENDA",
            24f, TEXT_PRI, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "Header");
        hdr.raycastTarget = false;
        hdr.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;

        // ── Featured banner ──
        BuildFeaturedBanner(content.transform);

        // ── Diamantes ──
        BuildSectionHeader(content.transform, "💎 DIAMANTES");
        var dRow = BuildRow(content.transform, 168f);
        BuildProductCard(dRow, "💎", "PUÑADO",  "x25",   "0,99 €",  ACCENT_BLUE);
        BuildProductCard(dRow, "💎", "BOLSA",   "x150",  "4,99 €",  ACCENT_BLUE);
        BuildProductCard(dRow, "💎", "COFRE",   "x400",  "9,99 €",  ACCENT_BLUE);

        // ── Packs ──
        BuildSectionHeader(content.transform, "🎁 PACKS");
        var pRow = BuildRow(content.transform, 188f);
        BuildProductCard(pRow, "🎬", "PACK DIRECTOR", "Diamantes + Boost\n+ Película rara", "7,99 €", ACCENT_PURP, wide: true);
        BuildProductCard(pRow, "🌟", "PACK ESTRELLA", "Diamantes + REP\n+ Película épica",  "14,99 €", ACCENT_GOLD, wide: true);

        // ── Boosts ──
        BuildSectionHeader(content.transform, "⚡ BOOSTS");
        var bRow = BuildRow(content.transform, 168f);
        BuildProductCard(bRow, "⏩", "PRODUCCIÓN x2", "30 min", "📺 Ver anuncio", ACCENT_GREEN);
        BuildProductCard(bRow, "💵", "INGRESOS x2",   "30 min", "📺 Ver anuncio", ACCENT_GREEN);

        // ── Premium ──
        BuildSectionHeader(content.transform, "👑 PREMIUM");
        BuildPremiumBanner(content.transform);
    }

    // ── Building blocks ───────────────────────────────────────────────────────

    void BuildFeaturedBanner(Transform parent)
    {
        var banner = new GameObject("Featured", typeof(RectTransform), typeof(Image));
        banner.transform.SetParent(parent, false);
        banner.GetComponent<Image>().color = Color.Lerp(ACCENT_GOLD, BG_DARK, 0.55f);
        banner.AddComponent<LayoutElement>().preferredHeight = 110f;

        var hlg = banner.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(16, 16, 12, 12);
        hlg.spacing = 12;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;

        var icon = RuntimeTmpText.Create(banner.transform, "🚀",
            44f, Color.white, FontStyles.Normal, TextAlignmentOptions.Center, "Icon");
        icon.raycastTarget = false;
        icon.gameObject.AddComponent<LayoutElement>().preferredWidth = 64f;

        var txtCol = new GameObject("Texts", typeof(RectTransform));
        txtCol.transform.SetParent(banner.transform, false);
        txtCol.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var txtVLG = txtCol.AddComponent<VerticalLayoutGroup>();
        txtVLG.spacing = 2;
        txtVLG.childAlignment = TextAnchor.MiddleLeft;
        txtVLG.childControlWidth = txtVLG.childControlHeight = true;
        txtVLG.childForceExpandWidth = true; txtVLG.childForceExpandHeight = false;

        var t1 = RuntimeTmpText.Create(txtCol.transform, "STARTER PACK",
            18f, TEXT_PRI, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "T1");
        t1.raycastTarget = false;
        t1.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        var t2 = RuntimeTmpText.Create(txtCol.transform, "Diamantes + Boost + Sin anuncios 24h",
            12f, TEXT_PRI, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, "T2");
        t2.raycastTarget = false;
        t2.textWrappingMode = TextWrappingModes.Normal;
        t2.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;

        var t3 = RuntimeTmpText.Create(txtCol.transform, "OFERTA ÚNICA",
            10f, ACCENT_GOLD, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "T3");
        t3.raycastTarget = false;
        t3.gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;

        BuildPriceButton(banner.transform, "2,99 €", 92f);
    }

    void BuildPremiumBanner(Transform parent)
    {
        var banner = new GameObject("Premium", typeof(RectTransform), typeof(Image));
        banner.transform.SetParent(parent, false);
        banner.GetComponent<Image>().color = Color.Lerp(ACCENT_PURP, BG_DARK, 0.50f);
        banner.AddComponent<LayoutElement>().preferredHeight = 96f;

        var hlg = banner.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(16, 16, 12, 12);
        hlg.spacing = 12;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;

        var icon = RuntimeTmpText.Create(banner.transform, "🚫",
            36f, Color.white, FontStyles.Normal, TextAlignmentOptions.Center, "Icon");
        icon.raycastTarget = false;
        icon.gameObject.AddComponent<LayoutElement>().preferredWidth = 56f;

        var txtCol = new GameObject("Texts", typeof(RectTransform));
        txtCol.transform.SetParent(banner.transform, false);
        txtCol.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var txtVLG = txtCol.AddComponent<VerticalLayoutGroup>();
        txtVLG.spacing = 2;
        txtVLG.childAlignment = TextAnchor.MiddleLeft;
        txtVLG.childControlWidth = txtVLG.childControlHeight = true;
        txtVLG.childForceExpandWidth = true; txtVLG.childForceExpandHeight = false;

        var t1 = RuntimeTmpText.Create(txtCol.transform, "SIN ANUNCIOS",
            17f, TEXT_PRI, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "T1");
        t1.raycastTarget = false;
        t1.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        var t2 = RuntimeTmpText.Create(txtCol.transform, "Elimina los anuncios para siempre",
            12f, TEXT_SEC, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, "T2");
        t2.raycastTarget = false;
        t2.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        BuildPriceButton(banner.transform, "5,99 €", 92f);
    }

    void BuildSectionHeader(Transform parent, string title)
    {
        var tmp = RuntimeTmpText.Create(parent, title,
            16f, TEXT_PRI, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "Section");
        tmp.raycastTarget = false;
        tmp.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
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

    void BuildProductCard(Transform parent, string icon, string title, string detail, string price, Color accent, bool wide = false)
    {
        var card = new GameObject("Product_" + title, typeof(RectTransform), typeof(Image));
        card.transform.SetParent(parent, false);
        card.GetComponent<Image>().color = BG_CARD;

        var vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(8, 8, 0, 8);
        vlg.spacing = 4;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        // Accent header strip
        var strip = new GameObject("Strip", typeof(RectTransform), typeof(Image));
        strip.transform.SetParent(card.transform, false);
        strip.GetComponent<Image>().color = accent;
        strip.GetComponent<Image>().raycastTarget = false;
        strip.AddComponent<LayoutElement>().preferredHeight = 5f;

        var iconTmp = RuntimeTmpText.Create(card.transform, icon,
            wide ? 30f : 26f, Color.white, FontStyles.Normal, TextAlignmentOptions.Center, "Icon");
        iconTmp.raycastTarget = false;
        iconTmp.gameObject.AddComponent<LayoutElement>().preferredHeight = wide ? 38f : 34f;

        var titleTmp = RuntimeTmpText.Create(card.transform, title,
            12f, TEXT_PRI, FontStyles.Bold, TextAlignmentOptions.Center, "Title");
        titleTmp.raycastTarget = false;
        titleTmp.textWrappingMode = TextWrappingModes.Normal;
        titleTmp.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        var detailTmp = RuntimeTmpText.Create(card.transform, detail,
            11f, accent, FontStyles.Bold, TextAlignmentOptions.Center, "Detail");
        detailTmp.raycastTarget = false;
        detailTmp.textWrappingMode = TextWrappingModes.Normal;
        detailTmp.gameObject.AddComponent<LayoutElement>().preferredHeight = wide ? 34f : 20f;

        var spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(card.transform, false);
        spacer.AddComponent<LayoutElement>().flexibleHeight = 1f;

        BuildPriceButton(card.transform, price, -1f);
    }

    void BuildPriceButton(Transform parent, string price, float width)
    {
        var btnGo = new GameObject("BuyBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(parent, false);
        btnGo.GetComponent<Image>().color = BTN_BUY;
        var le = btnGo.AddComponent<LayoutElement>();
        le.preferredHeight = 38f;
        if (width > 0f) { le.preferredWidth = width; le.flexibleWidth = 0f; }

        var lbl = RuntimeTmpText.Create(btnGo.transform, price,
            13f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center, "Lbl");
        lbl.raycastTarget = false;
        Stretch(lbl.rectTransform);

        // Placeholder — products are not purchasable yet
        btnGo.GetComponent<Button>().onClick.AddListener(() =>
            Debug.Log("[Store] Producto placeholder — compra no disponible todavía"));
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
