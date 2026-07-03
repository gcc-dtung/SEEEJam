using System;
using UnityEngine;

[Serializable]
public abstract class PlantCondition
{
    public abstract bool CheckCondition(ItemSlot itemSlot);
}
