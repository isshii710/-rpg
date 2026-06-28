using System.Collections;
using UnityEngine;

/// <summary>
/// 農業の成長ロジックを管理するシングルトン。
/// 種を植えると内部でコルーチンが走り、自動的に Seed→Sprout→Mature へ進む。
/// </summary>
public class FarmManager : MonoBehaviour
{
    public static FarmManager Instance { get; private set; }

    [Header("各成長段階のプレハブ")]
    [SerializeField] GameObject seedPrefab;
    [SerializeField] GameObject sproutPrefab;
    [SerializeField] GameObject maturePrefab;
    [SerializeField] GameObject farmlandPrefab;

    [Header("成長時間（秒）— テスト中は小さい値にすると確認しやすい")]
    [SerializeField] float seedToSproutSeconds   = 5f;
    [SerializeField] float sproutToMatureSeconds = 10f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ---- 外部から呼ぶAPI ----

    /// <summary>
    /// 畑マスに種を植える。
    /// 条件：IsFarmland が true かつ作物がまだない。
    /// </summary>
    public bool TryPlantSeed(int x, int z)
    {
        GridCell cell = GridManager.Instance.GetCell(x, z);
        if (cell == null || !cell.IsFarmland || cell.HasCrop) return false;

        // インベントリから種を1個消費（InventoryManager がいない場合はスキップ）
        if (InventoryManager.Instance != null &&
            !InventoryManager.Instance.ConsumeItem(ItemId.VegetableSeed))
            return false;

        Vector3 pos = GridManager.Instance.GetWorldPosition(x, z);
        GameObject seedObj = Instantiate(seedPrefab, pos, Quaternion.identity);
        cell.PlantSeed(seedObj);

        StartCoroutine(GrowCrop(x, z));
        return true;
    }

    /// <summary>
    /// 収穫可能（Mature）な作物を収穫する。
    /// 収穫後は畑が空になり、再び種を植えられる状態に戻る。
    /// </summary>
    public bool TryHarvest(int x, int z)
    {
        GridCell cell = GridManager.Instance.GetCell(x, z);
        if (cell == null || cell.CropStage != CropStage.Mature) return false;

        cell.ClearCrop();

        // インベントリに野菜を追加
        InventoryManager.Instance?.AddItem(ItemId.Vegetable);
        StoryManager.Instance?.OnVegetableHarvested();
        Debug.Log($"[FarmManager] 収穫成功！ マス({x},{z}) → 野菜をインベントリに追加");
        return true;
    }

    // ---- 内部：成長コルーチン ----

    IEnumerator GrowCrop(int x, int z)
    {
        GridCell cell = GridManager.Instance.GetCell(x, z);

        // --- Seed → Sprout ---
        yield return new WaitForSeconds(seedToSproutSeconds);

        // 待機中に作物が撤去されていたら中断
        if (cell == null || cell.CropStage != CropStage.Seed) yield break;

        SwapCropVisual(cell, sproutPrefab, CropStage.Sprout);

        // --- Sprout → Mature ---
        yield return new WaitForSeconds(sproutToMatureSeconds);

        if (cell == null || cell.CropStage != CropStage.Sprout) yield break;

        SwapCropVisual(cell, maturePrefab, CropStage.Mature);
        Debug.Log($"[FarmManager] マス({x},{z}) の作物が収穫可能になりました！");
    }

    /// <summary>現在の作物オブジェクトを破棄し、次の段階のプレハブに差し替える。</summary>
    void SwapCropVisual(GridCell cell, GameObject nextPrefab, CropStage nextStage)
    {
        if (cell.CropObject != null) Destroy(cell.CropObject);

        Vector3 pos = GridManager.Instance.GetWorldPosition(cell.X, cell.Z);
        GameObject newObj = Instantiate(nextPrefab, pos, Quaternion.identity);
        cell.AdvanceTo(nextStage, newObj);
    }

    // ---- セーブ/ロード用の復元API ----

    public bool RestoreFarmland(int x, int z)
    {
        if (farmlandPrefab == null) return false;
        var cell = GridManager.Instance.GetCell(x, z);
        if (cell == null || cell.IsOccupied) return false;
        var obj = Instantiate(farmlandPrefab, GridManager.Instance.GetWorldPosition(x, z), Quaternion.identity);
        cell.Place(obj);
        cell.SetFarmland(true);
        return true;
    }

    public bool RestoreCrop(int x, int z, CropStage stage)
    {
        var cell = GridManager.Instance.GetCell(x, z);
        if (cell == null || !cell.IsFarmland || stage == CropStage.None) return false;
        GameObject prefab = stage switch
        {
            CropStage.Seed    => seedPrefab,
            CropStage.Sprout  => sproutPrefab,
            CropStage.Mature  => maturePrefab,
            _ => null,
        };
        if (prefab == null) return false;
        var obj = Instantiate(prefab, GridManager.Instance.GetWorldPosition(x, z), Quaternion.identity);
        cell.AdvanceTo(stage, obj);
        return true;
    }
}
