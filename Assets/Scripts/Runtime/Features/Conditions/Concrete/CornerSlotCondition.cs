using System;

[Serializable]
public class CornerSlotCondition : PlantCondition
{
    public override bool CheckCondition(ItemSlot itemSlot)
    {
        return itemSlot != null && itemSlot.IsCorner;
    }
}
