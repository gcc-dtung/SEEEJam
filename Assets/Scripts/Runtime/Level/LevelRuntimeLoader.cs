using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class LevelRuntimeLoader : MonoBehaviour
{
    [Header("Level")]
    [SerializeField] private TextAsset levelJson;
    [SerializeField] private bool loadOnStart = true;

    [Header("Prefabs")]
    [SerializeField] private ItemSlot slotPrefab;
    [SerializeField] private DragItem treeItemPrefab;
    [SerializeField] private DragItem lightItemPrefab;

    [Header("Plant Visuals")]
    [SerializeField] private PlantCatalogSO plantCatalog;

    [Header("Placement")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool mapScreenPreviewToCamera = true;
    [SerializeField] private Transform slotRoot;
    [SerializeField] private Transform itemRoot;
    [SerializeField] private float editorUnitsPerWorldUnit = 48f;
    [SerializeField] private bool invertY = true;
    [SerializeField] private Vector3 worldOffset;
    [SerializeField] private float itemInputZOffset = -0.1f;

    private readonly List<GameObject> _spawnedObjects = new List<GameObject>();

    private void Start()
    {
        EnsureRuntimeInteraction();
        if (loadOnStart && !Bootstrap.HasBootstrapped)
            LevelManager.Instance.LoadAssignedLevel(this);
    }

    public bool LoadAssignedLevel()
    {
        return LoadLevelAsset(levelJson);
    }

    public bool LoadLevelAsset(TextAsset levelAsset)
    {
        EnsureRuntimeInteraction();
        if (levelAsset == null)
        {
            Debug.LogWarning("[LevelRuntimeLoader] No level JSON assigned.");
            return false;
        }

        LevelData level = JsonUtility.FromJson<LevelData>(levelAsset.text);
        if (level == null)
        {
            Debug.LogWarning("[LevelRuntimeLoader] Could not parse assigned level JSON.");
            return false;
        }

        LoadLevel(level);
        return true;
    }

    public void LoadLevel(LevelData level)
    {
        BoosterManager.Instance.ResetForLevel();
        ClearSpawnedObjects();
        MoveManager.Instance.ClearHistory();
        LevelManager.Instance.ConfigureLoadedLevel(level);

        Dictionary<string, ItemSlot> landSlots = SpawnLandSlots(level);
        ConnectLandNeighbors(level, landSlots);
        List<Item> items = SpawnTreeWaitSlotsAndItems(level);
        BoardManager.Instance.RegisterLevelBoard(landSlots.Values, items);
    }

    private Dictionary<string, ItemSlot> SpawnLandSlots(LevelData level)
    {
        Dictionary<string, ItemSlot> landSlots = new Dictionary<string, ItemSlot>();
        foreach (LevelCellData land in level.cells)
        {
            ItemSlot slot = Instantiate(slotPrefab, ToWorldPosition(level, land.x, land.y), Quaternion.identity, slotRoot);
            slot.name = string.IsNullOrWhiteSpace(land.landId) ? "LandSlot" : land.landId;
            ApplyEditorSize(slot.gameObject, level, level.slotSize);
            slot.Configure(land.slotType, null, land.isCorner, land.isEdge, land.landId, land.row);
            landSlots[land.landId] = slot;
            _spawnedObjects.Add(slot.gameObject);
        }
        return landSlots;
    }

    private void ConnectLandNeighbors(LevelData level, Dictionary<string, ItemSlot> landSlots)
    {
        foreach (LevelCellData land in level.cells)
        {
            if (!landSlots.TryGetValue(land.landId, out ItemSlot slot) || land.neighborIds == null)
                continue;

            foreach (string neighborId in land.neighborIds)
            {
                if (landSlots.TryGetValue(neighborId, out ItemSlot neighbor))
                    slot.AddNeighbor(neighbor);
            }
        }
    }

    private List<Item> SpawnTreeWaitSlotsAndItems(LevelData level)
    {
        List<Item> items = new List<Item>();
        foreach (TreeData tree in level.trees)
        {
            ItemSlot waitSlot = Instantiate(slotPrefab, ToWorldPosition(level, tree.x, tree.y), Quaternion.identity, slotRoot);
            waitSlot.name = tree.treeId + "_WaitSlot";
            ApplyEditorSize(waitSlot.gameObject, level, level.slotSize);
            waitSlot.Configure(SlotType.Wait, newSlotId: tree.treeId + "_wait");
            _spawnedObjects.Add(waitSlot.gameObject);

            Vector3 itemPosition = waitSlot.transform.position + new Vector3(0f, 0f, itemInputZOffset);
            DragItem itemPrefab = tree.itemType == ItemType.Light && lightItemPrefab != null
                ? lightItemPrefab
                : treeItemPrefab;
            DragItem dragItem = Instantiate(itemPrefab, itemPosition, Quaternion.identity, itemRoot);
            dragItem.name = tree.treeId;
            ApplyEditorSize(dragItem.gameObject, level, level.itemSize);

            Item item = dragItem.CurrentDragItem;
            if (item != null)
            {
                item.Configure(
                    tree.itemType,
                    tree.requiredSlotType,
                    tree.treeId,
                    BuildPlantConditions(tree.conditions, level.trees),
                    tree.solutionLandId,
                    tree.displayName);

                if (tree.itemType == ItemType.Light)
                {
                    LightEmitter lightEmitter = item.GetComponent<LightEmitter>();
                    if (lightEmitter == null)
                        lightEmitter = item.gameObject.AddComponent<LightEmitter>();

                    lightEmitter.Configure(tree.lightDirection);
                }
                else
                {
                    ApplyPlantData(item, tree);
                    items.Add(item);
                }
            }

            dragItem.ConfigureStart(waitSlot, itemInputZOffset);
            _spawnedObjects.Add(dragItem.gameObject);
        }
        return items;
    }

    private void ApplyPlantData(Item item, TreeData tree)
    {
        if (string.IsNullOrWhiteSpace(tree.plantDataId))
            return;

        if (plantCatalog != null && plantCatalog.TryGetPlant(tree.plantDataId, out PlantDataSO plantData))
        {
            item.ApplyPlantData(plantData);
            return;
        }

        Debug.LogWarning($"[LevelRuntimeLoader] Plant data '{tree.plantDataId}' was not found for tree '{tree.treeId}'.", this);
    }

    private List<PlantCondition> BuildPlantConditions(List<TreeConditionData> conditionData, List<TreeData> allTrees = null)
    {
        List<PlantCondition> conditions = new List<PlantCondition>();
        if (conditionData == null)
            return conditions;

        foreach (TreeConditionData data in conditionData)
        {
            if (data.conditionType == TreeConditionType.NearTreeCount)
                conditions.Add(new NearTreeCountCondition { nCount = data.n });
            else if (data.conditionType == TreeConditionType.NearSpecificTree && !string.IsNullOrWhiteSpace(data.targetTreeId))
            {
                string targetDisplay = ResolveDisplayName(data.targetTreeId, allTrees);
                conditions.Add(new NearSpecificTreeCondition { targetTreeId = data.targetTreeId, targetDisplayName = targetDisplay });
            }
            else if (data.conditionType == TreeConditionType.EdgeSlot)
                conditions.Add(new EdgeSlotCondition());
            else if (data.conditionType == TreeConditionType.CornerSlot)
                conditions.Add(new CornerSlotCondition());
            else if (data.conditionType == TreeConditionType.Row1Slot)
                conditions.Add(new RowSlotCondition { requiredRow = BoardRow.Row1 });
            else if (data.conditionType == TreeConditionType.Row2Slot)
                conditions.Add(new RowSlotCondition { requiredRow = BoardRow.Row2 });
            else if (data.conditionType == TreeConditionType.Row3Slot)
                conditions.Add(new RowSlotCondition { requiredRow = BoardRow.Row3 });
            else if (data.conditionType == TreeConditionType.NeighborSmell && data.smell != PlantSmell.None)
                conditions.Add(new NeighborSmellCondition { smell = data.smell, preference = data.smellPreference });
            else if (data.conditionType == TreeConditionType.EmitSmell && data.smell != PlantSmell.None)
                conditions.Add(new EmitSmellCondition { smell = data.smell });
            else if (data.conditionType == TreeConditionType.PlantAlone)
                conditions.Add(new PlantAloneCondition());
            else if (data.conditionType == TreeConditionType.Anywhere)
                conditions.Add(new AnywhereCondition());
            else if (data.conditionType == TreeConditionType.RequiresLight)
                conditions.Add(new RequiresLightCondition());
        }
        return conditions;
    }

    private static string ResolveDisplayName(string treeId, List<TreeData> allTrees)
    {
        if (allTrees != null)
        {
            foreach (TreeData t in allTrees)
            {
                if (t.treeId == treeId && !string.IsNullOrWhiteSpace(t.displayName))
                    return t.displayName;
            }
        }
        return TreeNameRegistry.GetDisplayName(treeId);
    }

    private Vector3 ToWorldPosition(LevelData level, int x, int y)
    {
        if (mapScreenPreviewToCamera && TryGetCameraWorldRect(out Rect cameraRect))
        {
            float normalizedX = Mathf.InverseLerp(level.screenX, level.screenX + level.screenWidth, x);
            float normalizedY = Mathf.InverseLerp(level.screenY, level.screenY + level.screenHeight, y);
            float cameraWorldX = Mathf.Lerp(cameraRect.xMin, cameraRect.xMax, normalizedX);
            float cameraWorldY = Mathf.Lerp(cameraRect.yMax, cameraRect.yMin, normalizedY);
            return new Vector3(cameraWorldX, cameraWorldY, worldOffset.z);
        }

        float worldX = x / editorUnitsPerWorldUnit;
        float worldY = y / editorUnitsPerWorldUnit;
        if (invertY)
            worldY *= -1f;
        return worldOffset + new Vector3(worldX, worldY, 0f);
    }

    private void ApplyEditorSize(GameObject target, LevelData level, int editorSize)
    {
        float worldSize;
        if (mapScreenPreviewToCamera && TryGetCameraWorldRect(out Rect cameraRect))
        {
            worldSize = Mathf.Max(1, editorSize) / Mathf.Max(1f, level.screenHeight) * cameraRect.height;
        }
        else
        {
            worldSize = Mathf.Max(1, editorSize) / editorUnitsPerWorldUnit;
        }

        SetVisibleWorldSize(target, worldSize);
        ResizeColliderToWorldSize(target, worldSize);
    }

    private void SetVisibleWorldSize(GameObject target, float worldSize)
    {
        target.transform.localScale = Vector3.one;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            target.transform.localScale = Vector3.one * worldSize;
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        float currentSize = Mathf.Max(bounds.size.x, bounds.size.y);
        if (currentSize <= 0.0001f)
            return;

        float scale = worldSize / currentSize;
        target.transform.localScale = Vector3.one * scale;
    }

    private void ResizeColliderToWorldSize(GameObject target, float worldSize)
    {
        BoxCollider2D collider = target.GetComponent<BoxCollider2D>();
        if (collider == null)
            return;

        Vector3 scale = target.transform.lossyScale;
        collider.size = new Vector2(
            worldSize / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
            worldSize / Mathf.Max(0.0001f, Mathf.Abs(scale.y)));
    }

    private bool TryGetCameraWorldRect(out Rect rect)
    {
        Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
        if (cameraToUse == null || !cameraToUse.orthographic)
        {
            rect = default;
            return false;
        }

        float height = cameraToUse.orthographicSize * 2f;
        float width = height * cameraToUse.aspect;
        Vector3 center = cameraToUse.transform.position;
        rect = new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
        return true;
    }

    private void EnsureRuntimeInteraction()
    {
        Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
        if (cameraToUse == null)
        {
            Debug.LogWarning("[LevelRuntimeLoader] No camera found. DragItem pointer input needs a camera.");
            return;
        }

        if (cameraToUse.GetComponent<Physics2DRaycaster>() == null)
            cameraToUse.gameObject.AddComponent<Physics2DRaycaster>();
    }

    private void ClearSpawnedObjects()
    {
        for (int i = _spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (_spawnedObjects[i] != null)
                Destroy(_spawnedObjects[i]);
        }
        _spawnedObjects.Clear();
    }
}
