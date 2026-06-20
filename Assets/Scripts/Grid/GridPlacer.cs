using UnityEngine;

/// <summary>
/// マウス操作でグリッドにオブジェクトを配置・削除するコントローラー。
/// 左クリック：配置　右クリック：削除
/// 数字キー 1〜9：配置するプレハブを切り替え
/// </summary>
public class GridPlacer : MonoBehaviour
{
    [Header("配置するプレハブ（1〜9キーで切替）")]
    [SerializeField] GameObject[] placementPrefabs;

    [Header("地面のレイヤー（Groundレイヤーを設定する）")]
    [SerializeField] LayerMask groundLayer;

    int selectedIndex = 0;
    GameObject previewObject;
    Camera mainCamera;

    // プレビュー用に使う半透明マテリアルの色
    static readonly Color ColorOk  = new Color(0f, 1f, 0f, 0.5f);
    static readonly Color ColorNg  = new Color(1f, 0f, 0f, 0.5f);

    void Start()
    {
        mainCamera = Camera.main;
        SpawnPreview();
    }

    void Update()
    {
        HandlePrefabSwitch();

        if (!RaycastToGrid(out int x, out int z))
        {
            SetPreviewVisible(false);
            return;
        }

        SetPreviewVisible(true);
        MovePreview(x, z);

        bool occupied = GridManager.Instance.GetCell(x, z)?.IsOccupied ?? true;
        TintPreview(occupied ? ColorNg : ColorOk);

        if (Input.GetMouseButtonDown(0))   // 左クリック → 配置
            GridManager.Instance.TryPlace(x, z, placementPrefabs[selectedIndex]);

        if (Input.GetMouseButtonDown(1))   // 右クリック → 削除
            GridManager.Instance.TryRemove(x, z);
    }

    // ---- プレハブ切り替え ----

    void HandlePrefabSwitch()
    {
        for (int i = 0; i < placementPrefabs.Length && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                selectedIndex = i;
                SpawnPreview();
            }
        }
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
        if (placementPrefabs == null || placementPrefabs.Length == 0) return;

        previewObject = Instantiate(placementPrefabs[selectedIndex]);

        // コライダーをオフにしてレイキャストに干渉させない
        foreach (var col in previewObject.GetComponentsInChildren<Collider>())
            col.enabled = false;

        // 物理演算もオフ
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
        {
            // マテリアルを複製してシーンのアセットを汚さない
            r.material.color = color;
        }
    }

    void OnDestroy()
    {
        if (previewObject != null) Destroy(previewObject);
    }
}
