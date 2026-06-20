using UnityEngine;

/// <summary>
/// 配置アイテムの種類。
/// 将来「線路」「水路」などを追加するときはここに値を足す。
/// </summary>
public enum ItemType
{
    Building,  // 建物（BuildingDataを使う）
    Farmland,  // 畑タイル（配置後に農業が可能になる）
    Seed,      // 種（畑マスにのみ植えられる）
    Track,     // 線路（RailManager が接続と見た目を自動管理する）
}

/// <summary>
/// Inspector で設定するアイテム1件分の情報。
/// </summary>
[System.Serializable]
public class PlacementItemConfig
{
    public string label           = "アイテム";
    public ItemType type          = ItemType.Building;
    public BuildingData buildingData; // Building 用（設定するとサイズ・回転が有効になる）
    public GameObject prefab;         // Farmland / Seed 用のプレハブ
}

/// <summary>
/// マウス操作でグリッドを操作するコントローラー。
///
/// 【操作】
///   数字キー 1〜9  : アイテム切り替え
///   R キー         : 建物の向きを 90° 回転（Building のみ有効）
///   左クリック     : 配置 / 種まき / 線路敷設
///   右クリック     : 収穫（Mature）/ 線路撤去 / 建物撤去 / 畑タイル撤去
/// </summary>
public class GridPlacer : MonoBehaviour
{
    [Header("配置アイテム（数字キー 1〜9 で切替）")]
    [SerializeField] PlacementItemConfig[] items;

    [Header("地面のレイヤー（Ground レイヤーを設定する）")]
    [SerializeField] LayerMask groundLayer;

    int selectedIndex   = 0;
    int currentRotation = 0;  // 0=0°, 1=90°, 2=180°, 3=270°

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
        HandleRotation();

        if (!RaycastToGrid(out int x, out int z))
        {
            SetPreviewVisible(false);
            return;
        }

        SetPreviewVisible(true);
        MovePreview(x, z);

        GridCell cell = GridManager.Instance.GetCell(x, z);
        PlacementItemConfig current = items[selectedIndex];

        TintPreview(CanActOnCell(current, cell, x, z) ? ColorOk : ColorNg);

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
                selectedIndex   = i;
                currentRotation = 0;
                SpawnPreview();
            }
        }
    }

    void HandleRotation()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentRotation = (currentRotation + 1) % 4;
            if (previewObject != null)
                previewObject.transform.rotation = Quaternion.Euler(0f, currentRotation * 90f, 0f);
        }
    }

    void HandleLeftClick(int x, int z, PlacementItemConfig item, GridCell cell)
    {
        switch (item.type)
        {
            case ItemType.Building:
                if (item.buildingData != null)
                    BuildingManager.Instance.TryPlace(x, z, item.buildingData, currentRotation);
                else
                    GridManager.Instance.TryPlace(x, z, item.prefab);
                break;

            case ItemType.Farmland:
                if (GridManager.Instance.TryPlace(x, z, item.prefab))
                    cell.SetFarmland(true);
                break;

            case ItemType.Seed:
                FarmManager.Instance.TryPlantSeed(x, z);
                break;

            case ItemType.Track:
                RailManager.Instance.TryPlaceTrack(x, z);
                break;
        }
    }

    void HandleRightClick(int x, int z, GridCell cell)
    {
        if (cell == null) return;

        // 優先順位：収穫 > 線路撤去 > 建物撤去 > 畑タイル撤去
        if (cell.CropStage == CropStage.Mature)
        {
            FarmManager.Instance.TryHarvest(x, z);
            return;
        }

        if (cell.IsTrack)
        {
            RailManager.Instance.TryRemoveTrack(x, z);
            return;
        }

        if (cell.IsPartOfBuilding)
        {
            BuildingManager.Instance.TryRemove(x, z);
            return;
        }

        GridManager.Instance.TryRemove(x, z);
    }

    /// <summary>選択中のアイテムでそのマスを操作できるか判定する。</summary>
    bool CanActOnCell(PlacementItemConfig item, GridCell cell, int x, int z)
    {
        if (cell == null) return false;

        if (item.type == ItemType.Building && item.buildingData != null)
        {
            BuildingData bd = item.buildingData;
            int effX = currentRotation % 2 == 0 ? bd.sizeX : bd.sizeZ;
            int effZ = currentRotation % 2 == 0 ? bd.sizeZ : bd.sizeX;
            return BuildingManager.Instance.CanPlace(x, z, effX, effZ);
        }

        return item.type switch
        {
            ItemType.Building => !cell.IsOccupied,
            ItemType.Farmland => !cell.IsOccupied,
            ItemType.Seed     => cell.IsFarmland && !cell.HasCrop,
            ItemType.Track    => !cell.IsOccupied,
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

        PlacementItemConfig current = items[selectedIndex];
        GameObject prefab = (current.type == ItemType.Building && current.buildingData != null)
            ? current.buildingData.prefab
            : current.prefab;

        if (prefab == null) return;

        previewObject = Instantiate(prefab);
        previewObject.transform.rotation = Quaternion.Euler(0f, currentRotation * 90f, 0f);

        foreach (var col in previewObject.GetComponentsInChildren<Collider>())
            col.enabled = false;
        foreach (var rb in previewObject.GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = true;
    }

    void MovePreview(int x, int z)
    {
        if (previewObject == null) return;

        PlacementItemConfig current = items[selectedIndex];
        if (current.type == ItemType.Building && current.buildingData != null)
        {
            BuildingData bd = current.buildingData;
            int effX = currentRotation % 2 == 0 ? bd.sizeX : bd.sizeZ;
            int effZ = currentRotation % 2 == 0 ? bd.sizeZ : bd.sizeX;
            previewObject.transform.position = BuildingManager.Instance.GetPlacementCenter(x, z, effX, effZ);
            previewObject.transform.rotation = Quaternion.Euler(0f, currentRotation * 90f, 0f);
        }
        else
        {
            previewObject.transform.position = GridManager.Instance.GetWorldPosition(x, z);
        }
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
