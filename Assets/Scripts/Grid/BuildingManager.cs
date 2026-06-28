using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 複数マスを占有する建物の配置・撤去を管理するシングルトン。
/// 1マスの建物も扱えるため、将来的にすべての建物配置をここに集約できる。
/// </summary>
public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    // (rootX, rootZ) → 配置した BuildingData の逆引きテーブル
    readonly Dictionary<(int, int), BuildingData> buildingDataMap = new();
    readonly Dictionary<(int,int), int> rotationMap = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ---- 判定 ----

    /// <summary>
    /// rootX/rootZ を起点として sizeX×sizeZ マスがすべて空いているか確認する。
    /// </summary>
    public bool CanPlace(int rootX, int rootZ, int sizeX, int sizeZ)
    {
        for (int dx = 0; dx < sizeX; dx++)
            for (int dz = 0; dz < sizeZ; dz++)
            {
                GridCell cell = GridManager.Instance.GetCell(rootX + dx, rootZ + dz);
                if (cell == null || cell.IsOccupied) return false;
            }
        return true;
    }

    // ---- 配置 ----

    /// <summary>
    /// BuildingData の定義に従って建物を配置する。
    /// rotation: 0=0°, 1=90°, 2=180°, 3=270°（90°刻み）
    /// </summary>
    public bool TryPlace(int rootX, int rootZ, BuildingData data, int rotation)
    {
        // 90°/270° 回転時は X と Z のサイズが入れ替わる
        int sizeX = rotation % 2 == 0 ? data.sizeX : data.sizeZ;
        int sizeZ = rotation % 2 == 0 ? data.sizeZ : data.sizeX;

        if (!CanPlace(rootX, rootZ, sizeX, sizeZ)) return false;

        // フットプリントの中心にオブジェクトを生成
        Vector3 center = GetPlacementCenter(rootX, rootZ, sizeX, sizeZ);
        Quaternion rot = Quaternion.Euler(0f, rotation * 90f, 0f);
        GameObject obj = Instantiate(data.prefab, center, rot);

        // ルートセルにオブジェクトを登録し、フットプリント全体をマーク
        GridCell rootCell = GridManager.Instance.GetCell(rootX, rootZ);
        rootCell.Place(obj);

        var footprint = new List<GridCell>();
        for (int dx = 0; dx < sizeX; dx++)
            for (int dz = 0; dz < sizeZ; dz++)
            {
                GridCell c = GridManager.Instance.GetCell(rootX + dx, rootZ + dz);
                c.SetBuildingRoot(rootCell);
                footprint.Add(c);
            }

        rootCell.RegisterFootprint(footprint);
        buildingDataMap[(rootX, rootZ)] = data;
        rotationMap[(rootX, rootZ)] = rotation;
        return true;
    }

    // ---- 撤去 ----

    /// <summary>
    /// クリックしたマスが建物の一部なら、建物全体を撤去する。
    /// </summary>
    public bool TryRemove(int x, int z)
    {
        GridCell cell = GridManager.Instance.GetCell(x, z);
        if (cell == null || !cell.IsPartOfBuilding) return false;

        GridCell root = cell.BuildingRoot;

        // イテレーション中にリストを変えないよう、先にコピーを取る
        var footprint = new List<GridCell>(root.FootprintCells);
        foreach (var c in footprint)
            c.ClearBuilding();

        buildingDataMap.Remove((root.X, root.Z));
        rotationMap.Remove((root.X, root.Z));
        Destroy(root.PlacedObject);
        root.ClearPlacedObject();
        return true;
    }

    // ---- 交易所チェック ----

    /// <summary>指定マスの建物の BuildingData を返す。建物がなければ null。</summary>
    public BuildingData GetBuildingDataAt(int x, int z)
    {
        GridCell cell = GridManager.Instance.GetCell(x, z);
        if (cell == null || !cell.IsPartOfBuilding) return null;
        GridCell root = cell.BuildingRoot;
        if (root == null) return null;
        buildingDataMap.TryGetValue((root.X, root.Z), out var data);
        return data;
    }

    /// <summary>指定マスが交易所建物かどうかを返す。</summary>
    public bool IsTradePost(int x, int z) =>
        GetBuildingDataAt(x, z)?.type == BuildingType.TradePost;

    /// <summary>配置済みすべての建物を (rootX, rootZ, rotation, data) で列挙する。</summary>
    public System.Collections.Generic.IEnumerable<(int rootX, int rootZ, int rotation, BuildingData data)> GetAllPlacements()
    {
        foreach (var kv in buildingDataMap)
        {
            rotationMap.TryGetValue(kv.Key, out int rot);
            yield return (kv.Key.Item1, kv.Key.Item2, rot, kv.Value);
        }
    }

    // ---- ユーティリティ ----

    /// <summary>
    /// フットプリントの中心ワールド座標を返す。GridPlacer のプレビュー位置計算にも使う。
    /// </summary>
    public Vector3 GetPlacementCenter(int rootX, int rootZ, int sizeX, int sizeZ)
    {
        float cs = GridManager.Instance.CellSize;
        Vector3 rootPos = GridManager.Instance.GetWorldPosition(rootX, rootZ);
        return rootPos + new Vector3((sizeX - 1) * 0.5f * cs, 0f, (sizeZ - 1) * 0.5f * cs);
    }
}
