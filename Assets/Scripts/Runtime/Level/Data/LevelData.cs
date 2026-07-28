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
    public bool isCorner;
    public bool isEdge;
    public string itemId; // optional pre-placed item
    public List<string> neighborIds = new List<string>();
}

[Serializable]
public enum PlantSmell
{
    None,
    Perfume,
    Disgust
}

[Serializable]
public enum SmellConditionPreference
{
    Like,
    Dislike
}

[Serializable]
public enum TreeConditionType
{
    NearTreeCount,
    NearSpecificTree,
    EdgeSlot,
    CornerSlot,
    NeighborSmell,
    EmitSmell,
    PlantAlone,
    Anywhere
}

[Serializable]
public class TreeConditionData
{
    public TreeConditionType conditionType = TreeConditionType.NearTreeCount;
    public int n = 0;
    public string targetTreeId = "";
    public PlantSmell smell = PlantSmell.Perfume;
    public SmellConditionPreference smellPreference = SmellConditionPreference.Like;
}

[Serializable]
public class TreeData
{
    public string treeId      = "tree_01";
    public string solutionLandId = "";
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
    public int maxMoves = 10;
    public int width  = 5;
    public int height = 5;
    public int screenX = 0;
    public int screenY = 0;
    public int screenWidth = 1080;
    public int screenHeight = 2340;
    public int slotSize = 125;
    public int itemSize = 100;
    public List<LevelCellData> cells = new List<LevelCellData>();
    public List<TreeData>      trees = new List<TreeData>();
}
