using UnityEngine;

public class GameManager : SingletonMonoBehaviour<GameManager>
{
    public GameState CurrentState { get; private set; } = GameState.Boot;

    public void SetState(GameState newState)
    {
        if (CurrentState == newState)
            return;

        GameState previousState = CurrentState;
        CurrentState = newState;

        if (CurrentState == GameState.Won)
            Debug.Log("[GameManager] WIN");
        else if (CurrentState == GameState.Lost)
            Debug.Log("[GameManager] LOSE");

        EventBus.Instance.Publish(new GameStateChangedEvent(previousState, CurrentState));
    }
}
