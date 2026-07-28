using System;
using UnityEngine;

[Serializable]
public abstract class PlantCondition
{
    public abstract bool CheckCondition(ItemSlot itemSlot);

    public virtual string GetDescription()
    {
        string typeName = GetType().Name;
        return typeName.EndsWith("Condition", StringComparison.Ordinal)
            ? typeName.Substring(0, typeName.Length - "Condition".Length)
            : typeName;
    }
}
