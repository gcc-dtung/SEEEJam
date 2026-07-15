using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class LevelEditorWindow : EditorWindow
{
    // ── State ──────────────────────────────────────────────────────────────
    private LevelData _level = new LevelData();

    // Land panel
    private SlotType _paintType   = SlotType.Dirt;
    private string   _paintItemId = "";
    private List<LevelCellData> _spawnedLands = new List<LevelCellData>();
    private int _selectedLandIndex = -1;

    // Tree panel
    private List<TreeData> _spawnedTrees = new List<TreeData>();
    private int _selectedTreeIndex = -1;

    // ── Tabs ──────────────────────────────────────────────────────────────
    private enum BottomTab { Land, Tree }
    private BottomTab _activeTab = BottomTab.Land;

    // ── Layout constants ───────────────────────────────────────────────────
    private const float CellSize     = 42f;
    private const float HeaderH      = 110f;
    private const float PaletteW     = 155f;
    private const float BottomH      = 220f;
    private const float PaddingOuter = 10f;

    private static readonly Dictionary<SlotType, Color> SlotColors = new Dictionary<SlotType, Color>
    {
        { SlotType.Dirt,    new Color(0.60f, 0.40f, 0.20f) },
        { SlotType.Sand,    new Color(0.93f, 0.84f, 0.55f) },
        { SlotType.Water,   new Color(0.25f, 0.55f, 0.90f) },
        { SlotType.DryDirt, new Color(0.72f, 0.58f, 0.38f) },
        { SlotType.Wait,    new Color(0.45f, 0.45f, 0.45f) },
    };

    private Vector2 _gridScroll;
    private Vector2 _listScroll;

    // ── Menu entry ─────────────────────────────────────────────────────────
    [MenuItem("Tools/Level Editor")]
    public static void ShowWindow()
    {
        var win = GetWindow<LevelEditorWindow>("Level Editor");
        win.minSize = new Vector2(600, 620);
        win.Show();
    }

    // =======================================================================
    // MAIN GUI
    // =======================================================================
    private void OnGUI()
    {
        DrawHeader();
        GUILayout.Space(4);

        float midH = Mathf.Max(80, position.height - HeaderH - BottomH - 28f);
        EditorGUILayout.BeginHorizontal(GUILayout.Height(midH));
        {
            DrawPalette(midH);
            GUILayout.Space(4);
            DrawGrid(midH);
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(4);
        DrawBottomPanel();
    }

    // =======================================================================
    // HEADER  (level name, size, Save JSON, Load JSON)
    // =======================================================================
    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            GUILayout.Label("Level Settings", EditorStyles.boldLabel);
            _level.levelName = EditorGUILayout.TextField("Level Name", _level.levelName);

            EditorGUILayout.BeginHorizontal();
            {
                int newW = EditorGUILayout.IntField("Width",  _level.width,  GUILayout.Width(180));
                int newH = EditorGUILayout.IntField("Height", _level.height, GUILayout.Width(180));
                if (newW != _level.width || newH != _level.height)
                    ResizeGrid(Mathf.Clamp(newW, 1, 50), Mathf.Clamp(newH, 1, 50));
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            {
                Color prev = GUI.backgroundColor;

                GUI.backgroundColor = new Color(0.30f, 0.70f, 0.35f);
                if (GUILayout.Button("Save as JSON", GUILayout.Height(32)))
                    SaveToJson();

                GUI.backgroundColor = new Color(0.30f, 0.55f, 0.80f);
                if (GUILayout.Button("Load from JSON", GUILayout.Height(32)))
                    LoadFromJson();

                GUI.backgroundColor = new Color(0.75f, 0.30f, 0.30f);
                if (GUILayout.Button("Clear All", GUILayout.Height(32), GUILayout.Width(80)))
                {
                    if (EditorUtility.DisplayDialog("Clear All", "Reset grid, lands and trees?", "Yes", "Cancel"))
                        ClearAll();
                }

                GUI.backgroundColor = prev;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    // =======================================================================
    // LEFT PALETTE
    // =======================================================================
    private void DrawPalette(float height)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(PaletteW), GUILayout.Height(height));
        {
            GUILayout.Label("Paint Type", EditorStyles.boldLabel);
            foreach (SlotType st in Enum.GetValues(typeof(SlotType)))
            {
                bool sel   = (_paintType == st);
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = sel
                    ? Color.Lerp(SlotColors[st], Color.white, 0.45f)
                    : SlotColors[st];

                GUIStyle btn = new GUIStyle(GUI.skin.button);
                if (sel) { btn.fontStyle = FontStyle.Bold; btn.normal.textColor = Color.black; }

                if (GUILayout.Button(st.ToString(), btn, GUILayout.Height(26)))
                    _paintType = st;

                GUI.backgroundColor = prev;
            }

            GUILayout.Space(8);
            GUILayout.Label("Item ID (paint)", EditorStyles.miniLabel);
            _paintItemId = EditorGUILayout.TextField(_paintItemId);
            EditorGUILayout.HelpBox("Leave blank = no item.", MessageType.None);
        }
        EditorGUILayout.EndVertical();
    }

    // =======================================================================
    // GRID
    // =======================================================================
    private void DrawGrid(float height)
    {
        EnsureGridFilled();
        float w = position.width - PaletteW - PaddingOuter * 3 - 4;

        _gridScroll = EditorGUILayout.BeginScrollView(
            _gridScroll, GUILayout.Width(w), GUILayout.Height(height));
        {
            for (int row = _level.height - 1; row >= 0; row--)
            {
                EditorGUILayout.BeginHorizontal();
                for (int col = 0; col < _level.width; col++)
                    DrawCell(GetCell(col, row), col, row);
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawCell(LevelCellData cell, int col, int row)
    {
        bool isLandSel = _selectedLandIndex >= 0
                         && _selectedLandIndex < _spawnedLands.Count
                         && _spawnedLands[_selectedLandIndex] == cell;
        bool isLand    = _spawnedLands.Contains(cell);

        TreeData treeHere = _level.trees.Find(t => t.x == col && t.y == row);
        bool isTreeSel    = treeHere != null && _selectedTreeIndex >= 0
                            && _selectedTreeIndex < _spawnedTrees.Count
                            && _spawnedTrees[_selectedTreeIndex] == treeHere;

        Color cellColor = SlotColors.TryGetValue(cell.slotType, out Color c) ? c : Color.grey;
        if (isLandSel) cellColor = Color.Lerp(cellColor, Color.yellow,         0.5f);
        if (isTreeSel) cellColor = Color.Lerp(cellColor, new Color(0.4f,1f,0.4f), 0.5f);

        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = cellColor;

        string label = cell.slotType.ToString().Substring(0, 2);
        if (treeHere != null)                        label = "T";
        else if (!string.IsNullOrEmpty(cell.itemId)) label = "*";
        else if (isLand)                             label = "#";

        GUIStyle style = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 9,
            fontStyle = (isLandSel || isTreeSel) ? FontStyle.Bold : FontStyle.Normal,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = Color.black }
        };

        if (GUILayout.Button(label, style, GUILayout.Width(CellSize), GUILayout.Height(CellSize)))
        {
            cell.slotType = _paintType;
            cell.itemId   = _paintItemId;
            GUI.changed   = true;
        }

        GUI.backgroundColor = prev;

        Rect last = GUILayoutUtility.GetLastRect();
        string tip = string.Format("({0},{1})  {2}", col, row, cell.slotType);
        if (!string.IsNullOrEmpty(cell.itemId)) tip += "\nItem: " + cell.itemId;
        if (isLand)         tip += "\n[Land]";
        if (treeHere != null) tip += "\n[Tree: " + treeHere.treeId + "]";
        EditorGUI.LabelField(last, new GUIContent("", tip));
    }

    // =======================================================================
    // BOTTOM PANEL  (tabbed: Spawn Land | Spawn Tree)
    // =======================================================================
    private void DrawBottomPanel()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(BottomH));
        {
            EditorGUILayout.BeginHorizontal();
            DrawTab("Spawn Land", BottomTab.Land);
            DrawTab("Spawn Tree", BottomTab.Tree);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(3);

            if (_activeTab == BottomTab.Land)
                DrawLandPanel();
            else
                DrawTreePanel();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawTab(string label, BottomTab tab)
    {
        bool active = (_activeTab == tab);
        Color prev  = GUI.backgroundColor;
        GUI.backgroundColor = active ? new Color(0.35f, 0.55f, 0.85f) : new Color(0.28f, 0.28f, 0.28f);
        GUIStyle style = new GUIStyle(GUI.skin.button)
            { fontStyle = active ? FontStyle.Bold : FontStyle.Normal };
        if (GUILayout.Button(label, style, GUILayout.Height(26)))
            _activeTab = tab;
        GUI.backgroundColor = prev;
    }

    // ---- Land panel -------------------------------------------------------
    private void DrawLandPanel()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Lands", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        if (GUILayout.Button("+ Spawn Land", GUILayout.Width(110), GUILayout.Height(22)))
            SpawnLand();
        GUI.backgroundColor = prev;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("#",       GUILayout.Width(22));
        GUILayout.Label("Type",    GUILayout.Width(80));
        GUILayout.Label("X",       GUILayout.Width(46));
        GUILayout.Label("Y",       GUILayout.Width(46));
        GUILayout.Label("Item ID", GUILayout.Width(90));
        EditorGUILayout.EndHorizontal();

        _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
        for (int i = 0; i < _spawnedLands.Count; i++)
            DrawLandRow(i);
        EditorGUILayout.EndScrollView();
    }

    private void DrawLandRow(int i)
    {
        LevelCellData land = _spawnedLands[i];
        bool sel = (_selectedLandIndex == i);

        Rect rowRect = EditorGUILayout.BeginHorizontal();
        EditorGUI.DrawRect(rowRect, sel
            ? new Color(0.27f, 0.40f, 0.60f)
            : (i % 2 == 0 ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.19f, 0.19f, 0.19f)));

        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = sel ? Color.yellow : Color.grey;
        if (GUILayout.Button(i.ToString(), GUILayout.Width(22), GUILayout.Height(20)))
        {
            _selectedLandIndex = sel ? -1 : i;
            Repaint();
        }
        GUI.backgroundColor = prev;

        SlotType newType = (SlotType)EditorGUILayout.EnumPopup(land.slotType, GUILayout.Width(80));
        if (newType != land.slotType) { land.slotType = newType; Repaint(); }

        int newX = Mathf.Clamp(EditorGUILayout.IntField(land.x, GUILayout.Width(46)), 0, _level.width  - 1);
        if (newX != land.x) { MoveLand(land, newX, land.y); Repaint(); }

        int newY = Mathf.Clamp(EditorGUILayout.IntField(land.y, GUILayout.Width(46)), 0, _level.height - 1);
        if (newY != land.y) { MoveLand(land, land.x, newY); Repaint(); }

        land.itemId = EditorGUILayout.TextField(land.itemId ?? "", GUILayout.Width(90));

        GUI.backgroundColor = new Color(0.8f, 0.25f, 0.25f);
        if (GUILayout.Button("Del", GUILayout.Width(38), GUILayout.Height(20)))
        {
            DeleteLand(i);
            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = prev;
            return;
        }
        GUI.backgroundColor = prev;
        EditorGUILayout.EndHorizontal();
    }

    // ---- Tree panel -------------------------------------------------------
    private void DrawTreePanel()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Trees", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.55f, 0.80f, 0.30f);
        if (GUILayout.Button("+ Spawn Tree", GUILayout.Width(110), GUILayout.Height(22)))
            SpawnTree();
        GUI.backgroundColor = prev;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("#",        GUILayout.Width(22));
        GUILayout.Label("Tree ID",  GUILayout.Width(90));
        GUILayout.Label("X",        GUILayout.Width(40));
        GUILayout.Label("Y",        GUILayout.Width(40));
        GUILayout.Label("ItemType", GUILayout.Width(70));
        GUILayout.Label("SlotReq",  GUILayout.Width(70));
        EditorGUILayout.EndHorizontal();

        _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
        for (int i = 0; i < _spawnedTrees.Count; i++)
        {
            DrawTreeRow(i);
            if (_selectedTreeIndex == i)
                DrawTreeAttributes(i);
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawTreeRow(int i)
    {
        TreeData tree = _spawnedTrees[i];
        bool sel = (_selectedTreeIndex == i);

        Rect rowRect = EditorGUILayout.BeginHorizontal();
        EditorGUI.DrawRect(rowRect, sel
            ? new Color(0.22f, 0.42f, 0.22f)
            : (i % 2 == 0 ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.19f, 0.19f, 0.19f)));

        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = sel ? new Color(0.5f, 1f, 0.5f) : Color.grey;
        if (GUILayout.Button(i.ToString(), GUILayout.Width(22), GUILayout.Height(20)))
        {
            _selectedTreeIndex = sel ? -1 : i;
            Repaint();
        }
        GUI.backgroundColor = prev;

        tree.treeId = EditorGUILayout.TextField(tree.treeId ?? "", GUILayout.Width(90));

        int newX = Mathf.Clamp(EditorGUILayout.IntField(tree.x, GUILayout.Width(40)), 0, _level.width  - 1);
        if (newX != tree.x) { tree.x = newX; Repaint(); }

        int newY = Mathf.Clamp(EditorGUILayout.IntField(tree.y, GUILayout.Width(40)), 0, _level.height - 1);
        if (newY != tree.y) { tree.y = newY; Repaint(); }

        tree.itemType         = (ItemType)EditorGUILayout.EnumPopup(tree.itemType,         GUILayout.Width(70));
        tree.requiredSlotType = (SlotType)EditorGUILayout.EnumPopup(tree.requiredSlotType, GUILayout.Width(70));

        GUI.backgroundColor = new Color(0.8f, 0.25f, 0.25f);
        if (GUILayout.Button("Del", GUILayout.Width(38), GUILayout.Height(20)))
        {
            DeleteTree(i);
            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = prev;
            return;
        }
        GUI.backgroundColor = prev;
        EditorGUILayout.EndHorizontal();
    }

    // Expanded attribute editor shown beneath a selected tree row
    private void DrawTreeAttributes(int i)
    {
        TreeData tree = _spawnedTrees[i];

        Rect bg = EditorGUILayout.BeginVertical();
        EditorGUI.DrawRect(bg, new Color(0.15f, 0.28f, 0.15f));
        GUILayout.Space(2);

        EditorGUI.indentLevel++;
        GUILayout.Label("Tree Attributes", EditorStyles.boldLabel);

        tree.treeId           = EditorGUILayout.TextField("Tree ID",      tree.treeId           ?? "");
        tree.itemType         = (ItemType)EditorGUILayout.EnumPopup("Item Type",     tree.itemType);
        tree.requiredSlotType = (SlotType)EditorGUILayout.EnumPopup("Required Slot", tree.requiredSlotType);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Position");
        EditorGUILayout.LabelField("X", GUILayout.Width(14));
        tree.x = Mathf.Clamp(EditorGUILayout.IntField(tree.x, GUILayout.Width(50)), 0, _level.width  - 1);
        EditorGUILayout.LabelField("Y", GUILayout.Width(14));
        tree.y = Mathf.Clamp(EditorGUILayout.IntField(tree.y, GUILayout.Width(50)), 0, _level.height - 1);
        EditorGUILayout.EndHorizontal();

        GUILayout.Label("Custom Notes");
        tree.customNotes = EditorGUILayout.TextArea(tree.customNotes ?? "", GUILayout.Height(36));

        EditorGUI.indentLevel--;
        GUILayout.Space(2);
        EditorGUILayout.EndVertical();
    }

    // =======================================================================
    // LAND helpers
    // =======================================================================
    private void SpawnLand()
    {
        LevelCellData target = null;
        for (int y = 0; y < _level.height && target == null; y++)
            for (int x = 0; x < _level.width && target == null; x++)
            {
                var c = GetCell(x, y);
                if (c != null && !_spawnedLands.Contains(c)) target = c;
            }

        if (target == null)
        {
            EditorUtility.DisplayDialog("No space", "All cells are already spawned lands.", "OK");
            return;
        }

        target.slotType = _paintType;
        target.itemId   = _paintItemId;
        _spawnedLands.Add(target);
        _selectedLandIndex = _spawnedLands.Count - 1;
        Repaint();
    }

    private void MoveLand(LevelCellData land, int newX, int newY)
    {
        LevelCellData dest = GetCell(newX, newY);
        if (dest == null) return;

        bool destIsLand = _spawnedLands.Contains(dest);

        SlotType tmpType = dest.slotType; dest.slotType = land.slotType; land.slotType = tmpType;
        string   tmpItem = dest.itemId;   dest.itemId   = land.itemId;   land.itemId   = tmpItem;

        int srcIdx  = _spawnedLands.IndexOf(land);
        int destIdx = _spawnedLands.IndexOf(dest);

        if (destIsLand)
        {
            _spawnedLands[srcIdx]  = dest;
            _spawnedLands[destIdx] = land;
        }
        else
        {
            _spawnedLands[srcIdx] = dest;
        }

        _selectedLandIndex = _spawnedLands.IndexOf(dest);
    }

    private void DeleteLand(int i)
    {
        var land = _spawnedLands[i];
        land.slotType = SlotType.Dirt;
        land.itemId   = "";
        _spawnedLands.RemoveAt(i);
        _selectedLandIndex = Mathf.Clamp(_selectedLandIndex, -1, _spawnedLands.Count - 1);
        Repaint();
    }

    // =======================================================================
    // TREE helpers
    // =======================================================================
    private void SpawnTree()
    {
        var tree = new TreeData
        {
            treeId           = "tree_" + (_level.trees.Count + 1).ToString("D2"),
            x                = 0,
            y                = 0,
            itemType         = ItemType.Plant,
            requiredSlotType = SlotType.Dirt,
            customNotes      = ""
        };
        _level.trees.Add(tree);
        _spawnedTrees.Add(tree);
        _selectedTreeIndex = _spawnedTrees.Count - 1;
        _activeTab = BottomTab.Tree;
        Repaint();
    }

    private void DeleteTree(int i)
    {
        var tree = _spawnedTrees[i];
        _level.trees.Remove(tree);
        _spawnedTrees.RemoveAt(i);
        _selectedTreeIndex = Mathf.Clamp(_selectedTreeIndex, -1, _spawnedTrees.Count - 1);
        Repaint();
    }

    // =======================================================================
    // GRID helpers
    // =======================================================================
    private LevelCellData GetCell(int x, int y)
    {
        return _level.cells.Find(c => c.x == x && c.y == y);
    }

    private void EnsureGridFilled()
    {
        bool dirty = false;
        for (int y = 0; y < _level.height; y++)
            for (int x = 0; x < _level.width; x++)
                if (_level.cells.Find(c => c.x == x && c.y == y) == null)
                {
                    _level.cells.Add(new LevelCellData { x = x, y = y, slotType = SlotType.Dirt });
                    dirty = true;
                }
        if (dirty) Repaint();
    }

    private void InitGrid(int w, int h)
    {
        _level.width  = w;
        _level.height = h;
        _level.cells.Clear();
        EnsureGridFilled();
        Repaint();
    }

    private void ResizeGrid(int newW, int newH)
    {
        _spawnedLands.RemoveAll(c => c.x >= newW || c.y >= newH);
        _level.cells.RemoveAll(c => c.x >= newW || c.y >= newH);
        _level.trees.RemoveAll(t => t.x >= newW || t.y >= newH);
        _spawnedTrees.RemoveAll(t => t.x >= newW || t.y >= newH);
        _level.width  = newW;
        _level.height = newH;
        EnsureGridFilled();
    }

    private void ClearAll()
    {
        _spawnedLands.Clear();
        _spawnedTrees.Clear();
        _level.trees.Clear();
        _selectedLandIndex = -1;
        _selectedTreeIndex = -1;
        InitGrid(_level.width, _level.height);
    }

    // =======================================================================
    // JSON Save / Load
    // =======================================================================
    private void SaveToJson()
    {
        string name = string.IsNullOrWhiteSpace(_level.levelName) ? "level" : _level.levelName;
        string path = EditorUtility.SaveFilePanel("Save Level as JSON", Application.dataPath, name + ".json", "json");
        if (string.IsNullOrEmpty(path)) return;

        File.WriteAllText(path, JsonUtility.ToJson(_level, prettyPrint: true));
        AssetDatabase.Refresh();
        Debug.Log("[LevelEditor] Saved to " + path);
    }

    private void LoadFromJson()
    {
        string path = EditorUtility.OpenFilePanel("Load Level JSON", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path)) return;

        LevelData loaded = JsonUtility.FromJson<LevelData>(System.IO.File.ReadAllText(path));
        if (loaded == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not parse the JSON file.", "OK");
            return;
        }

        _level             = loaded;
        _spawnedLands.Clear();
        _spawnedTrees      = new List<TreeData>(_level.trees);
        _selectedLandIndex = -1;
        _selectedTreeIndex = -1;
        Repaint();
        Debug.Log("[LevelEditor] Loaded from " + path);
    }
}