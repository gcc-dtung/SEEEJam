using UnityEngine;

public class MoveItemCommand : IBoardCommand
{
    private readonly Item _item;
    private readonly ItemSlot _sourceSlot;
    private readonly ItemSlot _targetSlot;
    private readonly DragController _dragController;
    private readonly Vector3 _sourcePosition;
    private readonly Vector3 _targetPosition;

    public MoveItemCommand(
        Item item,
        ItemSlot sourceSlot,
        ItemSlot targetSlot,
        DragController dragController,
        Vector3 sourcePosition,
        Vector3 targetPosition)
    {
        _item = item;
        _sourceSlot = sourceSlot;
        _targetSlot = targetSlot;
        _dragController = dragController;
        _sourcePosition = sourcePosition;
        _targetPosition = targetPosition;
    }

    public bool Execute()
    {
        return BoardManager.Instance.ApplyMove(
            _item,
            _sourceSlot,
            _targetSlot,
            _targetPosition,
            updateItemPosition: false);
    }

    public bool Undo()
    {
        if (_sourceSlot == null)
        {
            Debug.LogWarning("[MoveItemCommand] Undo rejected because the move has no source slot.");
            return false;
        }

        if (!BoardManager.Instance.ApplyMove(
                _item,
                _targetSlot,
                _sourceSlot,
                _sourcePosition,
                updateItemPosition: false))
        {
            return false;
        }

        if (_dragController != null)
            _dragController.MoveToRestingPosition(_sourcePosition, animated: true);
        else
            _item.transform.position = _sourcePosition;

        return true;
    }
}
