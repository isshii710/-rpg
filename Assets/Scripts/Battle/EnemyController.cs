using System.Collections;
using UnityEngine;

/// <summary>
/// 敵1体を管理する MonoBehaviour。敵プレハブにアタッチする。
///
/// BattleManager.SpawnEnemy() から Initialize() を呼んで起動する。
/// 生存中は一定間隔でパーティを攻撃するコルーチンが走る。
/// </summary>
public class EnemyController : MonoBehaviour
{
    // ---- Inspectorで設定（プレハブにあらかじめデータをセットする場合） ----
    [SerializeField] EnemyData defaultData;

    public EnemyData Data       { get; private set; }
    public int        CurrentHp { get; private set; }
    public bool       IsAlive   => CurrentHp > 0;

    // グリッド座標（BattleManager が位置検索に使う）
    public int GridX { get; private set; }
    public int GridZ { get; private set; }

    Coroutine attackRoutine;

    // ================================================================
    //  初期化（BattleManager から呼ばれる）
    // ================================================================

    public void Initialize(EnemyData data, int x, int z)
    {
        Data      = data;
        CurrentHp = data.maxHp;
        GridX     = x;
        GridZ     = z;

        attackRoutine = StartCoroutine(AttackLoop());
    }

    // プレハブに defaultData がセットされていて Initialize が呼ばれない場合のフォールバック
    void Start()
    {
        if (Data == null && defaultData != null)
            Initialize(defaultData, 0, 0);
    }

    // ================================================================
    //  攻撃ループ（自動でパーティを攻撃し続ける）
    // ================================================================

    IEnumerator AttackLoop()
    {
        while (IsAlive)
        {
            yield return new WaitForSeconds(Data.attackInterval);
            if (!IsAlive) yield break;

            BattleManager.Instance?.EnemyAttackParty(this);
        }
    }

    // ================================================================
    //  ダメージを受ける（BattleManager から呼ばれる）
    // ================================================================

    /// <summary>
    /// ダメージを受ける。HP が 0 以下になったら true を返す。
    /// </summary>
    public bool TakeDamage(int rawDamage)
    {
        int actual = Mathf.Max(1, rawDamage - Data.defense);
        CurrentHp  = Mathf.Max(0, CurrentHp - actual);
        Debug.Log($"[Enemy] {Data.enemyName} -{actual}ダメージ！  HP:{CurrentHp}/{Data.maxHp}");
        return CurrentHp <= 0;
    }

    void OnDestroy()
    {
        if (attackRoutine != null) StopCoroutine(attackRoutine);
    }
}
