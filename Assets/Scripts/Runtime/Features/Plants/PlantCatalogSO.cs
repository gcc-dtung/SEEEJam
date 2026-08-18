using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lookup table assigned to LevelRuntimeLoader. Its IDs are written into a
/// level's TreeData.plantDataId field.
/// </summary>
[CreateAssetMenu(fileName = "PlantCatalog", menuName = "SEEE/Plants/Plant Catalog")]
public class PlantCatalogSO : ScriptableObject
{
    [SerializeField] private List<PlantDataSO> plants = new List<PlantDataSO>();

    public bool TryGetPlant(string plantId, out PlantDataSO plantData)
    {
        if (!string.IsNullOrWhiteSpace(plantId))
        {
            for (int i = 0; i < plants.Count; i++)
            {
                PlantDataSO candidate = plants[i];
                if (candidate != null && string.Equals(candidate.Id, plantId, System.StringComparison.OrdinalIgnoreCase))
                {
                    plantData = candidate;
                    return true;
                }
            }
        }

        plantData = null;
        return false;
    }
}
