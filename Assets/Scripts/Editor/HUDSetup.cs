using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// メニュー「RPG → HUD を作成」を実行すると
/// Canvas + Text 群を自動生成して HUDManager に接続する。
/// </summary>
public static class HUDSetup
{
    [MenuItem("RPG/HUDを作成")]
    static void CreateHUD()
    {
        // 既存 HUD Canvas を削除して再生成
        var existing = GameObject.Find("HUDCanvas");
        if (existing != null) Object.DestroyImmediate(existing);

        // ---- Canvas ----
        var canvasGO = new GameObject("HUDCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ---- HUDManager をアタッチ ----
        var hud   = canvasGO.AddComponent<HUDManager>();
        var hudSO = new SerializedObject(hud);

        // ---- ゴールド（右上） ----
        var goldText = CreateText(canvasGO, "GoldText", "G 0",
            TextAnchor.UpperRight,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-10f, -10f), new Vector2(220f, 40f),
            28, new Color(1f, 0.9f, 0.1f));

        // ---- クエスト（左上） ----
        var harvestText = CreateText(canvasGO, "HarvestQuestText", "[ ] 農業の修行",
            TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(10f, -10f), new Vector2(520f, 36f),
            20, Color.white);

        var miningText = CreateText(canvasGO, "MiningQuestText", "[ ] 採掘の修行",
            TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(10f, -50f), new Vector2(520f, 36f),
            20, Color.white);

        var combatText = CreateText(canvasGO, "CombatQuestText", "[ ] 戦闘の修行",
            TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(10f, -90f), new Vector2(520f, 36f),
            20, Color.white);

        var leaveText = CreateText(canvasGO, "LeaveStatusText", "旅立ち：修行中...",
            TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(10f, -130f), new Vector2(400f, 36f),
            20, new Color(0.6f, 1f, 0.6f));

        // ---- パーティ HP（左下） ----
        var partyText = CreateText(canvasGO, "PartyHPText", "",
            TextAnchor.LowerLeft,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(10f, 10f), new Vector2(340f, 130f),
            20, Color.white);

        // ---- 選択アイテム（下中央） ----
        var itemText = CreateText(canvasGO, "SelectedItemText", "[ ]",
            TextAnchor.LowerCenter,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 14f), new Vector2(300f, 44f),
            28, new Color(1f, 1f, 0.5f));

        // ---- SerializedObject でフィールドをアサイン ----
        hudSO.FindProperty("goldText").objectReferenceValue         = goldText;
        hudSO.FindProperty("harvestQuestText").objectReferenceValue = harvestText;
        hudSO.FindProperty("miningQuestText").objectReferenceValue  = miningText;
        hudSO.FindProperty("combatQuestText").objectReferenceValue  = combatText;
        hudSO.FindProperty("leaveStatusText").objectReferenceValue  = leaveText;
        hudSO.FindProperty("partyHPText").objectReferenceValue      = partyText;
        hudSO.FindProperty("selectedItemText").objectReferenceValue = itemText;
        hudSO.ApplyModifiedProperties();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Selection.activeGameObject = canvasGO;
        Debug.Log("[HUDSetup] HUD 作成完了！ Ctrl+S でシーンを保存して Play を押してください。");
    }

    // ================================================================
    //  ヘルパー
    // ================================================================

    static Text CreateText(
        GameObject parent,
        string name,
        string defaultText,
        TextAnchor anchor,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta,
        int fontSize, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var rect              = go.AddComponent<RectTransform>();
        rect.anchorMin        = anchorMin;
        rect.anchorMax        = anchorMax;
        rect.pivot            = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta        = sizeDelta;

        var txt           = go.AddComponent<Text>();
        txt.text          = defaultText;
        txt.fontSize      = fontSize;
        txt.alignment     = anchor;
        txt.color         = color;
        txt.raycastTarget = false;
        txt.font          = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return txt;
    }
}
