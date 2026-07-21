using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class LevelEditorWindow : EditorWindow
{
    private enum SelectionKind { None, Land, Tree }

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
    private Vector2 _landScroll;
    private Vector2 _treeScroll;
    private Vector2 _inspectorScroll;
    private SelectionKind _selectionKind = SelectionKind.None;
    private int _selectedLandIndex = -1;
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
        GUILayout.Label("Level JSON", EditorStyles.boldLabel);
        _level.levelName = EditorGUILayout.TextField("Name", _level.levelName);
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
        _level.itemSize = EditorGUILayout.IntSlider("Item Size", _level.itemSize, 8, 160);

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

        EditorGUILayout.Space(10);
        GUILayout.Label("Lands", EditorStyles.boldLabel);
        _landScroll = EditorGUILayout.BeginScrollView(_landScroll, GUILayout.MinHeight(90));
        for (int i = 0; i < _level.cells.Count; i++)
            DrawListButton(i, SelectionKind.Land);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);
        GUILayout.Label("Trees", EditorStyles.boldLabel);
        _treeScroll = EditorGUILayout.BeginScrollView(_treeScroll, GUILayout.MinHeight(90));
        for (int i = 0; i < _level.trees.Count; i++)
            DrawListButton(i, SelectionKind.Tree);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    private void DrawListButton(int index, SelectionKind kind)
    {
        bool selected = _selectionKind == kind && GetSelectedIndex() == index;
        string label = kind == SelectionKind.Land
            ? _level.cells[index].landId + "  (" + _level.cells[index].slotType + ")"
            : _level.trees[index].treeId;

        Color previous = GUI.backgroundColor;
        GUI.backgroundColor = selected ? new Color(0.5f, 0.7f, 1f) : previous;
        if (GUILayout.Button(label, GUILayout.Height(24)))
            Select(kind, index);
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
            DrawNode(nodeRect, color, _selectionKind == SelectionKind.Land && _selectedLandIndex == i, land.landId);
        }
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
        bool isDraggingNode = _dragLandIndex >= 0 || _dragTreeIndex >= 0;

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
            _dragLandIndex = -1;
            _dragTreeIndex = -1;
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
                Select(SelectionKind.Land, i);
                _dragLandIndex = i;
                _dragTreeIndex = -1;
                _dragOffset = e.mousePosition - new Vector2(nodeRect.x, nodeRect.y);
                _dragNodeSize = nodeRect.width;
                e.Use();
                return;
            }

            Select(SelectionKind.None, -1);
            e.Use();
        }

        if (e.type == EventType.MouseDrag && e.button == 0)
        {
            Vector2 clampedMousePosition = ClampToRect(e.mousePosition, rect);
            if (_dragLandIndex >= 0 && _dragLandIndex < _level.cells.Count)
            {
                Vector2 pos = FromCanvas(rect, clampedMousePosition - _dragOffset + Vector2.one * (_dragNodeSize * 0.5f));
                _level.cells[_dragLandIndex].x = Mathf.RoundToInt(pos.x);
                _level.cells[_dragLandIndex].y = Mathf.RoundToInt(pos.y);
                Repaint();
                e.Use();
            }
            else if (_dragTreeIndex >= 0 && _dragTreeIndex < _level.trees.Count)
            {
                Vector2 pos = FromCanvas(rect, clampedMousePosition - _dragOffset + Vector2.one * (_dragNodeSize * 0.5f));
                _level.trees[_dragTreeIndex].x = Mathf.RoundToInt(pos.x);
                _level.trees[_dragTreeIndex].y = Mathf.RoundToInt(pos.y);
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
            DrawLandInspector(_level.cells[_selectedLandIndex]);
        else if (_selectionKind == SelectionKind.Tree && IsValidTreeSelection())
            DrawTreeInspector(_level.trees[_selectedTreeIndex]);
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

    private void DrawTreeInspector(TreeData tree)
    {
        GUILayout.Label("Tree", EditorStyles.boldLabel);
        string oldId = tree.treeId;
        tree.treeId = EditorGUILayout.TextField("ID", tree.treeId);
        if (tree.treeId != oldId)
            RenameTreeId(oldId, tree.treeId);

        tree.itemType = (ItemType)EditorGUILayout.EnumPopup("Item Type", tree.itemType);
        tree.requiredSlotType = (SlotType)EditorGUILayout.EnumPopup("Required Slot", tree.requiredSlotType);
        tree.parameterN = EditorGUILayout.IntField("Parameter n", tree.parameterN);

        DrawPositionFields(ref tree.x, ref tree.y);

        GUILayout.Label("Notes");
        tree.customNotes = EditorGUILayout.TextArea(tree.customNotes ?? "", GUILayout.Height(54));

        DrawTreeConditions(tree);

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Delete Tree", GUILayout.Height(28)))
            DeleteSelectedTree();
    }

    private void AddLand()
    {
        LevelCellData land = new LevelCellData
        {
            landId = NextLandId(),
            x = 80 + _level.cells.Count * 30,
            y = 80 + _level.cells.Count * 18,
            slotType = SlotType.Dirt,
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

    private void AddTree()
    {
        TreeData tree = new TreeData
        {
            treeId = NextTreeId(),
            x = 120 + _level.trees.Count * 30,
            y = 180 + _level.trees.Count * 18,
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
                else
                {
                    condition.targetTreeId = DrawTreeIdPopup("Target Tree", tree, condition.targetTreeId);
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
        string id = _level.cells[_selectedLandIndex].landId;
        _level.cells.RemoveAt(_selectedLandIndex);
        foreach (LevelCellData land in _level.cells)
            land.neighborIds.Remove(id);
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
        _level.screenWidth = Mathf.Clamp(_level.screenWidth <= 0 ? 540 : _level.screenWidth, 1, 4000);
        _level.screenHeight = Mathf.Clamp(_level.screenHeight <= 0 ? 960 : _level.screenHeight, 1, 4000);
        _level.slotSize = Mathf.Clamp(_level.slotSize <= 0 ? 48 : _level.slotSize, 12, 160);
        _level.itemSize = Mathf.Clamp(_level.itemSize <= 0 ? 32 : _level.itemSize, 8, 160);

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
            if (tree.conditions == null)
                tree.conditions = new List<TreeConditionData>();
            NormalizeTreeConditions(tree);
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
        }
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
        _selectedLandIndex = kind == SelectionKind.Land ? index : -1;
        _selectedTreeIndex = kind == SelectionKind.Tree ? index : -1;
        Repaint();
    }

    private int GetSelectedIndex()
    {
        if (_selectionKind == SelectionKind.Land)
            return _selectedLandIndex;
        if (_selectionKind == SelectionKind.Tree)
            return _selectedTreeIndex;
        return -1;
    }

    private bool IsValidLandSelection()
    {
        return _selectedLandIndex >= 0 && _selectedLandIndex < _level.cells.Count;
    }

    private bool IsValidTreeSelection()
    {
        return _selectedTreeIndex >= 0 && _selectedTreeIndex < _level.trees.Count;
    }
}
