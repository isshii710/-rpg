using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 線路の配置・撤去・接続検出・経路探索（BFS）を管理するシングルトン。
///
/// 【プレハブの向き規約】
///   Straight   : デフォルト向きで南北（Z軸方向）に通っている ┃
///   Curve      : デフォルト向きで南（-Z）と東（+X）を繋ぐ形  ┐
///   T-Junction : デフォルト向きで北・南・東を繋ぐ形           ├
///   Cross      : 回転不要（十字）                              ┼
/// </summary>
public class RailManager : MonoBehaviour
{
    public static RailManager Instance { get; private set; }

    [Header("線路の見た目プレハブ（Straight のみ必須。他は省略するとStraightで代用）")]
    [SerializeField] GameObject straightPrefab;
    [SerializeField] GameObject curvePrefab;
    [SerializeField] GameObject tJunctionPrefab;
    [SerializeField] GameObject crossPrefab;

    // 4方向の差分（North, East, South, West の順）
    static readonly int[] Dx = {  0, 1,  0, -1 };
    static readonly int[] Dz = {  1, 0, -1,  0 };
    static readonly TrackDirection[] Dirs =
    {
        TrackDirection.North, TrackDirection.East,
        TrackDirection.South, TrackDirection.West,
    };

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ================================================================
    //  配置・撤去
    // ================================================================

    public bool TryPlaceTrack(int x, int z)
    {
        GridCell cell = GridManager.Instance.GetCell(x, z);
        if (cell == null || cell.IsOccupied) return false;

        cell.SetTrack(true);
        RefreshVisual(x, z);
        RefreshNeighborVisuals(x, z);
        return true;
    }

    public bool TryRemoveTrack(int x, int z)
    {
        GridCell cell = GridManager.Instance.GetCell(x, z);
        if (cell == null || !cell.IsTrack) return false;

        if (cell.PlacedObject != null) Destroy(cell.PlacedObject);
        cell.SetTrack(false);
        cell.ClearPlacedObject();

        RefreshNeighborVisuals(x, z);  // 隣の線路の形も更新
        return true;
    }

    // ================================================================
    //  接続情報取得
    // ================================================================

    /// <summary>（x, z）マスの線路が、隣接4方向のどこと繋がっているかを返す。</summary>
    public TrackDirection GetConnections(int x, int z)
    {
        TrackDirection result = TrackDirection.None;
        for (int i = 0; i < 4; i++)
            if (IsTrackAt(x + Dx[i], z + Dz[i]))
                result |= Dirs[i];
        return result;
    }

    bool IsTrackAt(int x, int z)
    {
        GridCell cell = GridManager.Instance.GetCell(x, z);
        return cell != null && cell.IsTrack;
    }

    // ================================================================
    //  BFS 経路探索
    // ================================================================

    /// <summary>
    /// 始点から終点まで線路を辿った経路をグリッド座標のリストで返す。
    /// 繋がっていない場合は null を返す。
    /// </summary>
    public List<Vector2Int> FindPath(int startX, int startZ, int endX, int endZ)
    {
        if (!IsTrackAt(startX, startZ) || !IsTrackAt(endX, endZ))
        {
            Debug.LogWarning("[RailManager] 始点または終点が線路ではありません。");
            return null;
        }

        var start = new Vector2Int(startX, startZ);
        var end   = new Vector2Int(endX,   endZ);

        var queue   = new Queue<Vector2Int>();
        var visited = new HashSet<Vector2Int>();
        var parent  = new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == end)
                return ReconstructPath(parent, start, end);

