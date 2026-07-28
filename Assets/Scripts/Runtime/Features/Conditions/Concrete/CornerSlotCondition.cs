using System;

[Serializable]
public class CornerSlotCondition : PlantCondition
{
    public override bool CheckCondition(ItemSlot itemSlot)
    {
        return itemSlot != null && itemSlot.IsCorner;
    }

    public override string GetDescription()
    {
        return "Must be placed on a corner slot.";
    }
}
