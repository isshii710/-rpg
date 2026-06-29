using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーの所持アイテムと所持金を管理するシングルトン。
///
/// 【配置との連携フロー】
///   配置要求 → HasItem チェック → TryPlace → 成功したら ConsumeItem
///
/// 【ショップの使い方】
///   BuyItem (ItemId.Track, 5)  // 5本の線路を購入
///   SellItem(ItemId.Vegetable) // 野菜を1個売る
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("初期所持金（ゴールド）")]
    [SerializeField] int startGold = 500;

    [Header("テスト用：起動時に追加する初期アイテム")]
    [SerializeField] ItemId[] startItemIds    = { ItemId.Track, ItemId.VegetableSeed, ItemId.House };
    [SerializeField] int[]    startItemCounts = { 20,           10,                   3            };

    // ---- 内部データ ----
    int gold;
    readonly Dictionary<ItemId, int> items = new();

    public int Gold => gold;

    // ---- 価格表（買値 / 売値）----
    static readonly Dictionary<ItemId, (int buy, int sell)> PriceTable = new()
    {
        { ItemId.VegetableSeed, (buy:  10, sell:  5) },
        { ItemId.Vegetable,     (buy:   0, sell: 20) },  // 買えない・売れる
        { ItemId.Track,         (buy:  15, sell:  7) },
        { ItemId.Wood,          (buy:   8, sell:  4) },
        { ItemId.Stone,         (buy:  12, sell:  6) },
        { ItemId.House,         (buy: 100, sell: 50) },
        { ItemId.Workshop,      (buy: 120, sell: 60) },
        { ItemId.Storage,       (buy:  80, sell: 40) },
        { ItemId.Decoration,    (buy:  30, sell: 15) },
        // 装備・消耗品
        { ItemId.Sword,         (buy: 100, sell:  50) },
        { ItemId.Shield,        (buy:  80, sell:  40) },
        { ItemId.Armor,         (buy: 120, sell:  60) },
        { ItemId.Potion,        (buy:  20, sell:  10) },
    };

    // ================================================================
    //  初期化
    // ================================================================

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        gold = startGold;

        for (int i = 0; i < startItemIds.Length && i < startItemCounts.Length; i++)
            AddItem(startItemIds[i], startItemCounts[i]);
    }

    // ================================================================
    //  参照
    // ================================================================

    /// <summary>指定アイテムの所持数を返す。持っていなければ 0。</summary>
    public int GetCount(ItemId id) =>
        items.TryGetValue(id, out int n) ? n : 0;

    /// <summary>指定アイテムを quantity 個以上持っているか。</summary>
    public bool HasItem(ItemId id, int quantity = 1) =>
        GetCount(id) >= quantity;

    // ================================================================
    //  追加
    // ================================================================

    public void AddItem(ItemId id, int quantity = 1)
    {
        if (id == ItemId.None || quantity <= 0) return;
        items[id] = GetCount(id) + quantity;
        Debug.Log($"[Inventory] {id} +{quantity} → 計{items[id]}個");
    }

    public void AddGold(int amount)
    {
        gold += amount;
        Debug.Log($"[Inventory] Gold +{amount}G → {gold}G");
    }

    // ================================================================
    //  消費
    // ================================================================

    /// <summary>
    /// アイテムを quantity 個消費する。
    /// 成功 → true、足りない → false（消費しない）。
    /// </summary>
    public bool ConsumeItem(ItemId id, int quantity = 1)
    {
        if (id == ItemId.None) return true;  // None は消費不要

        if (!HasItem(id, quantity))
        {
            Debug.LogWarning($"[Inventory] {id} が不足 ({GetCount(id)}/{quantity}個)");
            return false;
        }

        items[id] -= quantity;
        Debug.Log($"[Inventory] {id} -{quantity} → 残り{items[id]}個");
        return true;
    }

    // ================================================================
    //  ショップ：購入
    // ================================================================

    /// <summary>
    /// アイテムを購入する。
    /// 価格表に買値が設定されていて、所持金が足りれば購入できる。
    /// </summary>
    public bool BuyItem(ItemId id, int quantity = 1)
    {
        if (!PriceTable.TryGetValue(id, out var price) || price.buy <= 0)
        {
            Debug.LogWarning($"[Shop] {id} は購入できません");
            return false;
        }

        int total = price.buy * quantity;
        if (gold < total)
        {
            Debug.LogWarning($"[Shop] お金が不足 ({gold}G / 必要{total}G)");
            return false;
        }

        gold -= total;
        AddItem(id, quantity);
        Debug.Log($"[Shop] {id}×{quantity} 購入 -{total}G → 残り{gold}G");
        return true;
    }

    // ================================================================
    //  ショップ：売却
    // ================================================================

    /// <summary>
    /// アイテムを売却する。
    /// 所持アイテムを消費し、売値×数量のゴールドを得る。
    /// </summary>
    public bool SellItem(ItemId id, int quantity = 1)
    {
        if (!PriceTable.TryGetValue(id, out var price) || price.sell <= 0)
        {
            Debug.LogWarning($"[Shop] {id} は売れません");
            return false;
        }

        if (!ConsumeItem(id, quantity)) return false;

        int earn = price.sell * quantity;
        gold += earn;
        Debug.Log($"[Shop] {id}×{quantity} 売却 +{earn}G → {gold}G");
        return true;
    }

    // ================================================================
    //  セーブ/ロード用API
    // ================================================================

    public void ClearForLoad()
    {
        gold = 0;
        items.Clear();
    }

    public void RestoreGold(int amount) => gold = amount;

    public void RestoreItem(ItemId id, int count)
    {
        if (id == ItemId.None || count <= 0) return;
        items[id] = count;
    }

    // ================================================================
    //  デバッグ
    // ================================================================

    [ContextMenu("インベントリをログ出力")]
    public void PrintInventory()
    {
        Debug.Log($"=== Inventory ===  所持金: {gold}G");
        foreach (var kv in items)
            if (kv.Value > 0)
                Debug.Log($"  {kv.Key} : {kv.Value}個");
    }
}
