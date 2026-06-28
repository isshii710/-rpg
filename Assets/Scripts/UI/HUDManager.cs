using UnityEngine;

/// <summary>
/// RPG 風コーナーHUD（PC横画面対応）。
/// 左上：クエスト / 右上：ゴールド / 左下：パーティHP / 下中央：選択アイテム
/// </summary>
public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    Texture2D texBg;
    Texture2D texBorder;
    Texture2D texHPFull;
    Texture2D texHPLow;
    Texture2D texHPBg;

    GUIStyle styleTitle;
    GUIStyle styleQuest;
    GUIStyle styleQuestDone;
    GUIStyle styleGold;
    GUIStyle styleGoldNum;
    GUIStyle styleLeave;
    GUIStyle styleHPName;
    GUIStyle styleItem;
    bool ready;

    void Awake() => Instance = this;

    void OnGUI()
    {
        if (!ready) Init();

        DrawQuestPanel();
        DrawGoldPanel();
        DrawPartyPanel();
        DrawItemBar();
    }

    // ── 左上：クエストパネル ────────────────────────────────────
    void DrawQuestPanel()
    {
        var sm = StoryManager.Instance;
        if (sm == null) return;

        float pw = 320, ph = 148;
        float px = 10, py = 10;

        DrawPanel(px, py, pw, ph);

        GUI.Label(new Rect(px + 10, py + 6, pw - 20, 22), "◆ 修行クエスト", styleTitle);

        float y = py + 30;
        y = QuestLine(px + 10, y, pw - 20, sm.HarvestQuest);
        y = QuestLine(px + 10, y, pw - 20, sm.MiningQuest);
        y = QuestLine(px + 10, y, pw - 20, sm.CombatQuest);

        string ls = sm.CanLeaveVillage ? "★ 旅立ち：解放済み！" : "旅立ち：修行中...";
        GUI.Label(new Rect(px + 10, y + 2, pw - 20, 22), ls, styleLeave);
    }

    // ── 右上：ゴールドパネル ────────────────────────────────────
    void DrawGoldPanel()
    {
        var inv = InventoryManager.Instance;
        int gold = inv != null ? inv.Gold : 0;

        float pw = 160, ph = 72;
        float px = Screen.width - pw - 10;
        float py = 10;

        DrawPanel(px, py, pw, ph);
        GUI.Label(new Rect(px + 10, py + 6,  pw - 20, 20), "GOLD", styleTitle);
        GUI.Label(new Rect(px + 10, py + 28, pw - 20, 36), $"{gold} G", styleGoldNum);
    }

    // ── 左下：パーティ HP パネル ────────────────────────────────
    void DrawPartyPanel()
    {
        var pm = PartyManager.Instance;
        if (pm == null) return;

        float pw = 260, ph = 10 + pm.Members.Count * 36 + 10;
        float px = 10;
        float py = Screen.height - ph - 50; // アイテムバー分を上にずらす

        DrawPanel(px, py, pw, ph);
        GUI.Label(new Rect(px + 10, py + 6, pw - 20, 20), "◆ パーティ", styleTitle);

        float y = py + 28;
        foreach (var m in pm.Members)
        {
            // 名前
            GUI.Label(new Rect(px + 8, y + 2, 72, 20), m.characterName, styleHPName);

            // HP バー背景
            float barX = px + 84;
            float barW = pw - 84 - 60;
            float barY = y + 8;
            float barH = 12;
            DrawTex(barX, barY, barW, barH, texHPBg);

            float ratio = m.maxHp > 0 ? (float)m.currentHp / m.maxHp : 0f;
            DrawTex(barX, barY, barW * ratio, barH, ratio > 0.3f ? texHPFull : texHPLow);

            // HP 数値
            GUI.Label(new Rect(barX + barW + 6, y + 2, 52, 20),
                      $"{m.currentHp}/{m.maxHp}", styleHPName);

            y += 36;
        }
    }

    // ── 下中央：選択アイテムバー ────────────────────────────────
    void DrawItemBar()
    {
        var gp = GridPlacer.Instance;
        string label = gp != null ? gp.SelectedItemLabel : "";
        if (string.IsNullOrEmpty(label)) return;

        float pw = 280, ph = 42;
        float px = (Screen.width - pw) * 0.5f;
        float py = Screen.height - ph - 8;

        DrawPanel(px, py, pw, ph);
        GUI.Label(new Rect(px, py, pw, ph), $"▶  {label}  ◀", styleItem);
    }

    // ================================================================
    //  共通ヘルパー
    // ================================================================

    float QuestLine(float x, float y, float w, QuestStep q)
    {
        if (q == null) return y;
        bool done = q.IsComplete;
        GUI.Label(new Rect(x, y, w, 24),
                  $"{(done ? "✦" : "◇")} {q.title}  {q.current}/{q.required}",
                  done ? styleQuestDone : styleQuest);
        return y + 26;
    }

    void DrawPanel(float x, float y, float w, float h)
    {
        // 背景
        DrawTex(x, y, w, h, texBg);
        // 4辺ボーダー
        float b = 2;
        DrawTex(x, y, w, b, texBorder);          // 上
        DrawTex(x, y + h - b, w, b, texBorder);  // 下
        DrawTex(x, y, b, h, texBorder);           // 左
        DrawTex(x + w - b, y, b, h, texBorder);  // 右
    }

    static void DrawTex(float x, float y, float w, float h, Texture2D tex)
        => GUI.DrawTexture(new Rect(x, y, w, h), tex);

    // ================================================================
    //  初期化（OnGUI 内で初回のみ）
    // ================================================================

    void Init()
    {
        texBg     = MakeTex(new Color(0.04f, 0.04f, 0.18f, 0.88f));
        texBorder = MakeTex(new Color(0.80f, 0.62f, 0.10f, 1.00f));
        texHPFull = MakeTex(new Color(0.20f, 0.82f, 0.25f, 1.00f));
        texHPLow  = MakeTex(new Color(0.85f, 0.18f, 0.10f, 1.00f));
        texHPBg   = MakeTex(new Color(0.08f, 0.08f, 0.08f, 0.90f));

        styleTitle    = Style(14, new Color(0.85f, 0.68f, 0.18f), bold: true);
        styleQuest    = Style(14, new Color(0.88f, 0.88f, 0.88f));
        styleQuestDone = Style(14, new Color(0.55f, 0.95f, 0.55f), bold: true);
        styleGoldNum  = Style(26, new Color(1.00f, 0.88f, 0.10f), bold: true);
        styleLeave    = Style(13, new Color(0.55f, 0.90f, 0.55f), italic: true);
        styleHPName   = Style(13, Color.white);
        styleItem     = Style(20, new Color(1.00f, 0.95f, 0.45f),
                              anchor: TextAnchor.MiddleCenter, bold: true);

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
}
