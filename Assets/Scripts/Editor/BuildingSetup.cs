using UnityEngine;
using UnityEditor;

/// <summary>
/// メニュー「RPG → 建物プレハブを作成」を実行すると
/// 建物マテリアル・プレハブ・BuildingData を自動生成して GridPlacer に設定する。
/// </summary>
public static class BuildingSetup
{
    [MenuItem("RPG/建物プレハブを作成")]
    static void CreateBuildingPrefabs()
    {
        // ---- フォルダ作成 ----
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Buildings"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Buildings");
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Buildings"))
            AssetDatabase.CreateFolder("Assets/Resources", "Buildings");

        // ---- マテリアル作成 ----
        var houseMat      = CreateMat("HouseMat",      new Color(0.75f, 0.55f, 0.38f)); // 木材色
        var workshopMat   = CreateMat("WorkshopMat",   new Color(0.42f, 0.42f, 0.48f)); // 石色
        var storageMat    = CreateMat("StorageMat",    new Color(0.55f, 0.38f, 0.22f)); // 茶色
        var decorationMat = CreateMat("DecorationMat", new Color(0.35f, 0.75f, 0.35f)); // 緑

        // ---- プレハブ作成 ----
        // 家：1×1 マス
        var housePrefab = CreateBuildingPrefab("House",
            new Vector3(0.9f, 1.2f, 0.9f), 0.6f, houseMat,
            "Assets/Prefabs/Buildings/House.prefab");

        // 工房：2×1 マス
        var workshopPrefab = CreateBuildingPrefab("Workshop",
            new Vector3(1.9f, 1.0f, 0.9f), 0.5f, workshopMat,
            "Assets/Prefabs/Buildings/Workshop.prefab");

        // 倉庫：1×2 マス
        var storagePrefab = CreateBuildingPrefab("Storage",
            new Vector3(0.9f, 0.8f, 1.9f), 0.4f, storageMat,
            "Assets/Prefabs/Buildings/Storage.prefab");

        // 装飾（柱）：1×1 マス
        var decorationPrefab = CreateBuildingPrefab("Decoration",
            new Vector3(0.25f, 1.8f, 0.25f), 0.9f, decorationMat,
            "Assets/Prefabs/Buildings/Decoration.prefab");

        // ---- BuildingData 作成 ----
        var houseData = CreateBuildingData("HouseData",
            buildingName: "家",
            type: BuildingType.Residential,
            sizeX: 1, sizeZ: 1,
            requiredItem: ItemId.House,
            prefab: housePrefab);

        var workshopData = CreateBuildingData("WorkshopData",
            buildingName: "工房",
            type: BuildingType.Workshop,
            sizeX: 2, sizeZ: 1,
            requiredItem: ItemId.Workshop,
            prefab: workshopPrefab);

        var storageData = CreateBuildingData("StorageData",
            buildingName: "倉庫",
            type: BuildingType.Storage,
            sizeX: 1, sizeZ: 2,
            requiredItem: ItemId.Storage,
            prefab: storagePrefab);

        var decorationData = CreateBuildingData("DecorationData",
            buildingName: "装飾",
            type: BuildingType.Decoration,
            sizeX: 1, sizeZ: 1,
            requiredItem: ItemId.Decoration,
            prefab: decorationPrefab);

        // ---- GridPlacer にアイテムをセット ----
        var playerGO = GameObject.Find("Player");
        if (playerGO != null)
        {
            var gp = playerGO.GetComponent<GridPlacer>();
            if (gp != null)
            {
                var so        = new SerializedObject(gp);
                var itemsProp = so.FindProperty("items");

                // arraySize を少なくとも 6 に拡張（既存の 0-1 を保持）
                if (itemsProp.arraySize < 6)
                    itemsProp.arraySize = 6;

                // index 2 : 家
                SetBuildingItem(itemsProp.GetArrayElementAtIndex(2), "家", houseData);

                // index 3 : 工房
                SetBuildingItem(itemsProp.GetArrayElementAtIndex(3), "工房", workshopData);

                // index 4 : 倉庫
                SetBuildingItem(itemsProp.GetArrayElementAtIndex(4), "倉庫", storageData);

                // index 5 : 装飾
                SetBuildingItem(itemsProp.GetArrayElementAtIndex(5), "装飾", decorationData);

                so.ApplyModifiedProperties();
                Debug.Log("[BuildingSetup] GridPlacer に建物アイテムをセットしました。");
            }
        }
        else
        {
            Debug.LogWarning("[BuildingSetup] Player が見つかりません。先に RPG → シーンをセットアップ を実行してください。");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[BuildingSetup] ✅ 完了！ 3=家, 4=工房, 5=倉庫, 6=装飾 (R=回転)");
    }

    // ================================================================
    //  ヘルパー
    // ================================================================

    /// <summary>建物アイテム設定（type=Building, buildingData, prefab=null）</summary>
    static void SetBuildingItem(SerializedProperty item, string label, BuildingData data)
    {
        item.FindPropertyRelative("label").stringValue                = label;
        item.FindPropertyRelative("type").enumValueIndex             = 0; // Building
        item.FindPropertyRelative("buildingData").objectReferenceValue = data;
        item.FindPropertyRelative("prefab").objectReferenceValue     = null;
    }

    /// <summary>親 GameObject + ビジュアル子要素（Cube）の構造でプレハブを作る。</summary>
    static GameObject CreateBuildingPrefab(
        string objName, Vector3 scale, float yOffset,
        Material mat, string path)
    {
        var root   = new GameObject(objName);
        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);

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

    /// <summary>BuildingData ScriptableObject を作成して Resources/Buildings/ に保存する。</summary>
    static BuildingData CreateBuildingData(
        string assetName, string buildingName,
        BuildingType type, int sizeX, int sizeZ,
        ItemId requiredItem, GameObject prefab)
    {
        var path = $"Assets/Resources/Buildings/{assetName}.asset";

        // 既存があれば上書き
        var existing = AssetDatabase.LoadAssetAtPath<BuildingData>(path);
        if (existing != null)
        {
            existing.buildingName = buildingName;
            existing.type         = type;
            existing.sizeX        = sizeX;
            existing.sizeZ        = sizeZ;
            existing.requiredItem = requiredItem;
            existing.prefab       = prefab;
            EditorUtility.SetDirty(existing);
            return existing;
        }

        var data = ScriptableObject.CreateInstance<BuildingData>();
        data.buildingName = buildingName;
        data.type         = type;
        data.sizeX        = sizeX;
        data.sizeZ        = sizeZ;
        data.requiredItem = requiredItem;
        data.prefab       = prefab;

        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    /// <summary>Standard シェーダーのマテリアルを作成して Buildings フォルダに保存する。</summary>
    static Material CreateMat(string matName, Color color)
    {
        var path = $"Assets/Prefabs/Buildings/{matName}.mat";

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
