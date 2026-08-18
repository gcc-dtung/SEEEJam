using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Emits one straight 2D light ray. A plant is lit only when it is the first
/// plant hit in the selected cardinal direction.
/// </summary>
public class LightEmitter : MonoBehaviour
{
    private static readonly HashSet<LightEmitter> ActiveEmitters = new HashSet<LightEmitter>();

    [Header("Light")]
    [SerializeField] private LightDirection direction = LightDirection.Up;
    [SerializeField, Min(0f)] private float range = 20f;
    [SerializeField] private LayerMask raycastMask = Physics2D.DefaultRaycastLayers;
    [SerializeField] private Vector2 rayOriginOffset;

    [Header("Directional Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite upSprite;
    [SerializeField] private Sprite downSprite;
    [SerializeField] private Sprite sideSprite;

    public LightDirection Direction => direction;

    private void Awake()
    {
        EnsureSpriteRenderer();
        RefreshVisual();
    }

    private void OnEnable()
    {
        ActiveEmitters.Add(this);
    }

    private void OnDisable()
    {
        ActiveEmitters.Remove(this);
    }

    private void OnValidate()
    {
        EnsureSpriteRenderer();
        RefreshVisual();
    }

    public void Configure(LightDirection newDirection)
    {
        direction = newDirection;
        RefreshVisual();
    }

    public void ConfigureVisuals(Sprite newUpSprite, Sprite newDownSprite, Sprite newSideSprite)
    {
        upSprite = newUpSprite;
        downSprite = newDownSprite;
        sideSprite = newSideSprite;
        RefreshVisual();
    }

    public bool IsLighting(Item item)
    {
        if (item == null || item.ItemType != ItemType.Plant)
            return false;

        Vector2 rayDirection = direction.ToVector();
        Vector2 rayOrigin = (Vector2)transform.position + rayOriginOffset;
        RaycastHit2D[] hits = Physics2D.RaycastAll(rayOrigin, rayDirection, range, raycastMask);

        float closestPlantDistance = float.PositiveInfinity;
        Item closestPlant = null;
        Item sourceItem = GetComponent<Item>();
        for (int i = 0; i < hits.Length; i++)
        {
            Item hitItem = hits[i].collider.GetComponentInParent<Item>();
            if (hitItem == null || hitItem == sourceItem || hitItem.ItemType != ItemType.Plant)
                continue;

            if (hits[i].distance < closestPlantDistance)
            {
                closestPlantDistance = hits[i].distance;
                closestPlant = hitItem;
            }
        }

        return closestPlant == item;
    }

    public static bool IsItemLitByAny(Item item)
    {
        foreach (LightEmitter emitter in ActiveEmitters)
        {
            if (emitter != null && emitter.isActiveAndEnabled && emitter.IsLighting(item))
                return true;
        }

        return false;
    }

    private void RefreshVisual()
    {
        EnsureSpriteRenderer();
        if (spriteRenderer == null)
            return;

        spriteRenderer.flipX = direction == LightDirection.Left;
        if (direction == LightDirection.Up && upSprite != null)
            spriteRenderer.sprite = upSprite;
        else if (direction == LightDirection.Down && downSprite != null)
            spriteRenderer.sprite = downSprite;
        else if (sideSprite != null)
            spriteRenderer.sprite = sideSprite;
    }

    private void EnsureSpriteRenderer()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position + (Vector3)rayOriginOffset,
            transform.position + (Vector3)rayOriginOffset + (Vector3)direction.ToVector() * range);
    }
}
