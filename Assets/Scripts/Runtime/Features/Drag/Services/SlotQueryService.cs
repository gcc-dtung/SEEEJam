using UnityEngine;

public class SlotQueryService
{
    private readonly LayerMask _slotLayerMask;

    public SlotQueryService(LayerMask slotLayerMask)
    {
        _slotLayerMask = slotLayerMask;
    }

    public ItemSlot GetSlotAt(Vector2 worldPosition)
    {
        Collider2D hit = Physics2D.OverlapPoint(worldPosition, _slotLayerMask);
        return hit != null ? hit.GetComponent<ItemSlot>() : null;
    }
}
