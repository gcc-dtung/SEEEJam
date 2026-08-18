using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Art set for one family of plants. A plant is drawn in three layers:
/// body/skin, face, and an optional trait overlay.
/// </summary>
[CreateAssetMenu(fileName = "PlantSkin", menuName = "SEEE/Plants/Plant Skin")]
public class PlantSkinSO : ScriptableObject
{
    [Header("Base Skin")]
    [SerializeField] private Sprite[] baseSkins;

    [Header("Face")]
    [SerializeField] private Sprite normalFace;
    [SerializeField] private Sprite happyFace;
    [SerializeField] private Sprite angryFace;

    [Header("Trait Overlay")]
    [SerializeField] private PlantTraitSkin[] traitSkins;

    public Sprite GetBaseSkin(int index = 0)
    {
        if (baseSkins == null || baseSkins.Length == 0)
            return null;

        return baseSkins[Mathf.Clamp(index, 0, baseSkins.Length - 1)];
    }

    public Sprite GetFace(PlantVisualState state)
    {
        switch (state)
        {
            case PlantVisualState.Happy:
                return happyFace;
            case PlantVisualState.Angry:
                return angryFace;
            default:
                return normalFace;
        }
    }

    public Sprite GetTraitSkin(IReadOnlyList<PlantTrait> traits)
    {
        if (traits == null || traitSkins == null)
            return null;

        for (int i = 0; i < traits.Count; i++)
        {
            Sprite traitSkin = GetTraitSkin(traits[i]);
            if (traitSkin != null)
                return traitSkin;
        }

        return null;
    }

    public Sprite GetTraitSkin(PlantTrait trait)
    {
        if (trait == PlantTrait.None || traitSkins == null)
            return null;

        for (int i = 0; i < traitSkins.Length; i++)
        {
            if (traitSkins[i].trait == trait)
                return traitSkins[i].sprite;
        }

        return null;
    }
}

[System.Serializable]
public class PlantTraitSkin
{
    public PlantTrait trait;
    public Sprite sprite;
}
