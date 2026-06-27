/// <summary>
/// ゲーム内アイテムの識別子。InventoryManager の Dictionary キーとして使う。
/// </summary>
public enum ItemId
{
    None          = 0,

    // 素材
    Wood          = 1,
    Stone         = 2,

    // 農業
    VegetableSeed = 10,
    Vegetable     = 11,

    // 鉄道
    Track         = 20,

    // 建物（BuildingData.requiredItem で各建物と紐付ける）
    House         = 30,
    Workshop      = 31,
    Storage       = 32,
    Decoration    = 33,
    TradePost     = 40,   // 交易所
}
