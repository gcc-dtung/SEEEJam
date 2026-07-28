using UnityEngine;

public class PlantAloneCondition : PlantCondition
{
    public override bool CheckCondition(ItemSlot itemSlot)
    {
        int actualTree = itemSlot.GetCountTreeAround();
        return actualTree == 0;
    }

    public override string GetDescription()
    {
        return "I want to be planted alone.";
    }
}
