using UnityEngine;

public class EconomyManager : SingletonMonoBehaviour<EconomyManager>
{
    [Header("First Launch Defaults")]
    [SerializeField, Min(0)] private int defaultCoinCount;

    private int _coinCount;

    public int CoinCount => _coinCount;

    protected override void Awake()
    {
        base.Awake();
        InitializeDefaultCoins();
    }

    public int GetCoins()
    {
        return _coinCount;
    }

    public bool CanAfford(int amount)
    {
        return amount <= 0 || _coinCount >= amount;
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0)
            return;

        int newCount = amount > int.MaxValue - _coinCount
            ? int.MaxValue
            : _coinCount + amount;

        SetCoins(newCount);
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0)
            return true;

        if (!CanAfford(amount))
            return false;

        SetCoins(_coinCount - amount);
        return true;
    }

    public void SetCoins(int count)
    {
        SetCoins(count, true);
    }

    public void ApplySavedCoins(int count)
    {
        SetCoins(count, false);
    }

    private void SetCoins(int count, bool saveGame)
    {
        int normalizedCount = Mathf.Max(0, count);
        if (_coinCount == normalizedCount)
            return;

        int previousCount = _coinCount;
        _coinCount = normalizedCount;

        EventBus.Instance.Publish(new EconomyChangedEvent(previousCount, _coinCount));

        if (saveGame &&
            SaveLoadManager.TryGetInstance(out SaveLoadManager saveLoadManager) &&
            !saveLoadManager.IsApplyingData)
        {
            saveLoadManager.SaveGame();
        }
    }

    private void InitializeDefaultCoins()
    {
        _coinCount = Mathf.Max(0, defaultCoinCount);
    }
}
