using System.Collections.Generic;
using UnityEngine;

public class ItemSlot : MonoBehaviour
{
    #region Fields

    [Header("Config")] 
    [SerializeField] private string slotId;
    [SerializeField] private SlotType type;
    [SerializeField] private BoardRow row;
    [SerializeField] private bool isCorner;
    [SerializeField] private bool isEdge;
    [SerializeField] private List<ItemSlot> neighbors;
    
    [SerializeField] private ItemSlotView itemSlotView;
    
    private Item _hoverItem;
    
    public string SlotId => slotId;
    public SlotType Type => type;
    public BoardRow Row => row;
    public bool IsCorner => isCorner;
    public bool IsEdge => isEdge;
    public Item currentItem;
    public bool HasCurrentItem => currentItem != null;
    public Item CurrentItem => currentItem;
    public List<ItemSlot> Neighbors => neighbors;
    #endregion

    #region LifeCycle
    private void OnEnable()
    {
        EventBus.Instance.Subscribe<DragStartedEvent>(HandleDragStart);
        EventBus.Instance.Subscribe<DragEndedEvent>(HandleDragEnd);
        EventBus.Instance.Subscribe<DragHoverChangedEvent>(HandleHoverChanged);
    }

    private void Start()
    {
        EnsureItemSlotView();
        itemSlotView.HideImmediately();
        foreach (ItemSlot neighbor in neighbors)
        {
            neighbor.AddNeighbor(this);
        }
    }   

    private void OnDisable()
    {
        if (EventBus.TryGetInstance(out EventBus eventBus))
        {
            eventBus.Unsubscribe<DragStartedEvent>(HandleDragStart);
            eventBus.Unsubscribe<DragEndedEvent>(HandleDragEnd);
            eventBus.Unsubscribe<DragHoverChangedEvent>(HandleHoverChanged);
        }

        if (itemSlotView != null)
            itemSlotView.HideImmediately();
    }
    #endregion

    #region EventHanlders

    private void HandleDragStart(DragStartedEvent gameEvent)
    {
        _hoverItem = gameEvent.DragItem.CurrentDragItem;
        RefreshVisualState(SlotVisualState.ActiveDrag);
    }

    private void HandleDragEnd(DragEndedEvent gameEvent)
    {
        _hoverItem = null;
        RefreshVisualState(SlotVisualState.Idle);
    }

    private void HandleHoverChanged(DragHoverChangedEvent gameEvent)
    {
        ItemSlot currentHoverItemSlot = gameEvent.HoveredSlot;
        if (currentHoverItemSlot == this)
        {
            RefreshVisualState(SlotVisualState.Hovered);
        }
        else
        {
            RefreshVisualState(SlotVisualState.ActiveDrag);
        }
    }

    #endregion
    
    private void RefreshVisualState(SlotVisualState visualState)
    {
        EnsureItemSlotView();
        itemSlotView.ShowState(visualState, _hoverItem != null && CanPlaceItem(_hoverItem));
    }
    
    #region PublicMethods
    private void EnsureItemSlotView()
    {
        if (itemSlotView == null)
            itemSlotView = GetComponent<ItemSlotView>();

        if (itemSlotView == null)
            itemSlotView = gameObject.AddComponent<ItemSlotView>();
    }

    public void AddNeighbor(ItemSlot neighbor)
    {
        if(!neighbors.Contains(neighbor))
            neighbors.Add(neighbor);
    }

    public void Configure(
        SlotType slotType,
        List<ItemSlot> slotNeighbors = null,
        bool slotIsCorner = false,
        bool slotIsEdge = false,
        string newSlotId = "",
        BoardRow slotRow = BoardRow.None)
    {
        slotId = newSlotId;
        type = slotType;
        neighbors = slotNeighbors ?? new List<ItemSlot>();
        isCorner = slotIsCorner;
        isEdge = slotIsEdge;
        row = slotRow;
    }

    public bool CanPlaceItem(Item item)
    {
        if (item == null) return false;

        bool isValidSlotType = type == SlotType.Wait || type == item.ItemSlotType;
        bool hasAnotherItem = currentItem != null && currentItem != item;
        return isValidSlotType && !hasAnotherItem;
    }

    public void SeCurrentItem(Item item)
    {
        currentItem = item;
        currentItem.SetCurrentSlot(this);
    }

    public void TakeCurrentItem(Item item)
    {
        if (item == currentItem)
        {
            currentItem.SetCurrentSlot(null);
            currentItem = null;
        }
    }
    
    public ItemType GetItemTypeInSlot()
    {
        return currentItem.ItemType;
    }

    public string GetItemIdInSlot()
    {
        return currentItem.ItemId;
    }
    
    public int GetCountTreeAround()
    {
        int count = 0;
        foreach (ItemSlot neighbor in neighbors)
        {
            if(neighbor.HasCurrentItem && neighbor.GetItemTypeInSlot() == ItemType.Plant)
                count++;
        }
        return count;
    }
    
    #endregion
    
}

