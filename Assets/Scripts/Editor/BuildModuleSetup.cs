using UnityEngine;
using UnityEditor;

/// <summary>
/// メニュー「RPG → ビルドモジュールを作成」を実行すると
/// モジュール建築に使うパーツプレハブと BuildPieceData を自動生成する。
///
/// パーツ一覧:
///   床 (Floor)  : 石床タイル 1×1
///   壁 (Wall)   : 石壁パネル 1×1
///   木壁 (Wall) : 木材パネル 1×1
///   屋根 (Roof) : 屋根タイル 1×1
///   柱 (Pillar) : 石柱
/// </summary>
public static class BuildModuleSetup
{
    [MenuItem("RPG/ビルドモジュールを作成")]
    static void CreateModules()
    {
        // ---- フォルダ ----
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/BuildPieces");
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/BuildPieces");

        // ---- マテリアル ----
        var matStoneFloor = Mat("BP_StoneFloor", new Color(0.58f, 0.56f, 0.52f));
        var matStoneWall  = Mat("BP_StoneWall",  new Color(0.72f, 0.69f, 0.63f));
        var matWoodWall   = Mat("BP_WoodWall",   new Color(0.52f, 0.36f, 0.18f));
        var matRoof       = Mat("BP_Roof",       new Color(0.28f, 0.16f, 0.08f));
        var matPillar     = Mat("BP_Pillar",     new Color(0.55f, 0.53f, 0.50f));

        // ---- プレハブ ----
        var floorPrefab  = FloorPrefab("Assets/Prefabs/BuildPieces/BP_Floor.prefab",     matStoneFloor);
        var stoneWPrefab = WallPrefab ("Assets/Prefabs/BuildPieces/BP_StoneWall.prefab", matStoneWall);
        var woodWPrefab  = WallPrefab ("Assets/Prefabs/BuildPieces/BP_WoodWall.prefab",  matWoodWall);
        var roofPrefab   = RoofPrefab ("Assets/Prefabs/BuildPieces/BP_Roof.prefab",      matRoof);
        var pillarPrefab = PillarPrefab("Assets/Prefabs/BuildPieces/BP_Pillar.prefab",   matPillar);

        // ---- BuildPieceData ----
        Data("BP_FloorData",     "石床",   BuildPieceCategory.Floor,  floorPrefab,  cost: 0);
        Data("BP_StoneWallData", "石壁",   BuildPieceCategory.Wall,   stoneWPrefab, cost: 0);
        Data("BP_WoodWallData",  "木壁",   BuildPieceCategory.Wall,   woodWPrefab,  cost: 0);
        Data("BP_RoofData",      "屋根",   BuildPieceCategory.Roof,   roofPrefab,   cost: 0);
        Data("BP_PillarData",    "石柱",   BuildPieceCategory.Pillar, pillarPrefab, cost: 0);

        // ---- BuildUI を Managers に追加 ----
        AddBuildUI();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[BuildModuleSetup] ✅ 完了！ F キーでビルドモード開始。");
    }

    // ================================================================
    //  プレハブ生成
    // ================================================================

    /// <summary>床タイル（薄い直方体）</summary>
    static GameObject FloorPrefab(string path, Material mat)
    {
        var root = new GameObject("Floor");
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Mesh";
        Setup(cube, root, mat, new Vector3(0, 0.05f, 0), new Vector3(1f, 0.1f, 1f));
        return SavePrefab(root, path);
    }

    /// <summary>壁パネル（縦長の薄い直方体）</summary>
    static GameObject WallPrefab(string path, Material mat)
    {
        var root = new GameObject("Wall");
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Mesh";
        // スナップ位置 (0,0,0) = 壁の下端中央 → y を 0.5 上げて中心を合わせる
        Setup(cube, root, mat, new Vector3(0, 0.5f, 0), new Vector3(1f, 1f, 0.1f));
        return SavePrefab(root, path);
    }

    /// <summary>屋根タイル（床より少し大きめ・フラット）</summary>
    static GameObject RoofPrefab(string path, Material mat)
    {
        var root = new GameObject("Roof");
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Mesh";
        // BuildModeManager は屋根を y=1 に配置するので、prefab は y=0.05 でよい
        Setup(cube, root, mat, new Vector3(0, 0.05f, 0), new Vector3(1.06f, 0.1f, 1.06f));
        return SavePrefab(root, path);
    }

    /// <summary>柱（細長い直方体）</summary>
    static GameObject PillarPrefab(string path, Material mat)
    {
        var root = new GameObject("Pillar");
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Mesh";
        Setup(cube, root, mat, new Vector3(0, 0.5f, 0), new Vector3(0.15f, 1f, 0.15f));
        return SavePrefab(root, path);
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
    //  BuildPieceData / Material ヘルパー
    // ================================================================

    static Material Mat(string name, Color color)
    {
        var path = $"Assets/Prefabs/BuildPieces/{name}.mat";
        var ex   = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (ex != null) { ex.color = color; EditorUtility.SetDirty(ex); return ex; }
        var m = new Material(Shader.Find("Standard")) { color = color };
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static BuildPieceData Data(string assetName, string pieceName,
        BuildPieceCategory category, GameObject prefab, int cost)
    {
        var path = $"Assets/Resources/BuildPieces/{assetName}.asset";
        var ex   = AssetDatabase.LoadAssetAtPath<BuildPieceData>(path);

        if (ex != null)
        {
            ex.pieceName = pieceName;
            ex.category  = category;
            ex.prefab    = prefab;
            ex.cost      = cost;
            EditorUtility.SetDirty(ex);
            return ex;
        }

        var d = ScriptableObject.CreateInstance<BuildPieceData>();
        d.pieceName = pieceName;
        d.category  = category;
        d.prefab    = prefab;
        d.cost      = cost;
        AssetDatabase.CreateAsset(d, path);
        return d;
    }

    // ================================================================
    //  BuildUI を Managers に追加
    // ================================================================

    static void AddBuildUI()
    {
        var managers = GameObject.Find("Managers");
        if (managers == null)
        {
            Debug.LogWarning("[BuildModuleSetup] Managers が見つかりません。RPG → シーンをセットアップ を先に実行してください。");
            return;
        }

        // BuildModeManager
        var bmGO = GameObject.Find("BuildModeManager");
        if (bmGO == null)
        {
            bmGO = new GameObject("BuildModeManager");
            bmGO.transform.parent = managers.transform;
            bmGO.AddComponent<BuildModeManager>();
            Debug.Log("[BuildModuleSetup] BuildModeManager を追加しました。");
        }

        // BuildUI
        var buiGO = GameObject.Find("BuildUI");
        if (buiGO == null)
        {
            buiGO = new GameObject("BuildUI");
            buiGO.transform.parent = managers.transform;
            buiGO.AddComponent<BuildUI>();
            Debug.Log("[BuildModuleSetup] BuildUI を追加しました。");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            int last = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, last), path.Substring(last + 1));
        }
    }
}
