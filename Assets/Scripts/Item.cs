using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private ItemType itemType;
    [SerializeField] private string itemId;
    
    [SerializeReference]
    [SubclassSelector]
    private List<PlantCondition> conditions = new List<PlantCondition>();

    private ItemSlot currentSlot;
    
    #region Public API
    public ItemType GetItemType() => itemType;
    public string GetItemId() => itemId;

    public bool CheckCondition()
    {
        foreach(PlantCondition cond in conditions)
            if (!cond.CheckCondition(currentSlot))
                return false;
        return true;
    }
    
    #endregion
}
