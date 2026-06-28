using UnityEngine;

/// <summary>
/// B キーでトグルするショップ画面。
/// InventoryManager の BuyItem / SellItem を呼ぶだけ。
/// OnGUI ベースなので Canvas・パッケージ不要。
/// </summary>
public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }

    bool isOpen;

    // ── レイアウト ──
    const float PW = 420f;
    const float PH = 420f;

    // ── テクスチャ / スタイル ──
    Texture2D texBg;
    Texture2D texBorder;
    Texture2D texBtnNormal;
    Texture2D texBtnHover;
    Texture2D texBtnBuy;
    Texture2D texBtnSell;
    Texture2D texBtnClose;

    GUIStyle styleTitle;
    GUIStyle styleSection;
    GUIStyle styleItem;
    GUIStyle styleGold;
    GUIStyle styleBtnBuy;
    GUIStyle styleBtnSell;
    GUIStyle styleBtnClose;
    GUIStyle styleFeedback;

    bool ready;
    string feedbackMsg = "";
    float feedbackTimer;

    // ── 商品リスト ──
    static readonly (ItemId id, string name, int buyPrice, int sellPrice)[] Catalog =
    {
        (ItemId.VegetableSeed, "野菜の種",  10,  5),
        (ItemId.Track,         "線路",       15,  7),
        (ItemId.Wood,          "木材",        8,  4),
        (ItemId.Stone,         "石材",       12,  6),
        (ItemId.Vegetable,     "野菜",        0, 20),   // 売るだけ
    };

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
            isOpen = !isOpen;

        if (feedbackTimer > 0f)
            feedbackTimer -= Time.deltaTime;
    }

    void OnGUI()
    {
        if (!ready) Init();
        if (!isOpen) return;

        // ── ウィンドウ中央配置 ──
        float px = (Screen.width  - PW) * 0.5f;
        float py = (Screen.height - PH) * 0.5f;

        // 背景パネル
        DrawPanel(px, py, PW, PH);

        // タイトル
        GUI.Label(new Rect(px + 14, py + 10, PW - 28, 26), "◆ 商店", styleTitle);

        // 所持金
        var inv = InventoryManager.Instance;
        int gold = inv != null ? inv.Gold : 0;
        GUI.Label(new Rect(px + PW - 140, py + 10, 126, 26),
                  $"所持金: {gold} G", styleGold);

        // 閉じるボタン
        if (GUI.Button(new Rect(px + PW - 38, py + 8, 28, 24), "✕", styleBtnClose))
            isOpen = false;

        float y = py + 46;

        // ── 購入セクション ──
        GUI.Label(new Rect(px + 14, y, PW - 28, 22), "【 購 入 】", styleSection);
        y += 24;

        foreach (var (id, name, buyPrice, _) in Catalog)
        {
            if (buyPrice <= 0) continue;
            int held = inv != null ? inv.GetCount(id) : 0;
            GUI.Label(new Rect(px + 20, y + 2, 200, 20),
                      $"{name}  所持: {held}", styleItem);
            GUI.Label(new Rect(px + 224, y + 2, 80, 20),
                      $"{buyPrice} G", styleItem);

            if (GUI.Button(new Rect(px + 318, y, 72, 24), "購入", styleBtnBuy))
                TryBuy(id, name, buyPrice);

            y += 32;
        }

        y += 8;

        // ── 売却セクション ──
        GUI.Label(new Rect(px + 14, y, PW - 28, 22), "【 売 却 】", styleSection);
        y += 24;

        foreach (var (id, name, _, sellPrice) in Catalog)
        {
            if (sellPrice <= 0) continue;
            int held = inv != null ? inv.GetCount(id) : 0;
            GUI.Label(new Rect(px + 20, y + 2, 200, 20),
                      $"{name}  所持: {held}", styleItem);
            GUI.Label(new Rect(px + 224, y + 2, 80, 20),
                      $"+{sellPrice} G", styleItem);

            bool canSell = held > 0;
            GUI.enabled = canSell;
            if (GUI.Button(new Rect(px + 318, y, 72, 24), "売却", styleBtnSell))
                TrySell(id, name, sellPrice);
            GUI.enabled = true;

            y += 32;
        }

        // フィードバックメッセージ
        if (feedbackTimer > 0f)
            GUI.Label(new Rect(px + 14, py + PH - 34, PW - 28, 26),
                      feedbackMsg, styleFeedback);
    }

    void TryBuy(ItemId id, string name, int price)
    {
        var inv = InventoryManager.Instance;
        if (inv == null) return;
        if (inv.BuyItem(id))
            ShowFeedback($"✓ {name} を購入しました！ (-{price}G)");
        else
            ShowFeedback($"✗ ゴールドが足りません (必要 {price}G)");
    }

    void TrySell(ItemId id, string name, int price)
    {
        var inv = InventoryManager.Instance;
        if (inv == null) return;
        if (inv.SellItem(id))
            ShowFeedback($"✓ {name} を売却しました！ (+{price}G)");
        else
            ShowFeedback($"✗ {name} を持っていません");
    }

    void ShowFeedback(string msg)
    {
        feedbackMsg   = msg;
        feedbackTimer = 2.5f;
    }

    // ── 描画ヘルパー ──

    void DrawPanel(float x, float y, float w, float h)
    {
        GUI.DrawTexture(new Rect(x, y, w, h), texBg);
        float b = 2;
        GUI.DrawTexture(new Rect(x,         y,         w, b), texBorder);
        GUI.DrawTexture(new Rect(x,         y + h - b, w, b), texBorder);
        GUI.DrawTexture(new Rect(x,         y,         b, h), texBorder);
        GUI.DrawTexture(new Rect(x + w - b, y,         b, h), texBorder);
    }

    // ── 初期化（OnGUI 内で一度だけ） ──

    void Init()
    {
        texBg      = MakeTex(new Color(0.04f, 0.04f, 0.18f, 0.94f));
        texBorder  = MakeTex(new Color(0.80f, 0.62f, 0.10f, 1.00f));
        texBtnBuy  = MakeTex(new Color(0.10f, 0.40f, 0.12f, 1.00f));
        texBtnSell = MakeTex(new Color(0.40f, 0.10f, 0.10f, 1.00f));
        texBtnClose= MakeTex(new Color(0.30f, 0.08f, 0.08f, 1.00f));

        styleTitle   = Style(16, new Color(0.85f, 0.68f, 0.18f), bold: true);
        styleSection = Style(14, new Color(0.60f, 0.90f, 0.60f), bold: true);
        styleItem    = Style(14, Color.white);
        styleGold    = Style(14, new Color(1.00f, 0.88f, 0.10f), bold: true,
                             anchor: TextAnchor.MiddleRight);
        styleFeedback= Style(14, new Color(0.90f, 0.90f, 0.50f), italic: true);

        styleBtnBuy   = BtnStyle(texBtnBuy,  new Color(0.50f, 1.00f, 0.50f));
        styleBtnSell  = BtnStyle(texBtnSell, new Color(1.00f, 0.50f, 0.50f));
        styleBtnClose = BtnStyle(texBtnClose,new Color(1.00f, 0.60f, 0.60f));

        ready = true;
    }

    static Texture2D MakeTex(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    static GUIStyle Style(int size, Color color,
        TextAnchor anchor = TextAnchor.MiddleLeft,
        bool bold = false, bool italic = false)
    {
        var s = new GUIStyle(GUI.skin.label)
        {
            fontSize  = size,
            alignment = anchor,
            fontStyle = bold && italic ? FontStyle.BoldAndItalic
                      : bold          ? FontStyle.Bold
                      : italic        ? FontStyle.Italic
                      :                 FontStyle.Normal,
        };
        s.normal.textColor = color;
        return s;
    }

    static GUIStyle BtnStyle(Texture2D bg, Color textColor)
    {
        var s = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
        s.normal.background   = bg;
        s.hover.background    = bg;
        s.active.background   = bg;
        s.normal.textColor    = textColor;
        s.hover.textColor     = textColor;
        s.active.textColor    = textColor;
        return s;
    }
}
