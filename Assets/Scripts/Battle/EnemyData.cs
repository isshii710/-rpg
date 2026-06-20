using UnityEngine;

/// <summary>
/// 敵1種類分の定義データ（ScriptableObject）。
/// 作り方: Project右クリック → Create → RPG → Enemy Data
/// </summary>
[CreateAssetMenu(fileName = "NewEnemy", menuName = "RPG/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("基本情報")]
    public string enemyName = "スライム";

    [Header("ステータス")]
    public int maxHp        = 20;
    public int attackPower  =  5;
    public int defense      =  2;

    [Header("ドロップ報酬")]
    public int goldReward   =  5;
    public int expReward    = 10;

    [Header("行動パターン")]
    [Tooltip("プレイヤーへの攻撃間隔（秒）")]
    public float attackInterval = 2f;

    [Header("見た目（省略時はデバッグ用カプセルで代用）")]
    public GameObject prefab;
}
