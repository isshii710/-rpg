/// <summary>
/// 建物のカテゴリ。
/// 将来「駅」「工場」「水路」などを追加するときはここに値を足す。
/// </summary>
public enum BuildingType
{
    Residential,  // 住宅
    Workshop,     // 作業場・工房
    Storage,      // 倉庫
    Decoration,   // 装飾物
    TradePost,    // 交易所（列車の終点隣に置くと自動交易が発動）
}
