using System;

[Serializable]
public class NearSpecificTreeCondition : PlantCondition
{
    public string targetTreeId;

    public override bool CheckCondition(ItemSlot itemSlot)
    {
        return itemSlot.GetItemIdInSlot() == targetTreeId;
    }
}
