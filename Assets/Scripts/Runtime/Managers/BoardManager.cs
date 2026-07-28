using System.Collections.Generic;
using UnityEngine;

public class BoardManager : SingletonMonoBehaviour<BoardManager>
{
    private readonly List<ItemSlot> _boardSlots = new List<ItemSlot>();
    private readonly List<Item> _levelItems = new List<Item>();
    private readonly Dictionary<string, ItemSlot> _boardSlotsById = new Dictionary<string, ItemSlot>();

    public IReadOnlyList<Item> LevelItems => _levelItems;

    public void RegisterLevelBoard(IEnumerable<ItemSlot> boardSlots, IEnumerable<Item> levelItems)
    {
        _boardSlots.Clear();
        _levelItems.Clear();
        _boardSlotsById.Clear();

        if (boardSlots != null)
            _boardSlots.AddRange(boardSlots);

        foreach (ItemSlot slot in _boardSlots)
        {
            if (slot != null && !string.IsNullOrWhiteSpace(slot.SlotId))
                _boardSlotsById[slot.SlotId] = slot;
        }

        if (levelItems != null)
            _levelItems.AddRange(levelItems);

        NotifyBoardChanged();
    }

    public bool ApplyMove(
        Item item,
        ItemSlot sourceSlot,
        ItemSlot targetSlot,
        Vector3 targetPosition,
        bool updateItemPosition = true)
    {
        if (item == null || targetSlot == null)
            return false;

        if (sourceSlot != null && sourceSlot != targetSlot)
            sourceSlot.TakeCurrentItem(item);

        targetSlot.SeCurrentItem(item);
        if (updateItemPosition)
            item.transform.position = targetPosition;

        NotifyRulesChanged();
        return true;
    }

    public void NotifyBoardChanged()
    {
        EventBus.Instance.Publish(new BoardChangedEvent());
    }

    public void NotifyRulesChanged()
    {
        NotifyBoardChanged();
        EvaluateWinCondition();
    }

    public bool TryGetBoardSlot(string slotId, out ItemSlot slot)
    {
        if (string.IsNullOrWhiteSpace(slotId))
        {
            slot = null;
            return false;
        }

        return _boardSlotsById.TryGetValue(slotId, out slot);
    }

    private void EvaluateWinCondition()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing)
            return;

        if (_levelItems.Count == 0)
            return;

        foreach (Item item in _levelItems)
        {
            if (item == null || !item.IsSatisfiedOnBoard())
                return;
        }

        GameManager.Instance.SetState(GameState.Won);
    }
}
