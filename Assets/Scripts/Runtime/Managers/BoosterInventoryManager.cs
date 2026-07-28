using UnityEngine;

public enum BoosterType
{
    Undo,
    RemoveConditions,
    Hint
}

public class BoosterInventoryManager : SingletonMonoBehaviour<BoosterInventoryManager>
{
    private const string UndoCountKey = "SEEE.Boosters.Undo";
    private const string RemoveConditionsCountKey = "SEEE.Boosters.RemoveConditions";
    private const string HintCountKey = "SEEE.Boosters.Hint";

    [Header("First Launch Defaults")]
    [SerializeField, Min(0)] private int defaultUndoCount = 3;
    [SerializeField, Min(0)] private int defaultRemoveConditionsCount = 3;
    [SerializeField, Min(0)] private int defaultHintCount = 3;

    private int _undoCount;
    private int _removeConditionsCount;
    private int _hintCount;

    protected override void Awake()
    {
        base.Awake();
        LoadInventory();
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
        int normalizedCount = Mathf.Max(0, count);
        if (GetCount(boosterType) == normalizedCount)
            return;

        SetRuntimeCount(boosterType, normalizedCount);
        PlayerPrefs.SetInt(GetStorageKey(boosterType), normalizedCount);
        PlayerPrefs.Save();
        EventBus.Instance.Publish(new BoosterInventoryChangedEvent(boosterType, normalizedCount));
    }

    private void LoadInventory()
    {
        bool createdStorage = false;
        _undoCount = LoadCount(UndoCountKey, defaultUndoCount, ref createdStorage);
        _removeConditionsCount = LoadCount(
            RemoveConditionsCountKey,
            defaultRemoveConditionsCount,
            ref createdStorage);
        _hintCount = LoadCount(HintCountKey, defaultHintCount, ref createdStorage);

        if (createdStorage)
            PlayerPrefs.Save();
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

    private static string GetStorageKey(BoosterType boosterType)
    {
        switch (boosterType)
        {
            case BoosterType.Undo:
                return UndoCountKey;
            case BoosterType.RemoveConditions:
                return RemoveConditionsCountKey;
            case BoosterType.Hint:
                return HintCountKey;
            default:
                return string.Empty;
        }
    }

    private static int LoadCount(string key, int defaultCount, ref bool createdStorage)
    {
        if (PlayerPrefs.HasKey(key))
            return Mathf.Max(0, PlayerPrefs.GetInt(key));

        int normalizedDefault = Mathf.Max(0, defaultCount);
        PlayerPrefs.SetInt(key, normalizedDefault);
        createdStorage = true;
        return normalizedDefault;
    }
}
