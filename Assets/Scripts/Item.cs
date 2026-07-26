using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private ItemType itemType;
    [SerializeField] private SlotType itemSlotType;
    [SerializeField] private string itemId;
    [SerializeField] private PlantSmell smell;
    [SerializeField] private SpriteRenderer itemSprite;
    
    [SerializeReference]
    [SubclassSelector]
    private List<PlantCondition> conditions = new List<PlantCondition>();

    private ItemSlot currentSlot;

    private void Awake()
    {
        EnsureItemSprite();
    }

    private void OnEnable()
    {
        EnsureItemSprite();
        DragItem.OnBoardChanged.AddListener(RefreshCurrentSlotVisual);
    }

    private void OnDisable()
    {
        DragItem.OnBoardChanged.RemoveListener(RefreshCurrentSlotVisual);
    }
    
    #region Public API
    public SlotType ItemSlotType => itemSlotType;
    public ItemType ItemType => itemType;
    public string ItemId => itemId;
    public PlantSmell Smell => smell;

    public void Configure(ItemType newItemType, SlotType newItemSlotType, string newItemId, PlantSmell newSmell, List<PlantCondition> newConditions)
    {
        itemType = newItemType;
        itemSlotType = newItemSlotType;
        itemId = newItemId;
        smell = newSmell;
        conditions = newConditions ?? new List<PlantCondition>();
    }

    private bool CheckCondition(ItemSlot slot)
    {
        if(conditions!= null)
            foreach(PlantCondition cond in conditions)
                if (!cond.CheckCondition(slot))
                    return false;
        return true;
    }

    public string GetTooltip()
    {
        string itemTooltip = itemId + ":\n";
        if (conditions == null || conditions.Count == 0)
            return itemId + ":\ncan place anywhere";
        foreach(PlantCondition cond in conditions)
            itemTooltip += cond.GetTooltip() + '\n';
        return itemTooltip;
    }

    public void SpriteWhenNormal()
    {
        EnsureItemSprite();
        if (itemSprite == null) return;
        itemSprite.color = Color.white;
    }

    public void SpriteWhenWrong()
    {
        EnsureItemSprite();
        if (itemSprite == null) return;
        itemSprite.color = Color.red;
    }

    public void SpriteWhenCorrect()
    {
        EnsureItemSprite();
        if (itemSprite == null) return;
        itemSprite.color = Color.blue;
    }

    public void UpdateVisualItem(ItemSlot slot)
    {
        currentSlot = slot;

        if (slot.Type == SlotType.Wait)
        {
            SpriteWhenNormal();
        }
        else if (CheckCondition(slot))
        {
            SpriteWhenCorrect();
        }
        else
        {
            SpriteWhenWrong();
        }
    }

    public void SetCurrentSlot(ItemSlot slot)
    {
        currentSlot = slot;
    }

    private void RefreshCurrentSlotVisual()
    {
        if (currentSlot == null) return;

        UpdateVisualItem(currentSlot);
    }

    private void EnsureItemSprite()
    {
        if (itemSprite == null)
            itemSprite = GetComponentInChildren<SpriteRenderer>();
    }
    
    #endregion
}
