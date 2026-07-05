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

    public override string GetTooltip()
    {
       return "Muon co " + nCount + " cay xung quanh";
    }
}
