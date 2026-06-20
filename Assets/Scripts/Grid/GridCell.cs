using UnityEngine;

/// <summary>
/// グリッドの1マス分のデータ。
/// 将来的に「作物の成長段階」「線路の向き」などをここに追加していく。
/// </summary>
public class GridCell
{
    public int X { get; }
    public int Z { get; }
    public GameObject PlacedObject { get; private set; }
    public bool IsOccupied => PlacedObject != null;

    public GridCell(int x, int z)
    {
        X = x;
        Z = z;
    }

    public void Place(GameObject obj)
    {
        PlacedObject = obj;
    }

    public void Clear()
    {
        PlacedObject = null;
    }
}
