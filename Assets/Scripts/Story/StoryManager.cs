using UnityEngine;

/// <summary>
/// ストーリー進行フラグと修行クエストを管理するシングルトン。
///
/// 【修行クエスト一覧】
///   1. 農業の修行  ── 野菜を N 個収穫する（FarmManager.TryHarvest から通知）
///   2. 採掘の修行  ── 石材を N 個手に入れる（ResourceManager から通知）
///   3. 戦闘の修行  ── 訓練カカシを 1 体倒す（BattleManager から通知）
///
/// 【旅立ちフラグ】
///   CanLeaveVillage が false の間は PlayerController が荒野マスへの移動をブロックする。
///   3 つすべて完了すると CanLeaveVillage = true になりゲートが開く。
///
/// 【Unity での設定】
///   空の GameObject に StoryManager.cs をアタッチし、Inspector で以下を設定する。
///   - Vegetable Required : 野菜の収穫目標数（デフォルト 3）
///   - Stone Required     : 石材の採掘目標数（デフォルト 3）
///   - Scarecrow Data     : 訓練カカシの EnemyData（isTutorialEnemy=true にしたもの）
///   - Scarecrow X / Z    : カカシを置くグリッド座標（村の内側を指定）
/// </summary>
public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance { get; private set; }

    // ---- Inspector 設定 ----

    [Header("修行クエストの目標数")]
    [SerializeField] int vegetableRequired = 3;
    [SerializeField] int stoneRequired     = 3;

    [Header("訓練カカシ（isTutorialEnemy=true の EnemyData）")]
    [SerializeField] EnemyData scarecrowData;
    [SerializeField] int scarecrowX = 4;
    [SerializeField] int scarecrowZ = 4;

    // ---- クエスト進捗 ----

    QuestStep harvestQuest;
    QuestStep miningQuest;
    QuestStep combatQuest;

    /// <summary>true になったら荒野への移動が解放される。</summary>
    public bool CanLeaveVillage { get; private set; } = false;

    // ================================================================
    //  初期化
    // ================================================================

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        harvestQuest = new QuestStep($"農業の修行：野菜を {vegetableRequired} 個収穫する", vegetableRequired);
        miningQuest  = new QuestStep($"採掘の修行：石材を {stoneRequired} 個手に入れる",  stoneRequired);
        combatQuest  = new QuestStep("戦闘の修行：訓練用カカシを 1 体倒す",               1);
    }

    void Start()
    {
        // カカシを村の内側に自動配置
        if (scarecrowData != null && BattleManager.Instance != null)
        {
            BattleManager.Instance.SpawnEnemy(scarecrowData, scarecrowX, scarecrowZ);
            Debug.Log($"[Story] 訓練カカシを ({scarecrowX},{scarecrowZ}) に配置しました。");
        }

        Debug.Log("[Story] ── 修行の旅、始まり ──");
        PrintQuestStatus();
    }

    // ================================================================
    //  各システムから呼ばれる通知メソッド
    // ================================================================

    /// <summary>FarmManager.TryHarvest() の成功後に呼ぶ。</summary>
    public void OnVegetableHarvested()
    {
        if (harvestQuest.Advance()) CheckAllComplete();
    }

    /// <summary>ResourceManager で Stone を入手したときに呼ぶ。</summary>
    public void OnStoneMined()
    {
        if (miningQuest.Advance()) CheckAllComplete();
    }

    /// <summary>BattleManager.DefeatEnemy() の後に呼ぶ。</summary>
    public void OnEnemyDefeated(EnemyData data)
    {
        if (data != null && data.isTutorialEnemy)
            if (combatQuest.Advance()) CheckAllComplete();
    }

    // ================================================================
    //  完了チェック・旅立ち解放
    // ================================================================

    void CheckAllComplete()
    {
        if (!harvestQuest.IsComplete || !miningQuest.IsComplete || !combatQuest.IsComplete)
            return;

        CanLeaveVillage = true;

        Debug.Log("[Story] ══════════════════════════════════════════");
        Debug.Log("[Story]   修行完了！外の世界へ旅立とう！");
        Debug.Log("[Story]   拉致された仲間を必ず助け出せ！");
        Debug.Log("[Story] ══════════════════════════════════════════");
    }

    // ================================================================
    //  ゲートブロック時に PlayerController から呼ばれる
    // ================================================================

    /// <summary>村の出口でブロックされたとき、未完了クエストを表示する。</summary>
    public void NotifyBlockedAtGate()
    {
        Debug.Log("[Story] 村の外へ出るにはまず3つの修行を完了させなさい！");
        PrintQuestStatus();
    }

    // ================================================================
    //  デバッグ
    // ================================================================

    [ContextMenu("クエスト状況をログ出力")]
    public void PrintQuestStatus()
    {
        string mark(bool ok) => ok ? "✓" : "・";
        Debug.Log("─── 修行クエスト状況 ───────────────────");
        Debug.Log($"  [{mark(harvestQuest.IsComplete)}] {harvestQuest.title}: {harvestQuest.current}/{harvestQuest.required}");
        Debug.Log($"  [{mark(miningQuest.IsComplete)}]  {miningQuest.title}: {miningQuest.current}/{miningQuest.required}");
        Debug.Log($"  [{mark(combatQuest.IsComplete)}]  {combatQuest.title}: {combatQuest.current}/{combatQuest.required}");
        Debug.Log($"  旅立ち: {(CanLeaveVillage ? "解放済み ✓" : "修行中...")}");
        Debug.Log("──────────────────────────────────────");
    }

    [ContextMenu("すべての修行を完了させる（デバッグ）")]
    void DebugCompleteAll()
    {
        while (!harvestQuest.IsComplete) harvestQuest.Advance();
        while (!miningQuest.IsComplete)  miningQuest.Advance();
        while (!combatQuest.IsComplete)  combatQuest.Advance();
        CheckAllComplete();
    }
}
