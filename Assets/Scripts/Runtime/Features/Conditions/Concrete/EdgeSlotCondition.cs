using System;

[Serializable]
public class EdgeSlotCondition : PlantCondition
{
    public override bool CheckCondition(ItemSlot itemSlot)
    {
        return itemSlot != null && itemSlot.IsEdge;
    }
}
