using System.IO;
using UnityEngine;

/// <summary>
/// F5 でセーブ、F9 でロード。起動時にセーブファイルが存在すれば自動ロードする。
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    string SavePath => Application.persistentDataPath + "/save.json";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (File.Exists(SavePath))
            Load();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5)) Save();
        if (Input.GetKeyDown(KeyCode.F9)) Load();
    }

    // ================================================================
    //  セーブ
    // ================================================================

    public void Save()
    {
        var data = new SaveData();

        // ---- インベントリ ----
        var inv = InventoryManager.Instance;
        if (inv != null)
        {
            data.gold = inv.Gold;
            foreach (ItemId id in System.Enum.GetValues(typeof(ItemId)))
            {
                if (id == ItemId.None) continue;
                int count = inv.GetCount(id);
                if (count > 0)
                    data.items.Add(new ItemEntry { id = id.ToString(), count = count });
            }
        }

        // ---- クエスト進捗 ----
        var story = StoryManager.Instance;
        if (story != null)
        {
            data.harvestCurrent = story.HarvestQuest.current;
            data.miningCurrent  = story.MiningQuest.current;
            data.combatCurrent  = story.CombatQuest.current;
        }

        // ---- グリッド状態 ----
        var gm = GridManager.Instance;
        if (gm != null)
        {
            foreach (var cell in gm.AllCells())
            {
                if (cell.IsFarmland)
                    data.farmlands.Add(new GridCellEntry { x = cell.X, z = cell.Z });

                if (cell.HasCrop)
                    data.crops.Add(new CropEntry { x = cell.X, z = cell.Z, stage = (int)cell.CropStage });

                if (cell.IsTrack)
                    data.tracks.Add(new GridCellEntry { x = cell.X, z = cell.Z });
            }
        }

        // ---- 建物 ----
        var bm = BuildingManager.Instance;
        if (bm != null)
        {
            foreach (var (rootX, rootZ, rotation, bd) in bm.GetAllPlacements())
            {
                data.buildings.Add(new BuildingEntry
                {
                    dataName = bd.name,
                    rootX    = rootX,
                    rootZ    = rootZ,
                    rotation = rotation,
                });
            }
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[SaveManager] セーブ完了: {SavePath}");
    }

    // ================================================================
    //  ロード
    // ================================================================

    public void Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.LogWarning("[SaveManager] セーブファイルが見つかりません。");
            return;
        }

        string json = File.ReadAllText(SavePath);
        var data = JsonUtility.FromJson<SaveData>(json);
        if (data == null)
        {
            Debug.LogWarning("[SaveManager] セーブデータのパースに失敗しました。");
            return;
        }

        // ---- インベントリをクリアして復元 ----
        var inv = InventoryManager.Instance;
        if (inv != null)
        {
            inv.ClearForLoad();
            inv.RestoreGold(data.gold);
            foreach (var entry in data.items)
            {
                if (System.Enum.TryParse<ItemId>(entry.id, out var id))
                    inv.RestoreItem(id, entry.count);
            }
        }

        // ---- クエスト進捗 ----
        StoryManager.Instance?.RestoreQuestProgress(
            data.harvestCurrent, data.miningCurrent, data.combatCurrent);

        // ---- 畑タイル ----
        foreach (var entry in data.farmlands)
            FarmManager.Instance?.RestoreFarmland(entry.x, entry.z);

        // ---- 作物 ----
        foreach (var entry in data.crops)
            FarmManager.Instance?.RestoreCrop(entry.x, entry.z, (CropStage)entry.stage);

        // ---- 線路 ----
        foreach (var entry in data.tracks)
            RailManager.Instance?.TryPlaceTrack(entry.x, entry.z);

        // ---- 建物 ----
        foreach (var entry in data.buildings)
        {
            var bd = Resources.Load<BuildingData>("Buildings/" + entry.dataName);
            if (bd == null)
            {
                Debug.LogWarning($"[SaveManager] BuildingData が見つかりません: Buildings/{entry.dataName}");
                continue;
            }
            BuildingManager.Instance?.TryPlace(entry.rootX, entry.rootZ, bd, entry.rotation);
        }

        Debug.Log("[SaveManager] ロード完了");
    }
}
