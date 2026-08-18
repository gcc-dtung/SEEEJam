using System;

[Serializable]
public class RequiresLightCondition : PlantCondition
{
    public override bool CheckCondition(ItemSlot itemSlot)
    {
        return itemSlot != null && LightEmitter.IsItemLitByAny(itemSlot.CurrentItem);
    }

    public override string GetDescription()
    {
        return "I need direct light.";
    }
}
