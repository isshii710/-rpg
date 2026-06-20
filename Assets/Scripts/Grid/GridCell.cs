using UnityEngine;

/// <summary>
/// グリッドの1マス分のデータ。
/// 建物の有無・畑かどうか・作物の状態をまとめて管理する。
/// </summary>
public class GridCell
{
    public int X { get; }
    public int Z { get; }

    // ---- 通常の建物配置 ----
    public GameObject PlacedObject { get; private set; }
    public bool IsOccupied => PlacedObject != null;

    // ---- 農業フィールド ----
    public bool IsFarmland { get; private set; }
    public CropStage CropStage { get; private set; }
    public GameObject CropObject { get; private set; }
    public bool HasCrop => CropStage != CropStage.None;

    public GridCell(int x, int z)
    {
        X = x;
        Z = z;
    }

    // ---- 通常配置メソッド ----

    public void Place(GameObject obj) => PlacedObject = obj;

    /// <summary>建物を取り除く。畑フラグと作物も同時にリセットする。</summary>
    public void Clear()
    {
        PlacedObject = null;
        IsFarmland = false;
        ClearCrop();
    }

    // ---- 農業メソッド ----

    public void SetFarmland(bool value) => IsFarmland = value;

    /// <summary>種を植える。CropStageをSeedに設定する。</summary>
    public void PlantSeed(GameObject seedObj)
    {
        CropStage = CropStage.Seed;
        CropObject = seedObj;
    }

    /// <summary>次の成長段階へ進める。FarmManagerのコルーチンから呼ぶ。</summary>
    public void AdvanceTo(CropStage stage, GameObject newObj)
    {
        CropStage = stage;
        CropObject = newObj;
    }

    /// <summary>作物を取り除いて畑を空にする（収穫・撤去のどちらでも使う）。</summary>
    public void ClearCrop()
    {
        if (CropObject != null)
        {
            Object.Destroy(CropObject);
            CropObject = null;
        }
        CropStage = CropStage.None;
    }
}
