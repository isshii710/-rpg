using UnityEngine;

/// <summary>
/// 右下にグリッド全体のミニマップを表示する。
/// 各マスをセルの状態に応じた色の正方形で描画し、プレイヤー位置を青丸で示す。
/// </summary>
public class MinimapUI : MonoBehaviour
{
    public static MinimapUI Instance { get; private set; }

    [Header("ミニマップ設定")]
    [SerializeField] Transform playerTransform;

    // レイアウト
    const float MARGIN  = 10f;   // 画面端からの余白
    const float CELL_PX = 12f;   // 1マスのピクセルサイズ
    const float BORDER  = 2f;

    // テクスチャキャッシュ（色ごとに1枚）
    Texture2D texBg;
    Texture2D texBorder;
    Texture2D texEmpty;
    Texture2D texFarmland;
    Texture2D texSeed;
    Texture2D texSprout;
    Texture2D texCrop;
    Texture2D texTrack;
    Texture2D texBuilding;
    Texture2D texResource;
    Texture2D texPlayer;

    GUIStyle styleTitle;

    bool ready;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (playerTransform == null)
        {
            var p = GameObject.Find("Player");
            if (p != null) playerTransform = p.transform;
        }
    }

    void OnGUI()
    {
        if (!ready) Init();

        var gm = GridManager.Instance;
        if (gm == null) return;

        int w = gm.Width;
        int h = gm.Height;

        float mapW = w * CELL_PX;
        float mapH = h * CELL_PX;

        // 右下に配置（タイトル行ぶん上に余白）
        float titleH = 20f;
        float panelW = mapW + BORDER * 2 + 6;
        float panelH = mapH + BORDER * 2 + titleH + 6;
        float px = Screen.width  - panelW - MARGIN;
        float py = Screen.height - panelH - MARGIN;

        // パネル背景 + ボーダー
        DrawPanel(px, py, panelW, panelH);

        // タイトル
        GUI.Label(new Rect(px + 4, py + 2, panelW - 8, titleH), "MAP", styleTitle);

        // グリッド描画開始座標
        float ox = px + BORDER + 3;
        float oy = py + titleH + BORDER + 2;

        // 各セルを描画（z=0が画面下になるよう反転）
        for (int x = 0; x < w; x++)
        {
            for (int z = 0; z < h; z++)
            {
                var cell = gm.GetCell(x, z);
                float cx = ox + x * CELL_PX;
                float cy = oy + (h - 1 - z) * CELL_PX;   // z反転で北が上

                var tex = CellTex(cell);
                GUI.DrawTexture(new Rect(cx, cy, CELL_PX - 1, CELL_PX - 1), tex);
            }
        }

        // プレイヤーを青い点で表示
        if (playerTransform != null && gm.WorldToGrid(playerTransform.position, out int px2, out int pz))
        {
            float cx = ox + px2 * CELL_PX;
            float cy = oy + (h - 1 - pz) * CELL_PX;
            float dotSize = CELL_PX - 1;
            GUI.DrawTexture(new Rect(cx, cy, dotSize, dotSize), texPlayer);
        }
    }

    Texture2D CellTex(GridCell cell)
    {
        if (cell == null) return texEmpty;

        if (cell.IsPartOfBuilding || (cell.PlacedObject != null && !cell.IsFarmland && !cell.IsTrack))
            return texBuilding;

        if (cell.IsResource)
            return texResource;

        if (cell.HasCrop)
        {
            return cell.CropStage switch
            {
                CropStage.Seed   => texSeed,
                CropStage.Sprout => texSprout,
                CropStage.Mature => texCrop,
                _                => texFarmland,
            };
        }

        if (cell.IsFarmland)  return texFarmland;
        if (cell.IsTrack)     return texTrack;

        return texEmpty;
    }

    void DrawPanel(float x, float y, float w, float h)
    {
        GUI.DrawTexture(new Rect(x, y, w, h), texBg);
        GUI.DrawTexture(new Rect(x,         y,         w, BORDER), texBorder);
        GUI.DrawTexture(new Rect(x,         y + h - BORDER, w, BORDER), texBorder);
        GUI.DrawTexture(new Rect(x,         y,         BORDER, h), texBorder);
        GUI.DrawTexture(new Rect(x + w - BORDER, y,   BORDER, h), texBorder);
    }

    void Init()
    {
        texBg       = MakeTex(new Color(0.04f, 0.04f, 0.18f, 0.90f));
        texBorder   = MakeTex(new Color(0.80f, 0.62f, 0.10f, 1.00f));
        texEmpty    = MakeTex(new Color(0.15f, 0.15f, 0.20f, 1.00f));
        texFarmland = MakeTex(new Color(0.40f, 0.25f, 0.10f, 1.00f));
        texSeed     = MakeTex(new Color(0.80f, 0.75f, 0.20f, 1.00f));
        texSprout   = MakeTex(new Color(0.45f, 0.75f, 0.30f, 1.00f));
        texCrop     = MakeTex(new Color(0.15f, 0.85f, 0.20f, 1.00f));
        texTrack    = MakeTex(new Color(0.50f, 0.50f, 0.55f, 1.00f));
        texBuilding = MakeTex(new Color(0.85f, 0.55f, 0.15f, 1.00f));
        texResource = MakeTex(new Color(0.60f, 0.60f, 0.65f, 1.00f));
        texPlayer   = MakeTex(new Color(0.20f, 0.50f, 1.00f, 1.00f));

        styleTitle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
        styleTitle.normal.textColor = new Color(0.80f, 0.62f, 0.10f);

        ready = true;
    }

    static Texture2D MakeTex(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }
}
