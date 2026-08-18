using UnityEngine;

public class LevelManager : SingletonMonoBehaviour<LevelManager>
{
    private LevelRuntimeLoader _levelLoader;
    private TextAsset[] _levelSequence;

    public int MaxMoves { get; private set; }
    public int RemainingMoves { get; private set; }
    public bool HasRemainingMoves => RemainingMoves > 0;
    public int CurrentLevelIndex { get; private set; }
    public string CurrentLevelName { get; private set; } = string.Empty;
    public int LevelCount => _levelSequence != null ? _levelSequence.Length : 0;
    public bool HasLevelSequence => LevelCount > 0;

    public void ConfigureLevelFlow(LevelRuntimeLoader levelLoader, TextAsset[] levelSequence = null)
    {
        _levelLoader = levelLoader;
        _levelSequence = levelSequence;
        CurrentLevelIndex = 0;
    }

    public bool LoadCurrentLevel()
    {
        if (_levelLoader == null)
        {
            Debug.LogWarning("[LevelManager] No LevelRuntimeLoader is configured.");
            return false;
        }

        if (_levelSequence != null && _levelSequence.Length > 0)
            return LoadLevelAt(CurrentLevelIndex);

        return LoadAssignedLevel(_levelLoader);
    }

    public bool LoadAssignedLevel(LevelRuntimeLoader levelLoader)
    {
        if (levelLoader == null)
        {
            Debug.LogWarning("[LevelManager] No LevelRuntimeLoader was provided.");
            return false;
        }

        _levelLoader = levelLoader;
        GameManager.Instance.SetState(GameState.LoadingLevel);
        bool didLoad = levelLoader.LoadAssignedLevel();

        CompleteLevelLoad(didLoad);

        return didLoad;
    }

    public bool LoadLevelAt(int levelIndex)
    {
        if (_levelLoader == null)
        {
            Debug.LogWarning("[LevelManager] No LevelRuntimeLoader is configured.");
            return false;
        }

        if (_levelSequence == null || _levelSequence.Length == 0)
            return LoadAssignedLevel(_levelLoader);

        if (levelIndex < 0 || levelIndex >= _levelSequence.Length)
        {
            Debug.LogWarning("[LevelManager] Level index is out of range: " + levelIndex);
            return false;
        }

        TextAsset levelAsset = _levelSequence[levelIndex];
        if (levelAsset == null)
        {
            Debug.LogWarning("[LevelManager] Level asset is missing at index: " + levelIndex);
            return false;
        }

        CurrentLevelIndex = levelIndex;
        GameManager.Instance.SetState(GameState.LoadingLevel);
        bool didLoad = _levelLoader.LoadLevelAsset(levelAsset);
        CompleteLevelLoad(didLoad);
        SaveProgressIfNeeded(didLoad);
        return didLoad;
    }

    public void SetCurrentLevelIndex(int levelIndex)
    {
        if (_levelSequence == null || _levelSequence.Length == 0)
        {
            CurrentLevelIndex = Mathf.Max(0, levelIndex);
            return;
        }

        CurrentLevelIndex = Mathf.Clamp(levelIndex, 0, _levelSequence.Length - 1);
    }

    public bool ReloadLevel()
    {
        return LoadCurrentLevel();
    }

    public bool LoadNextLevel()
    {
        if (_levelSequence == null || _levelSequence.Length == 0)
        {
            Debug.LogWarning("[LevelManager] No level sequence is configured.");
            return false;
        }

        if (CurrentLevelIndex >= _levelSequence.Length - 1)
            return false;

        return LoadLevelAt(CurrentLevelIndex + 1);
    }

    public bool LoadPreviousLevel()
    {
        if (_levelSequence == null || _levelSequence.Length == 0)
        {
            Debug.LogWarning("[LevelManager] No level sequence is configured.");
            return false;
        }

        if (CurrentLevelIndex <= 0)
            return false;

        return LoadLevelAt(CurrentLevelIndex - 1);
    }

    public void ConfigureLoadedLevel(LevelData level)
    {
        MaxMoves = level != null ? Mathf.Max(0, level.maxMoves) : 0;
        RemainingMoves = MaxMoves;

        // Always derive the displayed level number from the 1-based index instead of
        // trusting the JSON's "levelName" field, so it can never show "Level 0" or a
        // mismatched name (e.g. a copy-pasted level with a stale levelName).
        CurrentLevelName = "Level " + (CurrentLevelIndex + 1);

        EventBus.Instance.Publish(new LevelChangedEvent(CurrentLevelName));
        EventBus.Instance.Publish(new MovesChangedEvent(RemainingMoves, MaxMoves));
    }

    public bool TrySpendMove()
    {
        if (!HasRemainingMoves)
            return false;

        RemainingMoves--;
        EventBus.Instance.Publish(new MovesChangedEvent(RemainingMoves, MaxMoves));

        if (RemainingMoves <= 0 && GameManager.Instance.CurrentState == GameState.Playing)
            GameManager.Instance.SetState(GameState.Lost);

        return true;
    }

    public void RestoreMove()
    {
        if (RemainingMoves >= MaxMoves)
            return;

        RemainingMoves++;
        EventBus.Instance.Publish(new MovesChangedEvent(RemainingMoves, MaxMoves));
    }

    public void SetRemainingMovesForPlaytest(int remainingMoves)
    {
        RemainingMoves = Mathf.Clamp(remainingMoves, 0, MaxMoves);
        EventBus.Instance.Publish(new MovesChangedEvent(RemainingMoves, MaxMoves));

        if (RemainingMoves > 0 && GameManager.Instance.CurrentState == GameState.Lost)
            GameManager.Instance.SetState(GameState.Playing);
        else if (RemainingMoves <= 0 && GameManager.Instance.CurrentState == GameState.Playing)
            GameManager.Instance.SetState(GameState.Lost);
    }

    private void CompleteLevelLoad(bool didLoad)
    {
        if (!didLoad)
        {
            GameManager.Instance.SetState(GameState.Boot);
            return;
        }

        GameManager.Instance.SetState(GameState.Playing);
        if (RemainingMoves <= 0)
            GameManager.Instance.SetState(GameState.Lost);
    }

    private static void SaveProgressIfNeeded(bool didLoad)
    {
        if (!didLoad)
            return;

        if (SaveLoadManager.TryGetInstance(out SaveLoadManager saveLoadManager) &&
            !saveLoadManager.IsApplyingData)
        {
            saveLoadManager.SaveGame();
        }
    }
}
