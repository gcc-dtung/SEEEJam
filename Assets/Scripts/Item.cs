using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private ItemType itemType;
    [SerializeField] private SlotType itemSlotType;
    [SerializeField] private string itemId;
    [SerializeField] private SpriteRenderer itemSprite;
    
    [SerializeReference]
    [SubclassSelector]
    private List<PlantCondition> conditions = new List<PlantCondition>();

    private ItemSlot currentSlot;
    

    private void OnEnable()
    {
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
        itemSprite.color = Color.white;
    }

    public void SpriteWhenWrong()
    {
        itemSprite.color = Color.red;
    }

    public void SpriteWhenCorrect()
    {
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
    
    #endregion
}
