using UnityEngine;

/// <summary>
/// グリッドの状態を一元管理するシングルトン。
/// 「配置できるか？」「削除する」などのロジックはすべてここ。
/// </summary>
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("グリッド設定")]
    [SerializeField] int width = 10;
    [SerializeField] int height = 10;
    [SerializeField] float cellSize = 1f;
    [SerializeField] Vector3 originPosition = Vector3.zero;

    GridCell[,] cells;

    // 外から読み取れるようにプロパティを公開
    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;
    public Vector3 OriginPosition => originPosition;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitGrid();
    }

    void InitGrid()
    {
        cells = new GridCell[width, height];
        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                cells[x, z] = new GridCell(x, z);
    }

    // ---- 配置・削除 ----

    public bool TryPlace(int x, int z, GameObject prefab)
    {
        if (!IsValid(x, z) || cells[x, z].IsOccupied) return false;

        Vector3 pos = GetWorldPosition(x, z);
        GameObject obj = Instantiate(prefab, pos, Quaternion.identity);
        cells[x, z].Place(obj);
        return true;
    }

    public bool TryRemove(int x, int z)
    {
        if (!IsValid(x, z) || !cells[x, z].IsOccupied) return false;

        // 複数マス建物は BuildingManager 経由で撤去する（ここでは扱わない）
        if (cells[x, z].IsPartOfBuilding) return false;

        // 畑タイルを撤去するとき、その上の作物も一緒に破棄する
        cells[x, z].ClearCrop();
        Destroy(cells[x, z].PlacedObject);
        cells[x, z].Clear();
        return true;
    }

    // ---- 情報取得 ----

    public GridCell GetCell(int x, int z) =>
        IsValid(x, z) ? cells[x, z] : null;

    public bool IsValid(int x, int z) =>
        x >= 0 && x < width && z >= 0 && z < height;

    /// <summary>グリッド座標 → ワールド座標（マスの左下隅）</summary>
    public Vector3 GetWorldPosition(int x, int z) =>
        originPosition + new Vector3((x + 0.5f) * cellSize, 0f, (z + 0.5f) * cellSize);

    /// <summary>ワールド座標 → グリッド座標。グリッド外なら false を返す。</summary>
    public bool WorldToGrid(Vector3 worldPos, out int x, out int z)
    {
        x = Mathf.FloorToInt((worldPos.x - originPosition.x) / cellSize);
        z = Mathf.FloorToInt((worldPos.z - originPosition.z) / cellSize);
        return IsValid(x, z);
    }

    // ---- エディタ上でグリッドを可視化 ----

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.4f);

        int w = width;
        int h = height;

        for (int x = 0; x <= w; x++)
        {
            Vector3 from = originPosition + new Vector3(x * cellSize, 0.01f, 0);
            Vector3 to   = originPosition + new Vector3(x * cellSize, 0.01f, h * cellSize);
            Gizmos.DrawLine(from, to);
        }
        for (int z = 0; z <= h; z++)
        {
            Vector3 from = originPosition + new Vector3(0,          0.01f, z * cellSize);
            Vector3 to   = originPosition + new Vector3(w * cellSize, 0.01f, z * cellSize);
            Gizmos.DrawLine(from, to);
        }
    }
}
