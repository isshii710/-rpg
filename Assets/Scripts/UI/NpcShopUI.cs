using UnityEngine;

/// <summary>
/// NPC ショップ UI（武器屋・防具屋）。
/// NpcController.Interact() → OpenShop(type, name) で表示。
/// Escape キーで閉じる。
/// </summary>
public class NpcShopUI : MonoBehaviour
{
    public static NpcShopUI Instance { get; private set; }

    public bool IsOpen { get; private set; }

    NpcShopType shopType;
    string shopTitle;

    const float PW = 420f;
    const float PH = 380f;

    Texture2D texBg;
    Texture2D texBorder;
    Texture2D texBtnBuy;
    Texture2D texBtnSell;

    GUIStyle styleTitle;
    GUIStyle styleSection;
    GUIStyle styleItem;
    GUIStyle styleGold;
    GUIStyle styleBtnBuy;
    GUIStyle styleBtnSell;
    GUIStyle styleFeedback;
    GUIStyle styleClose;

    bool ready;
    string feedbackMsg = "";
    float feedbackTimer;

    // 武器屋の購入リスト
    static readonly (ItemId id, string name, int price)[] WeaponBuyList =
    {
        (ItemId.Sword,  "剣",   100),
        (ItemId.Potion, "薬草",  20),
    };

    // 防具屋の購入リスト
    static readonly (ItemId id, string name, int price)[] ArmorBuyList =
    {
        (ItemId.Armor,  "鎧",   120),
        (ItemId.Shield, "盾",    80),
        (ItemId.Potion, "薬草",  20),
    };

    // 両店共通の売却リスト（素材を換金できる）
    static readonly (ItemId id, string name, int earn)[] SellList =
    {
        (ItemId.Wood,      "木材",   4),
        (ItemId.Stone,     "石材",   6),
        (ItemId.Vegetable, "野菜",  20),
    };

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>ショップを開く。既に開いていれば閉じる（トグル）。</summary>
    public void OpenShop(NpcShopType type, string title)
    {
        if (IsOpen) { IsOpen = false; return; }
        shopType  = type;
        shopTitle = title;
        IsOpen    = true;
    }

    void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            IsOpen = false;

