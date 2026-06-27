using UnityEngine;

/// <summary>
/// 交易所で1回の取引に使うレシピ1件。
/// TradeManager の Inspector リストに登録して使う。
/// 例）野菜3個 → 石材1個 / 石材2個 → 金貨30G
/// </summary>
[System.Serializable]
public class TradeRecipe
{
    [Tooltip("ログに表示される説明文（例: 野菜→石材）")]
    public string label;

    [Header("消費（村から持ち込む）")]
    public ItemId inputItem;
    [Min(1)] public int inputCount = 1;

    [Header("獲得（隣の町から受け取る）")]
    public ItemId outputItem;
    [Min(1)] public int outputCount = 1;
}
