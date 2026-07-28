public class PlacementService
{
    public bool CanPlace(Item item, ItemSlot targetSlot)
    {
        return item != null && targetSlot != null && targetSlot.CanPlaceItem(item);
    }
}
