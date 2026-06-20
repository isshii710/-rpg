using UnityEngine;

/// <summary>
/// 配置アイテムの種類。
/// 将来「線路」「水路」などを追加するときはここに値を足す。
/// </summary>
public enum ItemType
{
    Building,  // 家などの通常建物
    Farmland,  // 畑タイル（配置後に農業が可能になる）
    Seed,      // 種（畑マスにのみ植えられる）
}

/// <summary>
/// Inspectorで設定するアイテム1件分の情報。
/// </summary>
[System.Serializable]
public class PlacementItemConfig
{
    public string label   = "アイテム";
    public ItemType type  = ItemType.Building;
    public GameObject prefab;  // 配置・プレビューに使うプレハブ
}

/// <summary>
/// マウス操作でグリッドを操作するコントローラー。
///
/// 【操作】
///   数字キー 1〜9  : アイテム切り替え
///   左クリック     : アイテムに応じた配置（建物/畑/種まき）
///   右クリック     : 収穫可能な作物なら収穫、それ以外は撤去
/// </summary>
public class GridPlacer : MonoBehaviour
{
    [Header("配置アイテム（数字キー 1〜9 で切替）")]
    [SerializeField] PlacementItemConfig[] items;

    [Header("地面のレイヤー（Groundレイヤーを設定する）")]
    [SerializeField] LayerMask groundLayer;

    int selectedIndex = 0;
    GameObject previewObject;
    Camera mainCamera;

    static readonly Color ColorOk = new Color(0f, 1f, 0f, 0.5f);
    static readonly Color ColorNg = new Color(1f, 0f, 0f, 0.5f);

    void Start()
    {
        mainCamera = Camera.main;
        SpawnPreview();
    }

    void Update()
    {
        HandleItemSwitch();

        if (!RaycastToGrid(out int x, out int z))
        {
            SetPreviewVisible(false);
            return;
        }

        SetPreviewVisible(true);
        MovePreview(x, z);

        GridCell cell = GridManager.Instance.GetCell(x, z);
        PlacementItemConfig current = items[selectedIndex];

        TintPreview(CanActOnCell(current, cell) ? ColorOk : ColorNg);

        if (Input.GetMouseButtonDown(0)) HandleLeftClick(x, z, current, cell);
        if (Input.GetMouseButtonDown(1)) HandleRightClick(x, z, cell);
    }

    // ---- 入力処理 ----

    void HandleItemSwitch()
    {
        for (int i = 0; i < items.Length && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                selectedIndex = i;
                SpawnPreview();
            }
        }
    }

    void HandleLeftClick(int x, int z, PlacementItemConfig item, GridCell cell)
    {
        switch (item.type)
        {
            case ItemType.Building:
                GridManager.Instance.TryPlace(x, z, item.prefab);
                break;

            case ItemType.Farmland:
                // 配置に成功したらそのマスを畑フラグONにする
                if (GridManager.Instance.TryPlace(x, z, item.prefab))
                    cell.SetFarmland(true);
                break;

            case ItemType.Seed:
                FarmManager.Instance.TryPlantSeed(x, z);
                break;
        }
    }

    void HandleRightClick(int x, int z, GridCell cell)
    {
        // 収穫可能な作物があれば収穫を優先する
        if (cell != null && cell.CropStage == CropStage.Mature)
        {
            FarmManager.Instance.TryHarvest(x, z);
            return;
        }

        // それ以外は建物・畑タイルを撤去する
        GridManager.Instance.TryRemove(x, z);
    }

    /// <summary>選択中のアイテムでそのマスを操作できるか判定する。</summary>
    bool CanActOnCell(PlacementItemConfig item, GridCell cell)
    {
        if (cell == null) return false;

        return item.type switch
        {
            ItemType.Building => !cell.IsOccupied,
            ItemType.Farmland => !cell.IsOccupied,
            ItemType.Seed     => cell.IsFarmland && !cell.HasCrop,
            _                 => false,
        };
    }

    // ---- レイキャスト ----

    bool RaycastToGrid(out int x, out int z)
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, groundLayer))
            return GridManager.Instance.WorldToGrid(hit.point, out x, out z);

        x = z = -1;
        return false;
    }

    // ---- プレビューオブジェクト管理 ----

    void SpawnPreview()
    {
        if (previewObject != null) Destroy(previewObject);
        if (items == null || items.Length == 0) return;

        GameObject prefab = items[selectedIndex].prefab;
        if (prefab == null) return;

        previewObject = Instantiate(prefab);

        foreach (var col in previewObject.GetComponentsInChildren<Collider>())
            col.enabled = false;

        foreach (var rb in previewObject.GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = true;
    }

    void MovePreview(int x, int z)
    {
        if (previewObject == null) return;
        previewObject.transform.position = GridManager.Instance.GetWorldPosition(x, z);
    }

    void SetPreviewVisible(bool visible)
    {
        if (previewObject != null)
            previewObject.SetActive(visible);
    }

    void TintPreview(Color color)
    {
        if (previewObject == null) return;
        foreach (var r in previewObject.GetComponentsInChildren<Renderer>())
            r.material.color = color;
    }

    void OnDestroy()
    {
        if (previewObject != null) Destroy(previewObject);
    }
}
