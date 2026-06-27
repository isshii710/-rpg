using System.Collections.Generic;
using UnityEngine;

/// <summary>列車の走行モード。</summary>
public enum TrainMode
{
    Bounce,  // 往復（A→B→A→B…）
    Loop,    // ループ（終点の次は始点へ戻る）
    OneWay,  // 片道（終点で停止）
}

/// <summary>
/// 線路ルートに沿って滑らかに自動移動する列車コントローラー。
///
/// 【使い方】
///   1. 列車プレハブにこのスクリプトをアタッチする
///   2. Inspector で StartX/StartZ と EndX/EndZ にグリッド座標を設定する
///   3. Play すると自動でルートを検索して走り始める
///   4. ルートを変更したいときは Inspector 右クリック → 「経路を再構築」
/// </summary>
public class TrainController : MonoBehaviour
{
    [Header("経路（グリッド座標で指定）")]
    [SerializeField] int startX;
    [SerializeField] int startZ;
    [SerializeField] int endX;
    [SerializeField] int endZ;

    [Header("移動設定")]
    [SerializeField] float speed        = 4f;
    [SerializeField] float heightOffset = 0.5f;   // 地面からの浮かせ高さ
    [SerializeField] float rotateSpeed  = 360f;   // 方向転換の速度（度/秒）
    [SerializeField] TrainMode mode     = TrainMode.Bounce;

    List<Vector3>    route;
    List<Vector2Int> gridRoute;          // グリッド座標列（終点判定に使う）
    bool skipFirstArrival = true;        // 起動直後の初期位置では交易を発火しない

    int   waypointIndex  = 0;
    bool  movingForward  = true;
    bool  isRunning      = false;

    // ================================================================
    //  初期化
    // ================================================================

    void Start() => BuildRoute();

    /// <summary>
    /// Inspector 右クリック → 「経路を再構築」で呼び出せる。
    /// Play モード中に線路を追加・変更したあとに使う。
    /// </summary>
    [ContextMenu("経路を再構築")]
    public void BuildRoute()
    {
        if (RailManager.Instance == null)
        {
            Debug.LogError("[TrainController] RailManager がシーンにいません。");
            return;
        }

        List<Vector2Int> gridPath = RailManager.Instance.FindPath(startX, startZ, endX, endZ);
        if (gridPath == null || gridPath.Count < 2)
        {
            isRunning = false;
            return;
        }

        gridRoute         = gridPath;   // グリッド座標を保持しておく
        skipFirstArrival  = true;       // 再構築時も初回スキップをリセット

        List<Vector3> worldPath = RailManager.Instance.GridPathToWorld(gridPath);

        // 高さオフセットを適用
        route = new List<Vector3>(worldPath.Count);
        foreach (var p in worldPath)
            route.Add(p + Vector3.up * heightOffset);

        // 始点にテレポート
        transform.position = route[0];
        waypointIndex      = 0;
        movingForward      = true;
        isRunning          = true;

        Debug.Log($"[TrainController] 経路を構築しました。ウェイポイント数：{route.Count}");
    }

    // ================================================================
    //  移動
    // ================================================================

    void Update()
    {
        if (!isRunning || route == null || route.Count < 2) return;

        Vector3 target = route[waypointIndex];

        // 位置：一定速度で目標に近づく
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        // 回転：進行方向を向く（Y軸のみ）
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }

        // ウェイポイント到達チェック
        if (Vector3.Distance(transform.position, target) < 0.01f)
            AdvanceWaypoint();
    }

    void AdvanceWaypoint()
    {
        // 列車が waypointIndex に到着した瞬間 → 終点なら交易通知
        if (!skipFirstArrival) NotifyArrivalIfTerminal(waypointIndex);
        skipFirstArrival = false;

        switch (mode)
        {
            case TrainMode.Loop:
                waypointIndex = (waypointIndex + 1) % route.Count;
                break;

            case TrainMode.Bounce:
                waypointIndex += movingForward ? 1 : -1;
                if (waypointIndex >= route.Count) { waypointIndex = route.Count - 2; movingForward = false; }
                if (waypointIndex < 0)            { waypointIndex = 1;               movingForward = true;  }
                break;

            case TrainMode.OneWay:
                if (waypointIndex < route.Count - 1)
                    waypointIndex++;
                else
                    isRunning = false;  // 終点で停止
                break;
        }
    }

    // ================================================================
    //  交易通知
    // ================================================================

    void NotifyArrivalIfTerminal(int idx)
    {
        if (gridRoute == null || gridRoute.Count < 2) return;
        // 始点（0）または終点（Count-1）に到達したときだけ通知
        if (idx != 0 && idx != gridRoute.Count - 1) return;

        Vector2Int gp = gridRoute[idx];
        TradeManager.Instance?.OnTrainArrived(gp.x, gp.y);
    }

    // ================================================================
    //  デバッグ（Scene ビューにルートを可視化）
    // ================================================================

    void OnDrawGizmosSelected()
    {
        if (route == null || route.Count < 2) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < route.Count - 1; i++)
            Gizmos.DrawLine(route[i], route[i + 1]);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(route[0], 0.3f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(route[^1], 0.3f);
    }
}
