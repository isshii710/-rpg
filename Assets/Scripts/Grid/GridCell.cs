using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// グリッドの1マス分のデータ。
/// 建物の有無・畑かどうか・作物の状態・複数マス建物の参照をまとめて管理する。
/// </summary>
public class GridCell
{
    public int X { get; }
    public int Z { get; }

    // ---- 通常配置 ----
    public GameObject PlacedObject { get; private set; }

    /// <summary>「PlacedObjectがある」または「複数マス建物のフットプリントに含まれる」なら占有中。</summary>
    public bool IsOccupied => PlacedObject != null || IsPartOfBuilding;

    // ---- 農業 ----
    public bool IsFarmland { get; private set; }
    public CropStage CropStage { get; private set; }
    public GameObject CropObject { get; private set; }
    public bool HasCrop => CropStage != CropStage.None;

    // ---- 複数マス建物 ----
    public bool IsPartOfBuilding => BuildingRoot != null;
    public GridCell BuildingRoot { get; private set; }

    List<GridCell> footprintCells;
    public IReadOnlyList<GridCell> FootprintCells => footprintCells;

    public GridCell(int x, int z)
    {
        X = x;
        Z = z;
    }

    // ---- 通常配置メソッド ----

    public void Place(GameObject obj) => PlacedObject = obj;

    /// <summary>すべての状態をリセットする（マス全体の完全クリア）。</summary>
    public void Clear()
    {
        PlacedObject = null;
        IsFarmland = false;
        ClearCrop();
        ClearBuilding();
    }

    // ---- 農業メソッド ----

    public void SetFarmland(bool value) => IsFarmland = value;

    public void PlantSeed(GameObject seedObj)
    {
        CropStage = CropStage.Seed;
        CropObject = seedObj;
    }

    public void AdvanceTo(CropStage stage, GameObject newObj)
    {
        CropStage = stage;
        CropObject = newObj;
    }

    public void ClearCrop()
    {
        if (CropObject != null)
        {
            Object.Destroy(CropObject);
            CropObject = null;
        }
        CropStage = CropStage.None;
    }

    // ---- 複数マス建物メソッド ----

    /// <summary>このマスが属する建物のルートセルを設定する（ルートセル自身も self を渡す）。</summary>
    public void SetBuildingRoot(GridCell root) => BuildingRoot = root;

    /// <summary>フットプリントリストをルートセルに登録する（ルートセルのみ呼ぶ）。</summary>
    public void RegisterFootprint(List<GridCell> cells) => footprintCells = cells;

    /// <summary>建物参照をクリアする。BuildingManager が Destroy 後に呼ぶ。</summary>
    public void ClearBuilding()
    {
        BuildingRoot = null;
        footprintCells = null;
    }

    /// <summary>PlacedObject 参照だけをクリアする。BuildingManager が Destroy 後に呼ぶ。</summary>
    public void ClearPlacedObject() => PlacedObject = null;
}
