using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 交易所（TradePost）と列車を連携させた貿易システムを管理するシングルトン。
///
/// 【貿易フロー】
///   1. 線路の終点の隣のマスに TradePost 建物を置く
///   2. TrainController が終点に到着すると OnTrainArrived() を呼ぶ
///   3. 終点の隣接4マスに TradePost があれば、レシピ順にアイテムを自動交換する
///
/// 【Inspector での設定】
///   空の GameObject に TradeManager.cs をアタッチし、
///   Recipes リストに交易レシピを追加する。
///   例）野菜3個 → 石材1個 / 石材2個 → ゴールド30G
/// </summary>
public class TradeManager : MonoBehaviour
{
    public static TradeManager Instance { get; private set; }

    [Header("交易レシピ（列車が交易所に到着すると上から順に実行）")]
    [SerializeField] List<TradeRecipe> recipes = new();

    static readonly int[] Dx = {  0, 0, 1, -1 };
    static readonly int[] Dz = {  1, -1, 0, 0 };

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ================================================================
    //  列車到着通知（TrainController から呼ばれる）
    // ================================================================

    /// <summary>
    /// 列車が (x, z) の終点に到着したとき TrainController が呼ぶ。
    /// 隣接マスに TradePost 建物があればレシピを実行する。
    /// </summary>
    public void OnTrainArrived(int x, int z)
    {
        bool foundPost = false;
        for (int i = 0; i < 4; i++)
        {
            if (BuildingManager.Instance != null &&
                BuildingManager.Instance.IsTradePost(x + Dx[i], z + Dz[i]))
            {
                foundPost = true;
                break;
            }
        }

        if (!foundPost) return;

        Debug.Log($"[Trade] 列車が({x},{z})に到着！ 交易開始...");

        int count = 0;
        foreach (var r in recipes)
            if (TryExecuteRecipe(r)) count++;

        Debug.Log(count > 0
            ? $"[Trade] {count}件の交易を完了しました。"
            : "[Trade] 交易できるアイテムが足りませんでした。");
    }

    // ================================================================
    //  レシピ実行
    // ================================================================

    bool TryExecuteRecipe(TradeRecipe r)
    {
        if (InventoryManager.Instance == null) return false;
        if (!InventoryManager.Instance.HasItem(r.inputItem, r.inputCount)) return false;

        InventoryManager.Instance.ConsumeItem(r.inputItem, r.inputCount);
        InventoryManager.Instance.AddItem(r.outputItem, r.outputCount);

        Debug.Log($"[Trade] ✓ {r.label}: {r.inputItem}×{r.inputCount} → {r.outputItem}×{r.outputCount}");
        return true;
    }

    // ================================================================
    //  デバッグ
    // ================================================================

    [ContextMenu("交易レシピをログ出力")]
    public void PrintRecipes()
    {
        Debug.Log("=== 交易レシピ ===");
        foreach (var r in recipes)
            Debug.Log($"  {r.label}: {r.inputItem}×{r.inputCount} → {r.outputItem}×{r.outputCount}");
    }
}
