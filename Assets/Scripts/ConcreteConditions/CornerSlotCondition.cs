using System;

[Serializable]
public class CornerSlotCondition : PlantCondition
{
    public override bool CheckCondition(ItemSlot itemSlot)
    {
        return itemSlot != null && itemSlot.IsCorner;
    }

    public override string GetTooltip()
    {
        return "Muon dat o goc khu vuon";
    }
}