            foreach (var neighbor in GetTrackNeighbors(current.x, current.y))
            {
                if (visited.Contains(neighbor)) continue;
                visited.Add(neighbor);
                parent[neighbor] = current;
                queue.Enqueue(neighbor);
            }
        }

        Debug.LogWarning("[RailManager] 始点から終点への線路が繋がっていません。");
        return null;
    }

    List<Vector2Int> GetTrackNeighbors(int x, int z)
    {
        var list = new List<Vector2Int>(4);
        for (int i = 0; i < 4; i++)
            if (IsTrackAt(x + Dx[i], z + Dz[i]))
                list.Add(new Vector2Int(x + Dx[i], z + Dz[i]));
        return list;
    }

    static List<Vector2Int> ReconstructPath(
        Dictionary<Vector2Int, Vector2Int> parent, Vector2Int start, Vector2Int end)
    {
        var path    = new List<Vector2Int>();
        var current = end;
        while (current != start)
        {
            path.Add(current);
            current = parent[current];
        }
        path.Add(start);
        path.Reverse();
        return path;
    }

    /// <summary>グリッド座標のリストをワールド座標のリストに変換する。</summary>
    public List<Vector3> GridPathToWorld(List<Vector2Int> gridPath)
    {
        var worldPath = new List<Vector3>(gridPath.Count);
        foreach (var p in gridPath)
            worldPath.Add(GridManager.Instance.GetWorldPosition(p.x, p.y));
        return worldPath;
    }

    // ================================================================
    //  見た目の更新（接続数に応じてプレハブ＋回転を選択）
    // ================================================================

    void RefreshVisual(int x, int z)
    {
        GridCell cell = GridManager.Instance.GetCell(x, z);
        if (cell == null || !cell.IsTrack) return;

        if (cell.PlacedObject != null) Destroy(cell.PlacedObject);

        TrackDirection connections = GetConnections(x, z);
        (GameObject prefab, float angle) = SelectVisual(connections);

        Vector3 pos = GridManager.Instance.GetWorldPosition(x, z);
        GameObject obj = Instantiate(prefab, pos, Quaternion.Euler(0f, angle, 0f));
        cell.Place(obj);
    }

    void RefreshNeighborVisuals(int x, int z)
    {
        for (int i = 0; i < 4; i++)
            RefreshVisual(x + Dx[i], z + Dz[i]);
    }

    /// <summary>接続フラグからプレハブと回転角を決定する。</summary>
    (GameObject prefab, float angle) SelectVisual(TrackDirection d)
    {
        int count = CountBits((int)d);

        switch (count)
        {
            case 4:
                return (crossPrefab ?? straightPrefab, 0f);

            case 3:
                return (tJunctionPrefab ?? straightPrefab, GetTJunctionAngle(d));

            case 2:
                bool isNS = Has(d, TrackDirection.North | TrackDirection.South);
                bool isEW = Has(d, TrackDirection.East  | TrackDirection.West);
                if (isNS) return (straightPrefab, 0f);
                if (isEW) return (straightPrefab, 90f);
                return (curvePrefab ?? straightPrefab, GetCurveAngle(d));

            default: // 0 or 1（端点・孤立）→ 接続方向に向けた直線で表示
                bool east = Has(d, TrackDirection.East) || Has(d, TrackDirection.West);
                return (straightPrefab, east ? 90f : 0f);
        }
    }

    /// <summary>
    /// 曲線の回転角。プレハブのデフォルトを「南（-Z）と東（+X）を繋ぐ ┐」として定義。
    /// </summary>
    float GetCurveAngle(TrackDirection d)
    {
        if (Has(d, TrackDirection.South | TrackDirection.East)) return 0f;
        if (Has(d, TrackDirection.South | TrackDirection.West)) return 90f;
        if (Has(d, TrackDirection.North | TrackDirection.West)) return 180f;
        return 270f; // North | East
    }

    /// <summary>
    /// T字路の回転角。プレハブのデフォルトを「北・南・東を繋ぐ ├（西欠け）」として定義。
    /// </summary>
    float GetTJunctionAngle(TrackDirection d)
    {
        if (!Has(d, TrackDirection.West))  return 0f;    // NSE
        if (!Has(d, TrackDirection.North)) return 90f;   // SEW
        if (!Has(d, TrackDirection.East))  return 180f;  // NSW
        return 270f;                                      // NEW
    }

    static bool Has(TrackDirection d, TrackDirection flags) => (d & flags) == flags;

    static int CountBits(int v)
    {
        int c = 0;
        while (v != 0) { c += v & 1; v >>= 1; }
        return c;
    }
}
