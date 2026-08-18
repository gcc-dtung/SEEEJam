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
    [field: SerializeField, Min(0)] public int BaseSkinIndex { get; private set; }
    [field: SerializeField] public List<PlantTrait> Traits { get; private set; } = new List<PlantTrait>();
}
