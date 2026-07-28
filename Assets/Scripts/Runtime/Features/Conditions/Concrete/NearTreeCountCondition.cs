using System;

[Serializable]
public class NearTreeCountCondition : PlantCondition
{
    public int nCount;

    public override bool CheckCondition(ItemSlot itemSlot)
    {
        int actualTree = itemSlot.GetCountTreeAround();
        return (actualTree == nCount);
    }

    public override string GetDescription()
    {
        return $"I want to be planted near only {nCount} tree(s).";
    }
}
