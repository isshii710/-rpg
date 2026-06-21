using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// エンカウント・戦闘・報酬を一括管理するシングルトン。
///
/// 【エンカウントの流れ】
///   PlayerController が荒野マスに移動
///   → OnPlayerMoved(x, z) を呼ぶ
///   → 確率判定でパスするとその周囲に敵をスポーン
///
/// 【戦闘の流れ】
///   GridPlacer で敵マスを左クリック
///   → PlayerAttack(x, z) を呼ぶ
///   → 敵HP 0 → DefeatEnemy → InventoryManager にゴールド・PartyManager に経験値
///
/// 【村の範囲設定】
///   Inspector で Village Min/Max を設定する。
///   範囲外のグリッドマスが「荒野（危険エリア）」として扱われる。
///   デフォルトは 10×10 グリッドの中心 6×6（座標1〜8）を村とする。
/// </summary>
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("村の境界（これより外が荒野エリア）")]
    [SerializeField] int villageMinX = 1;
    [SerializeField] int villageMaxX = 8;
    [SerializeField] int villageMinZ = 1;
    [SerializeField] int villageMaxZ = 8;

    [Header("荒野に出現する敵のリスト（Inspector でアサイン）")]
    [SerializeField] EnemyData[] wildernessEnemies;

    [Header("エンカウント設定")]
    [Tooltip("荒野マスに入ったとき敵がスポーンする確率（0〜1）")]
    [SerializeField][Range(0f, 1f)] float encounterChance = 0.35f;

    // (gridX, gridZ) → EnemyController の高速検索テーブル
    readonly Dictionary<(int, int), EnemyController> enemyMap = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ================================================================
    //  ゾーン判定
    // ================================================================

    public bool IsWilderness(int x, int z) =>
        x < villageMinX || x > villageMaxX || z < villageMinZ || z > villageMaxZ;

    // ================================================================
    //  エンカウント（PlayerController から呼ばれる）
    // ================================================================

    /// <summary>プレイヤーが指定マスに移動したときに呼ぶ。荒野なら確率で敵をスポーン。</summary>
    public void OnPlayerMoved(int x, int z)
    {
        if (!IsWilderness(x, z)) return;
        if (wildernessEnemies == null || wildernessEnemies.Length == 0) return;
        if (Random.value > encounterChance) return;

        EnemyData data = wildernessEnemies[Random.Range(0, wildernessEnemies.Length)];
        TrySpawnEnemyNear(x, z, data);
    }

    void TrySpawnEnemyNear(int px, int pz, EnemyData data)
    {
        // 周囲4方向をランダムな順で探索し、空きマスにスポーン
        int[] dx = { 0, 0, 1, -1 };
        int[] dz = { 1, -1, 0, 0 };
        int start = Random.Range(0, 4);

        for (int i = 0; i < 4; i++)
        {
            int j  = (start + i) % 4;
            int nx = px + dx[j];
            int nz = pz + dz[j];

            if (!GridManager.Instance.IsValid(nx, nz)) continue;
            if (!IsWilderness(nx, nz)) continue;

            GridCell cell = GridManager.Instance.GetCell(nx, nz);
            if (cell.IsOccupied) continue;

            SpawnEnemy(data, nx, nz);
            return;
        }
    }

    public void SpawnEnemy(EnemyData data, int x, int z)
    {
        Vector3 pos = GridManager.Instance.GetWorldPosition(x, z) + Vector3.up * 0.5f;

        GameObject obj = data.prefab != null
            ? Instantiate(data.prefab, pos, Quaternion.identity)
            : CreateDebugEnemy(pos, data.enemyName);

        var ctrl = obj.GetComponent<EnemyController>() ?? obj.AddComponent<EnemyController>();
        ctrl.Initialize(data, x, z);

        GridManager.Instance.GetCell(x, z).Place(obj);
        enemyMap[(x, z)] = ctrl;

        Debug.Log($"[Battle] ★ {data.enemyName} が出現！ ({x},{z})  HP:{data.maxHp}");
    }

    // ================================================================
    //  プレイヤーが攻撃（GridPlacer から呼ばれる）
    // ================================================================

    public bool HasEnemyAt(int x, int z) => enemyMap.ContainsKey((x, z));

    /// <summary>プレイヤーが (x, z) の敵を攻撃する。敵がいなければ false を返す。</summary>
    public bool PlayerAttack(int x, int z)
    {
        if (!enemyMap.TryGetValue((x, z), out var enemy)) return false;

        int atk   = PartyManager.Instance?.GetLeaderAttack() ?? 10;
        bool died = enemy.TakeDamage(atk);

        if (died) DefeatEnemy(enemy, x, z);
        return true;
    }

    void DefeatEnemy(EnemyController enemy, int x, int z)
    {
        Debug.Log($"[Battle] {enemy.Data.enemyName} を倒した！  +{enemy.Data.goldReward}G  +{enemy.Data.expReward}EXP");

        // 報酬付与
        InventoryManager.Instance?.AddGold(enemy.Data.goldReward);
        PartyManager.Instance?.AddExperience(enemy.Data.expReward);
        StoryManager.Instance?.OnEnemyDefeated(enemy.Data);

        // グリッドからオブジェクトを除去
        GridCell cell = GridManager.Instance.GetCell(x, z);
        if (cell?.PlacedObject != null) Destroy(cell.PlacedObject);
        cell?.ClearPlacedObject();

        enemyMap.Remove((x, z));
    }

    // ================================================================
    //  敵がパーティを攻撃（EnemyController のコルーチンから呼ばれる）
    // ================================================================

    public void EnemyAttackParty(EnemyController enemy)
    {
        Debug.Log($"[Battle] {enemy.Data.enemyName} の攻撃！ → パーティに {enemy.Data.attackPower}ダメージ");
        PartyManager.Instance?.TakeDamage(enemy.Data.attackPower);
    }

    // ================================================================
    //  デバッグ用プリミティブ（プレハブ未設定時）
    // ================================================================

    static GameObject CreateDebugEnemy(Vector3 pos, string label)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        obj.name = label;
        obj.transform.position   = pos;
        obj.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        obj.GetComponent<Renderer>().material.color = new Color(1f, 0.25f, 0.25f);
        if (obj.TryGetComponent<Collider>(out var col)) col.enabled = false;
        return obj;
    }

    // ================================================================
    //  デバッグ（Inspector 右クリック）
    // ================================================================

    [ContextMenu("スライムを (0,0) にスポーン（テスト）")]
    void DebugSpawnSlime()
    {
        if (wildernessEnemies == null || wildernessEnemies.Length == 0)
        { Debug.LogWarning("[Battle] Wilderness Enemies が未設定です"); return; }
        SpawnEnemy(wildernessEnemies[0], 0, 0);
    }
}
