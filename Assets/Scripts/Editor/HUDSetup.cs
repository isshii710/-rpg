using UnityEngine;
using UnityEditor;

/// <summary>
/// メニュー「RPG → HUDを作成」を実行すると
/// HUDManager GameObject をシーンに追加する。
/// OnGUI ベースなので Canvas・パッケージ不要。
/// </summary>
public static class HUDSetup
{
    [MenuItem("RPG/HUDを作成")]
    static void CreateHUD()
    {
        // 既存を削除して再生成
        var existing = GameObject.Find("HUDManager");
        if (existing != null) Object.DestroyImmediate(existing);

        var go = new GameObject("HUDManager");
        go.AddComponent<HUDManager>();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Selection.activeGameObject = go;
        Debug.Log("[HUDSetup] HUD 作成完了！ Ctrl+S でシーンを保存して Play を押してください。");
    }
}
