using UnityEngine;
using UnityEditor;

/// <summary>
/// メニュー「RPG → 村をセットアップ」を実行すると
/// 武器屋・防具屋・宿屋・村人を自動配置する。
///
/// NPC 一覧:
///   武器屋のゴード (2, 0.5, 8) 青  : E キーで武器屋を開く
///   防具屋のマーラ (4, 0.5, 8) 緑  : E キーで防具屋を開く
///   宿屋のヤルン   (6, 0.5, 8) 橙  : E キーで 50G 宿泊 → 全回復
///   農夫のポーラ   (3, 0.5, 5) 紫  : E キーで台詞 / ウロウロ
///   見張りのカルロ (7, 0.5, 5) 灰  : E キーで台詞 / 固定
/// </summary>
public static class VillageSetup
{
    [MenuItem("RPG/村をセットアップ")]
    static void Setup()
    {
        // ── 商人・宿屋 ──
        Shopkeeper("武器屋のゴード", NpcShopType.Weapon,
            new Color(0.25f, 0.50f, 1.00f), new Vector3(2f, 0.5f, 8f));

        Shopkeeper("防具屋のマーラ", NpcShopType.Armor,
            new Color(0.20f, 0.80f, 0.45f), new Vector3(4f, 0.5f, 8f));

        Shopkeeper("宿屋のヤルン", NpcShopType.Inn,
            new Color(0.95f, 0.60f, 0.10f), new Vector3(6f, 0.5f, 8f));

        // ── 村人 ──
        Villager("農夫のポーラ", new Vector3(3f, 0.5f, 5f), wander: true,
            "今年の野菜は豊作だといいな！",
            "種を畑に植えるといいよ。育ったら収穫して売れるよ。",
            "東の岩場では石材が採れるよ。");

        Villager("見張りのカルロ", new Vector3(7f, 0.5f, 5f), wander: false,
            "村の外には魔物がいる。修行を積んでから旅立て！",
            "3つの修行（農業・採掘・戦闘）を終えると旅に出られる。",
            "南の草むらには訓練用のカカシがいる。戦ってみろ！");

        // ── UI システムを Managers に追加 ──
        AddUISystems();

        EditorSceneManager_MarkDirty();
        Debug.Log("[VillageSetup] ✅ 村をセットアップしました！ Ctrl+S で保存 → Play。");
        Debug.Log("[VillageSetup]   武器屋 / 防具屋 / 宿屋 に近づいて E キーで会話・購入。");
    }

    // ================================================================
    //  NPC 生成ヘルパー
    // ================================================================

    static void Shopkeeper(string npcName, NpcShopType shopType, Color color, Vector3 pos)
    {
        var go = MakeCapsule(npcName, color, pos);
        var npc = go.AddComponent<NpcController>();
        var so  = new SerializedObject(npc);
        so.FindProperty("npcName").stringValue      = npcName;
        so.FindProperty("shopType").enumValueIndex  = (int)shopType;
        so.FindProperty("canWander").boolValue      = false;
        so.FindProperty("dialogueLines").arraySize  = 0;
        so.ApplyModifiedProperties();
    }

    static void Villager(string npcName, Vector3 pos, bool wander, params string[] lines)
    {
        var go  = MakeCapsule(npcName, new Color(0.60f, 0.40f, 0.80f), pos);
        var npc = go.AddComponent<NpcController>();
        var so  = new SerializedObject(npc);
        so.FindProperty("npcName").stringValue     = npcName;
        so.FindProperty("shopType").enumValueIndex = (int)NpcShopType.None;
        so.FindProperty("canWander").boolValue     = wander;

        var dlProp = so.FindProperty("dialogueLines");
        dlProp.arraySize = lines.Length;
        for (int i = 0; i < lines.Length; i++)
            dlProp.GetArrayElementAtIndex(i).stringValue = lines[i];

        so.ApplyModifiedProperties();
    }

    static GameObject MakeCapsule(string npcName, Color color, Vector3 pos)
    {
        // 既存を削除
        var existing = GameObject.Find(npcName);
        if (existing != null) Object.DestroyImmediate(existing);

        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = npcName;
        go.transform.position = pos;
        go.GetComponent<Renderer>().material.color = color;

        // コライダー無効（GridPlacer のレイキャストに干渉しないように）
        var col = go.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        return go;
    }

    // ================================================================
    //  UI システムを Managers に追加
    // ================================================================

    static void AddUISystems()
    {
        var managers = GameObject.Find("Managers");
        if (managers == null)
        {
            Debug.LogWarning("[VillageSetup] 'Managers' が見つかりません。" +
                             "先に RPG → シーンをセットアップ を実行してください。");
            return;
        }

        if (GameObject.Find("NpcShopUI") == null)
        {
            var go = new GameObject("NpcShopUI");
            go.transform.parent = managers.transform;
            go.AddComponent<NpcShopUI>();
        }

        if (GameObject.Find("NpcDialogueUI") == null)
        {
            var go = new GameObject("NpcDialogueUI");
            go.transform.parent = managers.transform;
            go.AddComponent<NpcDialogueUI>();
        }
    }

    static void EditorSceneManager_MarkDirty()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
