using UnityEngine;

public readonly struct ItemPointerStateChangedEvent
{
    public DragItem DragItem { get; }
    public Item Item { get; }
    public ItemPointerState State { get; }
    public Vector2 ScreenPosition { get; }

    public ItemPointerStateChangedEvent(
        DragItem dragItem,
        Item item,
        ItemPointerState state,
        Vector2 screenPosition)
    {
        DragItem = dragItem;
        Item = item;
        State = state;
        ScreenPosition = screenPosition;
    }
}

public readonly struct DragStartedEvent
{
    public DragItem DragItem { get; }

    public DragStartedEvent(DragItem dragItem)
    {
        DragItem = dragItem;
    }
}

public readonly struct DragEndedEvent
{
    public DragItem DragItem { get; }

    public DragEndedEvent(DragItem dragItem)
    {
        DragItem = dragItem;
    }
}

public readonly struct DragHoverChangedEvent
{
    public DragItem DragItem { get; }
    public ItemSlot HoveredSlot { get; }

    public DragHoverChangedEvent(DragItem dragItem, ItemSlot hoveredSlot)
    {
        DragItem = dragItem;
        HoveredSlot = hoveredSlot;
    }
}

public readonly struct BoardChangedEvent
{
}

public readonly struct UndoAvailabilityChangedEvent
{
    public bool CanUndo { get; }

    public UndoAvailabilityChangedEvent(bool canUndo)
    {
        CanUndo = canUndo;
    }
}

public readonly struct BoosterSelectionChangedEvent
{
    public BoosterSelectionMode SelectionMode { get; }

    public BoosterSelectionChangedEvent(BoosterSelectionMode selectionMode)
    {
        SelectionMode = selectionMode;
    }
}

public readonly struct BoosterInventoryChangedEvent
{
    public BoosterType BoosterType { get; }
    public int RemainingCount { get; }

    public BoosterInventoryChangedEvent(BoosterType boosterType, int remainingCount)
    {
        BoosterType = boosterType;
        RemainingCount = remainingCount;
    }
}

public readonly struct OutOfBoosterRequestedEvent
{
    public BoosterType BoosterType { get; }

    public OutOfBoosterRequestedEvent(BoosterType boosterType)
    {
        BoosterType = boosterType;
    }
}

public readonly struct EconomyChangedEvent
{
    public int PreviousCoins { get; }
    public int CurrentCoins { get; }
    public int Delta => CurrentCoins - PreviousCoins;

    public EconomyChangedEvent(int previousCoins, int currentCoins)
    {
        PreviousCoins = previousCoins;
        CurrentCoins = currentCoins;
    }
}

public readonly struct HintRevealedEvent
{
    public Item Item { get; }
    public ItemSlot TargetSlot { get; }
    public float Duration { get; }

    public HintRevealedEvent(Item item, ItemSlot targetSlot, float duration)
    {
        Item = item;
        TargetSlot = targetSlot;
        Duration = duration;
    }
}

public readonly struct MovesChangedEvent
{
    public int RemainingMoves { get; }
    public int MaxMoves { get; }

    public MovesChangedEvent(int remainingMoves, int maxMoves)
    {
        RemainingMoves = remainingMoves;
        MaxMoves = maxMoves;
    }
}

public readonly struct LevelChangedEvent
{
    public string LevelName { get; }
    public LevelChangedEvent(string levelName)
    {
        LevelName = levelName;
    }
}

public readonly struct GameStateChangedEvent
{
    public GameState PreviousState { get; }
    public GameState CurrentState { get; }

    public GameStateChangedEvent(GameState previousState, GameState currentState)
    {
        PreviousState = previousState;
        CurrentState = currentState;
    }
}
