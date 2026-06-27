using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// メニュー「RPG → シーンをセットアップ」を実行すると、
/// ゲームに必要なすべての GameObject を自動生成するエディタツール。
/// </summary>
public static class GameSetup
{
    [MenuItem("RPG/シーンをセットアップ")]
    static void SetupScene()
    {
        // 既存セットアップを一掃（重複防止）
        foreach (var name in new[] { "Managers", "Player", "Ground" })
        {
            var existing = GameObject.Find(name);
            if (existing != null) Object.DestroyImmediate(existing);
        }

        // ================================================================
        //  マネージャー群
        // ================================================================

        var managers = new GameObject("Managers");

        // GridManager（グリッド描画込み）
        var gridGO = CreateChild(managers, "GridManager");
        gridGO.AddComponent<GridManager>();
        gridGO.AddComponent<GridVisualizer>();

        // FarmManager
        CreateChild(managers, "FarmManager").AddComponent<FarmManager>();

        // BuildingManager
        CreateChild(managers, "BuildingManager").AddComponent<BuildingManager>();

        // RailManager
        CreateChild(managers, "RailManager").AddComponent<RailManager>();

        // ResourceManager
        CreateChild(managers, "ResourceManager").AddComponent<ResourceManager>();

        // InventoryManager
        CreateChild(managers, "InventoryManager").AddComponent<InventoryManager>();

        // BattleManager
        CreateChild(managers, "BattleManager").AddComponent<BattleManager>();

        // PartyManager
        CreateChild(managers, "PartyManager").AddComponent<PartyManager>();

        // StoryManager
        CreateChild(managers, "StoryManager").AddComponent<StoryManager>();

        // TradeManager
        CreateChild(managers, "TradeManager").AddComponent<TradeManager>();

        // ================================================================
        //  プレイヤー（青いカプセル）
        // ================================================================

        var playerGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerGO.name = "Player";
        playerGO.transform.position = new Vector3(5.5f, 0.5f, 5.5f);
        playerGO.GetComponent<Renderer>().material.color = Color.blue;

        // コライダーはグリッドの当たり判定に干渉しないよう無効化
        var cap = playerGO.GetComponent<Collider>();
        if (cap != null) cap.enabled = false;

        playerGO.AddComponent<PlayerController>();

        // GridPlacer の groundLayer を Default に設定
        var gridPlacer = playerGO.AddComponent<GridPlacer>();
        var gpSO = new SerializedObject(gridPlacer);
        gpSO.FindProperty("groundLayer").intValue = LayerMask.GetMask("Default");
        gpSO.ApplyModifiedProperties();

        // ================================================================
        //  地面（Ground）― マウスのレイキャスト用
        // ================================================================

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        // グリッドは (0,0,0)〜(10,10) → Plane を中央 (5,0,5) に置くとぴったり覆う
        ground.transform.position  = new Vector3(5f, 0f, 5f);
        ground.transform.localScale = new Vector3(1f, 1f, 1f);
        // マテリアルを緑っぽく
        ground.GetComponent<Renderer>().material.color = new Color(0.4f, 0.6f, 0.3f);
        // GridPlacer がレイキャストする "Default" レイヤーのまま

        // ================================================================
        //  カメラ（斜め見下ろし 2.5D）
        // ================================================================

        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(5f, 12f, -2f);
            cam.transform.rotation = Quaternion.Euler(65f, 0f, 0f);
        }

        // シーンを「変更あり」にしてCtrl+Sで保存できるようにする
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        // Hierarchy で Managers を選択状態にする
        Selection.activeGameObject = managers;

        Debug.Log("[GameSetup] ✅ セットアップ完了！ Ctrl+S でシーンを保存して ▶ Play を押してください。");
    }

    static GameObject CreateChild(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.parent = parent.transform;
        return go;
    }
}
