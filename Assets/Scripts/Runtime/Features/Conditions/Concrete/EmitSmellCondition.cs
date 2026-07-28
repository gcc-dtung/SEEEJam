using System;

[Serializable]
public class EmitSmellCondition : PlantCondition
{
    public PlantSmell smell;
    public PlantSmell Smell => smell;

    public override bool CheckCondition(ItemSlot itemSlot)
    {
        return true;
    }

    public override string GetDescription()
    {
        switch (smell)
        {
            case PlantSmell.Perfume:
                return "Has a good smell.";
            case PlantSmell.Disgust:
                return "Has a bad smell.";
            default:
                return "Has no smell.";
        }
    }
}
