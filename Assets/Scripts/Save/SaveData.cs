using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public int gold;
    public List<ItemEntry> items = new();
    public int harvestCurrent;
    public int miningCurrent;
    public int combatCurrent;
    public List<GridCellEntry> farmlands = new();
    public List<CropEntry> crops = new();
    public List<GridCellEntry> tracks = new();
    public List<BuildingEntry> buildings = new();
}

[System.Serializable]
public class ItemEntry { public string id; public int count; }

[System.Serializable]
public class GridCellEntry { public int x, z; }

[System.Serializable]
public class CropEntry { public int x, z; public int stage; }

[System.Serializable]
public class BuildingEntry { public string dataName; public int rootX, rootZ, rotation; }
