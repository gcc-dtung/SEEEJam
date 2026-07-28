using System.Collections.Generic;

public class MoveManager : SingletonMonoBehaviour<MoveManager>
{
    private readonly Stack<IBoardCommand> _history = new Stack<IBoardCommand>();

    public bool CanUndoLastMove { get; private set; }

    public bool ExecuteMove(IBoardCommand command)
    {
        if (command == null || GameManager.Instance.CurrentState != GameState.Playing)
            return false;

        if (!LevelManager.Instance.HasRemainingMoves)
            return false;

        if (!command.Execute())
            return false;

        _history.Push(command);
        LevelManager.Instance.TrySpendMove();
        SetUndoAvailability(true);
        return true;
    }

    public bool UndoLastMove()
    {
        if (!CanUndoLastMove || _history.Count == 0)
            return false;

        IBoardCommand command = _history.Peek();
        GameState previousState = GameManager.Instance.CurrentState;
        bool wasTerminal = previousState == GameState.Won || previousState == GameState.Lost;
        if (wasTerminal)
            GameManager.Instance.SetState(GameState.Playing);

        if (!command.Undo())
        {
            if (wasTerminal)
                GameManager.Instance.SetState(previousState);
            return false;
        }

        _history.Pop();
        SetUndoAvailability(false);
        LevelManager.Instance.RestoreMove();
        return true;
    }

    public void ClearHistory()
    {
        _history.Clear();
        SetUndoAvailability(false);
    }

    private void SetUndoAvailability(bool canUndo)
    {
        if (CanUndoLastMove == canUndo)
            return;

        CanUndoLastMove = canUndo;
        EventBus.Instance.Publish(new UndoAvailabilityChangedEvent(CanUndoLastMove));
    }
}
