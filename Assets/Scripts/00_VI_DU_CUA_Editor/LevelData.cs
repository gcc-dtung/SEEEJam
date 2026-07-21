using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelCellData
{
    public string landId;
    public int x;
    public int y;
    public SlotType slotType;
    public string itemId; // optional pre-placed item
    public List<string> neighborIds = new List<string>();
}

[Serializable]
public enum TreeConditionType
{
    NearTreeCount,
    NearSpecificTree
}

[Serializable]
public class TreeConditionData
{
    public TreeConditionType conditionType = TreeConditionType.NearTreeCount;
    public int n = 0;
    public string targetTreeId = "";
}

[Serializable]
public class TreeData
{
    public string treeId      = "tree_01";
    public int    x;
    public int    y;
    public ItemType  itemType         = ItemType.Plant;
    public SlotType  requiredSlotType = SlotType.Dirt;
    public int parameterN = 0;
    public string customNotes = "";
    public List<TreeConditionData> conditions = new List<TreeConditionData>();
}

[Serializable]
public class LevelData
{
    public string levelName = "New Level";
    public int width  = 5;
    public int height = 5;
    public int screenX = 0;
    public int screenY = 0;
    public int screenWidth = 540;
    public int screenHeight = 960;
    public int slotSize = 48;
    public int itemSize = 32;
    public List<LevelCellData> cells = new List<LevelCellData>();
    public List<TreeData>      trees = new List<TreeData>();
}
