using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

public class ItemSlot : MonoBehaviour
{
    #region Fields

    [Header("Config")] 
    [SerializeField] private SlotType type;
    [SerializeField] private List<ItemSlot> neighbors;
    
    [Header("Visual Elements")]
    [SerializeField] private SpriteRenderer hoverIndicatorSprite;
    
    [Header("Animation Settiing")]
    [SerializeField] private float dragStateScale;
    [SerializeField] private float scaleTransitionDuration = 0.2f;
    
    private SlotVisualState _currentVisualState = SlotVisualState.Idle;
    private Tween _scaleTween;
    private Item _hoverItem;
    
    public SlotType Type => type;
    public Item currentItem;
    public bool HasCurrentItem => currentItem != null;
    public List<ItemSlot> Neighbors => neighbors;
    #endregion

    #region LifeCycle
    private void OnEnable()
    {
        DragItem.OnDragStart.AddListener(HandleDragStart);
        DragItem.OnDragEnd.AddListener(HandleDragEnd);
        DragItem.OnHoverChanged.AddListener(HandleHoverChanged);
    }

    private void Start()
    {
        hoverIndicatorSprite.transform.localScale = Vector3.zero;
        foreach (ItemSlot neighbor in neighbors)
        {
            neighbor.AddNeighbor(this);
        }
    }   

    private void OnDisable()
    {
        DragItem.OnDragStart.RemoveListener(HandleDragStart);
        DragItem.OnDragEnd.RemoveListener(HandleDragEnd);
        DragItem.OnHoverChanged.RemoveListener(HandleHoverChanged);
        if(_scaleTween.isAlive) _scaleTween.Stop();
    }
    #endregion

    #region EventHanlders

    private void HandleDragStart(DragItem item)
    {
        item.CurrentDragItem.SpriteWhenNormal();
        _hoverItem = item.CurrentDragItem;
        SetVisualState(SlotVisualState.ActiveDrag);
    }

    private void HandleDragEnd(DragItem item)
    {
        _hoverItem = null;
        SetVisualState(SlotVisualState.Idle);
    }

    private void HandleHoverChanged(DragItem item, ItemSlot currentHoverItemSlot)
    {
        if (currentHoverItemSlot == this)
        {
            SetVisualState(SlotVisualState.Hovered);
        }
        else
        {
            SetVisualState(SlotVisualState.ActiveDrag);
        }
    }

    #endregion
    
    #region PrivateMethods
    private void ApplyVisualState()
    {
        if (_hoverItem == null)
        {
            TweenHoverIndicatorScale(0f);
            return;
        }
        if (!CanPlaceItem(_hoverItem))
        {
            TweenHoverIndicatorScale(0f);
            return;
        }

        switch (_currentVisualState)
        {
            case SlotVisualState.ActiveDrag:
                TweenHoverIndicatorScale(dragStateScale);
                break;
            case SlotVisualState.Idle:
                TweenHoverIndicatorScale(0f);
                break;
            case SlotVisualState.Hovered:
                TweenHoverIndicatorScale(1);
                break;
        }
    }
    
    private void TweenHoverIndicatorScale(float newSize)
    {
        if(_scaleTween.isAlive)
            _scaleTween.Stop();
        if(hoverIndicatorSprite.transform.localScale != newSize * Vector3.one)
            _scaleTween = Tween.Scale(hoverIndicatorSprite.transform, newSize, scaleTransitionDuration);
    }
    #endregion
    
    #region PublicMethods
    public void SetVisualState(SlotVisualState visualState)
    {
        if(_currentVisualState == visualState) return;

        _currentVisualState = visualState;
        ApplyVisualState();
    }

    public void AddNeighbor(ItemSlot neighbor)
    {
        if(!neighbors.Contains(neighbor))
            neighbors.Add(neighbor);
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

