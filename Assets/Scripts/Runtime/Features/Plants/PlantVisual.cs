using System.Collections.Generic;
using UnityEngine;

public enum PlantVisualState
{
    Normal,
    Happy,
    Angry
}

/// <summary>
/// Applies a PlantSkinSO to independent body, face, and trait sprite layers.
/// The face changes with placement validity; the trait overlay stays attached
/// to the selected PlantDataSO.
/// </summary>
public class PlantVisual : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private PlantSkinSO skinSO;
    [SerializeField] private PlantBaseSkinType baseSkinType;
    [SerializeField] private List<PlantTrait> traits = new List<PlantTrait>();

    [Header("Layers")]
    [SerializeField] private SpriteRenderer skinRenderer;
    [SerializeField] private SpriteRenderer faceRenderer;
    [SerializeField] private SpriteRenderer traitRenderer;

    private PlantVisualState _state;

    private void Awake()
    {
        EnsureRenderers();
        Refresh();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            return;

        EnsureRenderers();
        Refresh();
    }

    public void Configure(PlantDataSO data)
    {
        if (data == null)
            return;

        Configure(data.Skin, data.BaseSkinType, data.Traits);
    }

    public void Configure(PlantSkinSO skin, PlantBaseSkinType skinType, IReadOnlyList<PlantTrait> selectedTraits)
    {
        skinSO = skin;
        baseSkinType = skinType;
        traits.Clear();

        if (selectedTraits != null)
        {
            for (int i = 0; i < selectedTraits.Count; i++)
                traits.Add(selectedTraits[i]);
        }

        EnsureRenderers();
        Refresh();
    }

    public void Configure(PlantSkinSO skin, int skinIndex, IReadOnlyList<PlantTrait> selectedTraits)
    {
        Configure(skin, (PlantBaseSkinType)Mathf.Clamp(skinIndex, 0, 6), selectedTraits);
    }

    public void SetState(PlantVisualState state)
    {
        if (_state == state)
            return;

        _state = state;
        ApplySkin();
        ApplyFace();
    }

    public void AddTrait(PlantTrait trait)
    {
        if (trait != PlantTrait.None && !traits.Contains(trait))
        {
            traits.Add(trait);
            ApplyTrait();
        }
    }

    public void Refresh()
    {
        ApplySkin();
        ApplyFace();
        ApplyTrait();
    }

    /// <summary>Keeps overlay layers above the body when another view changes its sorting.</summary>
    public void SyncSortingWithSkin()
    {
        if (skinRenderer == null)
            return;

        if (faceRenderer != null)
        {
            faceRenderer.sortingLayerID = skinRenderer.sortingLayerID;
            faceRenderer.sortingOrder = skinRenderer.sortingOrder + 1;
        }

        if (traitRenderer != null)
        {
            traitRenderer.sortingLayerID = skinRenderer.sortingLayerID;
            traitRenderer.sortingOrder = skinRenderer.sortingOrder + 2;
        }
    }

    private void ApplySkin()
    {
        if (skinSO == null || skinRenderer == null)
            return;

        Sprite body = skinSO.GetBaseSkin(baseSkinType, _state);
        if (body != null)
            skinRenderer.sprite = body;
    }

    private void ApplyFace()
    {
        if (skinSO == null || faceRenderer == null)
            return;

        Sprite face = skinSO.GetFace(_state);
        faceRenderer.sprite = face;
        faceRenderer.enabled = face != null;
    }

    private void ApplyTrait()
    {
        if (skinSO == null || traitRenderer == null)
            return;

        Sprite trait = skinSO.GetTraitSkin(traits);
        traitRenderer.sprite = trait;
        traitRenderer.enabled = trait != null;
    }

    private void EnsureRenderers()
    {
        if (skinRenderer == null)
        {
            ItemView itemView = GetComponent<ItemView>();
            if (itemView != null && itemView.SpriteRenderer != null)
                skinRenderer = itemView.SpriteRenderer;
        }

        if (skinRenderer == null)
        {
            Transform viewChild = transform.Find("View");
            if (viewChild != null)
                skinRenderer = viewChild.GetComponent<SpriteRenderer>();
        }

        if (skinRenderer == null)
            skinRenderer = GetComponent<SpriteRenderer>();

        if (skinRenderer == null)
            skinRenderer = GetComponentInChildren<SpriteRenderer>();

        if (skinRenderer == null)
            return;

        Transform parentTransform = skinRenderer.transform;

        // Do not auto-create face/trait layers at runtime.
        // Keep them null unless explicitly assigned on the prefab to avoid
        // adding GameObjects when the plant is spawned.

        SyncSortingWithSkin();
    }

    private SpriteRenderer CreateLayer(string layerName, int sortingOffset, Transform parent)
    {
        Transform targetParent = parent != null ? parent : transform;
        Transform existingLayer = targetParent.Find(layerName);
        SpriteRenderer layerRenderer = existingLayer != null
            ? existingLayer.GetComponent<SpriteRenderer>()
            : null;

        if (layerRenderer == null)
        {
            GameObject layer = new GameObject(layerName);
            layer.transform.SetParent(targetParent, false);
            layerRenderer = layer.AddComponent<SpriteRenderer>();
        }

        layerRenderer.sortingLayerID = skinRenderer.sortingLayerID;
        layerRenderer.sortingOrder = skinRenderer.sortingOrder + sortingOffset;
        return layerRenderer;
    }
}
