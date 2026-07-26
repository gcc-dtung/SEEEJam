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
            if (!neighbor.HasCurrentItem || neighbor.currentItem.Smell != smell)
                continue;

            hasTargetSmellNearby = true;
            break;
        }

        return preference == SmellConditionPreference.Like
            ? hasTargetSmellNearby
            : !hasTargetSmellNearby;
    }

    public override string GetTooltip()
    {
        string action = preference == SmellConditionPreference.Like ? "thich" : "khong thich";
        return "Muon dat canh mui " + smell + " (" + action + ")";
    }
}
