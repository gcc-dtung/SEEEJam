using UnityEngine;

public enum BoosterType
{
    Undo,
    RemoveConditions,
    Hint
}

public class BoosterInventoryManager : SingletonMonoBehaviour<BoosterInventoryManager>
{
    [Header("First Launch Defaults")]
    [SerializeField, Min(0)] private int defaultUndoCount = 50;
    [SerializeField, Min(0)] private int defaultRemoveConditionsCount = 50;
    [SerializeField, Min(0)] private int defaultHintCount = 50;

    private int _undoCount;
    private int _removeConditionsCount;
    private int _hintCount;

    protected override void Awake()
    {
        base.Awake();
        InitializeDefaultInventory();
    }

    public int GetCount(BoosterType boosterType)
    {
        switch (boosterType)
        {
            case BoosterType.Undo:
                return _undoCount;
            case BoosterType.RemoveConditions:
                return _removeConditionsCount;
            case BoosterType.Hint:
                return _hintCount;
            default:
                return 0;
        }
    }

    public bool HasBooster(BoosterType boosterType)
    {
        return GetCount(boosterType) > 0;
    }

    public bool TryConsume(BoosterType boosterType)
    {
        int currentCount = GetCount(boosterType);
        if (currentCount <= 0)
            return false;

        SetCount(boosterType, currentCount - 1);
        return true;
    }

    public void Add(BoosterType boosterType, int amount)
    {
        if (amount <= 0)
            return;

        SetCount(boosterType, GetCount(boosterType) + amount);
    }

    public void SetCount(BoosterType boosterType, int count)
    {
        SetCount(boosterType, count, true);
    }

    public void ApplySavedCounts(int undoCount, int removeConditionsCount, int hintCount)
    {
        SetCount(BoosterType.Undo, undoCount, false);
        SetCount(BoosterType.RemoveConditions, removeConditionsCount, false);
        SetCount(BoosterType.Hint, hintCount, false);
    }

    private void SetCount(BoosterType boosterType, int count, bool saveGame)
    {
        int normalizedCount = Mathf.Max(0, count);
        if (GetCount(boosterType) == normalizedCount)
            return;

        SetRuntimeCount(boosterType, normalizedCount);
        EventBus.Instance.Publish(new BoosterInventoryChangedEvent(boosterType, normalizedCount));

        if (saveGame &&
            SaveLoadManager.TryGetInstance(out SaveLoadManager saveLoadManager) &&
            !saveLoadManager.IsApplyingData)
        {
            saveLoadManager.SaveGame();
        }
    }

    private void InitializeDefaultInventory()
    {
        _undoCount = Mathf.Max(0, defaultUndoCount);
        _removeConditionsCount = Mathf.Max(0, defaultRemoveConditionsCount);
        _hintCount = Mathf.Max(0, defaultHintCount);
    }

    private void SetRuntimeCount(BoosterType boosterType, int count)
    {
        switch (boosterType)
        {
            case BoosterType.Undo:
                _undoCount = count;
                break;
            case BoosterType.RemoveConditions:
                _removeConditionsCount = count;
                break;
            case BoosterType.Hint:
                _hintCount = count;
                break;
        }
    }

}
