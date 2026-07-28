using System;

[Serializable]
public class NeighborSmellCondition : PlantCondition
{
    public PlantSmell smell;
    public SmellConditionPreference preference;

    public override bool CheckCondition(ItemSlot itemSlot)
    {
        if (itemSlot == null || smell == PlantSmell.None)
            return false;

        bool hasTargetSmellNearby = false;
        foreach (ItemSlot neighbor in itemSlot.Neighbors)
        {
            if (!neighbor.HasCurrentItem ||
                !neighbor.currentItem.TryGetEmittedSmell(out PlantSmell emittedSmell) ||
                emittedSmell != smell)
            {
                continue;
            }

            hasTargetSmellNearby = true;
            break;
        }

        return preference == SmellConditionPreference.Like
            ? hasTargetSmellNearby
            : !hasTargetSmellNearby;
    }

    public override string GetDescription()
    {
        string smellName = smell.ToString().ToLowerInvariant();
        return preference == SmellConditionPreference.Like
            ? $"I want to be near a {smellName} smell."
            : $"I must not be near a {smellName} smell.";
    }
}
