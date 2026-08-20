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
    public BoardRow row = BoardRow.None;
    public bool isCorner;
    public bool isEdge;
    public string itemId; // optional pre-placed item
    public List<string> neighborIds = new List<string>();
}

[Serializable]
public enum BoardRow
{
    None,
    Row1,
    Row2,
    Row3
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
    Row1Slot,
    Row2Slot,
    Row3Slot,
    NeighborSmell,
    EmitSmell,
    PlantAlone,
    Anywhere,
    RequiresLight
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
    /// <summary>Optional key resolved through LevelRuntimeLoader's Plant Catalog.</summary>
    public string plantDataId = "";
    /// <summary>
    /// Optional human-friendly display name shown in tooltips.
    /// Leave blank to use the auto-generated name from TreeNameRegistry.
    /// </summary>
    public string displayName = "";
    public string solutionLandId = "";
    public int    x;
    public int    y;
    public ItemType  itemType         = ItemType.Plant;
    public LightDirection lightDirection = LightDirection.Up;
    public SlotType  requiredSlotType = SlotType.Dirt;
    public int parameterN = 0;
    public string customNotes = "";
    public List<TreeConditionData> conditions = new List<TreeConditionData>();
}

[Serializable]
public enum DecorSizeMode
{
    /// <summary>Width/Height are direct pixel values (same units as screenWidth/screenHeight).</summary>
    Normal,
    /// <summary>Width/Height are computed from widthInLands/heightInLands * slotSize.</summary>
    Land
}

[Serializable]
public class DecorData
{
    /// <summary>Unique identifier for this decoration entry.</summary>
    public string decorId     = "decor_01";
    /// <summary>
    /// Path to the prefab relative to any Resources/ folder, without extension.
    /// E.g. "Decor/PotBackground" for Assets/Resources/Decor/PotBackground.prefab
    /// </summary>
    public string prefabPath  = "";
    /// <summary>Top-left X position in editor screen coordinates.</summary>
    public int    x;
    /// <summary>Top-left Y position in editor screen coordinates.</summary>
    public int    y;
    /// <summary>Which mode drives the width/height values below.</summary>
    public DecorSizeMode sizeMode = DecorSizeMode.Land;
    /// <summary>Width in land slots (e.g. 3 = 3 lands wide). Only used when sizeMode == Land.</summary>
    public float  widthInLands  = 3f;
    /// <summary>Height in land slots (e.g. 2 = 2 lands tall). Only used when sizeMode == Land.</summary>
    public float  heightInLands = 3f;
    /// <summary>Width in editor screen coordinates. Direct value when sizeMode == Normal, auto-computed when sizeMode == Land.</summary>
    public int    width       = 800;
    /// <summary>Height in editor screen coordinates. Direct value when sizeMode == Normal, auto-computed when sizeMode == Land.</summary>
    public int    height      = 800;
    /// <summary>SpriteRenderer sorting order. Negative values appear behind slots.</summary>
    public int    sortingOrder = -1;
}

[Serializable]
public class LevelData
{
    public string levelName = "New Level";
    public int maxMoves = 50;
    public int width  = 5;
    public int height = 5;
    public int screenX = 0;
    public int screenY = 0;
    public int screenWidth = 1080;
    public int screenHeight = 2340;
    public int slotSize = 125;
    public int itemSize = 220;
    public List<LevelCellData> cells       = new List<LevelCellData>();
    public List<TreeData>      trees       = new List<TreeData>();
    public List<DecorData>     decorations = new List<DecorData>();
}
