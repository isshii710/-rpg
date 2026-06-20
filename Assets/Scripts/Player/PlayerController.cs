using UnityEngine;

/// <summary>
/// プレイヤーキャラクターをグリッド上でWASD/矢印キーで移動させるコントローラー。
///
/// 【操作】
///   W / ↑  : 北（+Z）
///   S / ↓  : 南（-Z）
///   D / →  : 東（+X）
///   A / ←  : 西（-X）
///
/// 【荒野エンカウント】
///   村の境界外のマスに入ると BattleManager.OnPlayerMoved が呼ばれ、
///   確率で敵がスポーンする。
///
/// 【Unityでの設定】
///   プレイヤーキャラクターのプレハブにこのスクリプトをアタッチし、
///   Inspector で StartX / StartZ に初期グリッド座標を設定する。
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("初期グリッド座標")]
    [SerializeField] int startX = 5;
    [SerializeField] int startZ = 5;

    [Header("移動設定")]
    [SerializeField] float moveSpeed      = 8f;   // ワールド移動の補間速度
    [SerializeField] float stepInterval   = 0.18f; // 連続移動の最低間隔（秒）
    [SerializeField] float heightOffset   = 0.5f;  // 地面から浮かせる高さ

    // グリッド上の現在位置
    public int GridX { get; private set; }
    public int GridZ { get; private set; }

    Vector3 targetWorldPos;
    float   lastMoveTime;

    void Start()
    {
        GridX = startX;
        GridZ = startZ;
        Vector3 pos = GridManager.Instance.GetWorldPosition(GridX, GridZ) + Vector3.up * heightOffset;
        transform.position = targetWorldPos = pos;
    }

    void Update()
    {
        // ---- 移動補間 ----
        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);

        // 前回移動から最低間隔が経過するまで入力を受け付けない
        if (Time.time - lastMoveTime < stepInterval) return;
        // まだ目標地点に到達していなければ入力を受け付けない
        if (Vector3.Distance(transform.position, targetWorldPos) > 0.05f) return;

        // ---- 入力取得 ----
        int dx = 0, dz = 0;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    dz =  1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  dz = -1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dx =  1;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  dx = -1;

        if (dx == 0 && dz == 0) return;

        int newX = GridX + dx;
        int newZ = GridZ + dz;

        // グリッド範囲外はスキップ
        if (!GridManager.Instance.IsValid(newX, newZ)) return;

        GridCell target = GridManager.Instance.GetCell(newX, newZ);

        // 敵以外の障害物（建物・岩・木など）はすり抜けない
        // 敵がいるマスは押し込んで戦闘へ（攻撃は GridPlacer の左クリックで行う）
        bool blockedByEnemy = BattleManager.Instance != null &&
                              BattleManager.Instance.HasEnemyAt(newX, newZ);
        if (target.IsOccupied && !blockedByEnemy) return;

        // ---- 移動確定 ----
        GridX = newX;
        GridZ = newZ;
        targetWorldPos = GridManager.Instance.GetWorldPosition(GridX, GridZ) + Vector3.up * heightOffset;
        lastMoveTime   = Time.time;

        // 向きを進行方向に向ける
        Vector3 dir = new Vector3(dx, 0, dz);
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);

        // ---- 荒野エンカウント判定 ----
        BattleManager.Instance?.OnPlayerMoved(GridX, GridZ);
    }

    // ================================================================
    //  デバッグ
    // ================================================================

    void OnDrawGizmosSelected()
    {
        if (GridManager.Instance == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(
            GridManager.Instance.GetWorldPosition(GridX, GridZ) + Vector3.up * heightOffset,
            Vector3.one * 0.9f);
    }
}
