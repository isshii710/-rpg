using UnityEngine;
using UnityEditor;

/// <summary>
/// メニュー「RPG → 農業プレハブを作成」を実行すると
/// 畑・種・芽・作物のプレハブを自動生成して FarmManager と GridPlacer に設定する。
/// </summary>
public static class FarmingSetup
{
    [MenuItem("RPG/農業プレハブを作成")]
    static void CreateFarmingPrefabs()
    {
        // ---- フォルダ作成 ----
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Farming"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Farming");

        // ---- マテリアル作成 ----
        var farmlandMat = CreateMat("FarmlandMat", new Color(0.45f, 0.28f, 0.08f)); // 土色
        var seedMat     = CreateMat("SeedMat",     new Color(0.9f,  0.8f,  0.1f));  // 黄色
        var sproutMat   = CreateMat("SproutMat",   new Color(0.4f,  0.8f,  0.2f));  // 薄緑
        var matureMat   = CreateMat("MatureMat",   new Color(0.1f,  0.55f, 0.1f));  // 濃緑

        // ---- プレハブ作成 ----
        // 畑タイル：平たい茶色のキューブ
        var farmlandPrefab = CreatePrefab("Farmland", PrimitiveType.Cube,
            new Vector3(0.95f, 0.05f, 0.95f), 0.025f, farmlandMat,
            "Assets/Prefabs/Farming/Farmland.prefab");

        // 種：小さな黄色の球
        var seedPrefab = CreatePrefab("Seed", PrimitiveType.Sphere,
            new Vector3(0.15f, 0.15f, 0.15f), 0.15f, seedMat,
            "Assets/Prefabs/Farming/Seed.prefab");

        // 芽：細い薄緑シリンダー
        var sproutPrefab = CreatePrefab("Sprout", PrimitiveType.Cylinder,
            new Vector3(0.12f, 0.2f, 0.12f), 0.2f, sproutMat,
            "Assets/Prefabs/Farming/Sprout.prefab");

        // 成熟：太めの濃緑シリンダー
        var maturePrefab = CreatePrefab("Mature", PrimitiveType.Cylinder,
            new Vector3(0.25f, 0.35f, 0.25f), 0.35f, matureMat,
            "Assets/Prefabs/Farming/Mature.prefab");

        // ---- FarmManager にプレハブをアサイン ----
        var farmGO = GameObject.Find("FarmManager");
        if (farmGO != null)
        {
            var fm = farmGO.GetComponent<FarmManager>();
            if (fm != null)
            {
                var so = new SerializedObject(fm);
                so.FindProperty("seedPrefab").objectReferenceValue    = seedPrefab;
                so.FindProperty("sproutPrefab").objectReferenceValue  = sproutPrefab;
                so.FindProperty("maturePrefab").objectReferenceValue  = maturePrefab;
                so.FindProperty("farmlandPrefab").objectReferenceValue = farmlandPrefab;
                so.ApplyModifiedProperties();
                Debug.Log("[FarmingSetup] FarmManager にプレハブをセットしました。");
            }
        }
        else
        {
            Debug.LogWarning("[FarmingSetup] FarmManager が見つかりません。先に RPG → シーンをセットアップ を実行してください。");
        }

        // ---- GridPlacer にアイテムをセット ----
        // ItemType: Building=0, Farmland=1, Seed=2, Track=3
        var playerGO = GameObject.Find("Player");
        if (playerGO != null)
        {
            var gp = playerGO.GetComponent<GridPlacer>();
            if (gp != null)
            {
                var so        = new SerializedObject(gp);
                var itemsProp = so.FindProperty("items");
                itemsProp.arraySize = 2;

                // 1キー：畑タイル配置
                var item0 = itemsProp.GetArrayElementAtIndex(0);
                item0.FindPropertyRelative("label").stringValue               = "畑タイル";
                item0.FindPropertyRelative("type").enumValueIndex             = 1; // Farmland
                item0.FindPropertyRelative("prefab").objectReferenceValue     = farmlandPrefab;
                item0.FindPropertyRelative("buildingData").objectReferenceValue = null;

                // 2キー：種まき
                var item1 = itemsProp.GetArrayElementAtIndex(1);
                item1.FindPropertyRelative("label").stringValue               = "種";
                item1.FindPropertyRelative("type").enumValueIndex             = 2; // Seed
                item1.FindPropertyRelative("prefab").objectReferenceValue     = null;
                item1.FindPropertyRelative("buildingData").objectReferenceValue = null;

                so.ApplyModifiedProperties();
                Debug.Log("[FarmingSetup] GridPlacer に 畑(1キー)・種(2キー) をセットしました。");
            }
        }
        else
        {
            Debug.LogWarning("[FarmingSetup] Player が見つかりません。先に RPG → シーンをセットアップ を実行してください。");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[FarmingSetup] ✅ 完了！操作方法：1キー=畑配置、2キー=種まき、右クリック=収穫");
    }

    // ================================================================
    //  ヘルパー
    // ================================================================

    /// <summary>親 GameObject + ビジュアル子要素 の構造でプレハブを作る。</summary>
    static GameObject CreatePrefab(
        string objName, PrimitiveType prim,
        Vector3 scale, float yOffset,
        Material mat, string path)
    {
        var root   = new GameObject(objName);
        var visual = GameObject.CreatePrimitive(prim);

        visual.name                    = "Visual";
        visual.transform.parent        = root.transform;
        visual.transform.localPosition = new Vector3(0f, yOffset, 0f);
        visual.transform.localScale    = scale;
        visual.GetComponent<Renderer>().material = mat;

        // コライダーはグリッドに干渉しないよう無効化
        var col = visual.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    /// <summary>Standard シェーダーのマテリアルを作成して保存する。</summary>
    static Material CreateMat(string matName, Color color)
    {
        var path = $"Assets/Prefabs/Farming/{matName}.mat";

        // 既存があれば上書き
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            existing.color = color;
            EditorUtility.SetDirty(existing);
            return existing;
        }

        var mat = new Material(Shader.Find("Standard")) { color = color };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
