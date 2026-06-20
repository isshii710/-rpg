using UnityEngine;

/// <summary>
/// 建物1種類分の定義データ。ScriptableObjectなのでプロジェクトアセットとして保存できる。
/// 作り方：Project右クリック → Create → RPG → Building Data
/// </summary>
[CreateAssetMenu(fileName = "NewBuilding", menuName = "RPG/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("基本情報")]
    public string buildingName = "建物";
    public BuildingType type   = BuildingType.Residential;

    [Header("見た目")]
    public GameObject prefab;

    [Header("占有サイズ（マス数）")]
    [Min(1)] public int sizeX = 1;  // 東西方向
    [Min(1)] public int sizeZ = 1;  // 南北方向

    [Header("インベントリ連携")]
    [Tooltip("配置時に消費するアイテム。None なら消費なし。")]
    public ItemId requiredItem = ItemId.None;
}