        if (feedbackTimer > 0f) feedbackTimer -= Time.deltaTime;
    }

    void OnGUI()
    {
        if (!ready) Init();
        if (!IsOpen) return;

        float px = (Screen.width  - PW) * 0.5f;
        float py = (Screen.height - PH) * 0.5f;

        DrawPanel(px, py, PW, PH);

        var inv  = InventoryManager.Instance;
        int gold = inv != null ? inv.Gold : 0;

        GUI.Label(new Rect(px + 14, py + 10, PW - 60, 26), $"◆ {shopTitle}", styleTitle);
        GUI.Label(new Rect(px + PW - 150, py + 10, 136, 26), $"所持金: {gold} G", styleGold);

        if (GUI.Button(new Rect(px + PW - 36, py + 8, 28, 24), "✕", styleClose))
            IsOpen = false;

        float y = py + 44;

        // ── 購入 ──
        GUI.Label(new Rect(px + 14, y, PW - 28, 22), "【 購 入 】", styleSection);
        y += 26;

        var buyList = (shopType == NpcShopType.Weapon) ? WeaponBuyList : ArmorBuyList;
        foreach (var (id, itemName, price) in buyList)
        {
            int held = inv != null ? inv.GetCount(id) : 0;
            GUI.Label(new Rect(px + 20, y + 2, 200, 20), $"{itemName}  所持: {held}", styleItem);
            GUI.Label(new Rect(px + 224, y + 2, 80, 20), $"{price} G", styleItem);

            if (GUI.Button(new Rect(px + 318, y, 72, 24), "購入", styleBtnBuy))
                TryBuy(id, itemName, price, inv);

            y += 30;
        }

        y += 8;

        // ── 売却 ──
        GUI.Label(new Rect(px + 14, y, PW - 28, 22), "【 売 却 】", styleSection);
        y += 26;

        foreach (var (id, itemName, earn) in SellList)
        {
            int held = inv != null ? inv.GetCount(id) : 0;
            GUI.Label(new Rect(px + 20, y + 2, 200, 20), $"{itemName}  所持: {held}", styleItem);
            GUI.Label(new Rect(px + 224, y + 2, 80, 20), $"+{earn} G", styleItem);

            GUI.enabled = held > 0;
            if (GUI.Button(new Rect(px + 318, y, 72, 24), "売却", styleBtnSell))
                TrySell(id, itemName, earn, inv);
            GUI.enabled = true;

            y += 30;
        }

        // ── フィードバック ──
        if (feedbackTimer > 0f)
            GUI.Label(new Rect(px + 14, py + PH - 38, PW - 28, 22), feedbackMsg, styleFeedback);

        GUI.Label(new Rect(px + 14, py + PH - 18, PW - 28, 16), "Esc: 閉じる", styleFeedback);
    }

    // ── 購入ロジック（PriceTable を使わず直接処理）──

    void TryBuy(ItemId id, string itemName, int price, InventoryManager inv)
    {
        if (inv == null) return;
        if (inv.Gold >= price)
        {
            inv.AddGold(-price);
            inv.AddItem(id);
            SetFeedback($"✓ {itemName} を購入しました！ (-{price}G)");
        }
        else
        {
            SetFeedback($"✗ ゴールドが足りません (必要 {price}G)");
        }
    }

    void TrySell(ItemId id, string itemName, int earn, InventoryManager inv)
    {
        if (inv == null) return;
        if (inv.HasItem(id))
        {
            inv.ConsumeItem(id);
            inv.AddGold(earn);
            SetFeedback($"✓ {itemName} を売却しました！ (+{earn}G)");
        }
        else
        {
            SetFeedback($"✗ {itemName} を持っていません");
        }
    }

    void SetFeedback(string msg) { feedbackMsg = msg; feedbackTimer = 2.5f; }

    // ── 描画ヘルパー ──

    void DrawPanel(float x, float y, float w, float h)
    {
        GUI.DrawTexture(new Rect(x, y, w, h), texBg);
        float b = 2f;
        GUI.DrawTexture(new Rect(x,         y,         w, b), texBorder);
        GUI.DrawTexture(new Rect(x,         y + h - b, w, b), texBorder);
        GUI.DrawTexture(new Rect(x,         y,         b, h), texBorder);
        GUI.DrawTexture(new Rect(x + w - b, y,         b, h), texBorder);
    }

    void Init()
    {
        texBg      = MakeTex(new Color(0.04f, 0.04f, 0.18f, 0.94f));
        texBorder  = MakeTex(new Color(0.80f, 0.62f, 0.10f, 1.00f));
        texBtnBuy  = MakeTex(new Color(0.10f, 0.40f, 0.12f, 1.00f));
        texBtnSell = MakeTex(new Color(0.40f, 0.10f, 0.10f, 1.00f));

        styleTitle    = Style(16, new Color(0.85f, 0.68f, 0.18f), bold: true);
        styleSection  = Style(13, new Color(0.60f, 0.90f, 0.60f), bold: true);
        styleItem     = Style(13, Color.white);
        styleGold     = Style(13, new Color(1.00f, 0.88f, 0.10f), bold: true,
                              anchor: TextAnchor.MiddleRight);
        styleFeedback = Style(12, new Color(0.88f, 0.88f, 0.45f), italic: true);

        styleBtnBuy  = BtnStyle(texBtnBuy,  new Color(0.50f, 1.00f, 0.50f));
        styleBtnSell = BtnStyle(texBtnSell, new Color(1.00f, 0.50f, 0.50f));
        styleClose   = BtnStyle(MakeTex(new Color(0.35f, 0.08f, 0.08f, 1f)),
                                new Color(1f, 0.6f, 0.6f));

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
        s.normal.background = bg;
        s.hover.background  = bg;
        s.active.background = bg;
        s.normal.textColor  = textColor;
        s.hover.textColor   = textColor;
        s.active.textColor  = textColor;
        return s;
    }
}
