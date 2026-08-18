using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Art set for plants. Contains 7 base skin pairs (normal/happy & sad),
/// 3 face expressions, and optional trait overlays.
/// </summary>
[CreateAssetMenu(fileName = "PlantSkin", menuName = "SEEE/Plants/Plant Skin")]
public class PlantSkinSO : ScriptableObject
{
    [Header("7 Base Skin Pairs")]
    [SerializeField] private List<PlantBaseSkinEntry> baseSkins = new List<PlantBaseSkinEntry>();

    [Header("Face")]
    [SerializeField] private Sprite normalFace;
    [SerializeField] private Sprite happyFace;
    [SerializeField] private Sprite angryFace;

    [Header("Trait Overlay")]
    [SerializeField] private PlantTraitSkin[] traitSkins;

    public Sprite GetBaseSkin(PlantBaseSkinType skinType, PlantVisualState state)
    {
        if (baseSkins != null)
        {
            for (int i = 0; i < baseSkins.Count; i++)
            {
                if (baseSkins[i] != null && baseSkins[i].skinType == skinType)
                {
                    return state == PlantVisualState.Angry
                        ? baseSkins[i].sadSkin
                        : baseSkins[i].normalHappySkin;
                }
            }
        }

        return null;
    }

    public Sprite GetBaseSkin(int index, PlantVisualState state)
    {
        return GetBaseSkin((PlantBaseSkinType)index, state);
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

    public void SetData(
        List<PlantBaseSkinEntry> newBaseSkins,
        Sprite newNormalFace,
        Sprite newHappyFace,
        Sprite newAngryFace,
        PlantTraitSkin[] newTraitSkins = null)
    {
        baseSkins = newBaseSkins ?? new List<PlantBaseSkinEntry>();
        normalFace = newNormalFace;
        happyFace = newHappyFace;
        angryFace = newAngryFace;
        traitSkins = newTraitSkins;
    }
}

[System.Serializable]
public class PlantBaseSkinEntry
{
    public PlantBaseSkinType skinType;

    [Tooltip("Skin khi Bình thường (Normal) và Vui vẻ (Happy)")]
    public Sprite normalHappySkin;

    [Tooltip("Skin khi Buồn / Tức giận / Đặt sai vị trí (Angry)")]
    public Sprite sadSkin;
}

[System.Serializable]
public class PlantTraitSkin
{
    public PlantTrait trait;
    public Sprite sprite;
}
