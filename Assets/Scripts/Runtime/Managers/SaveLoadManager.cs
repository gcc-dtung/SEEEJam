using System;
using UnityEngine;

public class SaveLoadManager : SingletonMonoBehaviour<SaveLoadManager>
{
    [SerializeField] private string saveFileName = Constants.SaveLoad.FileName;

    private IDataService _dataService;

    public GameData GameData { get; private set; }
    public bool IsApplyingData { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        _dataService = new JsonDataService();
        LoadGame();
    }

    [ContextMenu("Save Game")]
    public void SaveGame()
    {
        if (GameData == null)
            GameData = new GameData();

        CaptureDataFromManagers();
        GameData.savedAtUtcTicks = DateTime.UtcNow.Ticks;
        _dataService.SaveData(saveFileName, GameData);
    }

    [ContextMenu("Load Game")]
    public void LoadGame()
    {
        if (_dataService == null)
            _dataService = new JsonDataService();

        if (!_dataService.HasData(saveFileName))
        {
            GameData = new GameData
            {
                undoBoosterCount = 50,
                removeConditionsBoosterCount = 50,
                hintBoosterCount = 50
            };
            CaptureDataFromManagers();
            SaveGame();
            ApplyDataToManagers();
            return;
        }

        try
        {
            GameData = _dataService.LoadData<GameData>(saveFileName);
        }
        catch (Exception exception)
        {
            Debug.LogError("[SaveLoadManager] Save file failed to load. Creating default data. Reason: " + exception.Message);
            GameData = new GameData();
            CaptureDataFromManagers();
            SaveGame();
        }

        ApplyDataToManagers();
    }

    [ContextMenu("Reset Save")]
    public void ResetSave()
    {
        GameData = new GameData();
        ApplyDataToManagers();
        SaveGame();
    }

    private void CaptureDataFromManagers()
    {
        if (LevelManager.TryGetInstance(out LevelManager levelManager))
            GameData.currentLevelIndex = levelManager.CurrentLevelIndex;

        if (EconomyManager.TryGetInstance(out EconomyManager economyManager))
            GameData.coinCount = economyManager.CoinCount;

        if (!BoosterInventoryManager.TryGetInstance(out BoosterInventoryManager boosterInventoryManager))
            return;

        GameData.undoBoosterCount = boosterInventoryManager.GetCount(BoosterType.Undo);
        GameData.removeConditionsBoosterCount = boosterInventoryManager.GetCount(BoosterType.RemoveConditions);
        GameData.hintBoosterCount = boosterInventoryManager.GetCount(BoosterType.Hint);
    }

    private void ApplyDataToManagers()
    {
        if (GameData == null)
            GameData = new GameData();

        IsApplyingData = true;
        try
        {
            if (LevelManager.TryGetInstance(out LevelManager levelManager))
                levelManager.SetCurrentLevelIndex(GameData.currentLevelIndex);

            if (EconomyManager.TryGetInstance(out EconomyManager economyManager))
                economyManager.ApplySavedCoins(GameData.coinCount);

            if (BoosterInventoryManager.TryGetInstance(out BoosterInventoryManager boosterInventoryManager))
            {
                boosterInventoryManager.ApplySavedCounts(
                    GameData.undoBoosterCount,
                    GameData.removeConditionsBoosterCount,
                    GameData.hintBoosterCount);
            }
        }
        finally
        {
            IsApplyingData = false;
        }
    }
}
