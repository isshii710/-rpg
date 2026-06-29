using UnityEngine;

/// <summary>
/// ビルドモード中に左サイドバーとしてパレットを表示する OnGUI UI。
/// BuildModeManager.IsActive が true のときのみ描画する。
/// </summary>
public class BuildUI : MonoBehaviour
{
    // レイアウト定数
    const float PW      = 192f;
    const float MARGIN  = 10f;
    const float BORDER  = 2f;

    // テクスチャ
    Texture2D texBg;
    Texture2D texBorder;
    Texture2D texBtnSel;    // 選択中ボタン背景
    Texture2D texBtnNorm;   // 通常ボタン背景
    Texture2D texBtnCat;    // カテゴリタブ背景
    Texture2D texBtnCatSel;

    // スタイル
    GUIStyle styleTitle;
    GUIStyle styleCat;
    GUIStyle styleCatSel;
    GUIStyle stylePiece;
    GUIStyle stylePieceSel;
    GUIStyle styleHint;
    GUIStyle styleRot;

    bool ready;

    static readonly string[] CategoryNames = { "床", "壁", "屋根", "柱" };
    BuildPieceCategory currentCat = BuildPieceCategory.Floor;

    void OnGUI()
    {
        if (!ready) Init();

        var bm = BuildModeManager.Instance;
        if (bm == null || !bm.IsActive) return;

        // 選択ピースに合わせてカテゴリタブを同期
        var selPiece = (bm.Pieces != null && bm.SelectedIndex < bm.Pieces.Length)
                       ? bm.Pieces[bm.SelectedIndex] : null;
        if (selPiece != null) currentCat = selPiece.category;

        // ── パネル全体 ──
        float ph = Screen.height - MARGIN * 2;
        float px = MARGIN;
        float py = MARGIN;

        DrawPanel(px, py, PW, ph);

        float y = py + 10;

        // タイトル
        GUI.Label(new Rect(px + 8, y, PW - 16, 24), "◆ BUILD MODE", styleTitle);
        y += 28;

        // 回転インジケーター
        string rotStr = (bm.Rotation * 90) + "°  " + RotArrow(bm.Rotation);
        GUI.Label(new Rect(px + 8, y, PW - 16, 20), $"向き: {rotStr}", styleRot);
        y += 24;

        // 区切り線
        DrawLine(px + 6, y, PW - 12);
        y += 8;

        // ── カテゴリタブ（2列） ──
        float tabW = (PW - 16) * 0.5f;
        for (int i = 0; i < 4; i++)
        {
            var cat = (BuildPieceCategory)i;
            bool sel = cat == currentCat;
            float tx = px + 8 + (i % 2) * (tabW + 2);
            float ty = y + (i / 2) * 28;
            if (GUI.Button(new Rect(tx, ty, tabW, 24), $"{i + 1}:{CategoryNames[i]}",
                           sel ? styleCatSel : styleCat))
            {
                currentCat = cat;
                SelectFirstInCategory(cat, bm);
            }
        }
        y += 62;

        DrawLine(px + 6, y, PW - 12);
        y += 8;

        // ── ピースリスト ──
        var pieces = bm.Pieces;
        if (pieces != null)
        {
            for (int i = 0; i < pieces.Length; i++)
            {
                var p = pieces[i];
                if (p == null || p.category != currentCat) continue;

                bool sel = (i == bm.SelectedIndex);
                string label = p.cost > 0 ? $"{p.pieceName}  {p.cost}G" : p.pieceName;

                if (GUI.Button(new Rect(px + 8, y, PW - 16, 26), label,
                               sel ? stylePieceSel : stylePiece))
                {
                    currentCat = p.category;
                    bm.SelectPiece(i);
                }
                y += 30;

                if (y > py + ph - 80) break; // パネルを超えたら打ち切り
            }
        }

        // ── 操作ヒント（下部固定） ──
        float hy = py + ph - 72;
        DrawLine(px + 6, hy, PW - 12);
        hy += 6;
        GUI.Label(new Rect(px + 8, hy, PW - 16, 18), "左Click: 配置", styleHint);
        GUI.Label(new Rect(px + 8, hy + 18, PW - 16, 18), "右Click: 撤去", styleHint);
        GUI.Label(new Rect(px + 8, hy + 36, PW - 16, 18), "R: 回転  F/Esc: 終了", styleHint);
    }

