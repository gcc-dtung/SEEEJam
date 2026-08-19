using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class LevelEditorWindow : EditorWindow
{
    private enum SelectionKind { None, Land, Tree, Decor }

    private const float SidebarMinWidth = 180f;
    private const float SidebarMaxWidth = 620f;
    private const float InspectorMinWidth = 220f;
    private const float InspectorMaxWidth = 760f;
    private const float SplitterWidth = 6f;
    private const float NodeSize = 42f;
    private const float CanvasPadding = 18f;
    private const float CanvasMinZoom = 0.25f;
    private const float CanvasMaxZoom = 2.5f;
    private const string LevelFolderRelativePath = "Assets/Level";
    private const string DecorClipboardPrefix = "IsThisLandTaken.Decor:";

    private static readonly Dictionary<SlotType, Color> SlotColors = new Dictionary<SlotType, Color>
    {
        { SlotType.Dirt, new Color(0.60f, 0.40f, 0.20f) },
        { SlotType.Sand, new Color(0.93f, 0.84f, 0.55f) },
        { SlotType.Water, new Color(0.25f, 0.55f, 0.90f) },
        { SlotType.DryDirt, new Color(0.72f, 0.58f, 0.38f) },
        { SlotType.Wait, new Color(0.45f, 0.45f, 0.45f) },
    };

    private LevelData _level = new LevelData();
    private string _currentJsonPath = "";
    private Vector2 _sidebarScroll;
    private Vector2 _landScroll;
    private Vector2 _treeScroll;
    private Vector2 _inspectorScroll;
    private SelectionKind _selectionKind = SelectionKind.None;
    private int _selectedLandIndex = -1;
    private readonly HashSet<int> _selectedLandIndices = new HashSet<int>();
    private int _selectedTreeIndex = -1;
    private int _dragLandIndex = -1;
    private int _dragTreeIndex = -1;
    private Vector2 _dragOffset;
    private float _dragNodeSize = NodeSize;
    private int _selectedConditionIndex = -1;
    private float _sidebarWidth = 330f;
    private float _inspectorWidth = 420f;
    private int _activeSplitter;
    private Vector2 _canvasPan;
    private float _canvasZoom = 1f;
    private bool _isPanningCanvas;
    private bool _isSpacePressed;
    private bool _snapEnabled;
    private float _autoNeighborDistance = 200f;
    private bool _autoNeighborUseSelectedOnly;
    private readonly Dictionary<int, Vector2Int> _dragLandStartPositions = new Dictionary<int, Vector2Int>();
    private Vector2 _dragLandStartMouseModel;
    private int     _selectedDecorIndex = -1;
    private Vector2 _decorScroll;
    private int     _dragDecorIndex  = -1;
    private Vector2 _dragDecorOffset;

    [MenuItem("Tools/Level Editor")]
    public static void ShowWindow()
    {
        LevelEditorWindow win = GetWindow<LevelEditorWindow>("Level Editor");
        win.minSize = new Vector2(1120, 560);
        win.Show();
    }

    private void OnEnable()
    {
        NormalizeLevelData();
    }

    private void OnGUI()
    {
        NormalizeLevelData();

        EditorGUILayout.BeginHorizontal();
        DrawSidebar();
        DrawSplitter(1);
        DrawCanvas();
        DrawSplitter(2);
        DrawInspector();
        EditorGUILayout.EndHorizontal();
    }

    private float GetSidebarWidth()
    {
        _sidebarWidth = Mathf.Clamp(_sidebarWidth, SidebarMinWidth, SidebarMaxWidth);
        return _sidebarWidth;
    }

    private float GetInspectorWidth()
    {
        _inspectorWidth = Mathf.Clamp(_inspectorWidth, InspectorMinWidth, InspectorMaxWidth);
        return _inspectorWidth;
    }

    private void DrawSplitter(int splitterId)
    {
        Rect rect = GUILayoutUtility.GetRect(SplitterWidth, SplitterWidth, GUILayout.ExpandHeight(true));
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
        EditorGUI.DrawRect(rect, new Color(0.08f, 0.08f, 0.08f));

        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
        {
            _activeSplitter = splitterId;
            e.Use();
        }

        if (_activeSplitter == splitterId && e.type == EventType.MouseDrag && e.button == 0)
        {
            if (splitterId == 1)
                _sidebarWidth = Mathf.Clamp(_sidebarWidth + e.delta.x, SidebarMinWidth, SidebarMaxWidth);
            else
                _inspectorWidth = Mathf.Clamp(_inspectorWidth - e.delta.x, InspectorMinWidth, InspectorMaxWidth);

            Repaint();
            e.Use();
        }

        if (_activeSplitter == splitterId && e.type == EventType.MouseUp)
        {
            _activeSplitter = 0;
            e.Use();
        }
    }

    private void DrawSidebar()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(GetSidebarWidth()), GUILayout.ExpandHeight(true));
        _sidebarScroll = EditorGUILayout.BeginScrollView(_sidebarScroll, GUILayout.ExpandHeight(true));
        GUILayout.Label("Level JSON", EditorStyles.boldLabel);
        _level.levelName = EditorGUILayout.TextField("Name", _level.levelName);
        _level.maxMoves = EditorGUILayout.IntField("Moves", _level.maxMoves);
        _level.width = EditorGUILayout.IntField("Width", _level.width);
        _level.height = EditorGUILayout.IntField("Height", _level.height);

        EditorGUILayout.Space(6);
        GUILayout.Label("Screen Preview", EditorStyles.boldLabel);
        DrawPositionFields(ref _level.screenX, ref _level.screenY);
        EditorGUILayout.BeginHorizontal();
        _level.screenWidth = EditorGUILayout.IntField("Screen W", _level.screenWidth);
        _level.screenHeight = EditorGUILayout.IntField("H", _level.screenHeight);
        EditorGUILayout.EndHorizontal();
        _level.slotSize = EditorGUILayout.IntSlider("Slot Size", _level.slotSize, 12, 160);
        _level.itemSize = EditorGUILayout.IntSlider("Item Size", _level.itemSize, 8, 300);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("File", string.IsNullOrEmpty(_currentJsonPath) ? "Not saved yet" : Path.GetFileName(_currentJsonPath));

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("New", GUILayout.Height(28)))
            NewLevel();
        if (GUILayout.Button("Load", GUILayout.Height(28)))
            LoadFromJson();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save", GUILayout.Height(30)))
            SaveToJson(false);
        if (GUILayout.Button("Export", GUILayout.Height(30)))
            SaveToJson(true);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        GUILayout.Label("Create", EditorStyles.boldLabel);
        if (GUILayout.Button("Add Land", GUILayout.Height(32)))
            AddLand();
        if (GUILayout.Button("Add Tree", GUILayout.Height(32)))
            AddTree();

        EditorGUILayout.Space(6);
        _snapEnabled = EditorGUILayout.ToggleLeft("Snap to 20", _snapEnabled);

        EditorGUILayout.Space(8);
        GUILayout.Label("Auto Neighbor", EditorStyles.boldLabel);
        _autoNeighborDistance = Mathf.Max(1f, EditorGUILayout.FloatField("Max Distance", _autoNeighborDistance));
        _autoNeighborUseSelectedOnly = EditorGUILayout.ToggleLeft("Selected Lands Only", _autoNeighborUseSelectedOnly);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Auto Connect (Replace)", GUILayout.Height(24)))
            AutoConnectNeighborsByDistance(_autoNeighborDistance, true, _autoNeighborUseSelectedOnly);
        if (GUILayout.Button("Auto Connect (Add)", GUILayout.Height(24)))
            AutoConnectNeighborsByDistance(_autoNeighborDistance, false, _autoNeighborUseSelectedOnly);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        GUILayout.Label("Auto Solve", EditorStyles.boldLabel);
        if (GUILayout.Button("Auto Solve", GUILayout.Height(28)))
            AutoSolveTrees();

        EditorGUILayout.Space(10);
        GUILayout.Label("Lands", EditorStyles.boldLabel);
        _landScroll = EditorGUILayout.BeginScrollView(_landScroll, GUILayout.Height(110));
        for (int i = 0; i < _level.cells.Count; i++)
            DrawListButton(i, SelectionKind.Land);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);
        GUILayout.Label("Trees", EditorStyles.boldLabel);
        _treeScroll = EditorGUILayout.BeginScrollView(_treeScroll, GUILayout.Height(110));
        for (int i = 0; i < _level.trees.Count; i++)
            DrawListButton(i, SelectionKind.Tree);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);
        GUILayout.Label("Decorations", EditorStyles.boldLabel);
        if (GUILayout.Button("Add Decor", GUILayout.Height(24)))
            AddDecor();
        _decorScroll = EditorGUILayout.BeginScrollView(_decorScroll, GUILayout.Height(90));
        if (_level.decorations != null)
        {
            for (int i = 0; i < _level.decorations.Count; i++)
            {
                bool decorSelected = _selectionKind == SelectionKind.Decor && _selectedDecorIndex == i;
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = decorSelected ? new Color(0.5f, 0.7f, 1f) : prev;
                if (GUILayout.Button(_level.decorations[i].decorId, GUILayout.Height(24)))
                    Select(SelectionKind.Decor, i);
                GUI.backgroundColor = prev;
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawListButton(int index, SelectionKind kind)
    {
        bool selected = kind == SelectionKind.Land
            ? IsLandSelected(index)
            : _selectionKind == kind && GetSelectedIndex() == index;
        string label = kind == SelectionKind.Land
            ? _level.cells[index].landId + "  (" + _level.cells[index].slotType + ")"
            : _level.trees[index].treeId;

        Color previous = GUI.backgroundColor;
        GUI.backgroundColor = selected ? new Color(0.5f, 0.7f, 1f) : previous;
        if (GUILayout.Button(label, GUILayout.Height(24)))
        {
            bool multiSelect = Event.current != null && (Event.current.control || Event.current.command);
            if (kind == SelectionKind.Land && multiSelect)
                ToggleLandSelection(index);
            else
                Select(kind, index);
        }
        GUI.backgroundColor = previous;
    }

    private void DrawCanvas()
    {
        Rect canvasRect = GUILayoutUtility.GetRect(10f, 100000f, 10f, 100000f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        GUI.Box(canvasRect, GUIContent.none);
        EditorGUI.DrawRect(canvasRect, new Color(0.18f, 0.15f, 0.13f));

        Handles.BeginGUI();
        DrawScreenPreview(canvasRect);
        DrawLandConnections(canvasRect);
        Handles.EndGUI();

        DrawCanvasGrid(canvasRect);
        DrawRegionBounds(canvasRect);
        DrawDecorNodes(canvasRect);
        DrawLandNodes(canvasRect);
        DrawTreeNodes(canvasRect);
        HandleCanvasEvents(canvasRect);
    }

    private void DrawCanvasGrid(Rect rect)
    {
        Handles.BeginGUI();
        Handles.color = new Color(1f, 1f, 1f, 0.06f);
        float step = 48f * _canvasZoom;
        if (step < 8f)
            step = 8f;
        Vector2 origin = ToCanvas(rect, 0, 0);
        float startX = origin.x % step;
        if (startX < rect.x)
            startX += Mathf.Ceil((rect.x - startX) / step) * step;
        float startY = origin.y % step;
        if (startY < rect.y)
            startY += Mathf.Ceil((rect.y - startY) / step) * step;

        for (float x = startX; x < rect.xMax; x += step)
            Handles.DrawLine(new Vector3(x, rect.y), new Vector3(x, rect.yMax));
        for (float y = startY; y < rect.yMax; y += step)
            Handles.DrawLine(new Vector3(rect.x, y), new Vector3(rect.xMax, y));
        Handles.EndGUI();
    }

    private void DrawScreenPreview(Rect rect)
    {
        Vector2 screenTopLeft = ToCanvas(rect, _level.screenX, _level.screenY);
        Rect screenRect = new Rect(
            screenTopLeft.x,
            screenTopLeft.y,
            _level.screenWidth * _canvasZoom,
            _level.screenHeight * _canvasZoom);

        EditorGUI.DrawRect(screenRect, new Color(0.16f, 0.28f, 0.46f, 0.25f));
        Handles.color = new Color(0.95f, 0.95f, 0.95f, 0.9f);
        Handles.DrawAAPolyLine(
            2f,
            new Vector3(screenRect.xMin, screenRect.yMin),
            new Vector3(screenRect.xMax, screenRect.yMin),
            new Vector3(screenRect.xMax, screenRect.yMax),
            new Vector3(screenRect.xMin, screenRect.yMax),
            new Vector3(screenRect.xMin, screenRect.yMin));

        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = Color.white }
        };
        GUI.Label(new Rect(screenRect.x + 6f, screenRect.y + 4f, 160f, 18f), "Screen", labelStyle);
    }

    private void DrawLandConnections(Rect rect)
    {
        Handles.color = new Color(0.88f, 0.88f, 0.88f, 0.9f);
        foreach (LevelCellData land in _level.cells)
        {
            foreach (string neighborId in land.neighborIds)
            {
                LevelCellData neighbor = FindLand(neighborId);
                if (neighbor == null || string.CompareOrdinal(land.landId, neighbor.landId) > 0)
                    continue;
                Handles.DrawAAPolyLine(4f, ToCanvas(rect, land), ToCanvas(rect, neighbor));
            }
        }
    }

    private void DrawLandNodes(Rect rect)
    {
        for (int i = 0; i < _level.cells.Count; i++)
        {
            LevelCellData land = _level.cells[i];
            Rect nodeRect = NodeRect(rect, land.x, land.y, _level.slotSize);
            Color color = SlotColors.TryGetValue(land.slotType, out Color slotColor) ? slotColor : Color.gray;
            DrawNode(nodeRect, color, IsLandSelected(i), land.landId);
        }
    }

    private void DrawRegionBounds(Rect rect)
    {
        Handles.BeginGUI();
        Handles.color = new Color(0.2f, 1f, 0.2f, 0.85f);

        // Land region: (100,620) to (1000,1340)
        Vector2 landTL = ToCanvas(rect, 100, 620);
        Vector2 landBR = ToCanvas(rect, 1000, 1340);
        Handles.DrawAAPolyLine(2f,
            new Vector3(landTL.x, landTL.y),
            new Vector3(landBR.x, landTL.y),
            new Vector3(landBR.x, landBR.y),
            new Vector3(landTL.x, landBR.y),
            new Vector3(landTL.x, landTL.y));

        // Tree region: (100,1614) to (1000,1820)
        Vector2 treeTL = ToCanvas(rect, 100, 1614);
        Vector2 treeBR = ToCanvas(rect, 1000, 1820);
        Handles.DrawAAPolyLine(2f,
            new Vector3(treeTL.x, treeTL.y),
            new Vector3(treeBR.x, treeTL.y),
            new Vector3(treeBR.x, treeBR.y),
            new Vector3(treeTL.x, treeBR.y),
            new Vector3(treeTL.x, treeTL.y));

        Handles.EndGUI();
    }

    private void DrawTreeNodes(Rect rect)
    {
        for (int i = 0; i < _level.trees.Count; i++)
        {
            TreeData tree = _level.trees[i];
            Rect slotRect = NodeRect(rect, tree.x, tree.y, _level.slotSize);
            Rect itemRect = NodeRect(rect, tree.x, tree.y, _level.itemSize);
            bool selected = _selectionKind == SelectionKind.Tree && _selectedTreeIndex == i;
            DrawNode(slotRect, SlotColors[SlotType.Wait], selected, "");
            DrawNode(itemRect, new Color(0.30f, 0.72f, 0.34f), selected, tree.treeId);
        }
    }

    private void DrawDecorNodes(Rect canvasRect)
    {
        if (_level.decorations == null || _level.decorations.Count == 0)
            return;

        for (int i = 0; i < _level.decorations.Count; i++)
        {
            DecorData decor = _level.decorations[i];
            Vector2 topLeft = ToCanvas(canvasRect, decor.x, decor.y);
            Rect nodeRect   = new Rect(topLeft.x, topLeft.y,
                                        decor.width  * _canvasZoom,
                                        decor.height * _canvasZoom);

            bool selected = _selectionKind == SelectionKind.Decor && _selectedDecorIndex == i;

            Color fill = selected
                ? new Color(1f, 0.85f, 0.2f, 0.22f)
                : new Color(0.45f, 0.75f, 1f, 0.13f);
            EditorGUI.DrawRect(nodeRect, fill);

            Handles.BeginGUI();
            Handles.color = selected ? new Color(1f, 0.85f, 0.2f, 1f) : new Color(0.45f, 0.75f, 1f, 0.85f);
            float lw = selected ? 2.5f : 1.5f;
            Handles.DrawAAPolyLine(lw,
                new Vector3(nodeRect.xMin, nodeRect.yMin),
                new Vector3(nodeRect.xMax, nodeRect.yMin),
                new Vector3(nodeRect.xMax, nodeRect.yMax),
                new Vector3(nodeRect.xMin, nodeRect.yMax),
                new Vector3(nodeRect.xMin, nodeRect.yMin));
            Handles.EndGUI();

            GUIStyle ls = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = selected ? Color.yellow : new Color(0.7f, 0.9f, 1f) },
                padding = new RectOffset(4, 0, 2, 0)
            };
            string label = decor.decorId + (string.IsNullOrWhiteSpace(decor.prefabPath) ? " (no prefab)" : "");
            GUI.Label(nodeRect, label, ls);
            EditorGUIUtility.AddCursorRect(nodeRect, MouseCursor.MoveArrow);
        }
    }

    private void DrawNode(Rect rect, Color color, bool selected, string label)
    {
        Color fillColor = selected ? Color.yellow : color;
        EditorGUI.DrawRect(rect, fillColor);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), Color.white);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), Color.black);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2f, rect.height), Color.white);
        EditorGUI.DrawRect(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), Color.black);
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.MoveArrow);

        GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 9,
            fontStyle = selected ? FontStyle.Bold : FontStyle.Normal,
            wordWrap = true,
            normal = { textColor = Color.black }
        };
        GUI.Label(rect, label, style);
    }

    private void HandleCanvasEvents(Rect rect)
    {
        Event e = Event.current;
        bool isDraggingNode = _dragLandIndex >= 0 || _dragTreeIndex >= 0 || _dragDecorIndex >= 0;

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Space)
        {
            _isSpacePressed = true;
            e.Use();
        }

        if (e.type == EventType.KeyUp && e.keyCode == KeyCode.Space)
        {
            _isSpacePressed = false;
            e.Use();
        }

        if (e.type == EventType.MouseUp)
        {
            _dragLandIndex   = -1;
            _dragTreeIndex   = -1;
            _dragDecorIndex  = -1;
            _dragLandStartPositions.Clear();
            _isPanningCanvas = false;
            return;
        }

        if (!rect.Contains(e.mousePosition) && !isDraggingNode && !_isPanningCanvas)
            return;

        if (rect.Contains(e.mousePosition))
        {
            if (e.type == EventType.ScrollWheel)
            {
                ZoomCanvas(rect, e.mousePosition, -e.delta.y * 0.04f);
                e.Use();
                return;
            }

            bool wantsPan = e.button == 2 || (e.button == 0 && _isSpacePressed);
            if (e.type == EventType.MouseDown && wantsPan)
            {
                _isPanningCanvas = true;
                e.Use();
                return;
            }
        }

        if (_isPanningCanvas && e.type == EventType.MouseDrag)
        {
            _canvasPan += e.delta;
            Repaint();
            e.Use();
            return;
        }

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            for (int i = _level.trees.Count - 1; i >= 0; i--)
            {
                Rect nodeRect = NodeRect(rect, _level.trees[i].x, _level.trees[i].y, Mathf.Max(_level.slotSize, _level.itemSize));
                if (!nodeRect.Contains(e.mousePosition))
                    continue;
                Select(SelectionKind.Tree, i);
                _dragTreeIndex = i;
                _dragLandIndex = -1;
                _dragOffset = e.mousePosition - new Vector2(nodeRect.x, nodeRect.y);
                _dragNodeSize = nodeRect.width;
                e.Use();
                return;
            }

            for (int i = _level.cells.Count - 1; i >= 0; i--)
            {
                Rect nodeRect = NodeRect(rect, _level.cells[i].x, _level.cells[i].y, _level.slotSize);
                if (!nodeRect.Contains(e.mousePosition))
                    continue;

                bool multiSelect = e.control || e.command;
                if (multiSelect)
                {
                    ToggleLandSelection(i);
                    _dragLandIndex = -1;
                    _dragTreeIndex = -1;
                    _dragLandStartPositions.Clear();
                    e.Use();
                    return;
                }

                if (!IsLandSelected(i))
                    Select(SelectionKind.Land, i);

                _dragLandIndex = i;
                _dragTreeIndex = -1;
                _dragOffset = e.mousePosition - new Vector2(nodeRect.x, nodeRect.y);
                _dragNodeSize = nodeRect.width;

                _dragLandStartPositions.Clear();
                _dragLandStartMouseModel = FromCanvas(rect, ClampToRect(e.mousePosition, rect));
                foreach (int landIndex in _selectedLandIndices)
                {
                    if (landIndex < 0 || landIndex >= _level.cells.Count)
                        continue;

                    LevelCellData selectedLand = _level.cells[landIndex];
                    _dragLandStartPositions[landIndex] = new Vector2Int(selectedLand.x, selectedLand.y);
                }

                e.Use();
                return;
            }

            // Decor hit test — lowest priority (background objects)
            if (_level.decorations != null)
            {
                for (int i = _level.decorations.Count - 1; i >= 0; i--)
                {
                    DecorData decor = _level.decorations[i];
                    Vector2 decorTL  = ToCanvas(rect, decor.x, decor.y);
                    Rect decorRect   = new Rect(decorTL.x, decorTL.y,
                                                decor.width  * _canvasZoom,
                                                decor.height * _canvasZoom);
                    if (!decorRect.Contains(e.mousePosition))
                        continue;

                    Select(SelectionKind.Decor, i);
                    _dragDecorIndex = i;
                    _dragLandIndex  = -1;
                    _dragTreeIndex  = -1;
                    _dragDecorOffset = e.mousePosition - decorTL;
                    e.Use();
                    return;
                }
            }

            Select(SelectionKind.None, -1);
            e.Use();
        }

        if (e.type == EventType.MouseDrag && e.button == 0)
        {
            Vector2 clampedMousePosition = ClampToRect(e.mousePosition, rect);
            if (_dragLandIndex >= 0 && _dragLandIndex < _level.cells.Count)
            {
                Vector2 currentMouseModel = FromCanvas(rect, clampedMousePosition);
                Vector2 modelDelta = currentMouseModel - _dragLandStartMouseModel;

                foreach (KeyValuePair<int, Vector2Int> pair in _dragLandStartPositions)
                {
                    int landIndex = pair.Key;
                    if (landIndex < 0 || landIndex >= _level.cells.Count)
                        continue;

                    float targetX = Mathf.Clamp(pair.Value.x + modelDelta.x, 100f, 1000f);
                    float targetY = Mathf.Clamp(pair.Value.y + modelDelta.y, 620f, 1340f);
                    _level.cells[landIndex].x = _snapEnabled ? SnapValue(targetX) : Mathf.RoundToInt(targetX);
                    _level.cells[landIndex].y = _snapEnabled ? SnapValue(targetY) : Mathf.RoundToInt(targetY);
                }

                Repaint();
                e.Use();
            }
            else if (_dragTreeIndex >= 0 && _dragTreeIndex < _level.trees.Count)
            {
                Vector2 pos = FromCanvas(rect, clampedMousePosition - _dragOffset + Vector2.one * (_dragNodeSize * 0.5f));
                float clampedX = Mathf.Clamp(pos.x, 100f, 1000f);
                float clampedY = Mathf.Clamp(pos.y, 1614f, 1820f);
                _level.trees[_dragTreeIndex].x = _snapEnabled ? SnapValue(clampedX) : Mathf.RoundToInt(clampedX);
                _level.trees[_dragTreeIndex].y = _snapEnabled ? SnapValue(clampedY) : Mathf.RoundToInt(clampedY);
                Repaint();
                e.Use();
            }
            else if (_dragDecorIndex >= 0 && _level.decorations != null && _dragDecorIndex < _level.decorations.Count)
            {
                DecorData draggingDecor = _level.decorations[_dragDecorIndex];
                Vector2 newTopLeft = FromCanvas(rect, clampedMousePosition - _dragDecorOffset);
                draggingDecor.x = _snapEnabled ? SnapValue(newTopLeft.x) : Mathf.RoundToInt(newTopLeft.x);
                draggingDecor.y = _snapEnabled ? SnapValue(newTopLeft.y) : Mathf.RoundToInt(newTopLeft.y);
                Repaint();
                e.Use();
            }
        }
    }

    private void ZoomCanvas(Rect rect, Vector2 mousePosition, float zoomDelta)
    {
        Vector2 modelPointBeforeZoom = FromCanvas(rect, mousePosition);
        float oldZoom = _canvasZoom;
        _canvasZoom = Mathf.Clamp(_canvasZoom * (1f + zoomDelta), CanvasMinZoom, CanvasMaxZoom);
        if (Mathf.Approximately(oldZoom, _canvasZoom))
            return;

        Vector2 modelPointAfterZoom = FromCanvas(rect, mousePosition);
        _canvasPan += (modelPointAfterZoom - modelPointBeforeZoom) * _canvasZoom;
        Repaint();
    }

    private static Vector2 ClampToRect(Vector2 point, Rect rect)
    {
        return new Vector2(
            Mathf.Clamp(point.x, rect.xMin, rect.xMax),
            Mathf.Clamp(point.y, rect.yMin, rect.yMax));
    }

    private void DrawInspector()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(GetInspectorWidth()), GUILayout.ExpandHeight(true));
        GUILayout.Label("Inspector", EditorStyles.boldLabel);
        _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);

        if (_selectionKind == SelectionKind.Land && IsValidLandSelection())
        {
            if (_selectedLandIndices.Count > 1)
                DrawMultiLandInspector();
            else
                DrawLandInspector(_level.cells[_selectedLandIndex]);
        }
        else if (_selectionKind == SelectionKind.Tree && IsValidTreeSelection())
            DrawTreeInspector(_level.trees[_selectedTreeIndex]);
        else if (_selectionKind == SelectionKind.Decor && IsValidDecorSelection())
            DrawDecorInspector(_level.decorations[_selectedDecorIndex]);
        else
            EditorGUILayout.HelpBox("Select a land or tree node to edit position, type, attributes and neighbors.", MessageType.Info);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawLandInspector(LevelCellData land)
    {
        GUILayout.Label("Land", EditorStyles.boldLabel);
        string oldId = land.landId;
        land.landId = EditorGUILayout.TextField("ID", land.landId);
        if (land.landId != oldId)
            RenameLandId(oldId, land.landId);

        land.slotType = (SlotType)EditorGUILayout.EnumPopup("Type", land.slotType);
        land.row = (BoardRow)EditorGUILayout.EnumPopup("Row", land.row);
        land.isCorner = EditorGUILayout.ToggleLeft("Corner", land.isCorner);
        land.isEdge = EditorGUILayout.ToggleLeft("Edge", land.isEdge);
        land.itemId = EditorGUILayout.TextField("Item ID", land.itemId ?? "");

        DrawPositionFields(ref land.x, ref land.y);

        EditorGUILayout.Space(6);
        GUILayout.Label("Neighbors", EditorStyles.boldLabel);
        for (int i = 0; i < _level.cells.Count; i++)
        {
            LevelCellData other = _level.cells[i];
            if (other == land)
                continue;
            bool connected = land.neighborIds.Contains(other.landId);
            bool newConnected = EditorGUILayout.ToggleLeft(other.landId, connected);
            if (newConnected != connected)
                SetLandNeighbor(land, other, newConnected);
        }

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Delete Land", GUILayout.Height(28)))
            DeleteSelectedLand();
    }

    private void DrawMultiLandInspector()
    {
        List<int> selectedIndices = GetSortedSelectedLandIndices();
        if (selectedIndices.Count <= 1)
        {
            DrawLandInspector(_level.cells[_selectedLandIndex]);
            return;
        }

        GUILayout.Label("Lands (" + selectedIndices.Count + ")", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Multi-edit mode. Changes apply to all selected lands.", MessageType.Info);

        LevelCellData first = _level.cells[selectedIndices[0]];

        bool mixedType = HasMixedSlotType(selectedIndices, first.slotType);
        EditorGUI.showMixedValue = mixedType;
        EditorGUI.BeginChangeCheck();
        SlotType slotType = (SlotType)EditorGUILayout.EnumPopup("Type", first.slotType);
        if (EditorGUI.EndChangeCheck())
            ApplySlotTypeToSelectedLands(selectedIndices, slotType);
        EditorGUI.showMixedValue = false;

        bool mixedRow = HasMixedRow(selectedIndices, first.row);
        EditorGUI.showMixedValue = mixedRow;
        EditorGUI.BeginChangeCheck();
        BoardRow row = (BoardRow)EditorGUILayout.EnumPopup("Row", first.row);
        if (EditorGUI.EndChangeCheck())
            ApplyRowToSelectedLands(selectedIndices, row);
        EditorGUI.showMixedValue = false;

        bool mixedCorner = HasMixedCorner(selectedIndices, first.isCorner);
        EditorGUI.showMixedValue = mixedCorner;
        EditorGUI.BeginChangeCheck();
        bool isCorner = EditorGUILayout.ToggleLeft("Corner", first.isCorner);
        if (EditorGUI.EndChangeCheck())
            ApplyCornerToSelectedLands(selectedIndices, isCorner);
        EditorGUI.showMixedValue = false;

        bool mixedEdge = HasMixedEdge(selectedIndices, first.isEdge);
        EditorGUI.showMixedValue = mixedEdge;
        EditorGUI.BeginChangeCheck();
        bool isEdge = EditorGUILayout.ToggleLeft("Edge", first.isEdge);
        if (EditorGUI.EndChangeCheck())
            ApplyEdgeToSelectedLands(selectedIndices, isEdge);
        EditorGUI.showMixedValue = false;

        int x = first.x;
        int y = first.y;
        DrawPositionFields(ref x, ref y);
        if (x != first.x || y != first.y)
        {
            int deltaX = x - first.x;
            int deltaY = y - first.y;
            ApplyPositionDeltaToSelectedLands(selectedIndices, deltaX, deltaY);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Selected IDs", string.Join(", ", GetSelectedLandIds(selectedIndices)));

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Delete Selected Lands", GUILayout.Height(28)))
            DeleteSelectedLand();
    }

    private void DrawTreeInspector(TreeData tree)
    {
        GUILayout.Label("Tree", EditorStyles.boldLabel);
        string oldId = tree.treeId;
        tree.treeId = EditorGUILayout.TextField("ID", tree.treeId);
        if (tree.treeId != oldId)
            RenameTreeId(oldId, tree.treeId);

        string autoName = TreeNameRegistry.GetDisplayName(tree.treeId);
        tree.displayName = EditorGUILayout.TextField(
            new GUIContent("Display Name", $"Name shown in-game. Leave blank to use auto: \"{autoName}\""),
            tree.displayName ?? "");
        if (string.IsNullOrWhiteSpace(tree.displayName))
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("  (auto)", autoName);
            EditorGUI.EndDisabledGroup();
        }

        tree.itemType = (ItemType)EditorGUILayout.EnumPopup("Item Type", tree.itemType);
        tree.requiredSlotType = (SlotType)EditorGUILayout.EnumPopup("Required Slot", tree.requiredSlotType);
        tree.solutionLandId = DrawLandIdPopup("Solution Land", tree.solutionLandId);
        tree.parameterN = EditorGUILayout.IntField("Parameter n", tree.parameterN);

        DrawPositionFields(ref tree.x, ref tree.y);

        GUILayout.Label("Notes");
        tree.customNotes = EditorGUILayout.TextArea(tree.customNotes ?? "", GUILayout.Height(54));

        DrawTreeConditions(tree);

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Delete Tree", GUILayout.Height(28)))
            DeleteSelectedTree();
    }

    private void DrawDecorInspector(DecorData decor)
    {
        GUILayout.Label("Decoration", EditorStyles.boldLabel);
        decor.decorId = EditorGUILayout.TextField("ID", decor.decorId);

        EditorGUILayout.Space(4);
        GUILayout.Label("Prefab", EditorStyles.boldLabel);

        // Convenience object-picker — auto-extracts the Resources-relative path
        GameObject currentPrefab = string.IsNullOrWhiteSpace(decor.prefabPath)
            ? null
            : Resources.Load<GameObject>(decor.prefabPath);

        EditorGUI.BeginChangeCheck();
        GameObject picked = (GameObject)EditorGUILayout.ObjectField(
            "Prefab (Resources)", currentPrefab, typeof(GameObject), false);
        if (EditorGUI.EndChangeCheck() && picked != null)
        {
            string assetPath = AssetDatabase.GetAssetPath(picked);
            const string resFolder = "/Resources/";
            int idx = assetPath.IndexOf(resFolder, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
                decor.prefabPath = Path.ChangeExtension(
                    assetPath.Substring(idx + resFolder.Length), null);
            else
                Debug.LogWarning("[LevelEditor] Decor prefab must be inside a Resources/ folder.");
        }

        decor.prefabPath = EditorGUILayout.TextField("  Path", decor.prefabPath ?? "");
        if (!string.IsNullOrWhiteSpace(decor.prefabPath) && currentPrefab == null)
            EditorGUILayout.HelpBox("Not found: Resources/" + decor.prefabPath, MessageType.Warning);
        else
            EditorGUILayout.HelpBox("Path inside any Resources/ folder, no extension.", MessageType.None);

        EditorGUILayout.Space(4);
        DrawPositionFields(ref decor.x, ref decor.y);

        EditorGUILayout.Space(4);
        GUILayout.Label("Size", EditorStyles.boldLabel);
        decor.sizeMode = DecorSizeMode.Normal;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Size (px)");
        GUILayout.Label("W", GUILayout.Width(14));
        decor.width  = Mathf.Max(1, EditorGUILayout.IntField(decor.width,  GUILayout.Width(58)));
        GUILayout.Space(8);
        GUILayout.Label("H", GUILayout.Width(14));
        decor.height = Mathf.Max(1, EditorGUILayout.IntField(decor.height, GUILayout.Width(58)));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox("Direct pixel size, same units as Screen W/H.", MessageType.None);

        decor.sortingOrder = EditorGUILayout.IntField("Sorting Order", decor.sortingOrder);

        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Copy Decor", GUILayout.Height(28)))
            CopySelectedDecorToClipboard();

        EditorGUI.BeginDisabledGroup(!HasDecorClipboard());
        if (GUILayout.Button("Paste Decor", GUILayout.Height(28)))
            PasteDecorFromClipboard();
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Delete Decor", GUILayout.Height(28)))
            DeleteSelectedDecor();
    }

    private void AddLand()
    {
        LevelCellData land = new LevelCellData
        {
            landId = NextLandId(),
            x = 80 + _level.cells.Count * 30,
            y = 80 + _level.cells.Count * 18,
            slotType = SlotType.Dirt,
            row = BoardRow.None,
            isCorner = false,
            isEdge = false,
            itemId = "",
            neighborIds = new List<string>()
        };
        _level.cells.Add(land);
        Select(SelectionKind.Land, _level.cells.Count - 1);
        Repaint();
    }

    private static void DrawPositionFields(ref int x, ref int y)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Position");
        GUILayout.Label("X", GUILayout.Width(12));
        x = EditorGUILayout.IntField(x, GUILayout.Width(58));
        GUILayout.Space(8);
        GUILayout.Label("Y", GUILayout.Width(12));
        y = EditorGUILayout.IntField(y, GUILayout.Width(58));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private static int SnapValue(float v)
    {
        return Mathf.RoundToInt(v / 20f) * 20;
    }

    private void AddTree()
    {
        const int treeStartX = 100;
        const int treeStepX = 150;
        const int treeY = 1614;
        TreeData tree = new TreeData
        {
            treeId = NextTreeId(),
            solutionLandId = "",
            x = treeStartX + _level.trees.Count * treeStepX,
            y = treeY,
            itemType = ItemType.Plant,
            requiredSlotType = SlotType.Dirt,
            parameterN = 0,
            customNotes = "",
            conditions = new List<TreeConditionData>
            {
                new TreeConditionData
                {
                    conditionType = TreeConditionType.NearTreeCount,
                    n = 0,
                    targetTreeId = ""
                }
            }
        };
        _level.trees.Add(tree);
        Select(SelectionKind.Tree, _level.trees.Count - 1);
        Repaint();
    }

    private void AddDecor()
    {
        if (_level.decorations == null)
            _level.decorations = new List<DecorData>();
        DecorData decor = new DecorData
        {
            decorId       = NextDecorId(),
            prefabPath    = "",
            x             = _level.screenX,
            y             = _level.screenY,
            sizeMode      = DecorSizeMode.Normal,
            width         = Mathf.RoundToInt(3f * _level.slotSize),
            height        = Mathf.RoundToInt(3f * _level.slotSize),
            sortingOrder  = -1
        };
        _level.decorations.Add(decor);
        Select(SelectionKind.Decor, _level.decorations.Count - 1);
        Repaint();
    }

    private void CopySelectedDecorToClipboard()
    {
        if (!IsValidDecorSelection())
            return;

        DecorData decorCopy = CloneDecor(_level.decorations[_selectedDecorIndex]);
        EditorGUIUtility.systemCopyBuffer = DecorClipboardPrefix + JsonUtility.ToJson(decorCopy, true);
        Debug.Log("[LevelEditor] Copied decor to clipboard.");
    }

    private void PasteDecorFromClipboard()
    {
        if (!TryGetDecorFromClipboard(out DecorData decorCopy))
        {
            Debug.LogWarning("[LevelEditor] Clipboard does not contain a decor.");
            return;
        }

        if (_level.decorations == null)
            _level.decorations = new List<DecorData>();

        decorCopy.decorId = NextDecorId();
        decorCopy.sizeMode = DecorSizeMode.Normal;
        decorCopy.width = Mathf.Max(1, decorCopy.width);
        decorCopy.height = Mathf.Max(1, decorCopy.height);

        _level.decorations.Add(decorCopy);
        Select(SelectionKind.Decor, _level.decorations.Count - 1);
        Repaint();
        Debug.Log("[LevelEditor] Pasted decor from clipboard.");
    }

    private static DecorData CloneDecor(DecorData source)
    {
        return source == null ? null : JsonUtility.FromJson<DecorData>(JsonUtility.ToJson(source, true));
    }

    private static bool TryGetDecorFromClipboard(out DecorData decor)
    {
        string clipboard = EditorGUIUtility.systemCopyBuffer;
        if (string.IsNullOrWhiteSpace(clipboard) || !clipboard.StartsWith(DecorClipboardPrefix, StringComparison.Ordinal))
        {
            decor = null;
            return false;
        }

        try
        {
            decor = JsonUtility.FromJson<DecorData>(clipboard.Substring(DecorClipboardPrefix.Length));
            return decor != null;
        }
        catch
        {
            decor = null;
            return false;
        }
    }

    private static bool HasDecorClipboard()
    {
        return TryGetDecorFromClipboard(out _);
    }

    private void DrawTreeConditions(TreeData tree)
    {
        if (tree.conditions == null)
            tree.conditions = new List<TreeConditionData>();

        EditorGUILayout.Space(8);
        GUILayout.Label("Conditions", EditorStyles.boldLabel);

        if (tree.conditions.Count == 0)
        {
            EditorGUILayout.HelpBox("List is empty.", MessageType.None);
        }
        else
        {
            _selectedConditionIndex = Mathf.Clamp(_selectedConditionIndex, 0, tree.conditions.Count - 1);
            for (int i = 0; i < tree.conditions.Count; i++)
            {
                TreeConditionData condition = tree.conditions[i];
                bool selected = _selectedConditionIndex == i;

                Rect boxRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (selected)
                    EditorGUI.DrawRect(boxRect, new Color(0.20f, 0.34f, 0.52f, 0.35f));
                if (Event.current.type == EventType.MouseDown && boxRect.Contains(Event.current.mousePosition))
                {
                    _selectedConditionIndex = i;
                    Repaint();
                }

                EditorGUILayout.LabelField("Element " + i, EditorStyles.boldLabel);
                condition.conditionType = (TreeConditionType)EditorGUILayout.EnumPopup("Condition", condition.conditionType);

                if (condition.conditionType == TreeConditionType.NearTreeCount)
                {
                    condition.n = EditorGUILayout.IntField("n", condition.n);
                    if (i == 0)
                        tree.parameterN = condition.n;
                }
                else if (condition.conditionType == TreeConditionType.NearSpecificTree)
                {
                    condition.targetTreeId = DrawTreeIdPopup("Target Tree", tree, condition.targetTreeId);
                }
                else if (condition.conditionType == TreeConditionType.NeighborSmell)
                {
                    condition.smell = DrawSmellConditionTargetPopup("Smell", condition.smell);
                    condition.smellPreference = (SmellConditionPreference)EditorGUILayout.EnumPopup("Preference", condition.smellPreference);
                }
                else if (condition.conditionType == TreeConditionType.EmitSmell)
                {
                    condition.smell = DrawSmellConditionTargetPopup("Emits", condition.smell);
                }

                EditorGUILayout.EndVertical();
            }
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("+", GUILayout.Width(32), GUILayout.Height(22)))
        {
            tree.conditions.Add(new TreeConditionData { conditionType = TreeConditionType.NearTreeCount, n = tree.parameterN });
            _selectedConditionIndex = tree.conditions.Count - 1;
        }
        EditorGUI.BeginDisabledGroup(tree.conditions.Count == 0);
        if (GUILayout.Button("-", GUILayout.Width(32), GUILayout.Height(22)))
        {
            int removeIndex = Mathf.Clamp(_selectedConditionIndex, 0, tree.conditions.Count - 1);
            tree.conditions.RemoveAt(removeIndex);
            _selectedConditionIndex = Mathf.Clamp(removeIndex - 1, 0, tree.conditions.Count - 1);
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
    }

    private string DrawTreeIdPopup(string label, TreeData owner, string currentId)
    {
        List<string> ids = new List<string>();
        foreach (TreeData tree in _level.trees)
        {
            if (tree != owner)
                ids.Add(tree.treeId);
        }

        if (ids.Count == 0)
        {
            EditorGUILayout.TextField(label, currentId ?? "");
            return "";
        }

        int selectedIndex = Mathf.Max(0, ids.IndexOf(currentId));
        selectedIndex = EditorGUILayout.Popup(label, selectedIndex, ids.ToArray());
        return ids[selectedIndex];
    }

    private string DrawLandIdPopup(string label, string currentId)
    {
        List<string> ids = new List<string> { "(Not set)" };
        foreach (LevelCellData land in _level.cells)
            ids.Add(land.landId);

        int selectedIndex = string.IsNullOrWhiteSpace(currentId)
            ? 0
            : ids.IndexOf(currentId);
        selectedIndex = Mathf.Max(0, selectedIndex);
        selectedIndex = EditorGUILayout.Popup(label, selectedIndex, ids.ToArray());
        return selectedIndex == 0 ? "" : ids[selectedIndex];
    }

    private static PlantSmell DrawSmellConditionTargetPopup(string label, PlantSmell currentSmell)
    {
        PlantSmell[] smells = { PlantSmell.Perfume, PlantSmell.Disgust };
        string[] labels = { PlantSmell.Perfume.ToString(), PlantSmell.Disgust.ToString() };
        int selectedIndex = currentSmell == PlantSmell.Disgust ? 1 : 0;
        selectedIndex = EditorGUILayout.Popup(label, selectedIndex, labels);
        return smells[selectedIndex];
    }

    private void NewLevel()
    {
        if (!EditorUtility.DisplayDialog("New Level", "Clear current level data?", "New", "Cancel"))
            return;
        _level = new LevelData();
        _currentJsonPath = "";
        Select(SelectionKind.None, -1);
        NormalizeLevelData();
        Repaint();
    }

    private void SaveToJson(bool forceSavePanel)
    {
        NormalizeLevelData();
        string name = string.IsNullOrWhiteSpace(_level.levelName) ? "level" : _level.levelName;
        string path;

        if (forceSavePanel)
        {
            path = EditorUtility.SaveFilePanel("Save Level JSON", Application.dataPath, name + ".json", "json");
            if (string.IsNullOrEmpty(path))
                return;
        }
        else
        {
            string folderPath = GetLevelFolderPath();
            Directory.CreateDirectory(folderPath);
            path = Path.Combine(folderPath, SanitizeFileName(name) + ".json");
        }

        File.WriteAllText(path, JsonUtility.ToJson(_level, true));
        _currentJsonPath = path;
        AssetDatabase.Refresh();
        Debug.Log("[LevelEditor] Saved level JSON: " + path);
    }

    private void LoadFromJson()
    {
        string path = EditorUtility.OpenFilePanel("Load Level JSON", GetLevelFolderPath(), "json");
        if (string.IsNullOrEmpty(path))
            return;

        LevelData loaded = JsonUtility.FromJson<LevelData>(File.ReadAllText(path));
        if (loaded == null)
        {
            EditorUtility.DisplayDialog("Load Level", "Could not parse JSON file.", "OK");
            return;
        }

        _level = loaded;
        _currentJsonPath = path;
        Select(SelectionKind.None, -1);
        NormalizeLevelData();
        Repaint();
        Debug.Log("[LevelEditor] Loaded level JSON: " + path);
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(invalidChar, '_');
        return string.IsNullOrWhiteSpace(fileName) ? "level" : fileName.Trim();
    }

    private static string GetLevelFolderPath()
    {
        string projectPath = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectPath, LevelFolderRelativePath);
    }

    private void DeleteSelectedLand()
    {
        if (!IsValidLandSelection())
            return;

        List<int> removeIndices = GetSortedSelectedLandIndices();
        if (removeIndices.Count == 0)
            return;

        HashSet<string> removedIds = new HashSet<string>();
        for (int i = 0; i < removeIndices.Count; i++)
        {
            int index = removeIndices[i];
            if (index >= 0 && index < _level.cells.Count)
                removedIds.Add(_level.cells[index].landId);
        }

        for (int i = removeIndices.Count - 1; i >= 0; i--)
        {
            int index = removeIndices[i];
            if (index >= 0 && index < _level.cells.Count)
                _level.cells.RemoveAt(index);
        }

        foreach (LevelCellData land in _level.cells)
            for (int i = land.neighborIds.Count - 1; i >= 0; i--)
                if (removedIds.Contains(land.neighborIds[i]))
                    land.neighborIds.RemoveAt(i);

        foreach (TreeData tree in _level.trees)
            if (removedIds.Contains(tree.solutionLandId))
                tree.solutionLandId = "";

        Select(SelectionKind.None, -1);
        Repaint();
    }

    private void DeleteSelectedTree()
    {
        if (!IsValidTreeSelection())
            return;
        string id = _level.trees[_selectedTreeIndex].treeId;
        _level.trees.RemoveAt(_selectedTreeIndex);
        foreach (TreeData tree in _level.trees)
            ClearConditionTarget(tree, id);
        Select(SelectionKind.None, -1);
        Repaint();
    }

    private void DeleteSelectedDecor()
    {
        if (!IsValidDecorSelection())
            return;
        _level.decorations.RemoveAt(_selectedDecorIndex);
        Select(SelectionKind.None, -1);
        Repaint();
    }

    private void SetLandNeighbor(LevelCellData a, LevelCellData b, bool connected)
    {
        SetNeighborId(a.neighborIds, b.landId, connected);
        SetNeighborId(b.neighborIds, a.landId, connected);
        Repaint();
    }

    private static void SetNeighborId(List<string> ids, string id, bool connected)
    {
        if (ids == null)
            return;
        if (connected)
        {
            if (!ids.Contains(id))
                ids.Add(id);
        }
        else
        {
            ids.Remove(id);
        }
    }

    private void RenameLandId(string oldId, string newId)
    {
        if (string.IsNullOrWhiteSpace(newId))
            return;
        foreach (LevelCellData land in _level.cells)
        {
            for (int i = 0; i < land.neighborIds.Count; i++)
                if (land.neighborIds[i] == oldId)
                    land.neighborIds[i] = newId;
        }
        foreach (TreeData tree in _level.trees)
            if (tree.solutionLandId == oldId)
                tree.solutionLandId = newId;
        NormalizeLevelData();
    }

    private void RenameTreeId(string oldId, string newId)
    {
        if (string.IsNullOrWhiteSpace(newId))
            return;
        foreach (TreeData tree in _level.trees)
            RenameConditionTarget(tree, oldId, newId);
        NormalizeLevelData();
    }

    private void NormalizeLevelData()
    {
        if (_level == null)
            _level = new LevelData();
        if (_level.cells == null)
            _level.cells = new List<LevelCellData>();
        if (_level.trees == null)
            _level.trees = new List<TreeData>();
        _level.screenWidth = Mathf.Clamp(_level.screenWidth <= 0 ? 1080 : _level.screenWidth, 1, 4000);
        _level.screenHeight = Mathf.Clamp(_level.screenHeight <= 0 ? 2340 : _level.screenHeight, 1, 4000);
        _level.maxMoves = Mathf.Max(0, _level.maxMoves);
        _level.slotSize = Mathf.Clamp(_level.slotSize <= 0 ? 125 : _level.slotSize, 12, 160);
        _level.itemSize = Mathf.Clamp(_level.itemSize <= 0 ? 100 : _level.itemSize, 8, 300);

        HashSet<string> landIds = new HashSet<string>();
        for (int i = 0; i < _level.cells.Count; i++)
        {
            LevelCellData land = _level.cells[i];
            if (string.IsNullOrWhiteSpace(land.landId))
                land.landId = "land_" + (i + 1).ToString("D2");
            land.landId = MakeUniqueId(land.landId, landIds);
            if (land.neighborIds == null)
                land.neighborIds = new List<string>();
            RemoveInvalidAndDuplicateIds(land.neighborIds, land.landId, true);
        }

        HashSet<string> treeIds = new HashSet<string>();
        for (int i = 0; i < _level.trees.Count; i++)
        {
            TreeData tree = _level.trees[i];
            if (string.IsNullOrWhiteSpace(tree.treeId))
                tree.treeId = "tree_" + (i + 1).ToString("D2");
            tree.treeId = MakeUniqueId(tree.treeId, treeIds);
            if (FindLand(tree.solutionLandId) == null)
                tree.solutionLandId = "";
            if (tree.conditions == null)
                tree.conditions = new List<TreeConditionData>();
            NormalizeTreeConditions(tree);
        }

        if (_level.decorations == null)
            _level.decorations = new List<DecorData>();
        HashSet<string> decorIds = new HashSet<string>();
        for (int i = 0; i < _level.decorations.Count; i++)
        {
            DecorData decor = _level.decorations[i];
            if (string.IsNullOrWhiteSpace(decor.decorId))
                decor.decorId = "decor_" + (i + 1).ToString("D2");
            decor.decorId = MakeUniqueId(decor.decorId, decorIds);

            decor.sizeMode = DecorSizeMode.Normal;
            decor.width  = Mathf.Max(1, decor.width);
            decor.height = Mathf.Max(1, decor.height);
        }
    }

    private void RenameConditionTarget(TreeData tree, string oldId, string newId)
    {
        if (tree.conditions == null)
            return;
        foreach (TreeConditionData condition in tree.conditions)
            if (condition.conditionType == TreeConditionType.NearSpecificTree && condition.targetTreeId == oldId)
                condition.targetTreeId = newId;
    }

    private void ClearConditionTarget(TreeData tree, string deletedId)
    {
        if (tree.conditions == null)
            return;
        foreach (TreeConditionData condition in tree.conditions)
            if (condition.conditionType == TreeConditionType.NearSpecificTree && condition.targetTreeId == deletedId)
                condition.targetTreeId = "";
    }

    private void NormalizeTreeConditions(TreeData tree)
    {
        for (int i = 0; i < tree.conditions.Count; i++)
        {
            TreeConditionData condition = tree.conditions[i];
            if (condition == null)
            {
                tree.conditions.RemoveAt(i);
                i--;
                continue;
            }

            if (condition.conditionType == TreeConditionType.NearSpecificTree && FindTree(condition.targetTreeId) == null)
                condition.targetTreeId = "";
            if ((condition.conditionType == TreeConditionType.NeighborSmell ||
                 condition.conditionType == TreeConditionType.EmitSmell) &&
                condition.smell == PlantSmell.None)
            {
                condition.smell = PlantSmell.Perfume;
            }
        }
    }

    private void AutoConnectNeighborsByDistance(float maxDistance, bool replaceExisting, bool selectedOnly)
    {
        List<LevelCellData> targets = GetAutoNeighborTargets(selectedOnly);
        if (targets.Count < 2)
        {
            Debug.LogWarning("[LevelEditor] Auto Neighbor needs at least 2 lands in the target set.");
            return;
        }

        HashSet<string> targetIds = new HashSet<string>();
        for (int i = 0; i < targets.Count; i++)
            targetIds.Add(targets[i].landId);

        if (replaceExisting)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                LevelCellData land = targets[i];
                for (int neighborIndex = land.neighborIds.Count - 1; neighborIndex >= 0; neighborIndex--)
                {
                    if (!targetIds.Contains(land.neighborIds[neighborIndex]))
                        continue;

                    land.neighborIds.RemoveAt(neighborIndex);
                }
            }
        }

        float maxDistanceSqr = maxDistance * maxDistance;
        int linkCount = 0;
        for (int i = 0; i < targets.Count; i++)
        {
            LevelCellData a = targets[i];
            for (int j = i + 1; j < targets.Count; j++)
            {
                LevelCellData b = targets[j];
                float dx = a.x - b.x;
                float dy = a.y - b.y;
                float distanceSqr = dx * dx + dy * dy;
                if (distanceSqr > maxDistanceSqr)
                    continue;

                bool wasConnected = a.neighborIds.Contains(b.landId) && b.neighborIds.Contains(a.landId);
                SetNeighborId(a.neighborIds, b.landId, true);
                SetNeighborId(b.neighborIds, a.landId, true);
                if (!wasConnected)
                    linkCount++;
            }
        }

        Repaint();
        Debug.Log("[LevelEditor] Auto Neighbor connected " + linkCount + " link(s) with max distance " + maxDistance + ".");
    }

    private List<LevelCellData> GetAutoNeighborTargets(bool selectedOnly)
    {
        List<LevelCellData> targets = new List<LevelCellData>();
        if (!selectedOnly)
        {
            targets.AddRange(_level.cells);
            return targets;
        }

        List<int> selectedIndices = GetSortedSelectedLandIndices();
        for (int i = 0; i < selectedIndices.Count; i++)
            targets.Add(_level.cells[selectedIndices[i]]);

        return targets;
    }

    private void AutoSolveTrees()
    {
        bool selectedSolve = _selectionKind == SelectionKind.Tree && IsValidTreeSelection();
        List<TreeData> treesToSolve = selectedSolve
            ? new List<TreeData> { _level.trees[_selectedTreeIndex] }
            : new List<TreeData>(_level.trees);

        if (treesToSolve.Count == 0)
        {
            Debug.LogWarning("[LevelEditor] Auto Solve has no trees to solve.");
            return;
        }

        int solvedCount = 0;
        for (int i = 0; i < treesToSolve.Count; i++)
        {
            TreeData tree = treesToSolve[i];
            if (tree == null)
                continue;

            string solutionId = FindAutoSolveLandId(tree);
            if (string.IsNullOrEmpty(solutionId))
                continue;

            tree.solutionLandId = solutionId;
            solvedCount++;
        }

        if (solvedCount == 0)
            Debug.LogWarning("[LevelEditor] Auto Solve found no valid land for the tree(s).");
        else
            Debug.Log("[LevelEditor] Auto Solve set solution land for " + solvedCount + " tree(s).");

        Repaint();
    }

    private string FindAutoSolveLandId(TreeData tree)
    {
        if (tree == null || _level == null || _level.cells == null || _level.cells.Count == 0)
            return "";

        for (int i = 0; i < _level.cells.Count; i++)
        {
            LevelCellData land = _level.cells[i];
            if (land != null && TreeMatchesAllConditions(tree, land))
                return land.landId;
        }

        return "";
    }

    private bool TreeMatchesAllConditions(TreeData tree, LevelCellData land)
    {
        if (tree == null || tree.conditions == null || land == null)
            return false;

        for (int i = 0; i < tree.conditions.Count; i++)
        {
            TreeConditionData condition = tree.conditions[i];
            if (condition == null)
                continue;

            if (!ConditionMatchesLand(condition, land, tree))
                return false;
        }

        return true;
    }

    private bool ConditionMatchesLand(TreeConditionData condition, LevelCellData land, TreeData ownerTree)
    {
        switch (condition.conditionType)
        {
            case TreeConditionType.NearTreeCount:
                return GetNeighborTreeCount(land, ownerTree) == condition.n;
            case TreeConditionType.NearSpecificTree:
                if (string.IsNullOrWhiteSpace(condition.targetTreeId))
                    return false;
                return IsTreePlacedNearLand(land, condition.targetTreeId);
            case TreeConditionType.EdgeSlot:
                return land.isEdge;
            case TreeConditionType.CornerSlot:
                return land.isCorner;
            case TreeConditionType.Row1Slot:
                return land.row == BoardRow.Row1;
            case TreeConditionType.Row2Slot:
                return land.row == BoardRow.Row2;
            case TreeConditionType.Row3Slot:
                return land.row == BoardRow.Row3;
            case TreeConditionType.PlantAlone:
                return GetNeighborTreeCount(land, ownerTree) == 0;
            case TreeConditionType.Anywhere:
                return true;
            case TreeConditionType.NeighborSmell:
            case TreeConditionType.EmitSmell:
                return true;
            default:
                return true;
        }
    }

    private int GetNeighborTreeCount(LevelCellData land, TreeData ownerTree)
    {
        if (land == null || _level == null || _level.cells == null)
            return 0;

        int count = 0;
        for (int i = 0; i < land.neighborIds.Count; i++)
        {
            string neighborId = land.neighborIds[i];
            if (string.IsNullOrWhiteSpace(neighborId))
                continue;

            LevelCellData neighbor = FindLand(neighborId);
            if (neighbor == null)
                continue;

            if (IsLandUsedAsTreeSolution(neighbor, ownerTree))
                count++;
        }

        return count;
    }

    private bool IsTreePlacedNearLand(LevelCellData land, string targetTreeId)
    {
        if (land == null || string.IsNullOrWhiteSpace(targetTreeId))
            return false;

        for (int i = 0; i < land.neighborIds.Count; i++)
        {
            string neighborId = land.neighborIds[i];
            if (string.IsNullOrWhiteSpace(neighborId))
                continue;

            LevelCellData neighbor = FindLand(neighborId);
            if (neighbor == null)
                continue;

            for (int j = 0; j < _level.trees.Count; j++)
            {
                TreeData tree = _level.trees[j];
                if (tree == null)
                    continue;

                if (tree.treeId == targetTreeId && tree.solutionLandId == neighbor.landId)
                    return true;
            }
        }

        return false;
    }

    private bool IsLandUsedAsTreeSolution(LevelCellData land, TreeData ignoreTree)
    {
        if (land == null)
            return false;

        for (int i = 0; i < _level.trees.Count; i++)
        {
            TreeData tree = _level.trees[i];
            if (tree == null || tree == ignoreTree)
                continue;

            if (tree.solutionLandId == land.landId)
                return true;
        }

        return false;
    }

    private static string MakeUniqueId(string requestedId, HashSet<string> usedIds)
    {
        string baseId = requestedId.Trim();
        string candidate = baseId;
        int suffix = 2;
        while (usedIds.Contains(candidate))
        {
            candidate = baseId + "_" + suffix;
            suffix++;
        }
        usedIds.Add(candidate);
        return candidate;
    }

    private void RemoveInvalidAndDuplicateIds(List<string> ids, string ownId, bool landIds)
    {
        for (int i = ids.Count - 1; i >= 0; i--)
        {
            string id = ids[i];
            bool valid = landIds ? FindLand(id) != null : FindTree(id) != null;
            if (string.IsNullOrWhiteSpace(id) || id == ownId || !valid || ids.IndexOf(id) != i)
                ids.RemoveAt(i);
        }
    }

    private string NextLandId()
    {
        int index = _level.cells.Count + 1;
        string id;
        do
        {
            id = "land_" + index.ToString("D2");
            index++;
        } while (FindLand(id) != null);
        return id;
    }

    private string NextTreeId()
    {
        int index = _level.trees.Count + 1;
        string id;
        do
        {
            id = "tree_" + index.ToString("D2");
            index++;
        } while (FindTree(id) != null);
        return id;
    }

    private string NextDecorId()
    {
        if (_level.decorations == null)
            return "decor_01";
        int index = _level.decorations.Count + 1;
        string id;
        do
        {
            id = "decor_" + index.ToString("D2");
            index++;
        } while (_level.decorations.Exists(d => d.decorId == id));
        return id;
    }

    private LevelCellData FindLand(string id)
    {
        return _level.cells.Find(land => land.landId == id);
    }

    private TreeData FindTree(string id)
    {
        return _level.trees.Find(tree => tree.treeId == id);
    }

    private Rect NodeRect(Rect canvasRect, int x, int y, float size)
    {
        Vector2 center = ToCanvas(canvasRect, x, y);
        float scaledSize = size * _canvasZoom;
        return new Rect(center.x - scaledSize * 0.5f, center.y - scaledSize * 0.5f, scaledSize, scaledSize);
    }

    private Vector2 ToCanvas(Rect rect, LevelCellData land)
    {
        return ToCanvas(rect, land.x, land.y);
    }

    private Vector2 ToCanvas(Rect rect, TreeData tree)
    {
        return ToCanvas(rect, tree.x, tree.y);
    }

    private Vector2 ToCanvas(Rect rect, int x, int y)
    {
        return new Vector2(
            rect.x + CanvasPadding + _canvasPan.x + x * _canvasZoom,
            rect.y + CanvasPadding + _canvasPan.y + y * _canvasZoom);
    }

    private Vector2 FromCanvas(Rect rect, Vector2 canvasPosition)
    {
        return new Vector2(
            (canvasPosition.x - rect.x - CanvasPadding - _canvasPan.x) / _canvasZoom,
            (canvasPosition.y - rect.y - CanvasPadding - _canvasPan.y) / _canvasZoom);
    }

    private void Select(SelectionKind kind, int index)
    {
        _selectionKind = kind;
        if (kind == SelectionKind.Land)
        {
            _selectedLandIndex = index;
            _selectedLandIndices.Clear();
            if (index >= 0 && index < _level.cells.Count)
                _selectedLandIndices.Add(index);
        }
        else
        {
            _selectedLandIndex = -1;
            _selectedLandIndices.Clear();
        }

        _selectedTreeIndex  = kind == SelectionKind.Tree  ? index : -1;
        _selectedDecorIndex = kind == SelectionKind.Decor ? index : -1;
        Repaint();
    }

    private void ToggleLandSelection(int index)
    {
        if (index < 0 || index >= _level.cells.Count)
            return;

        if (_selectionKind != SelectionKind.Land)
        {
            _selectionKind = SelectionKind.Land;
            _selectedTreeIndex = -1;
            _selectedLandIndices.Clear();
        }

        if (_selectedLandIndices.Contains(index))
        {
            if (_selectedLandIndices.Count > 1)
                _selectedLandIndices.Remove(index);
        }
        else
        {
            _selectedLandIndices.Add(index);
        }

        _selectedLandIndex = index;
        Repaint();
    }

    private bool IsLandSelected(int index)
    {
        return _selectionKind == SelectionKind.Land && _selectedLandIndices.Contains(index);
    }

    private List<int> GetSortedSelectedLandIndices()
    {
        List<int> indices = new List<int>();
        foreach (int index in _selectedLandIndices)
        {
            if (index >= 0 && index < _level.cells.Count)
                indices.Add(index);
        }

        if (indices.Count == 0 && IsValidLandSelection())
            indices.Add(_selectedLandIndex);

        indices.Sort();
        return indices;
    }

    private bool HasMixedSlotType(List<int> selectedIndices, SlotType first)
    {
        for (int i = 0; i < selectedIndices.Count; i++)
            if (_level.cells[selectedIndices[i]].slotType != first)
                return true;
        return false;
    }

    private bool HasMixedRow(List<int> selectedIndices, BoardRow first)
    {
        for (int i = 0; i < selectedIndices.Count; i++)
            if (_level.cells[selectedIndices[i]].row != first)
                return true;
        return false;
    }

    private bool HasMixedCorner(List<int> selectedIndices, bool first)
    {
        for (int i = 0; i < selectedIndices.Count; i++)
            if (_level.cells[selectedIndices[i]].isCorner != first)
                return true;
        return false;
    }

    private bool HasMixedEdge(List<int> selectedIndices, bool first)
    {
        for (int i = 0; i < selectedIndices.Count; i++)
            if (_level.cells[selectedIndices[i]].isEdge != first)
                return true;
        return false;
    }

    private void ApplySlotTypeToSelectedLands(List<int> selectedIndices, SlotType slotType)
    {
        for (int i = 0; i < selectedIndices.Count; i++)
            _level.cells[selectedIndices[i]].slotType = slotType;
    }

    private void ApplyRowToSelectedLands(List<int> selectedIndices, BoardRow row)
    {
        for (int i = 0; i < selectedIndices.Count; i++)
            _level.cells[selectedIndices[i]].row = row;
    }

    private void ApplyCornerToSelectedLands(List<int> selectedIndices, bool isCorner)
    {
        for (int i = 0; i < selectedIndices.Count; i++)
            _level.cells[selectedIndices[i]].isCorner = isCorner;
    }

    private void ApplyEdgeToSelectedLands(List<int> selectedIndices, bool isEdge)
    {
        for (int i = 0; i < selectedIndices.Count; i++)
            _level.cells[selectedIndices[i]].isEdge = isEdge;
    }

    private void ApplyPositionDeltaToSelectedLands(List<int> selectedIndices, int deltaX, int deltaY)
    {
        for (int i = 0; i < selectedIndices.Count; i++)
        {
            LevelCellData land = _level.cells[selectedIndices[i]];
            float targetX = Mathf.Clamp(land.x + deltaX, 100f, 1000f);
            float targetY = Mathf.Clamp(land.y + deltaY, 620f, 1340f);
            land.x = _snapEnabled ? SnapValue(targetX) : Mathf.RoundToInt(targetX);
            land.y = _snapEnabled ? SnapValue(targetY) : Mathf.RoundToInt(targetY);
        }
    }

    private string[] GetSelectedLandIds(List<int> selectedIndices)
    {
        string[] ids = new string[selectedIndices.Count];
        for (int i = 0; i < selectedIndices.Count; i++)
            ids[i] = _level.cells[selectedIndices[i]].landId;
        return ids;
    }

    private int GetSelectedIndex()
    {
        if (_selectionKind == SelectionKind.Land)
            return _selectedLandIndex;
        if (_selectionKind == SelectionKind.Tree)
            return _selectedTreeIndex;
        if (_selectionKind == SelectionKind.Decor)
            return _selectedDecorIndex;
        return -1;
    }

    private bool IsValidLandSelection()
    {
        if (_selectedLandIndex < 0 || _selectedLandIndex >= _level.cells.Count)
            return false;

        if (_selectedLandIndices.Count == 0)
            _selectedLandIndices.Add(_selectedLandIndex);

        return true;
    }

    private bool IsValidTreeSelection()
    {
        return _selectedTreeIndex >= 0 && _selectedTreeIndex < _level.trees.Count;
    }

    private bool IsValidDecorSelection()
    {
        return _level.decorations != null
            && _selectedDecorIndex >= 0
            && _selectedDecorIndex < _level.decorations.Count;
    }
}
