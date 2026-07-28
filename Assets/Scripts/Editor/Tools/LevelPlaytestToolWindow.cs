using UnityEditor;
using UnityEngine;

public class LevelPlaytestToolWindow : EditorWindow
{
    private int _targetLevelIndex;
    private int _targetRemainingMoves;

    [MenuItem("Tools/Level Playtest Tool")]
    public static void ShowWindow()
    {
        LevelPlaytestToolWindow window = GetWindow<LevelPlaytestToolWindow>("Level Playtest Tool");
        window.minSize = new Vector2(360f, 260f);
        window.RefreshTargets();
        window.Show();
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        RefreshTargets();
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
    }

    private void OnGUI()
    {
        DrawHeader();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to control loaded levels.", MessageType.Info);
            return;
        }

        if (!LevelManager.TryGetInstance(out LevelManager levelManager))
        {
            EditorGUILayout.HelpBox("LevelManager is not available in Play Mode.", MessageType.Warning);
            return;
        }

        DrawLevelInfo(levelManager);
        EditorGUILayout.Space(8f);
        DrawLevelControls(levelManager);
        EditorGUILayout.Space(8f);
        DrawMoveControls(levelManager);
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Level Playtest", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Runtime-only controls for quickly replaying, switching, and tuning levels while testing.", MessageType.None);
    }

    private void DrawLevelInfo(LevelManager levelManager)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Current", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Level", string.IsNullOrWhiteSpace(levelManager.CurrentLevelName) ? "(Unnamed)" : levelManager.CurrentLevelName);
        EditorGUILayout.LabelField("Index", levelManager.HasLevelSequence
            ? (levelManager.CurrentLevelIndex + 1) + " / " + levelManager.LevelCount
            : "Assigned Level");
        EditorGUILayout.LabelField("Moves", levelManager.RemainingMoves + " / " + levelManager.MaxMoves);
        EditorGUILayout.EndVertical();
    }

    private void DrawLevelControls(LevelManager levelManager)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Level Controls", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(!levelManager.HasLevelSequence || levelManager.CurrentLevelIndex <= 0);
        if (GUILayout.Button("Previous", GUILayout.Height(28f)))
            LoadAndRefresh(levelManager.LoadPreviousLevel);
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("Reset", GUILayout.Height(28f)))
            LoadAndRefresh(levelManager.ReloadLevel);

        EditorGUI.BeginDisabledGroup(!levelManager.HasLevelSequence || levelManager.CurrentLevelIndex >= levelManager.LevelCount - 1);
        if (GUILayout.Button("Next", GUILayout.Height(28f)))
            LoadAndRefresh(levelManager.LoadNextLevel);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginDisabledGroup(!levelManager.HasLevelSequence);
        _targetLevelIndex = EditorGUILayout.IntSlider(
            "Jump To",
            Mathf.Clamp(_targetLevelIndex, 1, Mathf.Max(1, levelManager.LevelCount)),
            1,
            Mathf.Max(1, levelManager.LevelCount));

        if (GUILayout.Button("Load Selected Level", GUILayout.Height(28f)))
            LoadAndRefresh(() => levelManager.LoadLevelAt(_targetLevelIndex - 1));
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndVertical();
    }

    private void DrawMoveControls(LevelManager levelManager)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Move Controls", EditorStyles.boldLabel);

        _targetRemainingMoves = EditorGUILayout.IntSlider(
            "Remaining",
            Mathf.Clamp(_targetRemainingMoves, 0, Mathf.Max(0, levelManager.MaxMoves)),
            0,
            Mathf.Max(0, levelManager.MaxMoves));

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Apply", GUILayout.Height(28f)))
            SetMoves(levelManager, _targetRemainingMoves);
        if (GUILayout.Button("Refill", GUILayout.Height(28f)))
            SetMoves(levelManager, levelManager.MaxMoves);
        if (GUILayout.Button("Set 0", GUILayout.Height(28f)))
            SetMoves(levelManager, 0);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void LoadAndRefresh(System.Func<bool> loadAction)
    {
        if (loadAction != null)
            loadAction.Invoke();

        RefreshTargets();
    }

    private void SetMoves(LevelManager levelManager, int remainingMoves)
    {
        levelManager.SetRemainingMovesForPlaytest(remainingMoves);
        RefreshTargets();
    }

    private void RefreshTargets()
    {
        if (Application.isPlaying && LevelManager.TryGetInstance(out LevelManager levelManager))
        {
            _targetLevelIndex = levelManager.HasLevelSequence ? levelManager.CurrentLevelIndex + 1 : 1;
            _targetRemainingMoves = levelManager.RemainingMoves;
        }
        else
        {
            _targetLevelIndex = 1;
            _targetRemainingMoves = 0;
        }

        Repaint();
    }

    private void HandlePlayModeStateChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredEditMode ||
            change == PlayModeStateChange.EnteredPlayMode)
            RefreshTargets();
    }
}
