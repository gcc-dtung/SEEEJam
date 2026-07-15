using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelCellData
{
    public int x;
    public int y;
    public SlotType slotType;
    public string itemId; // optional pre-placed item
}

[Serializable]
public class TreeData
{
    public string treeId      = "tree_01";
    public int    x;
    public int    y;
    public ItemType  itemType         = ItemType.Plant;
    public SlotType  requiredSlotType = SlotType.Dirt;
    public string customNotes = "";
}

[Serializable]
public class LevelData
{
    public string levelName = "New Level";
    public int width  = 5;
    public int height = 5;
    public List<LevelCellData> cells = new List<LevelCellData>();
    public List<TreeData>      trees = new List<TreeData>();
}
