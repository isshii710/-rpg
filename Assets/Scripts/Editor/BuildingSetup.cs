using UnityEngine;
using UnityEditor;

/// <summary>
/// メニュー「RPG → 建物プレハブを作成」を実行すると
/// 建物マテリアル・プレハブ・BuildingData を自動生成して GridPlacer に設定する。
///
/// 建物一覧（アイテム消費なし・自由配置）：
///   3キー : 家（煙突付き）  1×1
///   4キー : 工房（煙突2本） 2×1
///   5キー : 倉庫            1×2
///   6キー : 木（装飾）      1×1
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

        // ---- マテリアル ----
        var wallMat    = CreateMat("WallMat",    new Color(0.82f, 0.70f, 0.52f)); // ベージュ壁
        var roofMat    = CreateMat("RoofMat",    new Color(0.55f, 0.25f, 0.15f)); // 赤茶屋根
        var chimneyMat = CreateMat("ChimneyMat", new Color(0.45f, 0.40f, 0.38f)); // 石色
        var workshopWallMat = CreateMat("WorkshopWallMat", new Color(0.50f, 0.48f, 0.45f)); // 石壁
        var storageMat = CreateMat("StorageMat", new Color(0.55f, 0.38f, 0.22f)); // 木材倉庫
        var doorMat    = CreateMat("DoorMat",    new Color(0.35f, 0.20f, 0.10f)); // ドア（暗茶）
        var trunkMat   = CreateMat("TrunkMat",   new Color(0.42f, 0.28f, 0.12f)); // 幹
        var leavesMat  = CreateMat("LeavesMat",  new Color(0.20f, 0.62f, 0.22f)); // 葉

        // ---- プレハブ作成 ----
        var housePrefab    = CreateHouse("Assets/Prefabs/Buildings/House.prefab", wallMat, roofMat, chimneyMat);
        var workshopPrefab = CreateWorkshop("Assets/Prefabs/Buildings/Workshop.prefab", workshopWallMat, roofMat, chimneyMat);
        var storagePrefab  = CreateStorage("Assets/Prefabs/Buildings/Storage.prefab", storageMat, roofMat, doorMat);
        var treePrefab     = CreateTree("Assets/Prefabs/Buildings/Tree.prefab", trunkMat, leavesMat);

        // ---- BuildingData 作成（requiredItem = None → アイテム消費なし）----
        var houseData    = CreateData("HouseData",    "家",   BuildingType.Residential, 1, 1, housePrefab);
        var workshopData = CreateData("WorkshopData", "工房", BuildingType.Workshop,    2, 1, workshopPrefab);
        var storageData  = CreateData("StorageData",  "倉庫", BuildingType.Storage,     1, 2, storagePrefab);
        var treeData     = CreateData("DecorationData","木",  BuildingType.Decoration,  1, 1, treePrefab);

        // ---- GridPlacer にアイテムをセット ----
        var playerGO = GameObject.Find("Player");
        if (playerGO != null)
        {
            var gp = playerGO.GetComponent<GridPlacer>();
            if (gp != null)
            {
                var so        = new SerializedObject(gp);
                var itemsProp = so.FindProperty("items");

                if (itemsProp.arraySize < 6)
                    itemsProp.arraySize = 6;

                SetItem(itemsProp.GetArrayElementAtIndex(2), "家",   houseData);
                SetItem(itemsProp.GetArrayElementAtIndex(3), "工房", workshopData);
                SetItem(itemsProp.GetArrayElementAtIndex(4), "倉庫", storageData);
                SetItem(itemsProp.GetArrayElementAtIndex(5), "木",   treeData);

                so.ApplyModifiedProperties();
                Debug.Log("[BuildingSetup] GridPlacer に 3=家 4=工房 5=倉庫 6=木 をセット。");
            }
        }
        else
        {
            Debug.LogWarning("[BuildingSetup] Player が見つかりません。RPG → シーンをセットアップ を先に実行してください。");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BuildingSetup] ✅ 完了！ 3=家, 4=工房, 5=倉庫, 6=木  （R=回転 / アイテム消費なし）");
    }

    // ================================================================
    //  建物プレハブ作成（複数パーツ構成）
    // ================================================================

    /// <summary>家：壁 + 屋根 + 煙突</summary>
    static GameObject CreateHouse(string path, Material wall, Material roof, Material chimney)
    {
        var root = new GameObject("House");

        // 壁（本体）
        AddCube(root, "Walls", wall,
            pos:   new Vector3(0f, 0.42f, 0f),
            scale: new Vector3(0.88f, 0.84f, 0.88f));

        // 屋根（平たい台形っぽくするため2枚重ね）
        AddCube(root, "Roof1", roof,
            pos:   new Vector3(0f, 0.92f, 0f),
            scale: new Vector3(0.96f, 0.18f, 0.96f));
        AddCube(root, "Roof2", roof,
            pos:   new Vector3(0f, 1.05f, 0f),
            scale: new Vector3(0.72f, 0.22f, 0.72f));
        AddCube(root, "Roof3", roof,
            pos:   new Vector3(0f, 1.18f, 0f),
            scale: new Vector3(0.48f, 0.20f, 0.48f));

        // 煙突
        AddCylinder(root, "Chimney", chimney,
            pos:   new Vector3(0.22f, 1.15f, 0.18f),
            scale: new Vector3(0.10f, 0.20f, 0.10f));

        return SavePrefab(root, path);
    }

    /// <summary>工房：横長の壁 + 屋根 + 煙突2本</summary>
    static GameObject CreateWorkshop(string path, Material wall, Material roof, Material chimney)
    {
        var root = new GameObject("Workshop");

        AddCube(root, "Walls", wall,
            pos:   new Vector3(0f, 0.38f, 0f),
            scale: new Vector3(1.88f, 0.76f, 0.88f));

        AddCube(root, "Roof1", roof,
            pos:   new Vector3(0f, 0.80f, 0f),
            scale: new Vector3(1.96f, 0.14f, 0.96f));
        AddCube(root, "Roof2", roof,
            pos:   new Vector3(0f, 0.91f, 0f),
            scale: new Vector3(1.72f, 0.20f, 0.72f));

        // 煙突2本
        AddCylinder(root, "Chimney1", chimney,
            pos:   new Vector3(-0.45f, 1.0f, 0.15f),
            scale: new Vector3(0.12f, 0.22f, 0.12f));
        AddCylinder(root, "Chimney2", chimney,
            pos:   new Vector3(0.45f, 1.0f, 0.15f),
            scale: new Vector3(0.12f, 0.22f, 0.12f));

        return SavePrefab(root, path);
    }

    /// <summary>倉庫：縦長の箱 + ドア</summary>
    static GameObject CreateStorage(string path, Material wood, Material roof, Material door)
    {
        var root = new GameObject("Storage");

        AddCube(root, "Walls", wood,
            pos:   new Vector3(0f, 0.36f, 0f),
            scale: new Vector3(0.88f, 0.72f, 1.88f));

        AddCube(root, "Roof", roof,
            pos:   new Vector3(0f, 0.76f, 0f),
            scale: new Vector3(0.96f, 0.12f, 1.96f));

        // ドア（前面中央）
        AddCube(root, "Door", door,
            pos:   new Vector3(0f, 0.22f, -0.945f),
            scale: new Vector3(0.28f, 0.44f, 0.04f));

        return SavePrefab(root, path);
    }

    /// <summary>木（装飾）：幹 + 葉っぱ（球）</summary>
    static GameObject CreateTree(string path, Material trunk, Material leaves)
    {
        var root = new GameObject("Tree");

        AddCylinder(root, "Trunk", trunk,
            pos:   new Vector3(0f, 0.28f, 0f),
            scale: new Vector3(0.14f, 0.28f, 0.14f));

        AddSphere(root, "Leaves", leaves,
            pos:   new Vector3(0f, 0.82f, 0f),
            scale: new Vector3(0.62f, 0.70f, 0.62f));

        return SavePrefab(root, path);
    }

    // ================================================================
    //  パーツ追加ヘルパー
    // ================================================================

    static void AddCube(GameObject parent, string name, Material mat, Vector3 pos, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Setup(go, parent, mat, pos, scale);
    }

    static void AddCylinder(GameObject parent, string name, Material mat, Vector3 pos, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        Setup(go, parent, mat, pos, scale);
    }

    static void AddSphere(GameObject parent, string name, Material mat, Vector3 pos, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        Setup(go, parent, mat, pos, scale);
    }

    static void Setup(GameObject go, GameObject parent, Material mat, Vector3 pos, Vector3 scale)
    {
        go.transform.parent        = parent.transform;
        go.transform.localPosition = pos;
        go.transform.localScale    = scale;
        go.GetComponent<Renderer>().material = mat;
        var col = go.GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    static GameObject SavePrefab(GameObject root, string path)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ================================================================
    //  BuildingData / Material / GridPlacer ヘルパー
    // ================================================================

    static BuildingData CreateData(string assetName, string buildingName,
        BuildingType type, int sizeX, int sizeZ, GameObject prefab)
    {
        var path     = $"Assets/Resources/Buildings/{assetName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<BuildingData>(path);

        if (existing != null)
        {
            existing.buildingName = buildingName;
            existing.type         = type;
            existing.sizeX        = sizeX;
            existing.sizeZ        = sizeZ;
            existing.requiredItem = ItemId.None; // アイテム消費なし
            existing.prefab       = prefab;
            EditorUtility.SetDirty(existing);
            return existing;
        }

        var data = ScriptableObject.CreateInstance<BuildingData>();
        data.buildingName = buildingName;
        data.type         = type;
        data.sizeX        = sizeX;
        data.sizeZ        = sizeZ;
        data.requiredItem = ItemId.None;
        data.prefab       = prefab;
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    static Material CreateMat(string matName, Color color)
    {
        var path     = $"Assets/Prefabs/Buildings/{matName}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) { existing.color = color; EditorUtility.SetDirty(existing); return existing; }
        var mat = new Material(Shader.Find("Standard")) { color = color };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void SetItem(SerializedProperty item, string label, BuildingData data)
    {
        item.FindPropertyRelative("label").stringValue                 = label;
        item.FindPropertyRelative("type").enumValueIndex              = 0; // Building
        item.FindPropertyRelative("buildingData").objectReferenceValue = data;
        item.FindPropertyRelative("prefab").objectReferenceValue      = null;
    }
}
