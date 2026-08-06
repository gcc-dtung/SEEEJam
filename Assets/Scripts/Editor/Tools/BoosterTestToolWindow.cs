using System;
using UnityEditor;
using UnityEngine;

public class BoosterTestToolWindow : EditorWindow
{
    private const int DefaultBoosterCount = 3;

    private int _coinCount;
    private int _undoCount;
    private int _removeConditionsCount;
    private int _hintCount;
    private int _setAllCount = 3;

    [MenuItem("Tools/Booster Test Tool")]
    public static void ShowWindow()
    {
        BoosterTestToolWindow window = GetWindow<BoosterTestToolWindow>("Booster Test Tool");
        window.minSize = new Vector2(360f, 320f);
        window.RefreshCounts();
        window.Show();
    }

    private void OnEnable()
    {
        RefreshCounts();
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
    }

    private void OnGUI()
    {
        DrawHeader();

        EditorGUILayout.Space(8f);
        DrawCoinRow();

        EditorGUILayout.Space(8f);
        DrawBoosterRow("Undo", BoosterType.Undo, ref _undoCount);
        DrawBoosterRow("Remove Conditions", BoosterType.RemoveConditions, ref _removeConditionsCount);
        DrawBoosterRow("Hint", BoosterType.Hint, ref _hintCount);

        EditorGUILayout.Space(10f);
        DrawBulkControls();

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Refresh", GUILayout.Height(28f)))
            RefreshCounts();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Booster Inventory", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            Application.isPlaying
                ? "Play Mode: changes update runtime managers and JSON save."
                : "Edit Mode: changes update JSON save for the next Play session.",
            MessageType.Info);
    }

    private void DrawCoinRow()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Coins", EditorStyles.boldLabel, GUILayout.Width(140f));

        EditorGUI.BeginChangeCheck();
        int editedCount = EditorGUILayout.IntField(Mathf.Max(0, _coinCount), GUILayout.MinWidth(60f));
        if (EditorGUI.EndChangeCheck())
        {
            _coinCount = Mathf.Max(0, editedCount);
            SetCoins(_coinCount);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+100"))
            ChangeCoins(100);
        if (GUILayout.Button("+1000"))
            ChangeCoins(1000);
        if (GUILayout.Button("Set 0"))
        {
            _coinCount = 0;
            SetCoins(_coinCount);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawBoosterRow(string label, BoosterType boosterType, ref int count)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Width(140f));

        EditorGUI.BeginChangeCheck();
        int editedCount = EditorGUILayout.IntField(Mathf.Max(0, count), GUILayout.MinWidth(60f));
        if (EditorGUI.EndChangeCheck())
        {
            count = Mathf.Max(0, editedCount);
            SetCount(boosterType, count);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+1"))
            ChangeCount(boosterType, ref count, 1);
        if (GUILayout.Button("+5"))
            ChangeCount(boosterType, ref count, 5);
        if (GUILayout.Button("Set 0"))
        {
            count = 0;
            SetCount(boosterType, count);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawBulkControls()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Bulk", EditorStyles.boldLabel);
        _setAllCount = Mathf.Max(0, EditorGUILayout.IntField("Set All To", _setAllCount));

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Apply To All", GUILayout.Height(28f)))
            SetAll(_setAllCount);
        if (GUILayout.Button("Clear All", GUILayout.Height(28f)))
            SetAll(0);
        if (GUILayout.Button("Test 99", GUILayout.Height(28f)))
            SetAll(99);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void ChangeCount(BoosterType boosterType, ref int count, int delta)
    {
        count = Mathf.Max(0, count + delta);
        SetCount(boosterType, count);
    }

    private void ChangeCoins(int delta)
    {
        _coinCount = Mathf.Max(0, _coinCount + delta);
        SetCoins(_coinCount);
    }

    private void SetAll(int count)
    {
        _undoCount = Mathf.Max(0, count);
        _removeConditionsCount = Mathf.Max(0, count);
        _hintCount = Mathf.Max(0, count);

        SetCount(BoosterType.Undo, _undoCount);
        SetCount(BoosterType.RemoveConditions, _removeConditionsCount);
        SetCount(BoosterType.Hint, _hintCount);
    }

    private void SetCoins(int count)
    {
        int normalizedCount = Mathf.Max(0, count);

        if (Application.isPlaying &&
            EconomyManager.TryGetInstance(out EconomyManager economyManager))
        {
            economyManager.SetCoins(normalizedCount);
            RefreshCounts();
            return;
        }

        GameData saveData = LoadSavedData();
        saveData.coinCount = normalizedCount;
        SaveEditedData(saveData);
        RefreshCounts();
    }

    private void SetCount(BoosterType boosterType, int count)
    {
        int normalizedCount = Mathf.Max(0, count);

        if (Application.isPlaying &&
            BoosterInventoryManager.TryGetInstance(out BoosterInventoryManager inventoryManager))
        {
            inventoryManager.SetCount(boosterType, normalizedCount);
            RefreshCounts();
            return;
        }

        GameData saveData = LoadSavedData();
        SetSavedBoosterCount(saveData, boosterType, normalizedCount);
        SaveEditedData(saveData);
        RefreshCounts();
    }

    private void RefreshCounts()
    {
        GameData saveData = LoadSavedData();
        _coinCount = Mathf.Max(0, saveData.coinCount);
        _undoCount = Mathf.Max(0, saveData.undoBoosterCount);
        _removeConditionsCount = Mathf.Max(0, saveData.removeConditionsBoosterCount);
        _hintCount = Mathf.Max(0, saveData.hintBoosterCount);

        if (Application.isPlaying &&
            BoosterInventoryManager.TryGetInstance(out BoosterInventoryManager inventoryManager))
        {
            _undoCount = inventoryManager.GetCount(BoosterType.Undo);
            _removeConditionsCount = inventoryManager.GetCount(BoosterType.RemoveConditions);
            _hintCount = inventoryManager.GetCount(BoosterType.Hint);
        }

        if (Application.isPlaying &&
            EconomyManager.TryGetInstance(out EconomyManager economyManager))
        {
            _coinCount = economyManager.GetCoins();
        }

        Repaint();
    }

    private void HandlePlayModeStateChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredEditMode ||
            change == PlayModeStateChange.EnteredPlayMode)
            RefreshCounts();
    }

    private static GameData LoadSavedData()
    {
        JsonDataService dataService = new JsonDataService();
        if (!dataService.HasData(Constants.SaveLoad.FileName))
            return CreateDefaultData();

        try
        {
            GameData saveData = dataService.LoadData<GameData>(Constants.SaveLoad.FileName);
            return saveData ?? CreateDefaultData();
        }
        catch (Exception exception)
        {
            Debug.LogError("[BoosterTestToolWindow] Save file failed to load. Reason: " + exception.Message);
            return CreateDefaultData();
        }
    }

    private static void SaveEditedData(GameData saveData)
    {
        if (saveData == null)
            saveData = CreateDefaultData();

        saveData.savedAtUtcTicks = DateTime.UtcNow.Ticks;
        new JsonDataService().SaveData(Constants.SaveLoad.FileName, saveData);
    }

    private static GameData CreateDefaultData()
    {
        return new GameData
        {
            undoBoosterCount = DefaultBoosterCount,
            removeConditionsBoosterCount = DefaultBoosterCount,
            hintBoosterCount = DefaultBoosterCount
        };
    }

    private static void SetSavedBoosterCount(GameData saveData, BoosterType boosterType, int count)
    {
        switch (boosterType)
        {
            case BoosterType.Undo:
                saveData.undoBoosterCount = count;
                break;
            case BoosterType.RemoveConditions:
                saveData.removeConditionsBoosterCount = count;
                break;
            case BoosterType.Hint:
                saveData.hintBoosterCount = count;
                break;
        }
    }
}
