using UnityEditor;
using UnityEngine;

public class BoosterTestToolWindow : EditorWindow
{
    private const string UndoCountKey = "SEEE.Boosters.Undo";
    private const string RemoveConditionsCountKey = "SEEE.Boosters.RemoveConditions";
    private const string HintCountKey = "SEEE.Boosters.Hint";

    private int _undoCount;
    private int _removeConditionsCount;
    private int _hintCount;
    private int _setAllCount = 3;

    [MenuItem("Tools/Booster Test Tool")]
    public static void ShowWindow()
    {
        BoosterTestToolWindow window = GetWindow<BoosterTestToolWindow>("Booster Test Tool");
        window.minSize = new Vector2(360f, 250f);
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
                ? "Play Mode: changes update runtime inventory and PlayerPrefs."
                : "Edit Mode: changes update PlayerPrefs for the next Play session.",
            MessageType.Info);
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

    private void SetAll(int count)
    {
        _undoCount = Mathf.Max(0, count);
        _removeConditionsCount = Mathf.Max(0, count);
        _hintCount = Mathf.Max(0, count);

        SetCount(BoosterType.Undo, _undoCount);
        SetCount(BoosterType.RemoveConditions, _removeConditionsCount);
        SetCount(BoosterType.Hint, _hintCount);
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

        PlayerPrefs.SetInt(GetStorageKey(boosterType), normalizedCount);
        PlayerPrefs.Save();
        RefreshCounts();
    }

    private void RefreshCounts()
    {
        if (Application.isPlaying &&
            BoosterInventoryManager.TryGetInstance(out BoosterInventoryManager inventoryManager))
        {
            _undoCount = inventoryManager.GetCount(BoosterType.Undo);
            _removeConditionsCount = inventoryManager.GetCount(BoosterType.RemoveConditions);
            _hintCount = inventoryManager.GetCount(BoosterType.Hint);
            Repaint();
            return;
        }

        _undoCount = Mathf.Max(0, PlayerPrefs.GetInt(UndoCountKey, 3));
        _removeConditionsCount = Mathf.Max(0, PlayerPrefs.GetInt(RemoveConditionsCountKey, 3));
        _hintCount = Mathf.Max(0, PlayerPrefs.GetInt(HintCountKey, 3));
        Repaint();
    }

    private void HandlePlayModeStateChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredEditMode ||
            change == PlayModeStateChange.EnteredPlayMode)
            RefreshCounts();
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
}
