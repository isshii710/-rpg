using UnityEngine;

/// <summary>
/// RPG 風ステータスパネル HUD（OnGUI 方式・パッケージ不要）。
/// 画面下部にパネルを描画する縦画面レイアウト。
/// </summary>
public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    // ---- テクスチャ ----
    Texture2D texPanelBg;   // 濃紺半透明
    Texture2D texBorder;    // ゴールドボーダー
    Texture2D texDivider;   // 区切り線
    Texture2D texHPFull;    // HP バー（緑）
    Texture2D texHPLow;     // HP バー（赤）
    Texture2D texHPBg;      // HP バー背景（暗）

    // ---- スタイル ----
    GUIStyle styleQuest;
    GUIStyle styleQuestDone;
    GUIStyle styleGold;
    GUIStyle styleLeave;
    GUIStyle styleHPName;
    GUIStyle styleItem;
    GUIStyle styleSectionTitle;
    bool ready;

    void Awake() => Instance = this;

    void OnGUI()
    {
        if (!ready) Init();

        float sw = Screen.width;
        float sh = Screen.height;

        // パネルの高さ：画面の 42%（縦画面でも横画面でも適切）
        float panelH = sh * 0.42f;
        float panelY = sh - panelH;

        // ── 背景パネル ──────────────────────────────────────────
        DrawTex(0, panelY, sw, panelH, texPanelBg);
        DrawTex(0, panelY, sw, 4,      texBorder);   // 上ボーダー（金）

        // ── 上段：クエスト（左）＋ ゴールド（右） ───────────────
        float padX = sw * 0.04f;
        float topY = panelY + 12;
        float questW = sw * 0.6f;
        float goldX  = sw * 0.64f;

        // セクションタイトル
        GUI.Label(new Rect(padX, topY, 200, 24), "◆ 修行クエスト", styleSectionTitle);
        topY += 26;

        var sm = StoryManager.Instance;
        if (sm != null)
        {
            topY = DrawQuestLine(padX, topY, questW, sm.HarvestQuest, topY);
            topY = DrawQuestLine(padX, topY, questW, sm.MiningQuest,  topY);
            topY = DrawQuestLine(padX, topY, questW, sm.CombatQuest,  topY);

            string leaveStr = sm.CanLeaveVillage ? "★ 旅立ち解放！" : "旅立ち：修行中...";
            GUI.Label(new Rect(padX, topY, questW, 26), leaveStr, styleLeave);
        }

        // ゴールド
        var inv = InventoryManager.Instance;
        int gold = inv != null ? inv.Gold : 0;
        GUI.Label(new Rect(goldX, panelY + 12, sw - goldX - padX, 28), "GOLD", styleSectionTitle);
        GUI.Label(new Rect(goldX, panelY + 36, sw - goldX - padX, 36), $"{gold} G", styleGold);

        // ── 区切り線 ─────────────────────────────────────────────
        float divY = panelY + panelH * 0.43f;
        DrawTex(padX * 0.5f, divY, sw - padX, 2, texDivider);

        // ── 下段：パーティ HP ────────────────────────────────────
        float hpStartY = divY + 8;
        float hpAreaH  = panelH - (divY - panelY) - 8 - 44; // 選択アイテム分を引く
        float hpRowH   = hpAreaH / 4f;

        var pm = PartyManager.Instance;
        if (pm != null)
        {
            int idx = 0;
            foreach (var m in pm.Members)
            {
                DrawHPRow(padX, hpStartY + idx * hpRowH, sw - padX * 2, hpRowH - 2, m);
                idx++;
            }
        }

        // ── 最下段：選択アイテム ──────────────────────────────────
        var gp = GridPlacer.Instance;
        string itemLabel = gp != null ? gp.SelectedItemLabel : "";
        if (!string.IsNullOrEmpty(itemLabel))
        {
            float itemW = sw * 0.6f;
            GUI.Label(new Rect((sw - itemW) * 0.5f, sh - 42, itemW, 40),
                      $"▶  {itemLabel}  ◀", styleItem);
        }
    }

    // ================================================================
    //  描画ヘルパー
    // ================================================================

    float DrawQuestLine(float x, float y, float w, QuestStep q, float currentY)
    {
        if (q == null) return currentY;
        bool done = q.IsComplete;
        string prefix = done ? "✦ " : "◇ ";
        string text   = $"{prefix}{q.title}  {q.current}/{q.required}";
        GUI.Label(new Rect(x, y, w, 26), text, done ? styleQuestDone : styleQuest);
        return y + 28;
    }

    void DrawHPRow(float x, float y, float w, float h, CharacterStats m)
    {
        float nameW  = w * 0.28f;
        float barX   = x + nameW + 6;
        float barW   = w * 0.52f;
        float numX   = barX + barW + 6;
        float numW   = w - nameW - barW - 12;
        float barH   = Mathf.Min(h * 0.55f, 14);
        float barY   = y + (h - barH) * 0.5f;

        // 名前
        GUI.Label(new Rect(x, y, nameW, h), m.characterName, styleHPName);

        // HP バー背景
        DrawTex(barX, barY, barW, barH, texHPBg);

        // HP バー本体
        float ratio   = m.maxHp > 0 ? (float)m.currentHp / m.maxHp : 0f;
        Texture2D bar = ratio > 0.3f ? texHPFull : texHPLow;
        DrawTex(barX, barY, barW * ratio, barH, bar);

        // HP 数値
        GUI.Label(new Rect(numX, y, numW, h), $"{m.currentHp}/{m.maxHp}", styleHPName);
    }

    static void DrawTex(float x, float y, float w, float h, Texture2D tex)
    {
        GUI.DrawTexture(new Rect(x, y, w, h), tex);
    }

    // ================================================================
    //  初期化（OnGUI 内で初回だけ実行）
    // ================================================================

    void Init()
    {
        texPanelBg = MakeTex(new Color(0.04f, 0.04f, 0.18f, 0.92f));
        texBorder  = MakeTex(new Color(0.85f, 0.65f, 0.10f, 1.00f));
        texDivider = MakeTex(new Color(0.50f, 0.45f, 0.20f, 0.80f));
        texHPFull  = MakeTex(new Color(0.20f, 0.80f, 0.25f, 1.00f));
        texHPLow   = MakeTex(new Color(0.85f, 0.20f, 0.10f, 1.00f));
        texHPBg    = MakeTex(new Color(0.10f, 0.10f, 0.10f, 0.90f));

        styleQuest = Style(16, new Color(0.85f, 0.85f, 0.85f), TextAnchor.MiddleLeft);
        styleQuestDone = Style(16, new Color(0.60f, 0.95f, 0.60f), TextAnchor.MiddleLeft, FontStyle.Bold);
        styleGold  = Style(28, new Color(1.00f, 0.85f, 0.10f), TextAnchor.UpperLeft, FontStyle.Bold);
        styleLeave = Style(15, new Color(0.55f, 0.90f, 0.55f), TextAnchor.MiddleLeft, FontStyle.Italic);
        styleHPName = Style(15, Color.white, TextAnchor.MiddleLeft);
        styleItem  = Style(22, new Color(1.00f, 0.95f, 0.50f), TextAnchor.MiddleCenter, FontStyle.Bold);
        styleSectionTitle = Style(14, new Color(0.85f, 0.70f, 0.20f), TextAnchor.MiddleLeft, FontStyle.Bold);

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
        FontStyle fontStyle = FontStyle.Normal)
    {
        var s = new GUIStyle(GUI.skin.label)
        {
            fontSize  = size,
            fontStyle = fontStyle,
            alignment = anchor,
        };
        s.normal.textColor = color;
        return s;
    }
}
