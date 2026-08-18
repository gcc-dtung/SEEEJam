using System;

[Serializable]
public class NearSpecificTreeCondition : PlantCondition
{
    public string targetTreeId;
    public string targetDisplayName;

    public override bool CheckCondition(ItemSlot itemSlot)
    {
        foreach (ItemSlot neighbor in itemSlot.Neighbors)
        {
            if (neighbor.HasCurrentItem && neighbor.GetItemIdInSlot() == targetTreeId)
                return true;
        }
        return false;
    }

    public override string GetDescription()
    {
        string name = string.IsNullOrWhiteSpace(targetDisplayName)
            ? TreeNameRegistry.GetDisplayName(targetTreeId)
            : targetDisplayName;
        return $"I want to plant near {name}.";
    }
}
