using UnityEngine;

/// <summary>
/// グリッド上の採掘リソース（岩・木）の配置・採掘・ドロップを管理するシングルトン。
///
/// 【採掘フロー】
///   GridPlacer がリソースマスを左クリック
///   → TryMine(x, z) を呼ぶ
///   → HP が減り 0 になったら Destroy ＋ InventoryManager にアイテムを追加
///
/// 【マップへの配置方法】
///   スクリプトから呼ぶ例：
///     ResourceManager.Instance.TryPlaceResource(3, 4, ResourceType.Rock);
///   あるいは Inspector 右クリック → 「岩を (0,0) に配置（テスト）」で動作確認できる。
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("リソースのプレハブ（省略時はデバッグ球で代用）")]
    [SerializeField] GameObject rockPrefab;
    [SerializeField] GameObject treePrefab;

    [Header("各リソースの最大HP")]
    [SerializeField] int rockMaxHp = 3;
    [SerializeField] int treeMaxHp = 2;

    [Header("1クリックで与えるダメージ")]
    [SerializeField] int damagePerHit = 1;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ================================================================
    //  配置
    // ================================================================

    /// <summary>指定マスにリソースを配置する。</summary>
    public bool TryPlaceResource(int x, int z, ResourceType type)
    {
        GridCell cell = GridManager.Instance.GetCell(x, z);
        if (cell == null || cell.IsOccupied) return false;

        int maxHp = type == ResourceType.Rock ? rockMaxHp : treeMaxHp;

        // プレハブがなければプリミティブで代用（テスト用）
        GameObject prefab = type == ResourceType.Rock ? rockPrefab : treePrefab;
        Vector3 pos = GridManager.Instance.GetWorldPosition(x, z) + Vector3.up * 0.5f;
        GameObject obj = prefab != null
            ? Instantiate(prefab, pos, Quaternion.identity)
            : CreateDebugMesh(pos, type);

        cell.Place(obj);
        cell.SetResource(type, maxHp);

        Debug.Log($"[ResourceManager] ({x},{z}) に {type}(HP:{maxHp}) を配置");
        return true;
    }

    // ================================================================
    //  採掘
    // ================================================================

    /// <summary>
    /// リソースマスを1回採掘する。
    /// HP が 0 になると破壊してドロップアイテムをインベントリへ追加する。
    /// 戻り値：採掘に成功した場合 true（HP が残っていても true）。
    /// </summary>
    public bool TryMine(int x, int z)
    {
        GridCell cell = GridManager.Instance.GetCell(x, z);
        if (cell == null || !cell.IsResource) return false;

        cell.DamageResource(damagePerHit);
        Debug.Log($"[ResourceManager] ({x},{z}) {cell.ResourceType} HP: {cell.ResourceHp}/{cell.ResourceMaxHp}");

        if (cell.ResourceHp <= 0)
            BreakResource(cell, x, z);

        return true;
    }

    void BreakResource(GridCell cell, int x, int z)
    {
        ResourceType type = cell.ResourceType;
        ItemId drop      = type == ResourceType.Rock ? ItemId.Stone : ItemId.Wood;

        // 見た目を破棄
        if (cell.PlacedObject != null) Destroy(cell.PlacedObject);
        cell.ClearResource();

        // インベントリにドロップ
        InventoryManager.Instance?.AddItem(drop);
        if (drop == ItemId.Stone) StoryManager.Instance?.OnStoneMined();

        Debug.Log($"[ResourceManager] ({x},{z}) {type} を破壊！ → {drop} を入手");
    }

    // ================================================================
    //  デバッグ用メソッド（Inspector 右クリックで呼べる）
    // ================================================================

    [ContextMenu("岩を (0,0) に配置（テスト）")]
    void DebugPlaceRock() => TryPlaceResource(0, 0, ResourceType.Rock);

    [ContextMenu("木を (1,0) に配置（テスト）")]
    void DebugPlaceTree() => TryPlaceResource(1, 0, ResourceType.Tree);

    // ================================================================
    //  プレハブ未設定時のデバッグ用メッシュ
    // ================================================================

    static GameObject CreateDebugMesh(Vector3 pos, ResourceType type)
    {
        PrimitiveType prim = type == ResourceType.Rock ? PrimitiveType.Sphere : PrimitiveType.Cylinder;
        GameObject obj = GameObject.CreatePrimitive(prim);
        obj.transform.position = pos;
        obj.transform.localScale = Vector3.one * 0.7f;

        var mat = obj.GetComponent<Renderer>().material;
        mat.color = type == ResourceType.Rock ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.3f, 0.6f, 0.2f);

        // コライダーは物理干渉しないよう無効化
        if (obj.TryGetComponent<Collider>(out var col)) col.enabled = false;
        return obj;
    }
}