    static void SelectFirstInCategory(BuildPieceCategory cat, BuildModeManager bm)
    {
        var pieces = bm.Pieces;
        if (pieces == null) return;
        for (int i = 0; i < pieces.Length; i++)
            if (pieces[i] != null && pieces[i].category == cat) { bm.SelectPiece(i); return; }
    }

    static string RotArrow(int rot) => rot switch
    {
        0 => "↑", 1 => "→", 2 => "↓", 3 => "←", _ => "↑"
    };

    // ── 描画ヘルパー ──

    void DrawPanel(float x, float y, float w, float h)
    {
        GUI.DrawTexture(new Rect(x, y, w, h), texBg);
        GUI.DrawTexture(new Rect(x,         y,         w,     BORDER), texBorder);
        GUI.DrawTexture(new Rect(x,         y + h - BORDER, w, BORDER), texBorder);
        GUI.DrawTexture(new Rect(x,         y,         BORDER, h), texBorder);
        GUI.DrawTexture(new Rect(x + w - BORDER, y,   BORDER, h), texBorder);
    }

    void DrawLine(float x, float y, float w)
        => GUI.DrawTexture(new Rect(x, y, w, 1), texBorder);

    // ── 初期化（OnGUI 内で一度だけ） ──

    void Init()
    {
        texBg      = MakeTex(new Color(0.04f, 0.04f, 0.18f, 0.93f));
        texBorder  = MakeTex(new Color(0.80f, 0.62f, 0.10f, 1.00f));
        texBtnSel  = MakeTex(new Color(0.25f, 0.55f, 0.85f, 1.00f));
        texBtnNorm = MakeTex(new Color(0.10f, 0.12f, 0.30f, 1.00f));
        texBtnCat  = MakeTex(new Color(0.12f, 0.14f, 0.36f, 1.00f));
        texBtnCatSel = MakeTex(new Color(0.80f, 0.62f, 0.10f, 1.00f));

        styleTitle = MakeLabel(15, new Color(0.85f, 0.68f, 0.18f), bold: true);
        styleRot   = MakeLabel(12, new Color(0.80f, 0.85f, 1.00f));
        styleHint  = MakeLabel(11, new Color(0.65f, 0.65f, 0.65f));

        styleCat    = MakeBtn(texBtnCat,    new Color(0.85f, 0.85f, 0.85f), 12);
        styleCatSel = MakeBtn(texBtnCatSel, new Color(0.08f, 0.04f, 0.00f), 12, bold: true);

        stylePiece    = MakeBtn(texBtnNorm, Color.white, 13);
        stylePieceSel = MakeBtn(texBtnSel,  new Color(0.90f, 0.95f, 1.00f), 13, bold: true);

        ready = true;
    }

    static Texture2D MakeTex(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    static GUIStyle MakeLabel(int size, Color color, bool bold = false)
    {
        var s = new GUIStyle(GUI.skin.label)
        {
            fontSize  = size,
            fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
            alignment = TextAnchor.MiddleLeft,
        };
        s.normal.textColor = color;
        return s;
    }

    static GUIStyle MakeBtn(Texture2D bg, Color textColor, int size, bool bold = false)
    {
        var s = new GUIStyle(GUI.skin.button)
        {
            fontSize  = size,
            fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
            alignment = TextAnchor.MiddleLeft,
        };
        s.normal.background   = bg;
        s.hover.background    = bg;
        s.active.background   = bg;
        s.normal.textColor    = textColor;
        s.hover.textColor     = textColor;
        s.active.textColor    = textColor;
        s.padding             = new RectOffset(8, 4, 0, 0);
        return s;
    }
}
