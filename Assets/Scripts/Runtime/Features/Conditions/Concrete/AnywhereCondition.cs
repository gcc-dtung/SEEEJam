using System;

[Serializable]
public class AnywhereCondition : PlantCondition
{
    public override bool CheckCondition(ItemSlot itemSlot)
    {
        return true;
    }

    public override string GetDescription()
    {
        return "I can be planted anywhere.";
    }
}
