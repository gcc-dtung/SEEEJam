using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Authored identity and visual selection for a plant type. Level trees refer
/// to this asset by ID, allowing many placed trees to reuse one visual setup.
/// </summary>
[CreateAssetMenu(fileName = "PlantData", menuName = "SEEE/Plants/Plant Data")]
public class PlantDataSO : ScriptableObject
{
    [field: SerializeField] public string Id { get; private set; }
    [field: SerializeField] public string DisplayName { get; private set; }
    [field: SerializeField] public PlantSkinSO Skin { get; private set; }
    [field: SerializeField] public PlantBaseSkinType BaseSkinType { get; private set; }
    [field: SerializeField] public List<PlantTrait> Traits { get; private set; } = new List<PlantTrait>();

    public int BaseSkinIndex => (int)BaseSkinType;

    public void SetData(string newId, string newDisplayName, PlantSkinSO newSkin, PlantBaseSkinType newBaseSkinType, List<PlantTrait> newTraits = null)
    {
        Id = newId;
        DisplayName = newDisplayName;
        Skin = newSkin;
        BaseSkinType = newBaseSkinType;
        Traits = newTraits ?? new List<PlantTrait>();
    }
}
