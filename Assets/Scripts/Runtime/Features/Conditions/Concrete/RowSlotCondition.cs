using System;

[Serializable]
public class RowSlotCondition : PlantCondition
{
    public BoardRow requiredRow = BoardRow.None;

    public override bool CheckCondition(ItemSlot itemSlot)
    {
        return itemSlot != null && requiredRow != BoardRow.None && itemSlot.Row == requiredRow;
    }

    public override string GetDescription()
    {
        if (requiredRow == BoardRow.None)
            return "I want to be planted on a configured row.";

        return "I want to be planted on " + requiredRow + ".";
    }
}
