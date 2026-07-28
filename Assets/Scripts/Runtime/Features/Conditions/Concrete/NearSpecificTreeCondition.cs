using System;

[Serializable]
public class NearSpecificTreeCondition : PlantCondition
{
    public string targetTreeId;

    public override bool CheckCondition(ItemSlot itemSlot)
    {
        foreach (ItemSlot neighbor in itemSlot.Neighbors)
        {
            if (neighbor.HasCurrentItem && neighbor.GetItemIdInSlot() == targetTreeId)
                return true;
        }
        return false;
    }
}
