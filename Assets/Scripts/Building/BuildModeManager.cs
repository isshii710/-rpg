using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// F キーでビルドモードを切り替えるモジュール建築コントローラー。
///
/// 【操作】
///   F キー         : ビルドモード ON/OFF
///   左クリック     : パーツを配置
///   右クリック     : パーツを撤去
///   R キー         : 回転 (90° ずつ)
///   Escape         : ビルドモード終了
///
/// 【スナップ仕様】
///   床・屋根 : グリッドマスの中央 (0.5 刻み)
///   壁       : 回転 0/180° → マスの南北端（Z=整数）
///              回転 90/270° → マスの東西端（X=整数）
///   柱       : マスの角（X=整数, Z=整数）
/// </summary>
public class BuildModeManager : MonoBehaviour
{
    public static BuildModeManager Instance { get; private set; }

    public bool IsActive { get; private set; }
    public int  SelectedIndex  { get; private set; }
    public int  Rotation       { get; private set; }  // 0..3 (×90°)

    public BuildPieceData[] Pieces { get; private set; }

    Camera    cam;
    GameObject ghostObj;
    Material   ghostMat;

    // (x*2, y*2, z*2) → placed GameObject
    readonly Dictionary<Vector3Int, GameObject> placed = new();

    // ================================================================
    //  初期化
    // ================================================================

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        cam    = Camera.main;
        Pieces = Resources.LoadAll<BuildPieceData>("BuildPieces");
        CreateGhostMat();
    }

    // ================================================================
    //  Update
    // ================================================================

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F)) Toggle();
        if (!IsActive) return;

        if (Input.GetKeyDown(KeyCode.R))       Rotate();
        if (Input.GetKeyDown(KeyCode.Escape))  ExitBuildMode();

        // 1-4 でカテゴリ選択
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectCategory(BuildPieceCategory.Floor);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectCategory(BuildPieceCategory.Wall);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectCategory(BuildPieceCategory.Roof);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectCategory(BuildPieceCategory.Pillar);

        UpdateGhost();

        if (Input.GetMouseButtonDown(0)) Place();
        if (Input.GetMouseButtonDown(1)) Remove();
    }

    // ================================================================
    //  モード切替
    // ================================================================

    void Toggle()
    {
        if (IsActive) ExitBuildMode();
        else          EnterBuildMode();
    }

    void EnterBuildMode()
    {
        IsActive = true;

        // GridPlacer を無効化（競合防止）
        if (GridPlacer.Instance != null) GridPlacer.Instance.enabled = false;

        if (Pieces != null && Pieces.Length > 0) SelectPiece(0);
        Debug.Log("[Build] ビルドモード ON  (F/Esc=終了 / R=回転 / 1=床 2=壁 3=屋根 4=柱)");
    }

    void ExitBuildMode()
    {
        IsActive = false;
        DestroyGhost();
        if (GridPlacer.Instance != null) GridPlacer.Instance.enabled = true;
        Debug.Log("[Build] ビルドモード OFF");
    }

    // ================================================================
    //  ピース選択
    // ================================================================

    void SelectCategory(BuildPieceCategory cat)
    {
        if (Pieces == null) return;
        for (int i = 0; i < Pieces.Length; i++)
        {
            if (Pieces[i] != null && Pieces[i].category == cat)
            {
                SelectPiece(i);
                return;
            }
        }
    }

    public void SelectPiece(int index)
    {
        if (Pieces == null || index < 0 || index >= Pieces.Length) return;
        SelectedIndex = index;
        CreateGhost(Pieces[index]);
    }

    void Rotate()
    {
        Rotation = (Rotation + 1) % 4;
        if (ghostObj != null)
            ghostObj.transform.rotation = Quaternion.Euler(0, Rotation * 90f, 0);
    }

    // ================================================================
    //  ゴースト（プレビュー）
    // ================================================================

    void CreateGhostMat()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        ghostMat = new Material(shader) { color = new Color(0.25f, 0.75f, 1f, 1f) };

        // URP 透明設定を試みる（失敗しても視覚的に問題ない）
        ghostMat.SetFloat("_Surface", 1f);
        ghostMat.SetFloat("_ZWrite",  0f);
        ghostMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        ghostMat.renderQueue = 3000;
    }

    void CreateGhost(BuildPieceData data)
    {
        DestroyGhost();
        if (data?.prefab == null) return;

        ghostObj = Instantiate(data.prefab);
        ghostObj.name = "_BuildGhost";

        foreach (var r in ghostObj.GetComponentsInChildren<Renderer>())
        {
            var mats = new Material[r.materials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = ghostMat;
            r.materials = mats;
        }
        foreach (var c in ghostObj.GetComponentsInChildren<Collider>())
            c.enabled = false;
    }

    void DestroyGhost()
    {
        if (ghostObj != null) { Destroy(ghostObj); ghostObj = null; }
    }

    void UpdateGhost()
    {
        if (ghostObj == null) return;
        if (!GetGroundPoint(out Vector3 wp)) { ghostObj.SetActive(false); return; }

        ghostObj.SetActive(true);
        ghostObj.transform.position = Snap(wp);
        ghostObj.transform.rotation = Quaternion.Euler(0, Rotation * 90f, 0);
    }

    // ================================================================
    //  配置 / 撤去
    // ================================================================

    void Place()
    {
        if (!GetGroundPoint(out Vector3 wp)) return;
        var data = CurrentPiece();
        if (data?.prefab == null) return;

        Vector3    pos = Snap(wp);
        Vector3Int key = ToKey(pos);

        if (placed.ContainsKey(key)) { Debug.Log("[Build] すでに配置済みです"); return; }

        // コスト確認
        var inv = InventoryManager.Instance;
        if (data.cost > 0 && (inv == null || inv.Gold < data.cost))
        {
            Debug.LogWarning($"[Build] ゴールドが不足しています（必要: {data.cost}G）");
            return;
        }

        var go = Instantiate(data.prefab, pos, Quaternion.Euler(0, Rotation * 90f, 0));
        go.name = $"BP_{data.pieceName}";
        placed[key] = go;

        if (data.cost > 0) inv?.AddGold(-data.cost);
        Debug.Log($"[Build] {data.pieceName} を配置 @ {pos}");
    }

    void Remove()
    {
        if (!GetGroundPoint(out Vector3 wp)) return;

        var key = ToKey(Snap(wp));
        if (!placed.ContainsKey(key))
        {
            // 現在の選択・回転に依存せず、マウス周辺のすべてのスナップ候補を試す
            float x  = Mathf.Round(wp.x);
            float z  = Mathf.Round(wp.z);
            float xH = Mathf.Floor(wp.x) + 0.5f;
            float zH = Mathf.Floor(wp.z) + 0.5f;

            Vector3[] candidates = {
                new Vector3(xH, 0f, zH),  // 床
                new Vector3(xH, 1f, zH),  // 屋根
                new Vector3(xH, 0f, z),   // N/S 壁
                new Vector3(x,  0f, zH),  // E/W 壁
                new Vector3(x,  0f, z),   // 柱
            };

            bool found = false;
            foreach (var pos in candidates)
            {
                var k = ToKey(pos);
                if (placed.ContainsKey(k)) { key = k; found = true; break; }
            }
            if (!found) return;
        }

        Destroy(placed[key]);
        placed.Remove(key);
        Debug.Log("[Build] パーツを撤去しました");
    }

    // ================================================================
    //  ヘルパー
    // ================================================================

    BuildPieceData CurrentPiece() =>
        (Pieces != null && SelectedIndex < Pieces.Length) ? Pieces[SelectedIndex] : null;

    /// <summary>マウス位置から地平面との交点を求める（Y=0 平面）。</summary>
    bool GetGroundPoint(out Vector3 wp)
    {
        wp = Vector3.zero;
        if (cam == null) return false;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // 物理コライダーへのヒットを優先
        if (Physics.Raycast(ray, out RaycastHit hit, 300f))
        {
            wp = hit.point;
            return true;
        }

        // Y=0 平面との交点（フォールバック）
        if (Mathf.Abs(ray.direction.y) > 0.001f)
        {
            float t = -ray.origin.y / ray.direction.y;
            if (t > 0) { wp = ray.origin + ray.direction * t; return true; }
        }
        return false;
    }

    /// <summary>ワールド座標をピース種別・回転に応じてスナップする。</summary>
    Vector3 Snap(Vector3 wp)
    {
        float x  = Mathf.Round(wp.x);           // 整数 X
        float z  = Mathf.Round(wp.z);            // 整数 Z
        float xH = Mathf.Floor(wp.x) + 0.5f;    // 半整数 X
        float zH = Mathf.Floor(wp.z) + 0.5f;    // 半整数 Z

        var data = CurrentPiece();
        if (data == null) return new Vector3(xH, 0f, zH);

        return data.category switch
        {
            BuildPieceCategory.Floor  => new Vector3(xH, 0f, zH),
            BuildPieceCategory.Roof   => new Vector3(xH, 1f, zH),
            BuildPieceCategory.Pillar => new Vector3(x,  0f, z),
            BuildPieceCategory.Wall   => (Rotation % 2 == 0)
                                            ? new Vector3(xH, 0f, z)   // N/S 壁
                                            : new Vector3(x,  0f, zH), // E/W 壁
            _ => new Vector3(xH, 0f, zH),
        };
    }

    /// <summary>0.5m 単位の整数キーに変換する。</summary>
    static Vector3Int ToKey(Vector3 p) => new Vector3Int(
        Mathf.RoundToInt(p.x * 2),
        Mathf.RoundToInt(p.y * 2),
        Mathf.RoundToInt(p.z * 2));

    void OnDestroy() => DestroyGhost();
}
